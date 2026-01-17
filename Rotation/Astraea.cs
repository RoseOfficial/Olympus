using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.AstraeaCore.Context;
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

namespace Olympus.Rotation;

/// <summary>
/// Astrologian rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after Astraea, the Greek goddess of stars and justice.
/// </summary>
[Rotation("Astraea", JobRegistry.Astrologian, Role = RotationRole.Healer)]
public sealed class Astraea : BaseHealerRotation<AstraeaContext, IAstraeaModule>
{
    /// <inheritdoc />
    public override string Name => "Astraea";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.Astrologian];

    /// <inheritdoc />
    public override DebugState DebugState => ConvertToApolloDebugState();

    /// <inheritdoc />
    protected override List<IAstraeaModule> Modules => _modules;

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

    // Modules (sorted by priority)
    private readonly List<IAstraeaModule> _modules;

    public Astraea(
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
        IJobGauges jobGauges,
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
        // Initialize Astrologian-specific services
        _cardService = new CardTrackingService(jobGauges);
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

        Log.Info("Astraea (Astrologian) rotation initialized");
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
    }

    /// <inheritdoc />
    protected override AstraeaContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
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
    /// Converts Astraea debug state to Apollo debug state for UI compatibility.
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
            // AST-specific mappings for display compatibility
            LilyCount = _debugState.SealCount, // Map seals to Lily count for display
            BloodLilyCount = _debugState.UniqueSealCount, // Map unique seals
            LilyStrategy = _debugState.CardState,
        };
    }
}
