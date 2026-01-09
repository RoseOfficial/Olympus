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
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Helpers;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Cache;
using Olympus.Services.Targeting;
using Olympus.Services.Scholar;

namespace Olympus.Rotation;

/// <summary>
/// Scholar rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Athena, the Greek goddess of wisdom and strategic warfare.
/// </summary>
public sealed class Athena : IRotation
{
    /// <inheritdoc />
    public string Name => "Athena";

    /// <inheritdoc />
    public uint[] SupportedJobIds => [JobRegistry.Scholar, JobRegistry.Arcanist];

    // Services
    private readonly IPluginLog _log;
    private readonly ActionTracker _actionTracker;
    private readonly CombatEventService _combatEventService;
    private readonly IDamageIntakeService _damageIntakeService;
    private readonly IDamageTrendService _damageTrendService;
    private readonly IMpForecastService _mpForecastService;
    private readonly Configuration _configuration;
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;
    private readonly TargetingService _targetingService;
    private readonly HpPredictionService _hpPredictionService;
    private readonly ActionService _actionService;
    private readonly PlayerStatsService _playerStatsService;
    private readonly DebuffDetectionService _debuffDetectionService;
    private readonly ICooldownPlanner _cooldownPlanner;
    private readonly IErrorMetricsService? _errorMetrics;

    // Scholar-specific services
    private readonly AetherflowTrackingService _aetherflowService;
    private readonly FairyGaugeService _fairyGaugeService;
    private readonly FairyStateManager _fairyStateManager;

    // Smart healing services
    private readonly CoHealerDetectionService _coHealerDetectionService;
    private readonly BossMechanicDetector _bossMechanicDetector;
    private readonly ShieldTrackingService _shieldTrackingService;

    // Frame-scoped caching
    private readonly FrameScopedCache _frameCache = new();

    // Error throttling
    private DateTime _lastErrorTime = DateTime.MinValue;
    private int _suppressedErrorCount;

    // Helpers
    private readonly AthenaStatusHelper _statusHelper;
    private readonly AthenaPartyHelper _partyHelper;

    // Modules (sorted by priority)
    private readonly List<IAthenaModule> _modules;

    // Movement detection
    private Vector3 _lastPosition;
    private DateTime _lastMovementTime = DateTime.MinValue;

    // Debug state
    private readonly AthenaDebugState _debugState = new();

    /// <summary>
    /// Gets the Apollo-compatible debug state for UI compatibility.
    /// </summary>
    public DebugState DebugState => ConvertToApolloDebugState();

    /// <summary>
    /// Gets the Athena-specific debug state.
    /// </summary>
    public AthenaDebugState AthenaDebug => _debugState;

    public Athena(
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
        DebuffDetectionService debuffDetectionService,
        ICooldownPlanner cooldownPlanner,
        ShieldTrackingService shieldTrackingService,
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
        _cooldownPlanner = cooldownPlanner;
        _shieldTrackingService = shieldTrackingService;
        _errorMetrics = errorMetrics;

        // Initialize Scholar-specific services
        _aetherflowService = new AetherflowTrackingService();
        _fairyGaugeService = new FairyGaugeService();
        _fairyStateManager = new FairyStateManager(objectTable);

        // Initialize helpers
        _statusHelper = new AthenaStatusHelper();
        _partyHelper = new AthenaPartyHelper(objectTable, partyList, hpPredictionService, configuration, _statusHelper);

        // Initialize smart healing services
        _coHealerDetectionService = new CoHealerDetectionService(
            combatEventService, partyList, objectTable, configuration.Healing);
        _bossMechanicDetector = new BossMechanicDetector(
            configuration.Healing, combatEventService, damageIntakeService);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IAthenaModule>
        {
            new FairyModule(),           // Priority 3 - Summon fairy if needed
            new ResurrectionModule(),    // Priority 5 - Dead members are useless
            new HealingModule(),         // Priority 10 - Keep party alive
            new DefensiveModule(),       // Priority 20 - Mitigation
            new BuffModule(),            // Priority 30 - Buffs and utilities
            new DamageModule(),          // Priority 50 - DPS when safe
        };

        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        _log.Info("Athena (Scholar) rotation initialized");
    }

    /// <summary>
    /// Main execution loop - called every frame.
    /// </summary>
    public void Execute(IPlayerCharacter player)
    {
        try
        {
            ExecuteInternal(player);
        }
        catch (SEHException ex)
        {
            HandleCriticalError("SEHException", ex);
        }
        catch (AccessViolationException ex)
        {
            HandleCriticalError("AccessViolation", ex);
        }
        catch (NullReferenceException ex)
        {
            _errorMetrics?.RecordError("Athena.Execute.NullRef", ex.Message);
            _suppressedErrorCount++;
        }
        catch (Exception ex)
        {
            HandleThrottledError(ex);
        }
    }

    private void HandleCriticalError(string errorType, Exception ex)
    {
        _configuration.Enabled = false;
        _log.Error(ex, "Athena DISABLED due to {0} - memory access error", errorType);
        _errorMetrics?.RecordError($"Athena.Execute.{errorType}", ex.Message);
    }

    private void HandleThrottledError(Exception ex)
    {
        _suppressedErrorCount++;
        _errorMetrics?.RecordError("Athena.Execute", ex.Message);

        var now = DateTime.UtcNow;
        if ((now - _lastErrorTime).TotalSeconds >= FFXIVTimings.ErrorThrottleSeconds)
        {
            _lastErrorTime = now;
            _log.Error(ex, "Athena.Execute error (suppressed {0} errors in last {1}s)",
                _suppressedErrorCount, FFXIVTimings.ErrorThrottleSeconds);
            _suppressedErrorCount = 0;
        }
    }

    private unsafe void ExecuteInternal(IPlayerCharacter player)
    {
        // Invalidate frame cache
        _frameCache.InvalidateAll();

        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager == null)
            return;

        // Update GCD state
        _actionService.Update(player.IsCasting);

        // Update MP forecast
        _mpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            _statusHelper.HasLucidDreaming(player));

        // Movement detection
        var positionChanged = Vector3.DistanceSquared(player.Position, _lastPosition) > FFXIVTimings.MovementThresholdSquared;
        _lastPosition = player.Position;

        if (positionChanged)
            _lastMovementTime = DateTime.UtcNow;

        var timeSinceMovement = (DateTime.UtcNow - _lastMovementTime).TotalSeconds;
        var isMoving = positionChanged || timeSinceMovement < _configuration.MovementTolerance;

        // Combat tracking
        var inCombat = (player.StatusFlags & StatusFlags.InCombat) != 0;
        if (inCombat)
            _actionTracker.StartCombat();
        else
            _actionTracker.EndCombat();

        _combatEventService.UpdateCombatState(inCombat);

        // Update smart healing services
        _shieldTrackingService.Update();
        _coHealerDetectionService.Update(player.EntityId);
        _bossMechanicDetector.Update();

        // Update damage trend service
        if (inCombat)
        {
            var partyEntityIds = new List<uint>();
            foreach (var member in _partyHelper.GetAllPartyMembers(player))
            {
                partyEntityIds.Add(member.EntityId);
            }
            (_damageTrendService as DamageTrendService)?.Update(1f / 60f, partyEntityIds);

            var (avgHpPercent, lowestHpPercent, injuredCount) = _partyHelper.CalculatePartyHealthMetrics(player);
            var criticalCount = lowestHpPercent < 0.30f ? Math.Max(1, injuredCount / 2) : 0;
            _cooldownPlanner.Update(avgHpPercent, lowestHpPercent, injuredCount, criticalCount);
        }

        // Track GCD state for debug
        if (inCombat)
        {
            _actionTracker.TrackGcdState(
                gcdReady: _actionService.CanExecuteGcd,
                _actionService.GcdRemaining,
                player.IsCasting,
                _actionService.AnimationLockRemaining > 0,
                _actionService.GcdRemaining > 0);
        }

        // Update debug state
        _debugState.AetherflowStacks = _aetherflowService.CurrentStacks;
        _debugState.FairyGauge = _fairyGaugeService.CurrentGauge;
        _debugState.FairyState = _fairyStateManager.CurrentState.ToString();
        _debugState.PlayerHpPercent = player.MaxHp > 0 ? (float)player.CurrentHp / player.MaxHp : 1f;

        // Create context for modules
        var context = CreateContext(player, inCombat, isMoving);

        // Update debug state from all modules
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

    private AthenaContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new AthenaContext(
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
            objectTable: _objectTable,
            partyList: _partyList,
            playerStatsService: _playerStatsService,
            targetingService: _targetingService,
            aetherflowService: _aetherflowService,
            fairyGaugeService: _fairyGaugeService,
            fairyStateManager: _fairyStateManager,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            cooldownPlanner: _cooldownPlanner,
            coHealerDetectionService: _coHealerDetectionService,
            bossMechanicDetector: _bossMechanicDetector,
            shieldTrackingService: _shieldTrackingService,
            debugState: _debugState,
            log: _log);
    }

    /// <summary>
    /// Converts Athena debug state to Apollo debug state for UI compatibility.
    /// </summary>
    private DebugState ConvertToApolloDebugState()
    {
        return new DebugState
        {
            PlanningState = _debugState.PlanningState,
            PlannedAction = _debugState.PlannedAction,
            AoEInjuredCount = _debugState.AoEInjuredCount,
            AoEStatus = _debugState.AoEHealState,
            PlayerHpPercent = _debugState.PlayerHpPercent,
            PartyListCount = _debugState.PartyListCount,
            PartyValidCount = _debugState.PartyValidCount,
            DpsState = _debugState.DpsState,
            AoEDpsState = _debugState.AoEDpsState,
            AoEDpsEnemyCount = _debugState.AoEDpsEnemyCount,
            LastHealAmount = _debugState.LastHealAmount,
            LastHealStats = _debugState.LastHealStats,
            RaiseState = _debugState.RaiseState,
            RaiseTarget = _debugState.RaiseTarget,
            EsunaState = _debugState.EsunaState,
            EsunaTarget = _debugState.EsunaTarget,
            LucidState = _debugState.LucidState,
            // Scholar-specific mappings
            LilyCount = _debugState.AetherflowStacks, // Map Aetherflow to Lily for display
            BloodLilyCount = _debugState.FairyGauge / 33, // Rough conversion for display
            LilyStrategy = _debugState.AetherflowState,
        };
    }
}
