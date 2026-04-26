using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;

namespace Olympus.Rotation.Base;

/// <summary>
/// Base class for all rotation implementations.
/// Provides shared error handling, movement detection, and module execution patterns.
/// </summary>
/// <typeparam name="TContext">The job-specific context type.</typeparam>
/// <typeparam name="TModule">The job-specific module interface type.</typeparam>
public abstract class BaseRotation<TContext, TModule> : IRotation, IDisposable
    where TContext : IRotationContext
    where TModule : IRotationModule<TContext>
{
    #region Abstract Members

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract uint[] SupportedJobIds { get; }

    /// <inheritdoc />
    public abstract DebugState DebugState { get; }

    /// <summary>
    /// Gets the list of modules for this rotation, sorted by priority (lower = higher priority).
    /// </summary>
    protected abstract List<TModule> Modules { get; }

    /// <summary>
    /// Creates the job-specific context for module execution.
    /// </summary>
    protected abstract TContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving);

    /// <summary>
    /// Performs job-specific service updates before module execution.
    /// </summary>
    protected abstract void UpdateJobSpecificServices(IPlayerCharacter player, bool inCombat);

    #endregion

    #region Protected Fields (accessible by derived classes)

    protected readonly IPluginLog Log;
    protected readonly Configuration Configuration;
    protected readonly ActionService ActionService;
    protected readonly IActionTracker ActionTracker;
    protected readonly ICombatEventService CombatEventService;
    protected readonly IDamageIntakeService DamageIntakeService;
    protected readonly IDamageTrendService DamageTrendService;
    protected readonly IMpForecastService MpForecastService;
    protected readonly IObjectTable ObjectTable;
    protected readonly IPartyList PartyList;
    protected readonly ITargetingService TargetingService;
    protected readonly IHpPredictionService HpPredictionService;
    protected readonly IPlayerStatsService PlayerStatsService;
    protected readonly IDebuffDetectionService DebuffDetectionService;
    protected readonly IErrorMetricsService? ErrorMetrics;
    protected readonly IBurstWindowService? BurstWindowService;
    protected readonly Olympus.Services.Consumables.ITinctureDispatcher? TinctureDispatcher;
    protected readonly Olympus.Rotation.Common.Modules.PrePullModule? PrePullModule;
    protected readonly FrameScopedCache FrameCache = new();

    #endregion

    #region Private Fields

    // Error throttling to avoid log spam
    private DateTime _lastErrorTime = DateTime.MinValue;
    private int _suppressedErrorCount;

    // Pre-computed error key strings to avoid per-error allocations
    private string? _errorKeySeh;
    private string? _errorKeyNullRef;
    private string? _errorKeyGeneral;

    // Movement detection
    private Vector3 _lastPosition;
    private DateTime _lastMovementTime = DateTime.MinValue;

    // Cached timestamp for current frame — set once at start of ExecuteInternal
    protected DateTime FrameTimestamp;

    #endregion

    #region Constructor

    protected BaseRotation(
        IPluginLog log,
        IActionTracker actionTracker,
        ICombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
        IDamageTrendService damageTrendService,
        Configuration configuration,
        IObjectTable objectTable,
        IPartyList partyList,
        ITargetingService targetingService,
        IHpPredictionService hpPredictionService,
        ActionService actionService,
        IPlayerStatsService playerStatsService,
        IDebuffDetectionService debuffDetectionService,
        IErrorMetricsService? errorMetrics = null,
        IMpForecastService? mpForecastService = null,
        IBurstWindowService? burstWindowService = null,
        Olympus.Services.Consumables.ITinctureDispatcher? tinctureDispatcher = null,
        Olympus.Services.Pull.IPullIntentService? pullIntentService = null)
    {
        Log = log;
        ActionTracker = actionTracker;
        CombatEventService = combatEventService;
        DamageIntakeService = damageIntakeService;
        DamageTrendService = damageTrendService;
        MpForecastService = mpForecastService ?? new MpForecastService();
        Configuration = configuration;
        ObjectTable = objectTable;
        PartyList = partyList;
        TargetingService = targetingService;
        HpPredictionService = hpPredictionService;
        ActionService = actionService;
        PlayerStatsService = playerStatsService;
        DebuffDetectionService = debuffDetectionService;
        ErrorMetrics = errorMetrics;
        BurstWindowService = burstWindowService;
        TinctureDispatcher = tinctureDispatcher;

        // Construct PrePullModule with TinctureCandidate when both deps are available.
        // Future per-job pre-pull weaves register additional candidates in concrete rotations.
        if (tinctureDispatcher is not null && pullIntentService is not null)
        {
            PrePullModule = new Olympus.Rotation.Common.Modules.PrePullModule(pullIntentService);
            PrePullModule.Register(new Olympus.Rotation.Common.Modules.TinctureCandidate(tinctureDispatcher));
        }
    }

    #endregion

    #region IRotation Implementation

    /// <summary>
    /// Main execution loop - called every frame.
    /// Handles error recovery and delegates to ExecuteInternal.
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
            HandleNullReferenceError(ex);
        }
        catch (Exception ex)
        {
            // General error - throttled logging
            HandleThrottledError(ex);
        }
    }

    #endregion

    #region Core Execution

    /// <summary>
    /// Internal execution logic. Override in derived classes for job-specific behavior.
    /// Uses unsafe context for game memory access.
    /// </summary>
    protected virtual unsafe void ExecuteInternal(IPlayerCharacter player)
    {
        // Cache timestamp once per frame for all consumers
        FrameTimestamp = DateTime.UtcNow;

        // Invalidate frame cache at start of each frame
        FrameCache.InvalidateAll();

        var actionManager = SafeGameAccess.GetActionManager(ErrorMetrics);
        if (actionManager == null)
            return;

        // Update GCD state
        ActionService.Update(player.IsCasting);

        // Update MP forecast service with current state
        UpdateMpForecast(player);

        // Movement detection
        var (isMoving, _) = UpdateMovement(player);

        // Combat tracking — also treat auto-attack as combat if enabled
        var inCombat = (player.StatusFlags & StatusFlags.InCombat) != 0;
        if (!inCombat && Configuration.EnableOnAutoAttack)
            inCombat = IsAutoAttacking();
        UpdateCombatState(inCombat);

        // Job-specific service updates
        UpdateJobSpecificServices(player, inCombat);

        // Track GCD state for debug display
        if (inCombat)
        {
            TrackGcdState(player);
        }

        // Create context for modules
        var context = CreateContext(player, inCombat, isMoving);

        // Update debug state from all modules (skip if debug window closed for performance)
        UpdateModuleDebugStates(context);

        // Execute modules in priority order
        ExecuteModules(context, isMoving, inCombat);
    }

    /// <summary>
    /// Updates MP forecast service with current player MP state.
    /// Override to provide job-specific Lucid Dreaming detection.
    /// </summary>
    protected abstract void UpdateMpForecast(IPlayerCharacter player);

    /// <summary>
    /// Updates movement detection with configurable grace period.
    /// </summary>
    /// <returns>Tuple of (isMoving, positionChanged)</returns>
    protected (bool isMoving, bool positionChanged) UpdateMovement(IPlayerCharacter player)
    {
        var positionChanged = Vector3.DistanceSquared(player.Position, _lastPosition) > FFXIVTimings.MovementThresholdSquared;
        _lastPosition = player.Position;

        // Track when we last detected actual movement
        if (positionChanged)
            _lastMovementTime = FrameTimestamp;

        // Consider player as "moving" if position changed OR within grace period after stopping
        // This prevents stutter-casting when player briefly stops during movement
        var timeSinceMovement = (FrameTimestamp - _lastMovementTime).TotalSeconds;
        var isMoving = positionChanged || timeSinceMovement < Configuration.MovementTolerance;

        return (isMoving, positionChanged);
    }

    /// <summary>
    /// Updates combat state tracking.
    /// </summary>
    protected virtual void UpdateCombatState(bool inCombat)
    {
        if (inCombat)
            ActionTracker.StartCombat();
        else
            ActionTracker.EndCombat();
    }

    /// <summary>
    /// Tracks GCD state for debug display and downtime categorization.
    /// </summary>
    protected virtual void TrackGcdState(IPlayerCharacter player)
    {
        // Check for incapacitation buffs (Willful, Stun, Sleep, etc.)
        var canAct = true;
        foreach (var status in player.StatusList)
        {
            if (FFXIVConstants.IncapacitationStatusIds.Contains(status.StatusId))
            {
                canAct = false;
                break;
            }
        }

        ActionTracker.TrackGcdState(
            gcdReady: ActionService.CanExecuteGcd,
            ActionService.GcdRemaining,
            player.IsCasting,
            ActionService.AnimationLockRemaining > 0,
            ActionService.GcdRemaining > 0,
            playerAlive: canAct,
            playerPosition: player.Position,
            inMechanicWindow: false);
    }

    /// <summary>
    /// Updates debug state from all modules.
    /// </summary>
    protected virtual void UpdateModuleDebugStates(TContext context)
    {
        if (!Configuration.IsDebugWindowOpen)
            return;

        foreach (var module in Modules)
        {
            module.UpdateDebugState(context);
        }
    }

    /// <summary>
    /// Executes modules in priority order for both oGCD and GCD windows.
    /// </summary>
    protected virtual void ExecuteModules(TContext context, bool isMoving, bool inCombat)
    {
        // Hard pause: Pyretic-style debuff active — any GCD or oGCD kills the player.
        if (Configuration.Targeting.PauseAllOnStandStillPunisher
            && PlayerSafetyHelper.IsStandStillPunisherActive(context.Player))
        {
            return;
        }

        // Hard pause: player is holding a channel/stance.
        if (Configuration.Targeting.PauseOnPlayerChannel
            && PlayerSafetyHelper.IsPlayerIntentChannelActive(context.Player))
        {
            return;
        }

        var playerJobId = context.Player.ClassJob.RowId;

        // Pre-pull phase (Path 1): only runs when PullIntent != None. Currently dispatches
        // tincture for opener; future per-job pre-pull weaves register additional candidates.
        if (PrePullModule is not null
            && ActionService.CanExecuteOgcd
            && PrePullModule.TryDispatch(playerJobId, context))
        {
            return;
        }

        // In-combat re-pot phase (Path 2): direct tincture push during regular oGCD pass.
        // Same IConsumableService gate as Path 1 (different prePullPhase flag), so the
        // shared recast cooldown prevents double-firing across both paths.
        if (inCombat
            && ActionService.CanExecuteOgcd
            && TinctureDispatcher is not null
            && TinctureDispatcher.TryDispatch(playerJobId, inCombat: true, prePullPhase: false))
        {
            return;
        }

        // Try oGCD modules first during weave windows
        if (inCombat && ActionService.CanExecuteOgcd)
        {
            foreach (var module in Modules)
            {
                if (module.TryExecute(context, isMoving))
                    break;
            }
        }

        // Try GCD modules when GCD is ready
        if (ActionService.CanExecuteGcd)
        {
            foreach (var module in Modules)
            {
                if (module.TryExecute(context, isMoving))
                    break;
            }
        }
    }

    #endregion

    #region Error Handling

    /// <summary>
    /// Handle critical errors that indicate memory corruption.
    /// Disables the rotation to prevent further damage.
    /// </summary>
    protected virtual void HandleCriticalError(string errorType, Exception ex)
    {
        Configuration.Enabled = false;
        Log.Error(ex, "{0} DISABLED due to {1} - memory access error", Name, errorType);
        _errorKeySeh ??= string.Concat(Name, ".Execute.", errorType);
        ErrorMetrics?.RecordError(_errorKeySeh, ex.Message);
    }

    /// <summary>
    /// Handle null reference errors (likely stale pointers).
    /// </summary>
    protected virtual void HandleNullReferenceError(Exception ex)
    {
        _errorKeyNullRef ??= string.Concat(Name, ".Execute.NullRef");
        ErrorMetrics?.RecordError(_errorKeyNullRef, ex.Message);
        _suppressedErrorCount++;
    }

    /// <summary>
    /// Handle general errors with throttling to prevent log spam.
    /// </summary>
    protected virtual void HandleThrottledError(Exception ex)
    {
        _suppressedErrorCount++;
        _errorKeyGeneral ??= string.Concat(Name, ".Execute");
        ErrorMetrics?.RecordError(_errorKeyGeneral, ex.Message);

        var now = DateTime.UtcNow;
        if ((now - _lastErrorTime).TotalSeconds >= FFXIVTimings.ErrorThrottleSeconds)
        {
            _lastErrorTime = now;
            Log.Error(ex, "{0}.Execute error (suppressed {1} errors in last {2}s)",
                Name, _suppressedErrorCount, FFXIVTimings.ErrorThrottleSeconds);
            _suppressedErrorCount = 0;
        }
    }

    #endregion

    #region Auto-Attack Detection

    /// <summary>
    /// Checks if the player has auto-attack active via UIState.WeaponState.AutoAttackState.
    /// </summary>
    private static unsafe bool IsAutoAttacking()
    {
        try
        {
            var uiState = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance();
            if (uiState == null) return false;
            return uiState->WeaponState.AutoAttackState.IsAutoAttacking;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Override in derived classes to release managed or unmanaged resources.
    /// Always call <c>base.Dispose(disposing)</c> at the end of overrides.
    /// </summary>
    protected virtual void Dispose(bool disposing) { }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
