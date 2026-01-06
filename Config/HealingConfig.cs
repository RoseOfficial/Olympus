using System;

namespace Olympus.Config;

/// <summary>
/// Preset triage strategies for healing target prioritization.
/// </summary>
public enum TriagePreset
{
    /// <summary>Balanced healing priority across all metrics.</summary>
    Balanced,
    /// <summary>Prioritize tank healing over DPS.</summary>
    TankFocus,
    /// <summary>React to whoever is taking the most damage.</summary>
    SpreadDamage,
    /// <summary>Focus on lowest HP members (raid damage scenarios).</summary>
    RaidWide,
    /// <summary>Use custom weights defined in CustomTriageWeights.</summary>
    Custom
}

/// <summary>
/// Configurable weights for healing triage prioritization.
/// All weights should sum to approximately 1.0 for best results.
/// </summary>
public sealed class TriageWeights
{
    /// <summary>
    /// Weight for damage intake rate in triage scoring.
    /// Higher values prioritize targets taking more damage per second.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _damageRate = 0.35f;
    public float DamageRate
    {
        get => _damageRate;
        set => _damageRate = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Bonus weight for tank role in triage scoring.
    /// Higher values give tanks healing priority over DPS/healers.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _tankBonus = 0.25f;
    public float TankBonus
    {
        get => _tankBonus;
        set => _tankBonus = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Weight for missing HP percentage in triage scoring.
    /// Higher values prioritize targets with lower HP percentage.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _missingHp = 0.30f;
    public float MissingHp
    {
        get => _missingHp;
        set => _missingHp = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Weight for damage acceleration in triage scoring.
    /// Higher values prioritize targets whose damage intake is increasing.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _damageAcceleration = 0.10f;
    public float DamageAcceleration
    {
        get => _damageAcceleration;
        set => _damageAcceleration = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>Creates balanced weights (default).</summary>
    public static TriageWeights Balanced => new()
        { DamageRate = 0.35f, TankBonus = 0.25f, MissingHp = 0.30f, DamageAcceleration = 0.10f };

    /// <summary>Creates tank-focused weights.</summary>
    public static TriageWeights TankFocus => new()
        { DamageRate = 0.25f, TankBonus = 0.45f, MissingHp = 0.20f, DamageAcceleration = 0.10f };

    /// <summary>Creates spread damage weights (react to highest damage intake).</summary>
    public static TriageWeights SpreadDamage => new()
        { DamageRate = 0.45f, TankBonus = 0.10f, MissingHp = 0.30f, DamageAcceleration = 0.15f };

    /// <summary>Creates raidwide weights (focus on lowest HP).</summary>
    public static TriageWeights RaidWide => new()
        { DamageRate = 0.20f, TankBonus = 0.10f, MissingHp = 0.50f, DamageAcceleration = 0.20f };

    /// <summary>
    /// Gets the preset weights for a given triage preset.
    /// </summary>
    public static TriageWeights FromPreset(TriagePreset preset) => preset switch
    {
        TriagePreset.Balanced => Balanced,
        TriagePreset.TankFocus => TankFocus,
        TriagePreset.SpreadDamage => SpreadDamage,
        TriagePreset.RaidWide => RaidWide,
        _ => Balanced
    };
}

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

    /// <summary>
    /// Enable Lily cap prevention.
    /// When enabled and Lilies are at 3/3 (capped), forces Lily heals on anyone with
    /// any damage to prevent wasting Lily regeneration (1 Lily every 20 seconds).
    /// </summary>
    public bool EnableLilyCapPrevention { get; set; } = true;

    // HoT
    public bool EnableRegen { get; set; } = true;

    // Dynamic Regen Threshold Settings

    /// <summary>
    /// Enable dynamic Regen threshold based on damage rate.
    /// When enabled, Regen is applied at higher HP thresholds during high-damage phases
    /// to ensure the HoT is ticking before damage lands.
    /// Default true enables proactive Regen application.
    /// </summary>
    public bool EnableDynamicRegenThreshold { get; set; } = true;

    /// <summary>
    /// HP threshold for applying Regen during high-damage phases.
    /// When target is taking significant damage, apply Regen at this threshold
    /// instead of the default 90%. Default 0.95 (95%) allows proactive Regen.
    /// Valid range: 0.85 to 1.0.
    /// </summary>
    private float _regenHighDamageThreshold = 0.95f;
    public float RegenHighDamageThreshold
    {
        get => _regenHighDamageThreshold;
        set => _regenHighDamageThreshold = Math.Clamp(value, 0.85f, 1f);
    }

    /// <summary>
    /// Damage rate (DPS) threshold to trigger high-damage Regen threshold.
    /// When target is taking this much DPS or more, use RegenHighDamageThreshold
    /// instead of the default threshold.
    /// Default 300 means apply Regen more proactively when target is taking 300+ DPS.
    /// Valid range: 0 to 2000.
    /// </summary>
    private float _regenHighDamageDpsThreshold = 300f;
    public float RegenHighDamageDpsThreshold
    {
        get => _regenHighDamageDpsThreshold;
        set => _regenHighDamageDpsThreshold = Math.Clamp(value, 0f, 2000f);
    }

    // oGCD heals
    public bool EnableTetragrammaton { get; set; } = true;
    public bool EnableBenediction { get; set; } = true;
    public bool EnableAssize { get; set; } = true;
    public bool EnableAsylum { get; set; } = true;

    // Assize Healing Mode
    /// <summary>
    /// Enable Assize as a healing oGCD in addition to DPS usage.
    /// When enabled, Assize will be prioritized during weave windows
    /// when party healing needs are high, rather than holding for DPS.
    /// Default true enables dual-purpose Assize usage.
    /// </summary>
    public bool EnableAssizeHealing { get; set; } = true;

    /// <summary>
    /// Minimum number of injured party members to trigger Assize as healing.
    /// Default 3 means use Assize for healing when 3+ party members are injured.
    /// Valid range: 1 to 8.
    /// </summary>
    private int _assizeHealingMinTargets = 3;
    public int AssizeHealingMinTargets
    {
        get => _assizeHealingMinTargets;
        set => _assizeHealingMinTargets = Math.Clamp(value, 1, 8);
    }

    /// <summary>
    /// Average party HP threshold to trigger Assize healing mode.
    /// When party average HP is below this, Assize healing is prioritized.
    /// Default 0.85 means prioritize healing when avg HP below 85%.
    /// Valid range: 0.5 to 0.95.
    /// </summary>
    private float _assizeHealingHpThreshold = 0.85f;
    public float AssizeHealingHpThreshold
    {
        get => _assizeHealingHpThreshold;
        set => _assizeHealingHpThreshold = Math.Clamp(value, 0.5f, 0.95f);
    }

    // Healing Triage
    /// <summary>
    /// Use damage intake triage to prioritize healing targets.
    /// When enabled, considers damage intake rate along with current HP to determine healing priority.
    /// Weights are configurable via TriagePreset and CustomTriageWeights.
    /// Default true enables smarter healing decisions based on who is taking the most damage.
    /// </summary>
    public bool UseDamageIntakeTriage { get; set; } = true;

    /// <summary>
    /// Active triage preset. Set to Custom to use custom weights.
    /// Default Balanced uses: damageRate 35%, tankBonus 25%, missingHp 30%, acceleration 10%.
    /// </summary>
    public TriagePreset TriagePreset { get; set; } = TriagePreset.Balanced;

    /// <summary>
    /// Custom weights for triage scoring (used when TriagePreset is Custom).
    /// </summary>
    public TriageWeights CustomTriageWeights { get; set; } = new();

    /// <summary>
    /// Gets the effective triage weights based on the current preset.
    /// </summary>
    public TriageWeights GetEffectiveTriageWeights() => TriagePreset == TriagePreset.Custom
        ? CustomTriageWeights
        : TriageWeights.FromPreset(TriagePreset);

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

    // Preemptive Healing Settings

    /// <summary>
    /// Enable preemptive healing based on damage spike detection.
    /// When enabled, healing can begin BEFORE a spike lands if the system
    /// detects an imminent damage spike and predicts the target will drop
    /// below a safe threshold.
    /// Default true enables proactive healing during tank busters and raidwides.
    /// </summary>
    public bool EnablePreemptiveHealing { get; set; } = true;

    /// <summary>
    /// Projected HP threshold for preemptive healing.
    /// When a damage spike is detected and a target's projected HP would drop
    /// below this threshold, preemptive healing will trigger.
    /// Default 0.35 means heal preemptively if target will drop below 35% HP.
    /// Valid range: 0.1 to 0.8.
    /// </summary>
    private float _preemptiveHealingThreshold = 0.35f;
    public float PreemptiveHealingThreshold
    {
        get => _preemptiveHealingThreshold;
        set => _preemptiveHealingThreshold = Math.Clamp(value, 0.1f, 0.8f);
    }

    // Spike Prediction Settings

    /// <summary>
    /// Confidence threshold for spike pattern detection.
    /// Only predict spikes when pattern confidence exceeds this value.
    /// Default 0.6 means at least 60% confidence in the detected pattern.
    /// Valid range: 0.3 to 0.95.
    /// </summary>
    private float _spikePatternConfidenceThreshold = 0.6f;
    public float SpikePatternConfidenceThreshold
    {
        get => _spikePatternConfidenceThreshold;
        set => _spikePatternConfidenceThreshold = Math.Clamp(value, 0.3f, 0.95f);
    }

    /// <summary>
    /// How far ahead to look for spike predictions (seconds).
    /// Predictions beyond this window are ignored.
    /// Default 2.0 seconds allows preemptive healing just before a spike.
    /// Valid range: 0.5 to 5.0.
    /// </summary>
    private float _spikePredictionLookahead = 2.0f;
    public float SpikePredictionLookahead
    {
        get => _spikePredictionLookahead;
        set => _spikePredictionLookahead = Math.Clamp(value, 0.5f, 5.0f);
    }

    /// <summary>
    /// Use the planned healing spell's cast time for preemptive lookahead.
    /// When enabled, the damage projection uses the spell's cast time instead of a fixed window.
    /// This allows faster spells (Lily heals = instant) to be more reactive while
    /// slower spells (Cure II = 1.5s) project further ahead.
    /// Default true enables smarter preemptive healing timing.
    /// </summary>
    public bool UseSpellCastTimeForLookahead { get; set; } = true;

    /// <summary>
    /// Minimum lookahead time for preemptive healing (seconds).
    /// When using spell cast time for lookahead, this is the minimum projection window.
    /// Prevents instant-cast spells from being too reactive.
    /// Default 0.5 seconds ensures some lookahead even for instant-cast heals.
    /// Valid range: 0.0 to 2.0.
    /// </summary>
    private float _minPreemptiveLookahead = 0.5f;
    public float MinPreemptiveLookahead
    {
        get => _minPreemptiveLookahead;
        set => _minPreemptiveLookahead = Math.Clamp(value, 0f, 2f);
    }

    // Scored Heal Selection Settings

    /// <summary>
    /// Enable scored heal selection instead of tier-based.
    /// When enabled, all valid heals are scored using multiple factors and the highest score wins.
    /// This provides more nuanced heal selection that can adapt to complex situations.
    /// Default false uses the simpler tier-based selection.
    /// </summary>
    public bool EnableScoredHealSelection { get; set; } = false;

    /// <summary>
    /// Weights for the heal scoring system.
    /// Only used when EnableScoredHealSelection is true.
    /// </summary>
    public HealingScoreWeights ScoreWeights { get; set; } = new();

    // Survivability Trending Settings

    /// <summary>
    /// Enable survivability trending in heal selection.
    /// When enabled, targets with falling HP are prioritized higher than targets
    /// at the same HP percentage but stable/rising HP.
    /// Default true enables smarter healing triage.
    /// </summary>
    public bool EnableSurvivabilityTrending { get; set; } = true;

    /// <summary>
    /// Urgency bonus multiplier for targets with falling HP.
    /// Applied when HP trend is Falling or Critical.
    /// Default 0.2 adds 20% priority bonus for falling targets.
    /// Valid range: 0.0 to 0.5.
    /// </summary>
    private float _fallingTargetUrgencyBonus = 0.2f;
    public float FallingTargetUrgencyBonus
    {
        get => _fallingTargetUrgencyBonus;
        set => _fallingTargetUrgencyBonus = Math.Clamp(value, 0f, 0.5f);
    }

    /// <summary>
    /// Additional urgency bonus for targets with low time-to-death.
    /// Applied when estimated TTD is below LowTtdThresholdSeconds.
    /// Default 0.4 adds 40% priority bonus for critically endangered targets.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _lowTtdUrgencyBonus = 0.4f;
    public float LowTtdUrgencyBonus
    {
        get => _lowTtdUrgencyBonus;
        set => _lowTtdUrgencyBonus = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Time-to-death threshold (in seconds) to trigger low TTD urgency bonus.
    /// When a target's estimated TTD is below this, apply LowTtdUrgencyBonus.
    /// Default 3.0 seconds triggers urgency for targets dying soon.
    /// Valid range: 1.0 to 10.0.
    /// </summary>
    private float _lowTtdThresholdSeconds = 3f;
    public float LowTtdThresholdSeconds
    {
        get => _lowTtdThresholdSeconds;
        set => _lowTtdThresholdSeconds = Math.Clamp(value, 1f, 10f);
    }

    // HP Prediction Crit Variance Settings

    /// <summary>
    /// Enable pessimistic HP prediction variance to account for heal crits.
    /// When enabled, predicted HP is reduced slightly to avoid overpredicting
    /// based on crit heals landing higher than expected.
    /// Default true enables more conservative HP predictions.
    /// </summary>
    public bool EnableCritVarianceReduction { get; set; } = true;

    /// <summary>
    /// Percentage reduction applied to heal predictions to account for crit variance.
    /// Higher values make predictions more conservative (assume heals land lower).
    /// Default 0.08 (8%) accounts for typical crit variance in heal amounts.
    /// Valid range: 0.0 to 0.25.
    /// </summary>
    private float _critVarianceReduction = 0.08f;
    public float CritVarianceReduction
    {
        get => _critVarianceReduction;
        set => _critVarianceReduction = Math.Clamp(value, 0f, 0.25f);
    }

    // Overheal Prevention Settings

    /// <summary>
    /// Overheal tolerance percentage for single-target heals.
    /// Heals that would overheal by more than this percentage are rejected.
    /// Default 0.02 (2%) balances efficiency with not rejecting useful heals.
    /// Valid range: 0.0 to 0.20.
    /// </summary>
    private float _singleTargetOverhealTolerance = 0.02f;
    public float SingleTargetOverhealTolerance
    {
        get => _singleTargetOverhealTolerance;
        set => _singleTargetOverhealTolerance = Math.Clamp(value, 0f, 0.20f);
    }

    /// <summary>
    /// Enable overheal checking for AoE heals.
    /// When enabled, AoE heals are evaluated against average missing HP.
    /// Default true prevents wasteful AoE healing on mostly-healthy parties.
    /// </summary>
    public bool EnableAoEOverhealCheck { get; set; } = true;

    /// <summary>
    /// Overheal tolerance percentage for AoE heals.
    /// More generous than single-target since AoE heals hit multiple targets
    /// with varying damage levels. Default 0.15 (15%).
    /// Valid range: 0.0 to 0.50.
    /// </summary>
    private float _aoEOverhealTolerance = 0.15f;
    public float AoEOverhealTolerance
    {
        get => _aoEOverhealTolerance;
        set => _aoEOverhealTolerance = Math.Clamp(value, 0f, 0.50f);
    }

    // Damage-Aware Lily Selection Settings

    /// <summary>
    /// Enable damage-rate-aware lily selection.
    /// When enabled, lily heals are preferred more aggressively when target is taking
    /// high sustained damage, leveraging their instant-cast advantage.
    /// </summary>
    public bool EnableDamageAwareLilySelection { get; set; } = true;

    /// <summary>
    /// Damage rate (DPS) threshold to aggressively prefer lily heals.
    /// When target is taking this much DPS or more, prefer lily heals regardless
    /// of the configured lily strategy to leverage instant-cast advantage.
    /// Default 400 means targets taking 400+ DPS get priority lily healing.
    /// Valid range: 0 to 2000.
    /// </summary>
    private float _aggressiveLilyDamageRate = 400f;
    public float AggressiveLilyDamageRate
    {
        get => _aggressiveLilyDamageRate;
        set => _aggressiveLilyDamageRate = Math.Clamp(value, 0f, 2000f);
    }

    /// <summary>
    /// Damage rate (DPS) threshold for moderate lily preference.
    /// When target is taking this much DPS or more, the HP threshold for
    /// lily usage is raised (more willing to use lilies at higher HP).
    /// Default 200 means targets taking 200+ DPS get lily heals at higher HP.
    /// Valid range: 0 to 2000.
    /// </summary>
    private float _moderateLilyDamageRate = 200f;
    public float ModerateLilyDamageRate
    {
        get => _moderateLilyDamageRate;
        set => _moderateLilyDamageRate = Math.Clamp(value, 0f, 2000f);
    }
}

/// <summary>
/// Configurable weights for the healing score calculation.
/// All weights should sum to approximately 1.0 for best results.
/// </summary>
public sealed class HealingScoreWeights
{
    /// <summary>
    /// Weight for potency efficiency (heal amount relative to potency).
    /// Higher values favor spells that heal more per potency point.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _potency = 0.20f;
    public float Potency
    {
        get => _potency;
        set => _potency = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Weight for MP efficiency (prefer low/no MP cost heals).
    /// Higher values favor lilies and procs over MP-costing spells.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _mpEfficiency = 0.25f;
    public float MpEfficiency
    {
        get => _mpEfficiency;
        set => _mpEfficiency = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Weight for Blood Lily generation benefit (building toward Misery).
    /// Higher values favor lily heals when building blood lilies.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _lilyBenefit = 0.15f;
    public float LilyBenefit
    {
        get => _lilyBenefit;
        set => _lilyBenefit = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Weight for Freecure proc bonus (using free Cure II).
    /// Higher values strongly prefer using Cure II when Freecure is active.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _freecureBonus = 0.15f;
    public float FreecureBonus
    {
        get => _freecureBonus;
        set => _freecureBonus = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Weight for oGCD bonus (prefer instant casts in weave windows).
    /// Higher values favor oGCDs during weave windows.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _ogcdBonus = 0.10f;
    public float OgcdBonus
    {
        get => _ogcdBonus;
        set => _ogcdBonus = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Weight for overheal penalty (reduce score for excessive overhealing).
    /// Higher values more strongly penalize heals that would overheal.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    private float _overhealPenalty = 0.15f;
    public float OverhealPenalty
    {
        get => _overhealPenalty;
        set => _overhealPenalty = Math.Clamp(value, 0f, 1f);
    }
}
