using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.CirceCore.Helpers;
using Olympus.Rotation.CirceCore.Modules;
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
/// Red Mage rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Circe, the Greek goddess of sorcery who transformed her enemies with magic.
/// </summary>
[Rotation("Circe", JobRegistry.RedMage, Role = RotationRole.Caster)]
public sealed class Circe : BaseCasterDpsRotation<ICirceContext, ICirceModule>
{
    /// <inheritdoc />
    public override string Name => "Circe";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.RedMage];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<ICirceModule> Modules => _modules;

    /// <summary>
    /// Gets the Circe-specific debug state. Used for Red Mage-specific debug display.
    /// </summary>
    public CirceDebugState CirceDebug => _circeDebugState;

    // Persistent debug state
    private readonly CirceDebugState _circeDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly CirceStatusHelper _statusHelper;
    private readonly CasterPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<ICirceModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Party coordination service for raid buff synchronization (optional)
    private readonly IPartyCoordinationService? _partyCoordinationService;

    // Training service for decision explanations (optional)
    private readonly ITrainingService? _trainingService;

    // Gauge values (read each frame)
    private int _blackMana;
    private int _whiteMana;
    private int _manaStacks;

    // Combo tracking (read from game each frame)
    private uint _comboAction;
    private float _comboTimer;

    public Circe(
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
        _statusHelper = new CirceStatusHelper();
        _partyHelper = new CasterPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<ICirceModule>
        {
            new BuffModule(),    // Priority 20 - oGCD management (Fleche, Contre Sixte, Embolden, etc.)
            new DamageModule(),  // Priority 30 - GCD rotation (Dualcast, melee combo, finishers)
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _blackMana = SafeGameAccess.GetRdmBlackMana(ErrorMetrics);
        _whiteMana = SafeGameAccess.GetRdmWhiteMana(ErrorMetrics);
        _manaStacks = SafeGameAccess.GetRdmManaStacks(ErrorMetrics);

        // Read combo state from game
        _comboAction = SafeGameAccess.GetComboAction(ErrorMetrics);
        _comboTimer = SafeGameAccess.GetComboTimer(ErrorMetrics);
    }

    /// <summary>
    /// Updates MP forecast. Red Mages use Lucid Dreaming for MP management.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        var hasLucid = BaseStatusHelper.HasLucidDreaming(player);
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: hasLucid);
    }

    /// <inheritdoc />
    protected override ICirceContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new CirceContext(
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
            debugState: _circeDebugState,
            blackMana: _blackMana,
            whiteMana: _whiteMana,
            manaStacks: _manaStacks,
            comboAction: _comboAction,
            comboTimer: _comboTimer,
            timelineService: _timelineService,
            partyCoordinationService: _partyCoordinationService,
            trainingService: _trainingService,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(ICirceContext context)
    {
        // Map Red Mage debug state to common debug state fields
        _debugState.PlanningState = _circeDebugState.PlanningState;
        _debugState.PlannedAction = _circeDebugState.PlannedAction;
        _debugState.DpsState = _circeDebugState.DamageState;
        // Note: BuffState is tracked in CirceDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
