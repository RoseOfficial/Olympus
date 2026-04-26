using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.HecateCore.Helpers;
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

namespace Olympus.Rotation.HecateCore.Context;

/// <summary>
/// Black Mage-specific context implementation.
/// Provides all state needed for Hecate rotation modules.
/// </summary>
public sealed class HecateContext : IHecateContext
{
    #region IRotationContext Implementation

    public IPlayerCharacter Player { get; }
    public bool InCombat { get; }
    public bool IsMoving { get; }
    public bool CanExecuteGcd { get; }
    public bool CanExecuteOgcd { get; }

    public IActionService ActionService { get; }
    public IActionTracker ActionTracker { get; }
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

    public IPartyCoordinationService? PartyCoordinationService { get; }

    #endregion

    #region ICasterDpsRotationContext Implementation

    public int CurrentMp { get; }
    public int MaxMp { get; }
    public float MpPercent { get; }

    public bool IsCasting { get; }
    public float CastRemaining { get; }
    public bool CanSlidecast { get; }

    public bool HasSwiftcast { get; }
    public bool HasTriplecast { get; }
    public int TriplecastStacks { get; }
    public bool HasInstantCast { get; }

    #endregion

    #region IHecateContext Implementation

    // Element state
    public bool InAstralFire { get; }
    public bool InUmbralIce { get; }
    public int ElementStacks { get; }
    public float ElementTimer { get; }
    public bool IsEnochianActive { get; }
    public int AstralFireStacks { get; }
    public int UmbralIceStacks { get; }

    // Resource state
    public int UmbralHearts { get; }
    public int PolyglotStacks { get; }
    public int AstralSoulStacks { get; }
    public bool HasParadox { get; }

    // Buff state
    public bool HasFirestarter { get; }
    public float FirestarterRemaining { get; }
    public bool HasThunderhead { get; }
    public float ThunderheadRemaining { get; }
    public bool HasLeyLines { get; }
    public float LeyLinesRemaining { get; }

    // Target state
    public bool HasThunderDoT { get; }
    public float ThunderDoTRemaining { get; }

    // Movement state
    public bool NeedsInstant { get; }

    // Cooldown tracking
    public int TriplecastCharges { get; }
    public bool SwiftcastReady { get; }
    public bool ManafontReady { get; }
    public bool AmplifierReady { get; }
    public bool LeyLinesReady { get; }

    // Helpers
    public HecateStatusHelper StatusHelper { get; }
    public CasterPartyHelper PartyHelper { get; }
    public HecateDebugState Debug { get; }

    // Training
    public ITrainingService? TrainingService { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public HecateContext(
        IPlayerCharacter player,
        bool inCombat,
        bool isMoving,
        bool canExecuteGcd,
        bool canExecuteOgcd,
        IActionService actionService,
        IActionTracker actionTracker,
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
        HecateStatusHelper statusHelper,
        CasterPartyHelper partyHelper,
        HecateDebugState debugState,
        int elementStacks,
        float elementTimer,
        int umbralHearts,
        int polyglotStacks,
        int astralSoulStacks,
        bool hasParadox,
        ITimelineService? timelineService = null,
        ITrainingService? trainingService = null,
        IPartyCoordinationService? partyCoordinationService = null,
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
        TrainingService = trainingService;
        ObjectTable = objectTable;
        PartyList = partyList;
        Log = log;
        PartyCoordinationService = partyCoordinationService;

        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState;

        // MP state
        CurrentMp = (int)player.CurrentMp;
        MaxMp = (int)player.MaxMp;
        MpPercent = MaxMp > 0 ? (float)CurrentMp / MaxMp : 0f;

        // Cast state
        IsCasting = player.IsCasting;
        CastRemaining = player.TotalCastTime - player.CurrentCastTime;
        CanSlidecast = IsCasting && CastRemaining <= 0.5f;

        // Element state
        ElementStacks = elementStacks;
        ElementTimer = elementTimer;
        InAstralFire = elementStacks > 0;
        InUmbralIce = elementStacks < 0;
        IsEnochianActive = elementTimer > 0;
        AstralFireStacks = elementStacks > 0 ? elementStacks : 0;
        UmbralIceStacks = elementStacks < 0 ? -elementStacks : 0;

        // Resource state
        UmbralHearts = umbralHearts;
        PolyglotStacks = polyglotStacks;
        AstralSoulStacks = astralSoulStacks;
        HasParadox = hasParadox;

        // Buff state
        HasSwiftcast = BaseStatusHelper.HasSwiftcast(player);
        HasTriplecast = statusHelper.HasTriplecast(player);
        TriplecastStacks = statusHelper.GetTriplecastStacks(player);
        HasInstantCast = HasSwiftcast || HasTriplecast;

        HasFirestarter = statusHelper.HasFirestarter(player);
        FirestarterRemaining = statusHelper.GetFirestarterRemaining(player);
        HasThunderhead = statusHelper.HasThunderhead(player);
        ThunderheadRemaining = statusHelper.GetThunderheadRemaining(player);
        HasLeyLines = statusHelper.HasLeyLines(player);
        LeyLinesRemaining = statusHelper.GetLeyLinesRemaining(player);

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target for debuff tracking
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        // Target state
        if (_currentTarget != null)
        {
            HasThunderDoT = statusHelper.HasThunderDoT(_currentTarget, player.EntityId);
            ThunderDoTRemaining = statusHelper.GetThunderDoTRemaining(_currentTarget, player.EntityId);
        }
        else
        {
            HasThunderDoT = false;
            ThunderDoTRemaining = 0f;
        }

        // Movement state
        NeedsInstant = isMoving && !HasInstantCast;

        // Cooldown tracking
        TriplecastCharges = (int)actionService.GetCurrentCharges(BLMActions.Triplecast.ActionId);
        SwiftcastReady = actionService.IsActionReady(RoleActions.Swiftcast.ActionId);
        ManafontReady = actionService.IsActionReady(BLMActions.Manafont.ActionId);
        AmplifierReady = actionService.IsActionReady(BLMActions.Amplifier.ActionId);
        LeyLinesReady = actionService.IsActionReady(BLMActions.LeyLines.ActionId);

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
        // Element state
        Debug.InAstralFire = InAstralFire;
        Debug.InUmbralIce = InUmbralIce;
        Debug.ElementStacks = ElementStacks;
        Debug.ElementTimer = ElementTimer;
        Debug.IsEnochianActive = IsEnochianActive;

        // Resource state
        Debug.CurrentMp = CurrentMp;
        Debug.MaxMp = MaxMp;
        Debug.UmbralHearts = UmbralHearts;
        Debug.PolyglotStacks = PolyglotStacks;
        Debug.AstralSoulStacks = AstralSoulStacks;
        Debug.HasParadox = HasParadox;

        // Buff state
        Debug.HasFirestarter = HasFirestarter;
        Debug.FirestarterRemaining = FirestarterRemaining;
        Debug.HasThunderhead = HasThunderhead;
        Debug.ThunderheadRemaining = ThunderheadRemaining;
        Debug.HasLeyLines = HasLeyLines;
        Debug.LeyLinesRemaining = LeyLinesRemaining;
        Debug.TriplecastStacks = TriplecastStacks;
        Debug.HasSwiftcast = HasSwiftcast;

        // Target state
        Debug.HasThunderDoT = HasThunderDoT;
        Debug.ThunderDoTRemaining = ThunderDoTRemaining;

        // Cooldowns
        Debug.TriplecastCharges = TriplecastCharges;
        Debug.ManafontReady = ManafontReady;
        Debug.AmplifierReady = AmplifierReady;
        Debug.LeyLinesReady = LeyLinesReady;

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";

        // Phase
        Debug.Phase = InAstralFire ? "Fire" : InUmbralIce ? "Ice" : "None";
    }
}
