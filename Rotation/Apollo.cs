using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Rotation.Base;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;

namespace Olympus.Rotation;

/// <summary>
/// White Mage rotation module (RSR-style reactive execution).
/// Orchestrates modular execution: each module handles a specific concern.
/// Named after the Greek god of healing, light, and music.
/// </summary>
public sealed class Apollo : BaseHealerRotation<ApolloContext, IApolloModule>
{
    /// <inheritdoc />
    public override string Name => "Apollo";

    /// <inheritdoc />
    public override uint[] SupportedJobIds => [JobRegistry.WhiteMage, JobRegistry.Conjurer];

    /// <inheritdoc />
    public override DebugState DebugState => _debugState;

    /// <inheritdoc />
    protected override List<IApolloModule> Modules => _modules;

    // Persistent debug state (shared with contexts, exposed for DebugService)
    private readonly DebugState _debugState = new();

    // Helpers (shared across modules)
    private readonly StatusHelper _statusHelper;
    private readonly PartyHelper _partyHelper;

    // Modules (sorted by priority - lower = higher priority)
    private readonly List<IApolloModule> _modules;

    public Apollo(
        IPluginLog log,
        ActionTracker actionTracker,
        CombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
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
        IErrorMetricsService? errorMetrics = null)
        : base(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
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
            errorMetrics)
    {
        // Initialize helpers
        _statusHelper = new StatusHelper();
        _partyHelper = new PartyHelper(objectTable, partyList, hpPredictionService, configuration);

        // Initialize modules (ordered by priority - lower = executed first)
        _modules = new List<IApolloModule>
        {
            new ResurrectionModule(),  // Priority 5 - Dead members are useless
            new HealingModule(),       // Priority 10 - Keep party alive
            new DefensiveModule(),     // Priority 20 - Mitigation
            new BuffModule(),          // Priority 30 - Buffs and utilities
            new DamageModule(),        // Priority 50 - DPS when safe
        };

        // Sort by priority
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    #region Abstract Implementation

    /// <inheritdoc />
    protected override void UpdateMpForecast(IPlayerCharacter player)
    {
        MpForecastService.Update(
            (int)player.CurrentMp,
            (int)player.MaxMp,
            StatusHelper.HasLucidDreaming(player));
    }

    /// <inheritdoc />
    protected override ApolloContext CreateContext(IPlayerCharacter player, bool inCombat, bool isMoving)
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
}
