namespace Olympus.Rotation.Common;

/// <summary>
/// Melee DPS-specific rotation module interface.
/// Extends base module interface with melee-specific constraints.
/// </summary>
/// <typeparam name="TContext">The melee DPS job-specific context type.</typeparam>
public interface IMeleeDpsRotationModule<TContext> : IRotationModule<TContext>
    where TContext : IMeleeDpsRotationContext
{
}
