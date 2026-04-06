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
    /// Enable Reprisal (party-wide mitigation).
    /// </summary>
    public bool EnableReprisal { get; set; } = true;

    /// <summary>
    /// Enable Interject (interrupt).
    /// </summary>
    public bool EnableInterject { get; set; } = true;

    /// <summary>
    /// Enable Low Blow (stun).
    /// </summary>
    public bool EnableLowBlow { get; set; } = true;

    /// <summary>
    /// Enable Arm's Length (knockback immunity + slow).
    /// </summary>
    public bool EnableArmsLength { get; set; } = true;

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
    /// Use Fight or Flight buff.
    /// </summary>
    public bool EnableFightOrFlight { get; set; } = true;

    /// <summary>
    /// Use Requiescat / Imperator for magic phase.
    /// </summary>
    public bool EnableRequiescat { get; set; } = true;

    /// <summary>
    /// Use Circle of Scorn (oGCD DoT).
    /// </summary>
    public bool EnableCircleOfScorn { get; set; } = true;

    /// <summary>
    /// Use Spirits Within / Expiacion (oGCD damage).
    /// </summary>
    public bool EnableSpiritsWithin { get; set; } = true;

    /// <summary>
    /// Use Intervene (gap closer).
    /// </summary>
    public bool EnableIntervene { get; set; } = true;

    /// <summary>
    /// Use Sentinel / Guardian (major mitigation).
    /// </summary>
    public bool EnableSentinel { get; set; } = true;

    /// <summary>
    /// Use Sheltron / Holy Sheltron (gauge-based mitigation).
    /// </summary>
    public bool EnableSheltron { get; set; } = true;

    /// <summary>
    /// Use Hallowed Ground (invulnerability).
    /// </summary>
    public bool EnableHallowedGround { get; set; } = true;

    /// <summary>
    /// Use Bulwark (party mitigation).
    /// </summary>
    public bool EnableBulwark { get; set; } = true;

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
    /// Use Inner Release / Berserk buff window.
    /// </summary>
    public bool EnableInnerRelease { get; set; } = true;

    /// <summary>
    /// Use Infuriate (gauge generation).
    /// </summary>
    public bool EnableInfuriate { get; set; } = true;

    /// <summary>
    /// Use Vengeance / Damnation (major mitigation).
    /// </summary>
    public bool EnableVengeance { get; set; } = true;

    /// <summary>
    /// Use Raw Intuition / Blood Whetting (short mitigation + heal).
    /// </summary>
    public bool EnableBloodWhetting { get; set; } = true;

    /// <summary>
    /// Use Thrill of Battle (HP boost + heal).
    /// </summary>
    public bool EnableThrillOfBattle { get; set; } = true;

    /// <summary>
    /// Use Shake It Off (party shield).
    /// </summary>
    public bool EnableShakeItOff { get; set; } = true;

    /// <summary>
    /// Use Upheaval / Orogeny (oGCD damage).
    /// </summary>
    public bool EnableOrogeny { get; set; } = true;

    /// <summary>
    /// Use Equilibrium (self-heal).
    /// </summary>
    public bool EnableEquilibrium { get; set; } = true;

    /// <summary>
    /// Use Onslaught (gap closer).
    /// </summary>
    public bool EnableOnslaught { get; set; } = true;

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
    /// Use Blood Weapon / Delirium (gauge generation buff).
    /// </summary>
    public bool EnableBloodWeapon { get; set; } = true;

    /// <summary>
    /// Use Delirium (burst window).
    /// </summary>
    public bool EnableDelirium { get; set; } = true;

    /// <summary>
    /// Use Shadow Wall / Shadow Vigil (major mitigation).
    /// </summary>
    public bool EnableShadowWall { get; set; } = true;

    /// <summary>
    /// Use Dark Mind (magic mitigation).
    /// </summary>
    public bool EnableDarkMind { get; set; } = true;

    /// <summary>
    /// Use Oblation (short mitigation).
    /// </summary>
    public bool EnableOblation { get; set; } = true;

    /// <summary>
    /// Use Salted Earth (ground DoT).
    /// </summary>
    public bool EnableSaltedEarth { get; set; } = true;

    /// <summary>
    /// Use Carve and Spit (oGCD damage + MP).
    /// </summary>
    public bool EnableCarveAndSpit { get; set; } = true;

    /// <summary>
    /// Use Shadowbringer (oGCD damage).
    /// </summary>
    public bool EnableShadowbringer { get; set; } = true;

    /// <summary>
    /// Use Living Shadow (pet summon).
    /// </summary>
    public bool EnableLivingShadow { get; set; } = true;

    /// <summary>
    /// Use Abyssal Drain (AoE oGCD damage + heal).
    /// </summary>
    public bool EnableAbyssalDrain { get; set; } = true;

    /// <summary>
    /// Use Shadowstride (gap closer).
    /// </summary>
    public bool EnableShadowstride { get; set; } = true;

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
    /// Use No Mercy (damage buff).
    /// </summary>
    public bool EnableNoMercy { get; set; } = true;

    /// <summary>
    /// Use Bloodfest (cartridge generation).
    /// </summary>
    public bool EnableBloodfest { get; set; } = true;

    /// <summary>
    /// Use Camouflage (mitigation).
    /// </summary>
    public bool EnableCamouflage { get; set; } = true;

    /// <summary>
    /// Use Nebula / Great Nebula (major mitigation).
    /// </summary>
    public bool EnableNebula { get; set; } = true;

    /// <summary>
    /// Use Aurora (regen).
    /// </summary>
    public bool EnableAurora { get; set; } = true;

    /// <summary>
    /// Use Bow Shock (oGCD AoE damage + DoT).
    /// </summary>
    public bool EnableBowShock { get; set; } = true;

    /// <summary>
    /// Use Trajectory (gap closer).
    /// </summary>
    public bool EnableTrajectory { get; set; } = true;

    /// <summary>
    /// Use Continuation abilities (Jugular Rip, Abdomen Tear, Eye Gouge, Hypervelocity).
    /// </summary>
    public bool EnableContinuation { get; set; } = true;

    /// <summary>
    /// Use Superbolide (invulnerability).
    /// </summary>
    public bool EnableSuperbolide { get; set; } = true;

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
