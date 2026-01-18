using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;

namespace Olympus.Rotation.PrometheusCore.Helpers;

/// <summary>
/// Helper for party member queries in Machinist rotation.
/// </summary>
public sealed class PrometheusPartyHelper
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;

    public PrometheusPartyHelper(IObjectTable objectTable, IPartyList partyList)
    {
        _objectTable = objectTable;
        _partyList = partyList;
    }

    /// <summary>
    /// Gets all party members including the player (solo) or party list (in party).
    /// </summary>
    public IEnumerable<IBattleChara> GetAllPartyMembers(IPlayerCharacter player)
    {
        if (_partyList.Length == 0)
        {
            // Solo - just the player
            yield return player;
            yield break;
        }

        foreach (var member in _partyList)
        {
            if (member.GameObject is IBattleChara battleChara)
            {
                yield return battleChara;
            }
        }
    }

    /// <summary>
    /// Gets the HP percentage for a party member.
    /// </summary>
    public float GetHpPercent(IBattleChara member)
    {
        if (member.MaxHp == 0)
            return 1f;

        return (float)member.CurrentHp / member.MaxHp;
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
}
