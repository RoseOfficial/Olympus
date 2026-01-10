namespace Olympus.Rotation.Tank;

/// <summary>
/// Interface for tank rotation implementations.
/// Provides tank-specific properties beyond base IRotation.
/// </summary>
public interface ITankRotation : IRotation
{
    /// <summary>
    /// Whether the tank is currently main-tanking (has highest enmity on target).
    /// </summary>
    bool IsMainTank { get; }

    /// <summary>
    /// Current tank gauge value (job-specific resource).
    /// For Paladin: Oath Gauge (0-100)
    /// For Warrior: Beast Gauge (0-100)
    /// For Dark Knight: Blood Gauge (0-100)
    /// For Gunbreaker: Cartridges (0-3)
    /// </summary>
    int GaugeValue { get; }
}
