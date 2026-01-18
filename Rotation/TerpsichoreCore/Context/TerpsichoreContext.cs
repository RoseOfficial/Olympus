using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.TerpsichoreCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Timeline;

namespace Olympus.Rotation.TerpsichoreCore.Context;

/// <summary>
/// Dancer-specific context implementation.
/// Provides all state needed for Terpsichore rotation modules.
/// </summary>
public sealed class TerpsichoreContext : ITerpsichoreContext
{
    #region IRotationContext Implementation

    public IPlayerCharacter Player { get; }
    public bool InCombat { get; }
    public bool IsMoving { get; }
    public bool CanExecuteGcd { get; }
    public bool CanExecuteOgcd { get; }

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

    public IObjectTable ObjectTable { get; }
    public IPartyList PartyList { get; }
    public IPluginLog? Log { get; }

    public (float avgHpPercent, float lowestHpPercent, int injuredCount) PartyHealthMetrics { get; }
    public bool HasSwiftcast => false; // Ranged physical DPS don't use Swiftcast

    #endregion

    #region IRangedDpsRotationContext Implementation

    public int ComboStep { get; }
    public uint LastComboAction { get; }
    public float ComboTimeRemaining { get; }

    #endregion

    #region ITerpsichoreContext Implementation

    // Gauge state
    public int Esprit { get; }
    public int Feathers { get; }
    public bool IsDancing { get; }
    public int StepIndex { get; }
    public byte CurrentStep { get; }
    public byte[] DanceSteps { get; }

    // Proc state
    public bool HasSilkenSymmetry { get; }
    public bool HasSilkenFlow { get; }
    public bool HasThreefoldFanDance { get; }
    public bool HasFourfoldFanDance { get; }

    // Buff state
    public bool HasFlourishingFinish { get; }
    public bool HasFlourishingStarfall { get; }
    public bool HasDevilment { get; }
    public float DevilmentRemaining { get; }
    public bool HasStandardFinish { get; }
    public bool HasTechnicalFinish { get; }

    // High-level procs
    public bool HasLastDanceReady { get; }
    public bool HasFinishingMoveReady { get; }
    public bool HasDanceOfTheDawnReady { get; }

    // Partner state
    public bool HasDancePartner { get; }
    public uint DancePartnerId { get; }

    // Helpers
    public TerpsichoreStatusHelper StatusHelper { get; }
    public TerpsichorePartyHelper PartyHelper { get; }
    public TerpsichoreDebugState Debug { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public TerpsichoreContext(
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
        IPlayerStatsService playerStatsService,
        ITargetingService targetingService,
        IObjectTable objectTable,
        IPartyList partyList,
        TerpsichoreStatusHelper statusHelper,
        TerpsichorePartyHelper partyHelper,
        TerpsichoreDebugState debugState,
        int esprit,
        int feathers,
        bool isDancing,
        int stepIndex,
        byte currentStep,
        byte[] danceSteps,
        int comboStep,
        uint lastComboAction,
        float comboTimeRemaining,
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
        PlayerStatsService = playerStatsService;
        TargetingService = targetingService;
        TimelineService = timelineService;
        ObjectTable = objectTable;
        PartyList = partyList;
        Log = log;

        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState;

        // Combo state (DNC has combo for Cascade->Fountain, Windmill->Bladeshower)
        ComboStep = comboStep;
        LastComboAction = lastComboAction;
        ComboTimeRemaining = comboTimeRemaining;

        // Gauge state
        Esprit = esprit;
        Feathers = feathers;
        IsDancing = isDancing;
        StepIndex = stepIndex;
        CurrentStep = currentStep;
        DanceSteps = danceSteps;

        // Proc state
        HasSilkenSymmetry = statusHelper.HasSilkenSymmetry(player);
        HasSilkenFlow = statusHelper.HasSilkenFlow(player);
        HasThreefoldFanDance = statusHelper.HasThreefoldFanDance(player);
        HasFourfoldFanDance = statusHelper.HasFourfoldFanDance(player);

        // Buff state
        HasFlourishingFinish = statusHelper.HasFlourishingFinish(player);
        HasFlourishingStarfall = statusHelper.HasFlourishingStarfall(player);
        HasDevilment = statusHelper.HasDevilment(player);
        DevilmentRemaining = statusHelper.GetDevilmentRemaining(player);
        HasStandardFinish = statusHelper.HasStandardFinish(player);
        HasTechnicalFinish = statusHelper.HasTechnicalFinish(player);

        // High-level procs
        HasLastDanceReady = statusHelper.HasLastDanceReady(player);
        HasFinishingMoveReady = statusHelper.HasFinishingMoveReady(player);
        HasDanceOfTheDawnReady = statusHelper.HasDanceOfTheDawnReady(player);

        // Partner state
        HasDancePartner = statusHelper.HasClosedPosition(player);
        DancePartnerId = partyHelper.GetDancePartnerId(player, statusHelper);

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);

        // Update debug state
        UpdateDebugState();
    }

    private (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealth(IPlayerCharacter player)
    {
        var totalHp = 0f;
        var lowestHp = 1f;
        var injuredCount = 0;
        var memberCount = 0;

        foreach (var member in PartyHelper.GetAllPartyMembers(player))
        {
            var hp = PartyHelper.GetHpPercent(member);
            totalHp += hp;
            memberCount++;

            if (hp < lowestHp)
                lowestHp = hp;

            if (hp < 0.95f)
                injuredCount++;
        }

        var avgHp = memberCount > 0 ? totalHp / memberCount : 1f;
        return (avgHp, lowestHp, injuredCount);
    }

    private void UpdateDebugState()
    {
        // Gauge
        Debug.Esprit = Esprit;
        Debug.Feathers = Feathers;
        Debug.IsDancing = IsDancing;
        Debug.StepIndex = StepIndex;

        // Current step name
        Debug.CurrentStep = CurrentStep switch
        {
            1 => "Emboite (Red)",
            2 => "Entrechat (Blue)",
            3 => "Jete (Green)",
            4 => "Pirouette (Yellow)",
            _ => "None"
        };

        // Procs
        Debug.HasSilkenSymmetry = HasSilkenSymmetry;
        Debug.HasSilkenFlow = HasSilkenFlow;
        Debug.HasThreefoldFanDance = HasThreefoldFanDance;
        Debug.HasFourfoldFanDance = HasFourfoldFanDance;

        // Buffs
        Debug.HasFlourishingFinish = HasFlourishingFinish;
        Debug.HasFlourishingStarfall = HasFlourishingStarfall;
        Debug.HasDevilment = HasDevilment;
        Debug.DevilmentRemaining = DevilmentRemaining;
        Debug.HasStandardFinish = HasStandardFinish;
        Debug.HasTechnicalFinish = HasTechnicalFinish;

        // High-level procs
        Debug.HasLastDanceReady = HasLastDanceReady;
        Debug.HasFinishingMoveReady = HasFinishingMoveReady;
        Debug.HasDanceOfTheDawnReady = HasDanceOfTheDawnReady;

        // Partner
        Debug.HasDancePartner = HasDancePartner;
        Debug.DancePartner = PartyHelper.GetDancePartnerName(Player, StatusHelper) ?? "None";

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
    }
}
