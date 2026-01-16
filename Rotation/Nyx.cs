using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Base;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Helpers;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;

namespace Olympus.Rotation;

/// <summary>
/// Dark Knight rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Nyx, the Greek goddess of night and darkness.
/// </summary>
[Rotation("Nyx", JobRegistry.DarkKnight, Role = RotationRole.Tank)]
public sealed class Nyx : BaseTankRotation<INyxContext, INyxModule>
{
    /// <inheritdoc />
    public override string Name => "Nyx";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.DarkKnight];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<INyxModule> Modules => _modules;

    /// <summary>
    /// Gets the Nyx-specific debug state. Used for Dark Knight-specific debug display.
    /// </summary>
    public NyxDebugState NyxDebug => _nyxDebugState;

    // Persistent debug state
    private readonly NyxDebugState _nyxDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly NyxStatusHelper _statusHelper;
    private readonly NyxPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<INyxModule> _modules;

    // Darkside timer (read from game gauge)
    private float _darksideTimer;

    public Nyx(
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
        IEnmityService enmityService,
        ITankCooldownService tankCooldownService,
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
            enmityService,
            tankCooldownService,
            errorMetrics)
    {
        // Initialize helpers
        _statusHelper = new NyxStatusHelper();
        _partyHelper = new NyxPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<INyxModule>
        {
            new EnmityModule(),     // Priority 5 - Enmity management is critical
            new MitigationModule(), // Priority 10 - Stay alive (TBN intelligence)
            new BuffModule(),       // Priority 20 - Buff management
            new DamageModule(),     // Priority 30 - DPS rotation with Darkside
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override int ReadGaugeValue()
    {
        // Read Blood Gauge
        var bloodGauge = SafeGameAccess.GetDrkBloodGauge(ErrorMetrics);

        // Also read Darkside timer while we're reading gauges
        _darksideTimer = SafeGameAccess.GetDrkDarksideTimer(ErrorMetrics);

        return bloodGauge;
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
    {
        // No combo active
        if (comboAction == 0 || comboTimer <= 0)
            return 0;

        // Check for single-target combo: Hard Slash -> Syphon Strike -> Souleater
        if (comboAction == DRKActions.HardSlash.ActionId)
            return 1; // Ready for Syphon Strike

        if (comboAction == DRKActions.SyphonStrike.ActionId)
            return 2; // Ready for Souleater

        // Check for AoE combo: Unleash -> Stalwart Soul
        if (comboAction == DRKActions.Unleash.ActionId)
            return 1; // Ready for Stalwart Soul

        // Unknown combo action, restart
        return 0;
    }

    /// <inheritdoc />
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Dark Knights use MP heavily for Edge/Flood of Shadow and TBN
        // Track MP for intelligent resource management
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false); // Tanks don't have Lucid Dreaming
    }

    /// <inheritdoc />
    protected override INyxContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new NyxContext(
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
            enmityService: EnmityService,
            tankCooldownService: TankCooldownService,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            debugState: _nyxDebugState,
            bloodGauge: GaugeValue,
            darksideTimer: _darksideTimer,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            log: Log);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(INyxContext context)
    {
        // Map tank debug state to common debug state fields
        _debugState.PlanningState = _nyxDebugState.DamageState;
        _debugState.PlannedAction = _nyxDebugState.PlannedAction;
        _debugState.DpsState = _nyxDebugState.DamageState;
        _debugState.DefensiveState = _nyxDebugState.MitigationState;

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
