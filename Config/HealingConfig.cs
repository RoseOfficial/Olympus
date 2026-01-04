namespace Olympus.Config;

/// <summary>
/// Configuration for healing spells and thresholds.
/// </summary>
public sealed class HealingConfig
{
    // Single-target heals
    public bool EnableCure { get; set; } = true;
    public bool EnableCureII { get; set; } = true;

    // AoE heals
    public bool EnableMedica { get; set; } = true;
    public bool EnableMedicaII { get; set; } = true;
    public bool EnableMedicaIII { get; set; } = true;
    public bool EnableCureIII { get; set; } = true;

    // Lily heals (Afflatus)
    public bool EnableAfflatusSolace { get; set; } = true;
    public bool EnableAfflatusRapture { get; set; } = true;

    // Blood Lily optimization
    /// <summary>
    /// Blood Lily generation strategy for optimizing Afflatus Misery usage.
    /// Default Balanced prefers lily heals when Blood Lilies are below 3.
    /// </summary>
    public LilyGenerationStrategy LilyStrategy { get; set; } = LilyGenerationStrategy.Balanced;

    /// <summary>
    /// When using Conservative strategy, only prefer lily heals below this HP threshold.
    /// Default 0.75 means only use lily heals when target is below 75% HP.
    /// </summary>
    public float ConservativeLilyHpThreshold { get; set; } = 0.75f;

    // HoT
    public bool EnableRegen { get; set; } = true;

    // oGCD heals
    public bool EnableTetragrammaton { get; set; } = true;
    public bool EnableBenediction { get; set; } = true;
    public bool EnableAssize { get; set; } = true;
    public bool EnableAsylum { get; set; } = true;

    // Thresholds
    /// <summary>
    /// Minimum number of party members below threshold to trigger AoE healing.
    /// Default 3 means use AoE heal when 3+ party members need healing.
    /// </summary>
    public int AoEHealMinTargets { get; set; } = 3;

    /// <summary>
    /// HP percentage threshold for Benediction (emergency heal).
    /// Only use Benediction when target HP is below this threshold.
    /// Default 0.30 means only use when below 30% HP.
    /// </summary>
    public float BenedictionEmergencyThreshold { get; set; } = 0.30f;
}
