using System.Collections.Generic;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Rotation.Base;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation;

/// <summary>
/// White Mage rotation module (scheduler-driven execution).
/// Named after Apollo, the Greek god of healing and light.
/// </summary>
[Rotation("Apollo", JobRegistry.WhiteMage, JobRegistry.Conjurer, Role = RotationRole.Healer)]
public sealed class Apollo : BaseHealerRotation<IApolloContext, IApolloModule>
{
    public override string Name => "Apollo";
    public override uint[] SupportedJobIds => [JobRegistry.WhiteMage, JobRegistry.Conjurer];
    public override DebugState DebugState => _debugState;
    protected override List<IApolloModule> Modules => _modules;
    protected override HealerPartyHelper HealerParty => _partyHelper;

    private readonly DebugState _debugState = new();
    private readonly StatusHelper _statusHelper;
    private readonly PartyHelper _partyHelper;
    private readonly ITimelineService? _timelineService;
    private readonly ITrainingService? _trainingService;
    private readonly List<IApolloModule> _modules;
    private readonly RotationScheduler _scheduler;

    public Apollo(
        IPluginLog log,
        IActionTracker actionTracker,
        CombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
        IDamageTrendService damageTrendService,
        Configuration configuration,
        IObjectTable objectTable,
        IPartyList partyList,
        TargetingService targetingService,
        HpPredictionService hpPredictionService,
        ActionService actionService,
        PlayerStatsService playerStatsService,
        HealingSpellSelector healingSpellSelector,
        DebuffDetectionService debuffDetectionService,
        ICooldownPlanner cooldownPlanner,
        ShieldTrackingService shieldTrackingService,
        IJobGauges jobGauges,
        ITimelineService? timelineService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITrainingService? trainingService = null,
        IErrorMetricsService? errorMetrics = null)
        : base(log, actionTracker, combatEventService, damageIntakeService, damageTrendService,
               configuration, objectTable, partyList, targetingService, hpPredictionService,
               actionService, playerStatsService, debuffDetectionService, healingSpellSelector,
               cooldownPlanner, shieldTrackingService, partyCoordinationService, errorMetrics)
    {
        _timelineService = timelineService;
        _trainingService = trainingService;

        _scheduler = new RotationScheduler(actionService, jobGauges, configuration, timelineService, errorMetrics);

        _statusHelper = new StatusHelper();
        _partyHelper = new PartyHelper(objectTable, partyList, hpPredictionService, configuration);

        _modules = new List<IApolloModule>
        {
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
            new BuffModule(),
            new DamageModule(),
        };
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        PartyCoordinationService?.DeclareHealerRole(JobRegistry.WhiteMage, Configuration.PartyCoordination.PreferredHealerRole);
    }

    protected override void BroadcastHealerGaugeState(IPlayerCharacter player)
    {
        var lilyCount = StatusHelper.GetLilyCount();
        var bloodLily = StatusHelper.GetBloodLilyCount();
        PartyCoordinationService?.BroadcastGaugeState(JobRegistry.WhiteMage, lilyCount, bloodLily, 0);
    }

    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        MpForecastService.Update((int)player.CurrentMp, (int)player.MaxMp, StatusHelper.HasLucidDreaming(player));
    }

    protected override IApolloContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new ApolloContext(
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
            healingSpellSelector: HealingSpellSelector,
            hpPredictionService: HpPredictionService,
            mpForecastService: MpForecastService,
            objectTable: ObjectTable,
            partyList: PartyList,
            playerStatsService: PlayerStatsService,
            targetingService: TargetingService,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            cooldownPlanner: CooldownPlanner,
            coHealerDetectionService: CoHealerDetectionService,
            bossMechanicDetector: BossMechanicDetector,
            shieldTrackingService: ShieldTrackingService,
            partyCoordinationService: PartyCoordinationService,
            timelineService: _timelineService,
            trainingService: _trainingService,
            debugState: _debugState,
            log: Log);
    }

    /// <summary>
    /// Scheduler-aware execution. Runs CollectCandidates per module (no-op for healer modules
    /// until deep migration), then the authoritative legacy TryExecute priority chain. Scheduler
    /// Dispatch calls are safe no-ops when no candidates are pushed.
    /// </summary>
    protected override void ExecuteModules(IApolloContext context, bool isMoving, bool inCombat)
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

        if (inCombat && ActionService.CanExecuteOgcd)
        {
            foreach (var module in _modules)
                if (module.TryExecute(context, isMoving)) return;
            _scheduler.DispatchOgcd(context);
        }

        if (ActionService.CanExecuteGcd)
        {
            foreach (var module in _modules)
                if (module.TryExecute(context, isMoving)) return;
            _scheduler.DispatchGcd(context);
        }
    }
}
