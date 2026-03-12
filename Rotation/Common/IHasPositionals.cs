namespace Olympus.Rotation.Common;

/// <summary>
/// Snapshot of the player's current positional state relative to their target.
/// Updated once per frame by the rotation.
/// </summary>
public readonly struct PositionalSnapshot
{
    /// <summary>Player is at the target's rear (135–225°).</summary>
    public bool IsAtRear { get; init; }

    /// <summary>Player is at the target's flank (45–135° or 225–315°).</summary>
    public bool IsAtFlank { get; init; }

    /// <summary>Player is in front of the target.</summary>
    public bool IsAtFront => !IsAtRear && !IsAtFlank;

    /// <summary>Target has positional immunity — no positional bonuses apply.</summary>
    public bool TargetHasImmunity { get; init; }

    /// <summary>Player has an active target within melee range.</summary>
    public bool HasTarget { get; init; }
}

/// <summary>
/// Implemented by rotations that track player positionals (melee DPS jobs).
/// Allows MainWindow and other UI to display the current positional without
/// depending on job-specific context types.
/// </summary>
public interface IHasPositionals
{
    /// <summary>
    /// Gets the current per-frame positional snapshot.
    /// </summary>
    PositionalSnapshot Positionals { get; }
}
