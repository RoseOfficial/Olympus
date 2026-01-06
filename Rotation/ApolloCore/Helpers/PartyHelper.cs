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
    private static readonly HashSet<uint> TankJobIds = new() { 19, 21, 32, 37, 1, 3 };

    // Party member caching to avoid HashSet allocation every frame
    private readonly HashSet<uint> _cachedPartyEntityIds = new(8);
    private int _lastPartyCount = -1;
    private uint _lastPlayerEntityId;

    // Pre-allocated arrays for endangered member triage (avoids per-frame allocation)
    private const int MaxPartySize = 8;
    private readonly IBattleChara?[] _endangeredMembers = new IBattleChara?[MaxPartySize];
    private readonly float[] _endangeredDamageRates = new float[MaxPartySize];
    private readonly float[] _endangeredMissingHpPcts = new float[MaxPartySize];
    private readonly float[] _endangeredTankBonuses = new float[MaxPartySize];
    private readonly float[] _endangeredDamageAccelerations = new float[MaxPartySize];

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
            // Rebuild cache only if party composition changed
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

            // Iterate objectTable directly for fresh HP data, using cached IDs
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
            return TankJobIds.Contains(pc.ClassJob.RowId);
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
        if (Vector3.DistanceSquared(playerPos, chara.Position) > WHMActions.Cure.RangeSquared)
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
            if (Vector3.DistanceSquared(player.Position, member.Position) > WHMActions.Raise.RangeSquared)
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
    /// Optimized to pre-filter injured members and use pre-computed squared distances.
    /// </summary>
    public (IBattleChara? target, int count, List<uint> targetIds) FindBestCureIIITarget(
        IPlayerCharacter player, int healAmount)
    {
        if (player.Level < WHMActions.CureIII.MinLevel)
            return (null, 0, _emptyTargetIds);

        // Pre-compute squared distances
        var rangeSquared = WHMActions.CureIII.RangeSquared;
        var radiusSquared = WHMActions.CureIII.RadiusSquared;
        var playerPos = player.Position;

        // First pass: collect all injured members within cast range with their positions
        var injuredCount = 0;
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;

            if (Vector3.DistanceSquared(playerPos, member.Position) > rangeSquared)
                continue;

            var predictedHp = _hpPredictionService.GetPredictedHp(member.EntityId, member.CurrentHp, member.MaxHp);
            var missingHp = member.MaxHp - predictedHp;

            // Cache member data for potential center evaluation
            _cureIIIMembers[injuredCount] = member;
            _cureIIIPositions[injuredCount] = member.Position;
            _cureIIINeedsHeal[injuredCount] = healAmount <= missingHp;
            injuredCount++;

            if (injuredCount >= MaxPartySize)
                break;
        }

        if (injuredCount == 0)
            return (null, 0, _emptyTargetIds);

        // Count how many members actually need healing
        var totalNeedingHeal = 0;
        for (var i = 0; i < injuredCount; i++)
        {
            if (_cureIIINeedsHeal[i])
                totalNeedingHeal++;
        }

        // No one needs healing
        if (totalNeedingHeal == 0)
            return (null, 0, _emptyTargetIds);

        IBattleChara? bestTarget = null;
        int bestCount = 0;

        // Evaluate each member as a potential center
        for (var centerIdx = 0; centerIdx < injuredCount; centerIdx++)
        {
            var centerPos = _cureIIIPositions[centerIdx];
            var countNearby = 0;

            // Count nearby members that need healing
            for (var memberIdx = 0; memberIdx < injuredCount; memberIdx++)
            {
                if (!_cureIIINeedsHeal[memberIdx])
                    continue;

                if (Vector3.DistanceSquared(centerPos, _cureIIIPositions[memberIdx]) <= radiusSquared)
                {
                    countNearby++;
                }
            }

            // Early termination: if all needing-heal members are in range, this is optimal
            if (countNearby == totalNeedingHeal)
            {
                bestTarget = _cureIIIMembers[centerIdx];
                bestCount = countNearby;
                break;
            }

            if (countNearby > bestCount)
            {
                bestCount = countNearby;
                bestTarget = _cureIIIMembers[centerIdx];
            }
        }

        // Build target ID list for the best target (only when we have a result)
        var bestTargetIds = new List<uint>(bestCount);
        if (bestTarget is not null && bestCount > 0)
        {
            var bestPos = bestTarget.Position;
            for (var i = 0; i < injuredCount; i++)
            {
                if (_cureIIINeedsHeal[i] && Vector3.DistanceSquared(bestPos, _cureIIIPositions[i]) <= radiusSquared)
                {
                    bestTargetIds.Add(_cureIIIMembers[i]!.EntityId);
                }
            }
        }

        return (bestTarget, bestCount, bestTargetIds);
    }

    // Shared empty list for early returns (avoids allocation)
    private static readonly List<uint> _emptyTargetIds = new();

    // Pre-allocated arrays for Cure III target evaluation
    private readonly IBattleChara?[] _cureIIIMembers = new IBattleChara?[MaxPartySize];
    private readonly Vector3[] _cureIIIPositions = new Vector3[MaxPartySize];
    private readonly bool[] _cureIIINeedsHeal = new bool[MaxPartySize];

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
                WHMActions.Medica.RadiusSquared)
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
                WHMActions.Regen.RangeSquared)
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

    /// <summary>
    /// Finds the most endangered party member using damage intake triage.
    /// Weights: damageRate (35%) + tankBonus (25%) + missingHp (30%) + damageAcceleration (10%).
    /// Optimized single-pass algorithm with deferred normalization.
    /// </summary>
    public IBattleChara? FindMostEndangeredPartyMember(
        IPlayerCharacter player,
        IDamageIntakeService damageIntakeService,
        int healAmount = 0,
        IDamageTrendService? damageTrendService = null)
    {
        // Use instance-level arrays to avoid allocation (max 8 party members in FFXIV)
        var candidateCount = 0;
        float maxDamageRate = 1f; // Minimum 1 to avoid division by zero
        float maxAcceleration = 1f; // For normalization

        var playerPos = player.Position;
        var rangeSquared = WHMActions.Cure.RangeSquared;

        // Single pass: collect candidates and track max damage rate
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;

            // Early exit if we have max candidates
            if (candidateCount >= MaxPartySize)
                break;

            // Range check using pre-computed squared value
            if (Vector3.DistanceSquared(playerPos, member.Position) > rangeSquared)
                continue;

            var predictedHp = _hpPredictionService.GetPredictedHp(member.EntityId, member.CurrentHp, member.MaxHp);

            // Skip if already at full HP
            if (predictedHp >= member.MaxHp)
                continue;

            // Skip if heal would overheal too much
            if (healAmount > 0)
            {
                var missingHp = member.MaxHp - predictedHp;
                if (healAmount > missingHp)
                    continue;
            }

            var hpPercent = (float)predictedHp / member.MaxHp;
            var damageRate = damageIntakeService.GetDamageRate(member.EntityId, 5f);

            // Track max damage rate for normalization
            if (damageRate > maxDamageRate)
                maxDamageRate = damageRate;

            // Get damage acceleration if service is available
            // Positive acceleration = damage increasing (HP dropping faster)
            var damageAccel = 0f;
            if (damageTrendService is not null)
            {
                damageAccel = damageTrendService.GetDamageAcceleration(member.EntityId, 5f);
                // Only track positive acceleration (increasing damage) for max calculation
                if (damageAccel > maxAcceleration)
                    maxAcceleration = damageAccel;
            }

            // Store candidate data in pre-allocated arrays
            _endangeredMembers[candidateCount] = member;
            _endangeredDamageRates[candidateCount] = damageRate;
            _endangeredMissingHpPcts[candidateCount] = 1f - hpPercent;
            _endangeredTankBonuses[candidateCount] = IsTankRole(member) ? 1f : 0f;
            _endangeredDamageAccelerations[candidateCount] = damageAccel;
            candidateCount++;
        }

        if (candidateCount == 0)
            return null;

        // Score candidates with actual max damage rate (no second iteration through game objects)
        IBattleChara? mostEndangered = null;
        float highestScore = float.MinValue;

        for (var i = 0; i < candidateCount; i++)
        {
            var normalizedDamageRate = _endangeredDamageRates[i] / maxDamageRate;

            // Normalize acceleration (only positive values contribute)
            // Negative acceleration (damage decreasing) gets 0 contribution
            var normalizedAcceleration = 0f;
            if (_endangeredDamageAccelerations[i] > 0 && maxAcceleration > 0)
            {
                normalizedAcceleration = _endangeredDamageAccelerations[i] / maxAcceleration;
            }

            // Weight: damageRate (35%) + tankBonus (25%) + missingHp (30%) + damageAcceleration (10%)
            // Acceleration bonus rewards targets whose damage intake is increasing (HP dropping faster)
            var score = (normalizedDamageRate * 0.35f) +
                        (_endangeredTankBonuses[i] * 0.25f) +
                        (_endangeredMissingHpPcts[i] * 0.30f) +
                        (normalizedAcceleration * 0.10f);

            if (score > highestScore)
            {
                highestScore = score;
                mostEndangered = _endangeredMembers[i];
            }
        }

        return mostEndangered;
    }
}
