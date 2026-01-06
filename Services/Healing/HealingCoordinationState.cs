using System.Collections.Generic;

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
    /// Checks if a target is already reserved for healing this frame.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <returns>True if the target is already reserved.</returns>
    public bool IsTargetReserved(uint entityId)
    {
        return _reservedTargets.Contains(entityId);
    }

    /// <summary>
    /// Clears all reservations. Called at the start of each frame.
    /// </summary>
    public void Clear()
    {
        _reservedTargets.Clear();
    }

    /// <summary>
    /// Gets the count of currently reserved targets.
    /// Useful for debugging.
    /// </summary>
    public int ReservedCount => _reservedTargets.Count;
}
