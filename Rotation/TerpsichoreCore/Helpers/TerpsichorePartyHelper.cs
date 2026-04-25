using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Config.DPS;
using Olympus.Data;

namespace Olympus.Rotation.TerpsichoreCore.Helpers;

/// <summary>
/// Helper for party member queries and dance partner selection in Dancer rotation.
/// </summary>
public sealed class TerpsichorePartyHelper
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;

    // Dance partner priority: higher potency gain jobs first
    // Priority: SAM > NIN > VPR > RPR > MNK > DRG > BLM > SMN > RDM > PCT > MCH > BRD > DNC > Tank > Healer
    private static readonly uint[] PartnerPriority = new[]
    {
        JobRegistry.Samurai,     // 1st priority - SAM
        JobRegistry.Ninja,       // 2nd - NIN
        JobRegistry.Viper,       // 3rd - VPR
        JobRegistry.Reaper,      // 4th - RPR
        JobRegistry.Monk,        // 5th - MNK
        JobRegistry.Dragoon,     // 6th - DRG
        JobRegistry.BlackMage,   // 7th - BLM
        JobRegistry.Summoner,    // 8th - SMN
        JobRegistry.RedMage,     // 9th - RDM
        JobRegistry.Pictomancer, // 10th - PCT
        JobRegistry.Machinist,   // 11th - MCH
        JobRegistry.Bard,        // 12th - BRD
        JobRegistry.Dancer,      // 13th - DNC
        // Tanks
        JobRegistry.Paladin,
        JobRegistry.Gladiator,
        JobRegistry.Warrior,
        JobRegistry.Marauder,
        JobRegistry.DarkKnight,
        JobRegistry.Gunbreaker,
        // Healers (lowest priority)
        JobRegistry.WhiteMage,
        JobRegistry.Conjurer,
        JobRegistry.Scholar,
        JobRegistry.Arcanist,
        JobRegistry.Astrologian,
        JobRegistry.Sage
    };

    public TerpsichorePartyHelper(IObjectTable objectTable, IPartyList partyList)
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

    /// <summary>
    /// Selects the best dance partner based on job priority and the configured selection mode.
    /// </summary>
    public IBattleChara? SelectDancePartner(IPlayerCharacter player, PartnerSelection mode = PartnerSelection.HighestDps)
    {
        if (_partyList.Length == 0)
            return null; // Solo, no partner available

        return mode switch
        {
            PartnerSelection.MeleePriority => SelectPartnerWithRolePriority(player, preferMelee: true),
            PartnerSelection.RangedPriority => SelectPartnerWithRolePriority(player, preferMelee: false),
            _ => SelectPartnerByRank(player),
        };
    }

    private IBattleChara? SelectPartnerByRank(IPlayerCharacter player)
    {
        IBattleChara? bestPartner = null;
        var bestPriority = int.MaxValue;

        foreach (var member in _partyList)
        {
            if (member.GameObject is not IBattleChara battleChara)
                continue;
            if (battleChara.EntityId == player.EntityId)
                continue;
            if (battleChara.CurrentHp == 0)
                continue;

            var priority = GetJobPriority(member.ClassJob.RowId);
            if (priority < bestPriority)
            {
                bestPriority = priority;
                bestPartner = battleChara;
            }
        }

        return bestPartner;
    }

    private IBattleChara? SelectPartnerWithRolePriority(IPlayerCharacter player, bool preferMelee)
    {
        // First pass: look for a partner in the preferred role group
        IBattleChara? preferred = null;
        var preferredPriority = int.MaxValue;

        foreach (var member in _partyList)
        {
            if (member.GameObject is not IBattleChara battleChara)
                continue;
            if (battleChara.EntityId == player.EntityId)
                continue;
            if (battleChara.CurrentHp == 0)
                continue;

            var jobId = member.ClassJob.RowId;
            bool inPreferredRole = preferMelee
                ? JobRegistry.IsMeleeDps(jobId)
                : (JobRegistry.IsRangedPhysicalDps(jobId) || JobRegistry.IsCasterDps(jobId));

            if (!inPreferredRole)
                continue;

            var priority = GetJobPriority(jobId);
            if (priority < preferredPriority)
            {
                preferredPriority = priority;
                preferred = battleChara;
            }
        }

        if (preferred != null)
            return preferred;

        // Fallback: no one in the preferred role — use the default rank
        return SelectPartnerByRank(player);
    }

    /// <summary>
    /// Gets the entity ID of our current dance partner.
    /// </summary>
    public uint GetDancePartnerId(IPlayerCharacter player, TerpsichoreStatusHelper statusHelper)
    {
        if (!statusHelper.HasClosedPosition(player))
            return 0;

        // Find who has the Dance Partner buff from us
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.EntityId == player.EntityId)
                continue;

            if (statusHelper.HasDancePartnerFrom(member, player.EntityId))
                return member.EntityId;
        }

        return 0;
    }

    /// <summary>
    /// Gets the name of our current dance partner.
    /// </summary>
    public string? GetDancePartnerName(IPlayerCharacter player, TerpsichoreStatusHelper statusHelper)
    {
        var partnerId = GetDancePartnerId(player, statusHelper);
        if (partnerId == 0)
            return null;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.EntityId == partnerId)
                return member.Name?.TextValue;
        }

        return null;
    }

    /// <summary>
    /// Checks if we need to update our dance partner (current one is dead or a better option is available).
    /// </summary>
    public bool ShouldUpdatePartner(IPlayerCharacter player, TerpsichoreStatusHelper statusHelper)
    {
        var currentPartnerId = GetDancePartnerId(player, statusHelper);

        // No partner, we should get one
        if (currentPartnerId == 0)
            return true;

        // Check if current partner is dead
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.EntityId == currentPartnerId)
            {
                if (member.CurrentHp == 0)
                    return true;
                break;
            }
        }

        return false;
    }

    private static int GetJobPriority(uint jobId)
    {
        for (int i = 0; i < PartnerPriority.Length; i++)
        {
            if (PartnerPriority[i] == jobId)
                return i;
        }

        // Unknown job, lowest priority
        return int.MaxValue;
    }
}
