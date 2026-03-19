using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Models.Action;

namespace Olympus.Rotation.Common.Modules;

/// <summary>
/// Base damage module for tank jobs.
/// Handles the common TryExecute preamble: config check, combat check, two-pass target
/// acquisition (melee → gap-closer range), gap-closer/ranged fallback, enemy count, oGCD/GCD phases.
/// Job-specific implementations override abstract methods for their unique rotation logic.
/// </summary>
/// <typeparam name="TContext">The tank job-specific context type.</typeparam>
public abstract class BaseTankDamageModule<TContext> : IRotationModule<TContext>
    where TContext : ITankRotationContext
{
    public virtual int Priority => 30;
    public virtual string Name => "Damage";

    #region Abstract Methods — job-specific overrides

    /// <summary>
    /// Returns the action ID of the job's basic melee combo starter (e.g. Heavy Swing, Hard Slash).
    /// Used for game-native melee range checking via FindEnemyForAction.
    /// </summary>
    protected abstract uint GetMeleeRangeCheckActionId();

    /// <summary>
    /// Attempts to use the job's gap-closer oGCD on an out-of-range target.
    /// Return false if unavailable or not implemented.
    /// </summary>
    protected abstract bool TryGapCloser(TContext context, IBattleChara target);

    /// <summary>
    /// Attempts to use the job's ranged GCD attack on an out-of-range target.
    /// Return false if unavailable or not implemented.
    /// </summary>
    protected abstract bool TryRangedAttack(TContext context, IBattleChara target);

    /// <summary>
    /// Attempts to execute oGCD damage abilities during the weave window.
    /// </summary>
    protected abstract bool TryOgcdDamage(TContext context, IBattleChara target, int enemyCount);

    /// <summary>
    /// Attempts to execute GCD damage abilities (main combo rotation).
    /// </summary>
    protected abstract bool TryGcdDamage(TContext context, IBattleChara target, int enemyCount, bool isMoving);

    /// <summary>Sets the damage state debug string.</summary>
    protected abstract void SetDamageState(TContext context, string state);

    /// <summary>Sets the nearby enemy count for debug display.</summary>
    protected abstract void SetNearbyEnemies(TContext context, int count);

    #endregion

    /// <summary>
    /// Main execution method implementing the template method pattern.
    /// Handles: config check → combat check → target → gap-close → enemy count → oGCD → GCD.
    /// </summary>
    public virtual bool TryExecute(TContext context, bool isMoving)
    {
        // Phase 1: Config check
        if (!context.Configuration.Tank.EnableDamage)
        {
            SetDamageState(context, "Disabled");
            return false;
        }

        // Phase 2: Combat check
        if (!context.InCombat)
        {
            SetDamageState(context, "Not in combat");
            return false;
        }

        var player = context.Player;

        // Phase 3: Two-pass target acquisition
        // First pass: melee range via game API
        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            GetMeleeRangeCheckActionId(),
            player);

        // Second pass: wider range for gap-closer engagement
        var engageTarget = target ?? context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            20f,
            player);

        if (engageTarget == null)
        {
            SetDamageState(context, "No target");
            return false;
        }

        // Phase 4: Out of melee range — gap closer / ranged attack
        if (target == null)
        {
            if (context.CanExecuteOgcd && TryGapCloser(context, engageTarget))
                return true;
            if (context.CanExecuteGcd && TryRangedAttack(context, engageTarget))
                return true;

            SetDamageState(context, "Target out of melee range");
            return false;
        }

        // Phase 5: Enemy count for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        SetNearbyEnemies(context, enemyCount);

        // Phase 6: oGCD damage (weave window)
        if (context.CanExecuteOgcd)
        {
            if (TryOgcdDamage(context, target, enemyCount))
                return true;
        }

        // Phase 7: GCD readiness check
        if (!context.CanExecuteGcd)
        {
            SetDamageState(context, "GCD not ready");
            return false;
        }

        // Phase 8: GCD damage (main rotation)
        if (TryGcdDamage(context, target, enemyCount, isMoving))
            return true;

        // Phase 9: No action available
        SetDamageState(context, "No action available");
        return false;
    }

    /// <summary>
    /// Updates debug state for this module. Override for job-specific gauge display.
    /// </summary>
    public virtual void UpdateDebugState(TContext context) { }

    #region Utility Methods

    /// <summary>
    /// Helper to execute a GCD and set debug state.
    /// </summary>
    protected bool ExecuteGcdWithDebug(TContext context, ActionDefinition action, ulong targetId, string? stateOverride = null)
    {
        if (context.ActionService.ExecuteGcd(action, targetId))
        {
            SetDamageState(context, stateOverride ?? action.Name);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Helper to execute an oGCD and set debug state.
    /// </summary>
    protected bool ExecuteOgcdWithDebug(TContext context, ActionDefinition action, ulong targetId, string? stateOverride = null)
    {
        if (context.ActionService.ExecuteOgcd(action, targetId))
        {
            SetDamageState(context, stateOverride ?? action.Name);
            return true;
        }
        return false;
    }

    #endregion
}
