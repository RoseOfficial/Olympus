using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

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
}
