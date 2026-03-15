using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Base;
using Olympus.Rotation.Common;
using Olympus.Rotation.TerpsichoreCore.Context;
using Olympus.Rotation.TerpsichoreCore.Helpers;
using Olympus.Rotation.TerpsichoreCore.Modules;
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
/// Dancer rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Terpsichore, the Greek muse of dance.
/// </summary>
[Rotation("Terpsichore", JobRegistry.Dancer, Role = RotationRole.RangedDps)]
public sealed class Terpsichore : BaseRangedDpsRotation<ITerpsichoreContext, ITerpsichoreModule>
{
    /// <inheritdoc />
    public override string Name => "Terpsichore";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Dancer];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<ITerpsichoreModule> Modules => _modules;

    /// <summary>
    /// Gets the Terpsichore-specific debug state. Used for Dancer-specific debug display.
    /// </summary>
    public TerpsichoreDebugState TerpsichoreDebug => _terpsichoreDebugState;

    // Persistent debug state
    private readonly TerpsichoreDebugState _terpsichoreDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly TerpsichoreStatusHelper _statusHelper;
    private readonly TerpsichorePartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<ITerpsichoreModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Party coordination service for raid buff synchronization (optional)
    private readonly IPartyCoordinationService? _partyCoordinationService;

    // Training service for explaining rotation decisions (optional)
    private readonly ITrainingService? _trainingService;

    // Gauge values (read each frame)
    private int _esprit;
    private int _feathers;
    private bool _isDancing;
    private int _stepIndex;
    private byte _currentStep;
    private byte[] _danceSteps = new byte[4];

    public Terpsichore(
        IPluginLog log,
        ActionTracker actionTracker,
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
        ITimelineService? timelineService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITrainingService? trainingService = null,
        IErrorMetricsService? errorMetrics = null)
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
            errorMetrics)
    {
        _timelineService = timelineService;
        _partyCoordinationService = partyCoordinationService;
        _trainingService = trainingService;

        // Initialize helpers
        _statusHelper = new TerpsichoreStatusHelper();
        _partyHelper = new TerpsichorePartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<ITerpsichoreModule>
        {
            new BuffModule(),    // Priority 20 - Dance execution, buffs, oGCDs
            new DamageModule(),  // Priority 30 - GCD rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _esprit = SafeGameAccess.GetDncEsprit(ErrorMetrics);
        _feathers = SafeGameAccess.GetDncFeathers(ErrorMetrics);
        _isDancing = SafeGameAccess.IsDncDancing(ErrorMetrics);
        _stepIndex = SafeGameAccess.GetDncStepIndex(ErrorMetrics);
        _currentStep = SafeGameAccess.GetDncCurrentStep(ErrorMetrics);
        _danceSteps = SafeGameAccess.GetDncDanceSteps(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // Dancer has two combo chains:
        // Single target: Cascade (15989) → Fountain (15990)
        // AoE: Windmill (15993) → Bladeshower (15994)

        if (comboTimer <= 0)
            return 0;

        // Cascade started - next is Fountain
        if (comboAction == DNCActions.Cascade.ActionId)
            return 1;

        // Windmill started - next is Bladeshower
        if (comboAction == DNCActions.Windmill.ActionId)
            return 1;

        return 0;
    }

    /// <summary>
    /// Updates MP forecast. Dancers don't use MP for abilities.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Dancers don't use MP for any abilities
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override ITerpsichoreContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new TerpsichoreContext(
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
            debugState: _terpsichoreDebugState,
            esprit: _esprit,
            feathers: _feathers,
            isDancing: _isDancing,
            stepIndex: _stepIndex,
            currentStep: _currentStep,
            danceSteps: _danceSteps,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            timelineService: _timelineService,
            partyCoordinationService: _partyCoordinationService,
            trainingService: _trainingService,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(ITerpsichoreContext context)
    {
        // Map Dancer debug state to common debug state fields
        _debugState.PlanningState = _terpsichoreDebugState.PlanningState;
        _debugState.PlannedAction = _terpsichoreDebugState.PlannedAction;
        _debugState.DpsState = _terpsichoreDebugState.DamageState;
        // Note: BuffState is tracked in TerpsichoreDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
