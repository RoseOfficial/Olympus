using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.Common;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Positional;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;

namespace Olympus.Rotation.Base;

/// <summary>
/// Base class for melee DPS rotation implementations.
/// Provides IMeleeDpsRotationContext interface implementation and melee-specific services.
/// </summary>
/// <typeparam name="TContext">The melee DPS job-specific context type.</typeparam>
/// <typeparam name="TModule">The melee DPS job-specific module interface type.</typeparam>
public abstract class BaseMeleeDpsRotation<TContext, TModule> : BaseRotation<TContext, TModule>, IHasPositionals
    where TContext : IMeleeDpsRotationContext
    where TModule : IRotationModule<TContext>
{
    #region Melee DPS-Specific Services

    protected readonly IPositionalService PositionalService;

    /// <summary>
    /// Optional service for detecting raid buff burst windows.
    /// Null when party coordination is disabled or unavailable.
    /// </summary>
    protected readonly IBurstWindowService? BurstWindowService;

    #endregion

    #region Combo Tracking

    /// <summary>
    /// Current combo step (1-3, or 0 for no combo).
    /// </summary>
    protected int ComboStep { get; set; }

    /// <summary>
    /// Last combo action ID.
    /// </summary>
    protected uint LastComboAction { get; set; }

    /// <summary>
    /// Time remaining on current combo chain.
    /// </summary>
    protected float ComboTimeRemaining { get; set; }

    #endregion

    #region Positional State

    /// <summary>
    /// Whether the player is at the target's rear.
    /// </summary>
    protected bool IsAtRear { get; set; }

    /// <summary>
    /// Whether the player is at the target's flank.
    /// </summary>
    protected bool IsAtFlank { get; set; }

    /// <summary>
    /// Whether the target has positional immunity.
    /// </summary>
    protected bool TargetHasPositionalImmunity { get; set; }

    /// <summary>
    /// Current positional snapshot — updated every frame by UpdatePositionalState.
    /// Exposed publicly via IHasPositionals for MainWindow display.
    /// </summary>
    public PositionalSnapshot Positionals { get; private set; }

    #endregion

    #region Debug State

    // Melee DPS rotations typically have both job-specific and common debug states
    protected readonly DebugState CommonDebugState = new();

    // Cached list to avoid per-frame heap allocation when passing player ID to DamageTrendService
    private readonly System.Collections.Generic.List<uint> _damageTrendIds = new(1);

    #endregion

    #region Constructor

    protected BaseMeleeDpsRotation(
        IPluginLog log,
        IActionTracker actionTracker,
        ICombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
        IDamageTrendService damageTrendService,
        Configuration configuration,
        IObjectTable objectTable,
        IPartyList partyList,
        ITargetingService targetingService,
        IHpPredictionService hpPredictionService,
        ActionService actionService,
        IPlayerStatsService playerStatsService,
        IDebuffDetectionService debuffDetectionService,
        IPositionalService positionalService,
        IBurstWindowService? burstWindowService = null,
        IErrorMetricsService? errorMetrics = null)
        : base(
            log,
            actionTracker,
            combatEventService,
            damageIntakeService,
            damageTrendService,
            configuration,
            objectTable,
            partyList,
            targetingService,
            hpPredictionService,
            actionService,
            playerStatsService,
            debuffDetectionService,
            errorMetrics)
    {
        PositionalService = positionalService;
        BurstWindowService = burstWindowService;
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Returns the action ID used for positional range checking via game API.
    /// Override in derived classes to use the job's own basic melee GCD.
    /// Defaults to Heavy Swing (WAR, ID 31) — valid for any 3y melee action.
    /// </summary>
    protected virtual uint GetMeleeRangeActionId() => WARActions.HeavySwing.ActionId;

    /// <summary>
    /// Reads the job-specific gauge value(s).
    /// Must be implemented by each melee DPS job.
    /// </summary>
    protected abstract void ReadGaugeValues();

    /// <summary>
    /// Determines the current combo step based on the last combo action and timer.
    /// Must be implemented by each melee DPS job since combo chains vary.
    /// </summary>
    protected abstract int DetermineComboStep(uint comboAction, float comboTimer);

    /// <summary>
    /// Syncs melee DPS-specific debug state to common debug state for UI compatibility.
    /// Override in derived classes to map job-specific fields.
    /// </summary>
    protected abstract void SyncDebugState(TContext context);

    #endregion

    #region Combo State Management

    /// <summary>
    /// Updates combo state from game memory.
    /// </summary>
    protected virtual void UpdateComboState()
    {
        LastComboAction = SafeGameAccess.GetComboAction(ErrorMetrics);
        ComboTimeRemaining = SafeGameAccess.GetComboTimer(ErrorMetrics);
        ComboStep = DetermineComboStep(LastComboAction, ComboTimeRemaining);
    }

    #endregion

    #region Positional State Management

    /// <summary>
    /// Updates positional state based on current target.
    /// </summary>
    protected virtual void UpdatePositionalState(IPlayerCharacter player)
    {
        // Find current target using game API range check for maximum accuracy
        var target = TargetingService.FindEnemyForAction(
            Configuration.Targeting.EnemyStrategy,
            GetMeleeRangeActionId(),
            player);

        if (target == null)
        {
            IsAtRear = false;
            IsAtFlank = false;
            TargetHasPositionalImmunity = false;
            Positionals = new PositionalSnapshot { HasTarget = false };
            return;
        }

        // Check positional using the service
        var positional = PositionalService.GetPositional(player, target);
        IsAtRear = positional == PositionalType.Rear;
        IsAtFlank = positional == PositionalType.Flank;
        TargetHasPositionalImmunity = PositionalService.HasPositionalImmunity(target);
        // True North (status 1250) suppresses positional requirements
        var hasTrueNorth = HasStatusEffect(player, 1250);

        Positionals = new PositionalSnapshot
        {
            IsAtRear = IsAtRear,
            IsAtFlank = IsAtFlank,
            TargetHasImmunity = TargetHasPositionalImmunity,
            HasTarget = true,
            RequiredPositional = (TargetHasPositionalImmunity || hasTrueNorth) ? null : GetNextRequiredPositional(),
        };
    }

    /// <summary>
    /// Override in job rotations to report what positional the next GCD requires.
    /// Used by DrawCanvas to highlight where the player should stand.
    /// </summary>
    protected virtual PositionalType? GetNextRequiredPositional() => null;

    private static bool HasStatusEffect(Dalamud.Game.ClientState.Objects.Types.IBattleChara player, uint statusId)
    {
        foreach (var s in player.StatusList)
            if (s.StatusId == statusId) return true;
        return false;
    }

    #endregion

    #region Override Base Methods

    /// <summary>
    /// Updates melee DPS-specific state (gauge, combo, positionals).
    /// </summary>
    protected override void UpdateJobSpecificServices(IPlayerCharacter player, bool inCombat)
    {
        // Read job gauge
        ReadGaugeValues();

        // Read combo state
        UpdateComboState();

        // Update positional state
        UpdatePositionalState(player);

        // Update burst window tracking
        BurstWindowService?.Update(player);

        // Update damage trend service with player entity ID (melee track their own damage for self-healing considerations)
        if (inCombat)
        {
            _damageTrendIds.Clear();
            _damageTrendIds.Add(player.EntityId);
            DamageTrendService.Update(1f / 60f, _damageTrendIds);
        }
    }

    /// <summary>
    /// Override to sync debug state after module updates.
    /// </summary>
    protected override void UpdateModuleDebugStates(TContext context)
    {
        base.UpdateModuleDebugStates(context);

        // Sync melee DPS debug state to common state for UI
        if (Configuration.IsDebugWindowOpen)
        {
            SyncDebugState(context);
        }
    }

    #endregion
}
