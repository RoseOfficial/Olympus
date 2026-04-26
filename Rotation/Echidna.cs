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
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Rotation.EchidnaCore.Helpers;
using Olympus.Rotation.EchidnaCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Party;
using Olympus.Services.Positional;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation;

/// <summary>
/// Viper rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Echidna, the Greek mother of serpents.
/// </summary>
[Rotation("Echidna", JobRegistry.Viper, Role = RotationRole.MeleeDps)]
public sealed class Echidna : BaseMeleeDpsRotation<IEchidnaContext, IEchidnaModule>
{
    /// <inheritdoc />
    public override string Name => "Echidna";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Viper];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IEchidnaModule> Modules => _modules;

    /// <summary>
    /// Gets the Echidna-specific debug state. Used for Viper-specific debug display.
    /// </summary>
    public EchidnaDebugState EchidnaDebug => _echidnaDebugState;

    // Persistent debug state
    private readonly EchidnaDebugState _echidnaDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly EchidnaStatusHelper _statusHelper;
    private readonly MeleeDpsPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IEchidnaModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Party coordination service for raid buff synchronization (optional)
    private readonly IPartyCoordinationService? _partyCoordinationService;

    // Training service for explaining rotation decisions (optional)
    private readonly ITrainingService? _trainingService;

    // Gauge values (read each frame)
    private int _serpentOffering;
    private int _anguineTribute;
    private int _rattlingCoils;
    private VPRActions.DreadCombo _dreadCombo;
    private VPRActions.SerpentCombo _serpentCombo;

    // Scheduler
    private readonly RotationScheduler _scheduler;

    public Echidna(
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
        IPositionalService positionalService,
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
            positionalService,
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
        _statusHelper = new EchidnaStatusHelper();
        _partyHelper = new MeleeDpsPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IEchidnaModule>
        {
            new BuffModule(BurstWindowService),    // Priority 20 - Buff management (Serpent's Ire)
            new DamageModule(BurstWindowService, SmartAoEService),  // Priority 30 - DPS rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _serpentOffering = SafeGameAccess.GetVprSerpentOffering(ErrorMetrics);
        _anguineTribute = SafeGameAccess.GetVprAnguineTribute(ErrorMetrics);
        _rattlingCoils = SafeGameAccess.GetVprRattlingCoilStacks(ErrorMetrics);
        _dreadCombo = (VPRActions.DreadCombo)SafeGameAccess.GetVprDreadCombo(ErrorMetrics);
        _serpentCombo = (VPRActions.SerpentCombo)SafeGameAccess.GetVprSerpentCombo(ErrorMetrics);
    }

    /// <summary>
    /// VPR positionals: Hindsting/Hindsbane = rear, Flanksting/Flanksbane = flank.
    /// Determined by active venom status.
    /// </summary>
    protected override PositionalType? GetNextRequiredPositional()
    {
        var player = ObjectTable.LocalPlayer;
        if (player == null) return null;

        // Only relevant at combo step 2 (after Hunter's/Swiftskin's Sting)
        if (LastComboAction is not (34608 or 34609)) return null;

        foreach (var s in player.StatusList)
        {
            // Hindstung/Hindsbane venoms → rear
            if (s.StatusId is VPRActions.StatusIds.HindstungVenom or VPRActions.StatusIds.HindsbaneVenom)
                return PositionalType.Rear;
            // Flankstung/Flanksbane venoms → flank
            if (s.StatusId is VPRActions.StatusIds.FlankstungVenom or VPRActions.StatusIds.FlanksbaneVenom)
                return PositionalType.Flank;
        }

        // Default: flank (Flanksting is the default when no venom)
        return PositionalType.Flank;
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
        => ComputeComboStep(comboAction, comboTimer);

    internal static int ComputeComboStep(uint comboAction, float comboTimer)
    {
        // Viper dual wield combos (2.5s GCD):
        // ST: Steel Fangs → Hunter's Sting → Positional Finisher
        //     Reaving Fangs → Swiftskin's Sting → Positional Finisher
        // AoE: Steel Maw → Hunter's Bite → Jagged Maw
        //      Reaving Maw → Swiftskin's Bite → Bloodied Maw
        if (comboTimer <= 0)
            return 0;

        return comboAction switch
        {
            // Single target combo starters
            34606 => 1, // Steel Fangs
            34607 => 1, // Reaving Fangs

            // Single target second hits
            34608 => 2, // Hunter's Sting
            34609 => 2, // Swiftskin's Sting

            // AoE combo starters
            34614 => 1, // Steel Maw
            34615 => 1, // Reaving Maw

            // AoE second hits
            34616 => 2, // Hunter's Bite
            34617 => 2, // Swiftskin's Bite

            _ => 0
        };
    }

    /// <summary>
    /// Updates MP forecast. Vipers don't use MP for abilities.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Vipers don't use MP for any abilities
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override IEchidnaContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new EchidnaContext(
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
            positionalService: PositionalService,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            debugState: _echidnaDebugState,
            serpentOffering: _serpentOffering,
            anguineTribute: _anguineTribute,
            rattlingCoils: _rattlingCoils,
            dreadCombo: _dreadCombo,
            serpentCombo: _serpentCombo,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            isAtRear: IsAtRear,
            isAtFlank: IsAtFlank,
            targetHasPositionalImmunity: TargetHasPositionalImmunity,
            timelineService: _timelineService,
            partyCoordinationService: _partyCoordinationService,
            trainingService: _trainingService,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(IEchidnaContext context)
    {
        // Map Viper debug state to common debug state fields
        _debugState.PlanningState = _echidnaDebugState.PlanningState;
        _debugState.PlannedAction = _echidnaDebugState.PlannedAction;
        _debugState.DpsState = _echidnaDebugState.DamageState;
        // Note: BuffState is tracked in EchidnaDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    /// <inheritdoc />
    protected override void ExecuteModules(IEchidnaContext context, bool isMoving, bool inCombat)
    {
        if (Configuration.Targeting.PauseAllOnStandStillPunisher
            && PlayerSafetyHelper.IsStandStillPunisherActive(context.Player))
            return;
        if (Configuration.Targeting.PauseOnPlayerChannel
            && PlayerSafetyHelper.IsPlayerIntentChannelActive(context.Player))
            return;

        if (TryDispatchTincture(context, inCombat)) return;

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
