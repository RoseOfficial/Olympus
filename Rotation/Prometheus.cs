using System.Collections.Generic;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Base;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.PrometheusCore.Context;
using Olympus.Rotation.PrometheusCore.Helpers;
using Olympus.Rotation.PrometheusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation;

/// <summary>
/// Machinist rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Prometheus, the Greek titan of fire and technology.
/// </summary>
[Rotation("Prometheus", JobRegistry.Machinist, Role = RotationRole.RangedDps)]
public sealed class Prometheus : BaseRangedDpsRotation<IPrometheusContext, IPrometheusModule>
{
    /// <inheritdoc />
    public override string Name => "Prometheus";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Machinist];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IPrometheusModule> Modules => _modules;

    /// <summary>
    /// Gets the Prometheus-specific debug state. Used for Machinist-specific debug display.
    /// </summary>
    public PrometheusDebugState PrometheusDebug => _prometheusDebugState;

    // Persistent debug state
    private readonly PrometheusDebugState _prometheusDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly PrometheusStatusHelper _statusHelper;
    private readonly RangedDpsPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IPrometheusModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Party coordination service for burst alignment (optional)
    private readonly IPartyCoordinationService? _partyCoordinationService;

    // Training service for explaining rotation decisions (optional)
    private readonly ITrainingService? _trainingService;

    // Gauge values (read each frame)
    private int _heat;
    private int _battery;
    private float _overheatRemaining;
    private float _queenRemaining;
    private int _lastQueenBattery;

    // Scheduler
    private readonly RotationScheduler _scheduler;

    public Prometheus(
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
        IJobGauges jobGauges,
        ITimelineService? timelineService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITrainingService? trainingService = null,
        IBurstWindowService? burstWindowService = null,
        IErrorMetricsService? errorMetrics = null,
        Olympus.Services.Consumables.ITinctureDispatcher? tinctureDispatcher = null,
        Olympus.Services.Pull.IPullIntentService? pullIntentService = null)
        : base(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            burstWindowService,
            errorMetrics,
            tinctureDispatcher: tinctureDispatcher,
            pullIntentService: pullIntentService)
    {
        _timelineService = timelineService;
        _partyCoordinationService = partyCoordinationService;
        _trainingService = trainingService;

        _scheduler = new RotationScheduler(actionService, jobGauges, configuration, timelineService, errorMetrics);

        // Initialize helpers
        _statusHelper = new PrometheusStatusHelper();
        _partyHelper = new RangedDpsPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IPrometheusModule>
        {
            new BuffModule(BurstWindowService),    // Priority 20 - Buff management (Wildfire, Hypercharge, Queen)
            new DamageModule(BurstWindowService, SmartAoEService),  // Priority 30 - DPS rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _heat = SafeGameAccess.GetMchHeat(ErrorMetrics);
        _battery = SafeGameAccess.GetMchBattery(ErrorMetrics);
        _overheatRemaining = SafeGameAccess.GetMchOverheatTimer(ErrorMetrics);
        _queenRemaining = SafeGameAccess.GetMchQueenTimer(ErrorMetrics);
        _lastQueenBattery = SafeGameAccess.GetMchLastQueenBattery(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
        => ComputeComboStep(comboAction, comboTimer);

    internal static int ComputeComboStep(uint comboAction, float comboTimer)
    {
        // MCH basic combo: Split Shot -> Slug Shot -> Clean Shot
        // (Heated versions at higher levels)
        if (comboTimer <= 0)
            return 0;

        return comboAction switch
        {
            // Combo starters
            2866 => 1, // Split Shot
            7411 => 1, // Heated Split Shot

            // Second hits
            2868 => 2, // Slug Shot
            7412 => 2, // Heated Slug Shot

            _ => 0
        };
    }

    /// <summary>
    /// Updates MP forecast. Machinists don't use MP for abilities.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Machinists don't use MP for any abilities
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override IPrometheusContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new PrometheusContext(
            player: player,
            inCombat: inCombat,
            isMoving: isMoving,
            canExecuteGcd: ActionService.CanExecuteGcd,
            canExecuteOgcd: ActionService.CanExecuteOgcd,
            actionService: ActionService,
            actionTracker: ActionTracker,
            combatEventService: CombatEventService,
            damageIntakeService: DamageIntakeService,
            damageTrendService: DamageTrendService,
            frameCache: FrameCache,
            configuration: Configuration,
            debuffDetectionService: DebuffDetectionService,
            hpPredictionService: HpPredictionService,
            mpForecastService: MpForecastService,
            playerStatsService: PlayerStatsService,
            targetingService: TargetingService,
            objectTable: ObjectTable,
            partyList: PartyList,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            debugState: _prometheusDebugState,
            heat: _heat,
            battery: _battery,
            overheatRemaining: _overheatRemaining,
            queenRemaining: _queenRemaining,
            lastQueenBattery: _lastQueenBattery,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            timelineService: _timelineService,
            log: Log,
            partyCoordinationService: _partyCoordinationService,
            trainingService: _trainingService);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(IPrometheusContext context)
    {
        // Map Machinist debug state to common debug state fields
        _debugState.PlanningState = _prometheusDebugState.PlanningState;
        _debugState.PlannedAction = _prometheusDebugState.PlannedAction;
        _debugState.DpsState = _prometheusDebugState.DamageState;
        // Note: BuffState is tracked in PrometheusDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    /// <inheritdoc />
    protected override void ExecuteModules(IPrometheusContext context, bool isMoving, bool inCombat)
    {
        if (TryDispatchTincture(context, inCombat)) return;

        if (Configuration.Targeting.PauseAllOnStandStillPunisher
            && PlayerSafetyHelper.IsStandStillPunisherActive(context.Player))
            return;
        if (Configuration.Targeting.PauseOnPlayerChannel
            && PlayerSafetyHelper.IsPlayerIntentChannelActive(context.Player))
            return;

        _scheduler.Reset();
        foreach (var module in _modules)
            module.CollectCandidates(context, _scheduler, isMoving);

        if (inCombat && ActionService.CanExecuteOgcd)
            _scheduler.DispatchOgcd(context);

        if (ActionService.CanExecuteGcd)
            _scheduler.DispatchGcd(context);
    }

    #endregion
}
