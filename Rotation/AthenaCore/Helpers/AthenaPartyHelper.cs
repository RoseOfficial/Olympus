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

namespace Olympus.Rotation.AthenaCore.Helpers;

/// <summary>
/// Scholar party helper with SCH-specific targeting logic.
/// Extends HealerPartyHelper with Deployment, Excog, and Fey Union targeting.
/// </summary>
public sealed class AthenaPartyHelper : HealerPartyHelper
{
    private readonly AthenaStatusHelper _statusHelper;

    public AthenaPartyHelper(
        IObjectTable objectTable,
        IPartyList partyList,
        HpPredictionService hpPredictionService,
        Configuration configuration,
        AthenaStatusHelper statusHelper)
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
        return FindLowestHpPartyMember(player, SCHActions.Physick.RangeSquared, healAmount);
    }

    /// <summary>
    /// Finds a dead party member that needs resurrection.
    /// </summary>
    public IBattleChara? FindDeadPartyMemberNeedingRaise(IPlayerCharacter player)
    {
        return FindDeadPartyMemberNeedingRaise(player, SCHActions.Resurrection.RangeSquared);
    }

    #endregion

    #region AoE Heal Counting

    /// <summary>
    /// Counts party members needing AoE heal (for Succor/Indomitability).
    /// </summary>
    public (int count, List<uint> targetIds) CountPartyMembersNeedingAoEHeal(IPlayerCharacter player, int healAmount)
    {
        return CountPartyMembersNeedingAoEHeal(player, SCHActions.Succor.RadiusSquared, healAmount);
    }

    #endregion

    #region Scholar-Specific Targeting

    /// <summary>
    /// Finds the best target for Deployment Tactics (has Galvanize shield to spread).
    /// </summary>
    public IBattleChara? FindDeploymentTarget(IPlayerCharacter player)
    {
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > SCHActions.DeploymentTactics.RangeSquared)
                continue;

            // Target must have Galvanize
            if (_statusHelper.HasGalvanize(member))
            {
                // Prefer target with Catalyze (crit shield) as it provides more value
                // But Catalyze cannot be spread, so any Galvanize target is valid
                return member;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the best target for Excogitation (tank or lowest HP member).
    /// </summary>
    public IBattleChara? FindExcogitationTarget(IPlayerCharacter player)
    {
        // Priority 1: Tank without Excog
        var tank = FindTankInParty(player);
        if (tank != null && !_statusHelper.HasExcogitation(tank))
        {
            var hpPercent = GetHpPercent(tank);
            if (hpPercent < 0.9f) // Only apply if tank has taken some damage
                return tank;
        }

        // Priority 2: Lowest HP member without Excog
        IBattleChara? lowestMember = null;
        float lowestHp = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > SCHActions.Excogitation.RangeSquared)
                continue;
            if (_statusHelper.HasExcogitation(member))
                continue;

            var hpPercent = GetHpPercent(member);
            if (hpPercent < lowestHp && hpPercent < 0.7f) // Only consider if below 70%
            {
                lowestHp = hpPercent;
                lowestMember = member;
            }
        }

        return lowestMember ?? tank; // Fall back to tank even at high HP if no injured members
    }

    /// <summary>
    /// Finds the best target for Fey Union (sustained single-target healing).
    /// </summary>
    public IBattleChara? FindFeyUnionTarget(IPlayerCharacter player, float hpThreshold = 0.7f)
    {
        // Priority 1: Tank taking heavy damage
        var tank = FindTankInParty(player);
        if (tank != null && !_statusHelper.HasFeyUnion(tank))
        {
            var hpPercent = GetHpPercent(tank);
            if (hpPercent < hpThreshold)
                return tank;
        }

        // Priority 2: Any member below threshold
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (_statusHelper.HasFeyUnion(member))
                continue;

            var hpPercent = GetHpPercent(member);
            if (hpPercent < hpThreshold)
                return member;
        }

        return null;
    }

    #endregion
}
