using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;

namespace Olympus.Rotation.IrisCore.Helpers;

/// <summary>
/// Helper class for party-related operations in the Iris rotation.
/// Pictomancer has limited party utility, so this is minimal.
/// </summary>
public sealed class IrisPartyHelper
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;

    public IrisPartyHelper(IObjectTable objectTable, IPartyList partyList)
    {
        _objectTable = objectTable;
        _partyList = partyList;
    }

    /// <summary>
    /// Gets all party members including the player.
    /// In solo play, returns just the player.
    /// </summary>
    public IEnumerable<IPlayerCharacter> GetAllPartyMembers(IPlayerCharacter player)
    {
        // If in a party, iterate through party list
        if (_partyList.Length > 0)
        {
            foreach (var member in _partyList)
            {
                if (member.GameObject is IPlayerCharacter pc)
                    yield return pc;
            }
        }
        else
        {
            // Solo play - just the player
            yield return player;
        }
    }

    /// <summary>
    /// Gets the HP percentage of a character.
    /// </summary>
    public float GetHpPercent(IPlayerCharacter character)
    {
        if (character.MaxHp == 0)
            return 1f;

        return (float)character.CurrentHp / character.MaxHp;
    }

    /// <summary>
    /// Checks if the player is in a party.
    /// </summary>
    public bool IsInParty()
    {
        return _partyList.Length > 0;
    }

    /// <summary>
    /// Gets the party size (including the player).
    /// Returns 1 for solo play.
    /// </summary>
    public int GetPartySize()
    {
        return _partyList.Length > 0 ? _partyList.Length : 1;
    }

    /// <summary>
    /// Counts party members within range of a target position.
    /// Useful for determining AoE healing value.
    /// </summary>
    public int CountPartyMembersInRange(IPlayerCharacter player, float range)
    {
        var count = 0;
        var playerPos = player.Position;

        foreach (var member in GetAllPartyMembers(player))
        {
            var distance = System.Numerics.Vector3.Distance(playerPos, member.Position);
            if (distance <= range)
                count++;
        }

        return count;
    }
}
