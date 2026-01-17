using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.ApolloCore.Context;
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
public abstract class BaseMeleeDpsRotation<TContext, TModule> : BaseRotation<TContext, TModule>
    where TContext : IMeleeDpsRotationContext
    where TModule : IRotationModule<TContext>
{
    #region Melee DPS-Specific Services

    protected readonly IPositionalService PositionalService;

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

    #endregion

    #region Debug State

    // Melee DPS rotations typically have both job-specific and common debug states
    protected readonly DebugState CommonDebugState = new();

    #endregion

    #region Constructor

    protected BaseMeleeDpsRotation(
        IPluginLog log,
        ActionTracker actionTracker,
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
    }

    #endregion

    #region Abstract Methods

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
        // Find current target
        var target = TargetingService.FindEnemy(
            Configuration.Targeting.EnemyStrategy,
            3f, // Melee range
            player);

        if (target == null)
        {
            IsAtRear = false;
            IsAtFlank = false;
            TargetHasPositionalImmunity = false;
            return;
        }

        // Check positional using the service
        var positional = PositionalService.GetPositional(player, target);
        IsAtRear = positional == PositionalType.Rear;
        IsAtFlank = positional == PositionalType.Flank;
        TargetHasPositionalImmunity = PositionalService.HasPositionalImmunity(target);
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

        // Update damage trend service with player entity ID (melee track their own damage for self-healing considerations)
        if (inCombat)
        {
            var entityIds = new System.Collections.Generic.List<uint> { player.EntityId };
            (DamageTrendService as DamageTrendService)?.Update(1f / 60f, entityIds);
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
