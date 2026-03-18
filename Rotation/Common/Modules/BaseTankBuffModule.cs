using Olympus.Models.Action;

namespace Olympus.Rotation.Common.Modules;

/// <summary>
/// Base buff module for tank jobs.
/// Handles common patterns like tank stance management.
/// Job-specific implementations override methods for burst buffs (Fight or Flight, Inner Release, etc.).
/// </summary>
/// <typeparam name="TContext">The job-specific context type that implements ITankRotationContext.</typeparam>
public abstract class BaseTankBuffModule<TContext> : ITankRotationModule<TContext>
    where TContext : ITankRotationContext
{
    public virtual int Priority => 20; // After mitigation, before damage
    public virtual string Name => "Buff";

    #region Abstract Methods - Must be implemented by job-specific modules

    /// <summary>
    /// Gets the tank stance action for this job (Iron Will, Defiance, Grit, Royal Guard).
    /// </summary>
    protected abstract ActionDefinition GetTankStanceAction();

    /// <summary>
    /// Gets whether the job-specific tank stance is currently active.
    /// </summary>
    protected abstract bool HasJobTankStance(TContext context);

    /// <summary>
    /// Sets the buff state for debug display.
    /// </summary>
    protected abstract void SetBuffState(TContext context, string state);

    /// <summary>
    /// Sets the planned action for debug display.
    /// </summary>
    protected abstract void SetPlannedAction(TContext context, string action);

    #endregion

    #region Virtual Methods - Can be overridden for job-specific behavior

    /// <summary>
    /// Whether auto tank stance is enabled in configuration.
    /// Default checks Configuration.Tank.AutoTankStance.
    /// </summary>
    protected virtual bool IsAutoTankStanceEnabled(TContext context)
        => context.Configuration.Tank.AutoTankStance;

    /// <summary>
    /// Whether damage abilities are enabled in configuration.
    /// Override if job needs to check this for buff usage.
    /// </summary>
    protected virtual bool IsDamageEnabled(TContext context)
        => context.Configuration.Tank.EnableDamage;

    /// <summary>
    /// Override to try job-specific burst buffs (Fight or Flight, Inner Release, No Mercy, Delirium).
    /// Called after tank stance check.
    /// </summary>
    protected virtual bool TryJobSpecificBuffs(TContext context) => false;

    /// <summary>
    /// Override to try job-specific resource generators (Infuriate, Bloodfest, etc.).
    /// Called after burst buffs.
    /// </summary>
    protected virtual bool TryJobSpecificResourceGeneration(TContext context) => false;

    #endregion

    public virtual bool TryExecute(TContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            SetBuffState(context, "Not in combat");
            return false;
        }

        if (!context.CanExecuteOgcd)
            return false;

        // Priority 1: Tank stance management (always runs regardless of damage toggle)
        if (TryTankStance(context))
            return true;

        // Burst buffs and resource generation are damage-oriented — skip when damage is disabled
        if (!IsDamageEnabled(context))
        {
            SetBuffState(context, "Damage disabled");
            return false;
        }

        // Priority 2: Job-specific burst buffs
        if (TryJobSpecificBuffs(context))
            return true;

        // Priority 3: Job-specific resource generation
        if (TryJobSpecificResourceGeneration(context))
            return true;

        return false;
    }

    public virtual void UpdateDebugState(TContext context)
    {
        // Base implementation - override for job-specific debug display
    }

    #region Protected Implementation Methods

    /// <summary>
    /// Attempts to enable tank stance if missing and configured.
    /// </summary>
    protected virtual bool TryTankStance(TContext context)
    {
        var player = context.Player;
        var stanceAction = GetTankStanceAction();

        if (player.Level < stanceAction.MinLevel)
            return false;

        // Check configuration
        if (!IsAutoTankStanceEnabled(context))
        {
            SetBuffState(context, "AutoTankStance disabled");
            return false;
        }

        // Already have tank stance
        if (HasJobTankStance(context))
            return false;

        // Check if action is ready
        if (!context.ActionService.IsActionReady(stanceAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(stanceAction, player.GameObjectId))
        {
            SetPlannedAction(context, stanceAction.Name);
            SetBuffState(context, $"Enabling {stanceAction.Name}");
            return true;
        }

        return false;
    }

    #endregion
}
