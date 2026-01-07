using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Services.Party;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.ApolloCore.Helpers;

/// <summary>
/// Interface for party member operations.
/// </summary>
public interface IPartyHelper
{
    /// <summary>
    /// Yields all party members (player + party list or Trust NPCs).
    /// </summary>
    IEnumerable<IBattleChara> GetAllPartyMembers(IPlayerCharacter player, bool includeDead = false);

    /// <summary>
    /// Finds the tank in the party.
    /// </summary>
    IBattleChara? FindTankInParty(IPlayerCharacter player);

    /// <summary>
    /// Finds the lowest HP party member that needs healing.
    /// </summary>
    IBattleChara? FindLowestHpPartyMember(IPlayerCharacter player, int healAmount = 0);

    /// <summary>
    /// Finds a dead party member that needs resurrection.
    /// </summary>
    IBattleChara? FindDeadPartyMemberNeedingRaise(IPlayerCharacter player);

    /// <summary>
    /// Gets predicted HP percent for a target.
    /// </summary>
    float GetHpPercent(IBattleChara target);

    /// <summary>
    /// Calculates party health metrics for defensive cooldown decisions.
    /// </summary>
    (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics(IPlayerCharacter player);

    /// <summary>
    /// Finds the best target for Cure III.
    /// </summary>
    (IBattleChara? target, int count, List<uint> targetIds) FindBestCureIIITarget(IPlayerCharacter player, int healAmount);

    /// <summary>
    /// Counts party members needing AoE heal.
    /// </summary>
    (int count, bool anyHaveRegen, List<(uint entityId, string name)> allTargets, int averageMissingHp)
        CountPartyMembersNeedingAoEHeal(IPlayerCharacter player, int healAmount);

    /// <summary>
    /// Finds the best target for Regen with tank priority.
    /// </summary>
    IBattleChara? FindRegenTarget(IPlayerCharacter player, float regenHpThreshold, float regenRefreshThreshold);

    /// <summary>
    /// Checks if a target needs Regen.
    /// </summary>
    bool NeedsRegen(IBattleChara target, float hpThreshold, float refreshThreshold);

    /// <summary>
    /// Finds the most endangered party member using enhanced damage intake triage.
    /// Uses configurable weights including damage rate, tank bonus, missing HP,
    /// damage acceleration, shield/mitigation penalties, healer bonus, and TTD urgency.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="damageIntakeService">Service providing damage intake data.</param>
    /// <param name="healAmount">Minimum missing HP to consider (prevents overhealing).</param>
    /// <param name="damageTrendService">Optional service for damage acceleration data.</param>
    /// <param name="shieldTrackingService">Optional service for shield/mitigation data.</param>
    /// <returns>The most endangered party member, or null if none need healing.</returns>
    IBattleChara? FindMostEndangeredPartyMember(
        IPlayerCharacter player,
        IDamageIntakeService damageIntakeService,
        int healAmount = 0,
        IDamageTrendService? damageTrendService = null,
        IShieldTrackingService? shieldTrackingService = null);

    /// <summary>
    /// Counts party members within AoE range that are below a certain HP threshold.
    /// Used for Lily cap prevention to decide between Solace and Rapture.
    /// </summary>
    /// <param name="player">The local player (center of AoE).</param>
    /// <param name="radius">The radius to check for party members.</param>
    /// <param name="hpThreshold">HP percent threshold (e.g., 0.99 = below 99% HP).</param>
    /// <returns>Count of party members below the threshold within range.</returns>
    int CountInjuredInAoERange(IPlayerCharacter player, float radius, float hpThreshold);

    /// <summary>
    /// Returns all party members (excluding dead) for iteration.
    /// Wrapper for GetAllPartyMembers with includeDead=false.
    /// </summary>
    IEnumerable<IBattleChara> GetPartyMembers(IPlayerCharacter player);
}
