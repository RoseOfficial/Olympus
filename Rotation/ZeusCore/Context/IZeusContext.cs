using Olympus.Rotation.Common;
using Olympus.Rotation.ZeusCore.Helpers;

namespace Olympus.Rotation.ZeusCore.Context;

/// <summary>
/// Dragoon-specific rotation context interface.
/// Extends IMeleeDpsRotationContext with Dragoon-specific state.
/// </summary>
public interface IZeusContext : IMeleeDpsRotationContext
{
    #region Gauge State

    /// <summary>
    /// Current Firstmind's Focus count (0-2).
    /// Used for Wyrmwind Thrust.
    /// </summary>
    int FirstmindsFocus { get; }

    /// <summary>
    /// Current Dragon Eye count (0-2).
    /// At 2, Geirskogul enters Life of the Dragon.
    /// </summary>
    int EyeCount { get; }

    /// <summary>
    /// Whether Life of the Dragon is currently active.
    /// </summary>
    bool IsLifeOfDragonActive { get; }

    /// <summary>
    /// Remaining time on Life of the Dragon (seconds).
    /// </summary>
    float LifeOfDragonRemaining { get; }

    #endregion

    #region Combo State

    /// <summary>
    /// Whether currently in the Vorpal line (True Thrust → Vorpal).
    /// </summary>
    bool IsInVorpalCombo { get; }

    /// <summary>
    /// Whether currently in the Disembowel line (True Thrust → Disembowel).
    /// </summary>
    bool IsInDisembowelCombo { get; }

    /// <summary>
    /// Whether currently in the AoE combo (Doom Spike line).
    /// </summary>
    bool IsInAoeCombo { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Power Surge (+15% damage) is active.
    /// </summary>
    bool HasPowerSurge { get; }

    /// <summary>
    /// Remaining duration of Power Surge (seconds).
    /// </summary>
    float PowerSurgeRemaining { get; }

    /// <summary>
    /// Whether Lance Charge (+10% damage) is active.
    /// </summary>
    bool HasLanceCharge { get; }

    /// <summary>
    /// Remaining duration of Lance Charge (seconds).
    /// </summary>
    float LanceChargeRemaining { get; }

    /// <summary>
    /// Whether Life Surge (guaranteed crit) is active.
    /// </summary>
    bool HasLifeSurge { get; }

    /// <summary>
    /// Whether Battle Litany (party crit) is active.
    /// </summary>
    bool HasBattleLitany { get; }

    /// <summary>
    /// Remaining duration of Battle Litany (seconds).
    /// </summary>
    float BattleLitanyRemaining { get; }

    /// <summary>
    /// Whether Right Eye (Dragon Sight self buff) is active.
    /// </summary>
    bool HasRightEye { get; }

    #endregion

    #region Proc State

    /// <summary>
    /// Whether Dive Ready (can use Mirage Dive) is active.
    /// </summary>
    bool HasDiveReady { get; }

    /// <summary>
    /// Whether Fang and Claw Bared proc is ready (flank positional).
    /// </summary>
    bool HasFangAndClawBared { get; }

    /// <summary>
    /// Whether Wheel in Motion proc is ready (rear positional).
    /// </summary>
    bool HasWheelInMotion { get; }

    /// <summary>
    /// Whether Draconian Fire is active (Enhanced Coerthan Torment / Rise of the Dragon).
    /// </summary>
    bool HasDraconianFire { get; }

    /// <summary>
    /// Whether Nastrond Ready is active (can use Nastrond during Life).
    /// </summary>
    bool HasNastrondReady { get; }

    /// <summary>
    /// Whether Stardiver Ready is active (can use Stardiver).
    /// </summary>
    bool HasStardiverReady { get; }

    /// <summary>
    /// Whether Starcross Ready is active (can use Starcross after Stardiver).
    /// </summary>
    bool HasStarcrossReady { get; }

    #endregion

    #region DoT State

    /// <summary>
    /// Whether the DoT (Chaos Thrust / Chaotic Spring) is on the target.
    /// </summary>
    bool HasDotOnTarget { get; }

    /// <summary>
    /// Remaining duration of the DoT on target (seconds).
    /// </summary>
    float DotRemaining { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    ZeusStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    ZeusPartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    ZeusDebugState Debug { get; }

    #endregion
}
