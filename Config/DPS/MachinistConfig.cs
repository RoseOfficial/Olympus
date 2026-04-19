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
    /// How aggressively to use Reassemble when a high-potency tool is queued next.
    /// </summary>
    public ReassembleStrategy ReassembleStrategy { get; set; } = ReassembleStrategy.Automatic;

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

    // Head Graze moved to RangedSharedConfig.
}

/// <summary>
/// Reassemble usage behavior. Controls how aggressively Reassemble is spent,
/// not which specific tool it pairs with — the rotation always pairs it with
/// the next queued high-potency tool (Drill / Air Anchor / Chain Saw / Excavator / Full Metal Field).
/// </summary>
public enum ReassembleStrategy
{
    /// <summary>
    /// Fire Reassemble when the next GCD is a high-potency tool. Also fires at max charges to prevent overcap.
    /// </summary>
    Automatic,

    /// <summary>
    /// Fire Reassemble whenever the next GCD is any weaponskill. Spends charges aggressively.
    /// </summary>
    Any,

    /// <summary>
    /// Fire Reassemble only when at max charges and the next GCD is a high-potency tool. Keeps one charge for manual use.
    /// </summary>
    HoldOne,

    /// <summary>
    /// Never fire Reassemble automatically. Manual usage only.
    /// </summary>
    Delay
}
