using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Helpers;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Rotation.Base;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Scholar;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;

namespace Olympus.Rotation;

/// <summary>
/// Scholar rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Athena, the Greek goddess of wisdom and strategic warfare.
/// </summary>
[Rotation("Athena", JobRegistry.Scholar, JobRegistry.Arcanist, Role = RotationRole.Healer)]
public sealed class Athena : BaseHealerRotation<AthenaContext, IAthenaModule>
{
    /// <inheritdoc />
    public override string Name => "Athena";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Scholar, JobRegistry.Arcanist];

    /// <inheritdoc />
    public override DebugState DebugState => ConvertToApolloDebugState();

    /// <inheritdoc />
    protected override List<IAthenaModule> Modules => _modules;

    /// <summary>
    /// Gets the Athena-specific debug state.
    /// </summary>
    public AthenaDebugState AthenaDebug => _debugState;

    // Scholar-specific services
    private readonly AetherflowTrackingService _aetherflowService;
    private readonly FairyGaugeService _fairyGaugeService;
    private readonly FairyStateManager _fairyStateManager;

    // Debug state
    private readonly AthenaDebugState _debugState = new();

    // Helpers
    private readonly AthenaStatusHelper _statusHelper;
    private readonly AthenaPartyHelper _partyHelper;

    // Modules (sorted by priority)
    private readonly List<IAthenaModule> _modules;

    public Athena(
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
        // Initialize Scholar-specific services
        _aetherflowService = new AetherflowTrackingService();
        _fairyGaugeService = new FairyGaugeService();
        _fairyStateManager = new FairyStateManager(objectTable);

        // Initialize helpers
        _statusHelper = new AthenaStatusHelper();
        _partyHelper = new AthenaPartyHelper(objectTable, partyList, hpPredictionService, configuration, _statusHelper);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IAthenaModule>
        {
            new FairyModule(),           // Priority 3 - Summon fairy if needed
            new ResurrectionModule(),    // Priority 5 - Dead members are useless
            new HealingModule(),         // Priority 10 - Keep party alive
            new DefensiveModule(),       // Priority 20 - Mitigation
            new BuffModule(),            // Priority 30 - Buffs and utilities
            new DamageModule(),          // Priority 50 - DPS when safe
        };

        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        Log.Info("Athena (Scholar) rotation initialized");
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            _statusHelper.HasLucidDreaming(player));
    }

    /// <inheritdoc />
    protected override void UpdateJobSpecificServices(IPlayerCharacter player, bool inCombat)
    {
        // Call base healer service updates
        base.UpdateJobSpecificServices(player, inCombat);

        // Update Scholar-specific debug state
        _debugState.AetherflowStacks = _aetherflowService.CurrentStacks;
        _debugState.FairyGauge = _fairyGaugeService.CurrentGauge;
        _debugState.FairyState = _fairyStateManager.CurrentState.ToString();
        _debugState.PlayerHpPercent = player.MaxHp > 0 ? (float)player.CurrentHp / player.MaxHp : 1f;
    }

    /// <inheritdoc />
    protected override AthenaContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
    {
        return new AthenaContext(
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
            aetherflowService: _aetherflowService,
            fairyGaugeService: _fairyGaugeService,
            fairyStateManager: _fairyStateManager,
            statusHelper: _statusHelper,
            partyHelper: _partyHelper,
            cooldownPlanner: CooldownPlanner,
            healingSpellSelector: HealingSpellSelector,
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
    /// Converts Athena debug state to Apollo debug state for UI compatibility.
    /// </summary>
    private DebugState ConvertToApolloDebugState()
    {
        return new DebugState
        {
            PlanningState = _debugState.PlanningState,
            PlannedAction = _debugState.PlannedAction,
            AoEInjuredCount = _debugState.AoEInjuredCount,
            AoEStatus = _debugState.AoEHealState,
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
            // Scholar-specific mappings
            LilyCount = _debugState.AetherflowStacks, // Map Aetherflow to Lily for display
            BloodLilyCount = _debugState.FairyGauge / 33, // Rough conversion for display
            LilyStrategy = _debugState.AetherflowState,
        };
    }
}
