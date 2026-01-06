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

    // HP Trend Analysis
    /// <summary>
    /// Use dynamic thresholds based on damage intake rate.
    /// When party is taking heavy damage, lower the threshold for proactive defensives.
    /// Default true enables smarter defensive timing based on incoming damage.
    /// </summary>
    public bool UseDynamicDefensiveThresholds { get; set; } = true;

    /// <summary>
    /// Party DPS threshold to trigger proactive defensives.
    /// When party is taking this much damage per second or more, use defensives early.
    /// Default 2000 means 2000+ party-wide DPS triggers early defensive usage.
    /// </summary>
    public float DamageSpikeTriggerRate { get; set; } = 2000f;

    // Proactive Cooldown Settings

    /// <summary>
    /// Enable proactive cooldown application based on damage patterns.
    /// When enabled, defensive cooldowns like Divine Benison will be applied
    /// before the target's HP drops, based on sustained damage intake.
    /// </summary>
    public bool EnableProactiveCooldowns { get; set; } = true;

    /// <summary>
    /// Tank damage rate threshold for proactive Divine Benison application.
    /// When tank is taking this much DPS or more, apply Divine Benison proactively
    /// even if their HP is still high (anticipating tank busters).
    /// Default 500 means apply when tank is taking 500+ DPS sustained.
    /// </summary>
    public float ProactiveBenisonDamageRate { get; set; } = 500f;

    /// <summary>
    /// Use damage trend analysis for smarter Temperance timing.
    /// When enabled, Temperance will trigger earlier if damage trend is spiking,
    /// anticipating incoming party-wide damage.
    /// </summary>
    public bool UseTemperanceTrendAnalysis { get; set; } = true;
}
