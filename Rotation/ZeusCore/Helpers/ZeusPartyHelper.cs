using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.ZeusCore.Helpers;

/// <summary>
/// Dragoon party helper with DRG-specific targeting logic.
/// Extends BasePartyHelper with Battle Litany and Dragon Sight considerations.
/// </summary>
public sealed class ZeusPartyHelper : BasePartyHelper
{
    public ZeusPartyHelper(IObjectTable objectTable, IPartyList partyList)
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
    /// Counts party members in range for Battle Litany.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="range">Range to check (default 30y for Battle Litany).</param>
    public int CountMembersInRange(IPlayerCharacter player, float range = 30f)
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
    /// Finds the best target for Dragon Sight tether.
    /// Prefers DPS players in range, prioritizing melee DPS.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="range">Range to check (default 12y for Dragon Sight).</param>
    public IBattleChara? FindDragonSightTarget(IPlayerCharacter player, float range = 12f)
    {
        var rangeSquared = range * range;
        IBattleChara? bestTarget = null;
        var bestPriority = -1;

        foreach (var member in GetAllPartyMembers(player))
        {
            // Skip self
            if (member.EntityId == player.EntityId)
                continue;

            var dx = player.Position.X - member.Position.X;
            var dz = player.Position.Z - member.Position.Z;
            var distSq = dx * dx + dz * dz;

            if (distSq > rangeSquared)
                continue;

            // Priority based on role (melee DPS > ranged DPS > caster > healer > tank)
            var priority = GetDragonSightPriority(member);
            if (priority > bestPriority)
            {
                bestPriority = priority;
                bestTarget = member;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Gets the priority for Dragon Sight targeting based on job.
    /// Higher values = higher priority.
    /// </summary>
    private static int GetDragonSightPriority(IBattleChara member)
    {
        if (member is not IPlayerCharacter pc)
            return 0;

        var jobId = pc.ClassJob.RowId;

        // Melee DPS (highest priority)
        if (jobId is 20 or 2 or 22 or 4 or 30 or 34 or 39 or 41)
            return 5;

        // Ranged Physical DPS
        if (jobId is 23 or 5 or 31 or 38)
            return 4;

        // Caster DPS
        if (jobId is 25 or 7 or 27 or 35 or 42)
            return 3;

        // Healers
        if (jobId is 24 or 6 or 28 or 33 or 40)
            return 1;

        // Tanks (lowest priority)
        if (jobId is 19 or 1 or 21 or 3 or 32 or 37)
            return 0;

        return 2; // Unknown
    }
}
