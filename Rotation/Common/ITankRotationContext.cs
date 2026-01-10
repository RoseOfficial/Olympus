using Olympus.Services.Tank;

namespace Olympus.Rotation.Common;

/// <summary>
/// Extended rotation context for tank jobs.
/// Adds tank-specific services and state tracking.
/// </summary>
public interface ITankRotationContext : IRotationContext
{
    /// <summary>
    /// Service for tracking enmity and aggro state.
    /// </summary>
    IEnmityService EnmityService { get; }

    /// <summary>
    /// Service for planning tank cooldown usage.
    /// </summary>
    ITankCooldownService TankCooldownService { get; }

    /// <summary>
    /// Whether the player is currently the main tank (has highest enmity on target).
    /// </summary>
    bool IsMainTank { get; }

    /// <summary>
    /// Whether tank stance is active (Iron Will, Defiance, Grit, Royal Guard).
    /// </summary>
    bool HasTankStance { get; }

    /// <summary>
    /// Current combo step (0 = no combo, 1-3 = combo position).
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
}
