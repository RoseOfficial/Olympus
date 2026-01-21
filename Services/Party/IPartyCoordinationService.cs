using System;
using System.Collections.Generic;
using Olympus.Ipc;

namespace Olympus.Services.Party;

/// <summary>
/// Interface for party coordination between multiple Olympus instances.
/// Enables heal overlap prevention and cooldown coordination.
/// </summary>
public interface IPartyCoordinationService
{
    /// <summary>
    /// Whether party coordination is enabled and active.
    /// </summary>
    bool IsPartyCoordinationEnabled { get; }

    /// <summary>
    /// Number of remote Olympus instances detected in the party.
    /// </summary>
    int RemoteInstanceCount { get; }

    /// <summary>
    /// Whether any remote instances are healers.
    /// </summary>
    bool HasRemoteHealers { get; }

    /// <summary>
    /// Unique identifier for this Olympus instance.
    /// </summary>
    Guid InstanceId { get; }

    /// <summary>
    /// Checks if a target is currently reserved by another Olympus instance.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <returns>True if another instance has reserved this target for healing.</returns>
    bool IsTargetReservedByOther(uint entityId);

    /// <summary>
    /// Reserves a target for healing and broadcasts the intent to other instances.
    /// </summary>
    /// <param name="entityId">The target entity ID.</param>
    /// <param name="healAmount">Estimated heal amount.</param>
    /// <param name="actionId">Action ID being used.</param>
    /// <param name="castTimeMs">Cast time in milliseconds (0 for instant).</param>
    /// <returns>True if reservation succeeded.</returns>
    bool ReserveTarget(uint entityId, int healAmount, uint actionId, int castTimeMs = 0);

    /// <summary>
    /// Notifies that a heal has landed on a target.
    /// Clears the local reservation and broadcasts to other instances.
    /// </summary>
    /// <param name="entityId">The healed target entity ID.</param>
    /// <param name="amount">Actual heal amount.</param>
    /// <param name="actionId">Action ID that was used.</param>
    void OnHealLanded(uint entityId, int amount, uint actionId);

    /// <summary>
    /// Notifies that a major cooldown was used.
    /// Broadcasts to other instances for coordination.
    /// </summary>
    /// <param name="actionId">The cooldown action ID.</param>
    /// <param name="recastTimeMs">Recast time in milliseconds.</param>
    void OnCooldownUsed(uint actionId, int recastTimeMs);

    /// <summary>
    /// Gets all current remote heal reservations.
    /// Key is target entity ID, value is the reservation info.
    /// </summary>
    IReadOnlyDictionary<uint, HealReservation> GetRemoteReservations();

    /// <summary>
    /// Gets all known remote Olympus instances.
    /// </summary>
    IReadOnlyList<RemoteOlympusInstance> GetRemoteInstances();

    /// <summary>
    /// Gets the estimated incoming heal amount for a target from remote instances.
    /// </summary>
    /// <param name="entityId">The target entity ID.</param>
    /// <returns>Total estimated heal amount from remote instances.</returns>
    int GetRemotePendingHealAmount(uint entityId);

    #region AoE Heal Coordination

    /// <summary>
    /// Checks if an AoE heal is currently reserved by another Olympus instance.
    /// </summary>
    /// <returns>True if another instance has reserved an AoE heal.</returns>
    bool IsAoEHealReservedByOther();

    /// <summary>
    /// Reserves an AoE heal and broadcasts the intent to other instances.
    /// </summary>
    /// <param name="actionId">Action ID of the AoE heal.</param>
    /// <param name="healPotency">Heal potency of the AoE heal.</param>
    /// <param name="castTimeMs">Cast time in milliseconds (0 for instant).</param>
    void ReserveAoEHeal(uint actionId, int healPotency, int castTimeMs);

    #endregion

    #region Cooldown Coordination

    /// <summary>
    /// Checks if a specific cooldown is currently active (on recast) on any remote instance.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>True if any remote instance has this cooldown on recast.</returns>
    bool IsCooldownActiveRemotely(uint actionId);

    /// <summary>
    /// Gets the count of remote instances that have a specific cooldown on recast.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Number of remote instances with this cooldown active.</returns>
    int GetRemoteCooldownCount(uint actionId);

    /// <summary>
    /// Gets the shortest remaining recast time for a cooldown across all remote instances.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Shortest remaining recast in seconds, or 0 if no remote has it on cooldown.</returns>
    float GetShortestRemoteCooldownRemaining(uint actionId);

    /// <summary>
    /// Checks if any party mitigation was used recently by a remote instance.
    /// Useful for preventing mitigation stacking within a time window.
    /// </summary>
    /// <param name="withinSeconds">Time window to check (default 3 seconds).</param>
    /// <returns>True if any coordinated mitigation was used within the time window.</returns>
    bool WasPartyMitigationUsedRecently(float withinSeconds = 3f);

    /// <summary>
    /// Gets all active remote cooldowns for a specific action.
    /// </summary>
    /// <param name="actionId">The action ID to query.</param>
    /// <returns>List of active cooldown info from remote instances.</returns>
    IReadOnlyList<RemoteCooldownInfo> GetRemoteCooldowns(uint actionId);

    #endregion

    #region Raid Buff Coordination

    /// <summary>
    /// Checks if any remote instance has announced intent to use a raid buff within the specified window.
    /// Used to determine if we should align our buffs with an incoming burst window.
    /// </summary>
    /// <param name="withinSeconds">Time window to check for pending intents (default 5 seconds).</param>
    /// <returns>True if any remote instance has a pending raid buff intent.</returns>
    bool HasPendingRaidBuffIntent(float withinSeconds = 5f);

    /// <summary>
    /// Gets all pending raid buff intents from remote instances.
    /// </summary>
    /// <returns>List of remote raid buff states with pending intents.</returns>
    IReadOnlyList<RemoteRaidBuffState> GetPendingRaidBuffIntents();

    /// <summary>
    /// Checks if the party is currently in a burst window (raid buffs active).
    /// </summary>
    /// <returns>True if any remote instance has active raid buffs.</returns>
    bool IsInBurstWindow();

    /// <summary>
    /// Gets the remaining time on the current burst window.
    /// </summary>
    /// <returns>Seconds remaining in burst window, or 0 if not in burst.</returns>
    float GetBurstWindowRemaining();

    /// <summary>
    /// Announces intent to use a raid buff.
    /// Other instances will receive this and can choose to align their buffs.
    /// </summary>
    /// <param name="actionId">The raid buff action ID.</param>
    /// <param name="secondsUntilActivation">Seconds until the buff will be activated (0 for immediate).</param>
    void AnnounceRaidBuffIntent(uint actionId, float secondsUntilActivation = 0f);

    /// <summary>
    /// Notifies that a raid buff was actually used.
    /// Broadcasts the burst window start to other instances.
    /// </summary>
    /// <param name="actionId">The raid buff action ID.</param>
    /// <param name="recastTimeMs">Recast time in milliseconds.</param>
    void OnRaidBuffUsed(uint actionId, int recastTimeMs);

    /// <summary>
    /// Checks if our raid buff is approximately aligned with remote instances.
    /// Returns false if desync exceeds the configured threshold, indicating we should use buffs independently.
    /// </summary>
    /// <param name="actionId">The action ID to check alignment for.</param>
    /// <param name="toleranceSeconds">Acceptable desync tolerance (default: uses config value).</param>
    /// <returns>True if buffs are aligned or no remote data exists, false if significantly desynced.</returns>
    bool IsRaidBuffAligned(uint actionId, float toleranceSeconds = 0f);

    /// <summary>
    /// Checks if any remote DPS instance is running.
    /// </summary>
    bool HasRemoteDps { get; }

    #endregion

    /// <summary>
    /// Updates the service state. Should be called once per frame.
    /// </summary>
    /// <param name="playerEntityId">The local player's entity ID.</param>
    /// <param name="jobId">The local player's job ID.</param>
    /// <param name="isEnabled">Whether Olympus is currently enabled.</param>
    void Update(uint playerEntityId, uint jobId, bool isEnabled);

    /// <summary>
    /// Clears all local state (reservations, remote instances).
    /// Called when leaving combat or changing zones.
    /// </summary>
    void Clear();
}
