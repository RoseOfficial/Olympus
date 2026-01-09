using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Models.Action;

namespace Olympus.Rotation.Common.Modules;

/// <summary>
/// Base resurrection module for healer jobs.
/// Handles common resurrection logic including Swiftcast + Raise patterns.
/// Job-specific implementations should override methods for unique behavior (e.g., Thin Air for WHM).
/// </summary>
/// <typeparam name="TContext">The job-specific context type that implements IHealerRotationContext.</typeparam>
public abstract class BaseResurrectionModule<TContext> : IHealerRotationModule<TContext>
    where TContext : IHealerRotationContext
{
    public virtual int Priority => 5; // Very high priority - dead party members are useless
    public virtual string Name => "Resurrection";

    #region Abstract Properties - Must be implemented by job-specific modules

    /// <summary>
    /// The raise action for this job (e.g., WHMActions.Raise, SCHActions.Resurrection).
    /// </summary>
    protected abstract ActionDefinition RaiseAction { get; }

    /// <summary>
    /// The Swiftcast action for this job.
    /// </summary>
    protected abstract ActionDefinition SwiftcastAction { get; }

    /// <summary>
    /// MP cost for the raise spell.
    /// </summary>
    protected abstract int RaiseMpCost { get; }

    #endregion

    #region Abstract Methods - Must be implemented by job-specific modules

    /// <summary>
    /// Finds a dead party member that needs raising.
    /// </summary>
    protected abstract IBattleChara? FindDeadPartyMemberNeedingRaise(TContext context);

    /// <summary>
    /// Checks if the player has Swiftcast active.
    /// </summary>
    protected abstract bool HasSwiftcast(TContext context);

    /// <summary>
    /// Sets the raise state in the job-specific debug state.
    /// </summary>
    protected abstract void SetRaiseState(TContext context, string state);

    /// <summary>
    /// Sets the raise target in the job-specific debug state.
    /// </summary>
    protected abstract void SetRaiseTarget(TContext context, string target);

    /// <summary>
    /// Sets the planning state in the job-specific debug state.
    /// </summary>
    protected abstract void SetPlanningState(TContext context, string state);

    /// <summary>
    /// Sets the planned action in the job-specific debug state.
    /// </summary>
    protected abstract void SetPlannedAction(TContext context, string action);

    #endregion

    #region Virtual Methods - Can be overridden for job-specific behavior

    /// <summary>
    /// Override to check if we should wait for a pre-raise buff (e.g., Thin Air for WHM).
    /// Default returns false.
    /// </summary>
    protected virtual bool ShouldWaitForPreRaiseBuff(TContext context) => false;

    /// <summary>
    /// Override to add additional raise success logging.
    /// </summary>
    protected virtual string GetRaiseSuccessNote(TContext context, bool hasSwiftcast)
    {
        return hasSwiftcast ? " (Swiftcast)" : "";
    }

    #endregion

    public bool TryExecute(TContext context, bool isMoving)
    {
        // oGCD: Swiftcast for pending raise
        if (context.CanExecuteOgcd && TrySwiftcastForRaise(context))
            return true;

        // GCD: Execute Raise
        if (context.CanExecuteGcd && TryExecuteRaise(context, isMoving))
        {
            SetPlanningState(context, "Raise");
            return true;
        }

        return false;
    }

    public virtual void UpdateDebugState(TContext context)
    {
        // Check for dead party members
        var deadMember = FindDeadPartyMemberNeedingRaise(context);

        if (deadMember != null)
        {
            SetRaiseState(context, "Dead member found");
            SetRaiseTarget(context, deadMember.Name?.TextValue ?? "Unknown");
        }
        else
        {
            SetRaiseState(context, "None needed");
            SetRaiseTarget(context, "");
        }
    }

    #region Protected Implementation Methods

    protected virtual bool TryExecuteRaise(TContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Resurrection.EnableRaise)
        {
            SetRaiseState(context, "Disabled");
            return false;
        }

        if (player.Level < RaiseAction.MinLevel)
        {
            SetRaiseState(context, $"Level {player.Level} < {RaiseAction.MinLevel}");
            return false;
        }

        var mpPercent = (float)player.CurrentMp / player.MaxMp;
        if (mpPercent < config.Resurrection.RaiseMpThreshold)
        {
            SetRaiseState(context, $"MP {mpPercent:P0} < {config.Resurrection.RaiseMpThreshold:P0}");
            return false;
        }

        if (player.CurrentMp < RaiseMpCost)
        {
            SetRaiseState(context, $"MP {player.CurrentMp} < {RaiseMpCost}");
            return false;
        }

        var target = FindDeadPartyMemberNeedingRaise(context);
        if (target is null)
        {
            SetRaiseState(context, "No target");
            SetRaiseTarget(context, "None");
            return false;
        }

        var targetName = target.Name?.TextValue ?? "Unknown";
        SetRaiseTarget(context, targetName);

        var hasSwiftcast = HasSwiftcast(context);

        if (hasSwiftcast)
        {
            if (ShouldWaitForPreRaiseBuff(context))
            {
                SetRaiseState(context, "Waiting for buff");
                return false;
            }

            SetRaiseState(context, "Swiftcast Raise");
            var success = context.ActionService.ExecuteGcd(RaiseAction, target.GameObjectId);
            if (success)
            {
                var note = GetRaiseSuccessNote(context, hasSwiftcast: true);
                SetPlannedAction(context, $"{RaiseAction.Name}{note}");
                context.ActionTracker.LogAttempt(RaiseAction.ActionId, targetName, 0, Models.ActionResult.Success, player.Level);
            }
            return success;
        }

        // Hardcast Raise (if allowed and not moving)
        if (config.Resurrection.AllowHardcastRaise && !isMoving)
        {
            var swiftcastCooldown = context.ActionService.GetCooldownRemaining(SwiftcastAction.ActionId);

            if (swiftcastCooldown > 10f)
            {
                if (ShouldWaitForPreRaiseBuff(context))
                {
                    SetRaiseState(context, "Waiting for buff");
                    return false;
                }

                SetRaiseState(context, "Hardcast Raise");
                var success = context.ActionService.ExecuteGcd(RaiseAction, target.GameObjectId);
                if (success)
                {
                    var note = GetRaiseSuccessNote(context, hasSwiftcast: false);
                    SetPlannedAction(context, $"{RaiseAction.Name} (Hardcast){note}");
                    context.ActionTracker.LogAttempt(RaiseAction.ActionId, targetName, 0, Models.ActionResult.Success, player.Level);
                }
                return success;
            }
            else
            {
                SetRaiseState(context, $"Waiting for Swiftcast ({swiftcastCooldown:F1}s)");
            }
        }
        else if (!hasSwiftcast && !config.Resurrection.AllowHardcastRaise)
        {
            SetRaiseState(context, "No Swiftcast (hardcast disabled)");
        }
        else if (isMoving)
        {
            SetRaiseState(context, "Moving (can't hardcast)");
        }

        return false;
    }

    protected virtual bool TrySwiftcastForRaise(TContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Resurrection.EnableRaise)
            return false;

        if (player.Level < SwiftcastAction.MinLevel)
            return false;

        if (HasSwiftcast(context))
            return false;

        var deadMember = FindDeadPartyMemberNeedingRaise(context);
        if (deadMember is null)
            return false;

        if (player.CurrentMp < RaiseMpCost)
            return false;

        if (!context.ActionService.IsActionReady(SwiftcastAction.ActionId))
            return false;

        return context.ActionService.ExecuteOgcd(SwiftcastAction, player.GameObjectId);
    }

    #endregion
}
