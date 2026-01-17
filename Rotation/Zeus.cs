using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Rotation.ZeusCore.Helpers;
using Olympus.Rotation.ZeusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Positional;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Timeline;

namespace Olympus.Rotation;

/// <summary>
/// Dragoon rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Zeus, the Greek god of sky and thunder.
/// </summary>
[Rotation("Zeus", JobRegistry.Dragoon, JobRegistry.Lancer, Role = RotationRole.MeleeDps)]
public sealed class Zeus : BaseMeleeDpsRotation<IZeusContext, IZeusModule>
{
    /// <inheritdoc />
    public override string Name => "Zeus";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Dragoon, JobRegistry.Lancer];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IZeusModule> Modules => _modules;

    /// <summary>
    /// Gets the Zeus-specific debug state. Used for Dragoon-specific debug display.
    /// </summary>
    public ZeusDebugState ZeusDebug => _zeusDebugState;

    // Persistent debug state
    private readonly ZeusDebugState _zeusDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly ZeusStatusHelper _statusHelper;
    private readonly ZeusPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IZeusModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Gauge values (read each frame)
    private int _firstmindsFocus;
    private int _eyeCount;
    private bool _isLifeOfDragonActive;
    private float _lifeOfDragonRemaining;

    public Zeus(
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
            errorMetrics)
    {
        _timelineService = timelineService;

        // Initialize helpers
        _statusHelper = new ZeusStatusHelper();
        _partyHelper = new ZeusPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IZeusModule>
        {
            new BuffModule(),    // Priority 20 - Buff management (Lance Charge, Battle Litany, Life Surge)
            new DamageModule(),  // Priority 30 - DPS rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _firstmindsFocus = SafeGameAccess.GetDrgFirstmindsFocus(ErrorMetrics);
        _eyeCount = SafeGameAccess.GetDrgEyeCount(ErrorMetrics);
        _isLifeOfDragonActive = SafeGameAccess.IsDrgLifeOfDragonActive(ErrorMetrics);
        _lifeOfDragonRemaining = SafeGameAccess.GetDrgLifeOfDragonTimer(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // Dragoon uses traditional combo system
        if (comboTimer <= 0 || comboAction == 0)
            return 0;

        // Determine combo step based on last action
        return comboAction switch
        {
            // Single-target combo
            75 => 1,     // True Thrust
            78 => 2,     // Vorpal Thrust
            87 => 2,     // Disembowel
            84 or 25771 => 3,  // Full Thrust / Heavens' Thrust
            88 or 25772 => 3,  // Chaos Thrust / Chaotic Spring

            // AoE combo
            86 => 1,     // Doom Spike
            7397 => 2,   // Sonic Thrust
            16477 => 3,  // Coerthan Torment

            _ => 0
        };
    }

    /// <summary>
    /// Updates MP forecast. Dragoons don't use MP for abilities.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Dragoons don't use MP for any abilities
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override IZeusContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new ZeusContext(
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
            debugState: _zeusDebugState,
            firstmindsFocus: _firstmindsFocus,
            eyeCount: _eyeCount,
            isLifeOfDragonActive: _isLifeOfDragonActive,
            lifeOfDragonRemaining: _lifeOfDragonRemaining,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            isAtRear: IsAtRear,
            isAtFlank: IsAtFlank,
            targetHasPositionalImmunity: TargetHasPositionalImmunity,
            timelineService: _timelineService,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(IZeusContext context)
    {
        // Map Dragoon debug state to common debug state fields
        _debugState.PlanningState = _zeusDebugState.PlanningState;
        _debugState.PlannedAction = _zeusDebugState.PlannedAction;
        _debugState.DpsState = _zeusDebugState.DamageState;
        // Note: BuffState is tracked in ZeusDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
