using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
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

    /// <summary>
    /// Inspection of the GCD queue contents.
    /// Public because InternalsVisibleTo is not configured in this project; production code uses Dispatch directly.
    /// </summary>
    public IReadOnlyList<AbilityCandidate> InspectGcdQueue() => _gcdQueue;

    /// <summary>
    /// Inspection of the oGCD queue contents.
    /// Public because InternalsVisibleTo is not configured in this project; production code uses Dispatch directly.
    /// </summary>
    public IReadOnlyList<AbilityCandidate> InspectOgcdQueue() => _ogcdQueue;

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

        // Tasks 5+ will add gates and dispatch logic here. Skeleton returns Empty so
        // subsequent TDD loops can start from a clean "nothing dispatches" state.
        return SchedulerDispatchResult.Empty;
    }
}
