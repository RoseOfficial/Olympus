using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Base class for party member operations.
/// Provides core party iteration, role detection, and caching.
/// </summary>
public abstract class BasePartyHelper
{
    protected readonly IObjectTable ObjectTable;
    protected readonly IPartyList PartyList;

    /// <summary>
    /// Tank ClassJob IDs (PLD, WAR, DRK, GNB + base classes GLA, MRD).
    /// </summary>
    protected static readonly HashSet<uint> TankJobIds = new() { 19, 21, 32, 37, 1, 3 };

    /// <summary>
    /// Healer ClassJob IDs (WHM, SCH, AST, SGE + base class CNJ).
    /// </summary>
    protected static readonly HashSet<uint> HealerJobIds = new() { 24, 28, 33, 40, 6 };

    /// <summary>
    /// Melee DPS ClassJob IDs (MNK, DRG, NIN, SAM, RPR, VPR + base classes PGL, LNC, ROG).
    /// </summary>
    protected static readonly HashSet<uint> MeleeDpsJobIds = new() { 20, 22, 30, 34, 39, 41, 2, 4, 29 };

    /// <summary>
    /// Ranged Physical DPS ClassJob IDs (BRD, MCH, DNC + base class ARC).
    /// </summary>
    protected static readonly HashSet<uint> RangedPhysicalDpsJobIds = new() { 23, 31, 38, 5 };

    /// <summary>
    /// Caster DPS ClassJob IDs (BLM, SMN, RDM, PCT + base classes THM, ACN).
    /// </summary>
    protected static readonly HashSet<uint> CasterDpsJobIds = new() { 25, 27, 35, 42, 7, 26 };

    // Party member caching for efficient iteration
    protected readonly HashSet<uint> CachedPartyEntityIds = new(8);
    protected int LastPartyCount = -1;
    protected uint LastPlayerEntityId;

    protected const int MaxPartySize = 8;

    protected BasePartyHelper(IObjectTable objectTable, IPartyList partyList)
    {
        ObjectTable = objectTable;
        PartyList = partyList;
    }

    #region Core Party Iteration

    /// <summary>
    /// Yields all party members (player + party list or Trust NPCs).
    /// </summary>
    public virtual IEnumerable<IBattleChara> GetAllPartyMembers(IPlayerCharacter player, bool includeDead = false)
    {
        yield return player;

        if (PartyList.Length > 0)
        {
            // Update party cache if needed
            if (PartyList.Length != LastPartyCount || player.EntityId != LastPlayerEntityId)
            {
                CachedPartyEntityIds.Clear();
                foreach (var partyMember in PartyList)
                {
                    if (partyMember.EntityId != player.EntityId)
                        CachedPartyEntityIds.Add(partyMember.EntityId);
                }
                LastPartyCount = PartyList.Length;
                LastPlayerEntityId = player.EntityId;
            }

            foreach (var obj in ObjectTable)
            {
                if (obj is IBattleChara chara && CachedPartyEntityIds.Contains(obj.EntityId))
                {
                    if (includeDead || !chara.IsDead)
                        yield return chara;
                }
            }
        }
        else
        {
            // Trust NPC mode
            foreach (var obj in ObjectTable)
            {
                if (IsValidTrustNpc(obj, out var npc, includeDead))
                    yield return npc!;
            }
        }
    }

    /// <summary>
    /// Returns all party members (excluding dead) for iteration.
    /// </summary>
    public IEnumerable<IBattleChara> GetPartyMembers(IPlayerCharacter player)
    {
        return GetAllPartyMembers(player, includeDead: false);
    }

    #endregion

    #region Trust NPC Detection

    /// <summary>
    /// Checks if an object is a valid Trust NPC party member.
    /// </summary>
    public static bool IsValidTrustNpc(IGameObject obj, out IBattleNpc? npc, bool includeDead = false)
    {
        npc = null;
        if (obj.ObjectKind != ObjectKind.BattleNpc)
            return false;
        if (obj is not IBattleNpc battleNpc)
            return false;
        if (!includeDead && battleNpc.CurrentHp == 0)
            return false;
        if (battleNpc.MaxHp == 0)
            return false;
        if ((battleNpc.StatusFlags & (StatusFlags)FFXIVConstants.HostileStatusFlag) != 0)
            return false;
        if (battleNpc.SubKind != FFXIVConstants.TrustNpcSubKind)
            return false;

        npc = battleNpc;
        return true;
    }

    #endregion

    #region Role Detection

    /// <summary>
    /// Checks if a character is a tank role.
    /// </summary>
    public static bool IsTankRole(IBattleChara chara)
    {
        if (chara is IPlayerCharacter pc)
        {
            return TankJobIds.Contains(pc.ClassJob.RowId);
        }
        return false;
    }

    /// <summary>
    /// Checks if a character is a healer role.
    /// </summary>
    public static bool IsHealerRole(IBattleChara chara)
    {
        if (chara is IPlayerCharacter pc)
        {
            return HealerJobIds.Contains(pc.ClassJob.RowId);
        }
        return false;
    }

    /// <summary>
    /// Checks if a character is a melee DPS.
    /// </summary>
    public static bool IsMeleeDps(IBattleChara chara)
    {
        if (chara is IPlayerCharacter pc)
        {
            return MeleeDpsJobIds.Contains(pc.ClassJob.RowId);
        }
        return false;
    }

    /// <summary>
    /// Checks if a character is a ranged physical DPS.
    /// </summary>
    public static bool IsRangedPhysicalDps(IBattleChara chara)
    {
        if (chara is IPlayerCharacter pc)
        {
            return RangedPhysicalDpsJobIds.Contains(pc.ClassJob.RowId);
        }
        return false;
    }

    /// <summary>
    /// Checks if a character is a caster DPS.
    /// </summary>
    public static bool IsCasterDps(IBattleChara chara)
    {
        if (chara is IPlayerCharacter pc)
        {
            return CasterDpsJobIds.Contains(pc.ClassJob.RowId);
        }
        return false;
    }

    /// <summary>
    /// Checks if a character is any type of DPS.
    /// </summary>
    public static bool IsDpsRole(IBattleChara chara)
    {
        return IsMeleeDps(chara) || IsRangedPhysicalDps(chara) || IsCasterDps(chara);
    }

    #endregion

    #region Tank Finding

    /// <summary>
    /// Finds the tank in the party.
    /// </summary>
    public virtual IBattleChara? FindTankInParty(IPlayerCharacter player)
    {
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.EntityId == player.EntityId)
                continue;
            if (member.IsDead)
                continue;
            if (IsTankRole(member))
                return member;
        }
        return null;
    }

    #endregion

    #region Basic HP Operations

    /// <summary>
    /// Gets the HP percentage of a character (raw, no prediction).
    /// </summary>
    public static float GetRawHpPercent(IBattleChara character)
    {
        if (character.MaxHp == 0) return 1f;
        return (float)character.CurrentHp / character.MaxHp;
    }

    #endregion
}
