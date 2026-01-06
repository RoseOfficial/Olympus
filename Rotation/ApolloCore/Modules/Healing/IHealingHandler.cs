using Olympus.Rotation.ApolloCore.Context;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Interface for healing sub-handlers within the HealingModule.
/// Each handler is responsible for one healing priority.
/// </summary>
public interface IHealingHandler
{
    /// <summary>
    /// Internal priority within HealingModule (lower = higher priority).
    /// </summary>
    HealingPriority Priority { get; }

    /// <summary>
    /// Display name for debugging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Attempts to execute the healing action.
    /// </summary>
    /// <param name="context">The Apollo context.</param>
    /// <param name="isMoving">Whether the player is moving.</param>
    /// <returns>True if an action was executed.</returns>
    bool TryExecute(ApolloContext context, bool isMoving);
}
