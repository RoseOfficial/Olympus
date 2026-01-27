using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.IrisCore.Context;
using Olympus.Rotation.IrisCore.Helpers;
using Olympus.Rotation.IrisCore.Modules;
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
/// Pictomancer rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Iris, the Greek goddess of the rainbow - fitting for a painter who creates with colors.
/// </summary>
[Rotation("Iris", JobRegistry.Pictomancer, Role = RotationRole.Caster)]
public sealed class Iris : BaseCasterDpsRotation<IIrisContext, IIrisModule>
{
    /// <inheritdoc />
    public override string Name => "Iris";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Pictomancer];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IIrisModule> Modules => _modules;

    /// <summary>
    /// Gets the Iris-specific debug state. Used for Pictomancer-specific debug display.
    /// </summary>
    public IrisDebugState IrisDebug => _irisDebugState;

    // Persistent debug state
    private readonly IrisDebugState _irisDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly IrisStatusHelper _statusHelper;
    private readonly CasterPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IIrisModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Party coordination service for multi-Olympus IPC (optional)
    private readonly IPartyCoordinationService? _partyCoordinationService;

    // Training service for decision explanations (optional)
    private readonly ITrainingService? _trainingService;

    // Gauge values (read each frame)
    private int _paletteGauge;
    private int _whitePaint;
    private bool _hasBlackPaint;
    private byte _creatureMotif;
    private bool _hasWeaponCanvas;
    private bool _hasLandscapeCanvas;
    private bool _mogReady;
    private bool _madeenReady;

    // Combo tracking (read from game each frame)
    private uint _comboAction;
    private float _comboTimer;

    public Iris(
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
        IErrorMetricsService? errorMetrics = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITrainingService? trainingService = null)
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
        _statusHelper = new IrisStatusHelper();
        _partyHelper = new CasterPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IIrisModule>
        {
            new BuffModule(),    // Priority 20 - oGCD management (Muses, Portraits, Subtractive Palette, etc.)
            new DamageModule(),  // Priority 30 - GCD rotation (combos, paint spenders, finishers)
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _paletteGauge = SafeGameAccess.GetPctPaletteGauge(ErrorMetrics);
        _whitePaint = SafeGameAccess.GetPctWhitePaint(ErrorMetrics);
        _hasBlackPaint = SafeGameAccess.GetPctHasBlackPaint(ErrorMetrics);
        _creatureMotif = SafeGameAccess.GetPctCreatureMotif(ErrorMetrics);
        _hasWeaponCanvas = SafeGameAccess.GetPctHasWeaponCanvas(ErrorMetrics);
        _hasLandscapeCanvas = SafeGameAccess.GetPctHasLandscapeCanvas(ErrorMetrics);
        _mogReady = SafeGameAccess.GetPctMogReady(ErrorMetrics);
        _madeenReady = SafeGameAccess.GetPctMadeenReady(ErrorMetrics);

        // Read combo state from game
        _comboAction = SafeGameAccess.GetComboAction(ErrorMetrics);
        _comboTimer = SafeGameAccess.GetComboTimer(ErrorMetrics);
    }

    /// <summary>
    /// Updates MP forecast. Pictomancers use Lucid Dreaming for MP management.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        var hasLucid = _statusHelper.HasLucidDreaming(player);
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: hasLucid);
    }

    /// <inheritdoc />
    protected override IIrisContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new IrisContext(
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
            debugState: _irisDebugState,
            paletteGauge: _paletteGauge,
            whitePaint: _whitePaint,
            hasBlackPaint: _hasBlackPaint,
            creatureMotif: _creatureMotif,
            hasWeaponCanvas: _hasWeaponCanvas,
            hasLandscapeCanvas: _hasLandscapeCanvas,
            mogReady: _mogReady,
            madeenReady: _madeenReady,
            comboAction: _comboAction,
            comboTimer: _comboTimer,
            timelineService: _timelineService,
            log: Log,
            partyCoordinationService: _partyCoordinationService,
            trainingService: _trainingService);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(IIrisContext context)
    {
        // Map Pictomancer debug state to common debug state fields
        _debugState.PlanningState = _irisDebugState.PlanningState;
        _debugState.PlannedAction = _irisDebugState.PlannedAction;
        _debugState.DpsState = _irisDebugState.DamageState;
        // Note: BuffState is tracked in IrisDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
