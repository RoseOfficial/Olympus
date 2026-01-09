namespace Olympus.Rotation.Common;

/// <summary>
/// Base interface for rotation modules.
/// Each module handles a specific aspect of a job rotation (healing, damage, defensive, etc.).
/// </summary>
/// <typeparam name="TContext">The job-specific context type.</typeparam>
public interface IRotationModule<TContext> where TContext : IRotationContext
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
    /// <param name="context">The shared context containing player state and services.</param>
    /// <param name="isMoving">Whether the player is currently moving.</param>
    /// <returns>True if an action was executed, false otherwise.</returns>
    bool TryExecute(TContext context, bool isMoving);

    /// <summary>
    /// Updates debug state for this module.
    /// Called every frame to keep debug information current.
    /// </summary>
    /// <param name="context">The shared context.</param>
    void UpdateDebugState(TContext context);
}

/// <summary>
/// Healer-specific rotation module interface.
/// </summary>
/// <typeparam name="TContext">The healer job-specific context type.</typeparam>
public interface IHealerRotationModule<TContext> : IRotationModule<TContext>
    where TContext : IHealerRotationContext
{
}
