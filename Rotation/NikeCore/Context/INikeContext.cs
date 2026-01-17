using Olympus.Data;
using Olympus.Rotation.Common;
using Olympus.Rotation.NikeCore.Helpers;

namespace Olympus.Rotation.NikeCore.Context;

/// <summary>
/// Samurai-specific rotation context interface.
/// Extends IMeleeDpsRotationContext with Samurai-specific state.
/// </summary>
public interface INikeContext : IMeleeDpsRotationContext
{
    #region Kenki Gauge

    /// <summary>
    /// Current Kenki gauge value (0-100).
    /// Spent on Hissatsu abilities.
    /// </summary>
    int Kenki { get; }

    #endregion

    #region Sen Gauge

    /// <summary>
    /// Current Sen flags (Setsu, Getsu, Ka).
    /// </summary>
    SAMActions.SenType Sen { get; }

    /// <summary>
    /// Number of active Sen (0-3).
    /// </summary>
    int SenCount { get; }

    /// <summary>
    /// Whether Setsu (Snow) Sen is active.
    /// </summary>
    bool HasSetsu { get; }

    /// <summary>
    /// Whether Getsu (Moon) Sen is active.
    /// </summary>
    bool HasGetsu { get; }

    /// <summary>
    /// Whether Ka (Flower) Sen is active.
    /// </summary>
    bool HasKa { get; }

    #endregion

    #region Meditation Gauge

    /// <summary>
    /// Current Meditation stacks (0-3).
    /// Spent on Shoha at 3 stacks.
    /// </summary>
    int Meditation { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Fugetsu (13% damage up) is active.
    /// </summary>
    bool HasFugetsu { get; }

    /// <summary>
    /// Remaining duration of Fugetsu in seconds.
    /// </summary>
    float FugetsuRemaining { get; }

    /// <summary>
    /// Whether Fuka (13% haste) is active.
    /// </summary>
    bool HasFuka { get; }

    /// <summary>
    /// Remaining duration of Fuka in seconds.
    /// </summary>
    float FukaRemaining { get; }

    /// <summary>
    /// Whether Meikyo Shisui is active (combo skip).
    /// </summary>
    bool HasMeikyoShisui { get; }

    /// <summary>
    /// Remaining stacks of Meikyo Shisui.
    /// </summary>
    int MeikyoStacks { get; }

    /// <summary>
    /// Whether Ogi Namikiri is ready (from Ikishoten).
    /// </summary>
    bool HasOgiNamikiriReady { get; }

    /// <summary>
    /// Whether Kaeshi: Namikiri is ready (after Ogi Namikiri).
    /// </summary>
    bool HasKaeshiNamikiriReady { get; }

    /// <summary>
    /// Whether Tsubame-gaeshi is ready (after Iaijutsu).
    /// </summary>
    bool HasTsubameGaeshiReady { get; }

    /// <summary>
    /// Whether Zanshin is ready (after Ogi Namikiri).
    /// </summary>
    bool HasZanshinReady { get; }

    #endregion

    #region DoT State

    /// <summary>
    /// Whether Higanbana DoT is on the current target.
    /// </summary>
    bool HasHiganbanaOnTarget { get; }

    /// <summary>
    /// Remaining duration of Higanbana on target.
    /// </summary>
    float HiganbanaRemaining { get; }

    #endregion

    #region Iaijutsu State

    /// <summary>
    /// The last Iaijutsu used (for Kaeshi selection).
    /// </summary>
    SAMActions.IaijutsuType LastIaijutsu { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    NikeStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    NikePartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    NikeDebugState Debug { get; }

    #endregion
}
