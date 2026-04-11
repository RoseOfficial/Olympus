using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.PrometheusCore.Helpers;
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

namespace Olympus.Rotation.PrometheusCore.Context;

/// <summary>
/// Machinist-specific context implementation.
/// Provides all state needed for Prometheus rotation modules.
/// </summary>
public sealed class PrometheusContext : IPrometheusContext
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
    public bool HasSwiftcast => false; // Ranged physical DPS don't use Swiftcast

    #endregion

    #region IRangedDpsRotationContext Implementation

    public int ComboStep { get; }
    public uint LastComboAction { get; }
    public float ComboTimeRemaining { get; }

    #endregion

    #region IPrometheusContext Implementation

    // Gauge state
    public int Heat { get; }
    public int Battery { get; }
    public bool IsOverheated { get; }
    public float OverheatRemaining { get; }
    public bool IsQueenActive { get; }
    public float QueenRemaining { get; }
    public int LastQueenBattery { get; }

    // Buff state
    public bool HasReassemble { get; }
    public float ReassembleRemaining { get; }
    public bool HasHypercharged { get; }
    public bool HasFullMetalMachinist { get; }
    public bool HasExcavatorReady { get; }

    // Target state
    public bool HasWildfire { get; }
    public float WildfireRemaining { get; }
    public bool HasBioblaster { get; }
    public float BioblasterRemaining { get; }

    // Cooldown tracking
    public int DrillCharges { get; }
    public int ReassembleCharges { get; }
    public int GaussRoundCharges { get; }
    public int RicochetCharges { get; }

    // Helpers
    public PrometheusStatusHelper StatusHelper { get; }
    public RangedDpsPartyHelper PartyHelper { get; }
    public PrometheusDebugState Debug { get; }

    // Party Coordination
    public IPartyCoordinationService? PartyCoordinationService { get; }

    // Training
    public ITrainingService? TrainingService { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public PrometheusContext(
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
        PrometheusStatusHelper statusHelper,
        RangedDpsPartyHelper partyHelper,
        PrometheusDebugState debugState,
        int heat,
        int battery,
        float overheatRemaining,
        float queenRemaining,
        int lastQueenBattery,
        int comboStep,
        uint lastComboAction,
        float comboTimeRemaining,
        ITimelineService? timelineService = null,
        IPluginLog? log = null,
        IPartyCoordinationService? partyCoordinationService = null,
        ITrainingService? trainingService = null)
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
        PartyCoordinationService = partyCoordinationService;
        TrainingService = trainingService;

        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState;

        // Combo state
        ComboStep = comboStep;
        LastComboAction = lastComboAction;
        ComboTimeRemaining = comboTimeRemaining;

        // Gauge state
        Heat = heat;
        Battery = battery;
        OverheatRemaining = overheatRemaining;
        IsOverheated = overheatRemaining > 0;
        QueenRemaining = queenRemaining;
        IsQueenActive = queenRemaining > 0;
        LastQueenBattery = lastQueenBattery;

        // Buff state
        HasReassemble = statusHelper.HasReassemble(player);
        ReassembleRemaining = statusHelper.GetReassembleRemaining(player);
        HasHypercharged = statusHelper.HasHypercharged(player);
        HasFullMetalMachinist = statusHelper.HasFullMetalMachinist(player);
        HasExcavatorReady = statusHelper.HasExcavatorReady(player);

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target for debuff tracking
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);

        // Target state
        if (_currentTarget != null)
        {
            HasWildfire = statusHelper.HasWildfire(_currentTarget, player.EntityId);
            WildfireRemaining = statusHelper.GetWildfireRemaining(_currentTarget, player.EntityId);
            HasBioblaster = statusHelper.HasBioblaster(_currentTarget, player.EntityId);
            BioblasterRemaining = statusHelper.GetBioblasterRemaining(_currentTarget, player.EntityId);
        }
        else
        {
            HasWildfire = false;
            WildfireRemaining = 0f;
            HasBioblaster = false;
            BioblasterRemaining = 0f;
        }

        // Cooldown tracking - use level-appropriate action IDs for replaced actions
        DrillCharges = GetActionCharges(MCHActions.Drill.ActionId);
        ReassembleCharges = GetActionCharges(MCHActions.Reassemble.ActionId);
        GaussRoundCharges = GetActionCharges(MCHActions.GetGaussRound(player.Level).ActionId);
        RicochetCharges = GetActionCharges(MCHActions.GetRicochet(player.Level).ActionId);

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
        Debug.Heat = Heat;
        Debug.Battery = Battery;
        Debug.IsOverheated = IsOverheated;
        Debug.OverheatRemaining = OverheatRemaining;
        Debug.IsQueenActive = IsQueenActive;
        Debug.QueenRemaining = QueenRemaining;
        Debug.LastQueenBattery = LastQueenBattery;

        // Buffs
        Debug.HasReassemble = HasReassemble;
        Debug.HasHypercharged = HasHypercharged;
        Debug.HasFullMetalMachinist = HasFullMetalMachinist;
        Debug.HasExcavatorReady = HasExcavatorReady;

        // Target
        Debug.HasWildfire = HasWildfire;
        Debug.WildfireRemaining = WildfireRemaining;
        Debug.HasBioblaster = HasBioblaster;
        Debug.BioblasterRemaining = BioblasterRemaining;

        // Cooldowns
        Debug.DrillCharges = DrillCharges;
        Debug.ReassembleCharges = ReassembleCharges;
        Debug.GaussRoundCharges = GaussRoundCharges;
        Debug.RicochetCharges = RicochetCharges;

        // Combo
        Debug.ComboStep = ComboStep;

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
    }
}
