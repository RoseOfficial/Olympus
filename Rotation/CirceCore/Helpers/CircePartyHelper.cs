using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;

namespace Olympus.Rotation.CirceCore.Helpers;

/// <summary>
/// Helper for party-related queries for Red Mage.
/// Red Mage has Vercure and Verraise for party utility.
/// </summary>
public sealed class CircePartyHelper
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;

    public CircePartyHelper(IObjectTable objectTable, IPartyList partyList)
    {
        _objectTable = objectTable;
        _partyList = partyList;
    }

    /// <summary>
    /// Gets all party members including the player.
    /// </summary>
    public IEnumerable<IPlayerCharacter> GetAllPartyMembers(IPlayerCharacter player)
    {
        // Solo case - just return the player
        if (_partyList.Length == 0)
        {
            yield return player;
            yield break;
        }

        // Party case - iterate through party list
        foreach (var member in _partyList)
        {
            if (member.GameObject is IPlayerCharacter pc)
            {
                yield return pc;
            }
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
    /// Gets the party size (including player).
    /// Returns 1 if solo.
    /// </summary>
    public int GetPartySize()
    {
        return _partyList.Length > 0 ? _partyList.Length : 1;
    }

    /// <summary>
    /// Checks if in a party (more than just the player).
    /// </summary>
    public bool IsInParty()
    {
        return _partyList.Length > 0;
    }

    /// <summary>
    /// Gets the lowest HP party member for Vercure targeting.
    /// </summary>
    public IPlayerCharacter? GetLowestHpMember(IPlayerCharacter player)
    {
        IPlayerCharacter? lowestMember = null;
        var lowestHp = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            var hp = GetHpPercent(member);
            if (hp < lowestHp)
            {
                lowestHp = hp;
                lowestMember = member;
            }
        }

        return lowestMember;
    }

    /// <summary>
    /// Gets count of party members below a given HP threshold.
    /// </summary>
    public int GetMembersBelowHpThreshold(IPlayerCharacter player, float threshold)
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
