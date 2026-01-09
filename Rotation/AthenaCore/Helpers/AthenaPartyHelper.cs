using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Data;
using Olympus.Services.Party;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.AthenaCore.Helpers;

/// <summary>
/// Helper class for party member operations specific to Scholar.
/// Extends base party helper functionality with shield-aware logic.
/// </summary>
public sealed class AthenaPartyHelper
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;
    private readonly HpPredictionService _hpPredictionService;
    private readonly Configuration _configuration;
    private readonly AthenaStatusHelper _statusHelper;

    // Tank ClassJob IDs (PLD, WAR, DRK, GNB + base classes GLA, MRD)
    private static readonly HashSet<uint> TankJobIds = new() { 19, 21, 32, 37, 1, 3 };

    // Party member caching
    private readonly HashSet<uint> _cachedPartyEntityIds = new(8);
    private int _lastPartyCount = -1;
    private uint _lastPlayerEntityId;

    private const int MaxPartySize = 8;

    public AthenaPartyHelper(
        IObjectTable objectTable,
        IPartyList partyList,
        HpPredictionService hpPredictionService,
        Configuration configuration,
        AthenaStatusHelper statusHelper)
    {
        _objectTable = objectTable;
        _partyList = partyList;
        _hpPredictionService = hpPredictionService;
        _configuration = configuration;
        _statusHelper = statusHelper;
    }

    /// <summary>
    /// Yields all party members (player + party list or Trust NPCs).
    /// </summary>
    public IEnumerable<IBattleChara> GetAllPartyMembers(IPlayerCharacter player, bool includeDead = false)
    {
        yield return player;

        if (_partyList.Length > 0)
        {
            if (_partyList.Length != _lastPartyCount || player.EntityId != _lastPlayerEntityId)
            {
                _cachedPartyEntityIds.Clear();
                foreach (var partyMember in _partyList)
                {
                    if (partyMember.EntityId != player.EntityId)
                        _cachedPartyEntityIds.Add(partyMember.EntityId);
                }
                _lastPartyCount = _partyList.Length;
                _lastPlayerEntityId = player.EntityId;
            }

            foreach (var obj in _objectTable)
            {
                if (obj is IBattleChara chara && _cachedPartyEntityIds.Contains(obj.EntityId))
                {
                    if (includeDead || !chara.IsDead)
                        yield return chara;
                }
            }
        }
        else
        {
            foreach (var obj in _objectTable)
            {
                if (IsValidTrustNpc(obj, out var npc, includeDead))
                    yield return npc!;
            }
        }
    }

    /// <summary>
    /// Checks if an object is a valid Trust NPC party member.
    /// </summary>
    public static bool IsValidTrustNpc(IGameObject obj, out IBattleNpc? npc, bool includeDead = false)
    {
        npc = null;
        if (obj.ObjectKind != ObjectKind.BattleNpc)
            return false;
        if (obj is not IBattleNpc battleNpc)
            return false;
        if (!includeDead && battleNpc.CurrentHp == 0)
            return false;
        if (battleNpc.MaxHp == 0)
            return false;
        if ((battleNpc.StatusFlags & (StatusFlags)FFXIVConstants.HostileStatusFlag) != 0)
            return false;
        if (battleNpc.SubKind != FFXIVConstants.TrustNpcSubKind)
            return false;

        npc = battleNpc;
        return true;
    }

    /// <summary>
    /// Checks if a character is a tank role.
    /// </summary>
    public static bool IsTankRole(IBattleChara chara)
    {
        if (chara is IPlayerCharacter pc)
        {
            return TankJobIds.Contains(pc.ClassJob.RowId);
        }
        return false;
    }

    /// <summary>
    /// Finds the tank in the party.
    /// </summary>
    public IBattleChara? FindTankInParty(IPlayerCharacter player)
    {
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.EntityId == player.EntityId)
                continue;
            if (member.IsDead)
                continue;
            if (IsTankRole(member))
                return member;
        }
        return null;
    }

    /// <summary>
    /// Finds the lowest HP party member that needs healing.
    /// </summary>
    public IBattleChara? FindLowestHpPartyMember(IPlayerCharacter player, int healAmount = 0)
    {
        IBattleChara? lowestHpMember = null;
        float lowestHpPercent = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > SCHActions.Physick.RangeSquared)
                continue;

            var predictedHp = _hpPredictionService.GetPredictedHp(member.EntityId, member.CurrentHp, member.MaxHp);
            var hpPercent = (float)predictedHp / member.MaxHp;

            if (predictedHp >= member.MaxHp)
                continue;

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
    public IBattleChara? FindDeadPartyMemberNeedingRaise(IPlayerCharacter player)
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

            if (Vector3.DistanceSquared(player.Position, member.Position) > SCHActions.Resurrection.RangeSquared)
                continue;

            return member;
        }

        return null;
    }

    /// <summary>
    /// Checks if a target has Raise status pending.
    /// </summary>
    private static bool HasRaiseStatus(IBattleChara chara)
    {
        const ushort RaiseStatusId = 148;
        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == RaiseStatusId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets predicted HP percent for a target.
    /// </summary>
    public float GetHpPercent(IBattleChara target)
    {
        return _hpPredictionService.GetPredictedHpPercent(target.EntityId, target.CurrentHp, target.MaxHp);
    }

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
    /// Counts party members needing AoE heal (for Succor/Indomitability).
    /// </summary>
    public (int count, List<uint> targetIds) CountPartyMembersNeedingAoEHeal(IPlayerCharacter player, int healAmount)
    {
        int count = 0;
        var targetIds = new List<uint>();

        foreach (var member in GetAllPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) > SCHActions.Succor.RadiusSquared)
                continue;
            if (member.IsDead)
                continue;

            var predictedHp = _hpPredictionService.GetPredictedHp(member.EntityId, member.CurrentHp, member.MaxHp);
            var missingHp = member.MaxHp - predictedHp;

            if (healAmount <= missingHp)
            {
                count++;
                targetIds.Add(member.EntityId);
            }
        }

        return (count, targetIds);
    }

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

    /// <summary>
    /// Returns all party members (excluding dead) for iteration.
    /// </summary>
    public IEnumerable<IBattleChara> GetPartyMembers(IPlayerCharacter player)
    {
        return GetAllPartyMembers(player, includeDead: false);
    }
}
