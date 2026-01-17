using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.KratosCore.Helpers;

/// <summary>
/// Monk party helper with MNK-specific targeting logic.
/// Extends BasePartyHelper with Brotherhood and Mantra considerations.
/// </summary>
public sealed class KratosPartyHelper : BasePartyHelper
{
    public KratosPartyHelper(IObjectTable objectTable, IPartyList partyList)
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
    /// Counts party members in range for Brotherhood/Mantra.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="range">Range to check (default 30y for Brotherhood).</param>
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
    /// Counts how many party members are injured (below threshold).
    /// Useful for Mantra timing.
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
}
