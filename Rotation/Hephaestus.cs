using System.Collections.Generic;
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

    // Gnashing Fang combo step tracking
    private int _gnashingFangStep;

    // Reign of Beasts combo step tracking (0=none, 1=Noble Blood next, 2=Lion Heart next)
    private int _reignComboStep;

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
        ITimelineService? timelineService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITrainingService? trainingService = null,
        IErrorMetricsService? errorMetrics = null,
        IBurstWindowService? burstWindowService = null)
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
            errorMetrics)
    {
        // Initialize training service
        _trainingService = trainingService;
        _burstWindowService = burstWindowService;

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
    /// Updates the Gnashing Fang combo step via action replacement detection.
    /// After using Gnashing Fang, the game replaces the action with Savage Claw, then Wicked Talon.
    /// This state persists across frames regardless of whether the Continuation oGCDs
    /// (Jugular Rip, Abdomen Tear, Eye Gouge) have consumed their Ready buffs.
    /// Tracking via the Ready buffs alone is unreliable because they are consumed by the
    /// oGCD weave before the next GCD frame, which caused the combo to drop.
    /// </summary>
    private unsafe void UpdateGnashingFangStep()
    {
        var actionManager = SafeGameAccess.GetActionManager(ErrorMetrics);
        if (actionManager == null)
        {
            _gnashingFangStep = 0;
            return;
        }

        var adjustedId = actionManager->GetAdjustedActionId(GNBActions.GnashingFang.ActionId);

        if (adjustedId == GNBActions.SavageClaw.ActionId)
            _gnashingFangStep = 1; // After Gnashing Fang, Savage Claw next
        else if (adjustedId == GNBActions.WickedTalon.ActionId)
            _gnashingFangStep = 2; // After Savage Claw, Wicked Talon next
        else
            _gnashingFangStep = 0; // Not in combo
    }

    /// <summary>
    /// Updates the Reign of Beasts combo step using action replacement detection.
    /// After using Reign of Beasts, the game replaces the action with Noble Blood, then Lion Heart.
    /// The ReadyToReign buff is consumed at step 1, so we track steps 2/3 via GetAdjustedActionId.
    /// </summary>
    private unsafe void UpdateReignComboStep()
    {
        var actionManager = SafeGameAccess.GetActionManager(ErrorMetrics);
        if (actionManager == null)
        {
            _reignComboStep = 0;
            return;
        }

        var adjustedId = actionManager->GetAdjustedActionId(GNBActions.ReignOfBeasts.ActionId);

        if (adjustedId == GNBActions.NobleBlood.ActionId)
            _reignComboStep = 1; // After Reign of Beasts, Noble Blood next
        else if (adjustedId == GNBActions.LionHeart.ActionId)
            _reignComboStep = 2; // After Noble Blood, Lion Heart next
        else
            _reignComboStep = 0; // Not in Reign combo
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
