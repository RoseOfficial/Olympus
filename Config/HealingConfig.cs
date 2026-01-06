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

    /// <summary>
    /// Enable aggressive lily flush when approaching Misery.
    /// When enabled and at 2 Blood Lilies, aggressively use lily heals to build the third
    /// Blood Lily for Afflatus Misery (1240p AoE). Helps avoid wasting Blood Lilies
    /// when combat ends.
    /// </summary>
    public bool EnableAggressiveLilyFlush { get; set; } = true;

    // HoT
    public bool EnableRegen { get; set; } = true;

    // oGCD heals
    public bool EnableTetragrammaton { get; set; } = true;
    public bool EnableBenediction { get; set; } = true;
    public bool EnableAssize { get; set; } = true;
    public bool EnableAsylum { get; set; } = true;

    // Healing Triage
    /// <summary>
    /// Use damage intake triage to prioritize healing targets.
    /// When enabled, considers damage intake rate along with current HP to determine healing priority.
    /// Weights: damageRate (40%) + tankBonus (30%) + missingHp (30%).
    /// Default true enables smarter healing decisions based on who is taking the most damage.
    /// </summary>
    public bool UseDamageIntakeTriage { get; set; } = true;

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

    /// <summary>
    /// HP percentage threshold for using emergency oGCD heals (Tetragrammaton).
    /// When any party member is below this threshold, prioritize oGCD heals.
    /// Default 0.50 means use emergency oGCDs when below 50% HP.
    /// </summary>
    public float OgcdEmergencyThreshold { get; set; } = 0.50f;

    /// <summary>
    /// HP percentage threshold for interrupting DPS with emergency GCD heals.
    /// When any party member is below this threshold, stop DPS and heal immediately.
    /// Default 0.40 means interrupt DPS for emergency healing when below 40% HP.
    /// </summary>
    public float GcdEmergencyThreshold { get; set; } = 0.40f;
}
