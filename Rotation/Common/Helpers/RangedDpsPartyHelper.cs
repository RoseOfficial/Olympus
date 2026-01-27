using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Base class for ranged physical DPS party helpers.
/// Extends BasePartyHelper with range checks and party health queries.
/// </summary>
public class RangedDpsPartyHelper : BasePartyHelper
{
    public RangedDpsPartyHelper(IObjectTable objectTable, IPartyList partyList)
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
    /// Gets the number of party members in range of a given radius.
    /// </summary>
    public int CountMembersInRange(IPlayerCharacter player, float radius)
    {
        var count = 0;
        var radiusSq = radius * radius;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.EntityId == player.EntityId)
                continue;

            var dx = player.Position.X - member.Position.X;
            var dz = player.Position.Z - member.Position.Z;
            var distSq = dx * dx + dz * dz;

            if (distSq <= radiusSq)
                count++;
        }

        return count + 1; // Include self
    }

    /// <summary>
    /// Counts party members below a certain HP threshold.
    /// </summary>
    public int CountMembersBelow(IPlayerCharacter player, float threshold)
    {
        var count = 0;
        foreach (var member in GetAllPartyMembers(player))
        {
            if (GetHpPercent(member) < threshold)
                count++;
        }
        return count;
    }
}
