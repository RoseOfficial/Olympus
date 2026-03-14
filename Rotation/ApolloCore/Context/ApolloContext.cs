using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
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
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.ApolloCore.Context;

/// <summary>
/// Context for Apollo (White Mage / Conjurer) rotation modules.
/// Extends BaseHealerContext with WHM-specific services and cached state.
/// </summary>
public sealed class ApolloContext : BaseHealerContext, IApolloContext
{
    // WHM-specific helpers
    public StatusHelper StatusHelper { get; }
    public IPartyHelper PartyHelper { get; }

    // Debug state (WHM-specific fields — see DebugState class below)
    public DebugState Debug { get; }

    #region WHM Cached Status Checks

    private bool? _hasThinAir;
    private bool? _hasFreecure;
    private int? _lilyCount;
    private int? _bloodLilyCount;
    private int? _sacredSightStacks;

    public bool HasThinAir => _hasThinAir ??= StatusHelper.HasThinAir(Player);
    public bool HasFreecure => _hasFreecure ??= StatusHelper.HasFreecure(Player);
    public int LilyCount => _lilyCount ??= StatusHelper.GetLilyCount();
    public int BloodLilyCount => _bloodLilyCount ??= StatusHelper.GetBloodLilyCount();
    public int SacredSightStacks => _sacredSightStacks ??= StatusHelper.GetSacredSightStacks(Player);

    #endregion

    protected override bool CheckHasSwiftcast() => BaseStatusHelper.HasSwiftcast(Player);
    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics()
        => PartyHelper.CalculatePartyHealthMetrics(Player);
    protected override string GetJobName() => "Apollo";

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
        IHpPredictionService hpPredictionService,
        IMpForecastService mpForecastService,
        IObjectTable objectTable,
        IPartyList partyList,
        IPlayerStatsService playerStatsService,
        ITargetingService targetingService,
        IHealingSpellSelector healingSpellSelector,
        ICooldownPlanner cooldownPlanner,
        StatusHelper statusHelper,
        IPartyHelper partyHelper,
        ICoHealerDetectionService? coHealerDetectionService = null,
        IBossMechanicDetector? bossMechanicDetector = null,
        IShieldTrackingService? shieldTrackingService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITimelineService? timelineService = null,
        ITrainingService? trainingService = null,
        DebugState? debugState = null,
        IPluginLog? log = null)
        : base(player, inCombat, isMoving, canExecuteGcd, canExecuteOgcd,
               actionService, actionTracker, combatEventService, damageIntakeService, damageTrendService,
               frameCache, configuration, debuffDetectionService, hpPredictionService, mpForecastService,
               objectTable, partyList, playerStatsService, targetingService,
               healingSpellSelector, cooldownPlanner,
               coHealerDetectionService, bossMechanicDetector, shieldTrackingService,
               partyAnalyzer: null,
               partyCoordinationService, timelineService, trainingService, log)
    {
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState ?? new DebugState();
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
