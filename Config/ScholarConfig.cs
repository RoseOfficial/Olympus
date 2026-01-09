using System;

namespace Olympus.Config;

/// <summary>
/// Scholar-specific configuration options.
/// Controls healing, fairy, Aetherflow, and damage behavior.
/// </summary>
public sealed class ScholarConfig
{
    #region Healing Toggles

    /// <summary>
    /// Whether to use Physick.
    /// </summary>
    public bool EnablePhysick { get; set; } = true;

    /// <summary>
    /// Whether to use Adloquium.
    /// </summary>
    public bool EnableAdloquium { get; set; } = true;

    /// <summary>
    /// Whether to use Succor for AoE healing.
    /// </summary>
    public bool EnableSuccor { get; set; } = true;

    /// <summary>
    /// Whether to use Lustrate (oGCD single-target heal).
    /// </summary>
    public bool EnableLustrate { get; set; } = true;

    /// <summary>
    /// Whether to use Excogitation proactively.
    /// </summary>
    public bool EnableExcogitation { get; set; } = true;

    /// <summary>
    /// Whether to use Indomitability (oGCD AoE heal).
    /// </summary>
    public bool EnableIndomitability { get; set; } = true;

    /// <summary>
    /// Whether to use Protraction.
    /// </summary>
    public bool EnableProtraction { get; set; } = true;

    /// <summary>
    /// Whether to use Recitation for guaranteed crit/free heals.
    /// </summary>
    public bool EnableRecitation { get; set; } = true;

    #endregion

    #region Healing Thresholds

    /// <summary>
    /// HP threshold to trigger Physick (basic heal).
    /// </summary>
    private float _physickThreshold = 0.50f;
    public float PhysickThreshold
    {
        get => _physickThreshold;
        set => _physickThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to trigger Adloquium (shield heal).
    /// </summary>
    private float _adloquiumThreshold = 0.65f;
    public float AdloquiumThreshold
    {
        get => _adloquiumThreshold;
        set => _adloquiumThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to trigger Lustrate (oGCD heal).
    /// </summary>
    private float _lustrateThreshold = 0.55f;
    public float LustrateThreshold
    {
        get => _lustrateThreshold;
        set => _lustrateThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to apply Excogitation proactively.
    /// </summary>
    private float _excogitationThreshold = 0.85f;
    public float ExcogitationThreshold
    {
        get => _excogitationThreshold;
        set => _excogitationThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Minimum party members below AoE threshold to trigger Succor/Indomitability.
    /// </summary>
    private int _aoeHealMinTargets = 3;
    public int AoEHealMinTargets
    {
        get => _aoeHealMinTargets;
        set => _aoeHealMinTargets = Math.Clamp(value, 1, 8);
    }

    /// <summary>
    /// HP threshold for AoE healing (Succor, Indomitability).
    /// </summary>
    private float _aoeHealThreshold = 0.70f;
    public float AoEHealThreshold
    {
        get => _aoeHealThreshold;
        set => _aoeHealThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Shield Settings

    /// <summary>
    /// Whether to use Emergency Tactics to convert shields to healing.
    /// </summary>
    public bool EnableEmergencyTactics { get; set; } = true;

    /// <summary>
    /// HP threshold to use Emergency Tactics.
    /// </summary>
    private float _emergencyTacticsThreshold = 0.40f;
    public float EmergencyTacticsThreshold
    {
        get => _emergencyTacticsThreshold;
        set => _emergencyTacticsThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Whether to use Deployment Tactics to spread shields.
    /// </summary>
    public bool EnableDeploymentTactics { get; set; } = true;

    /// <summary>
    /// Minimum party members that would benefit from Deployment Tactics.
    /// </summary>
    private int _deploymentMinTargets = 4;
    public int DeploymentMinTargets
    {
        get => _deploymentMinTargets;
        set => _deploymentMinTargets = Math.Clamp(value, 2, 8);
    }

    /// <summary>
    /// Whether to avoid overwriting Sage shields with Galvanize.
    /// </summary>
    public bool AvoidOverwritingSageShields { get; set; } = true;

    #endregion

    #region Aetherflow Settings

    /// <summary>
    /// Number of Aetherflow stacks to reserve for emergency healing.
    /// </summary>
    private int _aetherflowReserve = 1;
    public int AetherflowReserve
    {
        get => _aetherflowReserve;
        set => _aetherflowReserve = Math.Clamp(value, 0, 3);
    }

    /// <summary>
    /// Aetherflow usage strategy.
    /// </summary>
    public AetherflowUsageStrategy AetherflowStrategy { get; set; } = AetherflowUsageStrategy.Balanced;

    /// <summary>
    /// Whether to use Energy Drain when healing is not needed.
    /// </summary>
    public bool EnableEnergyDrain { get; set; } = true;

    /// <summary>
    /// Seconds before Aetherflow cooldown to start dumping stacks.
    /// </summary>
    private float _aetherflowDumpWindow = 5f;
    public float AetherflowDumpWindow
    {
        get => _aetherflowDumpWindow;
        set => _aetherflowDumpWindow = Math.Clamp(value, 0f, 15f);
    }

    #endregion

    #region Fairy Settings

    /// <summary>
    /// Whether to automatically summon the fairy if not present.
    /// </summary>
    public bool AutoSummonFairy { get; set; } = true;

    /// <summary>
    /// Whether to use fairy abilities automatically.
    /// </summary>
    public bool EnableFairyAbilities { get; set; } = true;

    /// <summary>
    /// HP threshold to use Whispering Dawn.
    /// </summary>
    private float _whisperingDawnThreshold = 0.80f;
    public float WhisperingDawnThreshold
    {
        get => _whisperingDawnThreshold;
        set => _whisperingDawnThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Minimum party members injured to use Whispering Dawn.
    /// </summary>
    private int _whisperingDawnMinTargets = 2;
    public int WhisperingDawnMinTargets
    {
        get => _whisperingDawnMinTargets;
        set => _whisperingDawnMinTargets = Math.Clamp(value, 1, 8);
    }

    /// <summary>
    /// HP threshold to use Fey Blessing.
    /// </summary>
    private float _feyBlessingThreshold = 0.70f;
    public float FeyBlessingThreshold
    {
        get => _feyBlessingThreshold;
        set => _feyBlessingThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to start Fey Union.
    /// </summary>
    private float _feyUnionThreshold = 0.65f;
    public float FeyUnionThreshold
    {
        get => _feyUnionThreshold;
        set => _feyUnionThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Minimum Fairy Gauge to use Fey Union.
    /// </summary>
    private int _feyUnionMinGauge = 30;
    public int FeyUnionMinGauge
    {
        get => _feyUnionMinGauge;
        set => _feyUnionMinGauge = Math.Clamp(value, 10, 100);
    }

    #endregion

    #region Seraph Settings

    /// <summary>
    /// Whether to use Consolation (Seraph AoE heal).
    /// </summary>
    public bool EnableConsolation { get; set; } = true;

    /// <summary>
    /// When to use Summon Seraph.
    /// </summary>
    public SeraphUsageStrategy SeraphStrategy { get; set; } = SeraphUsageStrategy.OnCooldown;

    /// <summary>
    /// Average party HP threshold to trigger Seraph.
    /// </summary>
    private float _seraphPartyHpThreshold = 0.70f;
    public float SeraphPartyHpThreshold
    {
        get => _seraphPartyHpThreshold;
        set => _seraphPartyHpThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// When to use Seraphism (level 100).
    /// </summary>
    public SeraphismUsageStrategy SeraphismStrategy { get; set; } = SeraphismUsageStrategy.SaveForDamage;

    #endregion

    #region Dissipation Settings

    /// <summary>
    /// Whether to use Dissipation for Aetherflow stacks.
    /// </summary>
    public bool EnableDissipation { get; set; } = false;

    /// <summary>
    /// Minimum Fairy Gauge before considering Dissipation.
    /// Low gauge means less waste from dismissing fairy.
    /// </summary>
    private int _dissipationMaxFairyGauge = 30;
    public int DissipationMaxFairyGauge
    {
        get => _dissipationMaxFairyGauge;
        set => _dissipationMaxFairyGauge = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Minimum party HP to consider Dissipation safe.
    /// </summary>
    private float _dissipationSafePartyHp = 0.80f;
    public float DissipationSafePartyHp
    {
        get => _dissipationSafePartyHp;
        set => _dissipationSafePartyHp = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Damage Settings

    /// <summary>
    /// Whether to use single-target damage spells (Ruin/Broil).
    /// </summary>
    public bool EnableSingleTargetDamage { get; set; } = true;

    /// <summary>
    /// Whether to use AoE damage (Art of War).
    /// </summary>
    public bool EnableAoEDamage { get; set; } = true;

    /// <summary>
    /// Whether to use Ruin II for instant damage while moving.
    /// </summary>
    public bool EnableRuinII { get; set; } = true;

    /// <summary>
    /// Whether to use Aetherflow to get stacks.
    /// </summary>
    public bool EnableAetherflow { get; set; } = true;

    /// <summary>
    /// Whether to maintain DoT on target.
    /// </summary>
    public bool EnableDot { get; set; } = true;

    /// <summary>
    /// Seconds remaining on DoT before refreshing.
    /// </summary>
    private float _dotRefreshThreshold = 3f;
    public float DotRefreshThreshold
    {
        get => _dotRefreshThreshold;
        set => _dotRefreshThreshold = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Minimum enemies for Art of War AoE damage.
    /// </summary>
    private int _aoeDamageMinTargets = 3;
    public int AoEDamageMinTargets
    {
        get => _aoeDamageMinTargets;
        set => _aoeDamageMinTargets = Math.Clamp(value, 1, 10);
    }

    /// <summary>
    /// Whether to use Chain Stratagem automatically.
    /// </summary>
    public bool EnableChainStratagem { get; set; } = true;

    /// <summary>
    /// Whether to use Baneful Impaction when Impact Imminent is active.
    /// </summary>
    public bool EnableBanefulImpaction { get; set; } = true;

    #endregion

    #region Recitation Settings

    /// <summary>
    /// Which ability to prioritize for Recitation.
    /// </summary>
    public RecitationPriority RecitationPriority { get; set; } = RecitationPriority.Excogitation;

    #endregion

    #region Sacred Soil Settings

    /// <summary>
    /// Whether to use Sacred Soil automatically.
    /// </summary>
    public bool EnableSacredSoil { get; set; } = true;

    /// <summary>
    /// Average party HP threshold to trigger Sacred Soil.
    /// </summary>
    private float _sacredSoilThreshold = 0.75f;
    public float SacredSoilThreshold
    {
        get => _sacredSoilThreshold;
        set => _sacredSoilThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Minimum party members in range for Sacred Soil.
    /// </summary>
    private int _sacredSoilMinTargets = 3;
    public int SacredSoilMinTargets
    {
        get => _sacredSoilMinTargets;
        set => _sacredSoilMinTargets = Math.Clamp(value, 1, 8);
    }

    #endregion

    #region Expedient Settings

    /// <summary>
    /// Whether to use Expedient for mitigation.
    /// </summary>
    public bool EnableExpedient { get; set; } = true;

    /// <summary>
    /// Average party HP threshold to trigger Expedient.
    /// </summary>
    private float _expedientThreshold = 0.60f;
    public float ExpedientThreshold
    {
        get => _expedientThreshold;
        set => _expedientThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Lucid Dreaming Settings

    /// <summary>
    /// Whether to use Lucid Dreaming automatically.
    /// </summary>
    public bool EnableLucidDreaming { get; set; } = true;

    /// <summary>
    /// MP percentage threshold to trigger Lucid Dreaming.
    /// </summary>
    private float _lucidDreamingThreshold = 0.70f;
    public float LucidDreamingThreshold
    {
        get => _lucidDreamingThreshold;
        set => _lucidDreamingThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Protraction Settings

    /// <summary>
    /// HP threshold to use Protraction.
    /// </summary>
    private float _protractionThreshold = 0.70f;
    public float ProtractionThreshold
    {
        get => _protractionThreshold;
        set => _protractionThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion
}

/// <summary>
/// Aetherflow usage strategy.
/// </summary>
public enum AetherflowUsageStrategy
{
    /// <summary>
    /// Balance between healing and Energy Drain.
    /// </summary>
    Balanced,

    /// <summary>
    /// Prioritize healing, minimal Energy Drain.
    /// </summary>
    HealingPriority,

    /// <summary>
    /// Aggressive Energy Drain when safe.
    /// </summary>
    AggressiveDps
}

/// <summary>
/// Seraph summoning strategy.
/// </summary>
public enum SeraphUsageStrategy
{
    /// <summary>
    /// Use Seraph on cooldown.
    /// </summary>
    OnCooldown,

    /// <summary>
    /// Save Seraph for high damage phases.
    /// </summary>
    SaveForDamage,

    /// <summary>
    /// Manual control only.
    /// </summary>
    Manual
}

/// <summary>
/// Seraphism usage strategy.
/// </summary>
public enum SeraphismUsageStrategy
{
    /// <summary>
    /// Use Seraphism on cooldown.
    /// </summary>
    OnCooldown,

    /// <summary>
    /// Save Seraphism for high damage phases.
    /// </summary>
    SaveForDamage,

    /// <summary>
    /// Manual control only.
    /// </summary>
    Manual
}

/// <summary>
/// Recitation target priority.
/// </summary>
public enum RecitationPriority
{
    /// <summary>
    /// Prioritize Excogitation for guaranteed crit.
    /// </summary>
    Excogitation,

    /// <summary>
    /// Prioritize Indomitability for AoE healing.
    /// </summary>
    Indomitability,

    /// <summary>
    /// Prioritize Adloquium for single-target shield.
    /// </summary>
    Adloquium,

    /// <summary>
    /// Prioritize Succor for AoE shields.
    /// </summary>
    Succor
}
