using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.ApolloCore.Helpers;

/// <summary>
/// Helper class for party member operations.
/// </summary>
public sealed class PartyHelper : IPartyHelper
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;
    private readonly HpPredictionService _hpPredictionService;

    // Tank ClassJob IDs (PLD, WAR, DRK, GNB + base classes GLA, MRD)
    private static readonly uint[] TankJobIds = { 19, 21, 32, 37, 1, 3 };

    public PartyHelper(
        IObjectTable objectTable,
        IPartyList partyList,
        HpPredictionService hpPredictionService)
    {
        _objectTable = objectTable;
        _partyList = partyList;
        _hpPredictionService = hpPredictionService;
    }

    /// <summary>
    /// Yields all party members (player + party list or Trust NPCs).
    /// Iterates objectTable directly for guaranteed fresh HP data.
    /// </summary>
    public IEnumerable<IBattleChara> GetAllPartyMembers(IPlayerCharacter player, bool includeDead = false)
    {
        yield return player;

        if (_partyList.Length > 0)
        {
            // Build set of party member entity IDs for fast lookup
            var partyEntityIds = new HashSet<uint>();
            foreach (var partyMember in _partyList)
            {
                if (partyMember.EntityId != player.EntityId)
                    partyEntityIds.Add(partyMember.EntityId);
            }

            // Iterate objectTable directly for fresh data
            foreach (var obj in _objectTable)
            {
                if (obj is IBattleChara chara && partyEntityIds.Contains(obj.EntityId))
                {
                    if (includeDead || !chara.IsDead)
                        yield return chara;
                }
            }
        }
        else
        {
            // Trust NPC handling
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
            var jobId = pc.ClassJob.RowId;
            foreach (var tankId in TankJobIds)
            {
                if (jobId == tankId)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Finds the tank in the party.
    /// First checks for player tanks by ClassJob, then falls back to
    /// finding the party member that enemies are targeting (for Trust NPCs).
    /// </summary>
    public IBattleChara? FindTankInParty(IPlayerCharacter player)
    {
        IBattleChara? effectiveTank = null;

        // First pass: Look for player tanks by ClassJob
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.EntityId == player.EntityId)
                continue;
            if (member.IsDead)
                continue;
            if (IsTankRole(member))
                return member;
        }

        // Second pass: For Trust NPCs, find who enemies are targeting
        foreach (var obj in _objectTable)
        {
            if (obj is not IBattleNpc enemy)
                continue;
            if (enemy.TargetObjectId == 0 || enemy.TargetObjectId == 0xE0000000)
                continue;

            foreach (var member in GetAllPartyMembers(player))
            {
                if (member.EntityId == player.EntityId)
                    continue;
                if (member.IsDead)
                    continue;
                if (member.GameObjectId == enemy.TargetObjectId)
                {
                    effectiveTank = member;
                    break;
                }
            }

            if (effectiveTank != null)
                break;
        }

        return effectiveTank;
    }

    /// <summary>
    /// Finds the lowest HP party member that needs healing.
    /// Uses predicted HP to account for pending heals.
    /// </summary>
    public IBattleChara? FindLowestHpPartyMember(IPlayerCharacter player, int healAmount = 0)
    {
        IBattleChara? lowestHpMember = null;
        float lowestHpPercent = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            CheckMemberHp(member, player.Position, healAmount, ref lowestHpMember, ref lowestHpPercent);
        }

        return lowestHpMember;
    }

    private void CheckMemberHp(IBattleChara chara, Vector3 playerPos, int healAmount,
        ref IBattleChara? lowestHpMember, ref float lowestHpPercent)
    {
        if (chara.IsDead)
            return;
        if (Vector3.DistanceSquared(playerPos, chara.Position) > WHMActions.Cure.Range * WHMActions.Cure.Range)
            return;

        var predictedHp = _hpPredictionService.GetPredictedHp(chara.EntityId, chara.CurrentHp, chara.MaxHp);
        var hpPercent = (float)predictedHp / chara.MaxHp;

        if (predictedHp >= chara.MaxHp)
            return;

        if (healAmount > 0)
        {
            var missingHp = chara.MaxHp - predictedHp;
            if (healAmount > missingHp)
                return;
        }

        if (hpPercent < lowestHpPercent)
        {
            lowestHpPercent = hpPercent;
            lowestHpMember = chara;
        }
    }

    /// <summary>
    /// Finds a dead party member that needs resurrection.
    /// Skips members who already have the "Raise" status (pending res).
    /// </summary>
    public IBattleChara? FindDeadPartyMemberNeedingRaise(IPlayerCharacter player)
    {
        foreach (var member in GetAllPartyMembers(player, includeDead: true))
        {
            if (member.EntityId == player.EntityId)
                continue;
            if (!member.IsDead)
                continue;
            if (StatusHelper.HasStatus(member, StatusHelper.StatusIds.Raise))
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > WHMActions.Raise.Range * WHMActions.Raise.Range)
                continue;

            return member;
        }

        return null;
    }

    /// <summary>
    /// Gets predicted HP percent for a target (shadow HP + pending heals).
    /// </summary>
    public float GetHpPercent(IBattleChara target)
    {
        return _hpPredictionService.GetPredictedHpPercent(target.EntityId, target.CurrentHp, target.MaxHp);
    }

    /// <summary>
    /// Calculates party health metrics for defensive cooldown decisions.
    /// Uses RAW game HP (not predicted) for immediate, accurate readings.
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
    /// Finds the best target for Cure III (party member with most injured allies within 10y radius).
    /// </summary>
    public (IBattleChara? target, int count, List<uint> targetIds) FindBestCureIIITarget(
        IPlayerCharacter player, int healAmount)
    {
        if (player.Level < WHMActions.CureIII.MinLevel)
            return (null, 0, new List<uint>());

        IBattleChara? bestTarget = null;
        int bestCount = 0;
        var bestTargetIds = new List<uint>();

        foreach (var potentialCenter in GetAllPartyMembers(player))
        {
            if (potentialCenter.IsDead)
                continue;

            if (Vector3.DistanceSquared(player.Position, potentialCenter.Position) >
                WHMActions.CureIII.Range * WHMActions.CureIII.Range)
                continue;

            int countNearby = 0;
            var nearbyTargetIds = new List<uint>();

            foreach (var member in GetAllPartyMembers(player))
            {
                if (member.IsDead)
                    continue;

                if (Vector3.DistanceSquared(potentialCenter.Position, member.Position) >
                    WHMActions.CureIII.Radius * WHMActions.CureIII.Radius)
                    continue;

                var predictedHp = _hpPredictionService.GetPredictedHp(member.EntityId, member.CurrentHp, member.MaxHp);
                var missingHp = member.MaxHp - predictedHp;

                if (healAmount <= missingHp)
                {
                    countNearby++;
                    nearbyTargetIds.Add(member.EntityId);
                }
            }

            if (countNearby > bestCount)
            {
                bestCount = countNearby;
                bestTarget = potentialCenter;
                bestTargetIds = nearbyTargetIds;
            }
        }

        return (bestTarget, bestCount, bestTargetIds);
    }

    /// <summary>
    /// Counts party members needing AoE heal and returns all targets in range.
    /// </summary>
    public (int count, bool anyHaveRegen, List<(uint entityId, string name)> allTargets, int averageMissingHp)
        CountPartyMembersNeedingAoEHeal(IPlayerCharacter player, int healAmount)
    {
        int count = 0;
        bool anyHaveRegen = false;
        var allTargets = new List<(uint entityId, string name)>();
        long totalMissingHp = 0;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) >
                WHMActions.Medica.Radius * WHMActions.Medica.Radius)
                continue;
            if (member.IsDead)
                continue;

            allTargets.Add((member.EntityId, member.Name.TextValue));

            if (CheckMemberNeedsAoEHeal(member, healAmount, out var memberHasRegen, out var missingHp))
            {
                count++;
                totalMissingHp += missingHp;
                anyHaveRegen |= memberHasRegen;
            }
        }

        var averageMissingHp = count > 0 ? (int)(totalMissingHp / count) : 0;
        return (count, anyHaveRegen, allTargets, averageMissingHp);
    }

    private bool CheckMemberNeedsAoEHeal(IBattleChara chara, int healAmount, out bool hasRegen, out int missingHp)
    {
        hasRegen = false;
        missingHp = 0;

        if (chara.IsDead)
            return false;

        hasRegen = StatusHelper.HasMedicaRegen(chara);

        var predictedHp = _hpPredictionService.GetPredictedHp(chara.EntityId, chara.CurrentHp, chara.MaxHp);
        missingHp = (int)(chara.MaxHp - predictedHp);

        return healAmount <= missingHp;
    }

    /// <summary>
    /// Finds the best target for Regen with tank priority.
    /// </summary>
    public IBattleChara? FindRegenTarget(IPlayerCharacter player, float regenHpThreshold, float regenRefreshThreshold)
    {
        IBattleChara? tankTarget = null;
        IBattleChara? otherTarget = null;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;

            if (Vector3.DistanceSquared(player.Position, member.Position) >
                WHMActions.Regen.Range * WHMActions.Regen.Range)
                continue;

            if (!NeedsRegen(member, regenHpThreshold, regenRefreshThreshold))
                continue;

            if (IsTankRole(member))
            {
                tankTarget ??= member;
            }
            else if (otherTarget == null || GetHpPercent(member) < GetHpPercent(otherTarget))
            {
                otherTarget = member;
            }
        }

        return tankTarget ?? otherTarget;
    }

    /// <summary>
    /// Checks if a target needs Regen (below threshold and no/expiring Regen).
    /// </summary>
    public bool NeedsRegen(IBattleChara target, float hpThreshold, float refreshThreshold)
    {
        var hpPercent = GetHpPercent(target);
        if (hpPercent >= hpThreshold)
            return false;

        if (!StatusHelper.HasRegenActive(target, out var remaining))
            return true;

        return remaining < refreshThreshold;
    }
}
