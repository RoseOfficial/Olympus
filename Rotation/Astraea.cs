using System.Collections.Generic;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Rotation.Base;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Astrologian;
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
/// Astrologian rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Astraea, the Greek goddess of stars and justice.
/// </summary>
[Rotation("Astraea", JobRegistry.Astrologian, Role = RotationRole.Healer)]
public sealed class Astraea : BaseHealerRotation<IAstraeaContext, IAstraeaModule>
{
    /// <inheritdoc />
    public override string Name => "Astraea";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Astrologian];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IAstraeaModule> Modules => _modules;

    /// <inheritdoc />
    protected override HealerPartyHelper HealerParty => _partyHelper;

    /// <summary>
    /// Gets the Astraea-specific debug state.
    /// </summary>
    public AstraeaDebugState AstraeaDebug => _debugState;

    // Astrologian-specific services
    private readonly CardTrackingService _cardService;
    private readonly EarthlyStarService _earthlyStarService;

    // Debug state
    private readonly AstraeaDebugState _debugState = new();

    // Helpers
    private readonly AstraeaStatusHelper _statusHelper;
    private readonly AstraeaPartyHelper _partyHelper;

    // Timeline integration
    private readonly ITimelineService? _timelineService;

    // Training mode
    private readonly ITrainingService? _trainingService;

    // Modules (sorted by priority)
    private readonly List<IAstraeaModule> _modules;

    // Scheduler
    private readonly RotationScheduler _scheduler;

    public Astraea(
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
        DebuffDetectionService debuffDetectionService,
        ICooldownPlanner cooldownPlanner,
        HealingSpellSelector healingSpellSelector,
        ShieldTrackingService shieldTrackingService,
        IJobGauges jobGauges,
        ITimelineService? timelineService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITrainingService? trainingService = null,
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
            healingSpellSelector,
            cooldownPlanner,
            shieldTrackingService,
            partyCoordinationService,
            errorMetrics)
    {
        // Store timeline service
        _timelineService = timelineService;

        // Store training service
        _trainingService = trainingService;

        // Initialize Astrologian-specific services
        _cardService = new CardTrackingService(jobGauges);

        // Initialize scheduler
        _scheduler = new RotationScheduler(actionService, jobGauges, configuration, timelineService, errorMetrics);
        _earthlyStarService = new EarthlyStarService(objectTable);

        // Initialize helpers
        _statusHelper = new AstraeaStatusHelper();
        _partyHelper = new AstraeaPartyHelper(objectTable, partyList, hpPredictionService, configuration, _statusHelper);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IAstraeaModule>
        {
            new CardModule(),           // Priority 3 - Cards should be played immediately
            new ResurrectionModule(),   // Priority 5 - Dead members are useless
            new HealingModule(),        // Priority 10 - Keep party alive
            new DefensiveModule(),      // Priority 20 - Mitigation (Neutral Sect, etc.)
            new BuffModule(),           // Priority 30 - Buffs (Lightspeed, Lucid)
            new DamageModule(),         // Priority 50 - DPS when safe
        };

        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        // Declare healer role for multi-healer coordination
        PartyCoordinationService?.DeclareHealerRole(JobRegistry.Astrologian, Configuration.PartyCoordination.PreferredHealerRole);

        Log.Info("Astraea (Astrologian) rotation initialized");
    }

    /// <inheritdoc />
    protected override void BroadcastHealerGaugeState(IPlayerCharacter player)
    {
        var sealCount = _cardService.SealCount;
        var hasCard = _cardService.CurrentCard != Data.ASTActions.CardType.None ? 1 : 0;
        PartyCoordinationService?.BroadcastGaugeState(JobRegistry.Astrologian, sealCount, hasCard, 0);
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            AstraeaStatusHelper.HasLucidDreaming(player));
    }

    /// <inheritdoc />
    protected override void UpdateJobSpecificServices(IPlayerCharacter player, bool inCombat)
    {
        // Call base healer service updates
        base.UpdateJobSpecificServices(player, inCombat);

        // Update Earthly Star tracking
        _earthlyStarService.Update();

        // Update Astrologian-specific debug state
        _debugState.CurrentCardType = _cardService.CurrentCard.ToString();
        _debugState.MinorArcanaType = _cardService.MinorArcanaCard.ToString();
        _debugState.SealCount = _cardService.SealCount;
        _debugState.UniqueSealCount = _cardService.UniqueSealCount;
        _debugState.IsStarMature = _earthlyStarService.IsStarMature;
        _debugState.StarTimeRemaining = _earthlyStarService.TimeRemaining;
        _debugState.PlayerHpPercent = player.MaxHp > 0 ? (float)player.CurrentHp / player.MaxHp : 1f;

        // Populate shared DebugState fields for the debug snapshot
        _debugState.AoEStatus = _debugState.AoEHealState;
        _debugState.LilyCount = _debugState.SealCount;
        _debugState.BloodLilyCount = _debugState.UniqueSealCount;
        _debugState.LilyStrategy = _debugState.CardState;
    }

    /// <inheritdoc />
    protected override IAstraeaContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new AstraeaContext(
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
            objectTable: ObjectTable,
            partyList: PartyList,
            playerStatsService: PlayerStatsService,
            targetingService: TargetingService,
            cardService: _cardService,
            earthlyStarService: _earthlyStarService,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            cooldownPlanner: CooldownPlanner,
            healingSpellSelector: HealingSpellSelector,
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
    /// Scheduler-aware execution with phased dispatch. Resurrection (priority 5) and
    /// Healing (priority 10) are migrated; CardModule (priority 3) sits above them
    /// and stays on legacy. Defensive (20), Buff (30), Damage (50) are below.
    /// HealingModule has hybrid handlers — most migrated to scheduler, but
    /// EarthlyStarPlacement (ground-targeted) and following priorities stay legacy.
    /// Order: CardModule legacy -> scheduler dispatch -> other legacy modules
    /// (HealingModule's TryExecute fires its own legacy handlers).
    /// </summary>
    protected override void ExecuteModules(IAstraeaContext context, bool isMoving, bool inCombat)
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
                if (module.Priority < 5 && module.TryExecute(context, isMoving)) return;

            if (_scheduler.DispatchOgcd(context).Dispatched) return;

            foreach (var module in _modules)
                if (module.Priority >= 5 && module.TryExecute(context, isMoving)) return;
        }

        if (ActionService.CanExecuteGcd)
        {
            foreach (var module in _modules)
                if (module.Priority < 5 && module.TryExecute(context, isMoving)) return;

            if (_scheduler.DispatchGcd(context).Dispatched) return;

            foreach (var module in _modules)
                if (module.Priority >= 5 && module.TryExecute(context, isMoving)) return;
        }
    }

    #endregion

}
