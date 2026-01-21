using System.Collections.Generic;
using Olympus.Services.Party;

namespace Olympus.Services.Healing;

/// <summary>
/// Frame-scoped coordination state for healing handlers.
/// Prevents multiple handlers from targeting the same entity in the same frame,
/// avoiding double-healing and wasted resources.
/// </summary>
/// <remarks>
/// Usage pattern:
/// 1. Clear() is called at the start of each frame's HealingModule.TryExecute()
/// 2. Each handler calls IsTargetReserved() before selecting a target
/// 3. Each handler calls TryReserveTarget() after committing to a heal
/// </remarks>
public sealed class HealingCoordinationState
{
    /// <summary>
    /// Set of entity IDs that have been reserved for healing this frame.
    /// </summary>
    private readonly HashSet<uint> _reservedTargets = new();

    /// <summary>
    /// Whether an AoE heal has been reserved this frame (by local or remote instance).
    /// </summary>
    private bool _aoEHealReservedThisFrame;

    /// <summary>
    /// Attempts to reserve a target for healing.
    /// Returns true if the reservation succeeded (target was not already reserved).
    /// Returns false if the target was already reserved by another handler.
    /// </summary>
    /// <param name="entityId">The entity ID to reserve.</param>
    /// <returns>True if successfully reserved, false if already taken.</returns>
    public bool TryReserveTarget(uint entityId)
    {
        return _reservedTargets.Add(entityId);
    }

    /// <summary>
    /// Attempts to reserve a target for healing with IPC coordination.
    /// Checks remote reservations first, then reserves locally and broadcasts intent.
    /// </summary>
    /// <param name="entityId">The entity ID to reserve.</param>
    /// <param name="partyCoord">Party coordination service (may be null if disabled).</param>
    /// <param name="healAmount">Estimated heal amount for IPC broadcast.</param>
    /// <param name="actionId">Action ID being used for IPC broadcast.</param>
    /// <param name="castTimeMs">Cast time in milliseconds (0 for instant).</param>
    /// <returns>True if successfully reserved, false if already taken locally or remotely.</returns>
    public bool TryReserveTarget(uint entityId, IPartyCoordinationService? partyCoord, int healAmount = 0, uint actionId = 0, int castTimeMs = 0)
    {
        // Check if reserved by remote Olympus instance
        if (partyCoord?.IsTargetReservedByOther(entityId) == true)
            return false;

        // Try to reserve locally
        var reserved = _reservedTargets.Add(entityId);
        if (reserved && partyCoord != null && healAmount > 0)
        {
            // Broadcast intent to other instances
            partyCoord.ReserveTarget(entityId, healAmount, actionId, castTimeMs);
        }

        return reserved;
    }

    /// <summary>
    /// Checks if a target is already reserved for healing this frame.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <returns>True if the target is already reserved.</returns>
    public bool IsTargetReserved(uint entityId)
    {
        return _reservedTargets.Contains(entityId);
    }

    /// <summary>
    /// Checks if a target is already reserved for healing, including remote reservations.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <param name="partyCoord">Party coordination service (may be null if disabled).</param>
    /// <returns>True if the target is reserved locally or remotely.</returns>
    public bool IsTargetReserved(uint entityId, IPartyCoordinationService? partyCoord)
    {
        return _reservedTargets.Contains(entityId)
            || partyCoord?.IsTargetReservedByOther(entityId) == true;
    }

    /// <summary>
    /// Clears all reservations. Called at the start of each frame.
    /// </summary>
    public void Clear()
    {
        _reservedTargets.Clear();
        _aoEHealReservedThisFrame = false;
    }

    /// <summary>
    /// Attempts to reserve an AoE heal for this frame.
    /// Checks both local frame state and remote reservations.
    /// Returns true if the reservation succeeded.
    /// </summary>
    /// <param name="partyCoord">Party coordination service (may be null if disabled).</param>
    /// <param name="actionId">Action ID of the AoE heal.</param>
    /// <param name="healPotency">Heal potency of the AoE heal.</param>
    /// <param name="castTimeMs">Cast time in milliseconds (0 for instant).</param>
    /// <returns>True if successfully reserved, false if already reserved locally or remotely.</returns>
    public bool TryReserveAoEHeal(IPartyCoordinationService? partyCoord, uint actionId, int healPotency, int castTimeMs)
    {
        // Check if already reserved this frame
        if (_aoEHealReservedThisFrame)
            return false;

        // Check if reserved by remote Olympus instance
        if (partyCoord?.IsAoEHealReservedByOther() == true)
            return false;

        // Reserve locally
        _aoEHealReservedThisFrame = true;

        // Broadcast intent to other instances
        partyCoord?.ReserveAoEHeal(actionId, healPotency, castTimeMs);

        return true;
    }

    /// <summary>
    /// Checks if an AoE heal is already reserved for this frame.
    /// </summary>
    /// <param name="partyCoord">Party coordination service (may be null if disabled).</param>
    /// <returns>True if an AoE heal is reserved locally or remotely.</returns>
    public bool IsAoEHealReserved(IPartyCoordinationService? partyCoord)
    {
        return _aoEHealReservedThisFrame || partyCoord?.IsAoEHealReservedByOther() == true;
    }

    /// <summary>
    /// Gets the count of currently reserved targets.
    /// Useful for debugging.
    /// </summary>
    public int ReservedCount => _reservedTargets.Count;
}
