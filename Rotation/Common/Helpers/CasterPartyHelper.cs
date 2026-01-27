using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Base class for caster DPS party helpers.
/// Extends BasePartyHelper with party health queries and utility targeting.
/// </summary>
public class CasterPartyHelper : BasePartyHelper
{
    public CasterPartyHelper(IObjectTable objectTable, IPartyList partyList)
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
    /// Gets the party size (including player).
    /// Returns 1 if solo.
    /// </summary>
    public int GetPartySize()
    {
        return PartyList.Length > 0 ? PartyList.Length : 1;
    }

    /// <summary>
    /// Checks if in a party (more than just the player).
    /// </summary>
    public bool IsInParty()
    {
        return PartyList.Length > 0;
    }

    /// <summary>
    /// Gets the lowest HP party member for utility targeting (e.g., Rekindle).
    /// </summary>
    public IBattleChara? GetLowestHpMember(IPlayerCharacter player)
    {
        IBattleChara? lowestMember = null;
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

    /// <summary>
    /// Finds a suitable utility target (party member with lowest HP below threshold).
    /// Returns null if no suitable target found.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="minHpThreshold">Maximum HP percent to consider (default 0.8 = 80%).</param>
    public IBattleChara? FindUtilityTarget(IPlayerCharacter player, float minHpThreshold = 0.8f)
    {
        IBattleChara? bestTarget = null;
        var lowestHp = minHpThreshold;

        foreach (var member in GetAllPartyMembers(player))
        {
            var hp = GetHpPercent(member);
            if (hp < lowestHp && hp > 0) // hp > 0 to exclude dead members
            {
                lowestHp = hp;
                bestTarget = member;
            }
        }

        return bestTarget;
    }
}
