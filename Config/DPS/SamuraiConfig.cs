using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Samurai (Nike) configuration options.
/// Controls Sen management, Kenki gauge, and Iaijutsu timing.
/// </summary>
public sealed class SamuraiConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use AoE combo rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Iaijutsu (Higanbana, Midare Setsugekka, etc.).
    /// </summary>
    public bool EnableIaijutsu { get; set; } = true;

    /// <summary>
    /// Whether to use Tsubame-gaeshi.
    /// </summary>
    public bool EnableTsubamegaeshi { get; set; } = true;

    /// <summary>
    /// Whether to use Ogi Namikiri.
    /// </summary>
    public bool EnableOgiNamikiri { get; set; } = true;

    /// <summary>
    /// Whether to use Shoha (Meditation spender).
    /// </summary>
    public bool EnableShoha { get; set; } = true;

    /// <summary>
    /// Whether to use Shinten (single-target Kenki spender).
    /// </summary>
    public bool EnableShinten { get; set; } = true;

    /// <summary>
    /// Whether to use Kyuten (AoE Kenki spender).
    /// </summary>
    public bool EnableKyuten { get; set; } = true;

    /// <summary>
    /// Whether to use Senei (high-damage Kenki spender).
    /// </summary>
    public bool EnableSenei { get; set; } = true;

    /// <summary>
    /// Whether to use Guren (AoE high-damage Kenki spender).
    /// </summary>
    public bool EnableGuren { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Ikishoten.
    /// </summary>
    public bool EnableIkishoten { get; set; } = true;

    /// <summary>
    /// Whether to use Meikyo Shisui.
    /// </summary>
    public bool EnableMeikyoShisui { get; set; } = true;

    /// <summary>
    /// Whether to use Zanshin (follow-up after Ikishoten Ogi gauge).
    /// </summary>
    public bool EnableZanshin { get; set; } = true;

    /// <summary>
    /// Whether to use True North for positional coverage.
    /// </summary>
    public bool EnableTrueNorth { get; set; } = true;

    #endregion

    #region Kenki Gauge Settings

    /// <summary>
    /// Minimum Kenki gauge to use Shinten/Kyuten.
    /// </summary>
    private int _kenkiMinGauge = 25;
    public int KenkiMinGauge
    {
        get => _kenkiMinGauge;
        set => _kenkiMinGauge = Math.Clamp(value, 25, 100);
    }

    /// <summary>
    /// Kenki threshold to dump gauge before overcapping.
    /// </summary>
    private int _kenkiOvercapThreshold = 80;
    public int KenkiOvercapThreshold
    {
        get => _kenkiOvercapThreshold;
        set => _kenkiOvercapThreshold = Math.Clamp(value, 25, 100);
    }

    /// <summary>
    /// Reserve Kenki for Senei/Guren during burst.
    /// </summary>
    private int _kenkiReserveForBurst = 25;
    public int KenkiReserveForBurst
    {
        get => _kenkiReserveForBurst;
        set => _kenkiReserveForBurst = Math.Clamp(value, 0, 50);
    }

    #endregion

    #region Sen Management

    /// <summary>
    /// Whether to maintain Higanbana DoT.
    /// </summary>
    public bool MaintainHiganbana { get; set; } = true;

    /// <summary>
    /// Seconds remaining on Higanbana before refreshing.
    /// </summary>
    private float _higanbanaRefreshThreshold = 5.0f;
    public float HiganbanaRefreshThreshold
    {
        get => _higanbanaRefreshThreshold;
        set => _higanbanaRefreshThreshold = Math.Clamp(value, 0f, 30f);
    }

    /// <summary>
    /// Minimum target HP percentage to apply Higanbana.
    /// Avoid applying to dying targets.
    /// </summary>
    private float _higanbanaMinTargetHp = 0.10f;
    public float HiganbanaMinTargetHp
    {
        get => _higanbanaMinTargetHp;
        set => _higanbanaMinTargetHp = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Maximum seconds to hold Ikishoten waiting for party buffs.
    /// </summary>
    private float _ikishotenHoldTime = 3.0f;
    public float IkishotenHoldTime
    {
        get => _ikishotenHoldTime;
        set => _ikishotenHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Use Meikyo Shisui during burst windows.
    /// </summary>
    public bool UseMeikyoInBurst { get; set; } = true;

    /// <summary>
    /// Pool gauge resources (Shinten, Senei) for raid buff burst windows.
    /// When enabled, holds Kenki spenders within 8s of an imminent burst.
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
