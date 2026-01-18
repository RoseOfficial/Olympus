namespace Olympus.Rotation.Common;

/// <summary>
/// Extended rotation context for ranged physical DPS jobs.
/// Simpler than melee DPS - no positional tracking, but keeps combo state.
/// </summary>
public interface IRangedDpsRotationContext : IRotationContext
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
}
