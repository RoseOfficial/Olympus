using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Ninja (Hermes) configuration options.
/// Controls mudra system, Ninki gauge, and trick attack windows.
/// </summary>
public sealed class NinjaConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use AoE combo rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Ninjutsu abilities.
    /// </summary>
    public bool EnableNinjutsu { get; set; } = true;

    /// <summary>
    /// Whether to use Bhavacakra (single-target Ninki spender).
    /// </summary>
    public bool EnableBhavacakra { get; set; } = true;

    /// <summary>
    /// Whether to use Hellfrog Medium (AoE Ninki spender).
    /// </summary>
    public bool EnableHellfrogMedium { get; set; } = true;

    /// <summary>
    /// Whether to use Phantom Kamaitachi.
    /// </summary>
    public bool EnablePhantomKamaitachi { get; set; } = true;

    /// <summary>
    /// Whether to use Forked/Fleeting Raiju procs.
    /// </summary>
    public bool EnableRaiju { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Kunai's Bane (formerly Trick Attack).
    /// </summary>
    public bool EnableKunaisBane { get; set; } = true;

    /// <summary>
    /// Whether to use Kassatsu.
    /// </summary>
    public bool EnableKassatsu { get; set; } = true;

    /// <summary>
    /// Whether to use Ten Chi Jin.
    /// </summary>
    public bool EnableTenChiJin { get; set; } = true;

    /// <summary>
    /// Whether to use Bunshin.
    /// </summary>
    public bool EnableBunshin { get; set; } = true;

    /// <summary>
    /// Whether to use Meisui (converts Suiton to Ninki).
    /// </summary>
    public bool EnableMeisui { get; set; } = true;

    #endregion

    #region Ninki Gauge Settings

    /// <summary>
    /// Minimum Ninki gauge to use Bhavacakra/Hellfrog.
    /// </summary>
    private int _ninkiMinGauge = 50;
    public int NinkiMinGauge
    {
        get => _ninkiMinGauge;
        set => _ninkiMinGauge = Math.Clamp(value, 50, 100);
    }

    /// <summary>
    /// Ninki threshold to dump gauge before overcapping.
    /// </summary>
    private int _ninkiOvercapThreshold = 80;
    public int NinkiOvercapThreshold
    {
        get => _ninkiOvercapThreshold;
        set => _ninkiOvercapThreshold = Math.Clamp(value, 50, 100);
    }

    #endregion

    #region Mudra Settings

    /// <summary>
    /// Preferred Ninjutsu priority for single-target.
    /// </summary>
    public NinjutsuPriority SingleTargetNinjutsuPriority { get; set; } = NinjutsuPriority.Suiton;

    /// <summary>
    /// Whether to use Doton for AoE.
    /// </summary>
    public bool UseDotonForAoE { get; set; } = true;

    /// <summary>
    /// Minimum enemies for Doton placement.
    /// </summary>
    private int _dotonMinTargets = 3;
    public int DotonMinTargets
    {
        get => _dotonMinTargets;
        set => _dotonMinTargets = Math.Clamp(value, 2, 8);
    }

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Align Kunai's Bane with party burst windows.
    /// </summary>
    public bool AlignKunaisBaneWithParty { get; set; } = true;

    /// <summary>
    /// Maximum seconds to hold Kunai's Bane waiting for party buffs.
    /// </summary>
    private float _kunaisBaneHoldTime = 3.0f;
    public float KunaisBaneHoldTime
    {
        get => _kunaisBaneHoldTime;
        set => _kunaisBaneHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Save Ninki for burst windows.
    /// </summary>
    public bool SaveNinkiForBurst { get; set; } = true;

    /// <summary>
    /// Pool gauge resources (Bhavacakra, Hellfrog Medium) for raid buff burst windows.
    /// When enabled, holds gauge spenders within 8s of an imminent burst.
    /// </summary>
    public bool EnableBurstPooling { get; set; } = true;

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

    #region Positional Settings

    /// <summary>
    /// Whether to enforce positional requirements.
    /// </summary>
    public bool EnforcePositionals { get; set; } = false;

    /// <summary>
    /// Allow weaponskills even without True North when out of position.
    /// </summary>
    public bool AllowPositionalLoss { get; set; } = true;

    #endregion
}

/// <summary>
/// Ninjutsu priority for single-target.
/// </summary>
public enum NinjutsuPriority
{
    /// <summary>
    /// Prioritize Suiton for Kunai's Bane setup.
    /// </summary>
    Suiton,

    /// <summary>
    /// Prioritize Raiton for damage.
    /// </summary>
    Raiton,

    /// <summary>
    /// Prioritize Hyosho Ranryu for burst damage.
    /// </summary>
    HyoshoRanryu
}
