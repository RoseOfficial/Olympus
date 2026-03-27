using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Reaper (Thanatos) configuration options.
/// Controls Soul/Shroud gauges, Enshroud windows, and party coordination.
/// </summary>
public sealed class ReaperConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use AoE combo rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Soul Reaver actions (Gibbet/Gallows/Guillotine).
    /// </summary>
    public bool EnableSoulReaver { get; set; } = true;

    /// <summary>
    /// Whether to use Enshroud burst window.
    /// </summary>
    public bool EnableEnshroud { get; set; } = true;

    /// <summary>
    /// Whether to use Communio.
    /// </summary>
    public bool EnableCommunio { get; set; } = true;

    /// <summary>
    /// Whether to use Perfectio.
    /// </summary>
    public bool EnablePerfectio { get; set; } = true;

    /// <summary>
    /// Whether to use Lemure abilities during Enshroud.
    /// </summary>
    public bool EnableLemureAbilities { get; set; } = true;

    /// <summary>
    /// Whether to use Harvest Moon.
    /// </summary>
    public bool EnableHarvestMoon { get; set; } = true;

    /// <summary>
    /// Whether to use Plentiful Harvest.
    /// </summary>
    public bool EnablePlentifulHarvest { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Arcane Circle (party buff).
    /// </summary>
    public bool EnableArcaneCircle { get; set; } = true;

    /// <summary>
    /// Whether to use Gluttony.
    /// </summary>
    public bool EnableGluttony { get; set; } = true;

    #endregion

    #region Soul Gauge Settings

    /// <summary>
    /// Minimum Soul gauge to use Blood Stalk/Grim Swathe.
    /// </summary>
    private int _soulMinGauge = 50;
    public int SoulMinGauge
    {
        get => _soulMinGauge;
        set => _soulMinGauge = Math.Clamp(value, 50, 100);
    }

    /// <summary>
    /// Soul gauge threshold to dump before overcapping.
    /// </summary>
    private int _soulOvercapThreshold = 80;
    public int SoulOvercapThreshold
    {
        get => _soulOvercapThreshold;
        set => _soulOvercapThreshold = Math.Clamp(value, 50, 100);
    }

    #endregion

    #region Shroud Gauge Settings

    /// <summary>
    /// Minimum Shroud gauge to enter Enshroud.
    /// </summary>
    private int _shroudMinGauge = 50;
    public int ShroudMinGauge
    {
        get => _shroudMinGauge;
        set => _shroudMinGauge = Math.Clamp(value, 50, 100);
    }

    /// <summary>
    /// Save Shroud for burst windows.
    /// </summary>
    public bool SaveShroudForBurst { get; set; } = true;

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Maximum seconds to hold Arcane Circle waiting for party buffs.
    /// </summary>
    private float _arcaneCircleHoldTime = 3.0f;
    public float ArcaneCircleHoldTime
    {
        get => _arcaneCircleHoldTime;
        set => _arcaneCircleHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Enter Enshroud during Arcane Circle.
    /// </summary>
    public bool UseEnshroudDuringArcaneCircle { get; set; } = true;

    /// <summary>
    /// Pool gauge resources (Gluttony, Enshroud) for raid buff burst windows.
    /// When enabled, holds Soul spenders within 8s of an imminent burst.
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

    /// <summary>
    /// Alternate Gibbet/Gallows based on position.
    /// </summary>
    public bool AlternateGibbetGallows { get; set; } = true;

    #endregion

    #region Death's Design Settings

    /// <summary>
    /// Seconds remaining on Death's Design before refreshing.
    /// </summary>
    private float _deathsDesignRefreshThreshold = 10.0f;
    public float DeathsDesignRefreshThreshold
    {
        get => _deathsDesignRefreshThreshold;
        set => _deathsDesignRefreshThreshold = Math.Clamp(value, 0f, 30f);
    }

    #endregion
}
