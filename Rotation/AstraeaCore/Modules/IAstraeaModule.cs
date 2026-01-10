using Olympus.Rotation.AstraeaCore.Context;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Interface for Astraea (Astrologian) rotation modules.
/// Each module handles a specific aspect of the AST rotation.
/// </summary>
public interface IAstraeaModule
{
    /// <summary>
    /// Priority order for this module (lower = higher priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Display name for this module (used in debug output).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Attempts to execute an action for this module.
    /// </summary>
    /// <param name="context">The shared Astraea context.</param>
    /// <param name="isMoving">Whether the player is currently moving.</param>
    /// <returns>True if an action was executed, false otherwise.</returns>
    bool TryExecute(AstraeaContext context, bool isMoving);

    /// <summary>
    /// Updates debug state for this module.
    /// </summary>
    /// <param name="context">The shared Astraea context.</param>
    void UpdateDebugState(AstraeaContext context);
}
