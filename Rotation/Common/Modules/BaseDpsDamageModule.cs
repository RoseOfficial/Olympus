using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Services.Targeting;

namespace Olympus.Rotation.Common.Modules;

/// <summary>
/// Base damage module for DPS jobs (melee, ranged physical, caster).
/// Handles the common TryExecute pattern: combat check, target acquisition, enemy count, oGCD/GCD phases.
/// Job-specific implementations override the abstract methods for their unique rotation logic.
/// </summary>
/// <typeparam name="TContext">The job-specific context type that implements IRotationContext.</typeparam>
public abstract class BaseDpsDamageModule<TContext> : IRotationModule<TContext>
    where TContext : IRotationContext
{
    /// <summary>
    /// Default priority for damage modules (30 = lowest priority, after buffs/utility).
    /// </summary>
    public virtual int Priority => 30;

    /// <summary>
    /// Module name for debug display.
    /// </summary>
    public virtual string Name => "Damage";

    /// <summary>
    /// Default AoE threshold - use AoE rotation when >= this many enemies.
    /// Override in derived class to change threshold.
    /// </summary>
    protected virtual int AoeThreshold => 3;

    #region Abstract Methods - Must be implemented by job-specific modules

    /// <summary>
    /// Gets the targeting range for this job (melee = 3y, ranged/caster = 25y).
    /// </summary>
    protected abstract float GetTargetingRange();

    /// <summary>
    /// Gets the range used for counting nearby enemies for AoE decisions.
    /// Typically 5y for melee, 8y for ranged AoE.
    /// </summary>
    protected abstract float GetAoECountRange();

    /// <summary>
    /// Attempts to execute oGCD damage abilities during the weave window.
    /// Called when context.CanExecuteOgcd is true.
    /// </summary>
    /// <param name="context">The job-specific context.</param>
    /// <param name="target">The current enemy target.</param>
    /// <param name="enemyCount">Number of nearby enemies for AoE decisions.</param>
    /// <returns>True if an oGCD was executed, false otherwise.</returns>
    protected abstract bool TryOgcdDamage(TContext context, IBattleChara target, int enemyCount);

    /// <summary>
    /// Attempts to execute GCD damage abilities.
    /// Called when context.CanExecuteGcd is true and no oGCD was executed.
    /// Contains the job's main rotation logic (combos, burst, etc.).
    /// </summary>
    /// <param name="context">The job-specific context.</param>
    /// <param name="target">The current enemy target.</param>
    /// <param name="enemyCount">Number of nearby enemies for AoE decisions.</param>
    /// <param name="isMoving">Whether the player is currently moving.</param>
    /// <returns>True if a GCD was executed, false otherwise.</returns>
    protected abstract bool TryGcdDamage(TContext context, IBattleChara target, int enemyCount, bool isMoving);

    /// <summary>
    /// Sets the damage state debug string for this job's debug display.
    /// </summary>
    protected abstract void SetDamageState(TContext context, string state);

    /// <summary>
    /// Sets the nearby enemy count in this job's debug display.
    /// </summary>
    protected abstract void SetNearbyEnemies(TContext context, int count);

    /// <summary>
    /// Sets the planned action name in this job's debug display.
    /// </summary>
    protected abstract void SetPlannedAction(TContext context, string action);

    #endregion

    #region Virtual Methods - Can be overridden for customization

    /// <summary>
    /// Performs pre-execution checks before attempting any damage.
    /// Default: checks InCombat only.
    /// Override to add job-specific checks (e.g., resources, cooldowns).
    /// </summary>
    /// <returns>True if execution should continue, false to abort.</returns>
    protected virtual bool PreExecuteChecks(TContext context)
    {
        return context.InCombat;
    }

    /// <summary>
    /// Returns the action ID to use for game-native range checking via GetActionInRangeOrLoS.
    /// Override in melee job modules to use the accurate game API instead of distance math.
    /// Return 0 to fall back to distance-based range checking.
    /// </summary>
    protected virtual uint GetRangeCheckActionId() => 0;

    /// <summary>
    /// Finds the best enemy target for damage.
    /// Uses action-based range check (GetActionInRangeOrLoS) when GetRangeCheckActionId() returns non-zero,
    /// otherwise falls back to distance-based range checking.
    /// </summary>
    protected virtual IBattleChara? AcquireTarget(TContext context)
    {
        var actionId = GetRangeCheckActionId();
        if (actionId != 0)
            return context.TargetingService.FindEnemyForAction(
                context.Configuration.Targeting.EnemyStrategy,
                actionId,
                context.Player);

        return context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            GetTargetingRange(),
            context.Player);
    }

    /// <summary>
    /// Called when no oGCD or GCD action was available.
    /// Override to add fallback behavior or additional logging.
    /// </summary>
    protected virtual void OnNoActionAvailable(TContext context)
    {
        SetDamageState(context, "No action available");
    }

    #endregion

    /// <summary>
    /// Main execution method implementing the template method pattern.
    /// Handles the common flow: combat check -> target -> enemy count -> oGCD -> GCD.
    /// Can be overridden for jobs with unique execution patterns (e.g., PCT prepaint).
    /// </summary>
    public virtual bool TryExecute(TContext context, bool isMoving)
    {
        // Phase 1: Pre-execution checks
        if (!PreExecuteChecks(context))
        {
            SetDamageState(context, "Not in combat");
            return false;
        }

        // Phase 2: Target acquisition
        var target = AcquireTarget(context);
        if (target == null)
        {
            SetDamageState(context, "No target");
            return false;
        }

        // Phase 3: Enemy count for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(GetAoECountRange(), context.Player);
        SetNearbyEnemies(context, enemyCount);

        // Phase 3b: Smart AoE — compute optimal facing for the last/next directional ability
        if (enemyCount > 0)
            UpdateSmartAoE(context, target);

        // Phase 4: oGCD damage (weave window)
        if (context.CanExecuteOgcd)
        {
            if (TryOgcdDamage(context, target, enemyCount))
                return true;
        }

        // Phase 5: GCD readiness check
        if (!context.CanExecuteGcd)
        {
            SetDamageState(context, "GCD not ready");
            return false;
        }

        // Phase 6: GCD damage (main rotation)
        if (TryGcdDamage(context, target, enemyCount, isMoving))
            return true;

        // Phase 7: No action available
        OnNoActionAvailable(context);
        return false;
    }

    /// <summary>
    /// Updates debug state for this module.
    /// Override in derived class to update job-specific gauge displays.
    /// </summary>
    public virtual void UpdateDebugState(TContext context)
    {
        // Base implementation - override for job-specific gauge display
    }

    #region Protected Utility Methods

    /// <summary>
    /// Checks if we should use AoE rotation based on enemy count.
    /// </summary>
    protected bool ShouldUseAoE(int enemyCount) => enemyCount >= AoeThreshold;

    /// <summary>
    /// Helper method to execute a GCD action and set debug state.
    /// </summary>
    protected bool ExecuteGcdWithDebug(TContext context, Models.Action.ActionDefinition action, ulong targetId, string? stateOverride = null)
    {
        if (context.ActionService.ExecuteGcd(action, targetId))
        {
            SetPlannedAction(context, action.Name);
            SetDamageState(context, stateOverride ?? action.Name);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Helper method to execute an oGCD action and set debug state.
    /// </summary>
    protected bool ExecuteOgcdWithDebug(TContext context, Models.Action.ActionDefinition action, ulong targetId, string? stateOverride = null)
    {
        if (context.ActionService.ExecuteOgcd(action, targetId))
        {
            SetPlannedAction(context, action.Name);
            SetDamageState(context, stateOverride ?? action.Name);
            return true;
        }
        return false;
    }

    #endregion

    #region Smart AoE Integration

    /// <summary>
    /// Override to provide the action ID of the next directional AoE the rotation plans to use.
    /// SmartAoEService will compute the optimal facing angle for it.
    /// Return 0 if no directional AoE is planned.
    /// </summary>
    protected virtual uint GetNextDirectionalAoEActionId(TContext context, IBattleChara target, int enemyCount) => 0;

    private uint _lastSmartAoEActionId;

    private void UpdateSmartAoE(TContext context, IBattleChara target)
    {
        var svc = SmartAoEService.Instance;
        if (svc == null) return;

        var actionId = GetNextDirectionalAoEActionId(context, target,
            context.TargetingService.CountEnemiesInRange(GetAoECountRange(), context.Player));

        if (actionId == 0)
        {
            _lastSmartAoEActionId = 0;
            return;
        }

        if (!svc.IsDirectionalAoE(actionId)) return;

        // Always update the tracker state (for debug display),
        // but only record a new prediction when the action changes
        var isNewAction = actionId != _lastSmartAoEActionId;
        _lastSmartAoEActionId = actionId;

        var result = svc.FindBestAoETarget(actionId, GetTargetingRange(), context.Player, recordPrediction: isNewAction);

        // Face the optimal direction for the next directional AoE
        if (result.HitCount > 0 && context.CanExecuteGcd)
            context.ActionService.FaceDirection(result.OptimalAngle);
    }

    #endregion
}
