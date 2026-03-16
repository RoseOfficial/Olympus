using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Sage;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.AsclepiusCore.Context;

/// <summary>
/// Shared context for all Asclepius (Sage) modules.
/// Contains player state, services, and helper utilities.
/// Implements IAsclepiusContext for testability.
/// </summary>
public sealed class AsclepiusContext : BaseHealerContext, IAsclepiusContext
{
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

    #region Debug State

    public AsclepiusDebugState Debug { get; }

    #endregion

    #region SGE Cached Status Checks

    private bool? _hasEukrasia;
    private bool? _hasZoe;
    private bool? _hasSoteria;
    private bool? _hasPhilosophia;
    private int? _addersgallStacks;
    private float? _addersgallTimer;
    private int? _adderstingStacks;

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

    #endregion

    protected override bool CheckHasSwiftcast() => AsclepiusStatusHelper.HasSwiftcast(Player);
    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealthMetrics()
        => PartyHelper.CalculatePartyHealthMetrics(Player);
    protected override string GetJobName() => "Asclepius";

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
        IHpPredictionService hpPredictionService,
        IMpForecastService mpForecastService,
        IObjectTable objectTable,
        IPartyList partyList,
        IPlayerStatsService playerStatsService,
        ITargetingService targetingService,
        IHealingSpellSelector healingSpellSelector,
        ICooldownPlanner cooldownPlanner,
        IAddersgallTrackingService addersgallService,
        IAdderstingTrackingService adderstingService,
        IKardiaManager kardiaManager,
        IEukrasiaStateService eukrasiaService,
        AsclepiusStatusHelper statusHelper,
        IPartyHelper partyHelper,
        ICoHealerDetectionService? coHealerDetectionService = null,
        IBossMechanicDetector? bossMechanicDetector = null,
        IShieldTrackingService? shieldTrackingService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITimelineService? timelineService = null,
        ITrainingService? trainingService = null,
        AsclepiusDebugState? debugState = null,
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
        AddersgallService = addersgallService;
        AdderstingService = adderstingService;
        KardiaManager = kardiaManager;
        EukrasiaService = eukrasiaService;
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState ?? new AsclepiusDebugState();
    }

    #region Logging Helpers

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
public sealed class AsclepiusDebugState : DebugState
{
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

    #region DPS

    public string DoTState { get; set; } = "Idle";
    public float DoTRemaining { get; set; }
    public string PhlegmaState { get; set; } = "Idle";
    public int PhlegmaCharges { get; set; }
    public string ToxikonState { get; set; } = "Idle";
    public string PsycheState { get; set; } = "Idle";

    #endregion

    #region Healing

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
}
