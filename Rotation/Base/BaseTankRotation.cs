using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Common;
using Olympus.Rotation.Tank;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;

namespace Olympus.Rotation.Base;

/// <summary>
/// Base class for tank rotation implementations.
/// Provides ITankRotation interface implementation and tank-specific services.
/// </summary>
/// <typeparam name="TContext">The tank job-specific context type.</typeparam>
/// <typeparam name="TModule">The tank job-specific module interface type.</typeparam>
public abstract class BaseTankRotation<TContext, TModule> : BaseRotation<TContext, TModule>, ITankRotation
    where TContext : ITankRotationContext
    where TModule : IRotationModule<TContext>
{
    #region ITankRotation Implementation

    /// <inheritdoc />
    public bool IsMainTank { get; protected set; }

    /// <inheritdoc />
    public int GaugeValue { get; protected set; }

    #endregion

    #region Tank-Specific Services

    protected readonly IEnmityService EnmityService;
    protected readonly ITankCooldownService TankCooldownService;

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

    #region Debug State

    // Tank rotations typically have both job-specific and common debug states
    protected readonly DebugState CommonDebugState = new();

    #endregion

    #region Constructor

    protected BaseTankRotation(
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
        IEnmityService enmityService,
        ITankCooldownService tankCooldownService,
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
        EnmityService = enmityService;
        TankCooldownService = tankCooldownService;
    }

    #endregion

    #region Tank-Specific Methods

    /// <summary>
    /// Reads the job-specific gauge value.
    /// Must be implemented by each tank job.
    /// </summary>
    protected abstract int ReadGaugeValue();

    /// <summary>
    /// Determines the current combo step based on the last combo action and timer.
    /// Must be implemented by each tank job since combo chains vary.
    /// </summary>
    protected abstract int DetermineComboStep(uint comboAction, float comboTimer);

    /// <summary>
    /// Updates combo state from game memory.
    /// </summary>
    protected virtual void UpdateComboState()
    {
        LastComboAction = SafeGameAccess.GetComboAction(ErrorMetrics);
        ComboTimeRemaining = SafeGameAccess.GetComboTimer(ErrorMetrics);
        ComboStep = DetermineComboStep(LastComboAction, ComboTimeRemaining);
    }

    /// <summary>
    /// Updates gauge value from game memory.
    /// </summary>
    protected virtual void UpdateGaugeValue()
    {
        GaugeValue = ReadGaugeValue();
    }

    /// <summary>
    /// Syncs tank-specific debug state to common debug state for UI compatibility.
    /// Override in derived classes to map job-specific fields.
    /// </summary>
    protected abstract void SyncDebugState(TContext context);

    #endregion

    #region Override Base Methods

    /// <summary>
    /// Updates tank-specific state (gauge, combo, enmity).
    /// </summary>
    protected override void UpdateJobSpecificServices(IPlayerCharacter player, bool inCombat)
    {
        // Read job gauge
        UpdateGaugeValue();

        // Read combo state
        UpdateComboState();

        // Update damage trend service with player entity ID (tanks track their own damage intake)
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

        // Sync tank debug state to common state for UI
        if (Configuration.IsDebugWindowOpen)
        {
            SyncDebugState(context);
        }
    }

    #endregion
}
