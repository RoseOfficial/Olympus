using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Machinist (Prometheus) configuration options.
/// Controls Heat/Battery gauges, Wildfire alignment, and Automaton Queen.
/// </summary>
public sealed class MachinistConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use AoE combo rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Heat Blast during Hypercharge.
    /// </summary>
    public bool EnableHeatBlast { get; set; } = true;

    /// <summary>
    /// Whether to use Auto Crossbow during AoE Hypercharge.
    /// </summary>
    public bool EnableAutoCrossbow { get; set; } = true;

    /// <summary>
    /// Whether to use Drill.
    /// </summary>
    public bool EnableDrill { get; set; } = true;

    /// <summary>
    /// Whether to use Air Anchor.
    /// </summary>
    public bool EnableAirAnchor { get; set; } = true;

    /// <summary>
    /// Whether to use Chain Saw.
    /// </summary>
    public bool EnableChainSaw { get; set; } = true;

    /// <summary>
    /// Whether to use Excavator.
    /// </summary>
    public bool EnableExcavator { get; set; } = true;

    /// <summary>
    /// Whether to use Full Metal Field.
    /// </summary>
    public bool EnableFullMetalField { get; set; } = true;

    /// <summary>
    /// Whether to use Gauss Round and Ricochet.
    /// </summary>
    public bool EnableGaussRicochet { get; set; } = true;

    /// <summary>
    /// Whether to use Double Check and Checkmate (upgraded Gauss/Ricochet).
    /// </summary>
    public bool EnableCheckAbilities { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Wildfire.
    /// </summary>
    public bool EnableWildfire { get; set; } = true;

    /// <summary>
    /// Whether to use Hypercharge.
    /// </summary>
    public bool EnableHypercharge { get; set; } = true;

    /// <summary>
    /// Whether to use Barrel Stabilizer.
    /// </summary>
    public bool EnableBarrelStabilizer { get; set; } = true;

    /// <summary>
    /// Whether to use Reassemble.
    /// </summary>
    public bool EnableReassemble { get; set; } = true;

    #endregion

    #region Heat Gauge Settings

    /// <summary>
    /// Minimum Heat gauge to use Hypercharge.
    /// </summary>
    private int _heatMinGauge = 50;
    public int HeatMinGauge
    {
        get => _heatMinGauge;
        set => _heatMinGauge = Math.Clamp(value, 50, 100);
    }

    /// <summary>
    /// Heat threshold to dump gauge before overcapping.
    /// </summary>
    private int _heatOvercapThreshold = 90;
    public int HeatOvercapThreshold
    {
        get => _heatOvercapThreshold;
        set => _heatOvercapThreshold = Math.Clamp(value, 50, 100);
    }

    /// <summary>
    /// Save Heat for Wildfire windows.
    /// </summary>
    public bool SaveHeatForWildfire { get; set; } = true;

    #endregion

    #region Battery Gauge Settings

    /// <summary>
    /// Minimum Battery gauge to summon Automaton Queen.
    /// </summary>
    private int _batteryMinGauge = 50;
    public int BatteryMinGauge
    {
        get => _batteryMinGauge;
        set => _batteryMinGauge = Math.Clamp(value, 50, 100);
    }

    /// <summary>
    /// Battery threshold to summon Queen before overcapping.
    /// </summary>
    private int _batteryOvercapThreshold = 90;
    public int BatteryOvercapThreshold
    {
        get => _batteryOvercapThreshold;
        set => _batteryOvercapThreshold = Math.Clamp(value, 50, 100);
    }

    /// <summary>
    /// Save Battery for burst windows.
    /// </summary>
    public bool SaveBatteryForBurst { get; set; } = true;

    #endregion

    #region Queen Settings

    /// <summary>
    /// Whether to summon Automaton Queen.
    /// </summary>
    public bool EnableAutomatonQueen { get; set; } = true;

    /// <summary>
    /// Whether to use Queen Overdrive for burst.
    /// </summary>
    public bool EnableQueenOverdrive { get; set; } = true;

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Pool Heat gauge for raid buff burst windows.
    /// When enabled, holds Hypercharge within 8s of an imminent burst.
    /// </summary>
    public bool EnableBurstPooling { get; set; } = true;

    /// <summary>
    /// Maximum seconds to hold Wildfire waiting for party buffs.
    /// </summary>
    private float _wildfireHoldTime = 3.0f;
    public float WildfireHoldTime
    {
        get => _wildfireHoldTime;
        set => _wildfireHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Use Reassemble on Drill/Air Anchor/Chain Saw.
    /// </summary>
    public ReassemblePriority ReassemblePriority { get; set; } = ReassemblePriority.Drill;

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

    #region Interrupt Settings

    /// <summary>
    /// Whether to use Head Graze for interrupts.
    /// </summary>
    public bool EnableHeadGraze { get; set; } = true;

    #endregion
}

/// <summary>
/// Reassemble usage priority.
/// </summary>
public enum ReassemblePriority
{
    /// <summary>
    /// Prioritize Drill for guaranteed crit.
    /// </summary>
    Drill,

    /// <summary>
    /// Prioritize Air Anchor for guaranteed crit.
    /// </summary>
    AirAnchor,

    /// <summary>
    /// Prioritize Chain Saw for guaranteed crit.
    /// </summary>
    ChainSaw,

    /// <summary>
    /// Use on whichever is available first.
    /// </summary>
    FirstAvailable
}
