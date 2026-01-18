using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;

namespace Olympus.Rotation.HecateCore.Helpers;

/// <summary>
/// Helper for party-related queries for Black Mage.
/// Black Mages don't have party-focused abilities, but this is included
/// for consistency with other rotation patterns.
/// </summary>
public sealed class HecatePartyHelper
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;

    public HecatePartyHelper(IObjectTable objectTable, IPartyList partyList)
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
}
