using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Cooldown;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Rotation.ThemisCore.Helpers;
using Olympus.Timeline;

namespace Olympus.Rotation.ThemisCore.Context;

/// <summary>
/// Paladin-specific context implementation.
/// Provides all state needed for Themis rotation modules.
/// </summary>
public sealed class ThemisContext : IThemisContext
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
    public bool HasSwiftcast => false; // Tanks don't use Swiftcast

    #endregion

    #region ITankRotationContext Implementation

    public IEnmityService EnmityService { get; }
    public ITankCooldownService TankCooldownService { get; }
    public IPartyCoordinationService? PartyCoordinationService { get; }
    public bool IsMainTank { get; }
    public bool HasTankStance { get; }
    public int ComboStep { get; }
    public uint LastComboAction { get; }
    public float ComboTimeRemaining { get; }

    #endregion

    #region IThemisContext Implementation

    public int OathGauge { get; }
    public bool HasFightOrFlight { get; }
    public float FightOrFlightRemaining { get; }
    public bool HasRequiescat { get; }
    public int RequiescatStacks { get; }
    public bool HasSwordOath { get; }
    public int SwordOathStacks { get; }
    public int AtonementStep { get; private set; }
    public int ConfiteorStep { get; private set; }
    public bool HasBladeOfHonor { get; }
    public bool HasActiveMitigation { get; }
    public bool HasHallowedGround { get; }
    public float GoringBladeRemaining { get; }

    public ThemisStatusHelper StatusHelper { get; }
    public ThemisPartyHelper PartyHelper { get; }
    public ThemisDebugState Debug { get; }
    public ITrainingService? TrainingService { get; }

    public IBattleChara? CurrentTarget { get; private set; }

    #endregion

    public ThemisContext(
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
        IEnmityService enmityService,
        ITankCooldownService tankCooldownService,
        ThemisStatusHelper statusHelper,
        ThemisPartyHelper partyHelper,
        ThemisDebugState debugState,
        int oathGauge,
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
        ObjectTable = objectTable;
        PartyList = partyList;
        Log = log;

        EnmityService = enmityService;
        TankCooldownService = tankCooldownService;
        PartyCoordinationService = partyCoordinationService;
        TrainingService = trainingService;
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState;

        OathGauge = oathGauge;
        ComboStep = comboStep;
        LastComboAction = lastComboAction;
        ComboTimeRemaining = comboTimeRemaining;

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target using game API range check for accuracy
        CurrentTarget = targetingService.FindEnemyForAction(
            configuration.Targeting.EnemyStrategy,
            PLDActions.FastBlade.ActionId,
            player);

        // Check main tank status
        IsMainTank = configuration.Tank.IsMainTankOverride
            ?? (CurrentTarget != null && enmityService.IsMainTankOn(CurrentTarget, player.EntityId));

        // Status checks
        HasTankStance = statusHelper.HasIronWill(player);
        HasFightOrFlight = statusHelper.HasFightOrFlight(player);
        FightOrFlightRemaining = statusHelper.GetFightOrFlightRemaining(player);
        HasRequiescat = statusHelper.HasRequiescat(player);
        RequiescatStacks = statusHelper.GetRequiescatStacks(player);
        HasSwordOath = statusHelper.HasSwordOath(player);
        SwordOathStacks = statusHelper.GetSwordOathStacks(player);
        HasActiveMitigation = statusHelper.HasActiveMitigation(player);
        HasHallowedGround = statusHelper.HasHallowedGround(player);

        // DoT tracking
        GoringBladeRemaining = statusHelper.GetGoringBladeRemaining(CurrentTarget, player.EntityId);

        // Blade of Honor is only usable when the buff is active after completing Blade of Valor (Lv.100)
        var level = player.Level;
        HasBladeOfHonor = level >= PLDActions.BladeOfHonor.MinLevel &&
                         actionService.IsActionReady(PLDActions.BladeOfHonor.ActionId);

        // Determine Atonement chain position based on Sword Oath stacks
        // Sword Oath starts at 3, each Atonement reduces it
        AtonementStep = HasSwordOath ? (4 - SwordOathStacks) : 0;

        // Determine Confiteor chain position by checking which action is ready
        // The game enables each action in sequence: Confiteor → Faith → Truth → Valor
        if (level >= PLDActions.BladeOfValor.MinLevel &&
            actionService.IsActionReady(PLDActions.BladeOfValor.ActionId))
            ConfiteorStep = 3;  // Blade of Valor ready
        else if (level >= PLDActions.BladeOfTruth.MinLevel &&
                 actionService.IsActionReady(PLDActions.BladeOfTruth.ActionId))
            ConfiteorStep = 2;  // Blade of Truth ready
        else if (level >= PLDActions.BladeOfFaith.MinLevel &&
                 actionService.IsActionReady(PLDActions.BladeOfFaith.ActionId))
            ConfiteorStep = 1;  // Blade of Faith ready
        else if (HasRequiescat && level >= PLDActions.Confiteor.MinLevel)
            ConfiteorStep = 0;  // Confiteor ready (at start of chain)
        else
            ConfiteorStep = -1; // Not in chain

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
        // Execution flow debug info (critical for diagnosing issues)
        Debug.InCombat = InCombat;
        Debug.CanExecuteGcd = CanExecuteGcd;
        Debug.CanExecuteOgcd = CanExecuteOgcd;
        Debug.GcdState = ActionService.CurrentGcdState.ToString();
        Debug.GcdRemaining = ActionService.GcdRemaining;

        Debug.ComboStep = ComboStep;
        Debug.ComboTimeRemaining = ComboTimeRemaining;
        Debug.OathGauge = OathGauge;
        Debug.HasFightOrFlight = HasFightOrFlight;
        Debug.FightOrFlightRemaining = FightOrFlightRemaining;
        Debug.HasRequiescat = HasRequiescat;
        Debug.RequiescatStacks = RequiescatStacks;
        Debug.SwordOathStacks = SwordOathStacks;
        Debug.AtonementStep = AtonementStep;
        Debug.ConfiteorStep = ConfiteorStep;
        Debug.GoringBladeRemaining = GoringBladeRemaining;
        Debug.HasActiveMitigation = HasActiveMitigation;
        Debug.ActiveMitigations = StatusHelper.GetActiveMitigations(Player);
        Debug.IsMainTank = IsMainTank;
        Debug.CurrentTarget = CurrentTarget?.Name?.TextValue ?? "None";
    }
}
