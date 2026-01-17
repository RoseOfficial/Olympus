using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Models;
using Olympus.Rotation;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;

using LuminaAction = Lumina.Excel.Sheets.Action;

namespace Olympus.Services.Debug;

/// <summary>
/// Central debug data aggregation service.
/// Collects data from Apollo, Athena, ActionTracker, ActionService, CombatEventService, and HpPredictionService
/// into a single snapshot for the debug window.
/// </summary>
public sealed class DebugService
{
    private readonly ActionTracker _actionTracker;
    private readonly ActionService _actionService;
    private readonly CombatEventService _combatEventService;
    private readonly HpPredictionService _hpPredictionService;
    private readonly PlayerStatsService _playerStatsService;
    private readonly HealingSpellSelector _healingSpellSelector;
    private readonly SpellStatusService _spellStatusService;
    private readonly RotationManager _rotationManager;
    private readonly Apollo _apollo;
    private readonly Athena? _athena;
    private readonly Astraea? _astraea;
    private readonly IObjectTable _objectTable;
    private readonly IDataManager _dataManager;

    // Cached snapshot - updated on demand
    private DebugSnapshot? _cachedSnapshot;
    private int _lastSnapshotFrame;
    private int _currentFrame;

    public DebugService(
        ActionTracker actionTracker,
        ActionService actionService,
        CombatEventService combatEventService,
        HpPredictionService hpPredictionService,
        PlayerStatsService playerStatsService,
        HealingSpellSelector healingSpellSelector,
        SpellStatusService spellStatusService,
        RotationManager rotationManager,
        Apollo apollo,
        IObjectTable objectTable,
        IDataManager dataManager,
        Athena? athena = null,
        Astraea? astraea = null)
    {
        _actionTracker = actionTracker;
        _actionService = actionService;
        _combatEventService = combatEventService;
        _hpPredictionService = hpPredictionService;
        _playerStatsService = playerStatsService;
        _healingSpellSelector = healingSpellSelector;
        _spellStatusService = spellStatusService;
        _rotationManager = rotationManager;
        _apollo = apollo;
        _athena = athena;
        _astraea = astraea;
        _objectTable = objectTable;
        _dataManager = dataManager;
    }

    /// <summary>
    /// Call once per frame to increment frame counter.
    /// </summary>
    public void Update()
    {
        _currentFrame++;
    }

    /// <summary>
    /// Gets the current debug snapshot.
    /// Cached per frame to avoid redundant data collection.
    /// </summary>
    public DebugSnapshot GetSnapshot()
    {
        // Return cached if same frame
        if (_cachedSnapshot != null && _lastSnapshotFrame == _currentFrame)
            return _cachedSnapshot;

        _cachedSnapshot = BuildSnapshot();
        _lastSnapshotFrame = _currentFrame;
        return _cachedSnapshot;
    }

    private DebugSnapshot BuildSnapshot()
    {
        return new DebugSnapshot
        {
            Statistics = BuildStatistics(),
            GcdState = BuildGcdState(),
            Rotation = BuildRotationState(),
            Healing = BuildHealingState(),
            Actions = BuildActionState(),
            OverhealStats = BuildOverhealStats()
        };
    }

    private DebugStatistics BuildStatistics()
    {
        var (total, success, successRate, gcdUptime, avgCastGap) = _actionTracker.GetStatistics();
        var topFailure = _actionTracker.GetMostCommonFailure();

        return new DebugStatistics
        {
            TotalAttempts = total,
            SuccessCount = success,
            SuccessRate = successRate,
            GcdUptime = gcdUptime,
            AverageCastGap = avgCastGap,
            TopFailureReason = topFailure?.reason.ToString(),
            TopFailureCount = topFailure?.count ?? 0,
            DowntimeEventCount = _actionTracker.DowntimeEventCount,
            LastDowntimeTime = _actionTracker.LastDowntimeTime,
            LastDowntimeReason = _actionTracker.LastDowntimeReason
        };
    }

    private DebugGcdState BuildGcdState()
    {
        return new DebugGcdState
        {
            State = _actionService.CurrentGcdState,
            GcdRemaining = _actionService.GcdRemaining,
            AnimationLockRemaining = _actionService.AnimationLockRemaining,
            IsCasting = _actionService.IsCasting,
            CanExecuteGcd = _actionService.CanExecuteGcd,
            CanExecuteOgcd = _actionService.CanExecuteOgcd,
            WeaveSlots = _actionService.GetAvailableWeaveSlots(),
            LastActionName = _actionService.LastExecutedAction?.Name ?? "None",
            DebugGcdReady = _actionTracker.DebugGcdReady,
            DebugIsActive = _actionTracker.DebugIsActive
        };
    }

    private DebugRotationState BuildRotationState()
    {
        // Use active rotation's debug state (supports all jobs, not just healers)
        var activeRotation = _rotationManager.ActiveRotation;
        var debug = activeRotation?.DebugState ?? _apollo.DebugState;
        return new DebugRotationState
        {
            // Core state
            PlanningState = debug.PlanningState,
            PlannedAction = debug.PlannedAction,
            DpsState = debug.DpsState,
            TargetInfo = debug.TargetInfo,

            // Resurrection
            RaiseState = debug.RaiseState,
            RaiseTarget = debug.RaiseTarget,

            // Esuna
            EsunaState = debug.EsunaState,
            EsunaTarget = debug.EsunaTarget,

            // oGCD States
            ThinAirState = debug.ThinAirState,
            AsylumState = debug.AsylumState,
            AsylumTarget = debug.AsylumTarget,
            DefensiveState = debug.DefensiveState,
            TemperanceState = debug.TemperanceState,
            SurecastState = debug.SurecastState,

            // DPS Details
            AoEDpsState = debug.AoEDpsState,
            AoEDpsEnemyCount = debug.AoEDpsEnemyCount,
            MiseryState = debug.MiseryState,

            // Resources
            LilyCount = debug.LilyCount,
            BloodLilyCount = debug.BloodLilyCount,
            LilyStrategy = debug.LilyStrategy,
            SacredSightStacks = debug.SacredSightStacks
        };
    }

    private DebugHealingState BuildHealingState()
    {
        var pendingHeals = BuildPendingHeals();
        var recentHeals = BuildRecentHeals();
        var shadowHpEntries = BuildShadowHpEntries();
        var debug = _apollo.DebugState;

        return new DebugHealingState
        {
            AoEStatus = debug.AoEStatus,
            AoEInjuredCount = debug.AoEInjuredCount,
            AoESelectedSpell = debug.AoESelectedSpell,
            PlayerHpPercent = debug.PlayerHpPercent,
            PartyListCount = debug.PartyListCount,
            PartyValidCount = debug.PartyValidCount,
            BattleNpcCount = debug.BattleNpcCount,
            NpcInfo = debug.NpcInfo,
            PendingHeals = pendingHeals,
            TotalPendingHealAmount = pendingHeals.Sum(h => h.Amount),
            LastHealAmount = debug.LastHealAmount,
            LastHealStats = debug.LastHealStats,
            RecentHeals = recentHeals,
            TotalRecentHealAmount = recentHeals.Sum(h => h.Amount),
            ShadowHpEntries = shadowHpEntries
        };
    }

    private List<DebugPendingHeal> BuildPendingHeals()
    {
        var result = new List<DebugPendingHeal>();
        var pendingHeals = _hpPredictionService.GetAllPendingHeals();

        foreach (var (targetId, amount) in pendingHeals)
        {
            var targetObj = _objectTable.SearchById(targetId);
            var targetName = targetObj?.Name.TextValue ?? $"ID:{targetId}";

            result.Add(new DebugPendingHeal
            {
                TargetId = targetId,
                TargetName = targetName,
                Amount = amount
            });
        }

        return result;
    }

    private List<DebugRecentHeal> BuildRecentHeals()
    {
        var result = new List<DebugRecentHeal>();
        var recentHeals = _combatEventService.GetRecentHeals();
        var actionSheet = _dataManager.GetExcelSheet<LuminaAction>();

        foreach (var heal in recentHeals.Take(10))
        {
            var actionName = actionSheet?.GetRowOrDefault(heal.ActionId)?.Name.ToString()
                ?? $"Action {heal.ActionId}";

            result.Add(new DebugRecentHeal
            {
                Timestamp = heal.Timestamp,
                ActionId = heal.ActionId,
                ActionName = actionName,
                TargetName = heal.TargetName,
                Amount = heal.Amount
            });
        }

        return result;
    }

    private List<DebugShadowHpEntry> BuildShadowHpEntries()
    {
        var result = new List<DebugShadowHpEntry>();
        var shadowHpData = _combatEventService.GetAllShadowHp().ToList();

        foreach (var (entityId, shadowHp) in shadowHpData)
        {
            var gameObj = _objectTable.SearchById(entityId);
            if (gameObj is not IBattleChara chara)
                continue;

            result.Add(new DebugShadowHpEntry
            {
                EntityId = entityId,
                EntityName = chara.Name.TextValue,
                GameHp = chara.CurrentHp,
                ShadowHp = shadowHp,
                MaxHp = chara.MaxHp
            });
        }

        return result;
    }

    private DebugActionState BuildActionState()
    {
        var spellUsage = _actionTracker.GetSpellUsageCounts()
            .Select(s => new DebugSpellUsage
            {
                Name = s.name,
                ActionId = s.actionId,
                Count = s.count
            })
            .ToList();

        return new DebugActionState
        {
            History = _actionTracker.GetHistory(),
            SpellUsage = spellUsage
        };
    }

    private DebugOverhealStats BuildOverhealStats()
    {
        var stats = _combatEventService.GetOverhealStatistics();
        var actionSheet = _dataManager.GetExcelSheet<LuminaAction>();

        // Convert per-spell stats with resolved names
        var bySpell = stats.BySpell.Select(s => new DebugSpellOverheal
        {
            ActionId = s.ActionId,
            SpellName = actionSheet?.GetRowOrDefault(s.ActionId)?.Name.ToString() ?? s.SpellName,
            TotalHealing = s.TotalHealing,
            TotalOverheal = s.TotalOverheal,
            CastCount = s.CastCount
        }).OrderByDescending(s => s.TotalHealing).ToList();

        // Convert per-target stats
        var byTarget = stats.ByTarget.Select(t => new DebugTargetOverheal
        {
            TargetId = t.TargetId,
            TargetName = t.TargetName,
            TotalHealing = t.TotalHealing,
            TotalOverheal = t.TotalOverheal,
            HealCount = t.HealCount
        }).OrderByDescending(t => t.TotalHealing).ToList();

        // Convert recent overheal events with resolved spell names
        var recentOverheals = stats.RecentOverhealEvents.Select(e => new DebugOverhealEvent
        {
            Timestamp = e.Timestamp,
            SpellName = ResolveOverhealSpellName(e.SpellName, actionSheet),
            TargetName = e.TargetName,
            HealAmount = e.HealAmount,
            OverhealAmount = e.OverhealAmount
        }).ToList();

        return new DebugOverhealStats
        {
            SessionStartTime = stats.SessionStartTime,
            SessionDuration = stats.SessionDuration,
            TotalHealing = stats.TotalHealing,
            TotalOverheal = stats.TotalOverheal,
            OverhealPercent = stats.OverhealPercent,
            BySpell = bySpell,
            ByTarget = byTarget,
            RecentOverheals = recentOverheals
        };
    }

    private static string ResolveOverhealSpellName(string spellName, Lumina.Excel.ExcelSheet<LuminaAction>? actionSheet)
    {
        // SpellName format is "Action{actionId}" - extract the ID and resolve
        if (spellName.StartsWith("Action") && uint.TryParse(spellName.AsSpan(6), out var actionId))
        {
            return actionSheet?.GetRowOrDefault(actionId)?.Name.ToString() ?? spellName;
        }
        return spellName;
    }

    /// <summary>
    /// Clears action tracker data.
    /// </summary>
    public void ClearHistory()
    {
        _actionTracker.Clear();
    }

    /// <summary>
    /// Resets all overheal statistics for a new tracking session.
    /// </summary>
    public void ResetOverhealStatistics()
    {
        _combatEventService.ResetOverhealStatistics();
    }

    /// <summary>
    /// Gets the action name from Lumina data.
    /// </summary>
    public string GetActionName(uint actionId)
    {
        var actionSheet = _dataManager.GetExcelSheet<LuminaAction>();
        var row = actionSheet?.GetRowOrDefault(actionId);
        return row?.Name.ToString() ?? $"Action {actionId}";
    }

    /// <summary>
    /// Gets player stats debug info.
    /// </summary>
    public string GetPlayerStatsDebugInfo()
    {
        var player = _objectTable.LocalPlayer;
        if (player == null)
            return "No player";

        return _playerStatsService.GetDebugInfo(player.Level);
    }

    /// <summary>
    /// Gets the current player level, or 0 if not logged in.
    /// </summary>
    public byte GetPlayerLevel()
    {
        return _objectTable.LocalPlayer?.Level ?? 0;
    }

    /// <summary>
    /// Gets the last spell selection decision for debugging.
    /// </summary>
    public SpellSelectionDebug? GetLastSpellSelection()
    {
        return _healingSpellSelector.LastSelection;
    }

    /// <summary>
    /// Gets real-time status of all WHM spells.
    /// </summary>
    public SpellStatusSnapshot GetSpellStatus(byte playerLevel)
    {
        return _spellStatusService.GetSnapshot(playerLevel);
    }

    /// <summary>
    /// Gets the Athena (Scholar) debug state, if available.
    /// </summary>
    public AthenaDebugState? GetAthenaDebugState()
    {
        return _athena?.AthenaDebug;
    }

    /// <summary>
    /// Gets the Astraea (Astrologian) debug state, if available.
    /// </summary>
    public AstraeaDebugState? GetAstraeaDebugState()
    {
        return _astraea?.AstraeaDebug;
    }
}
