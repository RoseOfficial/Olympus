using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.TerpsichoreCore.Helpers;

/// <summary>
/// Helper for checking Dancer-specific buffs and debuffs.
/// </summary>
public sealed class TerpsichoreStatusHelper : BaseStatusHelper
{
    #region Proc Buffs

    /// <summary>
    /// Checks if Silken Symmetry (or Flourishing Symmetry) buff is active.
    /// </summary>
    public bool HasSilkenSymmetry(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.SilkenSymmetry) ||
           HasStatus(player, DNCActions.StatusIds.FlourishingSymmetry);

    /// <summary>
    /// Checks if Silken Flow (or Flourishing Flow) buff is active.
    /// </summary>
    public bool HasSilkenFlow(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.SilkenFlow) ||
           HasStatus(player, DNCActions.StatusIds.FlourishingFlow);

    /// <summary>
    /// Checks if Threefold Fan Dance buff is active.
    /// </summary>
    public bool HasThreefoldFanDance(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.ThreefoldFanDance);

    /// <summary>
    /// Checks if Fourfold Fan Dance buff is active.
    /// </summary>
    public bool HasFourfoldFanDance(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.FourfoldFanDance);

    #endregion

    #region Dance Finisher Buffs

    /// <summary>
    /// Checks if Flourishing Finish buff is active (enables Tillana).
    /// </summary>
    public bool HasFlourishingFinish(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.FlourishingFinish);

    /// <summary>
    /// Checks if Flourishing Starfall buff is active (enables Starfall Dance).
    /// </summary>
    public bool HasFlourishingStarfall(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.FlourishingStarfall);

    #endregion

    #region Burst Buffs

    /// <summary>
    /// Checks if Devilment buff is active.
    /// </summary>
    public bool HasDevilment(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.Devilment);

    /// <summary>
    /// Gets remaining duration of Devilment buff.
    /// </summary>
    public float GetDevilmentRemaining(IBattleChara player)
        => GetStatusRemaining(player, DNCActions.StatusIds.Devilment);

    /// <summary>
    /// Checks if Standard Finish party buff is active.
    /// </summary>
    public bool HasStandardFinish(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.StandardFinish);

    /// <summary>
    /// Checks if Technical Finish party buff is active.
    /// </summary>
    public bool HasTechnicalFinish(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.TechnicalFinish);

    #endregion

    #region High-Level Procs (Lv.90+)

    /// <summary>
    /// Checks if Last Dance Ready buff is active (Lv.92+).
    /// </summary>
    public bool HasLastDanceReady(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.LastDanceReady);

    /// <summary>
    /// Checks if Finishing Move Ready buff is active (Lv.96+).
    /// </summary>
    public bool HasFinishingMoveReady(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.FinishingMoveReady);

    /// <summary>
    /// Checks if Dance of the Dawn Ready buff is active (Lv.100).
    /// </summary>
    public bool HasDanceOfTheDawnReady(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.DanceOfTheDawnReady);

    #endregion

    #region Partner Buffs

    /// <summary>
    /// Checks if Closed Position buff is active (we have a partner).
    /// </summary>
    public bool HasClosedPosition(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.ClosedPosition);

    /// <summary>
    /// Checks if a character has the Dance Partner buff from a specific source.
    /// </summary>
    public bool HasDancePartnerFrom(IBattleChara target, uint sourceId)
        => HasStatusFromSource(target, DNCActions.StatusIds.DancePartner, sourceId);

    #endregion

    #region Utility Buffs

    /// <summary>
    /// Checks if Shield Samba buff is active.
    /// </summary>
    public bool HasShieldSamba(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.ShieldSamba);

    /// <summary>
    /// Checks if Improvisation buff is active.
    /// </summary>
    public bool HasImprovisation(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.Improvisation);

    /// <summary>
    /// Checks if Arm's Length buff is active.
    /// </summary>
    public bool HasArmsLength(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.ArmsLength);

    /// <summary>
    /// Checks if Peloton buff is active.
    /// </summary>
    public bool HasPeloton(IBattleChara player)
        => HasStatus(player, DNCActions.StatusIds.Peloton);

    #endregion

}
