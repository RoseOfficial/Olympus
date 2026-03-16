using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.AthenaCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Cache;
using Olympus.Services.Targeting;
using Olympus.Services.Scholar;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.AthenaCore.Context;

/// <summary>
/// Shared context for all Athena (Scholar) modules.
/// Contains player state, services, and helper utilities.
/// </summary>
public sealed class AthenaContext : BaseHealerContext, IAthenaContext
{
    // Scholar-specific services
    public IAetherflowTrackingService AetherflowService { get; }
    public IFairyGaugeService FairyGaugeService { get; }
    public IFairyStateManager FairyStateManager { get; }

    // Helpers
    public AthenaStatusHelper StatusHelper { get; }
    public AthenaPartyHelper PartyHelper { get; }

    // Debug state (mutable, updated by modules)
    public AthenaDebugState Debug { get; }

    // Cached status checks (computed once per frame, lazy-initialized)
    private int? _aetherflowStacks;
    private int? _fairyGauge;
    private bool? _hasRecitation;
    private bool? _hasEmergencyTactics;
    private bool? _hasDissipation;
    private bool? _hasSeraphism;
    private bool? _hasImpactImminent;

    public int AetherflowStacks => _aetherflowStacks ??= AetherflowService.CurrentStacks;
    public int FairyGauge => _fairyGauge ??= FairyGaugeService.CurrentGauge;
    public bool HasRecitation => _hasRecitation ??= StatusHelper.HasRecitation(Player);
    public bool HasEmergencyTactics => _hasEmergencyTactics ??= StatusHelper.HasEmergencyTactics(Player);
    public bool HasDissipation => _hasDissipation ??= StatusHelper.HasDissipation(Player);
    public bool HasSeraphism => _hasSeraphism ??= StatusHelper.HasSeraphism(Player);
    public bool HasImpactImminent => _hasImpactImminent ??= StatusHelper.HasImpactImminent(Player);

    protected override bool CheckHasSwiftcast() => AthenaStatusHelper.HasSwiftcast(Player);
    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics()
        => PartyHelper.CalculatePartyHealthMetrics(Player);
    protected override string GetJobName() => "Athena";

    public AthenaContext(
        IPlayerCharacter player,
        bool inCombat,
        bool isMoving,
        bool canExecuteGcd,
        bool canExecuteOgcd,
        IActionService actionService,
        ActionTracker actionTracker,
        ICombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
        IDamageTrendService damageTrendService,
        IFrameScopedCache frameCache,
        Configuration configuration,
        IDebuffDetectionService debuffDetectionService,
        IHpPredictionService hpPredictionService,
        IMpForecastService mpForecastService,
        IObjectTable objectTable,
        IPartyList partyList,
        IPlayerStatsService playerStatsService,
        ITargetingService targetingService,
        IAetherflowTrackingService aetherflowService,
        IFairyGaugeService fairyGaugeService,
        IFairyStateManager fairyStateManager,
        AthenaStatusHelper statusHelper,
        AthenaPartyHelper partyHelper,
        ICooldownPlanner cooldownPlanner,
        IHealingSpellSelector healingSpellSelector,
        ICoHealerDetectionService? coHealerDetectionService = null,
        IPartyAnalyzer? partyAnalyzer = null,
        IBossMechanicDetector? bossMechanicDetector = null,
        IShieldTrackingService? shieldTrackingService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITimelineService? timelineService = null,
        ITrainingService? trainingService = null,
        AthenaDebugState? debugState = null,
        IPluginLog? log = null)
        : base(player, inCombat, isMoving, canExecuteGcd, canExecuteOgcd,
               actionService, actionTracker, combatEventService, damageIntakeService, damageTrendService,
               frameCache, configuration, debuffDetectionService, hpPredictionService, mpForecastService,
               objectTable, partyList, playerStatsService, targetingService,
               healingSpellSelector, cooldownPlanner,
               coHealerDetectionService, bossMechanicDetector, shieldTrackingService,
               partyAnalyzer,
               partyCoordinationService, timelineService, trainingService, log)
    {
        AetherflowService = aetherflowService;
        FairyGaugeService = fairyGaugeService;
        FairyStateManager = fairyStateManager;
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState ?? new AthenaDebugState();
    }

    /// <summary>
    /// Logs an Aetherflow usage decision.
    /// </summary>
    public void LogAetherflowDecision(string spellName, int stacksRemaining, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[Athena Aetherflow] {0} (stacks: {1}) - {2}",
            spellName, stacksRemaining, reason);
    }

    /// <summary>
    /// Logs a fairy ability decision.
    /// </summary>
    public void LogFairyDecision(string abilityName, FairyState fairyState, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[Athena Fairy] {0} (state: {1}) - {2}",
            abilityName, fairyState, reason);
    }

    /// <summary>
    /// Logs a shield decision.
    /// </summary>
    public void LogShieldDecision(string targetName, string spellName, int shieldAmount, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[Athena Shield] {0} → {1} (shield: {2}) - {3}",
            targetName, spellName, shieldAmount, reason);
    }
}

/// <summary>
/// Mutable debug state for Athena modules.
/// </summary>
public sealed class AthenaDebugState : DebugState
{
    // Aetherflow
    public int AetherflowStacks { get; set; }
    public string AetherflowState { get; set; } = "Idle";
    public string EnergyDrainState { get; set; } = "Idle";

    // Fairy
    public int FairyGauge { get; set; }
    public string FairyState { get; set; } = "None";
    public string FeyUnionState { get; set; } = "Idle";
    public string SeraphState { get; set; } = "Idle";

    // Shields
    public string ShieldState { get; set; } = "Idle";
    public string DeploymentState { get; set; } = "Idle";
    public string EmergencyTacticsState { get; set; } = "Idle";

    // oGCD Heals
    public string LustrateState { get; set; } = "Idle";
    public string IndomitabilityState { get; set; } = "Idle";
    public string ExcogitationState { get; set; } = "Idle";
    public string SacredSoilState { get; set; } = "Idle";

    // DPS
    public string ChainStratagemState { get; set; } = "Idle";

    // Buffs/Utilities
    public string RecitationState { get; set; } = "Idle";
    public string DissipationState { get; set; } = "Idle";
    public string ExpedientState { get; set; } = "Idle";
}
