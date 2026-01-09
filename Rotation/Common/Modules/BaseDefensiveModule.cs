namespace Olympus.Rotation.Common.Modules;

/// <summary>
/// Base defensive module for healer jobs.
/// Handles common patterns like combat checks and oGCD execution.
/// Job-specific implementations override methods for unique defensive abilities.
/// </summary>
/// <typeparam name="TContext">The job-specific context type that implements IHealerRotationContext.</typeparam>
public abstract class BaseDefensiveModule<TContext> : IHealerRotationModule<TContext>
    where TContext : IHealerRotationContext
{
    public virtual int Priority => 20; // After healing, before buffs
    public virtual string Name => "Defensive";

    #region Abstract Methods - Must be implemented by job-specific modules

    /// <summary>
    /// Sets the defensive state debug string.
    /// </summary>
    protected abstract void SetDefensiveState(TContext context, string state);

    /// <summary>
    /// Sets the planned action debug string.
    /// </summary>
    protected abstract void SetPlannedAction(TContext context, string action);

    #endregion

    #region Virtual Methods - Can be overridden for job-specific behavior

    /// <summary>
    /// Override to try job-specific defensive abilities.
    /// Return true if an action was executed, false otherwise.
    /// </summary>
    protected virtual bool TryJobSpecificDefensives(TContext context, bool isMoving) => false;

    /// <summary>
    /// Override to provide party health metrics for defensive decisions.
    /// Default returns (1.0, 1.0, 0) indicating full health party.
    /// </summary>
    protected virtual (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(TContext context)
    {
        return (1f, 1f, 0);
    }

    #endregion

    public virtual bool TryExecute(TContext context, bool isMoving)
    {
        if (!context.InCombat)
            return false;

        if (!context.CanExecuteOgcd)
            return false;

        // Execute job-specific defensives
        if (TryJobSpecificDefensives(context, isMoving))
            return true;

        // Update debug state if no action taken
        var (avgHp, _, injuredCount) = GetPartyHealthMetrics(context);
        SetDefensiveState(context, $"Idle (avg HP {avgHp:P0}, {injuredCount} injured)");

        return false;
    }

    public virtual void UpdateDebugState(TContext context)
    {
        // Base implementation - override for job-specific debug display
    }
}
