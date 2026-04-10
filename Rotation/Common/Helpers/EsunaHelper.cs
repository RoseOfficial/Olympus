using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Debuff;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Shared Esuna target-finding logic used by all healer rotations.
/// </summary>
public static class EsunaHelper
{
    /// <summary>
    /// Finds the highest-priority cleansable debuff target among party members.
    /// Prioritizes lethal debuffs, then by priority tier, then by shortest remaining time.
    /// </summary>
    public static (IBattleChara? target, uint statusId, DebuffPriority priority) FindBestTarget(
        IPlayerCharacter player,
        IEnumerable<IBattleChara> partyMembers,
        IDebuffDetectionService debuffService)
    {
        IBattleChara? bestTarget = null;
        uint bestStatusId = 0;
        var bestPriority = DebuffPriority.None;
        float bestRemainingTime = float.MaxValue;

        foreach (var member in partyMembers)
        {
            if (member.IsDead)
                continue;

            if (!DistanceHelper.IsInRange(player, member, RoleActions.Esuna.Range))
                continue;

            var (statusId, priority, remainingTime) = debuffService.FindHighestPriorityDebuff(member);

            if (priority == DebuffPriority.None)
                continue;

            if (priority < bestPriority ||
                (priority == bestPriority && remainingTime < bestRemainingTime))
            {
                bestTarget = member;
                bestStatusId = statusId;
                bestPriority = priority;
                bestRemainingTime = remainingTime;
            }
        }

        return (bestTarget, bestStatusId, bestPriority);
    }
}
