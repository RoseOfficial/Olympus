using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin.Services;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
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

    /// <summary>
    /// Push a ground-targeted oGCD candidate. Dispatch routes through
    /// <c>ExecuteGroundTargetedOgcd(action, position)</c>. Used for healer
    /// abilities like Asylum, Liturgy of the Bell, and Earthly Star.
    /// </summary>
    public void PushGroundTargetedOgcd(AbilityBehavior behavior, Vector3 position, int priority,
                                       Action<IRotationContext>? onDispatched = null)
        => _ogcdQueue.Add(new AbilityCandidate
        {
            Behavior = behavior,
            TargetId = 0,
            GroundPosition = position,
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

        // Memo for per-ability targeting overrides: each distinct (strategy, range) pair is
        // resolved at most once per dispatch pass and the result is reused for subsequent
        // candidates with the same strategy and range. Range is included in the key because
        // two abilities with the same strategy but different ranges may resolve to different
        // enemies (e.g. a 3f melee ability and a 25f ranged ability both using HighestHp).
        // Only allocated when at least one candidate carries an override.
        Dictionary<(EnemyTargetingStrategy Strategy, float Range), ulong>? overrideMemo = null;

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

            // Gate: target. Skipped when TargetId == 0 (self-targeted, ground-targeted, or intentional).
            if (candidate.TargetId != 0 && ctx.ObjectTable is { } objectTable)
            {
                var target = objectTable.SearchById(candidate.TargetId);
                if (target is null)
                {
                    RecordFail(candidate, "Target missing");
                    continue;
                }
            }

            // Gate: cooldown / charges
            //
            // Non-charge oGCDs: pre-check via IsActionReady. IsActionReady operates on the
            // level-resolved effective.ActionId, so charge-based actions with level replacements
            // (e.g. MCH GaussRound -> DoubleCheck at Lv.92) are automatically checked against
            // the replacement's charge stack — no separate ChargeSource field is needed.
            //
            // Non-charge GCDs: skip the pre-check. For plain GCDs on the global recast group,
            // GetCurrentCharges returns 0 during the GCD roll — that would incorrectly reject
            // valid queue-window dispatches (the server-side action queue accepts UseAction
            // in the last ~0.5s of the GCD and fires the action on rollover). ExecuteGcd
            // delegates to UseAction which handles the queue window correctly; if the GCD
            // is on its own independent cooldown (Sonic Break, Gnashing Fang, etc.) UseAction
            // returns false and we fall through to DispatchRejected at the bottom of the loop.
            if (isOgcd && !_actionService.IsActionReady(effective.ActionId))
            {
                var remaining = _actionService.GetCooldownRemaining(effective.ActionId);
                RecordFail(candidate, $"Cooldown {remaining:F1}s");
                continue;
            }

            // Gate: mechanic
            if (candidate.Behavior.MechanicGate && effective.CastTime > 0f && _timelineService is not null)
            {
                if (MechanicCastGate.ShouldBlock(ctx, effective.CastTime))
                {
                    RecordFail(candidate, "Mechanic");
                    continue;
                }
            }

            // Per-ability targeting override: re-resolve the dispatch target using the
            // declared strategy rather than the target ID the module pre-resolved at push
            // time. Falls back to the module-pushed ID when the service returns no result.
            // Results are memoised per (strategy, range) pair to avoid redundant scans.
            var dispatchTargetId = candidate.TargetId;
            if (candidate.Behavior.TargetingOverride is { } overrideStrategy
                && ctx.TargetingService is { } targetingSvc)
            {
                var range = effective.Range > 0f ? effective.Range : 25f;
                var memoKey = (overrideStrategy, range);
                if (overrideMemo is null || !overrideMemo.TryGetValue(memoKey, out var memoHit))
                {
                    var resolved = targetingSvc.FindEnemy(overrideStrategy, range, ctx.Player);
                    var resolvedId = resolved?.GameObjectId ?? candidate.TargetId;
                    overrideMemo ??= new Dictionary<(EnemyTargetingStrategy Strategy, float Range), ulong>(4);
                    overrideMemo[memoKey] = resolvedId;
                    dispatchTargetId = resolvedId;
                }
                else
                {
                    dispatchTargetId = memoHit;
                }
            }

            bool dispatched;
            if (candidate.GroundPosition is { } position)
            {
                // Ground-targeted dispatch (oGCD only). Asylum, Liturgy of the Bell,
                // Earthly Star, Sacred Soil, etc.
                dispatched = _actionService.ExecuteGroundTargetedOgcd(effective, position);
            }
            else if (candidate.Behavior.ReplacementBaseId is { } rawId)
            {
                dispatched = isOgcd
                    ? _actionService.ExecuteOgcdRaw(effective, rawId, dispatchTargetId)
                    : _actionService.ExecuteGcdRaw(effective, rawId, dispatchTargetId);
            }
            else
            {
                dispatched = isOgcd
                    ? _actionService.ExecuteOgcd(effective, dispatchTargetId)
                    : _actionService.ExecuteGcd(effective, dispatchTargetId);
            }

            if (dispatched)
            {
                candidate.OnDispatched?.Invoke(ctx);
                return new SchedulerDispatchResult
                {
                    Dispatched = true,
                    Winner = candidate.Behavior,
                    GateFailReasons = _configuration.IsDebugWindowOpen ? _lastFailReasons.ToArray() : Array.Empty<string>(),
                };
            }

            RecordFail(candidate, "DispatchRejected");
        }

        return new SchedulerDispatchResult
        {
            Dispatched = false,
            Winner = null,
            GateFailReasons = _configuration.IsDebugWindowOpen ? _lastFailReasons.ToArray() : Array.Empty<string>(),
        };
    }

    private void RecordFail(in AbilityCandidate candidate, string reason)
    {
        if (!_configuration.IsDebugWindowOpen) return;
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
