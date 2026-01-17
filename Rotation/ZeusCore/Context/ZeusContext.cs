using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.ZeusCore.Helpers;
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

namespace Olympus.Rotation.ZeusCore.Context;

/// <summary>
/// Dragoon-specific context implementation.
/// Provides all state needed for Zeus rotation modules.
/// </summary>
public sealed class ZeusContext : IZeusContext
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

    #region IZeusContext Implementation

    // Gauge state
    public int FirstmindsFocus { get; }
    public int EyeCount { get; }
    public bool IsLifeOfDragonActive { get; }
    public float LifeOfDragonRemaining { get; }

    // Combo state
    public bool IsInVorpalCombo { get; }
    public bool IsInDisembowelCombo { get; }
    public bool IsInAoeCombo { get; }

    // Buffs
    public bool HasPowerSurge { get; }
    public float PowerSurgeRemaining { get; }
    public bool HasLanceCharge { get; }
    public float LanceChargeRemaining { get; }
    public bool HasLifeSurge { get; }
    public bool HasBattleLitany { get; }
    public float BattleLitanyRemaining { get; }
    public bool HasRightEye { get; }

    // Procs
    public bool HasDiveReady { get; }
    public bool HasFangAndClawBared { get; }
    public bool HasWheelInMotion { get; }
    public bool HasDraconianFire { get; }
    public bool HasNastrondReady { get; }
    public bool HasStardiverReady { get; }
    public bool HasStarcrossReady { get; }

    // DoT
    public bool HasDotOnTarget { get; }
    public float DotRemaining { get; }

    // Helpers
    public ZeusStatusHelper StatusHelper { get; }
    public ZeusPartyHelper PartyHelper { get; }
    public ZeusDebugState Debug { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public ZeusContext(
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
        ZeusStatusHelper statusHelper,
        ZeusPartyHelper partyHelper,
        ZeusDebugState debugState,
        int firstmindsFocus,
        int eyeCount,
        bool isLifeOfDragonActive,
        float lifeOfDragonRemaining,
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

        // Gauge state
        FirstmindsFocus = firstmindsFocus;
        EyeCount = eyeCount;
        IsLifeOfDragonActive = isLifeOfDragonActive;
        LifeOfDragonRemaining = lifeOfDragonRemaining;

        // Determine combo state from last action
        IsInVorpalCombo = lastComboAction == DRGActions.TrueThrust.ActionId && comboStep == 1;
        IsInDisembowelCombo = lastComboAction == DRGActions.Disembowel.ActionId ||
                              (lastComboAction == DRGActions.TrueThrust.ActionId && comboStep == 1);
        IsInAoeCombo = lastComboAction == DRGActions.DoomSpike.ActionId ||
                       lastComboAction == DRGActions.SonicThrust.ActionId;

        // Buff state
        HasPowerSurge = statusHelper.HasPowerSurge(player);
        PowerSurgeRemaining = statusHelper.GetPowerSurgeRemaining(player);
        HasLanceCharge = statusHelper.HasLanceCharge(player);
        LanceChargeRemaining = statusHelper.GetLanceChargeRemaining(player);
        HasLifeSurge = statusHelper.HasLifeSurge(player);
        HasBattleLitany = statusHelper.HasBattleLitany(player);
        BattleLitanyRemaining = statusHelper.GetBattleLitanyRemaining(player);
        HasRightEye = statusHelper.HasRightEye(player);

        // Proc state
        HasDiveReady = statusHelper.HasDiveReady(player);
        HasFangAndClawBared = statusHelper.HasFangAndClawBared(player);
        HasWheelInMotion = statusHelper.HasWheelInMotion(player);
        HasDraconianFire = statusHelper.HasDraconianFire(player);
        HasNastrondReady = statusHelper.HasNastrondReady(player);
        HasStardiverReady = statusHelper.HasStardiverReady(player);
        HasStarcrossReady = statusHelper.HasStarcrossReady(player);

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
            HasDotOnTarget = statusHelper.HasDot(_currentTarget, player.EntityId);
            DotRemaining = statusHelper.GetDotRemaining(_currentTarget, player.EntityId);
        }
        else
        {
            HasDotOnTarget = false;
            DotRemaining = 0f;
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
        Debug.FirstmindsFocus = FirstmindsFocus;
        Debug.EyeCount = EyeCount;
        Debug.IsLifeOfDragonActive = IsLifeOfDragonActive;
        Debug.LifeOfDragonRemaining = LifeOfDragonRemaining;

        // Combo
        Debug.ComboStep = ComboStep;
        Debug.LastComboAction = LastComboAction;
        Debug.ComboTimeRemaining = ComboTimeRemaining;
        Debug.ComboState = ZeusDebugState.FormatComboState(LastComboAction, ComboStep);

        // Buffs
        Debug.HasPowerSurge = HasPowerSurge;
        Debug.PowerSurgeRemaining = PowerSurgeRemaining;
        Debug.HasLanceCharge = HasLanceCharge;
        Debug.LanceChargeRemaining = LanceChargeRemaining;
        Debug.HasLifeSurge = HasLifeSurge;
        Debug.HasBattleLitany = HasBattleLitany;
        Debug.BattleLitanyRemaining = BattleLitanyRemaining;
        Debug.HasRightEye = HasRightEye;

        // Procs
        Debug.HasDiveReady = HasDiveReady;
        Debug.HasFangAndClawBared = HasFangAndClawBared;
        Debug.HasWheelInMotion = HasWheelInMotion;
        Debug.HasDraconianFire = HasDraconianFire;
        Debug.HasNastrondReady = HasNastrondReady;
        Debug.HasStardiverReady = HasStardiverReady;
        Debug.HasStarcrossReady = HasStarcrossReady;

        // DoT
        Debug.HasDotOnTarget = HasDotOnTarget;
        Debug.DotRemaining = DotRemaining;

        // Positional
        Debug.IsAtRear = IsAtRear;
        Debug.IsAtFlank = IsAtFlank;
        Debug.HasTrueNorth = HasTrueNorth;
        Debug.TargetHasPositionalImmunity = TargetHasPositionalImmunity;

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
    }
}
