using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Helpers;
using Olympus.Rotation.HermesCore.Modules;
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
/// Ninja rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Hermes, the Greek god of speed and trickery.
/// </summary>
[Rotation("Hermes", JobRegistry.Ninja, JobRegistry.Rogue, Role = RotationRole.MeleeDps)]
public sealed class Hermes : BaseMeleeDpsRotation<IHermesContext, IHermesModule>
{
    /// <inheritdoc />
    public override string Name => "Hermes";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Ninja, JobRegistry.Rogue];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IHermesModule> Modules => _modules;

    /// <summary>
    /// Gets the Hermes-specific debug state. Used for Ninja-specific debug display.
    /// </summary>
    public HermesDebugState HermesDebug => _hermesDebugState;

    // Persistent debug state
    private readonly HermesDebugState _hermesDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly HermesStatusHelper _statusHelper;
    private readonly HermesPartyHelper _partyHelper;
    private readonly MudraHelper _mudraHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IHermesModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Gauge values (read each frame)
    private int _ninki;
    private int _kazematoi;

    public Hermes(
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
        _statusHelper = new HermesStatusHelper();
        _partyHelper = new HermesPartyHelper(objectTable, partyList);
        _mudraHelper = new MudraHelper();

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IHermesModule>
        {
            new NinjutsuModule(),  // Priority 10 - Mudra sequences (highest priority)
            new BuffModule(),      // Priority 20 - Buff management
            new DamageModule(),    // Priority 30 - DPS rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _ninki = SafeGameAccess.GetNinNinki(ErrorMetrics);
        _kazematoi = SafeGameAccess.GetNinKazematoi(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // Ninja combo: Spinning Edge -> Gust Slash -> Aeolian Edge/Armor Crush
        // AoE combo: Death Blossom -> Hakke Mujinsatsu
        if (comboTimer <= 0)
            return 0;

        return comboAction switch
        {
            // Single target combo
            2240 => 1, // Spinning Edge
            2242 => 2, // Gust Slash

            // AoE combo
            2254 => 1, // Death Blossom

            _ => 0
        };
    }

    /// <summary>
    /// Updates MP forecast. Ninjas don't use MP for abilities.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Ninjas don't use MP for any abilities
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override IHermesContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new HermesContext(
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
            mudraHelper: _mudraHelper,
            debugState: _hermesDebugState,
            ninki: _ninki,
            kazematoi: _kazematoi,
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
    protected override void SyncDebugState(IHermesContext context)
    {
        // Map Ninja debug state to common debug state fields
        _debugState.PlanningState = _hermesDebugState.PlanningState;
        _debugState.PlannedAction = _hermesDebugState.PlannedAction;
        _debugState.DpsState = _hermesDebugState.DamageState;
        // Note: NinjutsuState and BuffState are tracked in HermesDebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
