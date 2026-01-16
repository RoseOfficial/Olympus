using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
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
using Olympus.Timeline;

namespace Olympus.Rotation.AthenaCore.Context;

/// <summary>
/// Shared context for all Athena (Scholar) modules.
/// Contains player state, services, and helper utilities.
/// </summary>
public sealed class AthenaContext : IAthenaContext
{
    // Player state
    public IPlayerCharacter Player { get; }
    public bool InCombat { get; }
    public bool IsMoving { get; }
    public bool CanExecuteGcd { get; }
    public bool CanExecuteOgcd { get; }

    // Services with interfaces
    public IActionService ActionService { get; }
    public ActionTracker ActionTracker { get; }
    public ICombatEventService CombatEventService { get; }
    public IDamageIntakeService DamageIntakeService { get; }
    public IDamageTrendService DamageTrendService { get; }
    public IFrameScopedCache FrameCache { get; }
    public Configuration Configuration { get; }
    public IDebuffDetectionService DebuffDetectionService { get; }
    public IHpPredictionService HpPredictionService { get; }
    public IMpForecastService MpForecastService { get; }
    public IObjectTable ObjectTable { get; }
    public IPartyList PartyList { get; }
    public IPlayerStatsService PlayerStatsService { get; }
    public ITargetingService TargetingService { get; }
    public ITimelineService? TimelineService { get; }

    // Scholar-specific services
    public IAetherflowTrackingService AetherflowService { get; }
    public IFairyGaugeService FairyGaugeService { get; }
    public IFairyStateManager FairyStateManager { get; }

    // Helpers
    public AthenaStatusHelper StatusHelper { get; }
    public AthenaPartyHelper PartyHelper { get; }

    // Cooldown planning
    public ICooldownPlanner CooldownPlanner { get; }

    // Healer rotation context requirements
    public IHealingSpellSelector HealingSpellSelector { get; }
    public IPartyAnalyzer? PartyAnalyzer { get; }

    // Smart healing services
    public ICoHealerDetectionService? CoHealerDetectionService { get; }
    public IBossMechanicDetector? BossMechanicDetector { get; }
    public IShieldTrackingService? ShieldTrackingService { get; }

    // Debug state (mutable, updated by modules)
    public AthenaDebugState Debug { get; }

    // Healing coordination (frame-scoped)
    public HealingCoordinationState HealingCoordination { get; }

    // Optional logging (null in tests)
    public IPluginLog? Log { get; }

    // Cached status checks (computed once per frame, lazy-initialized)
    private int? _aetherflowStacks;
    private int? _fairyGauge;
    private bool? _hasSwiftcast;
    private bool? _hasRecitation;
    private bool? _hasEmergencyTactics;
    private bool? _hasDissipation;
    private bool? _hasSeraphism;
    private bool? _hasImpactImminent;
    private (float avgHpPercent, float lowestHpPercent, int injuredCount)? _partyHealthMetrics;

    public int AetherflowStacks => _aetherflowStacks ??= AetherflowService.CurrentStacks;
    public int FairyGauge => _fairyGauge ??= FairyGaugeService.CurrentGauge;
    public bool HasSwiftcast => _hasSwiftcast ??= StatusHelper.HasSwiftcast(Player);
    public bool HasRecitation => _hasRecitation ??= StatusHelper.HasRecitation(Player);
    public bool HasEmergencyTactics => _hasEmergencyTactics ??= StatusHelper.HasEmergencyTactics(Player);
    public bool HasDissipation => _hasDissipation ??= StatusHelper.HasDissipation(Player);
    public bool HasSeraphism => _hasSeraphism ??= StatusHelper.HasSeraphism(Player);
    public bool HasImpactImminent => _hasImpactImminent ??= StatusHelper.HasImpactImminent(Player);

    /// <summary>
    /// Cached party health metrics (avgHpPercent, lowestHpPercent, injuredCount).
    /// </summary>
    public (float avgHpPercent, float lowestHpPercent, int injuredCount) PartyHealthMetrics
        => _partyHealthMetrics ??= PartyHelper.CalculatePartyHealthMetrics(Player);

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
        ITimelineService? timelineService = null,
        AthenaDebugState? debugState = null,
        IPluginLog? log = null)
    {
        Player = player;
        InCombat = inCombat;
        IsMoving = isMoving;
        CanExecuteGcd = canExecuteGcd;
        CanExecuteOgcd = canExecuteOgcd;
        ActionService = actionService;
        ActionTracker = actionTracker;
        CombatEventService = combatEventService;
        DamageIntakeService = damageIntakeService;
        DamageTrendService = damageTrendService;
        FrameCache = frameCache;
        Configuration = configuration;
        DebuffDetectionService = debuffDetectionService;
        HpPredictionService = hpPredictionService;
        MpForecastService = mpForecastService;
        ObjectTable = objectTable;
        PartyList = partyList;
        PlayerStatsService = playerStatsService;
        TargetingService = targetingService;
        AetherflowService = aetherflowService;
        FairyGaugeService = fairyGaugeService;
        FairyStateManager = fairyStateManager;
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        CooldownPlanner = cooldownPlanner;
        HealingSpellSelector = healingSpellSelector;
        PartyAnalyzer = partyAnalyzer;
        CoHealerDetectionService = coHealerDetectionService;
        BossMechanicDetector = bossMechanicDetector;
        ShieldTrackingService = shieldTrackingService;
        TimelineService = timelineService;
        Debug = debugState ?? new AthenaDebugState();
        HealingCoordination = new HealingCoordinationState();
        Log = log;
    }

    /// <summary>
    /// Logs a healing decision for debugging.
    /// </summary>
    public void LogHealDecision(string targetName, float hpPercent, string spellName, int predictedHeal, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[Athena Heal] {0} at {1:P0} → {2} (est. {3} HP) - {4}",
            targetName, hpPercent, spellName, predictedHeal, reason);
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
/// Interface for Athena context (for testability).
/// Extends the healer rotation context with SCH-specific properties.
/// </summary>
public interface IAthenaContext : IHealerRotationContext
{
    // SCH-specific services
    IAetherflowTrackingService AetherflowService { get; }
    IFairyGaugeService FairyGaugeService { get; }
    IFairyStateManager FairyStateManager { get; }

    // SCH Job Gauge
    int AetherflowStacks { get; }
    int FairyGauge { get; }

    // SCH-specific status checks
    bool HasRecitation { get; }
    bool HasEmergencyTactics { get; }
    bool HasDissipation { get; }
    bool HasSeraphism { get; }
    bool HasImpactImminent { get; }

    // Debug state
    AthenaDebugState Debug { get; }

    // Smart healing services
    ICoHealerDetectionService? CoHealerDetectionService { get; }
    IBossMechanicDetector? BossMechanicDetector { get; }
    IShieldTrackingService? ShieldTrackingService { get; }

    // Helpers
    AthenaStatusHelper StatusHelper { get; }
    AthenaPartyHelper PartyHelper { get; }

    // Healing coordination
    HealingCoordinationState HealingCoordination { get; }

    // Logging helpers
    void LogHealDecision(string targetName, float hpPercent, string spellName, int predictedHeal, string reason);
    void LogAetherflowDecision(string spellName, int stacksRemaining, string reason);
    void LogFairyDecision(string abilityName, FairyState fairyState, string reason);
    void LogShieldDecision(string targetName, string spellName, int shieldAmount, string reason);
}

/// <summary>
/// Mutable debug state for Athena modules.
/// </summary>
public sealed class AthenaDebugState
{
    // General
    public string PlanningState { get; set; } = "Idle";
    public string PlannedAction { get; set; } = "None";

    // Aetherflow
    public int AetherflowStacks { get; set; }
    public string AetherflowState { get; set; } = "Idle";
    public string EnergyDrainState { get; set; } = "Idle";

    // Fairy
    public int FairyGauge { get; set; }
    public string FairyState { get; set; } = "None";
    public string FeyUnionState { get; set; } = "Idle";
    public string SeraphState { get; set; } = "Idle";

    // Healing
    public int LastHealAmount { get; set; }
    public string LastHealStats { get; set; } = "";
    public string SingleHealState { get; set; } = "Idle";
    public string AoEHealState { get; set; } = "Idle";
    public int AoEInjuredCount { get; set; }

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
    public string DpsState { get; set; } = "Idle";
    public string DotState { get; set; } = "Idle";
    public string AoEDpsState { get; set; } = "Idle";
    public int AoEDpsEnemyCount { get; set; }
    public string ChainStratagemState { get; set; } = "Idle";

    // Resurrection
    public string RaiseState { get; set; } = "Idle";
    public string RaiseTarget { get; set; } = "None";

    // Esuna
    public string EsunaState { get; set; } = "Idle";
    public string EsunaTarget { get; set; } = "None";

    // Buffs/Utilities
    public string RecitationState { get; set; } = "Idle";
    public string DissipationState { get; set; } = "Idle";
    public string ExpedientState { get; set; } = "Idle";
    public string LucidState { get; set; } = "Idle";

    // Party
    public float PlayerHpPercent { get; set; }
    public int PartyListCount { get; set; }
    public int PartyValidCount { get; set; }
}
