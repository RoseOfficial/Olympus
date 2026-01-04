namespace Olympus.Config;

/// <summary>
/// Configuration for resurrection settings.
/// </summary>
public sealed class ResurrectionConfig
{
    /// <summary>
    /// Enable automatic resurrection of dead party members.
    /// </summary>
    public bool EnableRaise { get; set; } = true;

    /// <summary>
    /// Allow hardcasting Raise when Swiftcast is on cooldown.
    /// Hardcast Raise takes 8 seconds and should only be used when safe.
    /// </summary>
    public bool AllowHardcastRaise { get; set; } = false;

    /// <summary>
    /// Minimum MP percentage required before attempting to raise (0.0 - 1.0).
    /// Default 0.25 means 25% MP minimum (2400 MP for Raise + buffer).
    /// </summary>
    public float RaiseMpThreshold { get; set; } = 0.25f;
}
