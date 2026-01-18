namespace Olympus.Rotation.Common;

/// <summary>
/// Ranged physical DPS-specific rotation module interface.
/// Extends base module interface with ranged DPS-specific constraints.
/// </summary>
/// <typeparam name="TContext">The ranged DPS job-specific context type.</typeparam>
public interface IRangedDpsRotationModule<TContext> : IRotationModule<TContext>
    where TContext : IRangedDpsRotationContext
{
}
