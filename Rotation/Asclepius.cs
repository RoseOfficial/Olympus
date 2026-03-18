using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Rotation.Base;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Sage;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation;

/// <summary>
/// Sage rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Asclepius, the Greek god of medicine.
/// </summary>
[Rotation("Asclepius", JobRegistry.Sage, Role = RotationRole.Healer)]
public sealed class Asclepius : BaseHealerRotation<IAsclepiusContext, IAsclepiusModule>
{
    /// <inheritdoc />
    public override string Name => "Asclepius";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Sage];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IAsclepiusModule> Modules => _modules;

    /// <inheritdoc />
    protected override HealerPartyHelper HealerParty => _partyHelper;

    /// <summary>
    /// Gets the Asclepius-specific debug state.
    /// </summary>
    public AsclepiusDebugState AsclepiusDebug => _debugState;

    // Sage-specific services
    private readonly IAddersgallTrackingService _addersgallService;
    private readonly IAdderstingTrackingService _adderstingService;
    private readonly IKardiaManager _kardiaManager;
    private readonly IEukrasiaStateService _eukrasiaService;

    // Debug state
    private readonly AsclepiusDebugState _debugState = new();

    // Helpers
    private readonly AsclepiusStatusHelper _statusHelper;
    private readonly PartyHelper _partyHelper;

    // Timeline integration
    private readonly ITimelineService? _timelineService;

    // Training mode
    private readonly ITrainingService? _trainingService;

    // Modules (sorted by priority)
    private readonly List<IAsclepiusModule> _modules;

    public Asclepius(
        IPluginLog log,
        ActionTracker actionTracker,
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

        // Initialize Sage-specific services
        _addersgallService = new AddersgallTrackingService();
        _adderstingService = new AdderstingTrackingService();
        _kardiaManager = new KardiaManager(partyList);
        _eukrasiaService = new EukrasiaStateService();

        // Initialize helpers
        _statusHelper = new AsclepiusStatusHelper();
        _partyHelper = new PartyHelper(objectTable, partyList, hpPredictionService, configuration);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IAsclepiusModule>
        {
            new KardiaModule(),         // Priority 3 - Ensure Kardia is placed
            new ResurrectionModule(),   // Priority 5 - Raise dead party members
            new HealingModule(),        // Priority 10 - Addersgall heals, oGCDs, GCD heals
            new DefensiveModule(),      // Priority 20 - Kerachole, Taurochole, Holos, Panhaima
            new DamageModule(),         // Priority 50 - DoT, Dosis, Phlegma, Psyche
        };

        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        // Declare healer role for multi-healer coordination
        PartyCoordinationService?.DeclareHealerRole(JobRegistry.Sage, Configuration.PartyCoordination.PreferredHealerRole);

        Log.Info("Asclepius (Sage) rotation initialized");
    }

    /// <inheritdoc />
    protected override void BroadcastHealerGaugeState(IPlayerCharacter player)
    {
        var addersgall = _addersgallService.CurrentStacks;
        var addersting = _adderstingService.CurrentStacks;
        PartyCoordinationService?.BroadcastGaugeState(JobRegistry.Sage, addersgall, addersting, 0);
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            AsclepiusStatusHelper.HasLucidDreaming(player));
    }

    /// <inheritdoc />
    protected override void UpdateJobSpecificServices(IPlayerCharacter player, bool inCombat)
    {
        // Call base healer service updates
        base.UpdateJobSpecificServices(player, inCombat);

        // Update Kardia target tracking
        _kardiaManager.UpdateKardiaTarget(player);

        // Update Sage-specific debug state
        _debugState.AddersgallStacks = _addersgallService.CurrentStacks;
        _debugState.AddersgallTimer = _addersgallService.TimerRemaining;
        _debugState.AdderstingStacks = _adderstingService.CurrentStacks;
        _debugState.EukrasiaActive = _eukrasiaService.IsEukrasiaActive(player);
        _debugState.ZoeActive = _eukrasiaService.IsZoeActive(player);
        _debugState.KardiaTarget = _kardiaManager.HasKardia ? $"ID: {_kardiaManager.CurrentKardiaTarget}" : "None";
        _debugState.SoteriaStacks = _kardiaManager.GetSoteriaStacks(player);
        _debugState.PlayerHpPercent = player.MaxHp > 0 ? (float)player.CurrentHp / player.MaxHp : 1f;

        // Populate shared DebugState resource fields for the debug snapshot
        _debugState.LilyCount = _debugState.AddersgallStacks;
        _debugState.BloodLilyCount = _debugState.AdderstingStacks;
        _debugState.LilyStrategy = _debugState.AddersgallStrategy;
    }

    /// <inheritdoc />
    protected override IAsclepiusContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new AsclepiusContext(
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
            addersgallService: _addersgallService,
            adderstingService: _adderstingService,
            kardiaManager: _kardiaManager,
            eukrasiaService: _eukrasiaService,
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

    #endregion

}
