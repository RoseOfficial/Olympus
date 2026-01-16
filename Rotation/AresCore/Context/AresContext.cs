using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
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
using Olympus.Rotation.AresCore.Helpers;
using Olympus.Timeline;

namespace Olympus.Rotation.AresCore.Context;

/// <summary>
/// Warrior-specific context implementation.
/// Provides all state needed for Ares rotation modules.
/// </summary>
public sealed class AresContext : IAresContext
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

    #region IAresContext Implementation

    public int BeastGauge { get; }
    public bool HasDefiance { get; }
    public bool HasSurgingTempest { get; }
    public float SurgingTempestRemaining { get; }
    public bool HasInnerRelease { get; }
    public int InnerReleaseStacks { get; }
    public bool HasNascentChaos { get; }
    public bool HasPrimalRendReady { get; }
    public bool HasPrimalRuinationReady { get; }
    public bool HasActiveMitigation { get; }
    public bool HasHolmgang { get; }
    public bool HasVengeance { get; }
    public bool HasBloodwhetting { get; }

    public AresStatusHelper StatusHelper { get; }
    public AresPartyHelper PartyHelper { get; }
    public AresDebugState Debug { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public AresContext(
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
        AresStatusHelper statusHelper,
        AresPartyHelper partyHelper,
        AresDebugState debugState,
        int beastGauge,
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

        BeastGauge = beastGauge;
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
        HasDefiance = statusHelper.HasDefiance(player);
        HasTankStance = HasDefiance;

        // Damage buff checks
        HasSurgingTempest = statusHelper.HasSurgingTempest(player);
        SurgingTempestRemaining = statusHelper.GetSurgingTempestRemaining(player);
        HasInnerRelease = statusHelper.HasInnerRelease(player);
        InnerReleaseStacks = statusHelper.GetInnerReleaseStacks(player);
        HasNascentChaos = statusHelper.HasNascentChaos(player);
        HasPrimalRendReady = statusHelper.HasPrimalRendReady(player);
        HasPrimalRuinationReady = statusHelper.HasPrimalRuinationReady(player);

        // Defensive checks
        HasActiveMitigation = statusHelper.HasActiveMitigation(player);
        HasHolmgang = statusHelper.HasHolmgang(player);
        HasVengeance = statusHelper.HasVengeance(player);
        HasBloodwhetting = statusHelper.HasBloodwhetting(player);

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
        Debug.ComboStep = ComboStep;
        Debug.ComboTimeRemaining = ComboTimeRemaining;
        Debug.BeastGauge = BeastGauge;
        Debug.HasDefiance = HasDefiance;
        Debug.HasSurgingTempest = HasSurgingTempest;
        Debug.SurgingTempestRemaining = SurgingTempestRemaining;
        Debug.HasInnerRelease = HasInnerRelease;
        Debug.InnerReleaseStacks = InnerReleaseStacks;
        Debug.HasNascentChaos = HasNascentChaos;
        Debug.HasPrimalRendReady = HasPrimalRendReady;
        Debug.HasPrimalRuinationReady = HasPrimalRuinationReady;
        Debug.HasActiveMitigation = HasActiveMitigation;
        Debug.ActiveMitigations = StatusHelper.GetActiveMitigations(Player);
        Debug.IsMainTank = IsMainTank;
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
    }
}
