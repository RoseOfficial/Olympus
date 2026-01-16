using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.HephaestusCore.Helpers;

/// <summary>
/// Gunbreaker party helper with GNB-specific targeting logic.
/// Extends BasePartyHelper with Aurora and Heart of Corundum targeting.
/// </summary>
public sealed class HephaestusPartyHelper : BasePartyHelper
{
    public HephaestusPartyHelper(IObjectTable objectTable, IPartyList partyList)
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
    /// Finds a party member that could benefit from Aurora HoT.
    /// Returns null if no one needs healing.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="hpThreshold">HP threshold below which to consider using Aurora.</param>
    public IBattleChara? FindAuroraTarget(IPlayerCharacter player, float hpThreshold = 0.70f)
    {
        IBattleChara? lowestMember = null;
        float lowestHp = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            var hp = GetHpPercent(member);

            // Prioritize self if injured
            if (member.GameObjectId == player.GameObjectId && hp < hpThreshold)
                return member;

            // Otherwise find lowest HP member
            if (hp < hpThreshold && hp < lowestHp)
            {
                lowestHp = hp;
                lowestMember = member;
            }
        }

        return lowestMember;
    }

    /// <summary>
    /// Finds a party member that could benefit from Heart of Corundum.
    /// Prioritizes main tank swap targets, then lowest HP party members.
    /// Returns null if no one needs protection.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="hpThreshold">HP threshold below which to consider using HoC.</param>
    public IBattleChara? FindHeartOfCorundumTarget(IPlayerCharacter player, float hpThreshold = 0.80f)
    {
        IBattleChara? lowestMember = null;
        float lowestHp = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            var hp = GetHpPercent(member);

            // Prioritize self if tanking and injured
            if (member.GameObjectId == player.GameObjectId && hp < hpThreshold)
                return member;

            // Prioritize co-tank if they're taking damage
            if (member.GameObjectId != player.GameObjectId &&
                JobRegistry.IsTank(member.ClassJob.RowId) && hp < 0.80f)
                return member;

            // Otherwise find lowest HP member
            if (member.GameObjectId != player.GameObjectId && hp < hpThreshold && hp < lowestHp)
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
