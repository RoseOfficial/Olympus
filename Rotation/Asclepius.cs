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
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Sage;
using Olympus.Services.Stats;
using Olympus.Services.Cache;
using Olympus.Services.Targeting;

namespace Olympus.Rotation;

/// <summary>
/// Sage rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Asclepius, the Greek god of medicine.
/// </summary>
public sealed class Asclepius : IRotation
{
    /// <inheritdoc />
    public string Name => "Asclepius";

    /// <inheritdoc />
    public uint[] SupportedJobIds => [JobRegistry.Sage];

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
    private readonly HealingSpellSelector _healingSpellSelector;
    private readonly IErrorMetricsService? _errorMetrics;

    // Sage-specific services
    private readonly IAddersgallTrackingService _addersgallService;
    private readonly IAdderstingTrackingService _adderstingService;
    private readonly IKardiaManager _kardiaManager;
    private readonly IEukrasiaStateService _eukrasiaService;

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
    private readonly AsclepiusStatusHelper _statusHelper;
    private readonly IPartyHelper _partyHelper;

    // Modules (sorted by priority)
    private readonly List<IAsclepiusModule> _modules;

    // Movement detection
    private Vector3 _lastPosition;
    private DateTime _lastMovementTime = DateTime.MinValue;

    // Debug state
    private readonly AsclepiusDebugState _debugState = new();

    /// <summary>
    /// Gets the Apollo-compatible debug state for UI compatibility.
    /// </summary>
    public DebugState DebugState => ConvertToApolloDebugState();

    /// <summary>
    /// Gets the Asclepius-specific debug state.
    /// </summary>
    public AsclepiusDebugState AsclepiusDebug => _debugState;

    public Asclepius(
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
        HealingSpellSelector healingSpellSelector,
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
        _healingSpellSelector = healingSpellSelector;
        _shieldTrackingService = shieldTrackingService;
        _errorMetrics = errorMetrics;

        // Initialize Sage-specific services
        _addersgallService = new AddersgallTrackingService();
        _adderstingService = new AdderstingTrackingService();
        _kardiaManager = new KardiaManager(partyList);
        _eukrasiaService = new EukrasiaStateService();

        // Initialize helpers
        _statusHelper = new AsclepiusStatusHelper();
        _partyHelper = new PartyHelper(objectTable, partyList, hpPredictionService, configuration);

        // Initialize smart healing services
        _coHealerDetectionService = new CoHealerDetectionService(
            combatEventService, partyList, objectTable, configuration.Healing);
        _bossMechanicDetector = new BossMechanicDetector(
            configuration.Healing, combatEventService, damageIntakeService);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IAsclepiusModule>
        {
            new KardiaModule(),         // Priority 3 - Ensure Kardia is placed
            new ResurrectionModule(),   // Priority 5 - Raise dead party members
            new HealingModule(),        // Priority 10 - Addersgall heals, oGCDs, GCD heals
            new DamageModule(),         // Priority 50 - DoT, Dosis, Phlegma, Psyche
        };

        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        _log.Info("Asclepius (Sage) rotation initialized");
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
            _errorMetrics?.RecordError("Asclepius.Execute.NullRef", ex.Message);
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
        _log.Error(ex, "Asclepius DISABLED due to {0} - memory access error", errorType);
        _errorMetrics?.RecordError($"Asclepius.Execute.{errorType}", ex.Message);
    }

    private void HandleThrottledError(Exception ex)
    {
        _suppressedErrorCount++;
        _errorMetrics?.RecordError("Asclepius.Execute", ex.Message);

        var now = DateTime.UtcNow;
        if ((now - _lastErrorTime).TotalSeconds >= FFXIVTimings.ErrorThrottleSeconds)
        {
            _lastErrorTime = now;
            _log.Error(ex, "Asclepius.Execute error (suppressed {0} errors in last {1}s)",
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

        // Update Kardia target tracking
        _kardiaManager.UpdateKardiaTarget(player);

        // Update MP forecast
        _mpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            AsclepiusStatusHelper.HasLucidDreaming(player));

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
        _debugState.AddersgallStacks = _addersgallService.CurrentStacks;
        _debugState.AddersgallTimer = _addersgallService.TimerRemaining;
        _debugState.AdderstingStacks = _adderstingService.CurrentStacks;
        _debugState.EukrasiaActive = _eukrasiaService.IsEukrasiaActive(player);
        _debugState.ZoeActive = _eukrasiaService.IsZoeActive(player);
        _debugState.KardiaTarget = _kardiaManager.HasKardia ? $"ID: {_kardiaManager.CurrentKardiaTarget}" : "None";
        _debugState.SoteriaStacks = _kardiaManager.GetSoteriaStacks(player);
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

    private AsclepiusContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new AsclepiusContext(
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
            healingSpellSelector: _healingSpellSelector,
            hpPredictionService: _hpPredictionService,
            mpForecastService: _mpForecastService,
            objectTable: _objectTable,
            partyList: _partyList,
            playerStatsService: _playerStatsService,
            targetingService: _targetingService,
            addersgallService: _addersgallService,
            adderstingService: _adderstingService,
            kardiaManager: _kardiaManager,
            eukrasiaService: _eukrasiaService,
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
    /// Converts Asclepius debug state to Apollo debug state for UI compatibility.
    /// </summary>
    private DebugState ConvertToApolloDebugState()
    {
        return new DebugState
        {
            PlanningState = _debugState.PlanningState,
            PlannedAction = _debugState.PlannedAction,
            AoEInjuredCount = _debugState.AoEInjuredCount,
            AoEStatus = _debugState.AoEStatus,
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
            // SGE-specific mappings for display compatibility
            LilyCount = _debugState.AddersgallStacks, // Map Addersgall to Lily count for display
            BloodLilyCount = _debugState.AdderstingStacks, // Map Addersting to Blood Lily
            LilyStrategy = _debugState.AddersgallStrategy,
        };
    }
}
