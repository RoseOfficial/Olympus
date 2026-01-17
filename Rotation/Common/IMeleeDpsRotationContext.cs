namespace Olympus.Rotation.Common;

/// <summary>
/// Extended rotation context for melee DPS jobs.
/// Adds melee-specific services and state tracking (positionals, combo state).
/// </summary>
public interface IMeleeDpsRotationContext : IRotationContext
{
    #region Combo State

    /// <summary>
    /// Current combo step (0 = no combo, 1+ = combo position).
    /// </summary>
    int ComboStep { get; }

    /// <summary>
    /// Action ID of the last GCD used for combo tracking.
    /// </summary>
    uint LastComboAction { get; }

    /// <summary>
    /// Time remaining on the current combo chain before it breaks (in seconds).
    /// Typically 30 seconds from last combo action.
    /// </summary>
    float ComboTimeRemaining { get; }

    #endregion

    #region Positional State

    /// <summary>
    /// Whether the player is currently at the target's rear (135-225 degrees).
    /// </summary>
    bool IsAtRear { get; }

    /// <summary>
    /// Whether the player is currently at the target's flank (45-135 or 225-315 degrees).
    /// </summary>
    bool IsAtFlank { get; }

    /// <summary>
    /// Whether the current target has true north/positional immunity
    /// (rings/circles that indicate no positional bonus applies).
    /// </summary>
    bool TargetHasPositionalImmunity { get; }

    /// <summary>
    /// Whether the player has True North buff active (ignores positional requirements).
    /// </summary>
    bool HasTrueNorth { get; }

    #endregion
}
