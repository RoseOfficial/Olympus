using System;

namespace Olympus.Services.Pull;

/// <summary>
/// Per-frame state machine that classifies the player's pull intent.
/// Inputs are passed in by <c>Plugin.Update</c> rather than read internally,
/// so the service is unit-testable without Dalamud dependencies.
/// </summary>
public interface IPullIntentService
{
    /// <summary>Current state. Updated by <see cref="Update"/>.</summary>
    PullIntent Current { get; }

    /// <summary>
    /// Drives state transitions. Call once per frame from <c>Plugin.Update</c>
    /// before rotation execution.
    /// </summary>
    /// <param name="isPlayerCasting">True if <c>LocalPlayer.IsCasting</c>.</param>
    /// <param name="isCastTargetHostile">True if the cast target is a hostile actor.</param>
    /// <param name="queuedActionId">
    /// Currently queued action ID, or null if no action is queued.
    /// Read from <c>ActionManager.QueuedActionId</c>.
    /// </param>
    /// <param name="isQueuedActionHostile">True if the queued action is hostile.</param>
    /// <param name="isInCombat">True if <c>LocalPlayer.StatusFlags &amp; InCombat</c>.</param>
    /// <param name="utcNow">Current UTC time. Injected for testability.</param>
    void Update(
        bool isPlayerCasting,
        bool isCastTargetHostile,
        uint? queuedActionId,
        bool isQueuedActionHostile,
        bool isInCombat,
        DateTime utcNow);
}
