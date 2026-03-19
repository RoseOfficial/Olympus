using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.PersephoneCore.Helpers;
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

namespace Olympus.Rotation.PersephoneCore.Context;

/// <summary>
/// Summoner-specific context implementation.
/// Provides all state needed for Persephone rotation modules.
/// </summary>
public sealed class PersephoneContext : IPersephoneContext
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

    #endregion

    #region ICasterDpsRotationContext Implementation

    public int CurrentMp { get; }
    public int MaxMp { get; }
    public float MpPercent { get; }

    public bool IsCasting { get; }
    public float CastRemaining { get; }
    public bool CanSlidecast { get; }

    public bool HasSwiftcast { get; }
    public bool HasTriplecast { get; } // Summoners don't have Triplecast
    public int TriplecastStacks { get; }
    public bool HasInstantCast { get; }

    #endregion

    #region IPersephoneContext Implementation

    // Demi-summon state
    public bool IsBahamutActive { get; }
    public bool IsPhoenixActive { get; }
    public bool IsSolarBahamutActive { get; }
    public bool IsDemiSummonActive { get; }
    public float DemiSummonTimer { get; }
    public int DemiSummonGcdsRemaining { get; }

    // Primal attunement state
    public int CurrentAttunement { get; }
    public int AttunementStacks { get; }
    public float AttunementTimer { get; }
    public bool IsIfritAttuned { get; }
    public bool IsTitanAttuned { get; }
    public bool IsGarudaAttuned { get; }

    // Primal availability
    public bool CanSummonIfrit { get; }
    public bool CanSummonTitan { get; }
    public bool CanSummonGaruda { get; }
    public int PrimalsAvailable { get; }

    // Aetherflow state
    public int AetherflowStacks { get; }
    public bool HasAetherflow { get; }

    // Buff state
    public bool HasFurtherRuin { get; }
    public float FurtherRuinRemaining { get; }
    public bool HasSearingLight { get; }
    public float SearingLightRemaining { get; }
    public bool HasIfritsFavor { get; }
    public bool HasTitansFavor { get; }
    public bool HasGarudasFavor { get; }
    public bool HasRadiantAegis { get; }

    // Cooldown state
    public bool SearingLightReady { get; }
    public bool EnergyDrainReady { get; }
    public bool EnkindleReady { get; }
    public bool AstralFlowReady { get; }
    public int RadiantAegisCharges { get; }
    public bool SwiftcastReady { get; }
    public bool LucidDreamingReady { get; }

    // Tracking state
    public bool HasUsedEnkindleThisPhase { get; private set; }
    public bool HasUsedAstralFlowThisPhase { get; private set; }

    /// <inheritdoc />
    public void MarkEnkindleUsed() => HasUsedEnkindleThisPhase = true;

    /// <inheritdoc />
    public void MarkAstralFlowUsed() => HasUsedAstralFlowThisPhase = true;
    public bool HasPetSummoned { get; }

    // Helpers
    public PersephoneStatusHelper StatusHelper { get; }
    public PersephonePartyHelper PartyHelper { get; }
    public PersephoneDebugState Debug { get; }

    // Party Coordination
    public IPartyCoordinationService? PartyCoordinationService { get; }

    // Training
    public ITrainingService? TrainingService { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    // GCD time for calculating GCDs remaining in phase
    private const float GcdTime = 2.5f;

    public PersephoneContext(
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
        PersephoneStatusHelper statusHelper,
        PersephonePartyHelper partyHelper,
        PersephoneDebugState debugState,
        int aetherflowStacks,
        int attunement,
        int attunementStacks,
        float attunementTimer,
        float summonTimer,
        bool ifritReady,
        bool titanReady,
        bool garudaReady,
        bool isBahamutActive,
        bool isPhoenixActive,
        bool isSolarBahamutActive,
        bool hasUsedEnkindleThisPhase,
        bool hasUsedAstralFlowThisPhase,
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

        // MP state
        CurrentMp = (int)player.CurrentMp;
        MaxMp = (int)player.MaxMp;
        MpPercent = MaxMp > 0 ? (float)CurrentMp / MaxMp : 0f;

        // Cast state
        IsCasting = player.IsCasting;
        CastRemaining = player.TotalCastTime - player.CurrentCastTime;
        CanSlidecast = IsCasting && CastRemaining <= 0.5f;

        // Summoners don't have Triplecast
        HasTriplecast = false;
        TriplecastStacks = 0;
        HasSwiftcast = BaseStatusHelper.HasSwiftcast(player);
        HasInstantCast = HasSwiftcast;

        // Demi-summon state
        DemiSummonTimer = summonTimer;
        IsBahamutActive = isBahamutActive && summonTimer > 0;
        IsPhoenixActive = isPhoenixActive && summonTimer > 0;
        IsSolarBahamutActive = isSolarBahamutActive && summonTimer > 0;
        IsDemiSummonActive = summonTimer > 0;
        DemiSummonGcdsRemaining = IsDemiSummonActive ? (int)(DemiSummonTimer / GcdTime) : 0;

        // Primal attunement state
        CurrentAttunement = attunement;
        AttunementStacks = attunementStacks;
        AttunementTimer = attunementTimer;
        IsIfritAttuned = attunement == 1 && attunementStacks > 0;
        IsTitanAttuned = attunement == 2 && attunementStacks > 0;
        IsGarudaAttuned = attunement == 3 && attunementStacks > 0;

        // Primal availability
        CanSummonIfrit = ifritReady;
        CanSummonTitan = titanReady;
        CanSummonGaruda = garudaReady;
        PrimalsAvailable = (ifritReady ? 1 : 0) + (titanReady ? 1 : 0) + (garudaReady ? 1 : 0);

        // Aetherflow state
        AetherflowStacks = aetherflowStacks;
        HasAetherflow = aetherflowStacks > 0;

        // Buff state
        HasFurtherRuin = statusHelper.HasFurtherRuin(player);
        FurtherRuinRemaining = statusHelper.GetFurtherRuinRemaining(player);
        HasSearingLight = statusHelper.HasSearingLight(player);
        SearingLightRemaining = statusHelper.GetSearingLightRemaining(player);
        HasIfritsFavor = statusHelper.HasIfritsFavor(player);
        HasTitansFavor = statusHelper.HasTitansFavor(player);
        HasGarudasFavor = statusHelper.HasGarudasFavor(player);
        HasRadiantAegis = statusHelper.HasRadiantAegis(player);

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        // Cooldown tracking
        var level = player.Level;
        SearingLightReady = level >= SMNActions.SearingLight.MinLevel &&
                           actionService.IsActionReady(SMNActions.SearingLight.ActionId);
        EnergyDrainReady = level >= SMNActions.EnergyDrain.MinLevel &&
                          actionService.IsActionReady(SMNActions.EnergyDrain.ActionId);

        // Enkindle readiness depends on which demi-summon is active
        EnkindleReady = false;
        if (IsBahamutActive && level >= SMNActions.EnkindleBahamut.MinLevel)
            EnkindleReady = actionService.IsActionReady(SMNActions.EnkindleBahamut.ActionId);
        else if (IsPhoenixActive && level >= SMNActions.EnkindlePhoenix.MinLevel)
            EnkindleReady = actionService.IsActionReady(SMNActions.EnkindlePhoenix.ActionId);
        else if (IsSolarBahamutActive && level >= SMNActions.EnkindleSolarBahamut.MinLevel)
            EnkindleReady = actionService.IsActionReady(SMNActions.EnkindleSolarBahamut.ActionId);

        // Astral Flow readiness depends on which demi-summon is active
        AstralFlowReady = false;
        if (IsBahamutActive && level >= SMNActions.Deathflare.MinLevel)
            AstralFlowReady = actionService.IsActionReady(SMNActions.Deathflare.ActionId);
        else if (IsPhoenixActive && level >= SMNActions.Rekindle.MinLevel)
            AstralFlowReady = actionService.IsActionReady(SMNActions.Rekindle.ActionId);
        else if (IsSolarBahamutActive && level >= SMNActions.Sunflare.MinLevel)
            AstralFlowReady = actionService.IsActionReady(SMNActions.Sunflare.ActionId);

        RadiantAegisCharges = level >= SMNActions.RadiantAegis.MinLevel
            ? (int)actionService.GetCurrentCharges(SMNActions.RadiantAegis.ActionId)
            : 0;

        SwiftcastReady = level >= RoleActions.Swiftcast.MinLevel &&
                        actionService.IsActionReady(RoleActions.Swiftcast.ActionId);
        LucidDreamingReady = level >= RoleActions.LucidDreaming.MinLevel &&
                            actionService.IsActionReady(RoleActions.LucidDreaming.ActionId);

        // Tracking state
        HasUsedEnkindleThisPhase = hasUsedEnkindleThisPhase;
        HasUsedAstralFlowThisPhase = hasUsedAstralFlowThisPhase;

        // Pet state - check ObjectTable for pets owned by this player
        // SubKind 2 = Pet/Companion in FFXIV
        HasPetSummoned = objectTable
            .OfType<IBattleNpc>()
            .Any(npc => npc.OwnerId == player.EntityId && npc.SubKind == 2);

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
        // Demi-summon state
        Debug.IsBahamutActive = IsBahamutActive;
        Debug.IsPhoenixActive = IsPhoenixActive;
        Debug.IsSolarBahamutActive = IsSolarBahamutActive;
        Debug.DemiSummonTimer = DemiSummonTimer;
        Debug.DemiSummonGcdsRemaining = DemiSummonGcdsRemaining;

        // Primal state
        Debug.CurrentAttunement = CurrentAttunement;
        Debug.AttunementStacks = AttunementStacks;
        Debug.AttunementTimer = AttunementTimer;
        Debug.CanSummonIfrit = CanSummonIfrit;
        Debug.CanSummonTitan = CanSummonTitan;
        Debug.CanSummonGaruda = CanSummonGaruda;
        Debug.PrimalsAvailable = PrimalsAvailable;

        // Resource state
        Debug.CurrentMp = CurrentMp;
        Debug.MaxMp = MaxMp;
        Debug.AetherflowStacks = AetherflowStacks;

        // Buff state
        Debug.HasFurtherRuin = HasFurtherRuin;
        Debug.FurtherRuinRemaining = FurtherRuinRemaining;
        Debug.HasSearingLight = HasSearingLight;
        Debug.SearingLightRemaining = SearingLightRemaining;
        Debug.HasIfritsFavor = HasIfritsFavor;
        Debug.HasTitansFavor = HasTitansFavor;
        Debug.HasGarudasFavor = HasGarudasFavor;
        Debug.HasSwiftcast = HasSwiftcast;

        // Cooldowns
        Debug.SearingLightReady = SearingLightReady;
        Debug.EnergyDrainReady = EnergyDrainReady;
        Debug.EnkindleReady = EnkindleReady;
        Debug.AstralFlowReady = AstralFlowReady;
        Debug.RadiantAegisCharges = RadiantAegisCharges;

        // Tracking
        Debug.HasUsedEnkindleThisPhase = HasUsedEnkindleThisPhase;
        Debug.HasUsedAstralFlowThisPhase = HasUsedAstralFlowThisPhase;

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";

        // Phase
        if (IsSolarBahamutActive)
            Debug.Phase = "Solar Bahamut";
        else if (IsBahamutActive)
            Debug.Phase = "Bahamut";
        else if (IsPhoenixActive)
            Debug.Phase = "Phoenix";
        else if (IsIfritAttuned)
            Debug.Phase = "Ifrit";
        else if (IsTitanAttuned)
            Debug.Phase = "Titan";
        else if (IsGarudaAttuned)
            Debug.Phase = "Garuda";
        else if (PrimalsAvailable > 0)
            Debug.Phase = "Summon Ready";
        else
            Debug.Phase = "Waiting";
    }
}
