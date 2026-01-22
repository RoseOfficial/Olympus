using System;
using System.Collections.Generic;
using System.Numerics;
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
    /// Checks if any personal defensive was used recently by a remote tank instance.
    /// Used for tank-to-tank mitigation staggering to maximize coverage.
    /// </summary>
    /// <param name="withinSeconds">Time window to check (default 3 seconds).</param>
    /// <returns>True if any personal defensive was used within the time window.</returns>
    bool WasPersonalDefensiveUsedRecently(float withinSeconds = 3f);

    /// <summary>
    /// Checks if any tank invulnerability was used recently by a remote tank instance.
    /// Used to prevent both tanks from wasting invulns simultaneously during emergencies.
    /// </summary>
    /// <param name="withinSeconds">Time window to check (default 5 seconds).</param>
    /// <returns>True if any invulnerability was used within the time window.</returns>
    bool WasInvulnerabilityUsedRecently(float withinSeconds = 5f);

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

    /// <summary>
    /// Gets the current burst window state for healer decision-making.
    /// Aggregates information about pending and active burst windows from remote DPS instances.
    /// </summary>
    /// <returns>Current burst window state including timing and activity information.</returns>
    BurstWindowState GetBurstWindowState();

    /// <summary>
    /// Convenience method to check if a burst is either imminent or currently active.
    /// </summary>
    /// <param name="imminentSeconds">Seconds to consider as "imminent" (default 5 seconds).</param>
    /// <returns>True if a burst is happening or about to happen.</returns>
    bool IsBurstImminentOrActive(float imminentSeconds = 5f);

    /// <summary>
    /// Gets the number of seconds until the next burst window.
    /// </summary>
    /// <returns>Seconds until burst, 0 if already active, -1 if unknown.</returns>
    float GetSecondsUntilBurst();

    #endregion

    #region Healer Gauge Coordination

    /// <summary>
    /// Broadcasts the local healer's gauge state to other instances.
    /// </summary>
    /// <param name="jobId">Job ID of the healer.</param>
    /// <param name="primary">Primary resource count (Lily, Aetherflow, etc.).</param>
    /// <param name="secondary">Secondary resource count (Blood Lily progress, Fairy Gauge, etc.).</param>
    /// <param name="tertiary">Tertiary resource count (cards, Addersting, etc.).</param>
    void BroadcastGaugeState(uint jobId, int primary, int secondary, int tertiary);

    /// <summary>
    /// Gets the gauge state from a remote healer instance.
    /// </summary>
    /// <param name="instanceId">The instance to query.</param>
    /// <returns>The gauge state, or null if not available.</returns>
    RemoteHealerGaugeState? GetRemoteHealerGaugeState(Guid instanceId);

    /// <summary>
    /// Gets all remote healer gauge states.
    /// </summary>
    IReadOnlyList<RemoteHealerGaugeState> GetAllRemoteHealerGaugeStates();

    /// <summary>
    /// Checks if a remote healer has resources available (is "resource-rich").
    /// A healer is considered resource-rich if they have 2+ of their primary resource.
    /// </summary>
    /// <param name="minimumPrimaryResource">Minimum primary resource to consider "rich".</param>
    bool IsAnyRemoteHealerResourceRich(int minimumPrimaryResource = 2);

    #endregion

    #region Healer Role Coordination

    /// <summary>
    /// Declares this instance's healer role.
    /// </summary>
    /// <param name="jobId">Job ID of the healer.</param>
    /// <param name="role">The role to declare.</param>
    void DeclareHealerRole(uint jobId, HealerRole role);

    /// <summary>
    /// Whether this instance is the primary healer.
    /// Auto-determined based on job priority if role is Auto.
    /// </summary>
    bool IsPrimaryHealer { get; }

    /// <summary>
    /// Gets the declared role of a remote healer.
    /// </summary>
    /// <param name="instanceId">The instance to query.</param>
    RemoteHealerRole? GetRemoteHealerRole(Guid instanceId);

    /// <summary>
    /// Gets all remote healer roles.
    /// </summary>
    IReadOnlyList<RemoteHealerRole> GetAllRemoteHealerRoles();

    /// <summary>
    /// Gets the job priority for a healer job (lower = higher priority).
    /// WHM=1, AST=2, SCH=3, SGE=4.
    /// </summary>
    int GetHealerJobPriority(uint jobId);

    #endregion

    #region Ground Effect Coordination

    /// <summary>
    /// Checks if placing a ground effect at the given position would overlap with
    /// an existing remote ground effect.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <param name="actionId">The action ID to place.</param>
    /// <param name="overlapThreshold">How much overlap is acceptable (0-1, where 1 = complete overlap).</param>
    /// <returns>True if placing would overlap significantly with a remote effect.</returns>
    bool WouldOverlapWithRemoteGroundEffect(Vector3 position, uint actionId, float overlapThreshold = 0.5f);

    /// <summary>
    /// Notifies that a ground effect was placed.
    /// Broadcasts to other instances.
    /// </summary>
    /// <param name="actionId">The action ID placed.</param>
    /// <param name="position">The position where it was placed.</param>
    void OnGroundEffectPlaced(uint actionId, Vector3 position);

    /// <summary>
    /// Gets all active remote ground effects.
    /// </summary>
    IReadOnlyList<RemoteGroundEffect> GetActiveRemoteGroundEffects();

    /// <summary>
    /// Checks if any remote ground effect is active near the given position.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <param name="radius">The radius to check within.</param>
    bool IsRemoteGroundEffectActiveNear(Vector3 position, float radius = 8f);

    #endregion

    #region Resurrection Coordination

    /// <summary>
    /// Checks if a raise target is currently reserved by another Olympus instance.
    /// </summary>
    /// <param name="entityId">The entity ID of the dead party member.</param>
    /// <returns>True if another instance is already raising this target.</returns>
    bool IsRaiseTargetReservedByOther(uint entityId);

    /// <summary>
    /// Reserves a raise target and broadcasts the intent to other instances.
    /// </summary>
    /// <param name="entityId">The target entity ID (dead party member).</param>
    /// <param name="actionId">The raise action ID.</param>
    /// <param name="castTimeMs">Cast time in milliseconds (0 for Swiftcast).</param>
    /// <param name="usingSwiftcast">Whether using Swiftcast.</param>
    /// <returns>True if reservation succeeded.</returns>
    bool ReserveRaiseTarget(uint entityId, uint actionId, int castTimeMs, bool usingSwiftcast);

    /// <summary>
    /// Clears a raise reservation after the raise completes or is interrupted.
    /// </summary>
    /// <param name="entityId">The target entity ID.</param>
    void ClearRaiseReservation(uint entityId);

    /// <summary>
    /// Gets all current remote raise reservations.
    /// Key is target entity ID, value is the reservation info.
    /// </summary>
    IReadOnlyDictionary<uint, RaiseReservation> GetRemoteRaiseReservations();

    #endregion

    #region Cleanse Coordination

    /// <summary>
    /// Checks if a cleanse target is currently reserved by another Olympus instance.
    /// </summary>
    /// <param name="entityId">The entity ID of the party member with the debuff.</param>
    /// <returns>True if another instance is already cleansing this target.</returns>
    bool IsCleanseTargetReservedByOther(uint entityId);

    /// <summary>
    /// Reserves a cleanse target and broadcasts the intent to other instances.
    /// </summary>
    /// <param name="entityId">The target entity ID.</param>
    /// <param name="statusId">The status ID of the debuff being cleansed.</param>
    /// <param name="actionId">The cleanse action ID (Esuna).</param>
    /// <param name="debuffPriority">The priority of the debuff being cleansed.</param>
    /// <returns>True if reservation succeeded.</returns>
    bool ReserveCleanseTarget(uint entityId, uint statusId, uint actionId, int debuffPriority);

    /// <summary>
    /// Clears a cleanse reservation after the cleanse completes or is interrupted.
    /// </summary>
    /// <param name="entityId">The target entity ID.</param>
    void ClearCleanseReservation(uint entityId);

    /// <summary>
    /// Gets all current remote cleanse reservations.
    /// Key is target entity ID, value is the reservation info.
    /// </summary>
    IReadOnlyDictionary<uint, CleanseReservation> GetRemoteCleanseReservations();

    #endregion

    #region Interrupt Coordination

    /// <summary>
    /// Checks if an interrupt target is currently reserved by another Olympus instance.
    /// </summary>
    /// <param name="entityId">The entity ID of the enemy being interrupted.</param>
    /// <returns>True if another instance is already interrupting this enemy.</returns>
    bool IsInterruptTargetReservedByOther(uint entityId);

    /// <summary>
    /// Reserves an interrupt target and broadcasts the intent to other instances.
    /// </summary>
    /// <param name="entityId">The target entity ID (enemy casting).</param>
    /// <param name="actionId">The interrupt action ID (Interject, Head Graze, Low Blow).</param>
    /// <param name="castTimeMs">Remaining cast time of the enemy in milliseconds.</param>
    /// <returns>True if reservation succeeded.</returns>
    bool ReserveInterruptTarget(uint entityId, uint actionId, int castTimeMs);

    /// <summary>
    /// Clears an interrupt reservation after the interrupt completes or fails.
    /// </summary>
    /// <param name="entityId">The target entity ID.</param>
    void ClearInterruptReservation(uint entityId);

    /// <summary>
    /// Gets all current remote interrupt reservations.
    /// Key is target entity ID, value is the reservation info.
    /// </summary>
    IReadOnlyDictionary<uint, InterruptReservation> GetRemoteInterruptReservations();

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
