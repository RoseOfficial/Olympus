using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ThanatosCore.Helpers;
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

namespace Olympus.Rotation.ThanatosCore.Context;

/// <summary>
/// Reaper-specific context implementation.
/// Provides all state needed for Thanatos rotation modules.
/// </summary>
public sealed class ThanatosContext : IThanatosContext
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

    #region IThanatosContext Implementation

    // Gauge state
    public int Soul { get; }
    public int Shroud { get; }
    public int LemureShroud { get; }
    public int VoidShroud { get; }
    public bool IsEnshrouded { get; }
    public float EnshroudTimer { get; }

    // Soul Reaver state
    public bool HasSoulReaver { get; }
    public int SoulReaverStacks { get; }
    public bool HasEnhancedGibbet { get; }
    public bool HasEnhancedGallows { get; }
    public bool HasEnhancedVoidReaping { get; }
    public bool HasEnhancedCrossReaping { get; }

    // Buff state
    public bool HasArcaneCircle { get; }
    public float ArcaneCircleRemaining { get; }
    public bool HasBloodsownCircle { get; }
    public int ImmortalSacrificeStacks { get; }
    public bool HasSoulsow { get; }

    // Proc state
    public bool HasPerfectioParata { get; }
    public bool HasOblatio { get; }
    public bool HasIdealHost { get; }
    public bool HasEnhancedHarpe { get; }

    // Target state
    public bool HasDeathsDesign { get; }
    public float DeathsDesignRemaining { get; }

    // Helpers
    public ThanatosStatusHelper StatusHelper { get; }
    public MeleeDpsPartyHelper PartyHelper { get; }
    public ThanatosDebugState Debug { get; }

    // Party Coordination
    public IPartyCoordinationService? PartyCoordinationService { get; }

    // Training
    public ITrainingService? TrainingService { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public ThanatosContext(
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
        ThanatosStatusHelper statusHelper,
        MeleeDpsPartyHelper partyHelper,
        ThanatosDebugState debugState,
        int soul,
        int shroud,
        int lemureShroud,
        int voidShroud,
        float enshroudTimer,
        int comboStep,
        uint lastComboAction,
        float comboTimeRemaining,
        bool isAtRear,
        bool isAtFlank,
        bool targetHasPositionalImmunity,
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
        Soul = soul;
        Shroud = shroud;
        LemureShroud = lemureShroud;
        VoidShroud = voidShroud;
        EnshroudTimer = enshroudTimer;
        IsEnshrouded = lemureShroud > 0 || enshroudTimer > 0;

        // Soul Reaver state
        HasSoulReaver = statusHelper.HasSoulReaver(player);
        SoulReaverStacks = statusHelper.GetSoulReaverStacks(player);
        HasEnhancedGibbet = statusHelper.HasEnhancedGibbet(player);
        HasEnhancedGallows = statusHelper.HasEnhancedGallows(player);
        HasEnhancedVoidReaping = statusHelper.HasEnhancedVoidReaping(player);
        HasEnhancedCrossReaping = statusHelper.HasEnhancedCrossReaping(player);

        // Buff state
        HasArcaneCircle = statusHelper.HasArcaneCircle(player);
        ArcaneCircleRemaining = statusHelper.GetArcaneCircleRemaining(player);
        HasBloodsownCircle = statusHelper.HasBloodsownCircle(player);
        ImmortalSacrificeStacks = statusHelper.GetImmortalSacrificeStacks(player);
        HasSoulsow = statusHelper.HasSoulsow(player);

        // Proc state
        HasPerfectioParata = statusHelper.HasPerfectioParata(player);
        HasOblatio = statusHelper.HasOblatio(player);
        HasIdealHost = statusHelper.HasIdealHost(player);
        HasEnhancedHarpe = statusHelper.HasEnhancedHarpe(player);

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target for debuff tracking using game API range check for accuracy
        _currentTarget = targetingService.FindEnemyForAction(
            configuration.Targeting.EnemyStrategy,
            RPRActions.Slice.ActionId,
            player);

        // Target state
        if (_currentTarget != null)
        {
            HasDeathsDesign = statusHelper.HasDeathsDesign(_currentTarget, player.EntityId);
            DeathsDesignRemaining = statusHelper.GetDeathsDesignRemaining(_currentTarget, player.EntityId);
        }
        else
        {
            HasDeathsDesign = false;
            DeathsDesignRemaining = 0f;
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
        Debug.Soul = Soul;
        Debug.Shroud = Shroud;
        Debug.LemureShroud = LemureShroud;
        Debug.VoidShroud = VoidShroud;
        Debug.EnshroudTimer = EnshroudTimer;

        // State
        Debug.IsEnshrouded = IsEnshrouded;
        Debug.HasSoulReaver = HasSoulReaver;
        Debug.SoulReaverStacks = SoulReaverStacks;

        // Enhanced buffs
        Debug.HasEnhancedGibbet = HasEnhancedGibbet;
        Debug.HasEnhancedGallows = HasEnhancedGallows;
        Debug.HasEnhancedVoidReaping = HasEnhancedVoidReaping;
        Debug.HasEnhancedCrossReaping = HasEnhancedCrossReaping;

        // Procs
        Debug.HasPerfectioParata = HasPerfectioParata;
        Debug.HasOblatio = HasOblatio;
        Debug.HasSoulsow = HasSoulsow;

        // Arcane Circle
        Debug.HasArcaneCircle = HasArcaneCircle;
        Debug.ImmortalSacrificeStacks = ImmortalSacrificeStacks;
        Debug.HasBloodsownCircle = HasBloodsownCircle;

        // Target
        Debug.HasDeathsDesign = HasDeathsDesign;
        Debug.DeathsDesignRemaining = DeathsDesignRemaining;

        // Positional
        Debug.IsAtRear = IsAtRear;
        Debug.IsAtFlank = IsAtFlank;
        Debug.HasTrueNorth = HasTrueNorth;
        Debug.TargetHasPositionalImmunity = TargetHasPositionalImmunity;

        // Combo
        Debug.ComboStep = ComboStep;

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
    }
}
