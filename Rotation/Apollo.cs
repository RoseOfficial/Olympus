using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;

namespace Olympus.Rotation;

/// <summary>
/// White Mage rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// </summary>
public sealed class Apollo
{
    // Services
    private readonly IPluginLog _log;
    private readonly ActionTracker _actionTracker;
    private readonly CombatEventService _combatEventService;
    private readonly Configuration _configuration;
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;
    private readonly TargetingService _targetingService;
    private readonly HpPredictionService _hpPredictionService;
    private readonly ActionService _actionService;
    private readonly PlayerStatsService _playerStatsService;
    private readonly HealingSpellSelector _healingSpellSelector;
    private readonly DebuffDetectionService _debuffDetectionService;
    private readonly IErrorMetricsService? _errorMetrics;

    // Error throttling to avoid log spam
    private DateTime _lastErrorTime = DateTime.MinValue;
    private const int ErrorThrottleSeconds = 10;
    private int _suppressedErrorCount;

    // Helpers (shared across modules)
    private readonly StatusHelper _statusHelper;
    private readonly PartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IApolloModule> _modules;

    // Movement detection
    private Vector3 _lastPosition;

    // Debug state (exposed for UI)
    public int DebugAoEInjuredCount { get; private set; }
    public uint DebugAoESelectedSpell { get; private set; }
    public string DebugAoEStatus { get; private set; } = "Idle";
    public float DebugPlayerHpPercent { get; private set; }
    public int DebugPartyListCount { get; private set; }
    public int DebugPartyValidCount { get; private set; }
    public int DebugBattleNpcCount { get; private set; }
    public string DebugNpcInfo { get; private set; } = "";
    public string DebugPlanningState { get; private set; } = "Idle";
    public string DebugPlannedAction { get; private set; } = "None";
    public string DebugDpsState { get; private set; } = "Idle";
    public string DebugTargetInfo { get; private set; } = "None";
    public int DebugLastHealAmount { get; private set; }
    public string DebugLastHealStats { get; private set; } = "";
    public string DebugRaiseState { get; private set; } = "Idle";
    public string DebugRaiseTarget { get; private set; } = "None";
    public string DebugAoEDpsState { get; private set; } = "Idle";
    public int DebugAoEDpsEnemyCount { get; private set; }
    public string DebugAsylumState { get; private set; } = "Idle";
    public string DebugAsylumTarget { get; private set; } = "None";
    public string DebugThinAirState { get; private set; } = "Idle";
    public string DebugDefensiveState { get; private set; } = "Idle";
    public string DebugTemperanceState { get; private set; } = "Idle";
    public int DebugLilyCount { get; private set; }
    public int DebugBloodLilyCount { get; private set; }
    public string DebugLilyStrategy { get; private set; } = "Balanced";
    public int DebugSacredSightStacks { get; private set; }
    public string DebugMiseryState { get; private set; } = "Idle";
    public string DebugEsunaState { get; private set; } = "Idle";
    public string DebugEsunaTarget { get; private set; } = "None";
    public string DebugSurecastState { get; private set; } = "Idle";

    public Apollo(
        IPluginLog log,
        ActionTracker actionTracker,
        CombatEventService combatEventService,
        Configuration configuration,
        IObjectTable objectTable,
        IPartyList partyList,
        TargetingService targetingService,
        HpPredictionService hpPredictionService,
        ActionService actionService,
        PlayerStatsService playerStatsService,
        HealingSpellSelector healingSpellSelector,
        DebuffDetectionService debuffDetectionService,
        IErrorMetricsService? errorMetrics = null)
    {
        _log = log;
        _actionTracker = actionTracker;
        _combatEventService = combatEventService;
        _configuration = configuration;
        _objectTable = objectTable;
        _partyList = partyList;
        _targetingService = targetingService;
        _hpPredictionService = hpPredictionService;
        _actionService = actionService;
        _playerStatsService = playerStatsService;
        _healingSpellSelector = healingSpellSelector;
        _debuffDetectionService = debuffDetectionService;
        _errorMetrics = errorMetrics;

        // Initialize helpers
        _statusHelper = new StatusHelper();
        _partyHelper = new PartyHelper(objectTable, partyList, hpPredictionService);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IApolloModule>
        {
            new ResurrectionModule(),  // Priority 5 - Dead members are useless
            new HealingModule(),       // Priority 10 - Keep party alive
            new DefensiveModule(),     // Priority 20 - Mitigation
            new BuffModule(),          // Priority 30 - Buffs and utilities
            new DamageModule(),        // Priority 50 - DPS when safe
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    /// Main execution loop - called every frame.
    /// Creates context and delegates to modules in priority order.
    /// </summary>
    public void Execute(IPlayerCharacter player)
    {
        try
        {
            ExecuteInternal(player);
        }
        catch (SEHException ex)
        {
            // Critical: Structured Exception Handler - game memory is in bad state
            HandleCriticalError("SEHException", ex);
        }
        catch (AccessViolationException ex)
        {
            // Critical: Access violation - pointer to invalid memory
            HandleCriticalError("AccessViolation", ex);
        }
        catch (NullReferenceException ex)
        {
            // Likely stale pointer or disposed object - log and continue
            _errorMetrics?.RecordError("Apollo.Execute.NullRef", ex.Message);
            _suppressedErrorCount++;
        }
        catch (Exception ex)
        {
            // General error - throttled logging
            HandleThrottledError(ex);
        }
    }

    /// <summary>
    /// Handle critical errors that indicate memory corruption.
    /// </summary>
    private void HandleCriticalError(string errorType, Exception ex)
    {
        _configuration.Enabled = false;
        _log.Error(ex, "Apollo DISABLED due to {0} - memory access error", errorType);
        _errorMetrics?.RecordError($"Apollo.Execute.{errorType}", ex.Message);
    }

    /// <summary>
    /// Handle general errors with throttling.
    /// </summary>
    private void HandleThrottledError(Exception ex)
    {
        _suppressedErrorCount++;
        _errorMetrics?.RecordError("Apollo.Execute", ex.Message);

        var now = DateTime.UtcNow;
        if ((now - _lastErrorTime).TotalSeconds >= ErrorThrottleSeconds)
        {
            _lastErrorTime = now;
            _log.Error(ex, "Apollo.Execute error (suppressed {0} errors in last {1}s)",
                _suppressedErrorCount, ErrorThrottleSeconds);
            _suppressedErrorCount = 0;
        }
    }

    /// <summary>
    /// Internal execution logic, separated for error handling.
    /// </summary>
    private unsafe void ExecuteInternal(IPlayerCharacter player)
    {
        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager == null)
            return;

        // Update GCD state
        _actionService.Update(player.IsCasting);

        // Movement detection
        var isMoving = Vector3.DistanceSquared(player.Position, _lastPosition) > 0.001f;
        _lastPosition = player.Position;

        // Combat tracking
        var inCombat = (player.StatusFlags & StatusFlags.InCombat) != 0;
        if (inCombat)
            _actionTracker.StartCombat();
        else
            _actionTracker.EndCombat();

        // Track GCD state for debug display
        if (inCombat)
        {
            _actionTracker.TrackGcdState(
                gcdReady: _actionService.CanExecuteGcd,
                _actionService.GcdRemaining,
                player.IsCasting,
                _actionService.AnimationLockRemaining > 0,
                _actionService.GcdRemaining > 0);
        }

        // Create context for modules
        var context = CreateContext(player, inCombat, isMoving);

        // Update debug state from all modules
        foreach (var module in _modules)
        {
            module.UpdateDebugState(context);
        }

        // Execute modules in priority order
        // Try oGCD modules first during weave windows
        if (inCombat && _actionService.CanExecuteOgcd)
        {
            foreach (var module in _modules)
            {
                if (module.TryExecute(context, isMoving))
                    break;
            }
        }

        // Try GCD modules when GCD is ready
        if (_actionService.CanExecuteGcd)
        {
            foreach (var module in _modules)
            {
                if (module.TryExecute(context, isMoving))
                    break;
            }
        }

        // Sync debug state from context to public properties
        SyncDebugState(context);
    }

    /// <summary>
    /// Creates the shared context for all modules.
    /// </summary>
    private ApolloContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new ApolloContext(
            player: player,
            inCombat: inCombat,
            isMoving: isMoving,
            canExecuteGcd: _actionService.CanExecuteGcd,
            canExecuteOgcd: _actionService.CanExecuteOgcd,
            actionService: _actionService,
            actionTracker: _actionTracker,
            combatEventService: _combatEventService,
            configuration: _configuration,
            debuffDetectionService: _debuffDetectionService,
            healingSpellSelector: _healingSpellSelector,
            hpPredictionService: _hpPredictionService,
            objectTable: _objectTable,
            partyList: _partyList,
            playerStatsService: _playerStatsService,
            targetingService: _targetingService,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper);
    }

    /// <summary>
    /// Syncs debug state from context to public properties.
    /// </summary>
    private void SyncDebugState(ApolloContext context)
    {
        DebugAoEInjuredCount = context.Debug.AoEInjuredCount;
        DebugAoESelectedSpell = context.Debug.AoESelectedSpell;
        DebugAoEStatus = context.Debug.AoEStatus;
        DebugPlayerHpPercent = context.Debug.PlayerHpPercent;
        DebugPartyListCount = context.Debug.PartyListCount;
        DebugPartyValidCount = context.Debug.PartyValidCount;
        DebugBattleNpcCount = context.Debug.BattleNpcCount;
        DebugNpcInfo = context.Debug.NpcInfo;
        DebugPlanningState = context.Debug.PlanningState;
        DebugPlannedAction = context.Debug.PlannedAction;
        DebugDpsState = context.Debug.DpsState;
        DebugTargetInfo = context.Debug.TargetInfo;
        DebugLastHealAmount = context.Debug.LastHealAmount;
        DebugLastHealStats = context.Debug.LastHealStats;
        DebugRaiseState = context.Debug.RaiseState;
        DebugRaiseTarget = context.Debug.RaiseTarget;
        DebugAoEDpsState = context.Debug.AoEDpsState;
        DebugAoEDpsEnemyCount = context.Debug.AoEDpsEnemyCount;
        DebugAsylumState = context.Debug.AsylumState;
        DebugAsylumTarget = context.Debug.AsylumTarget;
        DebugThinAirState = context.Debug.ThinAirState;
        DebugDefensiveState = context.Debug.DefensiveState;
        DebugTemperanceState = context.Debug.TemperanceState;
        DebugLilyCount = context.Debug.LilyCount;
        DebugBloodLilyCount = context.Debug.BloodLilyCount;
        DebugLilyStrategy = context.Debug.LilyStrategy;
        DebugSacredSightStacks = context.Debug.SacredSightStacks;
        DebugMiseryState = context.Debug.MiseryState;
        DebugEsunaState = context.Debug.EsunaState;
        DebugEsunaTarget = context.Debug.EsunaTarget;
        DebugSurecastState = context.Debug.SurecastState;
    }
}
