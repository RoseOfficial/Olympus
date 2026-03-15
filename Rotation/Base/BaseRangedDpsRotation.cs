using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.Common;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;

namespace Olympus.Rotation.Base;

/// <summary>
/// Base class for ranged physical DPS rotation implementations.
/// Simpler than melee DPS - no positional tracking, uses 25y range.
/// </summary>
/// <typeparam name="TContext">The ranged DPS job-specific context type.</typeparam>
/// <typeparam name="TModule">The ranged DPS job-specific module interface type.</typeparam>
public abstract class BaseRangedDpsRotation<TContext, TModule> : BaseRotation<TContext, TModule>
    where TContext : IRangedDpsRotationContext
    where TModule : IRotationModule<TContext>
{
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

    #region Debug State

    // Ranged DPS rotations typically have both job-specific and common debug states
    protected readonly DebugState CommonDebugState = new();

    #endregion

    #region Constructor

    protected BaseRangedDpsRotation(
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
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Reads the job-specific gauge value(s).
    /// Must be implemented by each ranged DPS job.
    /// </summary>
    protected abstract void ReadGaugeValues();

    /// <summary>
    /// Determines the current combo step based on the last combo action and timer.
    /// Must be implemented by each ranged DPS job since combo chains vary.
    /// </summary>
    protected abstract int DetermineComboStep(uint comboAction, float comboTimer);

    /// <summary>
    /// Syncs ranged DPS-specific debug state to common debug state for UI compatibility.
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

    #region Override Base Methods

    /// <summary>
    /// Updates ranged DPS-specific state (gauge, combo).
    /// No positional tracking for ranged jobs.
    /// </summary>
    protected override void UpdateJobSpecificServices(IPlayerCharacter player, bool inCombat)
    {
        // Read job gauge
        ReadGaugeValues();

        // Read combo state
        UpdateComboState();

        // Update damage trend service with player entity ID
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

        // Sync ranged DPS debug state to common state for UI
        if (Configuration.IsDebugWindowOpen)
        {
            SyncDebugState(context);
        }
    }

    #endregion
}
