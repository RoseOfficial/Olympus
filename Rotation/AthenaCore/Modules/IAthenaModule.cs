using Olympus.Rotation.AthenaCore.Context;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Interface for Athena (Scholar) rotation modules.
/// Each module handles a specific aspect of the SCH rotation.
/// </summary>
public interface IAthenaModule
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
    /// <param name="context">The shared Athena context.</param>
    /// <param name="isMoving">Whether the player is currently moving.</param>
    /// <returns>True if an action was executed, false otherwise.</returns>
    bool TryExecute(AthenaContext context, bool isMoving);

    /// <summary>
    /// Updates debug state for this module.
    /// </summary>
    /// <param name="context">The shared Athena context.</param>
    void UpdateDebugState(AthenaContext context);
}
