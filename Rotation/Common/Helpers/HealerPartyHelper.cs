using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Data;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Base class for healer party helpers.
/// Extends BasePartyHelper with HP prediction integration and common healing target logic.
/// </summary>
public abstract class HealerPartyHelper : BasePartyHelper
{
    protected readonly HpPredictionService HpPredictionService;
    protected readonly Configuration Configuration;

    /// <summary>
    /// Raise status ID used to check for pending resurrections.
    /// </summary>
    protected const ushort RaiseStatusId = 148;

    protected HealerPartyHelper(
        IObjectTable objectTable,
        IPartyList partyList,
        HpPredictionService hpPredictionService,
        Configuration configuration)
        : base(objectTable, partyList)
    {
        HpPredictionService = hpPredictionService;
        Configuration = configuration;
    }

    #region HP Prediction

    /// <summary>
    /// Gets predicted HP percent for a target using HP prediction service.
    /// </summary>
    public float GetHpPercent(IBattleChara target)
    {
        return HpPredictionService.GetPredictedHpPercent(target.EntityId, target.CurrentHp, target.MaxHp);
    }

    /// <summary>
    /// Gets predicted HP value for a target.
    /// </summary>
    public uint GetPredictedHp(IBattleChara target)
    {
        return HpPredictionService.GetPredictedHp(target.EntityId, target.CurrentHp, target.MaxHp);
    }

    #endregion

    #region Heal Target Finding

    /// <summary>
    /// Finds the lowest HP party member that needs healing.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="rangeSquared">Maximum range squared for healing.</param>
    /// <param name="healAmount">Optional heal amount to check for overheal prevention.</param>
    /// <returns>The lowest HP party member or null if none need healing.</returns>
    public IBattleChara? FindLowestHpPartyMember(IPlayerCharacter player, float rangeSquared, int healAmount = 0)
    {
        IBattleChara? lowestHpMember = null;
        float lowestHpPercent = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > rangeSquared)
                continue;

            var predictedHp = GetPredictedHp(member);
            var hpPercent = (float)predictedHp / member.MaxHp;

            if (predictedHp >= member.MaxHp)
                continue;

            // Overheal prevention
            if (healAmount > 0)
            {
                var missingHp = member.MaxHp - predictedHp;
                if (healAmount > missingHp)
                    continue;
            }

            if (hpPercent < lowestHpPercent)
            {
                lowestHpPercent = hpPercent;
                lowestHpMember = member;
            }
        }

        return lowestHpMember;
    }

    /// <summary>
    /// Finds a dead party member that needs resurrection.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="rangeSquared">Maximum range squared for resurrection.</param>
    /// <returns>A dead party member without raise pending, or null.</returns>
    public IBattleChara? FindDeadPartyMemberNeedingRaise(IPlayerCharacter player, float rangeSquared)
    {
        foreach (var member in GetAllPartyMembers(player, includeDead: true))
        {
            if (member.EntityId == player.EntityId)
                continue;
            if (!member.IsDead)
                continue;

            // Skip if already has Raise pending
            if (HasRaiseStatus(member))
                continue;

            if (Vector3.DistanceSquared(player.Position, member.Position) > rangeSquared)
                continue;

            return member;
        }

        return null;
    }

    /// <summary>
    /// Checks if a target has Raise status pending.
    /// </summary>
    protected static bool HasRaiseStatus(IBattleChara chara)
    {
        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == RaiseStatusId)
                return true;
        }
        return false;
    }

    #endregion

    #region Party Health Metrics

    /// <summary>
    /// Calculates party health metrics.
    /// </summary>
    public (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics(IPlayerCharacter player)
    {
        float totalHpPercent = 0;
        float lowestHp = 1f;
        int count = 0;
        int injured = 0;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;

            var hpPct = member.MaxHp > 0 ? (float)member.CurrentHp / member.MaxHp : 1f;
            totalHpPercent += hpPct;
            count++;

            if (hpPct < lowestHp)
                lowestHp = hpPct;

            if (hpPct < FFXIVConstants.InjuredHpThreshold)
                injured++;
        }

        return (count > 0 ? totalHpPercent / count : 1f, lowestHp, injured);
    }

    /// <summary>
    /// Counts party members needing AoE heal within a radius.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="radiusSquared">AoE heal radius squared.</param>
    /// <param name="healAmount">The heal amount to consider for overheal prevention.</param>
    /// <returns>Count and list of target entity IDs.</returns>
    public (int count, List<uint> targetIds) CountPartyMembersNeedingAoEHeal(
        IPlayerCharacter player,
        float radiusSquared,
        int healAmount)
    {
        int count = 0;
        var targetIds = new List<uint>();

        foreach (var member in GetAllPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) > radiusSquared)
                continue;
            if (member.IsDead)
                continue;

            var predictedHp = GetPredictedHp(member);
            var missingHp = member.MaxHp - predictedHp;

            if (healAmount <= missingHp)
            {
                count++;
                targetIds.Add(member.EntityId);
            }
        }

        return (count, targetIds);
    }

    #endregion
}
