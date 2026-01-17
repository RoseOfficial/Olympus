using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Rotation.KratosCore.Helpers;
using Olympus.Rotation.KratosCore.Modules;
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
/// Monk rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Kratos, the Greek god of strength and power.
/// </summary>
[Rotation("Kratos", JobRegistry.Monk, JobRegistry.Pugilist, Role = RotationRole.MeleeDps)]
public sealed class Kratos : BaseMeleeDpsRotation<IKratosContext, IKratosModule>
{
    /// <inheritdoc />
    public override string Name => "Kratos";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Monk, JobRegistry.Pugilist];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IKratosModule> Modules => _modules;

    /// <summary>
    /// Gets the Kratos-specific debug state. Used for Monk-specific debug display.
    /// </summary>
    public KratosDebugState KratosDebug => _kratosDebugState;

    // Persistent debug state
    private readonly KratosDebugState _kratosDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly KratosStatusHelper _statusHelper;
    private readonly KratosPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IKratosModule> _modules;

    // Timeline service for fight-aware rotation (optional)
    private readonly ITimelineService? _timelineService;

    // Gauge values (read each frame)
    private int _chakra;
    private byte[] _beastChakra = new byte[3];
    private bool _hasLunarNadi;
    private bool _hasSolarNadi;

    public Kratos(
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
        _statusHelper = new KratosStatusHelper();
        _partyHelper = new KratosPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IKratosModule>
        {
            new BuffModule(),    // Priority 20 - Buff management (RoF, Brotherhood, PB)
            new DamageModule(),  // Priority 30 - DPS rotation
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void ReadGaugeValues()
    {
        _chakra = SafeGameAccess.GetMnkChakra(ErrorMetrics);
        _beastChakra = SafeGameAccess.GetMnkBeastChakra(ErrorMetrics);
        _hasLunarNadi = SafeGameAccess.HasMnkLunarNadi(ErrorMetrics);
        _hasSolarNadi = SafeGameAccess.HasMnkSolarNadi(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // Monk doesn't have traditional combos like other jobs
        // Instead, forms determine which actions are available
        // The form system is tracked via status effects, not combo state
        // Return 0 as Monk uses the form system instead
        return 0;
    }

    /// <summary>
    /// Updates MP forecast. Monks don't use MP for abilities.
    /// </summary>
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Monks don't use MP for any abilities
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override IKratosContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new KratosContext(
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
            debugState: _kratosDebugState,
            chakra: _chakra,
            beastChakra: _beastChakra,
            hasLunarNadi: _hasLunarNadi,
            hasSolarNadi: _hasSolarNadi,
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
    protected override void SyncDebugState(IKratosContext context)
    {
        // Map Monk debug state to common debug state fields
        _debugState.PlanningState = _kratosDebugState.PlanningState;
        _debugState.PlannedAction = _kratosDebugState.PlannedAction;
        _debugState.DpsState = _kratosDebugState.DamageState;
        // Note: BuffState is tracked in KratosDebugState but not in common DebugState

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
