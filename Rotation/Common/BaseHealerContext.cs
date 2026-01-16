using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Timeline;

namespace Olympus.Rotation.Common;

/// <summary>
/// Base context class for healer rotations.
/// Contains all services and state common to all healer jobs.
/// Job-specific contexts should extend this class to add their unique properties.
/// </summary>
public abstract class BaseHealerContext : IHealerRotationContext
{
    #region Player State

    public IPlayerCharacter Player { get; }
    public bool InCombat { get; }
    public bool IsMoving { get; }
    public bool CanExecuteGcd { get; }
    public bool CanExecuteOgcd { get; }

    #endregion

    #region Core Services

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
    public IPlayerStatsService PlayerStatsService { get; }
    public ITargetingService TargetingService { get; }
    public ITimelineService? TimelineService { get; }

    #endregion

    #region Healer Services

    public IHealingSpellSelector HealingSpellSelector { get; }
    public IPartyAnalyzer? PartyAnalyzer { get; }
    public ICooldownPlanner CooldownPlanner { get; }

    #endregion

    #region Smart Healing Services

    public ICoHealerDetectionService? CoHealerDetectionService { get; }
    public IBossMechanicDetector? BossMechanicDetector { get; }
    public IShieldTrackingService? ShieldTrackingService { get; }

    #endregion

    #region Dalamud Services

    public IObjectTable ObjectTable { get; }
    public IPartyList PartyList { get; }
    public IPluginLog? Log { get; }

    #endregion

    #region Coordination

    public HealingCoordinationState HealingCoordination { get; }

    #endregion

    #region Cached Status Checks

    private bool? _hasSwiftcast;
    private (float avgHpPercent, float lowestHpPercent, int injuredCount)? _partyHealthMetrics;

    public bool HasSwiftcast => _hasSwiftcast ??= CheckHasSwiftcast();
    public (float avgHpPercent, float lowestHpPercent, int injuredCount) PartyHealthMetrics
        => _partyHealthMetrics ??= CalculatePartyHealthMetrics();

    /// <summary>
    /// Override to implement job-specific Swiftcast check.
    /// </summary>
    protected abstract bool CheckHasSwiftcast();

    /// <summary>
    /// Override to implement job-specific party health calculation.
    /// </summary>
    protected abstract (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics();

    #endregion

    /// <summary>
    /// Creates a new healer context with all shared services.
    /// </summary>
    protected BaseHealerContext(
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
        IHealingSpellSelector healingSpellSelector,
        ICooldownPlanner cooldownPlanner,
        ICoHealerDetectionService? coHealerDetectionService = null,
        IBossMechanicDetector? bossMechanicDetector = null,
        IShieldTrackingService? shieldTrackingService = null,
        IPartyAnalyzer? partyAnalyzer = null,
        ITimelineService? timelineService = null,
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
        HealingSpellSelector = healingSpellSelector;
        CooldownPlanner = cooldownPlanner;
        CoHealerDetectionService = coHealerDetectionService;
        BossMechanicDetector = bossMechanicDetector;
        ShieldTrackingService = shieldTrackingService;
        PartyAnalyzer = partyAnalyzer;
        TimelineService = timelineService;
        Log = log;
        HealingCoordination = new HealingCoordinationState();
    }

    #region Logging Helpers

    /// <summary>
    /// Logs a healing decision for debugging.
    /// Only logs if debug logging is enabled in configuration.
    /// </summary>
    public void LogHealDecision(string targetName, float hpPercent, string spellName, int predictedHeal, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[{0} Heal] {1} at {2:P0} → {3} (est. {4} HP) - {5}",
            GetJobName(), targetName, hpPercent, spellName, predictedHeal, reason);
    }

    /// <summary>
    /// Logs an oGCD decision.
    /// </summary>
    public void LogOgcdDecision(string targetName, float hpPercent, string spellName, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[{0} oGCD] {1} at {2:P0} → {3} - {4}",
            GetJobName(), targetName, hpPercent, spellName, reason);
    }

    /// <summary>
    /// Logs a defensive cooldown decision.
    /// </summary>
    public void LogDefensiveDecision(string targetName, float hpPercent, string spellName, float damageRate, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[{0} Defensive] {1} at {2:P0} (dmg rate: {3:F0} DPS) → {4} - {5}",
            GetJobName(), targetName, hpPercent, damageRate, spellName, reason);
    }

    /// <summary>
    /// Returns the job name for logging (e.g., "Apollo", "Athena").
    /// </summary>
    protected abstract string GetJobName();

    #endregion
}
