using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.CirceCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Debuff;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Timeline;

namespace Olympus.Rotation.CirceCore.Context;

/// <summary>
/// Red Mage-specific context implementation.
/// Provides all state needed for Circe rotation modules.
/// </summary>
public sealed class CirceContext : ICirceContext
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

    #endregion

    #region ICasterDpsRotationContext Implementation

    public int CurrentMp { get; }
    public int MaxMp { get; }
    public float MpPercent { get; }

    public bool IsCasting { get; }
    public float CastRemaining { get; }
    public bool CanSlidecast { get; }

    public bool HasSwiftcast { get; }
    public bool HasTriplecast { get; } // RDM doesn't have Triplecast
    public int TriplecastStacks { get; }
    public bool HasInstantCast { get; }

    #endregion

    #region ICirceContext Implementation

    // Mana state
    public int BlackMana { get; }
    public int WhiteMana { get; }
    public int ManaImbalance { get; }
    public int AbsoluteManaImbalance { get; }
    public bool IsManaBalanced { get; }
    public int ManaStacks { get; }
    public bool CanStartMeleeCombo { get; }
    public int LowerMana { get; }

    // Dualcast state
    public bool HasDualcast { get; }
    public float DualcastRemaining { get; }
    public bool ShouldHardcast { get; }

    // Proc state
    public bool HasVerfire { get; }
    public float VerfireRemaining { get; }
    public bool HasVerstone { get; }
    public float VerstoneRemaining { get; }
    public bool HasAnyProc { get; }
    public bool HasBothProcs { get; }

    // Melee combo state
    public bool IsInMeleeCombo { get; }
    public int MeleeComboStep { get; }
    public bool IsFinisherReady { get; }
    public bool IsScorchReady { get; }
    public bool IsResolutionReady { get; }
    public bool IsGrandImpactReady { get; }

    // Buff state
    public bool HasEmbolden { get; }
    public float EmboldenRemaining { get; }
    public bool HasManafication { get; }
    public float ManaficationRemaining { get; }
    public bool HasAcceleration { get; }
    public float AccelerationRemaining { get; }
    public bool HasThornedFlourish { get; }
    public bool HasPrefulgenceReady { get; }
    public bool HasMagickBarrier { get; }

    // Cooldown state
    public bool FlecheReady { get; }
    public bool ContreSixteReady { get; }
    public bool EmboldenReady { get; }
    public bool ManaficationReady { get; }
    public int CorpsACorpsCharges { get; }
    public int EngagementCharges { get; }
    public int AccelerationCharges { get; }
    public bool SwiftcastReady { get; }
    public bool LucidDreamingReady { get; }

    // Helpers
    public CirceStatusHelper StatusHelper { get; }
    public CircePartyHelper PartyHelper { get; }
    public CirceDebugState Debug { get; }

    // Party Coordination
    public IPartyCoordinationService? PartyCoordinationService { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public CirceContext(
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
        CirceStatusHelper statusHelper,
        CircePartyHelper partyHelper,
        CirceDebugState debugState,
        int blackMana,
        int whiteMana,
        int manaStacks,
        uint comboAction,
        float comboTimer,
        ITimelineService? timelineService = null,
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
        PartyCoordinationService = partyCoordinationService;
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

        // RDM doesn't have Triplecast
        HasTriplecast = false;
        TriplecastStacks = 0;

        // Mana state (from gauge)
        BlackMana = blackMana;
        WhiteMana = whiteMana;
        ManaImbalance = blackMana - whiteMana;
        AbsoluteManaImbalance = Math.Abs(ManaImbalance);
        IsManaBalanced = AbsoluteManaImbalance < 30;
        ManaStacks = manaStacks;
        CanStartMeleeCombo = blackMana >= 50 && whiteMana >= 50;
        LowerMana = Math.Min(blackMana, whiteMana);

        // Dualcast state
        HasDualcast = statusHelper.HasDualcast(player);
        DualcastRemaining = statusHelper.GetDualcastRemaining(player);

        // Buff state
        HasSwiftcast = statusHelper.HasSwiftcast(player);
        HasAcceleration = statusHelper.HasAcceleration(player);
        AccelerationRemaining = statusHelper.GetAccelerationRemaining(player);

        // Instant cast check
        HasInstantCast = HasDualcast || HasSwiftcast || HasAcceleration;
        ShouldHardcast = !HasInstantCast;

        // Proc state
        HasVerfire = statusHelper.HasVerfireReady(player);
        VerfireRemaining = statusHelper.GetVerfireRemaining(player);
        HasVerstone = statusHelper.HasVerstoneReady(player);
        VerstoneRemaining = statusHelper.GetVerstoneRemaining(player);
        HasAnyProc = HasVerfire || HasVerstone;
        HasBothProcs = HasVerfire && HasVerstone;

        // Other buff state
        HasEmbolden = statusHelper.HasEmbolden(player);
        EmboldenRemaining = statusHelper.GetEmboldenRemaining(player);
        HasManafication = statusHelper.HasManafication(player);
        ManaficationRemaining = statusHelper.GetManaficationRemaining(player);
        HasThornedFlourish = statusHelper.HasThornedFlourish(player);
        HasPrefulgenceReady = statusHelper.HasPrefulgenceReady(player);
        IsGrandImpactReady = statusHelper.HasGrandImpactReady(player);
        HasMagickBarrier = statusHelper.HasMagickBarrier(player);

        // Melee combo state - determined by combo action and mana stacks
        DetermineComboState(comboAction, comboTimer, player.Level, out var inCombo, out var comboStep,
            out var finisherReady, out var scorchReady, out var resolutionReady);
        IsInMeleeCombo = inCombo;
        MeleeComboStep = comboStep;
        IsFinisherReady = finisherReady;
        IsScorchReady = scorchReady;
        IsResolutionReady = resolutionReady;

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        // Cooldown tracking
        var level = player.Level;
        FlecheReady = level >= RDMActions.Fleche.MinLevel &&
                      actionService.IsActionReady(RDMActions.Fleche.ActionId);
        ContreSixteReady = level >= RDMActions.ContreSixte.MinLevel &&
                          actionService.IsActionReady(RDMActions.ContreSixte.ActionId);
        EmboldenReady = level >= RDMActions.Embolden.MinLevel &&
                       actionService.IsActionReady(RDMActions.Embolden.ActionId);
        ManaficationReady = level >= RDMActions.Manafication.MinLevel &&
                          actionService.IsActionReady(RDMActions.Manafication.ActionId);

        CorpsACorpsCharges = level >= RDMActions.CorpsACorps.MinLevel
            ? (int)actionService.GetCurrentCharges(RDMActions.CorpsACorps.ActionId)
            : 0;
        EngagementCharges = level >= RDMActions.Engagement.MinLevel
            ? (int)actionService.GetCurrentCharges(RDMActions.Engagement.ActionId)
            : 0;
        AccelerationCharges = level >= RDMActions.Acceleration.MinLevel
            ? (int)actionService.GetCurrentCharges(RDMActions.Acceleration.ActionId)
            : 0;

        SwiftcastReady = level >= RDMActions.Swiftcast.MinLevel &&
                        actionService.IsActionReady(RDMActions.Swiftcast.ActionId);
        LucidDreamingReady = level >= RDMActions.LucidDreaming.MinLevel &&
                            actionService.IsActionReady(RDMActions.LucidDreaming.ActionId);

        // Update debug state
        UpdateDebugState();
    }

    private void DetermineComboState(uint comboAction, float comboTimer, byte level,
        out bool inCombo, out int comboStep, out bool finisherReady, out bool scorchReady, out bool resolutionReady)
    {
        inCombo = false;
        comboStep = 0;
        finisherReady = false;
        scorchReady = false;
        resolutionReady = false;

        // No combo if timer expired
        if (comboTimer <= 0)
            return;

        // Check based on last combo action
        // Enchanted Riposte (7527) -> Zwerchhau ready
        if (comboAction == RDMActions.EnchantedRiposte.ActionId)
        {
            inCombo = true;
            comboStep = 1; // Zwerchhau is next
        }
        // Enchanted Zwerchhau (7528) -> Redoublement ready
        else if (comboAction == RDMActions.EnchantedZwerchhau.ActionId)
        {
            inCombo = true;
            comboStep = 2; // Redoublement is next
        }
        // Enchanted Redoublement (7529) -> Finisher ready
        else if (comboAction == RDMActions.EnchantedRedoublement.ActionId)
        {
            inCombo = true;
            comboStep = 3;
            finisherReady = level >= RDMActions.Verflare.MinLevel;
        }
        // Verflare (7525) or Verholy (7526) -> Scorch ready
        else if (comboAction == RDMActions.Verflare.ActionId || comboAction == RDMActions.Verholy.ActionId)
        {
            inCombo = true;
            comboStep = 4;
            scorchReady = level >= RDMActions.Scorch.MinLevel;
        }
        // Scorch (16530) -> Resolution ready
        else if (comboAction == RDMActions.Scorch.ActionId)
        {
            inCombo = true;
            comboStep = 5;
            resolutionReady = level >= RDMActions.Resolution.MinLevel;
        }
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
        // Mana state
        Debug.BlackMana = BlackMana;
        Debug.WhiteMana = WhiteMana;
        Debug.ManaImbalance = ManaImbalance;
        Debug.ManaStacks = ManaStacks;
        Debug.CanStartMeleeCombo = CanStartMeleeCombo;

        // Dualcast state
        Debug.HasDualcast = HasDualcast;
        Debug.DualcastRemaining = DualcastRemaining;

        // Proc state
        Debug.HasVerfire = HasVerfire;
        Debug.VerfireRemaining = VerfireRemaining;
        Debug.HasVerstone = HasVerstone;
        Debug.VerstoneRemaining = VerstoneRemaining;

        // Melee combo state
        Debug.IsInMeleeCombo = IsInMeleeCombo;
        Debug.MeleeComboStep = MeleeComboStep switch
        {
            0 => "None",
            1 => "Zwerchhau",
            2 => "Redoublement",
            3 => "Finisher",
            4 => "Scorch",
            5 => "Resolution",
            _ => "Unknown"
        };
        Debug.IsFinisherReady = IsFinisherReady;
        Debug.IsScorchReady = IsScorchReady;
        Debug.IsResolutionReady = IsResolutionReady;

        // Buff state
        Debug.HasEmbolden = HasEmbolden;
        Debug.EmboldenRemaining = EmboldenRemaining;
        Debug.HasManafication = HasManafication;
        Debug.ManaficationRemaining = ManaficationRemaining;
        Debug.HasAcceleration = HasAcceleration;
        Debug.AccelerationRemaining = AccelerationRemaining;
        Debug.HasSwiftcast = HasSwiftcast;

        // Special abilities
        Debug.HasThornedFlourish = HasThornedFlourish;
        Debug.HasGrandImpactReady = IsGrandImpactReady;
        Debug.HasPrefulgenceReady = HasPrefulgenceReady;

        // Cooldowns
        Debug.FlecheReady = FlecheReady;
        Debug.ContreSixteReady = ContreSixteReady;
        Debug.EmboldenReady = EmboldenReady;
        Debug.ManaficationReady = ManaficationReady;
        Debug.CorpsACorpsCharges = CorpsACorpsCharges;
        Debug.EngagementCharges = EngagementCharges;
        Debug.AccelerationCharges = AccelerationCharges;

        // Resources
        Debug.CurrentMp = CurrentMp;
        Debug.MaxMp = MaxMp;

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";

        // Phase
        if (IsResolutionReady)
            Debug.Phase = "Resolution";
        else if (IsScorchReady)
            Debug.Phase = "Scorch";
        else if (IsFinisherReady)
            Debug.Phase = "Finisher";
        else if (IsInMeleeCombo)
            Debug.Phase = $"Melee ({Debug.MeleeComboStep})";
        else if (CanStartMeleeCombo)
            Debug.Phase = "Combo Ready";
        else if (HasDualcast)
            Debug.Phase = "Dualcast";
        else
            Debug.Phase = "Building Mana";
    }
}
