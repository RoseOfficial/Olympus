using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.KratosCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Debuff;
using Olympus.Services.Positional;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Timeline;

namespace Olympus.Rotation.KratosCore.Context;

/// <summary>
/// Monk-specific context implementation.
/// Provides all state needed for Kratos rotation modules.
/// </summary>
public sealed class KratosContext : IKratosContext
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

    #region IKratosContext Implementation

    // Form state
    public MonkForm CurrentForm { get; }
    public bool HasFormlessFist { get; }
    public bool HasPerfectBalance { get; }
    public int PerfectBalanceStacks { get; }

    // Chakra
    public int Chakra { get; }
    public byte BeastChakra1 { get; }
    public byte BeastChakra2 { get; }
    public byte BeastChakra3 { get; }
    public int BeastChakraCount { get; }
    public bool HasLunarNadi { get; }
    public bool HasSolarNadi { get; }
    public bool HasBothNadi { get; }

    // Buffs
    public bool HasDisciplinedFist { get; }
    public float DisciplinedFistRemaining { get; }
    public bool HasLeadenFist { get; }
    public bool HasRiddleOfFire { get; }
    public float RiddleOfFireRemaining { get; }
    public bool HasBrotherhood { get; }
    public bool HasRiddleOfWind { get; }

    // Procs
    public bool HasRaptorsFury { get; }
    public bool HasCoeurlsFury { get; }
    public bool HasOpooposFury { get; }
    public bool HasFiresRumination { get; }
    public bool HasWindsRumination { get; }

    // DoT
    public bool HasDemolishOnTarget { get; }
    public float DemolishRemaining { get; }

    // Helpers
    public KratosStatusHelper StatusHelper { get; }
    public KratosPartyHelper PartyHelper { get; }
    public KratosDebugState Debug { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public KratosContext(
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
        KratosStatusHelper statusHelper,
        KratosPartyHelper partyHelper,
        KratosDebugState debugState,
        int chakra,
        byte[] beastChakra,
        bool hasLunarNadi,
        bool hasSolarNadi,
        int comboStep,
        uint lastComboAction,
        float comboTimeRemaining,
        bool isAtRear,
        bool isAtFlank,
        bool targetHasPositionalImmunity,
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

        // Combo state
        ComboStep = comboStep;
        LastComboAction = lastComboAction;
        ComboTimeRemaining = comboTimeRemaining;

        // Positional state
        IsAtRear = isAtRear;
        IsAtFlank = isAtFlank;
        TargetHasPositionalImmunity = targetHasPositionalImmunity;
        HasTrueNorth = statusHelper.HasTrueNorth(player);

        // Chakra state
        Chakra = chakra;
        BeastChakra1 = beastChakra[0];
        BeastChakra2 = beastChakra[1];
        BeastChakra3 = beastChakra[2];
        BeastChakraCount = CountBeastChakra(beastChakra);
        HasLunarNadi = hasLunarNadi;
        HasSolarNadi = hasSolarNadi;
        HasBothNadi = hasLunarNadi && hasSolarNadi;

        // Form state
        CurrentForm = statusHelper.GetCurrentForm(player);
        HasFormlessFist = statusHelper.HasFormlessFist(player);
        HasPerfectBalance = statusHelper.HasPerfectBalance(player);
        PerfectBalanceStacks = statusHelper.GetPerfectBalanceStacks(player);

        // Buff state
        HasDisciplinedFist = statusHelper.HasDisciplinedFist(player);
        DisciplinedFistRemaining = statusHelper.GetDisciplinedFistRemaining(player);
        HasLeadenFist = statusHelper.HasLeadenFist(player);
        HasRiddleOfFire = statusHelper.HasRiddleOfFire(player);
        RiddleOfFireRemaining = statusHelper.GetRiddleOfFireRemaining(player);
        HasBrotherhood = statusHelper.HasBrotherhood(player);
        HasRiddleOfWind = statusHelper.HasRiddleOfWind(player);

        // Proc state
        HasRaptorsFury = statusHelper.HasRaptorsFury(player);
        HasCoeurlsFury = statusHelper.HasCoeurlsFury(player);
        HasOpooposFury = statusHelper.HasOpooposFury(player);
        HasFiresRumination = statusHelper.HasFiresRumination(player);
        HasWindsRumination = statusHelper.HasWindsRumination(player);

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target for DoT tracking
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            3f,
            player);

        // DoT state
        if (_currentTarget != null)
        {
            HasDemolishOnTarget = statusHelper.HasDemolish(_currentTarget, player.EntityId);
            DemolishRemaining = statusHelper.GetDemolishRemaining(_currentTarget, player.EntityId);
        }
        else
        {
            HasDemolishOnTarget = false;
            DemolishRemaining = 0f;
        }

        // Update debug state
        UpdateDebugState();
    }

    private static int CountBeastChakra(byte[] beastChakra)
    {
        var count = 0;
        if (beastChakra[0] != 0) count++;
        if (beastChakra[1] != 0) count++;
        if (beastChakra[2] != 0) count++;
        return count;
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
        // Form
        Debug.CurrentForm = CurrentForm;
        Debug.HasPerfectBalance = HasPerfectBalance;
        Debug.PerfectBalanceStacks = PerfectBalanceStacks;
        Debug.HasFormlessFist = HasFormlessFist;

        // Chakra
        Debug.Chakra = Chakra;
        Debug.BeastChakraState = KratosDebugState.FormatBeastChakra(BeastChakra1, BeastChakra2, BeastChakra3);
        Debug.BeastChakraCount = BeastChakraCount;
        Debug.HasLunarNadi = HasLunarNadi;
        Debug.HasSolarNadi = HasSolarNadi;

        // Buffs
        Debug.HasDisciplinedFist = HasDisciplinedFist;
        Debug.DisciplinedFistRemaining = DisciplinedFistRemaining;
        Debug.HasLeadenFist = HasLeadenFist;
        Debug.HasRiddleOfFire = HasRiddleOfFire;
        Debug.RiddleOfFireRemaining = RiddleOfFireRemaining;
        Debug.HasBrotherhood = HasBrotherhood;
        Debug.HasRiddleOfWind = HasRiddleOfWind;

        // Procs
        Debug.HasRaptorsFury = HasRaptorsFury;
        Debug.HasCoeurlsFury = HasCoeurlsFury;
        Debug.HasOpooposFury = HasOpooposFury;
        Debug.HasFiresRumination = HasFiresRumination;
        Debug.HasWindsRumination = HasWindsRumination;

        // DoT
        Debug.HasDemolishOnTarget = HasDemolishOnTarget;
        Debug.DemolishRemaining = DemolishRemaining;

        // Positional
        Debug.IsAtRear = IsAtRear;
        Debug.IsAtFlank = IsAtFlank;
        Debug.HasTrueNorth = HasTrueNorth;
        Debug.TargetHasPositionalImmunity = TargetHasPositionalImmunity;

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
    }
}
