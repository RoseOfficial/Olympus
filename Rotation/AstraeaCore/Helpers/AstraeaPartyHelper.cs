using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Data;
using Olympus.Services.Party;
using Olympus.Services.Prediction;

namespace Olympus.Rotation.AstraeaCore.Helpers;

/// <summary>
/// Helper class for party member operations specific to Astrologian.
/// Includes card targeting logic for DPS-focused strategy.
/// </summary>
public sealed class AstraeaPartyHelper
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;
    private readonly HpPredictionService _hpPredictionService;
    private readonly Configuration _configuration;
    private readonly AstraeaStatusHelper _statusHelper;

    // Tank ClassJob IDs (PLD, WAR, DRK, GNB + base classes GLA, MRD)
    private static readonly HashSet<uint> TankJobIds = new() { 19, 21, 32, 37, 1, 3 };

    // Healer ClassJob IDs (WHM, SCH, AST, SGE + base class CNJ)
    private static readonly HashSet<uint> HealerJobIds = new() { 24, 28, 33, 40, 6 };

    // Melee DPS ClassJob IDs (MNK, DRG, NIN, SAM, RPR, VPR + base classes PGL, LNC, ROG)
    private static readonly HashSet<uint> MeleeDpsJobIds = new() { 20, 22, 30, 34, 39, 41, 2, 4, 29 };

    // Ranged Physical DPS ClassJob IDs (BRD, MCH, DNC + base class ARC)
    private static readonly HashSet<uint> RangedPhysicalDpsJobIds = new() { 23, 31, 38, 5 };

    // Caster DPS ClassJob IDs (BLM, SMN, RDM, PCT + base classes THM, ACN)
    private static readonly HashSet<uint> CasterDpsJobIds = new() { 25, 27, 35, 42, 7, 26 };

    // Party member caching
    private readonly HashSet<uint> _cachedPartyEntityIds = new(8);
    private int _lastPartyCount = -1;
    private uint _lastPlayerEntityId;

    private const int MaxPartySize = 8;

    public AstraeaPartyHelper(
        IObjectTable objectTable,
        IPartyList partyList,
        HpPredictionService hpPredictionService,
        Configuration configuration,
        AstraeaStatusHelper statusHelper)
    {
        _objectTable = objectTable;
        _partyList = partyList;
        _hpPredictionService = hpPredictionService;
        _configuration = configuration;
        _statusHelper = statusHelper;
    }

    /// <summary>
    /// Yields all party members (player + party list or Trust NPCs).
    /// </summary>
    public IEnumerable<IBattleChara> GetAllPartyMembers(IPlayerCharacter player, bool includeDead = false)
    {
        yield return player;

        if (_partyList.Length > 0)
        {
            if (_partyList.Length != _lastPartyCount || player.EntityId != _lastPlayerEntityId)
            {
                _cachedPartyEntityIds.Clear();
                foreach (var partyMember in _partyList)
                {
                    if (partyMember.EntityId != player.EntityId)
                        _cachedPartyEntityIds.Add(partyMember.EntityId);
                }
                _lastPartyCount = _partyList.Length;
                _lastPlayerEntityId = player.EntityId;
            }

            foreach (var obj in _objectTable)
            {
                if (obj is IBattleChara chara && _cachedPartyEntityIds.Contains(obj.EntityId))
                {
                    if (includeDead || !chara.IsDead)
                        yield return chara;
                }
            }
        }
        else
        {
            foreach (var obj in _objectTable)
            {
                if (IsValidTrustNpc(obj, out var npc, includeDead))
                    yield return npc!;
            }
        }
    }

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

    #region Card Targeting (DPS-Focused)

    /// <summary>
    /// Finds the best target for The Balance card (melee DPS buff).
    /// Prioritizes melee DPS, then ranged DPS if no melee available.
    /// </summary>
    public IBattleChara? FindBalanceTarget(IPlayerCharacter player)
    {
        IBattleChara? bestMelee = null;
        IBattleChara? bestRanged = null;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.PlayI.RangeSquared)
                continue;
            if (_statusHelper.HasAnyCardBuff(member))
                continue;

            if (IsMeleeDps(member))
            {
                // TODO: Could add damage contribution tracking here for true "highest DPS"
                if (bestMelee == null)
                    bestMelee = member;
            }
            else if (IsRangedPhysicalDps(member) || IsCasterDps(member))
            {
                if (bestRanged == null)
                    bestRanged = member;
            }
        }

        // Prefer melee for Balance, fallback to ranged
        return bestMelee ?? bestRanged;
    }

    /// <summary>
    /// Finds the best target for The Spear card (ranged DPS buff).
    /// Prioritizes ranged DPS (physical + caster), then melee if no ranged available.
    /// </summary>
    public IBattleChara? FindSpearTarget(IPlayerCharacter player)
    {
        IBattleChara? bestRanged = null;
        IBattleChara? bestMelee = null;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.PlayII.RangeSquared)
                continue;
            if (_statusHelper.HasAnyCardBuff(member))
                continue;

            if (IsRangedPhysicalDps(member) || IsCasterDps(member))
            {
                if (bestRanged == null)
                    bestRanged = member;
            }
            else if (IsMeleeDps(member))
            {
                if (bestMelee == null)
                    bestMelee = member;
            }
        }

        // Prefer ranged for Spear, fallback to melee
        return bestRanged ?? bestMelee;
    }

    /// <summary>
    /// Finds the best target for Lord of Crowns card (AoE damage buff).
    /// Any DPS is valid target.
    /// </summary>
    public IBattleChara? FindLordTarget(IPlayerCharacter player)
    {
        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.PlayIII.RangeSquared)
                continue;
            if (_statusHelper.HasAnyCardBuff(member))
                continue;

            if (IsDpsRole(member))
                return member;
        }

        // Fallback to tank if no DPS available
        return FindTankInParty(player);
    }

    /// <summary>
    /// Gets the appropriate card target based on the current card type.
    /// </summary>
    public IBattleChara? FindCardTarget(IPlayerCharacter player, ASTActions.CardType cardType)
    {
        return cardType switch
        {
            ASTActions.CardType.TheBalance => FindBalanceTarget(player),
            ASTActions.CardType.TheSpear => FindSpearTarget(player),
            ASTActions.CardType.Lord => FindLordTarget(player),
            _ => null
        };
    }

    #endregion

    #region Tank & Healing Targets

    /// <summary>
    /// Finds the tank in the party.
    /// </summary>
    public IBattleChara? FindTankInParty(IPlayerCharacter player)
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

    /// <summary>
    /// Finds the lowest HP party member that needs healing.
    /// </summary>
    public IBattleChara? FindLowestHpPartyMember(IPlayerCharacter player, int healAmount = 0)
    {
        IBattleChara? lowestHpMember = null;
        float lowestHpPercent = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.Benefic.RangeSquared)
                continue;

            var predictedHp = _hpPredictionService.GetPredictedHp(member.EntityId, member.CurrentHp, member.MaxHp);
            var hpPercent = (float)predictedHp / member.MaxHp;

            if (predictedHp >= member.MaxHp)
                continue;

            if (healAmount > 0)
            {
                var missingHp = member.MaxHp - predictedHp;
                if (healAmount > missingHp)
                    continue;
            }

            if (hpPercent < lowestHpPercent)
            {
                lowestHpPercent = hpPercent;
                lowestHpMember = member;
            }
        }

        return lowestHpMember;
    }

    /// <summary>
    /// Finds a dead party member that needs resurrection.
    /// </summary>
    public IBattleChara? FindDeadPartyMemberNeedingRaise(IPlayerCharacter player)
    {
        foreach (var member in GetAllPartyMembers(player, includeDead: true))
        {
            if (member.EntityId == player.EntityId)
                continue;
            if (!member.IsDead)
                continue;

            // Skip if already has Raise pending
            if (HasRaiseStatus(member))
                continue;

            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.Ascend.RangeSquared)
                continue;

            return member;
        }

        return null;
    }

    /// <summary>
    /// Checks if a target has Raise status pending.
    /// </summary>
    private static bool HasRaiseStatus(IBattleChara chara)
    {
        const ushort RaiseStatusId = 148;
        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == RaiseStatusId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets predicted HP percent for a target.
    /// </summary>
    public float GetHpPercent(IBattleChara target)
    {
        return _hpPredictionService.GetPredictedHpPercent(target.EntityId, target.CurrentHp, target.MaxHp);
    }

    #endregion

    #region Party Health Metrics

    /// <summary>
    /// Calculates party health metrics.
    /// </summary>
    public (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics(IPlayerCharacter player)
    {
        float totalHpPercent = 0;
        float lowestHp = 1f;
        int count = 0;
        int injured = 0;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;

            var hpPct = member.MaxHp > 0 ? (float)member.CurrentHp / member.MaxHp : 1f;
            totalHpPercent += hpPct;
            count++;

            if (hpPct < lowestHp)
                lowestHp = hpPct;

            if (hpPct < FFXIVConstants.InjuredHpThreshold)
                injured++;
        }

        return (count > 0 ? totalHpPercent / count : 1f, lowestHp, injured);
    }

    /// <summary>
    /// Counts party members needing AoE heal.
    /// </summary>
    public (int count, List<uint> targetIds) CountPartyMembersNeedingAoEHeal(IPlayerCharacter player, int healAmount)
    {
        int count = 0;
        var targetIds = new List<uint>();

        foreach (var member in GetAllPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.Helios.RadiusSquared)
                continue;
            if (member.IsDead)
                continue;

            var predictedHp = _hpPredictionService.GetPredictedHp(member.EntityId, member.CurrentHp, member.MaxHp);
            var missingHp = member.MaxHp - predictedHp;

            if (healAmount <= missingHp)
            {
                count++;
                targetIds.Add(member.EntityId);
            }
        }

        return (count, targetIds);
    }

    #endregion

    #region Essential Dignity Target

    /// <summary>
    /// Finds the best target for Essential Dignity.
    /// Essential Dignity scales with missing HP (400-1100 potency), so lower HP is better.
    /// </summary>
    public IBattleChara? FindEssentialDignityTarget(IPlayerCharacter player, float hpThreshold = 0.4f)
    {
        IBattleChara? bestTarget = null;
        float lowestHp = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.EssentialDignity.RangeSquared)
                continue;

            var hpPercent = GetHpPercent(member);

            if (hpPercent < hpThreshold && hpPercent < lowestHp)
            {
                lowestHp = hpPercent;
                bestTarget = member;
            }
        }

        return bestTarget;
    }

    #endregion

    #region Synastry Target

    /// <summary>
    /// Finds the best target for Synastry (usually tank taking sustained damage).
    /// </summary>
    public IBattleChara? FindSynastryTarget(IPlayerCharacter player)
    {
        // Priority 1: Tank
        var tank = FindTankInParty(player);
        if (tank != null && !_statusHelper.HasSynastryLink(tank))
        {
            var hpPercent = GetHpPercent(tank);
            if (hpPercent < 0.8f) // Only if tank has taken some damage
                return tank;
        }

        // Priority 2: Lowest HP party member
        return FindLowestHpPartyMember(player);
    }

    #endregion

    #region Exaltation Target

    /// <summary>
    /// Finds the best target for Exaltation (damage reduction + delayed heal).
    /// </summary>
    public IBattleChara? FindExaltationTarget(IPlayerCharacter player)
    {
        // Priority 1: Tank without Exaltation taking damage
        var tank = FindTankInParty(player);
        if (tank != null && !_statusHelper.HasExaltation(tank))
        {
            var hpPercent = GetHpPercent(tank);
            if (hpPercent < 0.8f)
                return tank;
        }

        // Priority 2: Any party member below threshold without Exaltation
        IBattleChara? lowestMember = null;
        float lowestHp = 1f;

        foreach (var member in GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > ASTActions.Exaltation.RangeSquared)
                continue;
            if (_statusHelper.HasExaltation(member))
                continue;

            var hpPercent = GetHpPercent(member);
            if (hpPercent < lowestHp && hpPercent < 0.7f)
            {
                lowestHp = hpPercent;
                lowestMember = member;
            }
        }

        return lowestMember ?? tank;
    }

    #endregion

    /// <summary>
    /// Returns all party members (excluding dead) for iteration.
    /// </summary>
    public IEnumerable<IBattleChara> GetPartyMembers(IPlayerCharacter player)
    {
        return GetAllPartyMembers(player, includeDead: false);
    }
}
