using System.Collections.Generic;

namespace Olympus.Data;

/// <summary>
/// FFXIV game mechanic constants. These are fixed values that
/// define game mechanics and NPC types.
/// </summary>
public static class FFXIVConstants
{
    // Party Member Types
    /// <summary>SubKind value for Trust NPC party members.</summary>
    public const int TrustNpcSubKind = 9;

    /// <summary>StatusFlags bit for hostile/untargetable.</summary>
    public const int HostileStatusFlag = 128;

    // Tank ClassJob IDs (PLD, WAR, DRK, GNB + base classes GLA, MRD)
    /// <summary>Paladin/Gladiator ClassJob IDs.</summary>
    public const uint PaladinJobId = 19;
    public const uint GladiatorJobId = 1;

    /// <summary>Warrior/Marauder ClassJob IDs.</summary>
    public const uint WarriorJobId = 21;
    public const uint MarauderJobId = 3;

    /// <summary>Dark Knight ClassJob ID.</summary>
    public const uint DarkKnightJobId = 32;

    /// <summary>Gunbreaker ClassJob ID.</summary>
    public const uint GunbreakerJobId = 37;

    // Healing Calibration
    /// <summary>Minimum valid calibration factor for healing formula.</summary>
    public const float MinCalibrationFactor = 0.8f;

    /// <summary>Maximum valid calibration factor for healing formula.</summary>
    public const float MaxCalibrationFactor = 1.5f;

    // Invalid Target ID
    /// <summary>Invalid target object ID used by the game.</summary>
    public const uint InvalidTargetId = 0xE0000000;

    // Thresholds
    /// <summary>HP percentage below which to consider applying Regen to tanks.</summary>
    public const float RegenHpThreshold = 0.90f;

    /// <summary>HP percentage below which to consider applying Regen to non-tanks.</summary>
    public const float RegenNonTankHpThreshold = 0.80f;

    /// <summary>Time remaining on DoT before refreshing (seconds).</summary>
    public const float DotRefreshThreshold = 3f;

    /// <summary>Time remaining on Regen before refreshing (seconds).</summary>
    public const float RegenRefreshThreshold = 3f;

    /// <summary>HP percentage for "injured" party member threshold.</summary>
    public const float InjuredHpThreshold = 0.95f;

    /// <summary>HP percentage for "critical" party member threshold (needs emergency healing).</summary>
    public const float CriticalHpThreshold = 0.40f;

    // Action Buffer Timings (small adjustments for weave windows)
    /// <summary>Small timing buffer for weave window calculations.</summary>
    public const float WeaveWindowBuffer = 0.1f;

    // Targeting Ranges
    /// <summary>
    /// Base melee targeting range: action range (3y) + player hitbox (~0.5y).
    /// Enemy hitbox radius is added dynamically at call sites so large bosses are handled correctly.
    /// </summary>
    public const float MeleeTargetingRange = 3.5f;

    /// <summary>
    /// Ranged physical DPS targeting range for center-to-center distance calculations.
    /// Most ranged actions have 25y range, so we use that as the standard.
    /// </summary>
    public const float RangedTargetingRange = 25f;

    /// <summary>
    /// Caster DPS targeting range for center-to-center distance calculations.
    /// Same as ranged physical (25y) for most caster spells.
    /// </summary>
    public const float CasterTargetingRange = 25f;

    // Cure III clustering
    /// <summary>Radius for detecting Cure III cluster targets.</summary>
    public const float CureIIIClusterRadius = 10f;

    // Tank Job IDs (array form for iteration)
    /// <summary>All tank job IDs: PLD=19, WAR=21, DRK=32, GNB=37.</summary>
    public static readonly uint[] TankJobIds = [19, 21, 32, 37];

    // Incapacitation status IDs — player is alive but unable to act.
    // Used by ActionTracker to classify GCD downtime as "incapacitated" instead of "unexplained."
    /// <summary>Status IDs that prevent the player from taking any action.</summary>
    public static readonly HashSet<uint> IncapacitationStatusIds = new()
    {
        18,    // Stun
        3,     // Sleep
        1,     // Petrification
        4,     // Bind (rooted — can still cast but movement-gated jobs lose uptime)
        149,   // Deep Freeze
        2656,  // Transcendent (post-raise invulnerability, actions locked)
        3581,  // Willful (duty support auto-revive, actions locked ~7s)
    };

    // Enemy invulnerability status IDs — enemy cannot take damage.
    // Used by TargetingService to skip immune targets during auto-targeting (aggregate
    // strategies only — explicit CurrentTarget/FocusTarget selections are never filtered).
    // Covers boss phase transitions, invulnerable adds, and untouchable objects across
    // ARR through DT content. If a new expansion adds a new invuln status ID, add it here.
    // To find new IDs: target the invulnerable enemy, check its StatusList in the debug
    // window or via /xldata, and add the StatusId that appears during the immune phase.
    /// <summary>Status IDs that indicate an enemy is immune to damage.</summary>
    public static readonly HashSet<uint> EnemyInvulnerabilityStatusIds = new()
    {
        151,   // Invincibility (legacy ARR)
        325,   // Invincibility (universal — used across all content)
        394,   // Invincibility (variant)
        529,   // Invincibility (variant, various duties)
        656,   // Invincibility (trials, e.g. Thordan)
        671,   // Invincibility (variant, widely used)
        775,   // Invincibility (HW content)
        969,   // Invincibility (SB+ content)
        981,   // Invincibility (variant)
        1570,  // Invincibility (ShB content)
        1697,  // Invincibility (ShB/EW raids)
        1829,  // Invincibility (EW content)
        2882,  // Invincibility (EW/DT content)
    };
}
