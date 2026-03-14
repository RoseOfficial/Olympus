using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
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
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.AstraeaCore.Context;

/// <summary>
/// Shared context for all Astraea (Astrologian) modules.
/// Contains player state, services, and helper utilities.
/// </summary>
public sealed class AstraeaContext : BaseHealerContext, IAstraeaContext
{
    // Astrologian-specific services
    public ICardTrackingService CardService { get; }
    public IEarthlyStarService EarthlyStarService { get; }

    // Helpers
    public AstraeaStatusHelper StatusHelper { get; }
    public AstraeaPartyHelper PartyHelper { get; }

    // Debug state (mutable, updated by modules)
    public AstraeaDebugState Debug { get; }

    // Cached status checks (computed once per frame, lazy-initialized)
    private bool? _hasLightspeed;
    private bool? _hasNeutralSect;
    private bool? _hasDivining;
    private bool? _hasHoroscope;
    private bool? _hasHoroscopeHelios;
    private bool? _hasMacrocosmos;
    private bool? _hasSynastry;

    // Card state (cached from service)
    // In Dawntrail, AST draws 4 cards at once
    private Data.ASTActions.CardType? _currentCard;
    private Data.ASTActions.CardType? _minorArcana;
    private bool? _hasCard;
    private bool? _hasBalance;
    private bool? _hasSpear;
    private int? _sealCount;
    private int? _uniqueSealCount;
    private int? _balanceCount;
    private int? _spearCount;
    private int? _totalCardsInHand;

    public bool HasLightspeed => _hasLightspeed ??= StatusHelper.HasLightspeed(Player);
    public bool HasNeutralSect => _hasNeutralSect ??= StatusHelper.HasNeutralSect(Player);
    public bool HasDivining => _hasDivining ??= StatusHelper.HasDivining(Player);
    public bool HasHoroscope => _hasHoroscope ??= StatusHelper.HasHoroscope(Player);
    public bool HasHoroscopeHelios => _hasHoroscopeHelios ??= StatusHelper.HasHoroscopeHelios(Player);
    public bool HasMacrocosmos => _hasMacrocosmos ??= StatusHelper.HasMacrocosmos(Player);
    public bool HasSynastry => _hasSynastry ??= StatusHelper.HasSynastry(Player);

    // Card state properties (cached per frame)
    // In Dawntrail, AST draws 4 cards at once and plays them individually
    public Data.ASTActions.CardType CurrentCard => _currentCard ??= CardService.CurrentCard;
    public Data.ASTActions.CardType MinorArcana => _minorArcana ??= CardService.MinorArcanaCard;
    public bool HasCard => _hasCard ??= CardService.HasCard;
    public bool HasBalance => _hasBalance ??= CardService.HasBalance;
    public bool HasSpear => _hasSpear ??= CardService.HasSpear;
    public bool HasMinorArcana => MinorArcana != Data.ASTActions.CardType.None;
    public int SealCount => _sealCount ??= CardService.SealCount;
    public int UniqueSealCount => _uniqueSealCount ??= CardService.UniqueSealCount;
    public int BalanceCount => _balanceCount ??= CardService.BalanceCount;
    public int SpearCount => _spearCount ??= CardService.SpearCount;
    public int TotalCardsInHand => _totalCardsInHand ??= CardService.TotalCardsInHand;
    public bool CanUseAstrodyne => SealCount >= 3;

    // Earthly Star state (delegated to service)
    public bool IsStarPlaced => EarthlyStarService.IsStarPlaced;
    public bool IsStarMature => EarthlyStarService.IsStarMature;
    public float StarTimeRemaining => EarthlyStarService.TimeRemaining;

    protected override bool CheckHasSwiftcast() => AstraeaStatusHelper.HasSwiftcast(Player);
    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics()
        => PartyHelper.CalculatePartyHealthMetrics(Player);
    protected override string GetJobName() => "Astraea";

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
        IBossMechanicDetector? bossMechanicDetector = null,
        IShieldTrackingService? shieldTrackingService = null,
        IPartyAnalyzer? partyAnalyzer = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITimelineService? timelineService = null,
        ITrainingService? trainingService = null,
        AstraeaDebugState? debugState = null,
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
        CardService = cardService;
        EarthlyStarService = earthlyStarService;
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState ?? new AstraeaDebugState();
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
