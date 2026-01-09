using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Models.Action;

namespace Olympus.Rotation.Common.Modules;

/// <summary>
/// Base damage module for healer jobs.
/// Handles common DPS patterns: DoT maintenance, AoE damage, single-target fallback.
/// Job-specific implementations override methods for unique behavior (e.g., oGCD damage, special abilities).
/// </summary>
/// <typeparam name="TContext">The job-specific context type that implements IHealerRotationContext.</typeparam>
public abstract class BaseDamageModule<TContext> : IHealerRotationModule<TContext>
    where TContext : IHealerRotationContext
{
    public virtual int Priority => 50; // Low priority - DPS after healing
    public virtual string Name => "Damage";

    #region Abstract Properties - Must be implemented by job-specific modules

    /// <summary>
    /// Whether damage is enabled in configuration.
    /// </summary>
    protected abstract bool IsDamageEnabled(TContext context);

    /// <summary>
    /// Whether DoT is enabled in configuration.
    /// </summary>
    protected abstract bool IsDoTEnabled(TContext context);

    /// <summary>
    /// Whether AoE damage is enabled in configuration.
    /// </summary>
    protected abstract bool IsAoEDamageEnabled(TContext context);

    /// <summary>
    /// Minimum targets required for AoE damage from configuration.
    /// </summary>
    protected abstract int AoEMinTargets(TContext context);

    /// <summary>
    /// DoT refresh threshold in seconds from configuration.
    /// </summary>
    protected abstract float DoTRefreshThreshold(TContext context);

    #endregion

    #region Abstract Methods - Must be implemented by job-specific modules

    /// <summary>
    /// Gets the DoT status ID for the current player level.
    /// </summary>
    protected abstract uint GetDoTStatusId(TContext context);

    /// <summary>
    /// Gets the DoT action for the current player level.
    /// </summary>
    protected abstract ActionDefinition? GetDoTAction(TContext context);

    /// <summary>
    /// Gets the AoE damage action for the current player level.
    /// </summary>
    protected abstract ActionDefinition? GetAoEDamageAction(TContext context);

    /// <summary>
    /// Gets the single-target damage action for the current player level.
    /// </summary>
    protected abstract ActionDefinition GetSingleTargetAction(TContext context, bool isMoving);

    /// <summary>
    /// Sets the DPS state debug string.
    /// </summary>
    protected abstract void SetDpsState(TContext context, string state);

    /// <summary>
    /// Sets the AoE DPS state debug string.
    /// </summary>
    protected abstract void SetAoEDpsState(TContext context, string state);

    /// <summary>
    /// Sets the AoE DPS enemy count debug value.
    /// </summary>
    protected abstract void SetAoEDpsEnemyCount(TContext context, int count);

    /// <summary>
    /// Sets the planned action debug string.
    /// </summary>
    protected abstract void SetPlannedAction(TContext context, string action);

    #endregion

    #region Virtual Methods - Can be overridden for job-specific behavior

    /// <summary>
    /// Whether to return true when an action is executed (blocking other modules).
    /// Default: true. Override to return false for modules that shouldn't block (like WHM).
    /// </summary>
    protected virtual bool BlocksOnExecution => true;

    /// <summary>
    /// Override to try job-specific oGCD damage abilities (e.g., Chain Stratagem, Energy Drain).
    /// Called before GCD damage when oGCD is available.
    /// Default returns false.
    /// </summary>
    protected virtual bool TryOgcdDamage(TContext context) => false;

    /// <summary>
    /// Override to try job-specific special damage abilities (e.g., Afflatus Misery, Sacred Sight).
    /// Called at the start of damage execution, highest priority after oGCDs.
    /// Default returns false.
    /// </summary>
    protected virtual bool TrySpecialDamage(TContext context, bool isMoving) => false;

    /// <summary>
    /// Override to add job-specific checks before DoT (e.g., check if moving for cast-time DoTs).
    /// Default returns true (DoT is allowed).
    /// </summary>
    protected virtual bool CanDoT(TContext context, bool isMoving) => !isMoving;

    /// <summary>
    /// Override to add job-specific checks before single-target damage.
    /// Default returns true when not moving.
    /// </summary>
    protected virtual bool CanSingleTarget(TContext context, bool isMoving) => !isMoving;

    /// <summary>
    /// Override to try job-specific instant cast damage while moving (e.g., Ruin II).
    /// Called when moving and regular damage can't be cast.
    /// Default returns false.
    /// </summary>
    protected virtual bool TryMovementDamage(TContext context) => false;

    /// <summary>
    /// Override to check if a specific damage action is enabled in configuration.
    /// </summary>
    protected virtual bool IsActionEnabled(TContext context, ActionDefinition action) => true;

    #endregion

    public virtual bool TryExecute(TContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            SetDpsState(context, "Not in combat");
            return false;
        }

        // oGCD damage abilities first
        if (context.CanExecuteOgcd)
        {
            if (TryOgcdDamage(context))
                return BlocksOnExecution;
        }

        // GCD damage
        if (context.CanExecuteGcd)
        {
            // Priority 1: Special damage (job-specific, highest priority)
            if (TrySpecialDamage(context, isMoving))
                return BlocksOnExecution;

            // Priority 2: DoT maintenance
            if (CanDoT(context, isMoving) && TryDoT(context))
                return BlocksOnExecution;

            // Priority 3: AoE damage
            if (TryAoEDamage(context))
                return BlocksOnExecution;

            // Priority 4: Single-target damage
            if (CanSingleTarget(context, isMoving) && TrySingleTargetDamage(context, isMoving))
                return BlocksOnExecution;

            // Priority 5: Movement damage (instant casts while moving)
            if (isMoving && TryMovementDamage(context))
                return BlocksOnExecution;
        }

        return false;
    }

    public virtual void UpdateDebugState(TContext context)
    {
        // Base implementation - override for job-specific gauge display
    }

    #region Protected Implementation Methods

    /// <summary>
    /// Attempts to apply or refresh DoT on an enemy.
    /// </summary>
    protected virtual bool TryDoT(TContext context)
    {
        if (!IsDoTEnabled(context))
            return false;

        var dotAction = GetDoTAction(context);
        if (dotAction == null)
            return false;

        var dotStatusId = GetDoTStatusId(context);
        if (dotStatusId == 0)
            return false;

        var target = context.TargetingService.FindEnemyNeedingDot(
            dotStatusId,
            DoTRefreshThreshold(context),
            dotAction.Range,
            context.Player);

        if (target == null)
            return false;

        if (!IsActionEnabled(context, dotAction))
            return false;

        if (context.ActionService.ExecuteGcd(dotAction, target.GameObjectId))
        {
            SetPlannedAction(context, dotAction.Name);
            SetDpsState(context, "DoT");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts AoE damage when enough enemies are in range.
    /// </summary>
    protected virtual bool TryAoEDamage(TContext context)
    {
        if (!IsAoEDamageEnabled(context))
            return false;

        var aoeAction = GetAoEDamageAction(context);
        if (aoeAction == null)
            return false;

        if (!IsActionEnabled(context, aoeAction))
            return false;

        var enemyCount = context.TargetingService.CountEnemiesInRange(aoeAction.Radius, context.Player);
        SetAoEDpsEnemyCount(context, enemyCount);

        var minTargets = AoEMinTargets(context);
        if (enemyCount < minTargets)
        {
            SetAoEDpsState(context, $"{enemyCount} < {minTargets} min");
            return false;
        }

        // For self-targeted AoE (most healer AoEs)
        var targetId = aoeAction.TargetType == ActionTargetType.Self
            ? context.Player.GameObjectId
            : FindBestAoETarget(context, aoeAction);

        if (targetId == 0)
            return false;

        if (context.ActionService.ExecuteGcd(aoeAction, targetId))
        {
            SetPlannedAction(context, aoeAction.Name);
            SetDpsState(context, $"AoE ({enemyCount} targets)");
            SetAoEDpsState(context, $"{enemyCount} enemies");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts single-target damage on the current enemy.
    /// </summary>
    protected virtual bool TrySingleTargetDamage(TContext context, bool isMoving)
    {
        if (!IsDamageEnabled(context))
            return false;

        var action = GetSingleTargetAction(context, isMoving);
        if (!IsActionEnabled(context, action))
            return false;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            action.Range,
            context.Player);

        if (target == null)
        {
            SetDpsState(context, "No enemy found");
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            SetPlannedAction(context, action.Name);
            SetDpsState(context, action.Name);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the best target for a targeted AoE ability.
    /// Override for job-specific AoE targeting logic.
    /// </summary>
    protected virtual ulong FindBestAoETarget(TContext context, ActionDefinition aoeAction)
    {
        var (target, _) = context.TargetingService.FindBestAoETarget(
            aoeAction.Radius,
            aoeAction.Range,
            context.Player);

        return target?.GameObjectId ?? 0;
    }

    #endregion
}
