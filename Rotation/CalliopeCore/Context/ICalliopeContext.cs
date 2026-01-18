using Olympus.Rotation.Common;
using Olympus.Rotation.CalliopeCore.Helpers;

namespace Olympus.Rotation.CalliopeCore.Context;

/// <summary>
/// Bard-specific rotation context interface.
/// Extends IRangedDpsRotationContext with Bard-specific state.
/// </summary>
public interface ICalliopeContext : IRangedDpsRotationContext
{
    #region Gauge State

    /// <summary>
    /// Current Soul Voice gauge (0-100).
    /// Built by song procs, spent by Apex Arrow.
    /// </summary>
    int SoulVoice { get; }

    /// <summary>
    /// Current song timer remaining in seconds.
    /// </summary>
    float SongTimer { get; }

    /// <summary>
    /// Current Repertoire stacks (0-4).
    /// WM: Pitch Perfect stacks, AP: Speed stacks.
    /// </summary>
    int Repertoire { get; }

    /// <summary>
    /// Current active song (0=None, 1=MB, 2=AP, 3=WM).
    /// </summary>
    byte CurrentSong { get; }

    /// <summary>
    /// Number of Coda available for Radiant Finale (0-3).
    /// </summary>
    int CodaCount { get; }

    #endregion

    #region Song State

    /// <summary>
    /// Whether The Wanderer's Minuet is currently active.
    /// </summary>
    bool IsWanderersMinuetActive { get; }

    /// <summary>
    /// Whether Mage's Ballad is currently active.
    /// </summary>
    bool IsMagesBalladActive { get; }

    /// <summary>
    /// Whether Army's Paeon is currently active.
    /// </summary>
    bool IsArmysPaeonActive { get; }

    /// <summary>
    /// Whether no song is currently active.
    /// </summary>
    bool NoSongActive { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Straight Shot Ready (Hawk's Eye) buff is active.
    /// </summary>
    bool HasHawksEye { get; }

    /// <summary>
    /// Whether Raging Strikes buff is active.
    /// </summary>
    bool HasRagingStrikes { get; }

    /// <summary>
    /// Remaining duration of Raging Strikes in seconds.
    /// </summary>
    float RagingStrikesRemaining { get; }

    /// <summary>
    /// Whether Battle Voice buff is active.
    /// </summary>
    bool HasBattleVoice { get; }

    /// <summary>
    /// Whether Barrage buff is active.
    /// </summary>
    bool HasBarrage { get; }

    /// <summary>
    /// Whether Radiant Finale buff is active.
    /// </summary>
    bool HasRadiantFinale { get; }

    /// <summary>
    /// Whether Blast Arrow Ready buff is active.
    /// </summary>
    bool HasBlastArrowReady { get; }

    /// <summary>
    /// Whether Resonant Arrow Ready buff is active.
    /// </summary>
    bool HasResonantArrowReady { get; }

    /// <summary>
    /// Whether Radiant Encore Ready buff is active.
    /// </summary>
    bool HasRadiantEncoreReady { get; }

    #endregion

    #region DoT State

    /// <summary>
    /// Whether Caustic Bite (or Venomous Bite) DoT is on current target.
    /// </summary>
    bool HasCausticBite { get; }

    /// <summary>
    /// Remaining duration of Caustic Bite on target in seconds.
    /// </summary>
    float CausticBiteRemaining { get; }

    /// <summary>
    /// Whether Stormbite (or Windbite) DoT is on current target.
    /// </summary>
    bool HasStormbite { get; }

    /// <summary>
    /// Remaining duration of Stormbite on target in seconds.
    /// </summary>
    float StormbiteRemaining { get; }

    #endregion

    #region Cooldown Tracking

    /// <summary>
    /// Charges of Bloodletter/Heartbreak Shot available (0-3).
    /// </summary>
    int BloodletterCharges { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    CalliopeStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    CalliopePartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    CalliopeDebugState Debug { get; }

    #endregion
}
