using System.Collections.Generic;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Base;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Rotation.HecateCore.Helpers;
using Olympus.Rotation.HecateCore.Modules;
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
/// Black Mage rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Hecate, the Greek goddess of magic and witchcraft.
/// </summary>
[Rotation("Hecate", JobRegistry.BlackMage, JobRegistry.Thaumaturge, Role = RotationRole.Caster)]
public sealed class Hecate : BaseCasterDpsRotation<IHecateContext, IHecateModule>
{
    /// <inheritdoc />
    public override string Name => "Hecate";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.BlackMage, JobRegistry.Thaumaturge];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IHecateModule> Modules => _modules;
    protected override RotationScheduler Scheduler => _scheduler;

    /// <summary>
    /// Gets the Hecate-specific debug state. Used for Black Mage-specific debug display.
    /// </summary>
    public HecateDebugState HecateDebug => _hecateDebugState;

    // Persistent debug state
    private readonly HecateDebugState _hecateDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly HecateStatusHelper _statusHelper;
    private readonly CasterPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IHecateModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Training service for explaining rotation decisions (optional)
    private readonly ITrainingService? _trainingService;

    // Party coordination service (optional)
    private readonly IPartyCoordinationService? _partyCoordinationService;

    // Dalamud job gauge service for reliable BLM gauge access
    private readonly IJobGauges _jobGauges;

    // Gauge values (read each frame)
    private int _elementStacks;
    private float _elementTimer;
    private int _umbralHearts;
    private int _polyglotStacks;
    private int _astralSoulStacks;
    private bool _hasParadox;

    // Scheduler
    private readonly RotationScheduler _scheduler;

    public Hecate(
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
        ITrainingService? trainingService = null,
        IBurstWindowService? burstWindowService = null,
        IErrorMetricsService? errorMetrics = null,
        IPartyCoordinationService? partyCoordinationService = null,
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
        _jobGauges = jobGauges;
        _timelineService = timelineService;
        _trainingService = trainingService;
        _partyCoordinationService = partyCoordinationService;

        _scheduler = new RotationScheduler(actionService, jobGauges, configuration, timelineService, errorMetrics);

        // Initialize helpers
        _statusHelper = new HecateStatusHelper();
        _partyHelper = new CasterPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IHecateModule>
        {
            new BuffModule(BurstWindowService),    // Priority 20 - oGCD management (Ley Lines, Triplecast, Amplifier, Manafont)
            new DamageModule(BurstWindowService, SmartAoEService),  // Priority 30 - GCD rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        var gauge = _jobGauges.Get<BLMGauge>();
        // BLMGauge exposes AstralFireStacks (byte, 0-3) and UmbralIceStacks (byte, 0-3)
        // separately. Preserve the signed convention used downstream: positive = AF stacks,
        // negative = UI stacks, 0 = no element active.
        _elementStacks = gauge.AstralFireStacks > 0 ? (int)gauge.AstralFireStacks : -(int)gauge.UmbralIceStacks;
        _elementTimer = gauge.EnochianTimer / 1000f;
        _umbralHearts = gauge.UmbralHearts;
        _polyglotStacks = gauge.PolyglotStacks;
        _astralSoulStacks = gauge.AstralSoulStacks;
        _hasParadox = gauge.IsParadoxActive;
    }

    /// <summary>
    /// Updates MP forecast. Black Mages don't typically need Lucid Dreaming.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Black Mages don't use Lucid Dreaming - MP is managed through Umbral Ice
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override IHecateContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new HecateContext(
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
            debugState: _hecateDebugState,
            elementStacks: _elementStacks,
            elementTimer: _elementTimer,
            umbralHearts: _umbralHearts,
            polyglotStacks: _polyglotStacks,
            astralSoulStacks: _astralSoulStacks,
            hasParadox: _hasParadox,
            timelineService: _timelineService,
            trainingService: _trainingService,
            partyCoordinationService: _partyCoordinationService,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(IHecateContext context)
    {
        // Map Black Mage debug state to common debug state fields
        _debugState.PlanningState = _hecateDebugState.PlanningState;
        _debugState.PlannedAction = _hecateDebugState.PlannedAction;
        _debugState.DpsState = _hecateDebugState.DamageState;
        // Note: BuffState is tracked in HecateDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
