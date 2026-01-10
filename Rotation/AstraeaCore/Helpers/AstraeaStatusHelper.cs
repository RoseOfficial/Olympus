using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.AstraeaCore.Helpers;

/// <summary>
/// Helper class for checking Astrologian-specific status effects.
/// </summary>
public sealed class AstraeaStatusHelper
{
    #region Buff Status IDs

    private const ushort SwiftcastStatusId = 167;
    private const ushort LucidDreamingStatusId = 1204;

    #endregion

    #region Buff Checks

    /// <summary>
    /// Checks if the player has Swiftcast active.
    /// </summary>
    public bool HasSwiftcast(IPlayerCharacter player)
    {
        return HasStatus(player, SwiftcastStatusId);
    }

    /// <summary>
    /// Checks if the player has Lucid Dreaming active.
    /// </summary>
    public bool HasLucidDreaming(IPlayerCharacter player)
    {
        return HasStatus(player, LucidDreamingStatusId);
    }

    /// <summary>
    /// Checks if the player has Lightspeed active (instant casts).
    /// </summary>
    public bool HasLightspeed(IPlayerCharacter player)
    {
        return HasStatus(player, ASTActions.LightspeedStatusId);
    }

    /// <summary>
    /// Checks if the player has Neutral Sect active (enhanced heals + shields).
    /// </summary>
    public bool HasNeutralSect(IPlayerCharacter player)
    {
        return HasStatus(player, ASTActions.NeutralSectStatusId);
    }

    /// <summary>
    /// Checks if the player has Divining status (Oracle proc).
    /// </summary>
    public bool HasDivining(IPlayerCharacter player)
    {
        return HasStatus(player, ASTActions.DiviningStatusId);
    }

    /// <summary>
    /// Checks if the player has Horoscope buff (can detonate for heal).
    /// </summary>
    public bool HasHoroscope(IPlayerCharacter player)
    {
        return HasStatus(player, ASTActions.HoroscopeStatusId);
    }

    /// <summary>
    /// Checks if the player has Horoscope Helios buff (enhanced, 400 potency detonate).
    /// </summary>
    public bool HasHoroscopeHelios(IPlayerCharacter player)
    {
        return HasStatus(player, ASTActions.HoroscopeHeliosStatusId);
    }

    /// <summary>
    /// Checks if the player has Macrocosmos active (can detonate for heal).
    /// </summary>
    public bool HasMacrocosmos(IPlayerCharacter player)
    {
        return HasStatus(player, ASTActions.MacrocosmosStatusId);
    }

    /// <summary>
    /// Checks if the player has Synastry active.
    /// </summary>
    public bool HasSynastry(IPlayerCharacter player)
    {
        return HasStatus(player, ASTActions.SynastryStatusId);
    }

    #endregion

    #region Target Buff Checks

    /// <summary>
    /// Checks if the target has Aspected Benefic regen.
    /// </summary>
    public bool HasAspectedBenefic(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        return HasStatus(battleChara, ASTActions.AspectedBeneficStatusId);
    }

    /// <summary>
    /// Gets the remaining duration of Aspected Benefic on a target.
    /// </summary>
    public float GetAspectedBeneficDuration(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return 0f;

        return GetStatusDuration(battleChara, ASTActions.AspectedBeneficStatusId);
    }

    /// <summary>
    /// Checks if the target has Exaltation buff.
    /// </summary>
    public bool HasExaltation(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        return HasStatus(battleChara, ASTActions.ExaltationStatusId);
    }

    /// <summary>
    /// Checks if the target has Synastry link active.
    /// </summary>
    public bool HasSynastryLink(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        return HasStatus(battleChara, ASTActions.SynastryStatusId);
    }

    /// <summary>
    /// Checks if the target has The Balance card buff.
    /// </summary>
    public bool HasBalanceBuff(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        return HasStatus(battleChara, ASTActions.TheBalanceStatusId);
    }

    /// <summary>
    /// Checks if the target has The Spear card buff.
    /// </summary>
    public bool HasSpearBuff(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        return HasStatus(battleChara, ASTActions.TheSpearStatusId);
    }

    /// <summary>
    /// Checks if the target has any card buff active.
    /// </summary>
    public bool HasAnyCardBuff(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        foreach (var status in battleChara.StatusList)
        {
            if (status.StatusId is ASTActions.TheBalanceStatusId or
                ASTActions.TheSpearStatusId or
                ASTActions.LordOfCrownsStatusId)
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Enemy Debuff Checks

    /// <summary>
    /// Checks if the target has our DoT (Combust/Combust II/Combust III).
    /// </summary>
    public bool HasOurDot(IPlayerCharacter player, IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        foreach (var status in battleChara.StatusList)
        {
            if (status.SourceId != player.EntityId)
                continue;

            if (status.StatusId is ASTActions.CombustStatusId or
                ASTActions.CombustIIStatusId or
                ASTActions.CombustIIIStatusId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the remaining duration of our DoT on the target.
    /// </summary>
    public float GetDotDuration(IPlayerCharacter player, IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return 0f;

        foreach (var status in battleChara.StatusList)
        {
            if (status.SourceId != player.EntityId)
                continue;

            if (status.StatusId is ASTActions.CombustStatusId or
                ASTActions.CombustIIStatusId or
                ASTActions.CombustIIIStatusId)
            {
                return status.RemainingTime;
            }
        }

        return 0f;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Generic status check for IBattleChara.
    /// </summary>
    private static bool HasStatus(IBattleChara chara, ushort statusId)
    {
        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == statusId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the remaining duration of a status effect.
    /// </summary>
    private static float GetStatusDuration(IBattleChara chara, ushort statusId)
    {
        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == statusId)
                return status.RemainingTime;
        }
        return 0f;
    }

    #endregion
}
