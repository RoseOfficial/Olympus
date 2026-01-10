using System;

namespace Olympus.Config;

/// <summary>
/// Astrologian-specific configuration options.
/// Controls healing, cards, Earthly Star, and damage behavior.
/// </summary>
public sealed class AstrologianConfig
{
    #region Healing Toggles

    /// <summary>
    /// Whether to use Benefic.
    /// </summary>
    public bool EnableBenefic { get; set; } = true;

    /// <summary>
    /// Whether to use Benefic II.
    /// </summary>
    public bool EnableBeneficII { get; set; } = true;

    /// <summary>
    /// Whether to use Aspected Benefic (regen).
    /// </summary>
    public bool EnableAspectedBenefic { get; set; } = true;

    /// <summary>
    /// Whether to use Helios for AoE healing.
    /// </summary>
    public bool EnableHelios { get; set; } = true;

    /// <summary>
    /// Whether to use Aspected Helios/Helios Conjunction for AoE healing + regen.
    /// </summary>
    public bool EnableAspectedHelios { get; set; } = true;

    /// <summary>
    /// Whether to use Essential Dignity (oGCD single-target heal).
    /// </summary>
    public bool EnableEssentialDignity { get; set; } = true;

    /// <summary>
    /// Whether to use Celestial Intersection (oGCD heal + shield).
    /// </summary>
    public bool EnableCelestialIntersection { get; set; } = true;

    /// <summary>
    /// Whether to use Celestial Opposition (oGCD AoE heal + regen).
    /// </summary>
    public bool EnableCelestialOpposition { get; set; } = true;

    /// <summary>
    /// Whether to use Exaltation (damage reduction + delayed heal).
    /// </summary>
    public bool EnableExaltation { get; set; } = true;

    /// <summary>
    /// Whether to use Horoscope (delayed AoE heal).
    /// </summary>
    public bool EnableHoroscope { get; set; } = true;

    /// <summary>
    /// Whether to use Macrocosmos (damage absorption + heal).
    /// </summary>
    public bool EnableMacrocosmos { get; set; } = true;

    #endregion

    #region Healing Thresholds

    /// <summary>
    /// HP threshold to trigger Benefic (basic heal).
    /// </summary>
    private float _beneficThreshold = 0.50f;
    public float BeneficThreshold
    {
        get => _beneficThreshold;
        set => _beneficThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to trigger Benefic II.
    /// </summary>
    private float _beneficIIThreshold = 0.60f;
    public float BeneficIIThreshold
    {
        get => _beneficIIThreshold;
        set => _beneficIIThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to apply Aspected Benefic.
    /// </summary>
    private float _aspectedBeneficThreshold = 0.75f;
    public float AspectedBeneficThreshold
    {
        get => _aspectedBeneficThreshold;
        set => _aspectedBeneficThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to trigger Essential Dignity.
    /// Note: Essential Dignity scales with missing HP (400-1100 potency).
    /// </summary>
    private float _essentialDignityThreshold = 0.40f;
    public float EssentialDignityThreshold
    {
        get => _essentialDignityThreshold;
        set => _essentialDignityThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to trigger Celestial Intersection.
    /// </summary>
    private float _celestialIntersectionThreshold = 0.70f;
    public float CelestialIntersectionThreshold
    {
        get => _celestialIntersectionThreshold;
        set => _celestialIntersectionThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// HP threshold to apply Exaltation.
    /// </summary>
    private float _exaltationThreshold = 0.75f;
    public float ExaltationThreshold
    {
        get => _exaltationThreshold;
        set => _exaltationThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Minimum party members below AoE threshold to trigger Helios/Celestial Opposition.
    /// </summary>
    private int _aoeHealMinTargets = 3;
    public int AoEHealMinTargets
    {
        get => _aoeHealMinTargets;
        set => _aoeHealMinTargets = Math.Clamp(value, 1, 8);
    }

    /// <summary>
    /// HP threshold for AoE healing (Helios, Aspected Helios).
    /// </summary>
    private float _aoeHealThreshold = 0.70f;
    public float AoEHealThreshold
    {
        get => _aoeHealThreshold;
        set => _aoeHealThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Earthly Star Settings

    /// <summary>
    /// Whether to use Earthly Star automatically.
    /// </summary>
    public bool EnableEarthlyStar { get; set; } = true;

    /// <summary>
    /// Earthly Star placement strategy.
    /// </summary>
    public EarthlyStarPlacementStrategy StarPlacement { get; set; } = EarthlyStarPlacementStrategy.OnMainTank;

    /// <summary>
    /// HP threshold for party average to detonate Earthly Star.
    /// </summary>
    private float _earthlyStarDetonateThreshold = 0.65f;
    public float EarthlyStarDetonateThreshold
    {
        get => _earthlyStarDetonateThreshold;
        set => _earthlyStarDetonateThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Minimum party members in range to detonate Earthly Star.
    /// </summary>
    private int _earthlyStarMinTargets = 3;
    public int EarthlyStarMinTargets
    {
        get => _earthlyStarMinTargets;
        set => _earthlyStarMinTargets = Math.Clamp(value, 1, 8);
    }

    /// <summary>
    /// Whether to wait for Giant Dominance (mature star) before detonating.
    /// If false, will detonate immature star if healing is urgent.
    /// </summary>
    public bool WaitForGiantDominance { get; set; } = true;

    /// <summary>
    /// Emergency HP threshold to detonate immature star.
    /// Only used if WaitForGiantDominance is true.
    /// </summary>
    private float _earthlyStarEmergencyThreshold = 0.40f;
    public float EarthlyStarEmergencyThreshold
    {
        get => _earthlyStarEmergencyThreshold;
        set => _earthlyStarEmergencyThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Horoscope Settings

    /// <summary>
    /// Whether to auto-activate Horoscope preparation.
    /// </summary>
    public bool AutoCastHoroscope { get; set; } = true;

    /// <summary>
    /// HP threshold to detonate Horoscope.
    /// </summary>
    private float _horoscopeThreshold = 0.70f;
    public float HoroscopeThreshold
    {
        get => _horoscopeThreshold;
        set => _horoscopeThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Minimum party members injured to detonate Horoscope.
    /// </summary>
    private int _horoscopeMinTargets = 3;
    public int HoroscopeMinTargets
    {
        get => _horoscopeMinTargets;
        set => _horoscopeMinTargets = Math.Clamp(value, 1, 8);
    }

    #endregion

    #region Macrocosmos Settings

    /// <summary>
    /// Whether to auto-use Macrocosmos.
    /// </summary>
    public bool AutoUseMacrocosmos { get; set; } = true;

    /// <summary>
    /// Average party HP threshold to use Macrocosmos.
    /// </summary>
    private float _macrocosmosThreshold = 0.80f;
    public float MacrocosmosThreshold
    {
        get => _macrocosmosThreshold;
        set => _macrocosmosThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Minimum party members to use Macrocosmos.
    /// </summary>
    private int _macrocosmosMinTargets = 4;
    public int MacrocosmosMinTargets
    {
        get => _macrocosmosMinTargets;
        set => _macrocosmosMinTargets = Math.Clamp(value, 1, 8);
    }

    #endregion

    #region Neutral Sect Settings

    /// <summary>
    /// Whether to use Neutral Sect automatically.
    /// </summary>
    public bool EnableNeutralSect { get; set; } = true;

    /// <summary>
    /// Neutral Sect usage strategy.
    /// </summary>
    public NeutralSectUsageStrategy NeutralSectStrategy { get; set; } = NeutralSectUsageStrategy.SaveForDamage;

    /// <summary>
    /// Average party HP threshold to trigger Neutral Sect.
    /// </summary>
    private float _neutralSectThreshold = 0.65f;
    public float NeutralSectThreshold
    {
        get => _neutralSectThreshold;
        set => _neutralSectThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Whether to use Sun Sign (level 100 follow-up).
    /// </summary>
    public bool EnableSunSign { get; set; } = true;

    #endregion

    #region Card Settings

    /// <summary>
    /// Whether to use cards automatically.
    /// </summary>
    public bool EnableCards { get; set; } = true;

    /// <summary>
    /// Card play strategy.
    /// </summary>
    public CardPlayStrategy CardStrategy { get; set; } = CardPlayStrategy.DpsFocused;

    /// <summary>
    /// Whether to use Minor Arcana.
    /// </summary>
    public bool EnableMinorArcana { get; set; } = true;

    /// <summary>
    /// Minor Arcana usage strategy.
    /// </summary>
    public MinorArcanaUsageStrategy MinorArcanaStrategy { get; set; } = MinorArcanaUsageStrategy.EmergencyOnly;

    /// <summary>
    /// HP threshold for Lady of Crowns (Minor Arcana heal).
    /// </summary>
    private float _ladyOfCrownsThreshold = 0.60f;
    public float LadyOfCrownsThreshold
    {
        get => _ladyOfCrownsThreshold;
        set => _ladyOfCrownsThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Whether to use Divination automatically.
    /// </summary>
    public bool EnableDivination { get; set; } = true;

    /// <summary>
    /// Whether to use Astrodyne automatically.
    /// </summary>
    public bool EnableAstrodyne { get; set; } = true;

    /// <summary>
    /// Minimum unique seals for Astrodyne (1-3).
    /// Higher values wait for better buffs.
    /// </summary>
    private int _astrodyneMinSeals = 2;
    public int AstrodyneMinSeals
    {
        get => _astrodyneMinSeals;
        set => _astrodyneMinSeals = Math.Clamp(value, 1, 3);
    }

    /// <summary>
    /// Whether to use Oracle (Divination follow-up).
    /// </summary>
    public bool EnableOracle { get; set; } = true;

    #endregion

    #region Synastry Settings

    /// <summary>
    /// Whether to use Synastry automatically.
    /// </summary>
    public bool EnableSynastry { get; set; } = true;

    /// <summary>
    /// HP threshold to use Synastry.
    /// </summary>
    private float _synastryThreshold = 0.50f;
    public float SynastryThreshold
    {
        get => _synastryThreshold;
        set => _synastryThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Lightspeed Settings

    /// <summary>
    /// Whether to use Lightspeed automatically.
    /// </summary>
    public bool EnableLightspeed { get; set; } = true;

    /// <summary>
    /// Lightspeed usage strategy.
    /// </summary>
    public LightspeedUsageStrategy LightspeedStrategy { get; set; } = LightspeedUsageStrategy.OnCooldown;

    #endregion

    #region Collective Unconscious Settings

    /// <summary>
    /// Whether to use Collective Unconscious.
    /// Note: This is a channeled ability and may interrupt other actions.
    /// </summary>
    public bool EnableCollectiveUnconscious { get; set; } = false;

    /// <summary>
    /// Average party HP threshold to trigger Collective Unconscious.
    /// </summary>
    private float _collectiveUnconsciousThreshold = 0.50f;
    public float CollectiveUnconsciousThreshold
    {
        get => _collectiveUnconsciousThreshold;
        set => _collectiveUnconsciousThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Damage Settings

    /// <summary>
    /// Whether to use single-target damage spells (Malefic series).
    /// </summary>
    public bool EnableSingleTargetDamage { get; set; } = true;

    /// <summary>
    /// Whether to use AoE damage (Gravity).
    /// </summary>
    public bool EnableAoEDamage { get; set; } = true;

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
    /// Minimum enemies for Gravity AoE damage.
    /// </summary>
    private int _aoeDamageMinTargets = 3;
    public int AoEDamageMinTargets
    {
        get => _aoeDamageMinTargets;
        set => _aoeDamageMinTargets = Math.Clamp(value, 1, 10);
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
}

/// <summary>
/// Earthly Star placement strategy.
/// </summary>
public enum EarthlyStarPlacementStrategy
{
    /// <summary>
    /// Place Earthly Star on the main tank's position.
    /// </summary>
    OnMainTank,

    /// <summary>
    /// Place Earthly Star on self.
    /// </summary>
    OnSelf,

    /// <summary>
    /// Manual control only.
    /// </summary>
    Manual
}

/// <summary>
/// Card play targeting strategy.
/// </summary>
public enum CardPlayStrategy
{
    /// <summary>
    /// Target highest-contributing DPS for maximum damage.
    /// </summary>
    DpsFocused,

    /// <summary>
    /// Balance between DPS and support.
    /// </summary>
    Balanced,

    /// <summary>
    /// Prioritize safety over damage optimization.
    /// </summary>
    SafetyFocused
}

/// <summary>
/// Minor Arcana usage strategy.
/// </summary>
public enum MinorArcanaUsageStrategy
{
    /// <summary>
    /// Only use Lady of Crowns for emergency healing.
    /// </summary>
    EmergencyOnly,

    /// <summary>
    /// Use Minor Arcana on cooldown.
    /// </summary>
    OnCooldown,

    /// <summary>
    /// Save for burst damage phases.
    /// </summary>
    SaveForBurst
}

/// <summary>
/// Neutral Sect usage strategy.
/// </summary>
public enum NeutralSectUsageStrategy
{
    /// <summary>
    /// Use Neutral Sect on cooldown.
    /// </summary>
    OnCooldown,

    /// <summary>
    /// Save Neutral Sect for high damage phases.
    /// </summary>
    SaveForDamage,

    /// <summary>
    /// Manual control only.
    /// </summary>
    Manual
}

/// <summary>
/// Lightspeed usage strategy.
/// </summary>
public enum LightspeedUsageStrategy
{
    /// <summary>
    /// Use Lightspeed on cooldown.
    /// </summary>
    OnCooldown,

    /// <summary>
    /// Save Lightspeed for movement-heavy phases.
    /// </summary>
    SaveForMovement,

    /// <summary>
    /// Save Lightspeed for raising dead party members.
    /// </summary>
    SaveForRaise
}
