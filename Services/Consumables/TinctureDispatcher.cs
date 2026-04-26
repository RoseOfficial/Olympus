using Dalamud.Plugin.Services;
using Olympus.Services.Action;

namespace Olympus.Services.Consumables;

public interface ITinctureDispatcher
{
    /// <summary>See <see cref="TinctureDispatcher.TryDispatch"/>.</summary>
    bool TryDispatch(uint jobId, bool inCombat, bool prePullPhase);
}

/// <summary>
/// Shared dispatch helper used by both Path 1 (PrePullModule's TinctureCandidate
/// for opener) and Path 2 (in-combat re-pot push from BaseRotation). Owns the
/// "check gates -> resolve item -> dispatch" flow so both paths behave identically.
/// </summary>
public sealed class TinctureDispatcher : ITinctureDispatcher
{
    private readonly IConsumableService _consumables;
    private readonly IBurstWindowService _burstWindow;
    private readonly IActionService _actionService;
    private readonly IClientState _clientState;

    public TinctureDispatcher(
        IConsumableService consumables,
        IBurstWindowService burstWindow,
        IActionService actionService,
        IClientState clientState)
    {
        _consumables = consumables;
        _burstWindow = burstWindow;
        _actionService = actionService;
        _clientState = clientState;
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

        var targetId = _clientState.LocalPlayer?.GameObjectId ?? 0ul;
        return _actionService.ExecuteItem(itemId, isHq, targetId);
    }
}
