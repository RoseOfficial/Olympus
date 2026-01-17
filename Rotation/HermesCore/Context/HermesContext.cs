using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.HermesCore.Helpers;
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

namespace Olympus.Rotation.HermesCore.Context;

/// <summary>
/// Ninja-specific context implementation.
/// Provides all state needed for Hermes rotation modules.
/// </summary>
public sealed class HermesContext : IHermesContext
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

    #region IHermesContext Implementation

    // Gauge
    public int Ninki { get; }
    public int Kazematoi { get; }

    // Mudra state
    public bool IsMudraActive { get; }
    public int MudraCount { get; }
    public NINActions.MudraType Mudra1 { get; }
    public NINActions.MudraType Mudra2 { get; }
    public NINActions.MudraType Mudra3 { get; }
    public bool HasKassatsu { get; }
    public bool HasTenChiJin { get; }
    public int TenChiJinStacks { get; }

    // Buffs
    public bool HasSuiton { get; }
    public float SuitonRemaining { get; }
    public bool HasBunshin { get; }
    public int BunshinStacks { get; }
    public bool HasPhantomKamaitachiReady { get; }
    public bool HasRaijuReady { get; }
    public int RaijuStacks { get; }
    public bool HasMeisui { get; }
    public bool HasTenriJindoReady { get; }

    // Debuffs on target
    public bool HasKunaisBaneOnTarget { get; }
    public float KunaisBaneRemaining { get; }
    public bool HasDokumoriOnTarget { get; }
    public float DokumoriRemaining { get; }

    // Helpers
    public HermesStatusHelper StatusHelper { get; }
    public HermesPartyHelper PartyHelper { get; }
    public MudraHelper MudraHelper { get; }
    public HermesDebugState Debug { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public HermesContext(
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
        HermesStatusHelper statusHelper,
        HermesPartyHelper partyHelper,
        MudraHelper mudraHelper,
        HermesDebugState debugState,
        int ninki,
        int kazematoi,
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
        MudraHelper = mudraHelper;
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
        Ninki = ninki;
        Kazematoi = kazematoi;

        // Mudra state from helper
        IsMudraActive = statusHelper.IsMudraActive(player) || mudraHelper.IsSequenceActive;
        MudraCount = mudraHelper.MudraCount;
        Mudra1 = mudraHelper.Mudra1;
        Mudra2 = mudraHelper.Mudra2;
        Mudra3 = mudraHelper.Mudra3;

        // Buff state
        HasKassatsu = statusHelper.HasKassatsu(player);
        HasTenChiJin = statusHelper.HasTenChiJin(player);
        TenChiJinStacks = statusHelper.GetTenChiJinStacks(player);
        HasSuiton = statusHelper.HasSuiton(player);
        SuitonRemaining = statusHelper.GetSuitonRemaining(player);
        HasBunshin = statusHelper.HasBunshin(player);
        BunshinStacks = statusHelper.GetBunshinStacks(player);
        HasPhantomKamaitachiReady = statusHelper.HasPhantomKamaitachiReady(player);
        HasRaijuReady = statusHelper.HasRaijuReady(player);
        RaijuStacks = statusHelper.GetRaijuStacks(player);
        HasMeisui = statusHelper.HasMeisui(player);
        HasTenriJindoReady = statusHelper.HasTenriJindoReady(player);

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target for debuff tracking
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            3f,
            player);

        // Debuff state
        if (_currentTarget != null)
        {
            HasKunaisBaneOnTarget = statusHelper.HasKunaisBane(_currentTarget, player.EntityId);
            KunaisBaneRemaining = statusHelper.GetKunaisBaneRemaining(_currentTarget, player.EntityId);
            HasDokumoriOnTarget = statusHelper.HasDokumori(_currentTarget, player.EntityId);
            DokumoriRemaining = statusHelper.GetDokumoriRemaining(_currentTarget, player.EntityId);
        }
        else
        {
            HasKunaisBaneOnTarget = false;
            KunaisBaneRemaining = 0f;
            HasDokumoriOnTarget = false;
            DokumoriRemaining = 0f;
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
        Debug.Ninki = Ninki;
        Debug.Kazematoi = Kazematoi;

        // Mudra
        Debug.IsMudraActive = IsMudraActive;
        Debug.MudraCount = MudraCount;
        Debug.MudraSequence = HermesDebugState.FormatMudraSequence(Mudra1, Mudra2, Mudra3);
        Debug.PendingNinjutsu = MudraHelper.TargetNinjutsu;
        Debug.HasKassatsu = HasKassatsu;
        Debug.HasTenChiJin = HasTenChiJin;
        Debug.TenChiJinStacks = TenChiJinStacks;

        // Buffs
        Debug.HasSuiton = HasSuiton;
        Debug.SuitonRemaining = SuitonRemaining;
        Debug.HasBunshin = HasBunshin;
        Debug.BunshinStacks = BunshinStacks;
        Debug.HasPhantomKamaitachiReady = HasPhantomKamaitachiReady;
        Debug.HasRaijuReady = HasRaijuReady;
        Debug.RaijuStacks = RaijuStacks;
        Debug.HasMeisui = HasMeisui;
        Debug.HasTenriJindoReady = HasTenriJindoReady;

        // Debuffs
        Debug.HasKunaisBaneOnTarget = HasKunaisBaneOnTarget;
        Debug.KunaisBaneRemaining = KunaisBaneRemaining;
        Debug.HasDokumoriOnTarget = HasDokumoriOnTarget;
        Debug.DokumoriRemaining = DokumoriRemaining;

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
