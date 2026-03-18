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
    /// Overrides automatic MT/OT detection.
    /// Null = auto-detect based on who the enemy is targeting.
    /// True = always behave as Main Tank (suppress Provoke, use MT-specific mitigation).
    /// False = always behave as Off Tank (use Provoke when appropriate, use OT mitigation).
    /// </summary>
    public bool? IsMainTankOverride { get; set; } = null;

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

    #region Paladin

    /// <summary>
    /// Use Cover to redirect damage from a co-tank to yourself.
    /// </summary>
    public bool EnableCover { get; set; } = true;

    /// <summary>
    /// Proactively apply Divine Veil party shield.
    /// </summary>
    public bool EnableDivineVeil { get; set; } = true;

    /// <summary>
    /// Use Clemency GCD heal when HP is critically low.
    /// </summary>
    public bool EnableClemency { get; set; } = false;

    /// <summary>
    /// HP percentage threshold to trigger Clemency.
    /// Range: 0.0 to 1.0 (0% to 100%).
    /// </summary>
    private float _clemencyThreshold = 0.30f;
    public float ClemencyThreshold
    {
        get => _clemencyThreshold;
        set => _clemencyThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    #region Warrior

    /// <summary>
    /// Share mitigation and healing with a party member via Nascent Flash.
    /// </summary>
    public bool EnableNascentFlash { get; set; } = true;

    /// <summary>
    /// Use Holmgang as an invulnerability cooldown.
    /// </summary>
    public bool EnableHolmgang { get; set; } = true;

    /// <summary>
    /// Spend Beast Gauge before reaching this cap to avoid overcapping.
    /// Range: 0 to 100.
    /// </summary>
    private int _beastGaugeCap = 90;
    public int BeastGaugeCap
    {
        get => _beastGaugeCap;
        set => _beastGaugeCap = Math.Clamp(value, 0, 100);
    }

    #endregion

    #region Dark Knight

    /// <summary>
    /// Use Living Dead as an invulnerability cooldown.
    /// </summary>
    public bool EnableLivingDead { get; set; } = true;

    /// <summary>
    /// Use Dark Missionary for party magic damage mitigation.
    /// </summary>
    public bool EnableDarkMissionary { get; set; } = true;

    /// <summary>
    /// Apply The Blackest Night shield to the tank when HP is high enough.
    /// </summary>
    public bool EnableTheBlackestNight { get; set; } = true;

    /// <summary>
    /// HP percentage threshold to apply The Blackest Night.
    /// Range: 0.0 to 1.0 (0% to 100%).
    /// </summary>
    private float _tbnThreshold = 0.80f;
    public float TBNThreshold
    {
        get => _tbnThreshold;
        set => _tbnThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Spend Blood Gauge before reaching this cap to avoid overcapping.
    /// Range: 0 to 100.
    /// </summary>
    private int _bloodGaugeCap = 90;
    public int BloodGaugeCap
    {
        get => _bloodGaugeCap;
        set => _bloodGaugeCap = Math.Clamp(value, 0, 100);
    }

    #endregion

    #region Gunbreaker

    /// <summary>
    /// Use Heart of Light for party magic damage mitigation.
    /// </summary>
    public bool EnableHeartOfLight { get; set; } = true;

    /// <summary>
    /// Apply Heart of Corundum shield to the tank.
    /// </summary>
    public bool EnableHeartOfCorundum { get; set; } = true;

    /// <summary>
    /// HP percentage threshold to apply Heart of Corundum.
    /// Range: 0.0 to 1.0 (0% to 100%).
    /// </summary>
    private float _heartOfCorundumThreshold = 0.80f;
    public float HeartOfCorundumThreshold
    {
        get => _heartOfCorundumThreshold;
        set => _heartOfCorundumThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion

    /// <summary>
    /// Enable coordination of personal defensive cooldowns between Olympus tanks.
    /// When enabled, tanks will stagger major mitigations (Rampart, Sentinel, etc.)
    /// to maximize mitigation uptime across a tankbuster sequence.
    /// </summary>
    public bool EnableDefensiveCoordination { get; set; } = true;

    /// <summary>
    /// Time window in seconds to delay personal defensives if another tank used one recently.
    /// Range: 1.0 to 10.0 seconds.
    /// </summary>
    private float _defensiveStaggerWindowSeconds = 3.0f;
    public float DefensiveStaggerWindowSeconds
    {
        get => _defensiveStaggerWindowSeconds;
        set => _defensiveStaggerWindowSeconds = Math.Clamp(value, 1f, 10f);
    }

    /// <summary>
    /// Enable coordination of invulnerability abilities between Olympus tanks.
    /// When enabled, tanks will avoid using invulns simultaneously to maximize coverage.
    /// </summary>
    public bool EnableInvulnerabilityCoordination { get; set; } = true;

    /// <summary>
    /// Time window in seconds to delay invulnerability if another tank used one recently.
    /// Range: 1.0 to 10.0 seconds.
    /// </summary>
    private float _invulnerabilityStaggerWindowSeconds = 5.0f;
    public float InvulnerabilityStaggerWindowSeconds
    {
        get => _invulnerabilityStaggerWindowSeconds;
        set => _invulnerabilityStaggerWindowSeconds = Math.Clamp(value, 1f, 10f);
    }
}
