using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Base class for melee DPS party helpers.
/// Extends BasePartyHelper with raid buff targeting and party range checks.
/// </summary>
public class MeleeDpsPartyHelper : BasePartyHelper
{
    public MeleeDpsPartyHelper(IObjectTable objectTable, IPartyList partyList)
        : base(objectTable, partyList)
    {
    }

    /// <summary>
    /// Gets the HP percentage of a character.
    /// </summary>
    public float GetHpPercent(IBattleChara character)
    {
        return GetRawHpPercent(character);
    }

    /// <summary>
    /// Counts party members in range for raid buffs.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="range">Range to check.</param>
    public int CountMembersInRange(IPlayerCharacter player, float range)
    {
        var count = 0;
        var rangeSquared = range * range;

        foreach (var member in GetAllPartyMembers(player))
        {
            var dx = player.Position.X - member.Position.X;
            var dz = player.Position.Z - member.Position.Z;
            var distSq = dx * dx + dz * dz;

            if (distSq <= rangeSquared)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Counts how many party members are injured (below threshold).
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="threshold">HP threshold to consider injured.</param>
    public int CountInjuredMembers(IPlayerCharacter player, float threshold = 0.80f)
    {
        var count = 0;
        foreach (var member in GetAllPartyMembers(player))
        {
            if (GetHpPercent(member) < threshold)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Finds the best target for a buff ability (e.g., Dragon Sight).
    /// Prioritizes based on provided job priority array.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="priorityJobs">Array of job IDs in priority order (highest priority first).</param>
    /// <param name="range">Range to check.</param>
    public IBattleChara? FindBuffTarget(IPlayerCharacter player, uint[] priorityJobs, float range = 12f)
    {
        var rangeSquared = range * range;
        IBattleChara? bestTarget = null;
        var bestPriority = -1;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.EntityId == player.EntityId)
                continue;

            var dx = player.Position.X - member.Position.X;
            var dz = player.Position.Z - member.Position.Z;
            var distSq = dx * dx + dz * dz;

            if (distSq > rangeSquared)
                continue;

            var priority = GetJobPriority(member, priorityJobs);
            if (priority > bestPriority)
            {
                bestPriority = priority;
                bestTarget = member;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Gets the priority for targeting based on job.
    /// Higher values = higher priority.
    /// </summary>
    private static int GetJobPriority(IBattleChara member, uint[] priorityJobs)
    {
        if (member is not IPlayerCharacter pc)
            return 0;

        var jobId = pc.ClassJob.RowId;
        for (var i = 0; i < priorityJobs.Length; i++)
        {
            if (jobId == priorityJobs[i])
                return priorityJobs.Length - i;
        }

        return 0;
    }
}
