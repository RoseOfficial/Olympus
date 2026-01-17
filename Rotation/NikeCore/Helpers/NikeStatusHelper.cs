using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.NikeCore.Helpers;

/// <summary>
/// Helper class for checking Samurai status effects.
/// </summary>
public sealed class NikeStatusHelper
{
    #region Damage Buffs

    /// <summary>
    /// Checks if the player has Fugetsu active (13% damage up).
    /// </summary>
    public bool HasFugetsu(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.Fugetsu);
    }

    /// <summary>
    /// Gets the remaining duration of Fugetsu.
    /// </summary>
    public float GetFugetsuRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, SAMActions.StatusIds.Fugetsu);
    }

    /// <summary>
    /// Checks if the player has Fuka active (13% haste).
    /// </summary>
    public bool HasFuka(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.Fuka);
    }

    /// <summary>
    /// Gets the remaining duration of Fuka.
    /// </summary>
    public float GetFukaRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, SAMActions.StatusIds.Fuka);
    }

    #endregion

    #region Special State Buffs

    /// <summary>
    /// Checks if the player has Meikyo Shisui active (combo skip).
    /// </summary>
    public bool HasMeikyoShisui(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.MeikyoShisui);
    }

    /// <summary>
    /// Gets the remaining stacks of Meikyo Shisui.
    /// </summary>
    public int GetMeikyoStacks(IBattleChara player)
    {
        return GetStatusStacks(player, SAMActions.StatusIds.MeikyoShisui);
    }

    /// <summary>
    /// Checks if Ogi Namikiri is ready (from Ikishoten).
    /// </summary>
    public bool HasOgiNamikiriReady(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.OgiNamikiriReady);
    }

    /// <summary>
    /// Checks if Kaeshi: Namikiri is ready (after Ogi Namikiri).
    /// </summary>
    public bool HasKaeshiNamikiriReady(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.KaeshiNamikiriReady);
    }

    /// <summary>
    /// Checks if Tsubame-gaeshi is ready (after Iaijutsu).
    /// </summary>
    public bool HasTsubameGaeshiReady(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.TsubameGaeshiReady);
    }

    /// <summary>
    /// Checks if Zanshin is ready (after Ogi Namikiri).
    /// </summary>
    public bool HasZanshinReady(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.ZanshinReady);
    }

    #endregion

    #region DoT Tracking

    /// <summary>
    /// Checks if Higanbana DoT is on the target.
    /// </summary>
    public bool HasHiganbana(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == SAMActions.StatusIds.Higanbana && status.SourceId == playerId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the remaining duration of Higanbana on target.
    /// </summary>
    public float GetHiganbanaRemaining(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == SAMActions.StatusIds.Higanbana && status.SourceId == playerId)
                return status.RemainingTime;
        }
        return 0f;
    }

    #endregion

    #region Utility Buffs

    /// <summary>
    /// Checks if Enhanced Enpi is active (from Yaten).
    /// </summary>
    public bool HasEnhancedEnpi(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.EnhancedEnpi);
    }

    /// <summary>
    /// Checks if Third Eye is active.
    /// </summary>
    public bool HasThirdEye(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.ThirdEye);
    }

    /// <summary>
    /// Checks if Tengentsu is active.
    /// </summary>
    public bool HasTengentsu(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.Tengentsu);
    }

    #endregion

    #region Role Buffs

    /// <summary>
    /// Checks if True North is active.
    /// </summary>
    public bool HasTrueNorth(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.TrueNorth);
    }

    /// <summary>
    /// Checks if Bloodbath is active.
    /// </summary>
    public bool HasBloodbath(IBattleChara player)
    {
        return HasStatus(player, SAMActions.StatusIds.Bloodbath);
    }

    #endregion

    #region Public Helpers

    /// <summary>
    /// Checks if the character has a specific status effect.
    /// </summary>
    public bool HasStatus(IBattleChara character, uint statusId)
    {
        foreach (var status in character.StatusList)
        {
            if (status.StatusId == statusId)
                return true;
        }
        return false;
    }

    #endregion

    #region Private Helpers

    private static float GetStatusRemaining(IBattleChara character, uint statusId)
    {
        foreach (var status in character.StatusList)
        {
            if (status.StatusId == statusId)
                return status.RemainingTime;
        }
        return 0f;
    }

    private static int GetStatusStacks(IBattleChara character, uint statusId)
    {
        foreach (var status in character.StatusList)
        {
            if (status.StatusId == statusId)
                return status.Param;
        }
        return 0;
    }

    #endregion
}
