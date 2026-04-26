using Olympus.Services;

namespace Olympus.Services.Consumables;

/// <summary>
/// Centralizes consumable inventory probing, current-tier ID resolution,
/// HQ-vs-NQ preference, recast cooldown queries, and the unified
/// "should we use a tincture this frame" decision.
/// </summary>
public interface IConsumableService
{
    /// <summary>
    /// True if the player has the appropriate stat tincture for this job in inventory
    /// AND the tincture recast group is off cooldown.
    /// </summary>
    bool IsTinctureReady(uint jobId);

    /// <summary>
    /// Resolves the tincture item ID (HQ preferred over NQ) for the given job.
    /// </summary>
    /// <returns>True if a tincture is in inventory; false otherwise.</returns>
    bool TryGetTinctureForJob(uint jobId, out uint itemId, out bool isHq);

    /// <summary>
    /// The unified gate. Returns true iff:
    /// - Master toggle is on
    /// - Current zone is high-end
    /// - <see cref="IsTinctureReady"/> returns true
    /// - Burst is active or imminent (within 5s)
    /// - AND one of:
    ///   - <paramref name="prePullPhase"/> = true and PullIntent != None (Path 1)
    ///   - <paramref name="prePullPhase"/> = false and inCombat = true (Path 2)
    /// </summary>
    bool ShouldUseTinctureNow(IBurstWindowService burstWindow, bool inCombat, bool prePullPhase);

    /// <summary>
    /// Called when a tincture push was attempted but inventory was empty.
    /// Throttles a one-shot per-fight chat warning.
    /// </summary>
    void OnTinctureSkippedDueToEmptyBag(uint jobId);

    /// <summary>Called on combat-state false→true transitions; resets per-fight warning.</summary>
    void OnCombatStateChanged(bool inCombat);

    /// <summary>Called on territory change; resets per-fight and per-zone warnings.</summary>
    void OnTerritoryChanged();
}
