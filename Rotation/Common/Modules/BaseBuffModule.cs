namespace Olympus.Rotation.Common.Modules;

/// <summary>
/// Base buff module for healer jobs.
/// Handles common patterns like Lucid Dreaming and oGCD buff management.
/// Job-specific implementations override methods for unique buffs (e.g., Thin Air, Dissipation).
/// </summary>
/// <typeparam name="TContext">The job-specific context type that implements IHealerRotationContext.</typeparam>
public abstract class BaseBuffModule<TContext> : IHealerRotationModule<TContext>
    where TContext : IHealerRotationContext
{
    public virtual int Priority => 30; // After healing/defensive, before damage
    public virtual string Name => "Buffs";

    #region Abstract Methods - Must be implemented by job-specific modules

    /// <summary>
    /// Sets the Lucid Dreaming debug state.
    /// </summary>
    protected abstract void SetLucidState(TContext context, string state);

    /// <summary>
    /// Sets the planned action debug string.
    /// </summary>
    protected abstract void SetPlannedAction(TContext context, string action);

    /// <summary>
    /// Gets whether Lucid Dreaming is enabled in configuration.
    /// </summary>
    protected abstract bool IsLucidDreamingEnabled(TContext context);

    /// <summary>
    /// Gets the Lucid Dreaming action definition for this job.
    /// </summary>
    protected abstract Models.Action.ActionDefinition GetLucidDreamingAction();

    /// <summary>
    /// Gets whether Lucid Dreaming is currently active on the player.
    /// </summary>
    protected abstract bool HasLucidDreaming(TContext context);

    /// <summary>
    /// Gets the MP threshold below which Lucid Dreaming should be used.
    /// </summary>
    protected abstract float GetLucidDreamingThreshold(TContext context);

    #endregion

    #region Virtual Methods - Can be overridden for job-specific behavior

    /// <summary>
    /// Whether to require combat for buff usage.
    /// Default: false (Apollo uses buffs out of combat for raise prep, etc.)
    /// </summary>
    protected virtual bool RequiresCombat => false;

    /// <summary>
    /// Override to try job-specific buffs before Lucid Dreaming.
    /// Called first in the buff execution order.
    /// Default returns false.
    /// </summary>
    protected virtual bool TryJobSpecificBuffs(TContext context, bool isMoving) => false;

    /// <summary>
    /// Override to try job-specific utility abilities after Lucid Dreaming.
    /// Default returns false.
    /// </summary>
    protected virtual bool TryJobSpecificUtilities(TContext context, bool isMoving) => false;

    /// <summary>
    /// Override to add additional Lucid Dreaming conditions (e.g., predictive logic).
    /// Return true to use Lucid, false to skip.
    /// Default uses simple MP threshold check.
    /// </summary>
    protected virtual bool ShouldUseLucidDreaming(TContext context)
    {
        var player = context.Player;
        var mpPercent = player.MaxMp > 0 ? (float)player.CurrentMp / player.MaxMp : 1f;
        return mpPercent < GetLucidDreamingThreshold(context);
    }

    #endregion

    public virtual bool TryExecute(TContext context, bool isMoving)
    {
        if (RequiresCombat && !context.InCombat)
            return false;

        if (!context.CanExecuteOgcd)
            return false;

        // Priority 1: Job-specific buffs (Thin Air, PoM, etc.)
        if (TryJobSpecificBuffs(context, isMoving))
            return true;

        // Priority 2: Lucid Dreaming (common MP management)
        if (TryLucidDreaming(context))
            return true;

        // Priority 3: Job-specific utilities (Surecast, Aetherial Shift, etc.)
        if (TryJobSpecificUtilities(context, isMoving))
            return true;

        return false;
    }

    public virtual void UpdateDebugState(TContext context)
    {
        // Base implementation - override for job-specific debug display
    }

    #region Protected Implementation Methods

    /// <summary>
    /// Attempts to use Lucid Dreaming for MP regeneration.
    /// </summary>
    protected virtual bool TryLucidDreaming(TContext context)
    {
        var player = context.Player;
        var lucidAction = GetLucidDreamingAction();

        if (!IsLucidDreamingEnabled(context))
            return false;

        if (player.Level < lucidAction.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(lucidAction.ActionId))
            return false;

        if (HasLucidDreaming(context))
            return false;

        if (!ShouldUseLucidDreaming(context))
            return false;

        if (context.ActionService.ExecuteOgcd(lucidAction, player.GameObjectId))
        {
            SetPlannedAction(context, lucidAction.Name);
            SetLucidState(context, "Lucid Dreaming");
            return true;
        }

        return false;
    }

    #endregion
}
