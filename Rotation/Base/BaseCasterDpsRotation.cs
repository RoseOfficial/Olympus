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
/// Base class for caster DPS rotation implementations.
/// Casters don't have combos but need MP management and cast time awareness.
/// Uses 25y targeting range.
/// </summary>
/// <typeparam name="TContext">The caster DPS job-specific context type.</typeparam>
/// <typeparam name="TModule">The caster DPS job-specific module interface type.</typeparam>
public abstract class BaseCasterDpsRotation<TContext, TModule> : BaseRotation<TContext, TModule>
    where TContext : ICasterDpsRotationContext
    where TModule : IRotationModule<TContext>
{
    #region Caster DPS-Specific Services

    /// <summary>
    /// Optional service for detecting raid buff burst windows.
    /// Null when party coordination is disabled or unavailable.
    /// </summary>
    protected readonly IBurstWindowService? BurstWindowService;

    /// <summary>
    /// Optional service for computing optimal directional AoE facing.
    /// </summary>
    protected readonly ISmartAoEService? SmartAoEService;

    #endregion

    #region Debug State

    // Caster DPS rotations typically have both job-specific and common debug states
    protected readonly DebugState CommonDebugState = new();

    // Cached list to avoid per-frame heap allocation when passing player ID to DamageTrendService
    private readonly System.Collections.Generic.List<uint> _damageTrendIds = new(1);

    #endregion

    #region Constructor

    protected BaseCasterDpsRotation(
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
        IBurstWindowService? burstWindowService = null,
        IErrorMetricsService? errorMetrics = null,
        ISmartAoEService? smartAoEService = null)
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
        BurstWindowService = burstWindowService;
        SmartAoEService = smartAoEService;
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Reads the job-specific gauge value(s).
    /// Must be implemented by each caster DPS job.
    /// </summary>
    protected abstract void ReadGaugeValues();

    /// <summary>
    /// Syncs caster DPS-specific debug state to common debug state for UI compatibility.
    /// Override in derived classes to map job-specific fields.
    /// </summary>
    protected abstract void SyncDebugState(TContext context);

    #endregion

    #region Override Base Methods

    /// <summary>
    /// Updates caster DPS-specific state (gauge, MP).
    /// No combo tracking for casters.
    /// </summary>
    protected override void UpdateJobSpecificServices(IPlayerCharacter player, bool inCombat)
    {
        // Read job gauge
        ReadGaugeValues();

        // Update burst window tracking
        BurstWindowService?.Update(player);

        // Update damage trend service with player entity ID
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

        // Sync caster DPS debug state to common state for UI
        if (Configuration.IsDebugWindowOpen)
        {
            SyncDebugState(context);
        }
    }

    #endregion
}
