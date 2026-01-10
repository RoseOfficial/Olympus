using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.ThemisCore.Helpers;

/// <summary>
/// Paladin party helper with PLD-specific targeting logic.
/// Extends BasePartyHelper with Cover and co-tank targeting.
/// </summary>
public sealed class ThemisPartyHelper : BasePartyHelper
{
    public ThemisPartyHelper(IObjectTable objectTable, IPartyList partyList)
        : base(objectTable, partyList)
    {
    }

    /// <summary>
    /// Finds the co-tank in the party (another tank that isn't the player).
    /// Returns null if solo or no co-tank.
    /// </summary>
    public IBattleChara? FindCoTank(IPlayerCharacter player)
    {
        foreach (var member in PartyList)
        {
            if (member.EntityId == player.GameObjectId)
                continue;

            var partyMember = ObjectTable.SearchById(member.EntityId) as IBattleChara;
            if (partyMember != null && JobRegistry.IsTank(partyMember.ClassJob.RowId))
                return partyMember;
        }
        return null;
    }

    /// <summary>
    /// Gets the HP percentage of a character.
    /// </summary>
    public float GetHpPercent(IBattleChara character)
    {
        return GetRawHpPercent(character);
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
