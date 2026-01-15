using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
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
    public override DebugState DebugState => ConvertToApolloDebugState();

    /// <inheritdoc />
    protected override List<IAsclepiusModule> Modules => _modules;

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
    private readonly IPartyHelper _partyHelper;

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
            errorMetrics)
    {
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
            new DamageModule(),         // Priority 50 - DoT, Dosis, Phlegma, Psyche
        };

        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        Log.Info("Asclepius (Sage) rotation initialized");
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
            debugState: _debugState,
            log: Log);
    }

    /// <inheritdoc />
    protected override IEnumerable<uint> GetPartyEntityIds(IPlayerCharacter player)
    {
        foreach (var member in _partyHelper.GetAllPartyMembers(player))
        {
            yield return member.EntityId;
        }
    }

    /// <inheritdoc />
    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(IPlayerCharacter player)
    {
        return _partyHelper.CalculatePartyHealthMetrics(player);
    }

    #endregion

    /// <summary>
    /// Converts Asclepius debug state to Apollo debug state for UI compatibility.
    /// </summary>
    private DebugState ConvertToApolloDebugState()
    {
        return new DebugState
        {
            PlanningState = _debugState.PlanningState,
            PlannedAction = _debugState.PlannedAction,
            AoEInjuredCount = _debugState.AoEInjuredCount,
            AoEStatus = _debugState.AoEStatus,
            PlayerHpPercent = _debugState.PlayerHpPercent,
            PartyListCount = _debugState.PartyListCount,
            PartyValidCount = _debugState.PartyValidCount,
            DpsState = _debugState.DpsState,
            AoEDpsState = _debugState.AoEDpsState,
            AoEDpsEnemyCount = _debugState.AoEDpsEnemyCount,
            LastHealAmount = _debugState.LastHealAmount,
            LastHealStats = _debugState.LastHealStats,
            RaiseState = _debugState.RaiseState,
            RaiseTarget = _debugState.RaiseTarget,
            EsunaState = _debugState.EsunaState,
            EsunaTarget = _debugState.EsunaTarget,
            LucidState = _debugState.LucidState,
            // SGE-specific mappings for display compatibility
            LilyCount = _debugState.AddersgallStacks, // Map Addersgall to Lily count for display
            BloodLilyCount = _debugState.AdderstingStacks, // Map Addersting to Blood Lily
            LilyStrategy = _debugState.AddersgallStrategy,
        };
    }
}
