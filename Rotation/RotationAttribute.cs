using System;

namespace Olympus.Rotation;

/// <summary>
/// Marks a class as a rotation module for automatic discovery and registration.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Rotation("Apollo", JobRegistry.WhiteMage, JobRegistry.Conjurer)]
/// public class Apollo : BaseHealerRotation&lt;ApolloContext, IApolloModule&gt;
/// </code>
///
/// The RotationFactory will automatically discover classes with this attribute
/// and register them with the RotationManager.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RotationAttribute : Attribute
{
    /// <summary>
    /// Display name for this rotation (e.g., "Apollo").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Job IDs this rotation supports.
    /// </summary>
    public uint[] JobIds { get; }

    /// <summary>
    /// Role category for this rotation.
    /// </summary>
    public RotationRole Role { get; set; } = RotationRole.Unknown;

    /// <summary>
    /// Whether this rotation is enabled by default.
    /// </summary>
    public bool EnabledByDefault { get; set; } = true;

    /// <summary>
    /// Creates a new rotation attribute.
    /// </summary>
    /// <param name="name">Display name for the rotation.</param>
    /// <param name="jobIds">Job IDs this rotation supports.</param>
    public RotationAttribute(string name, params uint[] jobIds)
    {
        Name = name;
        JobIds = jobIds;
    }
}

/// <summary>
/// Role category for rotations.
/// </summary>
public enum RotationRole
{
    Unknown = 0,
    Tank = 1,
    Healer = 2,
    MeleeDps = 3,
    RangedDps = 4,
    Caster = 5
}
