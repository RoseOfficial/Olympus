using Olympus.Rotation.Common;
using Olympus.Rotation.TerpsichoreCore.Helpers;
using Olympus.Services.Party;

namespace Olympus.Rotation.TerpsichoreCore.Context;

/// <summary>
/// Dancer-specific rotation context interface.
/// Extends IRangedDpsRotationContext with Dancer-specific state.
/// </summary>
public interface ITerpsichoreContext : IRangedDpsRotationContext
{
    #region Gauge State

    /// <summary>
    /// Current Esprit gauge (0-100).
    /// Built by dance partner/self damage, spent by Saber Dance.
    /// </summary>
    int Esprit { get; }

    /// <summary>
    /// Current Feather count (0-4).
    /// Built by proc GCDs, spent by Fan Dance.
    /// </summary>
    int Feathers { get; }

    /// <summary>
    /// Whether currently in dance mode (Standard or Technical Step).
    /// </summary>
    bool IsDancing { get; }

    /// <summary>
    /// Current step index during a dance (0-3).
    /// </summary>
    int StepIndex { get; }

    /// <summary>
    /// The next step to execute in the current dance (1-4).
    /// 1=Emboite, 2=Entrechat, 3=Jete, 4=Pirouette.
    /// </summary>
    byte CurrentStep { get; }

    /// <summary>
    /// Array of dance steps to execute (length 4).
    /// Standard Step uses first 2, Technical Step uses all 4.
    /// </summary>
    byte[] DanceSteps { get; }

    #endregion

    #region Proc State

    /// <summary>
    /// Whether Silken Symmetry (or Flourishing Symmetry) proc is active.
    /// Enables Reverse Cascade / Rising Windmill.
    /// </summary>
    bool HasSilkenSymmetry { get; }

    /// <summary>
    /// Whether Silken Flow (or Flourishing Flow) proc is active.
    /// Enables Fountainfall / Bloodshower.
    /// </summary>
    bool HasSilkenFlow { get; }

    /// <summary>
    /// Whether Threefold Fan Dance proc is active.
    /// Enables Fan Dance III.
    /// </summary>
    bool HasThreefoldFanDance { get; }

    /// <summary>
    /// Whether Fourfold Fan Dance proc is active (from Flourish).
    /// Enables Fan Dance IV.
    /// </summary>
    bool HasFourfoldFanDance { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Flourishing Finish buff is active (enables Tillana).
    /// </summary>
    bool HasFlourishingFinish { get; }

    /// <summary>
    /// Whether Flourishing Starfall buff is active (enables Starfall Dance).
    /// Granted by Devilment at Lv.90+.
    /// </summary>
    bool HasFlourishingStarfall { get; }

    /// <summary>
    /// Whether Devilment buff is active (+20% crit/DH).
    /// </summary>
    bool HasDevilment { get; }

    /// <summary>
    /// Remaining duration of Devilment in seconds.
    /// </summary>
    float DevilmentRemaining { get; }

    /// <summary>
    /// Whether Standard Finish party buff is active.
    /// </summary>
    bool HasStandardFinish { get; }

    /// <summary>
    /// Whether Technical Finish party buff is active.
    /// </summary>
    bool HasTechnicalFinish { get; }

    #endregion

    #region High-Level Procs (Lv.90+)

    /// <summary>
    /// Whether Last Dance Ready buff is active (Lv.92+).
    /// </summary>
    bool HasLastDanceReady { get; }

    /// <summary>
    /// Whether Finishing Move Ready buff is active (Lv.96+).
    /// </summary>
    bool HasFinishingMoveReady { get; }

    /// <summary>
    /// Whether Dance of the Dawn Ready buff is active (Lv.100).
    /// </summary>
    bool HasDanceOfTheDawnReady { get; }

    #endregion

    #region Partner State

    /// <summary>
    /// Whether we have a dance partner set (Closed Position active).
    /// </summary>
    bool HasDancePartner { get; }

    /// <summary>
    /// Entity ID of the current dance partner, or 0 if none.
    /// </summary>
    uint DancePartnerId { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    TerpsichoreStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries and partner selection.
    /// </summary>
    TerpsichorePartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    TerpsichoreDebugState Debug { get; }

    #endregion

    #region Party Coordination

    /// <summary>
    /// Party coordination service for raid buff synchronization.
    /// </summary>
    IPartyCoordinationService? PartyCoordinationService { get; }

    #endregion
}
