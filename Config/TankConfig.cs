using System;

namespace Olympus.Config;

/// <summary>
/// Configuration settings for tank rotations.
/// </summary>
public sealed class TankConfig
{
    /// <summary>
    /// Enable automatic mitigation cooldown usage.
    /// </summary>
    public bool EnableMitigation { get; set; } = true;

    /// <summary>
    /// Enable damage rotation (DPS actions).
    /// </summary>
    public bool EnableDamage { get; set; } = true;

    /// <summary>
    /// Automatically enable tank stance when entering combat.
    /// </summary>
    public bool AutoTankStance { get; set; } = true;

    /// <summary>
    /// Enable automatic Provoke when losing aggro.
    /// </summary>
    public bool AutoProvoke { get; set; } = true;

    /// <summary>
    /// HP percentage threshold for using mitigation cooldowns.
    /// Lower values = more conservative (waits until lower HP).
    /// Range: 0.0 to 1.0 (0% to 100%).
    /// </summary>
    private float _mitigationThreshold = 0.70f;
    public float MitigationThreshold
    {
        get => _mitigationThreshold;
        set => _mitigationThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Use Rampart (or equivalent major cooldown) on cooldown when in combat.
    /// If false, major cooldowns are saved for tank busters.
    /// </summary>
    public bool UseRampartOnCooldown { get; set; } = false;

    /// <summary>
    /// Minimum Oath Gauge (Paladin) / Beast Gauge (Warrior) / etc. required to use short cooldowns.
    /// Range: 0 to 100.
    /// </summary>
    private int _sheltronMinGauge = 50;
    public int SheltronMinGauge
    {
        get => _sheltronMinGauge;
        set => _sheltronMinGauge = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Enable automatic Shirk to co-tank after tank swap.
    /// </summary>
    public bool AutoShirk { get; set; } = false;

    /// <summary>
    /// Number of seconds after losing aggro before using Provoke.
    /// Prevents accidental Provokes during intended tank swaps.
    /// Range: 0.0 to 5.0 seconds.
    /// </summary>
    private float _provokeDelay = 1.0f;
    public float ProvokeDelay
    {
        get => _provokeDelay;
        set => _provokeDelay = Math.Clamp(value, 0f, 5f);
    }

    /// <summary>
    /// Enable AoE damage abilities (Total Eclipse, etc.).
    /// </summary>
    public bool EnableAoEDamage { get; set; } = true;

    /// <summary>
    /// Minimum number of enemies required for AoE damage rotation.
    /// Range: 2 to 8.
    /// </summary>
    private int _aoEMinTargets = 3;
    public int AoEMinTargets
    {
        get => _aoEMinTargets;
        set => _aoEMinTargets = Math.Clamp(value, 2, 8);
    }
}
