using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.CalliopeCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Debuff;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.CalliopeCore.Context;

/// <summary>
/// Bard-specific context implementation.
/// Provides all state needed for Calliope rotation modules.
/// </summary>
public sealed class CalliopeContext : ICalliopeContext
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

    #region ICalliopeContext Implementation

    // Gauge state
    public int SoulVoice { get; }
    public float SongTimer { get; }
    public int Repertoire { get; }
    public byte CurrentSong { get; }
    public int CodaCount { get; }

    // Song state
    public bool IsWanderersMinuetActive { get; }
    public bool IsMagesBalladActive { get; }
    public bool IsArmysPaeonActive { get; }
    public bool NoSongActive { get; }

    // Buff state
    public bool HasHawksEye { get; }
    public bool HasRagingStrikes { get; }
    public float RagingStrikesRemaining { get; }
    public bool HasBattleVoice { get; }
    public bool HasBarrage { get; }
    public bool HasRadiantFinale { get; }
    public bool HasBlastArrowReady { get; }
    public bool HasResonantArrowReady { get; }
    public bool HasRadiantEncoreReady { get; }

    // DoT state
    public bool HasCausticBite { get; }
    public float CausticBiteRemaining { get; }
    public bool HasStormbite { get; }
    public float StormbiteRemaining { get; }

    // Cooldown tracking
    public int BloodletterCharges { get; }

    // Helpers
    public CalliopeStatusHelper StatusHelper { get; }
    public CalliopePartyHelper PartyHelper { get; }
    public CalliopeDebugState Debug { get; }

    // Party Coordination
    public IPartyCoordinationService? PartyCoordinationService { get; }

    // Training
    public ITrainingService? TrainingService { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public CalliopeContext(
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
        CalliopeStatusHelper statusHelper,
        CalliopePartyHelper partyHelper,
        CalliopeDebugState debugState,
        int soulVoice,
        float songTimer,
        int repertoire,
        byte currentSong,
        int codaCount,
        int comboStep,
        uint lastComboAction,
        float comboTimeRemaining,
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

        // Combo state (Bard has no combo, but base class requires it)
        ComboStep = comboStep;
        LastComboAction = lastComboAction;
        ComboTimeRemaining = comboTimeRemaining;

        // Gauge state
        SoulVoice = soulVoice;
        SongTimer = songTimer;
        Repertoire = repertoire;
        CurrentSong = currentSong;
        CodaCount = codaCount;

        // Song state from gauge
        IsWanderersMinuetActive = currentSong == (byte)BRDActions.Song.WanderersMinuet && songTimer > 0;
        IsMagesBalladActive = currentSong == (byte)BRDActions.Song.MagesBallad && songTimer > 0;
        IsArmysPaeonActive = currentSong == (byte)BRDActions.Song.ArmysPaeon && songTimer > 0;
        NoSongActive = currentSong == (byte)BRDActions.Song.None || songTimer <= 0;

        // Buff state
        HasHawksEye = statusHelper.HasHawksEye(player);
        HasRagingStrikes = statusHelper.HasRagingStrikes(player);
        RagingStrikesRemaining = statusHelper.GetRagingStrikesRemaining(player);
        HasBattleVoice = statusHelper.HasBattleVoice(player);
        HasBarrage = statusHelper.HasBarrage(player);
        HasRadiantFinale = statusHelper.HasRadiantFinale(player);
        HasBlastArrowReady = statusHelper.HasBlastArrowReady(player);
        HasResonantArrowReady = statusHelper.HasResonantArrowReady(player);
        HasRadiantEncoreReady = statusHelper.HasRadiantEncoreReady(player);

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target for DoT tracking
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);

        // DoT state
        if (_currentTarget != null)
        {
            HasCausticBite = statusHelper.HasCausticBite(_currentTarget, player.EntityId);
            CausticBiteRemaining = statusHelper.GetCausticBiteRemaining(_currentTarget, player.EntityId);
            HasStormbite = statusHelper.HasStormbite(_currentTarget, player.EntityId);
            StormbiteRemaining = statusHelper.GetStormbiteRemaining(_currentTarget, player.EntityId);
        }
        else
        {
            HasCausticBite = false;
            CausticBiteRemaining = 0f;
            HasStormbite = false;
            StormbiteRemaining = 0f;
        }

        // Cooldown tracking - get charges from ActionService
        BloodletterCharges = GetActionCharges(BRDActions.Bloodletter.ActionId);

        // Update debug state
        UpdateDebugState();
    }

    private int GetActionCharges(uint actionId)
    {
        // Get charges via ActionService
        return (int)ActionService.GetCurrentCharges(actionId);
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
        Debug.SoulVoice = SoulVoice;
        Debug.SongTimer = SongTimer;
        Debug.Repertoire = Repertoire;
        Debug.CodaCount = CodaCount;

        // Song name
        Debug.CurrentSong = CurrentSong switch
        {
            (byte)BRDActions.Song.WanderersMinuet => "Wanderer's Minuet",
            (byte)BRDActions.Song.MagesBallad => "Mage's Ballad",
            (byte)BRDActions.Song.ArmysPaeon => "Army's Paeon",
            _ => "None"
        };

        // Buffs
        Debug.HasHawksEye = HasHawksEye;
        Debug.HasRagingStrikes = HasRagingStrikes;
        Debug.RagingStrikesRemaining = RagingStrikesRemaining;
        Debug.HasBattleVoice = HasBattleVoice;
        Debug.HasBarrage = HasBarrage;
        Debug.HasRadiantFinale = HasRadiantFinale;
        Debug.HasBlastArrowReady = HasBlastArrowReady;
        Debug.HasResonantArrowReady = HasResonantArrowReady;
        Debug.HasRadiantEncoreReady = HasRadiantEncoreReady;

        // DoTs
        Debug.HasCausticBite = HasCausticBite;
        Debug.CausticBiteRemaining = CausticBiteRemaining;
        Debug.HasStormbite = HasStormbite;
        Debug.StormbiteRemaining = StormbiteRemaining;

        // Cooldowns
        Debug.BloodletterCharges = BloodletterCharges;

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
    }
}
