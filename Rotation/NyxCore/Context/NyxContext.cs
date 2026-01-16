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
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Rotation.NyxCore.Helpers;

namespace Olympus.Rotation.NyxCore.Context;

/// <summary>
/// Dark Knight-specific context implementation.
/// Provides all state needed for Nyx rotation modules.
/// </summary>
public sealed class NyxContext : INyxContext
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

    public IObjectTable ObjectTable { get; }
    public IPartyList PartyList { get; }
    public IPluginLog? Log { get; }

    public (float avgHpPercent, float lowestHpPercent, int injuredCount) PartyHealthMetrics { get; }
    public bool HasSwiftcast => false; // Tanks don't use Swiftcast

    #endregion

    #region ITankRotationContext Implementation

    public IEnmityService EnmityService { get; }
    public ITankCooldownService TankCooldownService { get; }
    public bool IsMainTank { get; }
    public bool HasTankStance { get; }
    public int ComboStep { get; }
    public uint LastComboAction { get; }
    public float ComboTimeRemaining { get; }

    #endregion

    #region INyxContext Implementation

    // Gauge and resources
    public int BloodGauge { get; }
    public int CurrentMp { get; }
    public int MaxMp { get; }
    public bool HasEnoughMpForTbn => CurrentMp >= DRKActions.TbnMpCost;
    public bool HasEnoughMpForEdge => CurrentMp >= DRKActions.EdgeFloodMpCost;

    // Buff state
    public bool HasGrit { get; }
    public bool HasDarkside { get; }
    public float DarksideRemaining { get; }
    public bool HasBloodWeapon { get; }
    public float BloodWeaponRemaining { get; }
    public bool HasDelirium { get; }
    public int DeliriumStacks { get; }
    public bool HasDarkArts { get; }
    public bool HasScornfulEdge { get; }

    // Defensive state
    public bool HasActiveMitigation { get; }
    public bool HasLivingDead { get; }
    public bool HasWalkingDead { get; }
    public bool HasShadowWall { get; }
    public bool HasDarkMind { get; }
    public bool HasTheBlackestNight { get; }
    public bool HasOblation { get; }

    // Living Shadow
    public bool HasLivingShadow { get; }

    // Ground DoT
    public bool HasSaltedEarth { get; }

    // Helpers
    public NyxStatusHelper StatusHelper { get; }
    public NyxPartyHelper PartyHelper { get; }
    public NyxDebugState Debug { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;
    private readonly float _darksideTimer;

    public NyxContext(
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
        IEnmityService enmityService,
        ITankCooldownService tankCooldownService,
        NyxStatusHelper statusHelper,
        NyxPartyHelper partyHelper,
        NyxDebugState debugState,
        int bloodGauge,
        float darksideTimer,
        int comboStep,
        uint lastComboAction,
        float comboTimeRemaining,
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
        ObjectTable = objectTable;
        PartyList = partyList;
        Log = log;

        EnmityService = enmityService;
        TankCooldownService = tankCooldownService;
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState;

        BloodGauge = bloodGauge;
        _darksideTimer = darksideTimer;
        ComboStep = comboStep;
        LastComboAction = lastComboAction;
        ComboTimeRemaining = comboTimeRemaining;

        // MP resources
        CurrentMp = (int)player.CurrentMp;
        MaxMp = (int)player.MaxMp;

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Get current target
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            3f,
            player);

        // Check main tank status
        IsMainTank = _currentTarget != null && enmityService.IsMainTankOn(_currentTarget, player.EntityId);

        // Tank stance
        HasGrit = statusHelper.HasGrit(player);
        HasTankStance = HasGrit;

        // Darkside (critical buff)
        HasDarkside = darksideTimer > 0;
        DarksideRemaining = darksideTimer;

        // Damage buff checks
        HasBloodWeapon = statusHelper.HasBloodWeapon(player);
        BloodWeaponRemaining = statusHelper.GetBloodWeaponRemaining(player);
        HasDelirium = statusHelper.HasDelirium(player);
        DeliriumStacks = statusHelper.GetDeliriumStacks(player);
        HasDarkArts = statusHelper.HasDarkArts(player);
        HasScornfulEdge = statusHelper.HasScornfulEdge(player);

        // Defensive checks
        HasActiveMitigation = statusHelper.HasActiveMitigation(player);
        HasLivingDead = statusHelper.HasLivingDead(player);
        HasWalkingDead = statusHelper.HasWalkingDead(player);
        HasShadowWall = statusHelper.HasShadowWall(player);
        HasDarkMind = statusHelper.HasDarkMind(player);
        HasTheBlackestNight = statusHelper.HasTheBlackestNight(player);
        HasOblation = statusHelper.HasOblation(player);

        // Living Shadow
        HasLivingShadow = statusHelper.HasLivingShadow(player);

        // Ground DoT
        HasSaltedEarth = statusHelper.HasSaltedEarth(player);

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
        // Combo tracking
        Debug.ComboStep = ComboStep;
        Debug.ComboTimeRemaining = ComboTimeRemaining;

        // Resources
        Debug.BloodGauge = BloodGauge;
        Debug.CurrentMp = CurrentMp;
        Debug.MaxMp = MaxMp;

        // Darkside (critical)
        Debug.HasDarkside = HasDarkside;
        Debug.DarksideRemaining = DarksideRemaining;

        // Dark Arts
        Debug.HasDarkArts = HasDarkArts;

        // Tank stance
        Debug.HasGrit = HasGrit;

        // Buffs
        Debug.HasBloodWeapon = HasBloodWeapon;
        Debug.BloodWeaponRemaining = BloodWeaponRemaining;
        Debug.HasDelirium = HasDelirium;
        Debug.DeliriumStacks = DeliriumStacks;
        Debug.HasScornfulEdge = HasScornfulEdge;
        Debug.HasLivingShadow = HasLivingShadow;

        // Defensives
        Debug.HasActiveMitigation = HasActiveMitigation;
        Debug.ActiveMitigations = StatusHelper.GetActiveMitigations(Player);
        Debug.HasLivingDead = HasLivingDead;
        Debug.HasWalkingDead = HasWalkingDead;
        Debug.HasTheBlackestNight = HasTheBlackestNight;
        Debug.HasShadowWall = HasShadowWall;
        Debug.HasDarkMind = HasDarkMind;
        Debug.HasOblation = HasOblation;

        // Ground DoT
        Debug.HasSaltedEarth = HasSaltedEarth;

        // Enmity
        Debug.IsMainTank = IsMainTank;
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
    }
}
