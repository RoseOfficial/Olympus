using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.EchidnaCore.Helpers;
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

namespace Olympus.Rotation.EchidnaCore.Context;

/// <summary>
/// Viper-specific context implementation.
/// Provides all state needed for Echidna rotation modules.
/// </summary>
public sealed class EchidnaContext : IEchidnaContext
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

    #region IEchidnaContext Implementation

    // Gauge state
    public int SerpentOffering { get; }
    public int AnguineTribute { get; }
    public int RattlingCoils { get; }
    public bool IsReawakened { get; }
    public VPRActions.DreadCombo DreadCombo { get; }

    // Buff state
    public bool HasHuntersInstinct { get; }
    public float HuntersInstinctRemaining { get; }
    public bool HasSwiftscaled { get; }
    public float SwiftscaledRemaining { get; }
    public bool HasHonedSteel { get; }
    public bool HasHonedReavers { get; }
    public bool HasReadyToReawaken { get; }

    // Venom buffs
    public bool HasFlankstungVenom { get; }
    public bool HasHindstungVenom { get; }
    public bool HasFlanksbaneVenom { get; }
    public bool HasHindsbaneVenom { get; }
    public bool HasGrimskinsVenom { get; }
    public bool HasGrimhuntersVenom { get; }

    // oGCD procs
    public bool HasPoisedForTwinfang { get; }
    public bool HasPoisedForTwinblood { get; }

    // Target state
    public bool HasNoxiousGnash { get; }
    public float NoxiousGnashRemaining { get; }

    // Helpers
    public EchidnaStatusHelper StatusHelper { get; }
    public MeleeDpsPartyHelper PartyHelper { get; }
    public EchidnaDebugState Debug { get; }

    // Party Coordination
    public IPartyCoordinationService? PartyCoordinationService { get; }

    // Training
    public ITrainingService? TrainingService { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public EchidnaContext(
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
        EchidnaStatusHelper statusHelper,
        MeleeDpsPartyHelper partyHelper,
        EchidnaDebugState debugState,
        int serpentOffering,
        int anguineTribute,
        int rattlingCoils,
        VPRActions.DreadCombo dreadCombo,
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
        SerpentOffering = serpentOffering;
        AnguineTribute = anguineTribute;
        RattlingCoils = rattlingCoils;
        DreadCombo = dreadCombo;
        IsReawakened = anguineTribute > 0;

        // Buff state
        HasHuntersInstinct = statusHelper.HasHuntersInstinct(player);
        HuntersInstinctRemaining = statusHelper.GetHuntersInstinctRemaining(player);
        HasSwiftscaled = statusHelper.HasSwiftscaled(player);
        SwiftscaledRemaining = statusHelper.GetSwiftscaledRemaining(player);
        HasHonedSteel = statusHelper.HasHonedSteel(player);
        HasHonedReavers = statusHelper.HasHonedReavers(player);
        HasReadyToReawaken = statusHelper.HasReadyToReawaken(player);

        // Venom buffs
        HasFlankstungVenom = statusHelper.HasFlankstungVenom(player);
        HasHindstungVenom = statusHelper.HasHindstungVenom(player);
        HasFlanksbaneVenom = statusHelper.HasFlanksbaneVenom(player);
        HasHindsbaneVenom = statusHelper.HasHindsbaneVenom(player);
        HasGrimskinsVenom = statusHelper.HasGrimskinsVenom(player);
        HasGrimhuntersVenom = statusHelper.HasGrimhuntersVenom(player);

        // oGCD procs
        HasPoisedForTwinfang = statusHelper.HasPoisedForTwinfang(player);
        HasPoisedForTwinblood = statusHelper.HasPoisedForTwinblood(player);

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target for debuff tracking
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            3f,
            player);

        // Target state
        if (_currentTarget != null)
        {
            HasNoxiousGnash = statusHelper.HasNoxiousGnash(_currentTarget, player.EntityId);
            NoxiousGnashRemaining = statusHelper.GetNoxiousGnashRemaining(_currentTarget, player.EntityId);
        }
        else
        {
            HasNoxiousGnash = false;
            NoxiousGnashRemaining = 0f;
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
        Debug.SerpentOffering = SerpentOffering;
        Debug.AnguineTribute = AnguineTribute;
        Debug.RattlingCoils = RattlingCoils;
        Debug.DreadCombo = DreadCombo;

        // State
        Debug.IsReawakened = IsReawakened;
        Debug.HasHuntersInstinct = HasHuntersInstinct;
        Debug.HasSwiftscaled = HasSwiftscaled;
        Debug.HuntersInstinctRemaining = HuntersInstinctRemaining;
        Debug.SwiftscaledRemaining = SwiftscaledRemaining;

        // Combo enhancement
        Debug.HasHonedSteel = HasHonedSteel;
        Debug.HasHonedReavers = HasHonedReavers;
        Debug.HasReadyToReawaken = HasReadyToReawaken;

        // Venoms
        Debug.HasFlankstungVenom = HasFlankstungVenom;
        Debug.HasHindstungVenom = HasHindstungVenom;
        Debug.HasFlanksbaneVenom = HasFlanksbaneVenom;
        Debug.HasHindsbaneVenom = HasHindsbaneVenom;

        // oGCD procs
        Debug.HasPoisedForTwinfang = HasPoisedForTwinfang;
        Debug.HasPoisedForTwinblood = HasPoisedForTwinblood;

        // Target
        Debug.HasNoxiousGnash = HasNoxiousGnash;
        Debug.NoxiousGnashRemaining = NoxiousGnashRemaining;

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
