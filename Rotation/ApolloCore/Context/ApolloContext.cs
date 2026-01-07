using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.ApolloCore.Helpers;
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

namespace Olympus.Rotation.ApolloCore.Context;

/// <summary>
/// Shared context for all Apollo modules.
/// Contains player state, services, and helper utilities.
/// Implements IApolloContext for testability.
/// </summary>
public sealed class ApolloContext : IApolloContext
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
    public IHealingSpellSelector HealingSpellSelector { get; }
    public IHpPredictionService HpPredictionService { get; }
    public IMpForecastService MpForecastService { get; }
    public IObjectTable ObjectTable { get; }
    public IPartyList PartyList { get; }
    public IPlayerStatsService PlayerStatsService { get; }
    public ITargetingService TargetingService { get; }

    // Helpers
    public StatusHelper StatusHelper { get; }
    public IPartyHelper PartyHelper { get; }

    // Party analyzer (null until implemented, uses PartyHelper for now)
    public IPartyAnalyzer? PartyAnalyzer { get; }

    // Cooldown planning
    public ICooldownPlanner CooldownPlanner { get; }

    // Smart healing services
    public ICoHealerDetectionService? CoHealerDetectionService { get; }
    public IBossMechanicDetector? BossMechanicDetector { get; }
    public IShieldTrackingService? ShieldTrackingService { get; }

    // Debug state (mutable, updated by modules)
    public DebugState Debug { get; }

    // Healing coordination (frame-scoped, cleared each frame by HealingModule)
    public HealingCoordinationState HealingCoordination { get; }

    // Optional logging (null in tests)
    public IPluginLog? Log { get; }

    // Cached status checks (computed once per frame, lazy-initialized)
    private bool? _hasThinAir;
    private bool? _hasFreecure;
    private bool? _hasSwiftcast;
    private int? _lilyCount;
    private int? _bloodLilyCount;
    private int? _sacredSightStacks;
    private (float avgHpPercent, float lowestHpPercent, int injuredCount)? _partyHealthMetrics;

    public bool HasThinAir => _hasThinAir ??= StatusHelper.HasThinAir(Player);
    public bool HasFreecure => _hasFreecure ??= StatusHelper.HasFreecure(Player);
    public bool HasSwiftcast => _hasSwiftcast ??= StatusHelper.HasSwiftcast(Player);
    public int LilyCount => _lilyCount ??= StatusHelper.GetLilyCount();
    public int BloodLilyCount => _bloodLilyCount ??= StatusHelper.GetBloodLilyCount();
    public int SacredSightStacks => _sacredSightStacks ??= StatusHelper.GetSacredSightStacks(Player);

    /// <summary>
    /// Cached party health metrics (avgHpPercent, lowestHpPercent, injuredCount).
    /// Computed once per frame to avoid redundant calculations.
    /// </summary>
    public (float avgHpPercent, float lowestHpPercent, int injuredCount) PartyHealthMetrics
        => _partyHealthMetrics ??= PartyHelper.CalculatePartyHealthMetrics(Player);

    public ApolloContext(
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
        IHealingSpellSelector healingSpellSelector,
        IHpPredictionService hpPredictionService,
        IMpForecastService mpForecastService,
        IObjectTable objectTable,
        IPartyList partyList,
        IPlayerStatsService playerStatsService,
        ITargetingService targetingService,
        StatusHelper statusHelper,
        IPartyHelper partyHelper,
        ICooldownPlanner cooldownPlanner,
        ICoHealerDetectionService? coHealerDetectionService = null,
        IBossMechanicDetector? bossMechanicDetector = null,
        IShieldTrackingService? shieldTrackingService = null,
        DebugState? debugState = null,
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
        HealingSpellSelector = healingSpellSelector;
        HpPredictionService = hpPredictionService;
        MpForecastService = mpForecastService;
        ObjectTable = objectTable;
        PartyList = partyList;
        PlayerStatsService = playerStatsService;
        TargetingService = targetingService;
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        CooldownPlanner = cooldownPlanner;
        CoHealerDetectionService = coHealerDetectionService;
        BossMechanicDetector = bossMechanicDetector;
        ShieldTrackingService = shieldTrackingService;
        Debug = debugState ?? new DebugState();
        HealingCoordination = new HealingCoordinationState();
        Log = log;
    }

    /// <summary>
    /// Logs a healing decision for debugging.
    /// Only logs if debug logging is enabled in configuration.
    /// </summary>
    /// <param name="targetName">Name of the heal target.</param>
    /// <param name="hpPercent">Target's HP percentage.</param>
    /// <param name="spellName">Name of the spell chosen.</param>
    /// <param name="predictedHeal">Expected heal amount.</param>
    /// <param name="reason">Why this spell was chosen.</param>
    public void LogHealDecision(string targetName, float hpPercent, string spellName, int predictedHeal, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[Heal] {0} at {1:P0} → {2} (est. {3} HP) - {4}",
            targetName, hpPercent, spellName, predictedHeal, reason);
    }

    /// <summary>
    /// Logs an oGCD healing decision.
    /// </summary>
    public void LogOgcdDecision(string targetName, float hpPercent, string spellName, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[oGCD] {0} at {1:P0} → {2} - {3}",
            targetName, hpPercent, spellName, reason);
    }

    /// <summary>
    /// Logs a defensive cooldown decision.
    /// </summary>
    public void LogDefensiveDecision(string targetName, float hpPercent, string spellName, float damageRate, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[Defensive] {0} at {1:P0} (dmg rate: {2:F0} DPS) → {3} - {4}",
            targetName, hpPercent, damageRate, spellName, reason);
    }
}

/// <summary>
/// Mutable debug state updated by modules.
/// Centralized location for all debug information.
/// </summary>
public sealed class DebugState
{
    // General
    public string PlanningState { get; set; } = "Idle";
    public string PlannedAction { get; set; } = "None";

    // AoE Healing
    public int AoEInjuredCount { get; set; }
    public uint AoESelectedSpell { get; set; }
    public string AoEStatus { get; set; } = "Idle";

    // Party
    public float PlayerHpPercent { get; set; }
    public int PartyListCount { get; set; }
    public int PartyValidCount { get; set; }
    public int BattleNpcCount { get; set; }
    public string NpcInfo { get; set; } = "";

    // DPS
    public string DpsState { get; set; } = "Idle";
    public string TargetInfo { get; set; } = "None";
    public string AoEDpsState { get; set; } = "Idle";
    public int AoEDpsEnemyCount { get; set; }

    // Healing
    public int LastHealAmount { get; set; }
    public string LastHealStats { get; set; } = "";

    // Resurrection
    public string RaiseState { get; set; } = "Idle";
    public string RaiseTarget { get; set; } = "None";

    // Buffs
    public string AsylumState { get; set; } = "Idle";
    public string AsylumTarget { get; set; } = "None";
    public string ThinAirState { get; set; } = "Idle";
    public string SurecastState { get; set; } = "Idle";
    public string PoMState { get; set; } = "Idle";
    public string LucidState { get; set; } = "Idle";
    public string AssizeState { get; set; } = "Idle";

    // Defensive
    public string DefensiveState { get; set; } = "Idle";
    public string TemperanceState { get; set; } = "Idle";

    // Resources
    public int LilyCount { get; set; }
    public int BloodLilyCount { get; set; }
    public string LilyStrategy { get; set; } = "Balanced";
    public int SacredSightStacks { get; set; }
    public string MiseryState { get; set; } = "Idle";

    // Esuna
    public string EsunaState { get; set; } = "Idle";
    public string EsunaTarget { get; set; } = "None";
}
