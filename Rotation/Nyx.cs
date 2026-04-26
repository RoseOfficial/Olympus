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
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Helpers;
using Olympus.Rotation.NyxCore.Modules;
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
/// Dark Knight rotation module (scheduler-driven execution).
/// Named after Nyx, the Greek goddess of night.
/// </summary>
[Rotation("Nyx", JobRegistry.DarkKnight, Role = RotationRole.Tank)]
public sealed class Nyx : BaseTankRotation<INyxContext, INyxModule>
{
    public override string Name => "Nyx";
    public override uint[] SupportedJobIds => [JobRegistry.DarkKnight];
    public override DebugState DebugState => _debugState;
    protected override List<INyxModule> Modules => _modules;

    public NyxDebugState NyxDebug => _nyxDebugState;

    private readonly NyxDebugState _nyxDebugState = new();
    private readonly DebugState _debugState = new();
    private readonly NyxStatusHelper _statusHelper;
    private readonly NyxPartyHelper _partyHelper;
    private readonly List<INyxModule> _modules;
    private readonly ITrainingService? _trainingService;
    private readonly IBurstWindowService? _burstWindowService;
    private readonly RotationScheduler _scheduler;
    private float _darksideTimer;

    public Nyx(
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

        _statusHelper = new NyxStatusHelper();
        _partyHelper = new NyxPartyHelper(objectTable, partyList);

        _modules = new List<INyxModule>
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
        var bloodGauge = SafeGameAccess.GetDrkBloodGauge(ErrorMetrics);
        _darksideTimer = SafeGameAccess.GetDrkDarksideTimer(ErrorMetrics);
        return bloodGauge;
    }

    protected override int DetermineComboStep(uint comboAction, float comboTimer)
        => ComputeComboStep(comboAction, comboTimer);

    internal static int ComputeComboStep(uint comboAction, float comboTimer)
    {
        if (comboAction == 0 || comboTimer <= 0) return 0;
        if (comboAction == DRKActions.HardSlash.ActionId) return 1;
        if (comboAction == DRKActions.SyphonStrike.ActionId) return 2;
        if (comboAction == DRKActions.Unleash.ActionId) return 1;
        return 0;
    }

    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        MpForecastService.Update((int)player.CurrentMp, (int)player.MaxMp, hasLucidDreaming: false);
    }

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
            timelineService: TimelineService,
            partyCoordinationService: PartyCoordinationService,
            trainingService: _trainingService,
            log: Log);
    }

    protected override void ExecuteModules(INyxContext context, bool isMoving, bool inCombat)
    {
        if (Configuration.Targeting.PauseAllOnStandStillPunisher
            && PlayerSafetyHelper.IsStandStillPunisherActive(context.Player))
            return;
        if (Configuration.Targeting.PauseOnPlayerChannel
            && PlayerSafetyHelper.IsPlayerIntentChannelActive(context.Player))
            return;

        if (TryDispatchTincture(context, inCombat)) return;

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

    protected override void SyncDebugState(INyxContext context)
    {
        _debugState.PlanningState = _nyxDebugState.DamageState;
        _debugState.PlannedAction = _nyxDebugState.PlannedAction;
        _debugState.DpsState = _nyxDebugState.DamageState;
        _debugState.DefensiveState = _nyxDebugState.MitigationState;
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }
}
