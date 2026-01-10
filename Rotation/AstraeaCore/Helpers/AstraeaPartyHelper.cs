using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.AstraeaCore.Helpers;

/// <summary>
/// Astrologian party helper with AST-specific targeting logic.
/// Extends HealerPartyHelper with card targeting and AST-specific heals.
/// </summary>
public sealed class AstraeaPartyHelper : HealerPartyHelper
{
    private readonly AstraeaStatusHelper _statusHelper;

    public AstraeaPartyHelper(
        IObjectTable objectTable,
        IPartyList partyList,
        HpPredictionService hpPredictionService,
        Configuration configuration,
        AstraeaStatusHelper statusHelper)
        : base(objectTable, partyList, hpPredictionService, configuration)
    {
        _statusHelper = statusHelper;
    }

    #region IPartyHelper-style Methods

    /// <summary>
    /// Finds the lowest HP party member that needs healing.
    /// </summary>
    public IBattleChara? FindLowestHpPartyMember(IPlayerCharacter player, int healAmount = 0)
    {
        return FindLowestHpPartyMember(player, ASTActions.Benefic.RangeSquared, healAmount);
    }

    /// <summary>
    /// Finds a dead party member that needs resurrection.
    /// </summary>
    public IBattleChara? FindDeadPartyMemberNeedingRaise(IPlayerCharacter player)
    {
        return FindDeadPartyMemberNeedingRaise(player, ASTActions.Ascend.RangeSquared);
    }

    #endregion

    #region AoE Heal Counting

    /// <summary>
    /// Counts party members needing AoE heal.
    /// </summary>
    public (int count, List<uint> targetIds) CountPartyMembersNeedingAoEHeal(IPlayerCharacter player, int healAmount)
    {
        return CountPartyMembersNeedingAoEHeal(player, ASTActions.Helios.RadiusSquared, healAmount);
    }

    #endregion

    #region Card Targeting (DPS-Focused)

    /// <summary>
    /// Finds the best target for The Balance card (melee DPS buff).
    /// Prioritizes melee DPS, then ranged DPS if no melee available.
    /// </summary>
    public IBattleChara? FindBalanceTarget(IPlayerCharacter player)
    {
        IBattleChara? bestMelee = null;
        IBattleChara? bestRanged = null;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.PlayI.RangeSquared)
                continue;
            if (_statusHelper.HasAnyCardBuff(member))
                continue;

            if (IsMeleeDps(member))
            {
                if (bestMelee == null)
                    bestMelee = member;
            }
            else if (IsRangedPhysicalDps(member) || IsCasterDps(member))
            {
                if (bestRanged == null)
                    bestRanged = member;
            }
        }

        // Prefer melee for Balance, fallback to ranged
        return bestMelee ?? bestRanged;
    }

    /// <summary>
    /// Finds the best target for The Spear card (ranged DPS buff).
    /// Prioritizes ranged DPS (physical + caster), then melee if no ranged available.
    /// </summary>
    public IBattleChara? FindSpearTarget(IPlayerCharacter player)
    {
        IBattleChara? bestRanged = null;
        IBattleChara? bestMelee = null;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.PlayII.RangeSquared)
                continue;
            if (_statusHelper.HasAnyCardBuff(member))
                continue;

            if (IsRangedPhysicalDps(member) || IsCasterDps(member))
            {
                if (bestRanged == null)
                    bestRanged = member;
            }
            else if (IsMeleeDps(member))
            {
                if (bestMelee == null)
                    bestMelee = member;
            }
        }

        // Prefer ranged for Spear, fallback to melee
        return bestRanged ?? bestMelee;
    }

    /// <summary>
    /// Finds the best target for Lord of Crowns card (AoE damage buff).
    /// Any DPS is valid target.
    /// </summary>
    public IBattleChara? FindLordTarget(IPlayerCharacter player)
    {
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.PlayIII.RangeSquared)
                continue;
            if (_statusHelper.HasAnyCardBuff(member))
                continue;

            if (IsDpsRole(member))
                return member;
        }

        // Fallback to tank if no DPS available
        return FindTankInParty(player);
    }

    /// <summary>
    /// Gets the appropriate card target based on the current card type.
    /// </summary>
    public IBattleChara? FindCardTarget(IPlayerCharacter player, ASTActions.CardType cardType)
    {
        return cardType switch
        {
            ASTActions.CardType.TheBalance => FindBalanceTarget(player),
            ASTActions.CardType.TheSpear => FindSpearTarget(player),
            ASTActions.CardType.Lord => FindLordTarget(player),
            _ => null
        };
    }

    #endregion

    #region Essential Dignity Target

    /// <summary>
    /// Finds the best target for Essential Dignity.
    /// Essential Dignity scales with missing HP (400-1100 potency), so lower HP is better.
    /// </summary>
    public IBattleChara? FindEssentialDignityTarget(IPlayerCharacter player, float hpThreshold = 0.4f)
    {
        IBattleChara? bestTarget = null;
        float lowestHp = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.EssentialDignity.RangeSquared)
                continue;

            var hpPercent = GetHpPercent(member);

            if (hpPercent < hpThreshold && hpPercent < lowestHp)
            {
                lowestHp = hpPercent;
                bestTarget = member;
            }
        }

        return bestTarget;
    }

    #endregion

    #region Synastry Target

    /// <summary>
    /// Finds the best target for Synastry (usually tank taking sustained damage).
    /// </summary>
    public IBattleChara? FindSynastryTarget(IPlayerCharacter player)
    {
        // Priority 1: Tank
        var tank = FindTankInParty(player);
        if (tank != null && !_statusHelper.HasSynastryLink(tank))
        {
            var hpPercent = GetHpPercent(tank);
            if (hpPercent < 0.8f) // Only if tank has taken some damage
                return tank;
        }

        // Priority 2: Lowest HP party member
        return FindLowestHpPartyMember(player);
    }

    #endregion

    #region Exaltation Target

    /// <summary>
    /// Finds the best target for Exaltation (damage reduction + delayed heal).
    /// </summary>
    public IBattleChara? FindExaltationTarget(IPlayerCharacter player)
    {
        // Priority 1: Tank without Exaltation taking damage
        var tank = FindTankInParty(player);
        if (tank != null && !_statusHelper.HasExaltation(tank))
        {
            var hpPercent = GetHpPercent(tank);
            if (hpPercent < 0.8f)
                return tank;
        }

        // Priority 2: Any party member below threshold without Exaltation
        IBattleChara? lowestMember = null;
        float lowestHp = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.Exaltation.RangeSquared)
                continue;
            if (_statusHelper.HasExaltation(member))
                continue;

            var hpPercent = GetHpPercent(member);
            if (hpPercent < lowestHp && hpPercent < 0.7f)
            {
                lowestHp = hpPercent;
                lowestMember = member;
            }
        }

        return lowestMember ?? tank;
    }

    #endregion
}
