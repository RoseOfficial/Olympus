using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Olympus.Models.Action;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Timeline;

namespace Olympus.Rotation.Common.Scheduling;

/// <summary>
/// Per-rotation, per-frame priority scheduler. Modules push candidates into
/// the GCD and oGCD queues during <c>CollectCandidates</c>; the scheduler
/// dispatches the best-fit candidate per queue in priority order.
/// </summary>
public sealed class RotationScheduler
{
    private readonly IActionService _actionService;
    private readonly IJobGauges _jobGauges;
    private readonly Configuration _configuration;
    private readonly ITimelineService? _timelineService;
    private readonly IErrorMetricsService? _errorMetrics;

    private readonly List<AbilityCandidate> _gcdQueue = new(capacity: 32);
    private readonly List<AbilityCandidate> _ogcdQueue = new(capacity: 32);
    private readonly List<string> _lastFailReasons = new(capacity: 16);
    private int _insertionCounter;

    public RotationScheduler(
        IActionService actionService,
        IJobGauges jobGauges,
        Configuration configuration,
        ITimelineService? timelineService = null,
        IErrorMetricsService? errorMetrics = null)
    {
        _actionService = actionService;
        _jobGauges = jobGauges;
        _configuration = configuration;
        _timelineService = timelineService;
        _errorMetrics = errorMetrics;
    }

    public void Reset()
    {
        _gcdQueue.Clear();
        _ogcdQueue.Clear();
        _lastFailReasons.Clear();
        _insertionCounter = 0;
    }

    public void PushGcd(AbilityBehavior behavior, ulong targetId, int priority,
                        Action<IRotationContext>? onDispatched = null)
        => _gcdQueue.Add(new AbilityCandidate
        {
            Behavior = behavior,
            TargetId = targetId,
            Priority = priority,
            InsertionOrder = _insertionCounter++,
            OnDispatched = onDispatched,
        });

    public void PushOgcd(AbilityBehavior behavior, ulong targetId, int priority,
                         Action<IRotationContext>? onDispatched = null)
        => _ogcdQueue.Add(new AbilityCandidate
        {
            Behavior = behavior,
            TargetId = targetId,
            Priority = priority,
            InsertionOrder = _insertionCounter++,
            OnDispatched = onDispatched,
        });

    public SchedulerDispatchResult DispatchGcd(IRotationContext ctx)
        => Dispatch(_gcdQueue, ctx, isOgcd: false);

    public SchedulerDispatchResult DispatchOgcd(IRotationContext ctx)
        => Dispatch(_ogcdQueue, ctx, isOgcd: true);

    /// <summary>Test-only inspection of the GCD queue contents.</summary>
    internal IReadOnlyList<AbilityCandidate> InspectGcdQueue() => _gcdQueue;

    /// <summary>Test-only inspection of the oGCD queue contents.</summary>
    internal IReadOnlyList<AbilityCandidate> InspectOgcdQueue() => _ogcdQueue;

    private SchedulerDispatchResult Dispatch(List<AbilityCandidate> queue, IRotationContext ctx, bool isOgcd)
    {
        _lastFailReasons.Clear();
        if (queue.Count == 0)
            return SchedulerDispatchResult.Empty;

        queue.Sort(static (a, b) =>
        {
            var cmp = a.Priority.CompareTo(b.Priority);
            return cmp != 0 ? cmp : a.InsertionOrder.CompareTo(b.InsertionOrder);
        });

        foreach (var candidate in queue)
        {
            var effective = ResolveLevelReplacement(candidate.Behavior, ctx.Player.Level);

            // Gate: level
            if (ctx.Player.Level < effective.MinLevel)
            {
                RecordFail(candidate, $"Level<{effective.MinLevel}");
                continue;
            }

            // Gate: toggle
            if (candidate.Behavior.Toggle is { } toggle && !toggle(_configuration))
            {
                RecordFail(candidate, "Toggle");
                continue;
            }

            // Gate: proc buff
            if (candidate.Behavior.ProcBuff is { } procId && !_actionService.PlayerHasStatus(procId))
            {
                RecordFail(candidate, $"ProcBuff {procId}");
                continue;
            }

            // Gate: combo step (typed gauge predicate)
            if (candidate.Behavior.ComboStep is { } comboStep)
            {
                bool stepOk;
                try { stepOk = comboStep(_jobGauges); }
                catch (Exception ex)
                {
                    _errorMetrics?.RecordError("Scheduler", $"ComboStep threw: {ex.Message}");
                    RecordFail(candidate, "ComboStep threw");
                    continue;
                }
                if (!stepOk)
                {
                    RecordFail(candidate, "ComboStep");
                    continue;
                }
            }

            // Gate: adjusted action probe
            if (candidate.Behavior.AdjustedActionProbe is { } probeId)
            {
                var adjusted = _actionService.GetAdjustedActionId(probeId);
                if (adjusted != effective.ActionId)
                {
                    RecordFail(candidate, $"AdjustedActionProbe (expected {effective.ActionId}, got {adjusted})");
                    continue;
                }
            }

            // Gate: target. Skipped when TargetId == 0 (self-targeted or intentional).
            if (candidate.TargetId != 0 && ctx.ObjectTable is { } objectTable)
            {
                var target = objectTable.SearchById(candidate.TargetId);
                if (target is null)
                {
                    RecordFail(candidate, "Target missing");
                    continue;
                }
            }

            // Provisional dispatch so the "level matches" test passes.
            // Tasks 6-13 will add the remaining gates before this line; Task 14 will
            // implement the real dispatch path (raw-ID variant). Until then, use
            // the existing ExecuteGcd/ExecuteOgcd signature.
            var dispatched = isOgcd
                ? _actionService.ExecuteOgcd(effective, candidate.TargetId)
                : _actionService.ExecuteGcd(effective, candidate.TargetId);

            if (dispatched)
            {
                candidate.OnDispatched?.Invoke(ctx);
                return new SchedulerDispatchResult
                {
                    Dispatched = true,
                    Winner = candidate.Behavior,
                    GateFailReasons = _lastFailReasons.ToArray(),
                };
            }

            RecordFail(candidate, "DispatchRejected");
        }

        return new SchedulerDispatchResult
        {
            Dispatched = false,
            Winner = null,
            GateFailReasons = _lastFailReasons.ToArray(),
        };
    }

    private void RecordFail(in AbilityCandidate candidate, string reason)
    {
        if (_lastFailReasons.Count >= 16) return;
        _lastFailReasons.Add($"{candidate.Behavior.Action.Name}: {reason}");
    }

    private static ActionDefinition ResolveLevelReplacement(AbilityBehavior behavior, byte playerLevel)
    {
        if (behavior.LevelReplacements is null) return behavior.Action;
        ActionDefinition current = behavior.Action;
        byte bestLevel = 0;
        foreach (var (level, replacement) in behavior.LevelReplacements)
        {
            if (playerLevel >= level && level >= bestLevel)
            {
                current = replacement;
                bestLevel = level;
            }
        }
        return current;
    }
}
