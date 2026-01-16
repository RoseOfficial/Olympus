using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Astrologian;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Cache;
using Olympus.Services.Targeting;
using Olympus.Timeline;

namespace Olympus.Rotation.AstraeaCore.Context;

/// <summary>
/// Shared context for all Astraea (Astrologian) modules.
/// Contains player state, services, and helper utilities.
/// </summary>
public sealed class AstraeaContext : IAstraeaContext
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

    // Astrologian-specific services
    public ICardTrackingService CardService { get; }
    public IEarthlyStarService EarthlyStarService { get; }

    // Helpers
    public AstraeaStatusHelper StatusHelper { get; }
    public AstraeaPartyHelper PartyHelper { get; }

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
    public AstraeaDebugState Debug { get; }

    // Healing coordination (frame-scoped)
    public HealingCoordinationState HealingCoordination { get; }

    // Optional logging (null in tests)
    public IPluginLog? Log { get; }

    // Cached status checks (computed once per frame, lazy-initialized)
    private bool? _hasSwiftcast;
    private bool? _hasLightspeed;
    private bool? _hasNeutralSect;
    private bool? _hasDivining;
    private bool? _hasHoroscope;
    private bool? _hasHoroscopeHelios;
    private bool? _hasMacrocosmos;
    private bool? _hasSynastry;
    private (float avgHpPercent, float lowestHpPercent, int injuredCount)? _partyHealthMetrics;

    // Card state (cached from service)
    private Data.ASTActions.CardType? _currentCard;
    private Data.ASTActions.CardType? _minorArcana;
    private int? _sealCount;
    private int? _uniqueSealCount;

    public bool HasSwiftcast => _hasSwiftcast ??= StatusHelper.HasSwiftcast(Player);
    public bool HasLightspeed => _hasLightspeed ??= StatusHelper.HasLightspeed(Player);
    public bool HasNeutralSect => _hasNeutralSect ??= StatusHelper.HasNeutralSect(Player);
    public bool HasDivining => _hasDivining ??= StatusHelper.HasDivining(Player);
    public bool HasHoroscope => _hasHoroscope ??= StatusHelper.HasHoroscope(Player);
    public bool HasHoroscopeHelios => _hasHoroscopeHelios ??= StatusHelper.HasHoroscopeHelios(Player);
    public bool HasMacrocosmos => _hasMacrocosmos ??= StatusHelper.HasMacrocosmos(Player);
    public bool HasSynastry => _hasSynastry ??= StatusHelper.HasSynastry(Player);

    // Card state properties (cached per frame)
    public Data.ASTActions.CardType CurrentCard => _currentCard ??= CardService.CurrentCard;
    public Data.ASTActions.CardType MinorArcana => _minorArcana ??= CardService.MinorArcanaCard;
    public bool HasCard => CurrentCard != Data.ASTActions.CardType.None;
    public bool HasMinorArcana => MinorArcana != Data.ASTActions.CardType.None;
    public int SealCount => _sealCount ??= CardService.SealCount;
    public int UniqueSealCount => _uniqueSealCount ??= CardService.UniqueSealCount;
    public bool CanUseAstrodyne => SealCount >= 3;

    // Earthly Star state (delegated to service)
    public bool IsStarPlaced => EarthlyStarService.IsStarPlaced;
    public bool IsStarMature => EarthlyStarService.IsStarMature;
    public float StarTimeRemaining => EarthlyStarService.TimeRemaining;

    /// <summary>
    /// Cached party health metrics (avgHpPercent, lowestHpPercent, injuredCount).
    /// </summary>
    public (float avgHpPercent, float lowestHpPercent, int injuredCount) PartyHealthMetrics
        => _partyHealthMetrics ??= PartyHelper.CalculatePartyHealthMetrics(Player);

    public AstraeaContext(
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
        ICardTrackingService cardService,
        IEarthlyStarService earthlyStarService,
        AstraeaStatusHelper statusHelper,
        AstraeaPartyHelper partyHelper,
        ICooldownPlanner cooldownPlanner,
        IHealingSpellSelector healingSpellSelector,
        ICoHealerDetectionService? coHealerDetectionService = null,
        IPartyAnalyzer? partyAnalyzer = null,
        IBossMechanicDetector? bossMechanicDetector = null,
        IShieldTrackingService? shieldTrackingService = null,
        ITimelineService? timelineService = null,
        AstraeaDebugState? debugState = null,
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
        CardService = cardService;
        EarthlyStarService = earthlyStarService;
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        CooldownPlanner = cooldownPlanner;
        HealingSpellSelector = healingSpellSelector;
        PartyAnalyzer = partyAnalyzer;
        CoHealerDetectionService = coHealerDetectionService;
        BossMechanicDetector = bossMechanicDetector;
        ShieldTrackingService = shieldTrackingService;
        TimelineService = timelineService;
        Debug = debugState ?? new AstraeaDebugState();
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

        Log.Debug("[Astraea Heal] {0} at {1:P0} → {2} (est. {3} HP) - {4}",
            targetName, hpPercent, spellName, predictedHeal, reason);
    }

    /// <summary>
    /// Logs a card decision.
    /// </summary>
    public void LogCardDecision(string cardName, string targetName, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[Astraea Card] {0} → {1} - {2}",
            cardName, targetName, reason);
    }

    /// <summary>
    /// Logs an Earthly Star decision.
    /// </summary>
    public void LogEarthlyStarDecision(string action, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[Astraea Star] {0} - {1}",
            action, reason);
    }

    /// <summary>
    /// Logs a buff decision.
    /// </summary>
    public void LogBuffDecision(string buffName, string targetName, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[Astraea Buff] {0} → {1} - {2}",
            buffName, targetName, reason);
    }
}

/// <summary>
/// Interface for Astraea context (for testability).
/// Extends the healer rotation context with AST-specific properties.
/// </summary>
public interface IAstraeaContext : IHealerRotationContext
{
    // AST-specific services
    ICardTrackingService CardService { get; }
    IEarthlyStarService EarthlyStarService { get; }

    // AST Card State
    Data.ASTActions.CardType CurrentCard { get; }
    Data.ASTActions.CardType MinorArcana { get; }
    bool HasCard { get; }
    bool HasMinorArcana { get; }
    int SealCount { get; }
    int UniqueSealCount { get; }
    bool CanUseAstrodyne { get; }

    // Earthly Star State
    bool IsStarPlaced { get; }
    bool IsStarMature { get; }
    float StarTimeRemaining { get; }

    // AST-specific status checks
    bool HasLightspeed { get; }
    bool HasNeutralSect { get; }
    bool HasDivining { get; }
    bool HasHoroscope { get; }
    bool HasHoroscopeHelios { get; }
    bool HasMacrocosmos { get; }
    bool HasSynastry { get; }

    // Debug state
    AstraeaDebugState Debug { get; }

    // Smart healing services
    ICoHealerDetectionService? CoHealerDetectionService { get; }
    IBossMechanicDetector? BossMechanicDetector { get; }
    IShieldTrackingService? ShieldTrackingService { get; }

    // Helpers
    AstraeaStatusHelper StatusHelper { get; }
    AstraeaPartyHelper PartyHelper { get; }

    // Healing coordination
    HealingCoordinationState HealingCoordination { get; }

    // Logging helpers
    void LogHealDecision(string targetName, float hpPercent, string spellName, int predictedHeal, string reason);
    void LogCardDecision(string cardName, string targetName, string reason);
    void LogEarthlyStarDecision(string action, string reason);
    void LogBuffDecision(string buffName, string targetName, string reason);
}

/// <summary>
/// Mutable debug state for Astraea modules.
/// </summary>
public sealed class AstraeaDebugState
{
    // General
    public string PlanningState { get; set; } = "Idle";
    public string PlannedAction { get; set; } = "None";

    // Cards
    public string CurrentCardType { get; set; } = "None";
    public string MinorArcanaType { get; set; } = "None";
    public int SealCount { get; set; }
    public int UniqueSealCount { get; set; }
    public string CardState { get; set; } = "Idle";
    public string DrawState { get; set; } = "Idle";
    public string PlayState { get; set; } = "Idle";
    public string AstrodyneState { get; set; } = "Idle";
    public string DivinationState { get; set; } = "Idle";
    public string OracleState { get; set; } = "Idle";

    // Earthly Star
    public string EarthlyStarState { get; set; } = "Not Placed";
    public float StarTimeRemaining { get; set; }
    public bool IsStarMature { get; set; }
    public int StarTargetsInRange { get; set; }

    // Healing
    public int LastHealAmount { get; set; }
    public string LastHealStats { get; set; } = "";
    public string SingleHealState { get; set; } = "Idle";
    public string AoEHealState { get; set; } = "Idle";
    public int AoEInjuredCount { get; set; }

    // oGCD Heals
    public string EssentialDignityState { get; set; } = "Idle";
    public string CelestialIntersectionState { get; set; } = "Idle";
    public string CelestialOppositionState { get; set; } = "Idle";
    public string ExaltationState { get; set; } = "Idle";
    public string HoroscopeState { get; set; } = "Idle";
    public string MacrocosmosState { get; set; } = "Idle";
    public string NeutralSectState { get; set; } = "Idle";
    public string SunSignState { get; set; } = "Idle";

    // Synastry
    public string SynastryState { get; set; } = "Idle";
    public string SynastryTarget { get; set; } = "None";

    // Buffs
    public string LightspeedState { get; set; } = "Idle";
    public string CollectiveUnconsciousState { get; set; } = "Idle";

    // DPS
    public string DpsState { get; set; } = "Idle";
    public string DotState { get; set; } = "Idle";
    public string AoEDpsState { get; set; } = "Idle";
    public int AoEDpsEnemyCount { get; set; }

    // Resurrection
    public string RaiseState { get; set; } = "Idle";
    public string RaiseTarget { get; set; } = "None";

    // Esuna
    public string EsunaState { get; set; } = "Idle";
    public string EsunaTarget { get; set; } = "None";

    // Resources
    public string LucidState { get; set; } = "Idle";

    // Party
    public float PlayerHpPercent { get; set; }
    public int PartyListCount { get; set; }
    public int PartyValidCount { get; set; }
}
