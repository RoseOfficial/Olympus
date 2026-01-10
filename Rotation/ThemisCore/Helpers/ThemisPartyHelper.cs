using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;

namespace Olympus.Rotation.ThemisCore.Helpers;

/// <summary>
/// Helper class for party-related queries for Paladin.
/// </summary>
public sealed class ThemisPartyHelper
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;

    public ThemisPartyHelper(IObjectTable objectTable, IPartyList partyList)
    {
        _objectTable = objectTable;
        _partyList = partyList;
    }

    /// <summary>
    /// Finds the co-tank in the party (another tank that isn't the player).
    /// Returns null if solo or no co-tank.
    /// </summary>
    public IBattleChara? FindCoTank(IPlayerCharacter player)
    {
        foreach (var member in _partyList)
        {
            if (member.EntityId == player.GameObjectId)
                continue;

            var partyMember = _objectTable.SearchById(member.EntityId) as IBattleChara;
            if (partyMember != null && JobRegistry.IsTank(partyMember.ClassJob.RowId))
                return partyMember;
        }
        return null;
    }

    /// <summary>
    /// Gets all party members as IBattleChara.
    /// </summary>
    public IEnumerable<IBattleChara> GetAllPartyMembers(IPlayerCharacter player)
    {
        // If not in party, just return the player
        if (_partyList.Length == 0)
        {
            yield return player;
            yield break;
        }

        foreach (var member in _partyList)
        {
            var partyMember = _objectTable.SearchById(member.EntityId) as IBattleChara;
            if (partyMember != null)
                yield return partyMember;
        }
    }

    /// <summary>
    /// Gets the HP percentage of a character.
    /// </summary>
    public float GetHpPercent(IBattleChara character)
    {
        if (character.MaxHp == 0) return 1f;
        return (float)character.CurrentHp / character.MaxHp;
    }

    /// <summary>
    /// Finds a party member that could benefit from Cover.
    /// Returns null if no one needs covering.
    /// </summary>
    public IBattleChara? FindCoverTarget(IPlayerCharacter player, float hpThreshold = 0.40f)
    {
        IBattleChara? lowestMember = null;
        float lowestHp = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.GameObjectId == player.GameObjectId)
                continue;

            var hp = GetHpPercent(member);
            if (hp < hpThreshold && hp < lowestHp)
            {
                lowestHp = hp;
                lowestMember = member;
            }
        }

        return lowestMember;
    }
}
