using System;

namespace Olympus.Config;

/// <summary>
/// Configuration for healing spells and thresholds.
/// All numeric values are bounds-checked to prevent invalid configurations.
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
    /// Valid range: 0.1 to 0.95.
    /// </summary>
    private float _conservativeLilyHpThreshold = 0.75f;
    public float ConservativeLilyHpThreshold
    {
        get => _conservativeLilyHpThreshold;
        set => _conservativeLilyHpThreshold = Math.Clamp(value, 0.1f, 0.95f);
    }

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
    /// Weights: damageRate (35%) + tankBonus (25%) + missingHp (30%) + damageAcceleration (10%).
    /// Default true enables smarter healing decisions based on who is taking the most damage.
    /// </summary>
    public bool UseDamageIntakeTriage { get; set; } = true;

    // Thresholds
    /// <summary>
    /// Minimum number of party members below threshold to trigger AoE healing.
    /// Default 3 means use AoE heal when 3+ party members need healing.
    /// Valid range: 1 to 8.
    /// </summary>
    private int _aoEHealMinTargets = 3;
    public int AoEHealMinTargets
    {
        get => _aoEHealMinTargets;
        set => _aoEHealMinTargets = Math.Clamp(value, 1, 8);
    }

    /// <summary>
    /// HP percentage threshold for Benediction (emergency heal).
    /// Only use Benediction when target HP is below this threshold.
    /// Default 0.30 means only use when below 30% HP.
    /// Valid range: 0.1 to 0.9.
    /// </summary>
    private float _benedictionEmergencyThreshold = 0.30f;
    public float BenedictionEmergencyThreshold
    {
        get => _benedictionEmergencyThreshold;
        set => _benedictionEmergencyThreshold = Math.Clamp(value, 0.1f, 0.9f);
    }

    // Proactive Benediction Settings

    /// <summary>
    /// Enable proactive Benediction usage based on damage patterns.
    /// When enabled, Benediction can be used at higher HP thresholds
    /// if the target is taking sustained heavy damage.
    /// </summary>
    public bool EnableProactiveBenediction { get; set; } = true;

    /// <summary>
    /// HP percentage threshold for proactive Benediction.
    /// When proactive mode is enabled, Benediction can trigger at this HP
    /// if the target's damage rate exceeds ProactiveBenedictionDamageRate.
    /// Default 0.60 means use proactively when below 60% HP under heavy damage.
    /// Valid range: 0.1 to 0.9.
    /// </summary>
    private float _proactiveBenedictionHpThreshold = 0.60f;
    public float ProactiveBenedictionHpThreshold
    {
        get => _proactiveBenedictionHpThreshold;
        set => _proactiveBenedictionHpThreshold = Math.Clamp(value, 0.1f, 0.9f);
    }

    /// <summary>
    /// Target damage rate threshold for proactive Benediction.
    /// When the target is taking this much DPS or more, allow Benediction
    /// at the proactive HP threshold instead of waiting for emergency.
    /// Default 500 means allow proactive use when target is taking 500+ DPS.
    /// Valid range: 0 to 5000.
    /// </summary>
    private float _proactiveBenedictionDamageRate = 500f;
    public float ProactiveBenedictionDamageRate
    {
        get => _proactiveBenedictionDamageRate;
        set => _proactiveBenedictionDamageRate = Math.Clamp(value, 0f, 5000f);
    }

    // Dynamic Tetragrammaton Settings

    /// <summary>
    /// Enable dynamic overheal threshold for Tetragrammaton based on damage spikes.
    /// When enabled, allows more overheal during damage spikes to save lives.
    /// Default true enables spike-aware healing.
    /// </summary>
    public bool EnableDynamicTetragrammatonOverheal { get; set; } = true;

    /// <summary>
    /// Overheal multiplier for Tetragrammaton during damage spikes.
    /// During normal conditions, overheal is rejected at 1.5x missing HP.
    /// During spikes, this higher multiplier is used to allow more healing.
    /// Default 2.0 means allow up to 2x missing HP during spikes.
    /// Valid range: 1.0 to 5.0.
    /// </summary>
    private float _tetragrammatonSpikeOverhealMultiplier = 2.0f;
    public float TetragrammatonSpikeOverhealMultiplier
    {
        get => _tetragrammatonSpikeOverhealMultiplier;
        set => _tetragrammatonSpikeOverhealMultiplier = Math.Clamp(value, 1.0f, 5.0f);
    }

    /// <summary>
    /// HP percentage threshold for using emergency oGCD heals (Tetragrammaton).
    /// When any party member is below this threshold, prioritize oGCD heals.
    /// Default 0.50 means use emergency oGCDs when below 50% HP.
    /// Valid range: 0.1 to 0.9.
    /// </summary>
    private float _ogcdEmergencyThreshold = 0.50f;
    public float OgcdEmergencyThreshold
    {
        get => _ogcdEmergencyThreshold;
        set => _ogcdEmergencyThreshold = Math.Clamp(value, 0.1f, 0.9f);
    }

    // MP Conservation Settings

    /// <summary>
    /// Enable MP-aware spell selection.
    /// When enabled, healing spell selection will factor in current MP and conservation status.
    /// </summary>
    public bool EnableMpAwareSpellSelection { get; set; } = true;

    /// <summary>
    /// When in MP conservation mode, aggressively prefer Lily heals over MP-costing heals.
    /// This overrides the normal lily strategy when MP is low.
    /// </summary>
    public bool PreferLiliesInConservationMode { get; set; } = true;

    /// <summary>
    /// When in MP conservation mode and no lilies available, prefer Cure over Cure II.
    /// Cure costs less MP and may proc Freecure for a free Cure II.
    /// </summary>
    public bool PreferCureInConservationMode { get; set; } = true;

    /// <summary>
    /// HP percentage threshold for interrupting DPS with emergency GCD heals.
    /// When any party member is below this threshold, stop DPS and heal immediately.
    /// Default 0.40 means interrupt DPS for emergency healing when below 40% HP.
    /// Valid range: 0.1 to 0.9.
    /// </summary>
    private float _gcdEmergencyThreshold = 0.40f;
    public float GcdEmergencyThreshold
    {
        get => _gcdEmergencyThreshold;
        set => _gcdEmergencyThreshold = Math.Clamp(value, 0.1f, 0.9f);
    }
}
