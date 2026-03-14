using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.AstraeaCore.Helpers;

/// <summary>
/// Helper class for checking Astrologian-specific status effects.
/// </summary>
public sealed class AstraeaStatusHelper : BaseStatusHelper
{
    #region Buff Status IDs

    // Tank stance status IDs (for detecting Trust NPC tanks)
    private const ushort IronWillStatusId = 79;      // PLD
    private const ushort DefianceStatusId = 91;      // WAR
    private const ushort GritStatusId = 743;         // DRK
    private const ushort RoyalGuardStatusId = 1833;  // GNB

    #endregion

    #region Buff Checks

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

        return GetStatusRemaining(battleChara, ASTActions.AspectedBeneficStatusId);
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

        if (battleChara.StatusList == null)
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

    /// <summary>
    /// Checks if the target has tank stance active.
    /// Used to detect Trust NPC tanks since they don't have ClassJob info.
    /// </summary>
    public bool HasTankStance(IGameObject target)
    {
        if (target is not IBattleChara battleChara)
            return false;

        if (battleChara.StatusList == null)
            return false;

        foreach (var status in battleChara.StatusList)
        {
            if (status.StatusId is IronWillStatusId or
                DefianceStatusId or
                GritStatusId or
                RoyalGuardStatusId)
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

        if (battleChara.StatusList == null)
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

        if (battleChara.StatusList == null)
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

    // Core status methods (HasStatus, GetStatusRemaining) inherited from BaseStatusHelper
}
