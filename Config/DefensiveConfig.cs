namespace Olympus.Config;

/// <summary>
/// Configuration for defensive cooldowns.
/// </summary>
public sealed class DefensiveConfig
{
    public bool EnableDivineBenison { get; set; } = true;
    public bool EnablePlenaryIndulgence { get; set; } = true;
    public bool EnableTemperance { get; set; } = true;
    public bool EnableAquaveil { get; set; } = true;
    public bool EnableLiturgyOfTheBell { get; set; } = true;
    public bool EnableDivineCaress { get; set; } = true;

    /// <summary>
    /// HP percentage threshold to use defensive cooldowns proactively.
    /// When party average HP falls below this, start using mitigation.
    /// Default 0.80 means use when average HP &lt; 80%.
    /// </summary>
    public float DefensiveCooldownThreshold { get; set; } = 0.80f;

    /// <summary>
    /// Use defensive cooldowns during AoE heals for synergy.
    /// E.g., Plenary Indulgence before Medica/Cure III.
    /// </summary>
    public bool UseDefensivesWithAoEHeals { get; set; } = true;
}
