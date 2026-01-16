using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Sage;
using Olympus.Services.Stats;
using Olympus.Services.Cache;
using Olympus.Services.Targeting;
using Olympus.Timeline;

namespace Olympus.Rotation.AsclepiusCore.Context;

/// <summary>
/// Shared context for all Asclepius (Sage) modules.
/// Contains player state, services, and helper utilities.
/// Implements IAsclepiusContext for testability.
/// </summary>
public sealed class AsclepiusContext : IAsclepiusContext
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
    public IHealingSpellSelector HealingSpellSelector { get; }
    public IHpPredictionService HpPredictionService { get; }
    public IMpForecastService MpForecastService { get; }
    public IObjectTable ObjectTable { get; }
    public IPartyList PartyList { get; }
    public IPlayerStatsService PlayerStatsService { get; }
    public ITargetingService TargetingService { get; }
    public ITimelineService? TimelineService { get; }

    #endregion

    #region SGE-Specific Services

    public IAddersgallTrackingService AddersgallService { get; }
    public IAdderstingTrackingService AdderstingService { get; }
    public IKardiaManager KardiaManager { get; }
    public IEukrasiaStateService EukrasiaService { get; }

    #endregion

    #region Helpers

    public AsclepiusStatusHelper StatusHelper { get; }
    public IPartyHelper PartyHelper { get; }

    #endregion

    #region Party Analyzer

    public IPartyAnalyzer? PartyAnalyzer { get; }

    #endregion

    #region Cooldown Planning

    public ICooldownPlanner CooldownPlanner { get; }

    #endregion

    #region Smart Healing Services

    public ICoHealerDetectionService? CoHealerDetectionService { get; }
    public IBossMechanicDetector? BossMechanicDetector { get; }
    public IShieldTrackingService? ShieldTrackingService { get; }

    #endregion

    #region Debug and Coordination State

    public AsclepiusDebugState Debug { get; }
    public HealingCoordinationState HealingCoordination { get; }

    #endregion

    #region Optional Services

    public IPluginLog? Log { get; }

    #endregion

    #region Cached Status Checks

    private bool? _hasSwiftcast;
    private bool? _hasEukrasia;
    private bool? _hasZoe;
    private bool? _hasSoteria;
    private bool? _hasPhilosophia;
    private int? _addersgallStacks;
    private float? _addersgallTimer;
    private int? _adderstingStacks;
    private (float avgHpPercent, float lowestHpPercent, int injuredCount)? _partyHealthMetrics;

    public bool HasSwiftcast => _hasSwiftcast ??= AsclepiusStatusHelper.HasSwiftcast(Player);
    public bool HasEukrasia => _hasEukrasia ??= AsclepiusStatusHelper.HasEukrasia(Player);
    public bool HasZoe => _hasZoe ??= AsclepiusStatusHelper.HasZoe(Player);
    public bool HasSoteria => _hasSoteria ??= AsclepiusStatusHelper.HasSoteria(Player);
    public bool HasPhilosophia => _hasPhilosophia ??= AsclepiusStatusHelper.HasPhilosophia(Player);

    public int AddersgallStacks => _addersgallStacks ??= AddersgallService.CurrentStacks;
    public float AddersgallTimer => _addersgallTimer ??= AddersgallService.TimerRemaining;
    public int AdderstingStacks => _adderstingStacks ??= AdderstingService.CurrentStacks;

    public bool HasKardiaPlaced => KardiaManager.HasKardia;
    public ulong KardiaTargetId => KardiaManager.CurrentKardiaTarget;
    public bool CanSwapKardia => KardiaManager.CanSwapKardia;

    public (float avgHpPercent, float lowestHpPercent, int injuredCount) PartyHealthMetrics
        => _partyHealthMetrics ??= PartyHelper.CalculatePartyHealthMetrics(Player);

    #endregion

    public AsclepiusContext(
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
        IAddersgallTrackingService addersgallService,
        IAdderstingTrackingService adderstingService,
        IKardiaManager kardiaManager,
        IEukrasiaStateService eukrasiaService,
        AsclepiusStatusHelper statusHelper,
        IPartyHelper partyHelper,
        ICooldownPlanner cooldownPlanner,
        ICoHealerDetectionService? coHealerDetectionService = null,
        IBossMechanicDetector? bossMechanicDetector = null,
        IShieldTrackingService? shieldTrackingService = null,
        ITimelineService? timelineService = null,
        AsclepiusDebugState? debugState = null,
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
        AddersgallService = addersgallService;
        AdderstingService = adderstingService;
        KardiaManager = kardiaManager;
        EukrasiaService = eukrasiaService;
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        CooldownPlanner = cooldownPlanner;
        CoHealerDetectionService = coHealerDetectionService;
        BossMechanicDetector = bossMechanicDetector;
        ShieldTrackingService = shieldTrackingService;
        TimelineService = timelineService;
        Debug = debugState ?? new AsclepiusDebugState();
        HealingCoordination = new HealingCoordinationState();
        Log = log;
    }

    #region Logging Helpers

    /// <summary>
    /// Logs a healing decision for debugging.
    /// </summary>
    public void LogHealDecision(string targetName, float hpPercent, string spellName, int predictedHeal, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[SGE Heal] {0} at {1:P0} → {2} (est. {3} HP) - {4}",
            targetName, hpPercent, spellName, predictedHeal, reason);
    }

    /// <summary>
    /// Logs an oGCD healing decision.
    /// </summary>
    public void LogOgcdDecision(string targetName, float hpPercent, string spellName, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[SGE oGCD] {0} at {1:P0} → {2} - {3}",
            targetName, hpPercent, spellName, reason);
    }

    /// <summary>
    /// Logs a defensive cooldown decision.
    /// </summary>
    public void LogDefensiveDecision(string targetName, float hpPercent, string spellName, float damageRate, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[SGE Defensive] {0} at {1:P0} (dmg rate: {2:F0} DPS) → {3} - {4}",
            targetName, hpPercent, damageRate, spellName, reason);
    }

    /// <summary>
    /// Logs an Addersgall spending decision.
    /// </summary>
    public void LogAddersgallDecision(string spellName, int stacksBefore, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[SGE Addersgall] {0} (stacks: {1} → {2}) - {3}",
            spellName, stacksBefore, stacksBefore - 1, reason);
    }

    /// <summary>
    /// Logs a Kardia-related decision.
    /// </summary>
    public void LogKardiaDecision(string targetName, string action, string reason)
    {
        if (Log is null || !Configuration.Debug.EnableVerboseLogging)
            return;

        Log.Debug("[SGE Kardia] {0} on {1} - {2}",
            action, targetName, reason);
    }

    #endregion
}

/// <summary>
/// Mutable debug state updated by Asclepius modules.
/// Centralized location for all debug information.
/// </summary>
public sealed class AsclepiusDebugState
{
    #region General

    public string PlanningState { get; set; } = "Idle";
    public string PlannedAction { get; set; } = "None";

    #endregion

    #region Resources

    public int AddersgallStacks { get; set; }
    public float AddersgallTimer { get; set; }
    public int AdderstingStacks { get; set; }
    public string AddersgallStrategy { get; set; } = "Balanced";

    #endregion

    #region Kardia

    public string KardiaState { get; set; } = "Idle";
    public string KardiaTarget { get; set; } = "None";
    public string SoteriaState { get; set; } = "Idle";
    public int SoteriaStacks { get; set; }
    public string PhilosophiaState { get; set; } = "Idle";

    #endregion

    #region Eukrasia

    public bool EukrasiaActive { get; set; }
    public bool ZoeActive { get; set; }
    public string EukrasiaState { get; set; } = "Idle";

    #endregion

    #region AoE Healing

    public int AoEInjuredCount { get; set; }
    public uint AoESelectedSpell { get; set; }
    public string AoEStatus { get; set; } = "Idle";

    #endregion

    #region Party

    public float PlayerHpPercent { get; set; }
    public int PartyListCount { get; set; }
    public int PartyValidCount { get; set; }
    public int BattleNpcCount { get; set; }
    public string NpcInfo { get; set; } = "";

    #endregion

    #region DPS

    public string DpsState { get; set; } = "Idle";
    public string TargetInfo { get; set; } = "None";
    public string DoTState { get; set; } = "Idle";
    public float DoTRemaining { get; set; }
    public string AoEDpsState { get; set; } = "Idle";
    public int AoEDpsEnemyCount { get; set; }
    public string PhlegmaState { get; set; } = "Idle";
    public int PhlegmaCharges { get; set; }
    public string ToxikonState { get; set; } = "Idle";
    public string PsycheState { get; set; } = "Idle";

    #endregion

    #region Healing

    public int LastHealAmount { get; set; }
    public string LastHealStats { get; set; } = "";
    public string DruocholeState { get; set; } = "Idle";
    public string TaurocholeState { get; set; } = "Idle";
    public string IxocholeState { get; set; } = "Idle";
    public string KeracholeState { get; set; } = "Idle";
    public string PneumaState { get; set; } = "Idle";

    #endregion

    #region Shields

    public string HaimaState { get; set; } = "Idle";
    public string HaimaTarget { get; set; } = "None";
    public string PanhaimaState { get; set; } = "Idle";
    public string EukrasianDiagnosisState { get; set; } = "Idle";
    public string EukrasianPrognosisState { get; set; } = "Idle";

    #endregion

    #region Buffs

    public string PhysisIIState { get; set; } = "Idle";
    public string HolosState { get; set; } = "Idle";
    public string KrasisState { get; set; } = "Idle";
    public string ZoeState { get; set; } = "Idle";
    public string RhizomataState { get; set; } = "Idle";
    public string PepsisState { get; set; } = "Idle";

    #endregion

    #region Resurrection

    public string RaiseState { get; set; } = "Idle";
    public string RaiseTarget { get; set; } = "None";

    #endregion

    #region Role Actions

    public string LucidState { get; set; } = "Idle";
    public string SurecastState { get; set; } = "Idle";

    #endregion

    #region Esuna

    public string EsunaState { get; set; } = "Idle";
    public string EsunaTarget { get; set; } = "None";

    #endregion
}
