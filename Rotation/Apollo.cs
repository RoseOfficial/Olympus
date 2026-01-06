using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
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
    private readonly IDamageIntakeService _damageIntakeService;
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
    private int _suppressedErrorCount;

    // Helpers (shared across modules)
    private readonly StatusHelper _statusHelper;
    private readonly PartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IApolloModule> _modules;

    // Movement detection
    private Vector3 _lastPosition;
    private DateTime _lastMovementTime = DateTime.MinValue;

    // Persistent debug state (shared with contexts, exposed for DebugService)
    private readonly DebugState _debugState = new();

    /// <summary>
    /// Gets the current debug state. Used by DebugService to display debug information.
    /// </summary>
    public DebugState DebugState => _debugState;

    public Apollo(
        IPluginLog log,
        ActionTracker actionTracker,
        CombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
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
        _damageIntakeService = damageIntakeService;
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
        if ((now - _lastErrorTime).TotalSeconds >= FFXIVTimings.ErrorThrottleSeconds)
        {
            _lastErrorTime = now;
            _log.Error(ex, "Apollo.Execute error (suppressed {0} errors in last {1}s)",
                _suppressedErrorCount, FFXIVTimings.ErrorThrottleSeconds);
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

        // Movement detection with configurable grace period
        var positionChanged = Vector3.DistanceSquared(player.Position, _lastPosition) > FFXIVTimings.MovementThresholdSquared;
        _lastPosition = player.Position;

        // Track when we last detected actual movement
        if (positionChanged)
            _lastMovementTime = DateTime.UtcNow;

        // Consider player as "moving" if position changed OR within grace period after stopping
        // This prevents stutter-casting when player briefly stops during movement
        var timeSinceMovement = (DateTime.UtcNow - _lastMovementTime).TotalSeconds;
        var isMoving = positionChanged || timeSinceMovement < _configuration.MovementTolerance;

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

        // Update debug state from all modules (skip if debug window closed for performance)
        if (_configuration.IsDebugWindowOpen)
        {
            foreach (var module in _modules)
            {
                module.UpdateDebugState(context);
            }
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
            damageIntakeService: _damageIntakeService,
            configuration: _configuration,
            debuffDetectionService: _debuffDetectionService,
            healingSpellSelector: _healingSpellSelector,
            hpPredictionService: _hpPredictionService,
            objectTable: _objectTable,
            partyList: _partyList,
            playerStatsService: _playerStatsService,
            targetingService: _targetingService,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            debugState: _debugState);
    }
}
