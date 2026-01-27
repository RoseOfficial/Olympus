using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Base class for tank party helpers.
/// Extends BasePartyHelper with co-tank targeting and party protection logic.
/// </summary>
public class TankPartyHelper : BasePartyHelper
{
    public TankPartyHelper(IObjectTable objectTable, IPartyList partyList)
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
    /// Finds a party member that could benefit from Cover/Nascent Flash/Intervention.
    /// Returns null if no one needs protection.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="hpThreshold">HP threshold below which to consider using protection ability.</param>
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

    /// <summary>
    /// Counts how many party members are injured (below threshold).
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
