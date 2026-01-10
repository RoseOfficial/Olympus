using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Party;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.ApolloCore.Helpers;

/// <summary>
/// White Mage party helper with WHM-specific targeting logic.
/// Extends HealerPartyHelper with Cure III, Regen, and triage functionality.
/// </summary>
public sealed class PartyHelper : HealerPartyHelper, IPartyHelper
{
    // Pre-allocated arrays for endangered member triage (avoids per-frame allocation)
    private readonly IBattleChara?[] _endangeredMembers = new IBattleChara?[MaxPartySize];
    private readonly float[] _endangeredDamageRates = new float[MaxPartySize];
    private readonly float[] _endangeredMissingHpPcts = new float[MaxPartySize];
    private readonly float[] _endangeredTankBonuses = new float[MaxPartySize];
    private readonly float[] _endangeredDamageAccelerations = new float[MaxPartySize];

    // Enhanced triage factors (v1.11.0)
    private readonly float[] _endangeredShieldPcts = new float[MaxPartySize];
    private readonly float[] _endangeredMitigations = new float[MaxPartySize];
    private readonly float[] _endangeredHealerBonuses = new float[MaxPartySize];
    private readonly float[] _endangeredTtdScores = new float[MaxPartySize];

    // Pre-allocated arrays for Cure III target evaluation
    private readonly IBattleChara?[] _cureIIIMembers = new IBattleChara?[MaxPartySize];
    private readonly Vector3[] _cureIIIPositions = new Vector3[MaxPartySize];
    private readonly bool[] _cureIIINeedsHeal = new bool[MaxPartySize];

    // Shared empty list for early returns (avoids allocation)
    private static readonly List<uint> _emptyTargetIds = new();

    public PartyHelper(
        IObjectTable objectTable,
        IPartyList partyList,
        HpPredictionService hpPredictionService,
        Configuration configuration)
        : base(objectTable, partyList, hpPredictionService, configuration)
    {
    }

    #region IPartyHelper Implementation

    /// <inheritdoc />
    public IBattleChara? FindLowestHpPartyMember(IPlayerCharacter player, int healAmount = 0)
    {
        return FindLowestHpPartyMember(player, WHMActions.Cure.RangeSquared, healAmount);
    }

    /// <inheritdoc />
    public IBattleChara? FindDeadPartyMemberNeedingRaise(IPlayerCharacter player)
    {
        return FindDeadPartyMemberNeedingRaise(player, WHMActions.Raise.RangeSquared);
    }

    #endregion

    #region WHM-Specific Tank Finding

    /// <summary>
    /// Finds the tank in the party.
    /// First checks for player tanks by ClassJob, then falls back to
    /// finding the party member that enemies are targeting (for Trust NPCs).
    /// </summary>
    public override IBattleChara? FindTankInParty(IPlayerCharacter player)
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
        foreach (var obj in ObjectTable)
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

    #endregion

    #region Cure III Targeting

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

            var predictedHp = GetPredictedHp(member);
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

    #endregion

    #region AoE Heal Counting

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

        var predictedHp = GetPredictedHp(chara);
        missingHp = (int)(chara.MaxHp - predictedHp);

        return healAmount <= missingHp;
    }

    #endregion

    #region Regen Targeting

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

    #endregion

    #region Endangered Member Triage

    /// <summary>
    /// Finds the most endangered party member using enhanced damage intake triage.
    /// Uses configurable weights including damage rate, tank bonus, missing HP,
    /// damage acceleration, shield/mitigation penalties, healer bonus, and TTD urgency.
    /// Optimized single-pass algorithm with deferred normalization.
    /// </summary>
    public IBattleChara? FindMostEndangeredPartyMember(
        IPlayerCharacter player,
        IDamageIntakeService damageIntakeService,
        int healAmount = 0,
        IDamageTrendService? damageTrendService = null,
        IShieldTrackingService? shieldTrackingService = null)
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

            var predictedHp = GetPredictedHp(member);

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

            // Enhanced triage factors (v1.11.0)
            // Shield penalty: targets with shields have effective HP buffer
            var shieldPct = 0f;
            var mitigation = 0f;
            if (shieldTrackingService != null)
            {
                var shieldValue = shieldTrackingService.GetTotalShieldValue(member.EntityId);
                shieldPct = member.MaxHp > 0 ? (float)shieldValue / member.MaxHp : 0f;
                mitigation = shieldTrackingService.GetCombinedMitigation(member.EntityId);
            }
            _endangeredShieldPcts[candidateCount] = shieldPct;
            _endangeredMitigations[candidateCount] = mitigation;

            // Healer bonus: keep co-healer alive for healing throughput
            var isHealer = false;
            if (member is IPlayerCharacter pc)
            {
                isHealer = JobRegistry.IsHealer(pc.ClassJob.RowId);
            }
            _endangeredHealerBonuses[candidateCount] = isHealer ? 1f : 0f;

            // TTD urgency: prioritize targets about to die
            var ttdScore = 0f;
            var survivability = HpPredictionService.GetSurvivabilityInfo(member.EntityId, member.CurrentHp, member.MaxHp);
            if (survivability.TimeUntilDeath < 10f)
            {
                // Normalize: 0s = 1.0, 10s+ = 0.0
                ttdScore = 1f - (survivability.TimeUntilDeath / 10f);
            }
            _endangeredTtdScores[candidateCount] = ttdScore;

            candidateCount++;
        }

        if (candidateCount == 0)
            return null;

        // Score candidates with actual max damage rate (no second iteration through game objects)
        IBattleChara? mostEndangered = null;
        float highestScore = float.MinValue;

        // Get configurable weights from config
        var weights = Configuration.Healing.GetEffectiveTriageWeights();

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

            // Weight using configurable triage weights
            // Enhanced scoring with shield/mitigation penalties and healer/TTD bonuses
            var score = (normalizedDamageRate * weights.DamageRate) +
                        (_endangeredTankBonuses[i] * weights.TankBonus) +
                        (_endangeredMissingHpPcts[i] * weights.MissingHp) +
                        (normalizedAcceleration * weights.DamageAcceleration) +
                        (_endangeredHealerBonuses[i] * weights.HealerBonus) +
                        (_endangeredTtdScores[i] * weights.TtdUrgency) -
                        (_endangeredShieldPcts[i] * weights.ShieldPenalty) -
                        (_endangeredMitigations[i] * weights.MitigationPenalty);

            if (score > highestScore)
            {
                highestScore = score;
                mostEndangered = _endangeredMembers[i];
            }
        }

        return mostEndangered;
    }

    #endregion

    #region AoE Range Counting

    /// <summary>
    /// Counts party members within AoE range that are below a certain HP threshold.
    /// Used for Lily cap prevention to decide between Solace and Rapture.
    /// </summary>
    public int CountInjuredInAoERange(IPlayerCharacter player, float radius, float hpThreshold)
    {
        var count = 0;
        var radiusSquared = radius * radius;
        var playerPos = player.Position;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;

            if (Vector3.DistanceSquared(playerPos, member.Position) > radiusSquared)
                continue;

            var predictedHp = GetPredictedHp(member);
            var hpPercent = (float)predictedHp / member.MaxHp;

            if (hpPercent < hpThreshold)
                count++;
        }

        return count;
    }

    #endregion
}
