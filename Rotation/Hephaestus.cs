using System.Collections.Generic;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Base;
using Olympus.Rotation.Common;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Helpers;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation;

/// <summary>
/// Gunbreaker rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Hephaestus, the Greek god of forge and weapons.
/// </summary>
[Rotation("Hephaestus", JobRegistry.Gunbreaker, Role = RotationRole.Tank)]
public sealed class Hephaestus : BaseTankRotation<IHephaestusContext, IHephaestusModule>
{
    /// <inheritdoc />
    public override string Name => "Hephaestus";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Gunbreaker];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IHephaestusModule> Modules => _modules;

    /// <summary>
    /// Gets the Hephaestus-specific debug state. Used for Gunbreaker-specific debug display.
    /// </summary>
    public HephaestusDebugState HephaestusDebug => _hephaestusDebugState;

    // Persistent debug state
    private readonly HephaestusDebugState _hephaestusDebugState = new();

    // IRotation-compatible debug state (for common debug interface)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly HephaestusStatusHelper _statusHelper;
    private readonly HephaestusPartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IHephaestusModule> _modules;

    // Training
    private readonly ITrainingService? _trainingService;

    // Burst window service
    private readonly IBurstWindowService? _burstWindowService;

    // Job gauge access (for reliable AmmoComboStep tracking)
    private readonly IJobGauges _jobGauges;

    // Gnashing Fang combo step tracking
    private int _gnashingFangStep;

    // Reign of Beasts combo step tracking (0=none, 1=Noble Blood next, 2=Lion Heart next)
    private int _reignComboStep;

    // Scheduler (per-rotation, per-frame priority queue)
    private readonly RotationScheduler _scheduler;

    public Hephaestus(
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
        IEnmityService enmityService,
        ITankCooldownService tankCooldownService,
        IJobGauges jobGauges,
        ITimelineService? timelineService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITrainingService? trainingService = null,
        IErrorMetricsService? errorMetrics = null,
        IBurstWindowService? burstWindowService = null,
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
            enmityService,
            tankCooldownService,
            timelineService,
            partyCoordinationService,
            errorMetrics,
            tinctureDispatcher,
            pullIntentService)
    {
        // Initialize training service
        _trainingService = trainingService;
        _burstWindowService = burstWindowService;
        _jobGauges = jobGauges;

        _scheduler = new RotationScheduler(
            actionService,
            jobGauges,
            configuration,
            timelineService,
            errorMetrics);

        // Initialize helpers
        _statusHelper = new HephaestusStatusHelper();
        _partyHelper = new HephaestusPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IHephaestusModule>
        {
            new EnmityModule(),                                // Priority 5 - Enmity management is critical
            new MitigationModule(),                            // Priority 10 - Stay alive (Heart of Corundum intelligence)
            new BuffModule(_burstWindowService),               // Priority 20 - Buff management (No Mercy, Bloodfest)
            new DamageModule(),                                // Priority 30 - DPS rotation with Gnashing Fang combo
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override int ReadGaugeValue()
    {
        // Read Cartridge Gauge (0-3)
        return SafeGameAccess.GetGnbCartridges(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
        => ComputeComboStep(comboAction, comboTimer);

    internal static int ComputeComboStep(uint comboAction, float comboTimer)
    {
        // No combo active
        if (comboAction == 0 || comboTimer <= 0)
            return 0;

        // Check for single-target combo: Keen Edge -> Brutal Shell -> Solid Barrel
        if (comboAction == GNBActions.KeenEdge.ActionId)
            return 1; // Ready for Brutal Shell

        if (comboAction == GNBActions.BrutalShell.ActionId)
            return 2; // Ready for Solid Barrel

        // Check for AoE combo: Demon Slice -> Demon Slaughter
        if (comboAction == GNBActions.DemonSlice.ActionId)
            return 1; // Ready for Demon Slaughter

        // Unknown combo action, restart
        return 0;
    }

    /// <inheritdoc />
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        // Gunbreakers don't use MP for their core rotation
        // Just track for completeness
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false); // Tanks don't have Lucid Dreaming
    }

    /// <inheritdoc />
    protected override IHephaestusContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        // Update Gnashing Fang step tracking via action replacement detection
        UpdateGnashingFangStep();

        // Update Reign of Beasts combo step via action replacement detection
        UpdateReignComboStep();

        return new HephaestusContext(
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
            debugState: _hephaestusDebugState,
            cartridges: GaugeValue,
            gnashingFangStep: _gnashingFangStep,
            reignComboStep: _reignComboStep,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            timelineService: TimelineService,
            partyCoordinationService: PartyCoordinationService,
            trainingService: _trainingService,
            log: Log);
    }

    /// <summary>
    /// Pure mapping from <c>GNBGauge.AmmoComboStep</c> byte to the per-chain step pair.
    /// Byte semantics:
    ///   0 = no combo, 1 = Savage Claw next, 2 = Wicked Talon next,
    ///   3 = Noble Blood next, 4 = Lion Heart next.
    /// Anything outside [1, 4] yields (0, 0).
    /// Extracted from the wrapper for unit-test coverage; no production behavior change.
    /// </summary>
    internal static (int gnashingFang, int reign) ComputeStepsFromAmmoCombo(byte ammoComboStep)
    {
        int gnashingFang = ammoComboStep switch { 1 => 1, 2 => 2, _ => 0 };
        int reign = ammoComboStep switch { 3 => 1, 4 => 2, _ => 0 };
        return (gnashingFang, reign);
    }

    /// <summary>
    /// Updates Gnashing Fang and Reign of Beasts combo step tracking from the job gauge.
    /// Earlier versions read `GetAdjustedActionId` on the chain heads, but the action
    /// replacement only updates on the Gnashing Fang chain — it never advanced for
    /// Reign of Beasts, so Noble Blood / Lion Heart were never queued and the bot
    /// fell through to Burst Strike spam after Bloodfest.
    /// </summary>
    private void UpdateGnashingFangStep()
    {
        byte step;
        try
        {
            step = _jobGauges.Get<GNBGauge>().AmmoComboStep;
        }
        catch
        {
            ErrorMetrics?.RecordError("Hephaestus", "Failed to read GNB AmmoComboStep");
            _gnashingFangStep = 0;
            _reignComboStep = 0;
            return;
        }

        (_gnashingFangStep, _reignComboStep) = ComputeStepsFromAmmoCombo(step);
    }

    // Kept as a no-op so the existing call site in UpdateRotation still compiles — the
    // combined update now happens in UpdateGnashingFangStep.
    private void UpdateReignComboStep() { }

    /// <inheritdoc />
    protected override void ExecuteModules(IHephaestusContext context, bool isMoving, bool inCombat)
    {
        if (TryDispatchTincture(context, inCombat)) return;

        // Preserve BaseRotation's safety pauses (same as parent loop).
        if (Configuration.Targeting.PauseAllOnStandStillPunisher
            && PlayerSafetyHelper.IsStandStillPunisherActive(context.Player))
        {
            return;
        }
        if (Configuration.Targeting.PauseOnPlayerChannel
            && PlayerSafetyHelper.IsPlayerIntentChannelActive(context.Player))
        {
            return;
        }

        _scheduler.Reset();
        foreach (var module in _modules)
        {
            module.CollectCandidates(context, _scheduler, isMoving);
        }

        if (inCombat && ActionService.CanExecuteOgcd)
        {
            _scheduler.DispatchOgcd(context);
        }
        if (ActionService.CanExecuteGcd)
        {
            _scheduler.DispatchGcd(context);
        }

        UpdateModuleDebugStates(context);
    }

    /// <inheritdoc />
    protected override void SyncDebugState(IHephaestusContext context)
    {
        // Map tank debug state to common debug state fields
        _debugState.PlanningState = _hephaestusDebugState.DamageState;
        _debugState.PlannedAction = _hephaestusDebugState.PlannedAction;
        _debugState.DpsState = _hephaestusDebugState.DamageState;
        _debugState.DefensiveState = _hephaestusDebugState.MitigationState;

        // Party/player info
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
