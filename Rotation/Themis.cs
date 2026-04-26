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
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Rotation.ThemisCore.Helpers;
using Olympus.Rotation.ThemisCore.Modules;
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
/// Paladin rotation module (scheduler-driven execution).
/// Named after Themis, the Greek goddess of divine law and order.
/// </summary>
[Rotation("Themis", JobRegistry.Paladin, JobRegistry.Gladiator, Role = RotationRole.Tank)]
public sealed class Themis : BaseTankRotation<IThemisContext, IThemisModule>
{
    /// <inheritdoc />
    public override string Name => "Themis";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Paladin, JobRegistry.Gladiator];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IThemisModule> Modules => _modules;

    /// <summary>
    /// Gets the Themis-specific debug state.
    /// </summary>
    public ThemisDebugState ThemisDebug => _themisDebugState;

    private readonly ThemisDebugState _themisDebugState = new();
    private readonly DebugState _debugState = new();
    private readonly ThemisStatusHelper _statusHelper;
    private readonly ThemisPartyHelper _partyHelper;
    private readonly ITrainingService? _trainingService;
    private readonly IBurstWindowService? _burstWindowService;
    private readonly List<IThemisModule> _modules;

    // Scheduler (per-rotation, per-frame priority queue)
    private readonly RotationScheduler _scheduler;

    public Themis(
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
        _trainingService = trainingService;
        _burstWindowService = burstWindowService;

        _scheduler = new RotationScheduler(
            actionService,
            jobGauges,
            configuration,
            timelineService,
            errorMetrics);

        _statusHelper = new ThemisStatusHelper();
        _partyHelper = new ThemisPartyHelper(objectTable, partyList);

        _modules = new List<IThemisModule>
        {
            new EnmityModule(),
            new MitigationModule(),
            new BuffModule(_burstWindowService),
            new DamageModule(),
        };

        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override int ReadGaugeValue()
    {
        return SafeGameAccess.GetPldOathGauge(ErrorMetrics);
    }

    /// <inheritdoc />
    protected override int DetermineComboStep(uint comboAction, float comboTimer)
        => ComputeComboStep(comboAction, comboTimer);

    internal static int ComputeComboStep(uint comboAction, float comboTimer)
    {
        if (comboAction == 0 || comboTimer <= 0)
            return 0;

        if (comboAction == PLDActions.FastBlade.ActionId)
            return 2;

        if (comboAction == PLDActions.RiotBlade.ActionId)
            return 3;

        if (comboAction == PLDActions.TotalEclipse.ActionId)
            return 2;

        return 0;
    }

    /// <inheritdoc />
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            hasLucidDreaming: false);
    }

    /// <inheritdoc />
    protected override IThemisContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new ThemisContext(
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
            debugState: _themisDebugState,
            oathGauge: GaugeValue,
            comboStep: ComboStep,
            lastComboAction: LastComboAction,
            comboTimeRemaining: ComboTimeRemaining,
            timelineService: TimelineService,
            partyCoordinationService: PartyCoordinationService,
            trainingService: _trainingService,
            log: Log);
    }

    /// <inheritdoc />
    protected override void ExecuteModules(IThemisContext context, bool isMoving, bool inCombat)
    {
        // Preserve BaseRotation's safety pauses.
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

    /// <inheritdoc />
    protected override void SyncDebugState(IThemisContext context)
    {
        _debugState.PlanningState = $"GCD:{_themisDebugState.GcdState} Rem:{_themisDebugState.GcdRemaining:F2}s Combat:{_themisDebugState.InCombat} CanGCD:{_themisDebugState.CanExecuteGcd} Tgt:{_themisDebugState.CurrentTarget}";
        _debugState.PlannedAction = _themisDebugState.PlannedAction;
        _debugState.DpsState = _themisDebugState.DamageState;
        _debugState.DefensiveState = _themisDebugState.MitigationState;
        _debugState.PlayerHpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
        _debugState.PartyListCount = context.PartyList.Length;
    }

    #endregion
}
