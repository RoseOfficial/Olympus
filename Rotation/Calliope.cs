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
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Rotation.CalliopeCore.Helpers;
using Olympus.Rotation.CalliopeCore.Modules;
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
/// Bard rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Calliope, the Greek muse of epic poetry.
/// </summary>
[Rotation("Calliope", JobRegistry.Bard, JobRegistry.Archer, Role = RotationRole.RangedDps)]
public sealed class Calliope : BaseRangedDpsRotation<ICalliopeContext, ICalliopeModule>
{
    /// <inheritdoc />
    public override string Name => "Calliope";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Bard, JobRegistry.Archer];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<ICalliopeModule> Modules => _modules;

    /// <summary>
    /// Gets the Calliope-specific debug state. Used for Bard-specific debug display.
    /// </summary>
    public CalliopeDebugState CalliopeDebug => _calliopeDebugState;

    // Persistent debug state
    private readonly CalliopeDebugState _calliopeDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly CalliopeStatusHelper _statusHelper;
    private readonly RangedDpsPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<ICalliopeModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Party coordination service for raid buff synchronization (optional)
    private readonly IPartyCoordinationService? _partyCoordinationService;

    // Training service for explaining rotation decisions (optional)
    private readonly ITrainingService? _trainingService;

    // Gauge values (read each frame)
    private int _soulVoice;
    private float _songTimer;
    private int _repertoire;
    private byte _currentSong;
    private int _codaCount;

    // Scheduler
    private readonly RotationScheduler _scheduler;

    public Calliope(
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
        _statusHelper = new CalliopeStatusHelper();
        _partyHelper = new RangedDpsPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<ICalliopeModule>
        {
            new BuffModule(BurstWindowService),    // Priority 20 - Song rotation, buffs, oGCDs
            new DamageModule(BurstWindowService, SmartAoEService),  // Priority 30 - GCD rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _soulVoice = SafeGameAccess.GetBrdSoulVoice(ErrorMetrics);
        _songTimer = SafeGameAccess.GetBrdSongTimer(ErrorMetrics);
        _repertoire = SafeGameAccess.GetBrdRepertoire(ErrorMetrics);
        _currentSong = SafeGameAccess.GetBrdCurrentSong(ErrorMetrics);
        _codaCount = SafeGameAccess.GetBrdCodaCount(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
        => ComputeComboStep(comboAction, comboTimer);

    internal static int ComputeComboStep(uint comboAction, float comboTimer)
    {
        // Bard has no strict combo system - all GCDs are standalone
        return 0;
    }

    /// <summary>
    /// Updates MP forecast. Bards don't use MP for abilities.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Bards don't use MP for any abilities
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override ICalliopeContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new CalliopeContext(
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
            debugState: _calliopeDebugState,
            soulVoice: _soulVoice,
            songTimer: _songTimer,
            repertoire: _repertoire,
            currentSong: _currentSong,
            codaCount: _codaCount,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            timelineService: _timelineService,
            partyCoordinationService: _partyCoordinationService,
            trainingService: _trainingService,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(ICalliopeContext context)
    {
        // Map Bard debug state to common debug state fields
        _debugState.PlanningState = _calliopeDebugState.PlanningState;
        _debugState.PlannedAction = _calliopeDebugState.PlannedAction;
        _debugState.DpsState = _calliopeDebugState.DamageState;
        // Note: BuffState is tracked in CalliopeDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    /// <inheritdoc />
    protected override void ExecuteModules(ICalliopeContext context, bool isMoving, bool inCombat)
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
