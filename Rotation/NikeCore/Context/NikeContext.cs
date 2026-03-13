using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.NikeCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Debuff;
using Olympus.Services.Party;
using Olympus.Services.Positional;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.NikeCore.Context;

/// <summary>
/// Samurai-specific context implementation.
/// Provides all state needed for Nike rotation modules.
/// </summary>
public sealed class NikeContext : INikeContext
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
    public bool HasSwiftcast => false; // Melee DPS don't use Swiftcast

    #endregion

    #region IMeleeDpsRotationContext Implementation

    public int ComboStep { get; }
    public uint LastComboAction { get; }
    public float ComboTimeRemaining { get; }
    public bool IsAtRear { get; }
    public bool IsAtFlank { get; }
    public bool TargetHasPositionalImmunity { get; }
    public bool HasTrueNorth { get; }

    #endregion

    #region INikeContext Implementation

    // Gauge
    public int Kenki { get; }
    public SAMActions.SenType Sen { get; }
    public int SenCount { get; }
    public bool HasSetsu { get; }
    public bool HasGetsu { get; }
    public bool HasKa { get; }
    public int Meditation { get; }

    // Buffs
    public bool HasFugetsu { get; }
    public float FugetsuRemaining { get; }
    public bool HasFuka { get; }
    public float FukaRemaining { get; }
    public bool HasMeikyoShisui { get; }
    public int MeikyoStacks { get; }
    public bool HasOgiNamikiriReady { get; }
    public bool HasKaeshiNamikiriReady { get; }
    public bool HasTsubameGaeshiReady { get; }
    public bool HasZanshinReady { get; }

    // DoT
    public bool HasHiganbanaOnTarget { get; }
    public float HiganbanaRemaining { get; }

    // Iaijutsu tracking
    public SAMActions.IaijutsuType LastIaijutsu { get; }

    // Helpers
    public NikeStatusHelper StatusHelper { get; }
    public MeleeDpsPartyHelper PartyHelper { get; }
    public NikeDebugState Debug { get; }

    // Party Coordination
    public IPartyCoordinationService? PartyCoordinationService { get; }

    // Training
    public ITrainingService? TrainingService { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public NikeContext(
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
        IPositionalService positionalService,
        NikeStatusHelper statusHelper,
        MeleeDpsPartyHelper partyHelper,
        NikeDebugState debugState,
        int kenki,
        SAMActions.SenType sen,
        int meditation,
        int comboStep,
        uint lastComboAction,
        float comboTimeRemaining,
        bool isAtRear,
        bool isAtFlank,
        bool targetHasPositionalImmunity,
        SAMActions.IaijutsuType lastIaijutsu,
        ITimelineService? timelineService = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITrainingService? trainingService = null,
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
        PartyCoordinationService = partyCoordinationService;
        TrainingService = trainingService;
        ObjectTable = objectTable;
        PartyList = partyList;
        Log = log;

        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState;

        // Combo state
        ComboStep = comboStep;
        LastComboAction = lastComboAction;
        ComboTimeRemaining = comboTimeRemaining;

        // Positional state
        IsAtRear = isAtRear;
        IsAtFlank = isAtFlank;
        TargetHasPositionalImmunity = targetHasPositionalImmunity;
        HasTrueNorth = statusHelper.HasTrueNorth(player);

        // Gauge state
        Kenki = kenki;
        Sen = sen;
        SenCount = SAMActions.CountSen(sen);
        HasSetsu = (sen & SAMActions.SenType.Setsu) != 0;
        HasGetsu = (sen & SAMActions.SenType.Getsu) != 0;
        HasKa = (sen & SAMActions.SenType.Ka) != 0;
        Meditation = meditation;

        // Buff state
        HasFugetsu = statusHelper.HasFugetsu(player);
        FugetsuRemaining = statusHelper.GetFugetsuRemaining(player);
        HasFuka = statusHelper.HasFuka(player);
        FukaRemaining = statusHelper.GetFukaRemaining(player);
        HasMeikyoShisui = statusHelper.HasMeikyoShisui(player);
        MeikyoStacks = statusHelper.GetMeikyoStacks(player);
        HasOgiNamikiriReady = statusHelper.HasOgiNamikiriReady(player);
        HasKaeshiNamikiriReady = statusHelper.HasKaeshiNamikiriReady(player);
        HasTsubameGaeshiReady = statusHelper.HasTsubameGaeshiReady(player);
        HasZanshinReady = statusHelper.HasZanshinReady(player);

        // Iaijutsu tracking
        LastIaijutsu = lastIaijutsu;

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target for DoT tracking using game API range check for accuracy
        _currentTarget = targetingService.FindEnemyForAction(
            configuration.Targeting.EnemyStrategy,
            SAMActions.Hakaze.ActionId,
            player);

        // DoT state
        if (_currentTarget != null)
        {
            HasHiganbanaOnTarget = statusHelper.HasHiganbana(_currentTarget, player.EntityId);
            HiganbanaRemaining = statusHelper.GetHiganbanaRemaining(_currentTarget, player.EntityId);
        }
        else
        {
            HasHiganbanaOnTarget = false;
            HiganbanaRemaining = 0f;
        }

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
        Debug.Kenki = Kenki;
        Debug.Sen = Sen;
        Debug.SenCount = SenCount;
        Debug.Meditation = Meditation;

        // Buffs
        Debug.HasFugetsu = HasFugetsu;
        Debug.FugetsuRemaining = FugetsuRemaining;
        Debug.HasFuka = HasFuka;
        Debug.FukaRemaining = FukaRemaining;
        Debug.HasMeikyoShisui = HasMeikyoShisui;
        Debug.MeikyoStacks = MeikyoStacks;
        Debug.HasOgiNamikiriReady = HasOgiNamikiriReady;
        Debug.HasKaeshiNamikiriReady = HasKaeshiNamikiriReady;
        Debug.HasTsubameGaeshiReady = HasTsubameGaeshiReady;
        Debug.HasZanshinReady = HasZanshinReady;

        // DoT
        Debug.HasHiganbanaOnTarget = HasHiganbanaOnTarget;
        Debug.HiganbanaRemaining = HiganbanaRemaining;

        // Iaijutsu
        Debug.LastIaijutsu = LastIaijutsu;

        // Combo
        Debug.ComboStep = ComboStep;
        Debug.ComboTimeRemaining = ComboTimeRemaining;

        // Positional
        Debug.IsAtRear = IsAtRear;
        Debug.IsAtFlank = IsAtFlank;
        Debug.HasTrueNorth = HasTrueNorth;
        Debug.TargetHasPositionalImmunity = TargetHasPositionalImmunity;

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
    }
}
