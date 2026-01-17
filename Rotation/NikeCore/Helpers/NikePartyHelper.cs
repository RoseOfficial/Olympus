using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.NikeCore.Helpers;

/// <summary>
/// Samurai party helper with SAM-specific logic.
/// Extends BasePartyHelper with party buff considerations.
/// </summary>
public sealed class NikePartyHelper : BasePartyHelper
{
    public NikePartyHelper(IObjectTable objectTable, IPartyList partyList)
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
    /// Counts party members in range.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="range">Range to check.</param>
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
}
