using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.HermesCore.Helpers;

/// <summary>
/// Helper class for checking Ninja status effects.
/// </summary>
public sealed class HermesStatusHelper
{
    #region Ninjutsu Buffs

    /// <summary>
    /// Checks if the player has Suiton active.
    /// </summary>
    public bool HasSuiton(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.Suiton);
    }

    /// <summary>
    /// Gets the remaining duration of Suiton.
    /// </summary>
    public float GetSuitonRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, NINActions.StatusIds.Suiton);
    }

    /// <summary>
    /// Checks if the player has Kassatsu active.
    /// </summary>
    public bool HasKassatsu(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.Kassatsu);
    }

    /// <summary>
    /// Checks if the player has Ten Chi Jin active.
    /// </summary>
    public bool HasTenChiJin(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.TenChiJin);
    }

    /// <summary>
    /// Gets the remaining stacks of Ten Chi Jin.
    /// </summary>
    public int GetTenChiJinStacks(IBattleChara player)
    {
        return GetStatusStacks(player, NINActions.StatusIds.TenChiJin);
    }

    /// <summary>
    /// Checks if a mudra is currently active (mid-sequence).
    /// </summary>
    public bool IsMudraActive(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.Mudra);
    }

    #endregion

    #region Combat Buffs

    /// <summary>
    /// Checks if the player has Bunshin active.
    /// </summary>
    public bool HasBunshin(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.Bunshin);
    }

    /// <summary>
    /// Gets the remaining stacks of Bunshin.
    /// </summary>
    public int GetBunshinStacks(IBattleChara player)
    {
        return GetStatusStacks(player, NINActions.StatusIds.Bunshin);
    }

    /// <summary>
    /// Checks if Phantom Kamaitachi is ready.
    /// </summary>
    public bool HasPhantomKamaitachiReady(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.PhantomKamaitachiReady);
    }

    /// <summary>
    /// Checks if Raiju is ready.
    /// </summary>
    public bool HasRaijuReady(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.RaijuReady);
    }

    /// <summary>
    /// Gets the number of Raiju stacks available.
    /// </summary>
    public int GetRaijuStacks(IBattleChara player)
    {
        return GetStatusStacks(player, NINActions.StatusIds.RaijuReady);
    }

    /// <summary>
    /// Checks if Meisui is active.
    /// </summary>
    public bool HasMeisui(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.Meisui);
    }

    /// <summary>
    /// Checks if Tenri Jindo is ready.
    /// </summary>
    public bool HasTenriJindoReady(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.TenriJindoReady);
    }

    #endregion

    #region Debuff Tracking

    /// <summary>
    /// Checks if Kunai's Bane is on the target.
    /// </summary>
    public bool HasKunaisBane(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == NINActions.StatusIds.KunaisBane && status.SourceId == playerId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the remaining duration of Kunai's Bane on target.
    /// </summary>
    public float GetKunaisBaneRemaining(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == NINActions.StatusIds.KunaisBane && status.SourceId == playerId)
                return status.RemainingTime;
        }
        return 0f;
    }

    /// <summary>
    /// Checks if Dokumori is on the target.
    /// </summary>
    public bool HasDokumori(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == NINActions.StatusIds.Dokumori && status.SourceId == playerId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the remaining duration of Dokumori on target.
    /// </summary>
    public float GetDokumoriRemaining(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == NINActions.StatusIds.Dokumori && status.SourceId == playerId)
                return status.RemainingTime;
        }
        return 0f;
    }

    /// <summary>
    /// Checks if Vulnerability Up (Trick Attack) is on the target.
    /// </summary>
    public bool HasVulnerabilityUp(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == NINActions.StatusIds.VulnerabilityUp && status.SourceId == playerId)
                return true;
        }
        return false;
    }

    #endregion

    #region Role Buffs

    /// <summary>
    /// Checks if True North is active.
    /// </summary>
    public bool HasTrueNorth(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.TrueNorth);
    }

    /// <summary>
    /// Checks if Bloodbath is active.
    /// </summary>
    public bool HasBloodbath(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.Bloodbath);
    }

    #endregion

    #region Defensive Buffs

    /// <summary>
    /// Checks if Shade Shift is active.
    /// </summary>
    public bool HasShadeShift(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.ShadeShift);
    }

    #endregion

    #region Kazematoi (Aeolian Edge buff)

    /// <summary>
    /// Checks if Kazematoi buff is active.
    /// Note: Kazematoi is tracked via gauge, this is for the buff display.
    /// </summary>
    public bool HasKazematoi(IBattleChara player)
    {
        return HasStatus(player, NINActions.StatusIds.Kazematoi);
    }

    /// <summary>
    /// Gets Kazematoi stacks from status.
    /// </summary>
    public int GetKazematoiStacks(IBattleChara player)
    {
        return GetStatusStacks(player, NINActions.StatusIds.Kazematoi);
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
