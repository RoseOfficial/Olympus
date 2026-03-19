using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Models;
using Olympus.Rotation;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Common;
using CommonDebugState = Olympus.Rotation.Common.DebugState;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.IrisCore.Context;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Rotation.PrometheusCore.Context;
using Olympus.Rotation.TerpsichoreCore.Context;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;

using LuminaAction = Lumina.Excel.Sheets.Action;

namespace Olympus.Services.Debug;

/// <summary>
/// Central debug data aggregation service.
/// Collects data from active rotation, ActionTracker, ActionService, CombatEventService, and HpPredictionService
/// into a single snapshot for the debug window.
/// </summary>
public sealed class DebugService
{
    private readonly IActionTracker _actionTracker;
    private readonly ActionService _actionService;
    private readonly CombatEventService _combatEventService;
    private readonly HpPredictionService _hpPredictionService;
    private readonly PlayerStatsService _playerStatsService;
    private readonly HealingSpellSelector _healingSpellSelector;
    private readonly SpellStatusService _spellStatusService;
    private readonly RotationManager _rotationManager;
    private readonly IObjectTable _objectTable;
    private readonly IDataManager _dataManager;

    // Cached snapshot - updated on demand
    private DebugSnapshot? _cachedSnapshot;
    private int _lastSnapshotFrame;
    private int _currentFrame;

    public DebugService(
        IActionTracker actionTracker,
        ActionService actionService,
        CombatEventService combatEventService,
        HpPredictionService hpPredictionService,
        PlayerStatsService playerStatsService,
        HealingSpellSelector healingSpellSelector,
        SpellStatusService spellStatusService,
        RotationManager rotationManager,
        IObjectTable objectTable,
        IDataManager dataManager)
    {
        _actionTracker = actionTracker;
        _actionService = actionService;
        _combatEventService = combatEventService;
        _hpPredictionService = hpPredictionService;
        _playerStatsService = playerStatsService;
        _healingSpellSelector = healingSpellSelector;
        _spellStatusService = spellStatusService;
        _rotationManager = rotationManager;
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
        // Use active rotation's debug state (supports all jobs)
        var activeRotation = _rotationManager.ActiveRotation;
        var debug = activeRotation?.DebugState;

        // Return empty state if no rotation is active
        if (debug == null)
        {
            return new DebugRotationState
            {
                PlanningState = "No rotation active",
                PlannedAction = "N/A"
            };
        }

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

        // Get debug state from active rotation (if it's a healer)
        var activeRotation = _rotationManager.ActiveRotation;
        var debug = activeRotation?.DebugState;

        return new DebugHealingState
        {
            AoEStatus = debug?.AoEStatus ?? "N/A",
            AoEInjuredCount = debug?.AoEInjuredCount ?? 0,
            AoESelectedSpell = debug?.AoESelectedSpell ?? 0,
            PlayerHpPercent = debug?.PlayerHpPercent ?? 0,
            PartyListCount = debug?.PartyListCount ?? 0,
            PartyValidCount = debug?.PartyValidCount ?? 0,
            BattleNpcCount = debug?.BattleNpcCount ?? 0,
            NpcInfo = debug?.NpcInfo ?? "N/A",
            PendingHeals = pendingHeals,
            TotalPendingHealAmount = pendingHeals.Sum(h => h.Amount),
            LastHealAmount = debug?.LastHealAmount ?? 0,
            LastHealStats = debug?.LastHealStats ?? "N/A",
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

    /// <summary>Returns the local player's current job ID, or 0 if unavailable.</summary>
    public uint GetJobId()
        => _objectTable.LocalPlayer?.ClassJob.RowId ?? 0;

    /// <summary>Resets spell usage counts without affecting history or GCD uptime data.</summary>
    public void ClearSpellUsageCounts()
        => _actionTracker.ClearSpellUsageCounts();

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

    // ========== Tank Debug States ==========

    /// <summary>
    /// Gets the Ares (Warrior) debug state, if the active rotation is Warrior.
    /// </summary>
    public AresDebugState? GetAresDebugState()
    {
        return (_rotationManager.ActiveRotation as Ares)?.AresDebug;
    }

    /// <summary>
    /// Gets the Nyx (Dark Knight) debug state, if the active rotation is Dark Knight.
    /// </summary>
    public NyxDebugState? GetNyxDebugState()
    {
        return (_rotationManager.ActiveRotation as Nyx)?.NyxDebug;
    }

    /// <summary>
    /// Gets the Themis (Paladin) debug state, if the active rotation is Paladin.
    /// </summary>
    public ThemisDebugState? GetThemisDebugState()
    {
        return (_rotationManager.ActiveRotation as Themis)?.ThemisDebug;
    }

    /// <summary>
    /// Gets the Hephaestus (Gunbreaker) debug state, if the active rotation is Gunbreaker.
    /// </summary>
    public HephaestusDebugState? GetHephaestusDebugState()
    {
        return (_rotationManager.ActiveRotation as Hephaestus)?.HephaestusDebug;
    }

    /// <summary>
    /// Gets the Apollo (White Mage) debug state, if the active rotation is White Mage.
    /// </summary>
    public CommonDebugState? GetApolloDebugState()
    {
        return (_rotationManager.ActiveRotation as Apollo)?.DebugState;
    }

    /// <summary>
    /// Gets the Asclepius (Sage) debug state, if the active rotation is Sage.
    /// </summary>
    public AsclepiusDebugState? GetAsclepiusDebugState()
    {
        return (_rotationManager.ActiveRotation as Asclepius)?.AsclepiusDebug;
    }

    /// <summary>
    /// Gets the Athena (Scholar) debug state, if the active rotation is Scholar.
    /// </summary>
    public AthenaDebugState? GetAthenaDebugState()
    {
        return (_rotationManager.ActiveRotation as Athena)?.AthenaDebug;
    }

    /// <summary>
    /// Gets the Astraea (Astrologian) debug state, if the active rotation is Astrologian.
    /// </summary>
    public AstraeaDebugState? GetAstraeaDebugState()
    {
        return (_rotationManager.ActiveRotation as Astraea)?.AstraeaDebug;
    }

    // ========== Melee DPS Debug States ==========

    /// <summary>
    /// Gets the Zeus (Dragoon) debug state, if the active rotation is Dragoon.
    /// </summary>
    public ZeusDebugState? GetZeusDebugState()
    {
        return (_rotationManager.ActiveRotation as Zeus)?.ZeusDebug;
    }

    /// <summary>
    /// Gets the Hermes (Ninja) debug state, if the active rotation is Ninja.
    /// </summary>
    public HermesDebugState? GetHermesDebugState()
    {
        return (_rotationManager.ActiveRotation as Hermes)?.HermesDebug;
    }

    /// <summary>
    /// Gets the Nike (Samurai) debug state, if the active rotation is Samurai.
    /// </summary>
    public NikeDebugState? GetNikeDebugState()
    {
        return (_rotationManager.ActiveRotation as Nike)?.NikeDebug;
    }

    /// <summary>
    /// Gets the Kratos (Monk) debug state, if the active rotation is Monk.
    /// </summary>
    public KratosDebugState? GetKratosDebugState()
    {
        return (_rotationManager.ActiveRotation as Kratos)?.KratosDebug;
    }

    /// <summary>
    /// Gets the Thanatos (Reaper) debug state, if the active rotation is Reaper.
    /// </summary>
    public ThanatosDebugState? GetThanatosDebugState()
    {
        return (_rotationManager.ActiveRotation as Thanatos)?.ThanatosDebug;
    }

    /// <summary>
    /// Gets the Echidna (Viper) debug state, if the active rotation is Viper.
    /// </summary>
    public EchidnaDebugState? GetEchidnaDebugState()
    {
        return (_rotationManager.ActiveRotation as Echidna)?.EchidnaDebug;
    }

    // ========== Ranged Physical DPS Debug States ==========

    /// <summary>
    /// Gets the Prometheus (Machinist) debug state, if the active rotation is Machinist.
    /// </summary>
    public PrometheusDebugState? GetPrometheusDebugState()
    {
        return (_rotationManager.ActiveRotation as Prometheus)?.PrometheusDebug;
    }

    /// <summary>
    /// Gets the Calliope (Bard) debug state, if the active rotation is Bard.
    /// </summary>
    public CalliopeDebugState? GetCalliopeDebugState()
    {
        return (_rotationManager.ActiveRotation as Calliope)?.CalliopeDebug;
    }

    /// <summary>
    /// Gets the Terpsichore (Dancer) debug state, if the active rotation is Dancer.
    /// </summary>
    public TerpsichoreDebugState? GetTerpsichoreDebugState()
    {
        return (_rotationManager.ActiveRotation as Terpsichore)?.TerpsichoreDebug;
    }

    // ========== Caster DPS Debug States ==========

    /// <summary>
    /// Gets the Hecate (Black Mage) debug state, if the active rotation is Black Mage.
    /// </summary>
    public HecateDebugState? GetHecateDebugState()
    {
        return (_rotationManager.ActiveRotation as Hecate)?.HecateDebug;
    }

    /// <summary>
    /// Gets the Persephone (Summoner) debug state, if the active rotation is Summoner.
    /// </summary>
    public PersephoneDebugState? GetPersephoneDebugState()
    {
        return (_rotationManager.ActiveRotation as Persephone)?.PersephoneDebug;
    }

    /// <summary>
    /// Gets the Circe (Red Mage) debug state, if the active rotation is Red Mage.
    /// </summary>
    public CirceDebugState? GetCirceDebugState()
    {
        return (_rotationManager.ActiveRotation as Circe)?.CirceDebug;
    }

    /// <summary>
    /// Gets the Iris (Pictomancer) debug state, if the active rotation is Pictomancer.
    /// </summary>
    public IrisDebugState? GetIrisDebugState()
    {
        return (_rotationManager.ActiveRotation as Iris)?.IrisDebug;
    }
}
