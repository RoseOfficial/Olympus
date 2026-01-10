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
using Olympus.Rotation.Tank;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Rotation.ThemisCore.Helpers;
using Olympus.Rotation.ThemisCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;

namespace Olympus.Rotation;

/// <summary>
/// Paladin rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Themis, the Greek goddess of divine law and order.
/// </summary>
public sealed class Themis : ITankRotation
{
    /// <inheritdoc />
    public string Name => "Themis";

    /// <inheritdoc />
    public uint[] SupportedJobIds => [JobRegistry.Paladin, JobRegistry.Gladiator];

    /// <inheritdoc />
    public bool IsMainTank { get; private set; }

    /// <inheritdoc />
    public int GaugeValue { get; private set; }

    // Services
    private readonly IPluginLog _log;
    private readonly ActionTracker _actionTracker;
    private readonly ICombatEventService _combatEventService;
    private readonly IDamageIntakeService _damageIntakeService;
    private readonly IDamageTrendService _damageTrendService;
    private readonly IMpForecastService _mpForecastService;
    private readonly Configuration _configuration;
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;
    private readonly ITargetingService _targetingService;
    private readonly IHpPredictionService _hpPredictionService;
    private readonly ActionService _actionService;
    private readonly IPlayerStatsService _playerStatsService;
    private readonly IDebuffDetectionService _debuffDetectionService;
    private readonly IEnmityService _enmityService;
    private readonly ITankCooldownService _tankCooldownService;
    private readonly IErrorMetricsService? _errorMetrics;

    // Frame-scoped caching for performance optimization
    private readonly FrameScopedCache _frameCache = new();

    // Error throttling to avoid log spam
    private DateTime _lastErrorTime = DateTime.MinValue;
    private int _suppressedErrorCount;

    // Helpers (shared across modules)
    private readonly ThemisStatusHelper _statusHelper;
    private readonly ThemisPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IThemisModule> _modules;

    // Movement detection
    private Vector3 _lastPosition;
    private DateTime _lastMovementTime = DateTime.MinValue;

    // Persistent debug state (shared with contexts, exposed for DebugService)
    private readonly ThemisDebugState _themisDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    /// <summary>
    /// Gets the Themis-specific debug state. Used for Paladin-specific debug display.
    /// </summary>
    public ThemisDebugState ThemisDebug => _themisDebugState;

    /// <summary>
    /// Gets the current debug state. Used by DebugService to display debug information.
    /// </summary>
    public DebugState DebugState => _debugState;

    public Themis(
        IPluginLog log,
        ActionTracker actionTracker,
        ICombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
        Configuration configuration,
        IObjectTable objectTable,
        IPartyList partyList,
        ITargetingService targetingService,
        IHpPredictionService hpPredictionService,
        ActionService actionService,
        IPlayerStatsService playerStatsService,
        IDebuffDetectionService debuffDetectionService,
        IEnmityService enmityService,
        ITankCooldownService tankCooldownService,
        IErrorMetricsService? errorMetrics = null)
    {
        _log = log;
        _actionTracker = actionTracker;
        _combatEventService = combatEventService;
        _damageIntakeService = damageIntakeService;
        _damageTrendService = new DamageTrendService(damageIntakeService);
        _mpForecastService = new MpForecastService();
        _configuration = configuration;
        _objectTable = objectTable;
        _partyList = partyList;
        _targetingService = targetingService;
        _hpPredictionService = hpPredictionService;
        _actionService = actionService;
        _playerStatsService = playerStatsService;
        _debuffDetectionService = debuffDetectionService;
        _enmityService = enmityService;
        _tankCooldownService = tankCooldownService;
        _errorMetrics = errorMetrics;

        // Initialize helpers
        _statusHelper = new ThemisStatusHelper();
        _partyHelper = new ThemisPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IThemisModule>
        {
            new EnmityModule(),     // Priority 5 - Enmity management is critical
            new MitigationModule(), // Priority 10 - Stay alive
            new BuffModule(),       // Priority 20 - Buff management
            new DamageModule(),     // Priority 30 - DPS rotation
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
            _errorMetrics?.RecordError("Themis.Execute.NullRef", ex.Message);
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
        _log.Error(ex, "Themis DISABLED due to {0} - memory access error", errorType);
        _errorMetrics?.RecordError($"Themis.Execute.{errorType}", ex.Message);
    }

    /// <summary>
    /// Handle general errors with throttling.
    /// </summary>
    private void HandleThrottledError(Exception ex)
    {
        _suppressedErrorCount++;
        _errorMetrics?.RecordError("Themis.Execute", ex.Message);

        var now = DateTime.UtcNow;
        if ((now - _lastErrorTime).TotalSeconds >= FFXIVTimings.ErrorThrottleSeconds)
        {
            _lastErrorTime = now;
            _log.Error(ex, "Themis.Execute error (suppressed {0} errors in last {1}s)",
                _suppressedErrorCount, FFXIVTimings.ErrorThrottleSeconds);
            _suppressedErrorCount = 0;
        }
    }

    /// <summary>
    /// Internal execution logic, separated for error handling.
    /// </summary>
    private unsafe void ExecuteInternal(IPlayerCharacter player)
    {
        // Invalidate frame cache at start of each frame
        _frameCache.InvalidateAll();

        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager == null)
            return;

        // Update GCD state
        _actionService.Update(player.IsCasting);

        // Read job gauge
        GaugeValue = SafeGameAccess.GetPldOathGauge(_errorMetrics);

        // Read combo state
        var comboAction = SafeGameAccess.GetComboAction(_errorMetrics);
        var comboTimer = SafeGameAccess.GetComboTimer(_errorMetrics);

        // Determine combo step from current combo action
        int comboStep = DetermineComboStep(comboAction, comboTimer);

        // Movement detection with configurable grace period
        var positionChanged = Vector3.DistanceSquared(player.Position, _lastPosition) > FFXIVTimings.MovementThresholdSquared;
        _lastPosition = player.Position;

        // Track when we last detected actual movement
        if (positionChanged)
            _lastMovementTime = DateTime.UtcNow;

        // Consider player as "moving" if position changed OR within grace period after stopping
        var timeSinceMovement = (DateTime.UtcNow - _lastMovementTime).TotalSeconds;
        var isMoving = positionChanged || timeSinceMovement < _configuration.MovementTolerance;

        // Combat tracking
        var inCombat = (player.StatusFlags & StatusFlags.InCombat) != 0;
        if (inCombat)
            _actionTracker.StartCombat();
        else
            _actionTracker.EndCombat();

        // Update damage trend service with delta time and player entity ID
        if (inCombat)
        {
            var entityIds = new List<uint> { player.EntityId };
            (_damageTrendService as DamageTrendService)?.Update(1f / 60f, entityIds);
        }

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
        var context = CreateContext(player, inCombat, isMoving, comboStep, comboAction, comboTimer);

        // Update main tank status for ITankRotation interface
        IsMainTank = context.IsMainTank;

        // Update debug state from all modules (skip if debug window closed for performance)
        if (_configuration.IsDebugWindowOpen)
        {
            foreach (var module in _modules)
            {
                module.UpdateDebugState(context);
            }

            // Sync Themis debug state to IRotation debug state
            SyncDebugState(context);
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
    /// Determines the current combo step based on the last combo action.
    /// </summary>
    private int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // No combo active
        if (comboAction == 0 || comboTimer <= 0)
            return 1;

        // Check for single-target combo
        if (comboAction == PLDActions.FastBlade.ActionId)
            return 2; // Ready for Riot Blade

        if (comboAction == PLDActions.RiotBlade.ActionId)
            return 3; // Ready for Royal Authority / Rage of Halone

        // Check for AoE combo
        if (comboAction == PLDActions.TotalEclipse.ActionId)
            return 2; // Ready for Prominence

        // Unknown combo action, restart
        return 1;
    }

    /// <summary>
    /// Creates the shared context for all modules.
    /// </summary>
    private ThemisContext CreateContext(
        IPlayerCharacter player,
        bool inCombat,
        bool isMoving,
        int comboStep,
        uint lastComboAction,
        float comboTimeRemaining)
    {
        return new ThemisContext(
            player: player,
            inCombat: inCombat,
            isMoving: isMoving,
            canExecuteGcd: _actionService.CanExecuteGcd,
            canExecuteOgcd: _actionService.CanExecuteOgcd,
            actionService: _actionService,
            actionTracker: _actionTracker,
            combatEventService: _combatEventService,
            damageIntakeService: _damageIntakeService,
            damageTrendService: _damageTrendService,
            frameCache: _frameCache,
            configuration: _configuration,
            debuffDetectionService: _debuffDetectionService,
            hpPredictionService: _hpPredictionService,
            mpForecastService: _mpForecastService,
            playerStatsService: _playerStatsService,
            targetingService: _targetingService,
            objectTable: _objectTable,
            partyList: _partyList,
            enmityService: _enmityService,
            tankCooldownService: _tankCooldownService,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            debugState: _themisDebugState,
            oathGauge: GaugeValue,
            comboStep: comboStep,
            lastComboAction: lastComboAction,
            comboTimeRemaining: comboTimeRemaining,
            log: _log);
    }

    /// <summary>
    /// Syncs Themis-specific debug state to IRotation-compatible debug state.
    /// </summary>
    private void SyncDebugState(ThemisContext context)
    {
        // Map tank debug state to common debug state fields
        _debugState.PlanningState = _themisDebugState.DamageState;
        _debugState.PlannedAction = _themisDebugState.PlannedAction;
        _debugState.DpsState = _themisDebugState.DamageState;
        _debugState.DefensiveState = _themisDebugState.MitigationState;

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }
}
