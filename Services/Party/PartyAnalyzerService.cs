using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Prediction;

namespace Olympus.Services.Party;

/// <summary>
/// Shared party analysis service for all healer rotation modules.
/// Provides a unified interface for finding heal targets and assessing party health.
/// </summary>
public sealed class PartyAnalyzerService : IPartyAnalyzer
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;
    private readonly IHpPredictionService _hpPredictionService;
    private readonly IDamageIntakeService _damageIntakeService;
    private readonly IDamageTrendService? _damageTrendService;

    // Tank ClassJob IDs (PLD, WAR, DRK, GNB + base classes GLA, MRD)
    private static readonly HashSet<uint> TankJobIds = new() { 19, 21, 32, 37, 1, 3 };

    // Current player reference (updated each frame via SetPlayer)
    private IPlayerCharacter? _player;

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

    // Default heal range (30y squared = 900)
    private const float DefaultRangeSquared = 900f;

    public PartyAnalyzerService(
        IObjectTable objectTable,
        IPartyList partyList,
        IHpPredictionService hpPredictionService,
        IDamageIntakeService damageIntakeService,
        IDamageTrendService? damageTrendService = null)
    {
        _objectTable = objectTable;
        _partyList = partyList;
        _hpPredictionService = hpPredictionService;
        _damageIntakeService = damageIntakeService;
        _damageTrendService = damageTrendService;
    }

    /// <summary>
    /// Sets the current player reference. Call this each frame before using the analyzer.
    /// </summary>
    public void SetPlayer(IPlayerCharacter player)
    {
        _player = player;
    }

    /// <inheritdoc />
    public IBattleChara? FindMostEndangeredMember(int healAmount = 0)
    {
        if (_player is null)
            return null;

        var candidateCount = 0;
        float maxDamageRate = 1f;
        float maxAcceleration = 1f;

        var playerPos = _player.Position;

        foreach (var member in GetAllPartyMembers(includeDead: false))
        {
            if (candidateCount >= MaxPartySize)
                break;

            if (Vector3.DistanceSquared(playerPos, member.Position) > DefaultRangeSquared)
                continue;

            var predictedHp = _hpPredictionService.GetPredictedHp(member.EntityId, member.CurrentHp, member.MaxHp);

            if (predictedHp >= member.MaxHp)
                continue;

            if (healAmount > 0)
            {
                var missingHp = member.MaxHp - predictedHp;
                if (healAmount > missingHp)
                    continue;
            }

            var hpPercent = (float)predictedHp / member.MaxHp;
            var damageRate = _damageIntakeService.GetDamageRate(member.EntityId, 5f);

            if (damageRate > maxDamageRate)
                maxDamageRate = damageRate;

            var damageAccel = 0f;
            if (_damageTrendService is not null)
            {
                damageAccel = _damageTrendService.GetDamageAcceleration(member.EntityId, 5f);
                if (damageAccel > maxAcceleration)
                    maxAcceleration = damageAccel;
            }

            _endangeredMembers[candidateCount] = member;
            _endangeredDamageRates[candidateCount] = damageRate;
            _endangeredMissingHpPcts[candidateCount] = 1f - hpPercent;
            _endangeredTankBonuses[candidateCount] = IsTankRole(member) ? 1f : 0f;
            _endangeredDamageAccelerations[candidateCount] = damageAccel;
            candidateCount++;
        }

        if (candidateCount == 0)
            return null;

        IBattleChara? mostEndangered = null;
        float highestScore = float.MinValue;

        for (var i = 0; i < candidateCount; i++)
        {
            var normalizedDamageRate = _endangeredDamageRates[i] / maxDamageRate;
            var normalizedAcceleration = 0f;
            if (_endangeredDamageAccelerations[i] > 0 && maxAcceleration > 0)
            {
                normalizedAcceleration = _endangeredDamageAccelerations[i] / maxAcceleration;
            }

            // Weight: damageRate (35%) + tankBonus (25%) + missingHp (30%) + damageAcceleration (10%)
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

    /// <inheritdoc />
    public IBattleChara? FindLowestHpMember(int healAmount = 0)
    {
        if (_player is null)
            return null;

        IBattleChara? lowestHpMember = null;
        float lowestHpPercent = 1f;

        foreach (var member in GetAllPartyMembers(includeDead: false))
        {
            if (Vector3.DistanceSquared(_player.Position, member.Position) > DefaultRangeSquared)
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

    /// <inheritdoc />
    public IBattleChara? FindTank()
    {
        if (_player is null)
            return null;

        // First pass: Look for player tanks by ClassJob
        foreach (var member in GetAllPartyMembers(includeDead: false))
        {
            if (member.EntityId == _player.EntityId)
                continue;
            if (IsTankRole(member))
                return member;
        }

        // Second pass: For Trust NPCs, find who enemies are targeting
        IBattleChara? effectiveTank = null;
        foreach (var obj in _objectTable)
        {
            if (obj is not IBattleNpc enemy)
                continue;
            if (enemy.TargetObjectId == 0 || enemy.TargetObjectId == 0xE0000000)
                continue;

            foreach (var member in GetAllPartyMembers(includeDead: false))
            {
                if (member.EntityId == _player.EntityId)
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

    /// <inheritdoc />
    public IBattleChara? FindDeadMemberNeedingRaise()
    {
        if (_player is null)
            return null;

        foreach (var member in GetAllPartyMembers(includeDead: true))
        {
            if (member.EntityId == _player.EntityId)
                continue;
            if (!member.IsDead)
                continue;
            if (StatusHelper.HasStatus(member, StatusHelper.StatusIds.Raise))
                continue;
            if (Vector3.DistanceSquared(_player.Position, member.Position) > DefaultRangeSquared)
                continue;

            return member;
        }

        return null;
    }

    /// <inheritdoc />
    public PartyHealthMetrics GetHealthMetrics()
    {
        if (_player is null)
            return new PartyHealthMetrics(1f, 1f, 0, 0);

        float totalHpPercent = 0;
        float lowestHp = 1f;
        int count = 0;
        int injured = 0;
        int critical = 0;

        foreach (var member in GetAllPartyMembers(includeDead: false))
        {
            var hpPct = member.MaxHp > 0 ? (float)member.CurrentHp / member.MaxHp : 1f;
            totalHpPercent += hpPct;
            count++;

            if (hpPct < lowestHp)
                lowestHp = hpPct;

            if (hpPct < FFXIVConstants.InjuredHpThreshold)
                injured++;

            if (hpPct < FFXIVConstants.CriticalHpThreshold)
                critical++;
        }

        return new PartyHealthMetrics(
            count > 0 ? totalHpPercent / count : 1f,
            lowestHp,
            injured,
            critical
        );
    }

    /// <inheritdoc />
    public int CountMembersNeedingAoEHeal(int healAmount)
    {
        if (_player is null)
            return 0;

        int count = 0;
        const float aoERadiusSquared = 225f; // 15y squared

        foreach (var member in GetAllPartyMembers(includeDead: false))
        {
            if (Vector3.DistanceSquared(_player.Position, member.Position) > aoERadiusSquared)
                continue;

            var predictedHp = _hpPredictionService.GetPredictedHp(member.EntityId, member.CurrentHp, member.MaxHp);
            var missingHp = member.MaxHp - predictedHp;

            if (healAmount <= missingHp)
                count++;
        }

        return count;
    }

    /// <inheritdoc />
    public IBattleChara? FindBestAoEHealTarget(float healRadius, int healAmount)
    {
        if (_player is null)
            return null;

        var radiusSquared = healRadius * healRadius;
        IBattleChara? bestTarget = null;
        int bestCount = 0;

        // Collect all members needing healing
        var members = new List<IBattleChara>();
        foreach (var member in GetAllPartyMembers(includeDead: false))
        {
            if (Vector3.DistanceSquared(_player.Position, member.Position) > DefaultRangeSquared)
                continue;

            var predictedHp = _hpPredictionService.GetPredictedHp(member.EntityId, member.CurrentHp, member.MaxHp);
            var missingHp = member.MaxHp - predictedHp;

            if (healAmount <= missingHp)
                members.Add(member);
        }

        if (members.Count == 0)
            return null;

        // Evaluate each member as a potential center
        foreach (var center in members)
        {
            var countNearby = 0;
            foreach (var other in members)
            {
                if (Vector3.DistanceSquared(center.Position, other.Position) <= radiusSquared)
                    countNearby++;
            }

            if (countNearby > bestCount)
            {
                bestCount = countNearby;
                bestTarget = center;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Yields all party members (player + party list or Trust NPCs).
    /// </summary>
    private IEnumerable<IBattleChara> GetAllPartyMembers(bool includeDead = false)
    {
        if (_player is null)
            yield break;

        yield return _player;

        if (_partyList.Length > 0)
        {
            // Rebuild cache only if party composition changed
            if (_partyList.Length != _lastPartyCount || _player.EntityId != _lastPlayerEntityId)
            {
                _cachedPartyEntityIds.Clear();
                foreach (var partyMember in _partyList)
                {
                    if (partyMember.EntityId != _player.EntityId)
                        _cachedPartyEntityIds.Add(partyMember.EntityId);
                }
                _lastPartyCount = _partyList.Length;
                _lastPlayerEntityId = _player.EntityId;
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
    private static bool IsValidTrustNpc(IGameObject obj, out IBattleNpc? npc, bool includeDead = false)
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
    private static bool IsTankRole(IBattleChara chara)
    {
        if (chara is IPlayerCharacter pc)
        {
            return TankJobIds.Contains(pc.ClassJob.RowId);
        }
        return false;
    }
}
