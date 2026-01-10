namespace Olympus.Rotation.Common;

/// <summary>
/// Tank-specific rotation module interface.
/// </summary>
/// <typeparam name="TContext">The tank job-specific context type.</typeparam>
public interface ITankRotationModule<TContext> : IRotationModule<TContext>
    where TContext : ITankRotationContext
{
}
