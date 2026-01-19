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
