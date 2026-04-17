using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Data;
using Olympus.Services.Party;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Base class for healer party helpers.
/// Extends BasePartyHelper with HP prediction integration and common healing target logic.
/// </summary>
public abstract class HealerPartyHelper : BasePartyHelper, ISpikeTargetSource
{
    protected readonly HpPredictionService HpPredictionService;
    protected readonly Configuration Configuration;

    /// <summary>
    /// Default healing range squared (30y × 30y) used when subclass doesn't
    /// override. Matches the standard GCD-heal cast range shared by Cure,
    /// Physick, Diagnosis, and Benefic.
    /// </summary>
    protected const float DefaultHealingRangeSquared = 900f;

    // Pre-allocated arrays for endangered-member triage (avoids per-frame allocation).
    // Sized to MaxPartySize (8) — the hard cap on FFXIV party size.
    private readonly IBattleChara?[] _endangeredMembers = new IBattleChara?[MaxPartySize];
    private readonly float[] _endangeredDamageRates = new float[MaxPartySize];
    private readonly float[] _endangeredMissingHpPcts = new float[MaxPartySize];
    private readonly float[] _endangeredTankBonuses = new float[MaxPartySize];
    private readonly float[] _endangeredDamageAccelerations = new float[MaxPartySize];
    private readonly float[] _endangeredShieldPcts = new float[MaxPartySize];
    private readonly float[] _endangeredMitigations = new float[MaxPartySize];
    private readonly float[] _endangeredHealerBonuses = new float[MaxPartySize];
    private readonly float[] _endangeredTtdScores = new float[MaxPartySize];

    /// <summary>
    /// Raise status ID used to check for pending resurrections.
    /// </summary>
    protected const ushort RaiseStatusId = 148;

    /// <summary>
    /// Transcendent status ID — post-raise invulnerability where the player cannot act.
    /// Healing targets with this buff is wasteful since they are invulnerable.
    /// </summary>
    protected const ushort TranscendentStatusId = 2656;

    /// <summary>
    /// Status IDs where casting a single-target direct oGCD heal (Benediction,
    /// Tetragrammaton, Lustrate, Essential Dignity, Druochole) is guaranteed
    /// waste. Covers tank invulnerability abilities and delayed-trigger heals
    /// that overlap any heal we would cast in the same window.
    /// </summary>
    /// <remarks>
    /// This does NOT cover shields, regens, ground-targeted heals, or AoE
    /// party heals — those can still be useful during an invuln window
    /// (duration outlasts the invuln, party members other than the invuln
    /// tank still benefit, etc). Check only from handlers that cast a
    /// target-specific instant heal.
    /// </remarks>
    // NOTE: Walking Dead (811) is NOT in this list. Walking Dead kicks in
    // when Living Dead expires — the DRK MUST be healed back to full during
    // that window or they die. Skipping heals during Walking Dead would kill
    // the player. Only the pure invuln (Living Dead, 810) is included.
    private static readonly ushort[] NoHealStatusIds =
    {
        82,   // Hallowed Ground (PLD invuln)
        409,  // Holmgang (WAR invuln)
        810,  // Living Dead (DRK invuln — Walking Dead 811 intentionally excluded)
        1836, // Superbolide (GNB invuln)
        1220, // Excogitation (SCH delayed heal — triggers before our heal lands)
        2685, // Catharsis of Corundum (GNB delayed heal from Heart of Corundum)
    };

    /// <summary>
    /// Doom status IDs that only clear when the target reaches 100% HP.
    /// A Doomed player at 85% looks healthy to normal heuristics but will
    /// die if not topped off before Doom expires.
    /// </summary>
    private static readonly ushort[] DoomStatusIds =
    {
        910,  // Doom (ARR/HW — Pharos Sirius, Tam-Tara Hard, etc.)
        1769, // Doom (ShB — Eden's Verse, The Seat of Sacrifice, etc.)
        2976, // Doom (EW — TOP phase 6, Abyssos, etc.)
    };

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
            if (HasTranscendent(member))
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > rangeSquared)
                continue;

            var predictedHp = GetPredictedHp(member);
            var hpPercent = (float)predictedHp / member.MaxHp;

            if (predictedHp >= member.MaxHp)
                continue;

            // Doom forces max priority — Doom only clears at 100% HP, so even a
            // target at 85% will die if not topped. Treat as near-zero effective HP.
            if (HasDoom(member))
                hpPercent = 0.01f;

            // Overheal prevention (skip for Doom targets — they need every heal)
            if (hpPercent > 0.01f && healAmount > 0)
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
        if (chara.StatusList == null)
            return false;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == RaiseStatusId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a target has the Transcendent (post-raise invulnerability) buff.
    /// Players with this status are invulnerable and should not be healed.
    /// </summary>
    protected static bool HasTranscendent(IBattleChara chara)
    {
        if (chara.StatusList == null)
            return false;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == TranscendentStatusId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks whether a target is in a state where a single-target direct
    /// oGCD heal (Benediction/Tetragrammaton/Lustrate/Essential Dignity/
    /// Druochole) would be wasted — tank invuln active, or a delayed heal
    /// (Excogitation, Catharsis of Corundum) already covering them.
    /// </summary>
    /// <remarks>
    /// Call this from oGCD ST heal handlers only. Do not apply to shields,
    /// regens, ground targets, or AoE party heals — those have legitimate
    /// use during invuln windows (duration outlasts the invuln, or other
    /// non-invuln party members benefit).
    /// </remarks>
    public static bool HasNoHealStatus(IBattleChara chara)
    {
        if (chara.StatusList == null)
            return false;

        foreach (var status in chara.StatusList)
        {
            if (IsNoHealStatusId(status.StatusId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Pure predicate: is the given status ID one of the NoHeal status IDs?
    /// Extracted from <see cref="HasNoHealStatus"/> so unit tests can exercise
    /// the ID list without having to fake Dalamud's <c>StatusList</c>.
    /// </summary>
    public static bool IsNoHealStatusId(uint statusId)
    {
        for (int i = 0; i < NoHealStatusIds.Length; i++)
        {
            if (NoHealStatusIds[i] == statusId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks whether a target has a Doom status that clears only at 100% HP.
    /// Doomed targets must be topped off or they die when Doom expires —
    /// normal HP-percent heuristics will under-prioritize them.
    /// </summary>
    public static bool HasDoom(IBattleChara chara)
    {
        if (chara.StatusList == null)
            return false;

        foreach (var status in chara.StatusList)
        {
            if (IsDoomStatusId(status.StatusId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Pure predicate: is the given status ID one of the Doom status IDs?
    /// </summary>
    public static bool IsDoomStatusId(uint statusId)
    {
        for (int i = 0; i < DoomStatusIds.Length; i++)
        {
            if (DoomStatusIds[i] == statusId)
                return true;
        }
        return false;
    }

    #endregion

    #region Endangered Member Triage

    /// <summary>
    /// Finds the most endangered party member using enhanced damage intake triage.
    /// Uses configurable weights including damage rate, tank bonus, missing HP,
    /// damage acceleration, shield/mitigation penalties, healer bonus, and TTD urgency.
    /// Single-pass algorithm with deferred normalization.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="damageIntakeService">Service providing damage intake data.</param>
    /// <param name="healAmount">Minimum missing HP to consider (prevents overhealing). 0 skips the check.</param>
    /// <param name="damageTrendService">Optional service for damage acceleration data.</param>
    /// <param name="shieldTrackingService">Optional service for shield/mitigation data.</param>
    /// <param name="rangeSquared">Cast range squared for target filtering. Defaults to 30y.</param>
    /// <returns>The most endangered party member, or null if none need healing.</returns>
    public IBattleChara? FindMostEndangeredPartyMember(
        IPlayerCharacter player,
        IDamageIntakeService damageIntakeService,
        int healAmount = 0,
        IDamageTrendService? damageTrendService = null,
        IShieldTrackingService? shieldTrackingService = null,
        float rangeSquared = DefaultHealingRangeSquared)
    {
        var candidateCount = 0;
        float maxDamageRate = 1f;
        float maxAcceleration = 1f;

        var playerPos = player.Position;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;

            if (candidateCount >= MaxPartySize)
                break;

            if (Vector3.DistanceSquared(playerPos, member.Position) > rangeSquared)
                continue;

            var predictedHp = GetPredictedHp(member);

            if (predictedHp >= member.MaxHp)
                continue;

            if (healAmount > 0)
            {
                var missingHp = member.MaxHp - predictedHp;
                if (healAmount > missingHp)
                    continue;
            }

            var hpPercent = (float)predictedHp / member.MaxHp;
            var damageRate = damageIntakeService.GetDamageRate(member.EntityId, 5f);

            if (damageRate > maxDamageRate)
                maxDamageRate = damageRate;

            var damageAccel = 0f;
            if (damageTrendService is not null)
            {
                damageAccel = damageTrendService.GetDamageAcceleration(member.EntityId, 5f);
                if (damageAccel > maxAcceleration)
                    maxAcceleration = damageAccel;
            }

            _endangeredMembers[candidateCount] = member;
            _endangeredDamageRates[candidateCount] = damageRate;
            _endangeredMissingHpPcts[candidateCount] = 1f - hpPercent;
            _endangeredTankBonuses[candidateCount] = IsTankRole(member) ? 1f : 0f;
            _endangeredDamageAccelerations[candidateCount] = damageAccel;

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

            var isHealer = false;
            if (member is IPlayerCharacter pc)
            {
                isHealer = JobRegistry.IsHealer(pc.ClassJob.RowId);
            }
            _endangeredHealerBonuses[candidateCount] = isHealer ? 1f : 0f;

            var ttdScore = 0f;
            var survivability = HpPredictionService.GetSurvivabilityInfo(member.EntityId, member.CurrentHp, member.MaxHp);
            if (survivability.TimeUntilDeath < 10f)
            {
                ttdScore = 1f - (survivability.TimeUntilDeath / 10f);
            }
            _endangeredTtdScores[candidateCount] = ttdScore;

            candidateCount++;
        }

        if (candidateCount == 0)
            return null;

        IBattleChara? mostEndangered = null;
        float highestScore = float.MinValue;

        var weights = Configuration.Healing.GetEffectiveTriageWeights();

        for (var i = 0; i < candidateCount; i++)
        {
            var normalizedDamageRate = _endangeredDamageRates[i] / maxDamageRate;

            var normalizedAcceleration = 0f;
            if (_endangeredDamageAccelerations[i] > 0 && maxAcceleration > 0)
            {
                normalizedAcceleration = _endangeredDamageAccelerations[i] / maxAcceleration;
            }

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

    #region Party Health Metrics

    /// <summary>
    /// Calculates party health metrics.
    /// </summary>
    public (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics(IPlayerCharacter player)
    {
        return CalculatePartyHealthMetrics(GetAllPartyMembers(player));
    }

    /// <summary>
    /// Calculates party health metrics from a pre-built member list.
    /// Use this overload when the caller already holds the party member list
    /// to avoid a second object-table scan.
    /// </summary>
    public static (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics(IEnumerable<IBattleChara> members)
    {
        float totalHpPercent = 0;
        float lowestHp = 1f;
        int count = 0;
        int injured = 0;

        foreach (var member in members)
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
            if (HasTranscendent(member))
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
