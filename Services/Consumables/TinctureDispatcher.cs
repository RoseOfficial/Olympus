using Olympus.Services.Action;

namespace Olympus.Services.Consumables;

/// <summary>
/// Shared dispatch helper used by both Path 1 (PrePullModule's TinctureCandidate
/// for opener) and Path 2 (in-combat re-pot push from BaseRotation). Owns the
/// "check gates -> resolve item -> dispatch" flow so both paths behave identically.
/// </summary>
public sealed class TinctureDispatcher
{
    private readonly IConsumableService _consumables;
    private readonly IBurstWindowService _burstWindow;
    private readonly IActionService _actionService;

    public TinctureDispatcher(
        IConsumableService consumables,
        IBurstWindowService burstWindow,
        IActionService actionService)
    {
        _consumables = consumables;
        _burstWindow = burstWindow;
        _actionService = actionService;
    }

    /// <summary>
    /// Attempts to dispatch a tincture for the given job. Returns true if dispatched
    /// (caller should treat the frame as having spent its oGCD slot).
    /// </summary>
    public bool TryDispatch(uint jobId, bool inCombat, bool prePullPhase)
    {
        if (!_consumables.ShouldUseTinctureNow(_burstWindow, inCombat, prePullPhase))
            return false;

        if (!_consumables.TryGetTinctureForJob(jobId, out var itemId, out var isHq))
        {
            _consumables.OnTinctureSkippedDueToEmptyBag(jobId);
            return false;
        }

        return _actionService.ExecuteItem(itemId, isHq, targetId: 0ul);
    }
}
