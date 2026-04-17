using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Dancer (Terpsichore) configuration options.
/// Controls dance timing, proc management, and partner selection.
/// </summary>
public sealed class DancerConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use AoE rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use proc weaponskills (Reverse Cascade, etc.).
    /// </summary>
    public bool EnableProcs { get; set; } = true;

    /// <summary>
    /// Whether to use Saber Dance.
    /// </summary>
    public bool EnableSaberDance { get; set; } = true;

    /// <summary>
    /// Whether to use Starfall Dance.
    /// </summary>
    public bool EnableStarfallDance { get; set; } = true;

    /// <summary>
    /// Whether to use Tillana.
    /// </summary>
    public bool EnableTillana { get; set; } = true;

    /// <summary>
    /// Whether to use Last Dance.
    /// </summary>
    public bool EnableLastDance { get; set; } = true;

    /// <summary>
    /// Whether to use Fan Dance abilities.
    /// </summary>
    public bool EnableFanDance { get; set; } = true;

    /// <summary>
    /// Whether to use Fan Dance IV.
    /// </summary>
    public bool EnableFanDanceIV { get; set; } = true;

    #endregion

    #region Dance Toggles

    /// <summary>
    /// Whether to use Standard Step.
    /// </summary>
    public bool EnableStandardStep { get; set; } = true;

    /// <summary>
    /// Whether to use Technical Step.
    /// </summary>
    public bool EnableTechnicalStep { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Devilment.
    /// </summary>
    public bool EnableDevilment { get; set; } = true;

    /// <summary>
    /// Whether to use Flourish.
    /// </summary>
    public bool EnableFlourish { get; set; } = true;

    /// <summary>
    /// Whether to use Finishing Move (follow-up after Last Dance Ready).
    /// </summary>
    public bool EnableFinishingMove { get; set; } = true;

    #endregion

    #region Esprit Gauge Settings

    /// <summary>
    /// Minimum Esprit gauge to use Saber Dance.
    /// </summary>
    private int _saberDanceMinGauge = 50;
    public int SaberDanceMinGauge
    {
        get => _saberDanceMinGauge;
        set => _saberDanceMinGauge = Math.Clamp(value, 50, 100);
    }

    /// <summary>
    /// Esprit threshold to dump gauge before overcapping.
    /// </summary>
    private int _espritOvercapThreshold = 85;
    public int EspritOvercapThreshold
    {
        get => _espritOvercapThreshold;
        set => _espritOvercapThreshold = Math.Clamp(value, 50, 100);
    }

    /// <summary>
    /// Save Esprit for Technical Finish windows.
    /// </summary>
    public bool SaveEspritForBurst { get; set; } = true;

    #endregion

    #region Feather Gauge Settings

    /// <summary>
    /// Minimum Feather stacks to use Fan Dance.
    /// </summary>
    private int _fanDanceMinFeathers = 1;
    public int FanDanceMinFeathers
    {
        get => _fanDanceMinFeathers;
        set => _fanDanceMinFeathers = Math.Clamp(value, 1, 4);
    }

    /// <summary>
    /// Feather threshold to dump before overcapping.
    /// </summary>
    private int _featherOvercapThreshold = 3;
    public int FeatherOvercapThreshold
    {
        get => _featherOvercapThreshold;
        set => _featherOvercapThreshold = Math.Clamp(value, 1, 4);
    }

    /// <summary>
    /// Save Feathers for burst windows.
    /// </summary>
    public bool SaveFeathersForBurst { get; set; } = true;

    #endregion

    #region Dance Settings

    /// <summary>
    /// Use Standard Step on cooldown.
    /// </summary>
    public bool UseStandardStepOnCooldown { get; set; } = true;

    /// <summary>
    /// Delay Standard Step if Technical Step is coming soon.
    /// </summary>
    public bool DelayStandardForTechnical { get; set; } = true;

    /// <summary>
    /// Seconds to hold Standard Step waiting for Technical.
    /// </summary>
    private float _standardHoldForTechnical = 5.0f;
    public float StandardHoldForTechnical
    {
        get => _standardHoldForTechnical;
        set => _standardHoldForTechnical = Math.Clamp(value, 0f, 15f);
    }

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Pool Esprit gauge for raid buff burst windows.
    /// When enabled, uses Saber Dance at 50+ during burst (vs 80+ normally).
    /// </summary>
    public bool EnableBurstPooling { get; set; } = true;

    /// <summary>
    /// Maximum seconds to hold Technical Step waiting for party buffs.
    /// </summary>
    private float _technicalHoldTime = 3.0f;
    public float TechnicalHoldTime
    {
        get => _technicalHoldTime;
        set => _technicalHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Use Devilment after Technical Finish.
    /// </summary>
    public bool UseDevilmentAfterTechnical { get; set; } = true;

    #endregion

    #region Partner Settings

    /// <summary>
    /// How to select dance partner.
    /// </summary>
    public PartnerSelection PartnerSelectionMode { get; set; } = PartnerSelection.HighestDps;

    /// <summary>
    /// Automatically re-partner if partner dies.
    /// </summary>
    public bool AutoRepartner { get; set; } = true;

    #endregion

    #region AoE Settings

    /// <summary>
    /// Minimum enemies for AoE rotation.
    /// </summary>
    private int _aoEMinTargets = 3;
    public int AoEMinTargets
    {
        get => _aoEMinTargets;
        set => _aoEMinTargets = Math.Clamp(value, 2, 8);
    }

    #endregion

    #region Utility Settings

    /// <summary>
    /// Whether to use Head Graze for interrupts.
    /// </summary>
    public bool EnableHeadGraze { get; set; } = true;

    #endregion
}

/// <summary>
/// Dance partner selection mode.
/// </summary>
public enum PartnerSelection
{
    /// <summary>
    /// Select highest DPS party member.
    /// </summary>
    HighestDps,

    /// <summary>
    /// Select first melee DPS found.
    /// </summary>
    MeleePriority,

    /// <summary>
    /// Select first ranged DPS found.
    /// </summary>
    RangedPriority,

    /// <summary>
    /// Never auto-select partner (manual only).
    /// </summary>
    Manual
}
