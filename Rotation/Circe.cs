using System.Collections.Generic;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Base;
using Olympus.Rotation.Common;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
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

    // Melee combo step tracking (0=None, 1=Zwerchhau next, 2=Redoublement next,
    // 3=Finisher next, 4=Scorch next, 5=Resolution next).
    // Computed via action replacement on Enchanted Riposte + ManaStacks + the game's
    // combo field. Replaces the old raw combo-action/combo-timer pair which was
    // unreliable for the Enchanted chain.
    private int _meleeComboStep;

    // Moulinet (AoE melee) combo step tracking (0=None, 1=Deux next, 2=Trois next).
    // Computed via action replacement on Enchanted Moulinet.
    private int _moulinetStep;

    // Scheduler
    private readonly RotationScheduler _scheduler;

    public Circe(
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
        IJobGauges jobGauges,
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
            burstWindowService,
            errorMetrics)
    {
        _timelineService = timelineService;
        _partyCoordinationService = partyCoordinationService;
        _trainingService = trainingService;

        _scheduler = new RotationScheduler(actionService, jobGauges, configuration, timelineService, errorMetrics);

        // Initialize helpers
        _statusHelper = new CirceStatusHelper();
        _partyHelper = new CasterPartyHelper(objectTable, partyList);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<ICirceModule>
        {
            new ResurrectionModule(),              // Priority 15 - Raise dead party members (Dualcast/Swiftcast Verraise)
            new BuffModule(BurstWindowService),    // Priority 20 - oGCD management (Fleche, Contre Sixte, Embolden, etc.)
            new DamageModule(BurstWindowService, SmartAoEService),  // Priority 30 - GCD rotation (Dualcast, melee combo, finishers)
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

        UpdateMeleeComboStep();
        UpdateMoulinetStep();
    }

    /// <summary>
    /// Updates the Moulinet (AoE melee) combo step using action replacement on
    /// Enchanted Moulinet. The chain is Moulinet → Moulinet Deux → Moulinet Trois.
    /// Only the Deux/Trois steps exist at Lv.96+; below that, Moulinet is a single hit.
    /// </summary>
    private unsafe void UpdateMoulinetStep()
    {
        _moulinetStep = 0;

        var actionManager = SafeGameAccess.GetActionManager(ErrorMetrics);
        if (actionManager == null)
            return;

        var adjustedId = actionManager->GetAdjustedActionId(RDMActions.EnchantedMoulinet.ActionId);
        if (adjustedId == RDMActions.EnchantedMoulinetDeux.ActionId)
            _moulinetStep = 1; // Deux next
        else if (adjustedId == RDMActions.EnchantedMoulinetTrois.ActionId)
            _moulinetStep = 2; // Trois next
    }

    /// <summary>
    /// Updates the melee combo step using action replacement on Enchanted Riposte
    /// for steps 1-2 (Zwerchhau/Redoublement), Mana Stacks for step 3 (Finisher),
    /// and the game's combo field for steps 4-5 (Scorch/Resolution).
    /// Action replacement is used rather than raw combo tracking because the game's
    /// combo field is unreliable for the Enchanted melee chain.
    /// </summary>
    private unsafe void UpdateMeleeComboStep()
    {
        _meleeComboStep = 0;

        var actionManager = SafeGameAccess.GetActionManager(ErrorMetrics);
        if (actionManager == null)
            return;

        // Steps 1-2: action replacement from Enchanted Riposte
        var adjustedId = actionManager->GetAdjustedActionId(RDMActions.EnchantedRiposte.ActionId);
        if (adjustedId == RDMActions.EnchantedZwerchhau.ActionId)
        {
            _meleeComboStep = 1; // Zwerchhau next
            return;
        }
        if (adjustedId == RDMActions.EnchantedRedoublement.ActionId)
        {
            _meleeComboStep = 2; // Redoublement next
            return;
        }

        // Step 3: Finisher (Verflare/Verholy) becomes available at 3 Mana Stacks,
        // which is granted by Enchanted Redoublement.
        if (_manaStacks >= 3)
        {
            _meleeComboStep = 3;
            return;
        }

        // Steps 4-5: Scorch/Resolution are chained via the vanilla combo system,
        // so the game's combo field reliably tracks them.
        var comboAction = SafeGameAccess.GetComboAction(ErrorMetrics);
        var comboTimer = SafeGameAccess.GetComboTimer(ErrorMetrics);
        if (comboTimer <= 0)
            return;

        if (comboAction == RDMActions.Verflare.ActionId || comboAction == RDMActions.Verholy.ActionId)
            _meleeComboStep = 4; // Scorch next
        else if (comboAction == RDMActions.Scorch.ActionId)
            _meleeComboStep = 5; // Resolution next
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
            meleeComboStep: _meleeComboStep,
            moulinetStep: _moulinetStep,
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

    /// <inheritdoc />
    protected override void ExecuteModules(ICirceContext context, bool isMoving, bool inCombat)
    {
        if (Configuration.Targeting.PauseAllOnStandStillPunisher
            && PlayerSafetyHelper.IsStandStillPunisherActive(context.Player))
            return;
        if (Configuration.Targeting.PauseOnPlayerChannel
            && PlayerSafetyHelper.IsPlayerIntentChannelActive(context.Player))
            return;

        _scheduler.Reset();
        foreach (var module in _modules)
            module.CollectCandidates(context, _scheduler, isMoving);

        // RDM ResurrectionModule fires Verraise via Dualcast/Swiftcast both pre and
        // post combat (raise during phase resets, downtime). Drop the inCombat gate
        // on the oGCD pass so Swiftcast can dispatch out of combat.
        if (ActionService.CanExecuteOgcd)
            _scheduler.DispatchOgcd(context);

        if (ActionService.CanExecuteGcd)
            _scheduler.DispatchGcd(context);
    }

    #endregion
}
