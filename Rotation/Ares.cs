using System.Collections.Generic;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Base;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Helpers;
using Olympus.Rotation.AresCore.Modules;
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
/// Warrior rotation module (scheduler-driven execution).
/// Named after Ares, the Greek god of war and battle fury.
/// </summary>
[Rotation("Ares", JobRegistry.Warrior, JobRegistry.Marauder, Role = RotationRole.Tank)]
public sealed class Ares : BaseTankRotation<IAresContext, IAresModule>
{
    public override string Name => "Ares";
    public override uint[] SupportedJobIds => [JobRegistry.Warrior, JobRegistry.Marauder];
    public override DebugState DebugState => _debugState;
    protected override List<IAresModule> Modules => _modules;

    public AresDebugState AresDebug => _aresDebugState;

    private readonly AresDebugState _aresDebugState = new();
    private readonly DebugState _debugState = new();
    private readonly AresStatusHelper _statusHelper;
    private readonly AresPartyHelper _partyHelper;
    private readonly ITrainingService? _trainingService;
    private readonly IBurstWindowService? _burstWindowService;
    private readonly List<IAresModule> _modules;
    private readonly RotationScheduler _scheduler;

    public Ares(
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
        : base(log, actionTracker, combatEventService, damageIntakeService, damageTrendService,
               configuration, objectTable, partyList, targetingService, hpPredictionService,
               actionService, playerStatsService, debuffDetectionService, enmityService,
               tankCooldownService, timelineService, partyCoordinationService, errorMetrics,
               tinctureDispatcher, pullIntentService)
    {
        _trainingService = trainingService;
        _burstWindowService = burstWindowService;

        _scheduler = new RotationScheduler(actionService, jobGauges, configuration, timelineService, errorMetrics);

        _statusHelper = new AresStatusHelper();
        _partyHelper = new AresPartyHelper(objectTable, partyList);

        _modules = new List<IAresModule>
        {
            new EnmityModule(),
            new MitigationModule(),
            new BuffModule(_burstWindowService),
            new DamageModule(),
        };

        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    protected override int ReadGaugeValue()
    {
        return SafeGameAccess.GetWarBeastGauge(ErrorMetrics);
    }

    protected override int DetermineComboStep(uint comboAction, float comboTimer)
        => ComputeComboStep(comboAction, comboTimer);

    internal static int ComputeComboStep(uint comboAction, float comboTimer)
    {
        if (comboAction == 0 || comboTimer <= 0) return 0;
        if (comboAction == WARActions.HeavySwing.ActionId) return 1;
        if (comboAction == WARActions.Maim.ActionId) return 2;
        if (comboAction == WARActions.Overpower.ActionId) return 1;
        return 0;
    }

    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        MpForecastService.Update((int)player.CurrentMp, (int)player.MaxMp, hasLucidDreaming: false);
    }

    protected override IAresContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new AresContext(
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
            debugState: _aresDebugState,
            beastGauge: GaugeValue,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            timelineService: TimelineService,
            partyCoordinationService: PartyCoordinationService,
            trainingService: _trainingService,
            log: Log);
    }

    protected override void ExecuteModules(IAresContext context, bool isMoving, bool inCombat)
    {
        if (TryDispatchTincture(context, inCombat)) return;

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
    }

    protected override void SyncDebugState(IAresContext context)
    {
        _debugState.PlanningState = _aresDebugState.DamageState;
        _debugState.PlannedAction = _aresDebugState.PlannedAction;
        _debugState.DpsState = _aresDebugState.DamageState;
        _debugState.DefensiveState = _aresDebugState.MitigationState;
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }
}
