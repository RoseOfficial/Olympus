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
    /// <summary>HP percentage below which to consider applying Regen.</summary>
    public const float RegenHpThreshold = 0.90f;

    /// <summary>Time remaining on DoT before refreshing (seconds).</summary>
    public const float DotRefreshThreshold = 3f;

    /// <summary>Time remaining on Regen before refreshing (seconds).</summary>
    public const float RegenRefreshThreshold = 3f;

    /// <summary>HP percentage for "injured" party member threshold.</summary>
    public const float InjuredHpThreshold = 0.95f;

    // Action Buffer Timings (small adjustments for weave windows)
    /// <summary>Small timing buffer for weave window calculations.</summary>
    public const float WeaveWindowBuffer = 0.1f;
}
