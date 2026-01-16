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
using Olympus.Rotation.HephaestusCore.Helpers;
using Olympus.Timeline;

namespace Olympus.Rotation.HephaestusCore.Context;

/// <summary>
/// Gunbreaker-specific context implementation.
/// Provides all state needed for Hephaestus rotation modules.
/// </summary>
public sealed class HephaestusContext : IHephaestusContext
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

    #region IHephaestusContext Implementation

    // Cartridge gauge
    public int Cartridges { get; }
    public bool HasMaxCartridges => Cartridges >= GNBActions.MaxCartridges;
    public bool CanUseGnashingFang => Cartridges >= GNBActions.GnashingFangCost;
    public bool CanUseDoubleDown => Cartridges >= GNBActions.DoubleDownCost;

    // Continuation ready states
    public bool IsReadyToRip { get; }
    public bool IsReadyToTear { get; }
    public bool IsReadyToGouge { get; }
    public bool IsReadyToBlast { get; }
    public bool IsReadyToReign { get; }
    public bool HasAnyContinuationReady => IsReadyToRip || IsReadyToTear || IsReadyToGouge || IsReadyToBlast;

    // Gnashing Fang combo state
    public int GnashingFangStep { get; }
    public bool IsInGnashingFangCombo => GnashingFangStep > 0 && GnashingFangStep < 3;

    // Buff state
    public bool HasRoyalGuard { get; }
    public bool HasNoMercy { get; }
    public float NoMercyRemaining { get; }

    // Defensive state
    public bool HasActiveMitigation { get; }
    public bool HasSuperbolide { get; }
    public bool HasNebula { get; }
    public bool HasHeartOfCorundum { get; }
    public bool HasCamouflage { get; }
    public bool HasAurora { get; }

    // DoT state
    public bool HasSonicBreakDot { get; }
    public bool HasBowShockDot { get; }

    // Helpers
    public HephaestusStatusHelper StatusHelper { get; }
    public HephaestusPartyHelper PartyHelper { get; }
    public HephaestusDebugState Debug { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public HephaestusContext(
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
        HephaestusStatusHelper statusHelper,
        HephaestusPartyHelper partyHelper,
        HephaestusDebugState debugState,
        int cartridges,
        int gnashingFangStep,
        int comboStep,
        uint lastComboAction,
        float comboTimeRemaining,
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

        EnmityService = enmityService;
        TankCooldownService = tankCooldownService;
        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState;

        Cartridges = cartridges;
        GnashingFangStep = gnashingFangStep;
        ComboStep = comboStep;
        LastComboAction = lastComboAction;
        ComboTimeRemaining = comboTimeRemaining;

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
        HasRoyalGuard = statusHelper.HasRoyalGuard(player);
        HasTankStance = HasRoyalGuard;

        // Continuation ready checks
        IsReadyToRip = statusHelper.HasReadyToRip(player);
        IsReadyToTear = statusHelper.HasReadyToTear(player);
        IsReadyToGouge = statusHelper.HasReadyToGouge(player);
        IsReadyToBlast = statusHelper.HasReadyToBlast(player);
        IsReadyToReign = statusHelper.HasReadyToReign(player);

        // Damage buff checks
        HasNoMercy = statusHelper.HasNoMercy(player);
        NoMercyRemaining = statusHelper.GetNoMercyRemaining(player);

        // Defensive checks
        HasActiveMitigation = statusHelper.HasActiveMitigation(player);
        HasSuperbolide = statusHelper.HasSuperbolide(player);
        HasNebula = statusHelper.HasNebula(player);
        HasHeartOfCorundum = statusHelper.HasHeartOfCorundum(player);
        HasCamouflage = statusHelper.HasCamouflage(player);
        HasAurora = statusHelper.HasAurora(player);

        // DoT checks (on target)
        HasSonicBreakDot = _currentTarget != null && statusHelper.HasSonicBreakDebuff(_currentTarget);
        HasBowShockDot = _currentTarget != null && statusHelper.HasBowShockDebuff(_currentTarget);

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

        // Cartridge resource
        Debug.Cartridges = Cartridges;

        // Gnashing Fang combo
        Debug.GnashingFangStep = GnashingFangStep;
        Debug.IsInGnashingFangCombo = IsInGnashingFangCombo;

        // Continuation ready states
        Debug.IsReadyToRip = IsReadyToRip;
        Debug.IsReadyToTear = IsReadyToTear;
        Debug.IsReadyToGouge = IsReadyToGouge;
        Debug.IsReadyToBlast = IsReadyToBlast;
        Debug.IsReadyToReign = IsReadyToReign;

        // Tank stance
        Debug.HasRoyalGuard = HasRoyalGuard;

        // Buffs
        Debug.HasNoMercy = HasNoMercy;
        Debug.NoMercyRemaining = NoMercyRemaining;

        // Defensives
        Debug.HasActiveMitigation = HasActiveMitigation;
        Debug.ActiveMitigations = StatusHelper.GetActiveMitigations(Player);
        Debug.HasSuperbolide = HasSuperbolide;
        Debug.HasNebula = HasNebula;
        Debug.HasHeartOfCorundum = HasHeartOfCorundum;
        Debug.HasCamouflage = HasCamouflage;
        Debug.HasAurora = HasAurora;

        // DoTs
        Debug.HasSonicBreakDot = HasSonicBreakDot;
        Debug.HasBowShockDot = HasBowShockDot;

        // Enmity
        Debug.IsMainTank = IsMainTank;
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
    }
}
