using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Base;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Rotation.ThanatosCore.Helpers;
using Olympus.Rotation.ThanatosCore.Modules;
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
/// Reaper rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Thanatos, the Greek god of death.
/// </summary>
[Rotation("Thanatos", JobRegistry.Reaper, Role = RotationRole.MeleeDps)]
public sealed class Thanatos : BaseMeleeDpsRotation<IThanatosContext, IThanatosModule>
{
    /// <inheritdoc />
    public override string Name => "Thanatos";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Reaper];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IThanatosModule> Modules => _modules;

    /// <summary>
    /// Gets the Thanatos-specific debug state. Used for Reaper-specific debug display.
    /// </summary>
    public ThanatosDebugState ThanatosDebug => _thanatosDebugState;

    // Persistent debug state
    private readonly ThanatosDebugState _thanatosDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly ThanatosStatusHelper _statusHelper;
    private readonly MeleeDpsPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IThanatosModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Party coordination service for raid buff synchronization (optional)
    private readonly IPartyCoordinationService? _partyCoordinationService;

    // Training service for explaining rotation decisions (optional)
    private readonly ITrainingService? _trainingService;

    // Gauge values (read each frame)
    private int _soul;
    private int _shroud;
    private int _lemureShroud;
    private int _voidShroud;
    private float _enshroudTimer;

    public Thanatos(
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
        IPositionalService positionalService,
        ITimelineService? timelineService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITrainingService? trainingService = null,
        IBurstWindowService? burstWindowService = null,
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
            positionalService,
            burstWindowService,
            errorMetrics)
    {
        _timelineService = timelineService;
        _partyCoordinationService = partyCoordinationService;
        _trainingService = trainingService;

        // Initialize helpers
        _statusHelper = new ThanatosStatusHelper();
        _partyHelper = new MeleeDpsPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IThanatosModule>
        {
            new BuffModule(),    // Priority 20 - Buff management (Arcane Circle, Enshroud)
            new DamageModule(BurstWindowService),  // Priority 30 - DPS rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _soul = SafeGameAccess.GetRprSoul(ErrorMetrics);
        _shroud = SafeGameAccess.GetRprShroud(ErrorMetrics);
        _lemureShroud = SafeGameAccess.GetRprLemureShroud(ErrorMetrics);
        _voidShroud = SafeGameAccess.GetRprVoidShroud(ErrorMetrics);
        _enshroudTimer = SafeGameAccess.GetRprEnshroudTimer(ErrorMetrics);
    }

    /// <summary>
    /// RPR positionals: Gibbet = flank, Gallows = rear.
    /// Enhanced status determines which one is buffed next.
    /// </summary>
    protected override PositionalType? GetNextRequiredPositional()
    {
        var player = ObjectTable.LocalPlayer;
        if (player == null) return null;

        foreach (var s in player.StatusList)
        {
            // EnhancedGibbet → next should be Gibbet (flank)
            if (s.StatusId == RPRActions.StatusIds.EnhancedGibbet)
                return PositionalType.Flank;
            // EnhancedGallows → next should be Gallows (rear)
            if (s.StatusId == RPRActions.StatusIds.EnhancedGallows)
                return PositionalType.Rear;
        }

        // Default: Gallows (rear) if no enhanced buff
        return null;
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // Reaper combos:
        // ST: Slice -> Waxing Slice -> Infernal Slice
        // AoE: Spinning Scythe -> Nightmare Scythe
        if (comboTimer <= 0)
            return 0;

        return comboAction switch
        {
            // Single target combo
            24373 => 1, // Slice
            24374 => 2, // Waxing Slice

            // AoE combo
            24376 => 1, // Spinning Scythe

            _ => 0
        };
    }

    /// <summary>
    /// Updates MP forecast. Reapers don't use MP for abilities.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Reapers don't use MP for any abilities
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override IThanatosContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new ThanatosContext(
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
            debugState: _thanatosDebugState,
            soul: _soul,
            shroud: _shroud,
            lemureShroud: _lemureShroud,
            voidShroud: _voidShroud,
            enshroudTimer: _enshroudTimer,
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
    protected override void SyncDebugState(IThanatosContext context)
    {
        // Map Reaper debug state to common debug state fields
        _debugState.PlanningState = _thanatosDebugState.PlanningState;
        _debugState.PlannedAction = _thanatosDebugState.PlannedAction;
        _debugState.DpsState = _thanatosDebugState.DamageState;
        // Note: BuffState is tracked in ThanatosDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
