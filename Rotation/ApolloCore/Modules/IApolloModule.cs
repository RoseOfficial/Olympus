using Olympus.Rotation.ApolloCore.Context;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Interface for Apollo rotation modules.
/// Each module handles a specific aspect of the WHM rotation (healing, damage, defensive, etc.).
/// </summary>
public interface IApolloModule
{
    /// <summary>
    /// Priority order for this module (lower = higher priority).
    /// Used to determine execution order when multiple modules could act.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Display name for this module (used in debug output).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Attempts to execute an action for this module.
    /// </summary>
    /// <param name="context">The shared Apollo context containing player state and services.</param>
    /// <param name="isMoving">Whether the player is currently moving.</param>
    /// <returns>True if an action was executed, false otherwise.</returns>
    bool TryExecute(ApolloContext context, bool isMoving);

    /// <summary>
    /// Updates debug state for this module.
    /// Called every frame to keep debug information current.
    /// </summary>
    /// <param name="context">The shared Apollo context.</param>
    void UpdateDebugState(ApolloContext context);
}
