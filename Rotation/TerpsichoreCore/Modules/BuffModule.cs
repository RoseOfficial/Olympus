using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.TerpsichoreCore.Context;

namespace Olympus.Rotation.TerpsichoreCore.Modules;

/// <summary>
/// Handles Dancer dance execution, buff management, and oGCD optimization.
/// Manages dances (Standard/Technical Step), Devilment, Flourish,
/// and oGCD damage (Fan Dance I/II/III/IV).
/// </summary>
public sealed class BuffModule : ITerpsichoreModule
{
    public int Priority => 20; // Higher priority than damage
    public string Name => "Buff";

    public bool TryExecute(ITerpsichoreContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Priority 1: Execute dance steps when dancing
        if (context.IsDancing)
        {
            if (TryExecuteDanceStep(context))
                return true;
        }

        // oGCDs require weave windows
        if (!context.CanExecuteOgcd)
        {
            context.Debug.BuffState = "oGCD not ready";
            return false;
        }

        // Find target for targeted oGCDs
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.BuffState = "No target";
            return false;
        }

        // Priority 2: Technical Step (2-min burst window opener)
        if (TryTechnicalStep(context))
            return true;

        // Priority 3: Devilment (weave after Technical Finish)
        if (TryDevilment(context))
            return true;

        // Priority 4: Standard Step (30s CD, use on cooldown)
        if (TryStandardStep(context))
            return true;

        // Priority 5: Flourish (during burst, grants all procs)
        if (TryFlourish(context))
            return true;

        // Priority 6: Fan Dance IV (Fourfold proc, use during burst)
        if (TryFanDanceIV(context, target))
            return true;

        // Priority 7: Fan Dance III (Threefold proc, use before I/II)
        if (TryFanDanceIII(context, target))
            return true;

        // Priority 8: Fan Dance I/II (dump feathers at 4 or during burst)
        if (TryFanDance(context, target))
            return true;

        context.Debug.BuffState = "No buff action";
        return false;
    }

    public void UpdateDebugState(ITerpsichoreContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Dance Execution

    private bool TryExecuteDanceStep(ITerpsichoreContext context)
    {
        var player = context.Player;
        var currentStep = context.CurrentStep;

        // Get the action for the current step
        var stepAction = DNCActions.GetStepAction(currentStep);
        if (stepAction == null)
        {
            // No step to execute, try finish
            return TryDanceFinish(context);
        }

        // Execute the step - dance steps are GCDs with 1s recast
        if (!context.CanExecuteGcd)
        {
            context.Debug.BuffState = $"Step {currentStep} - waiting for GCD";
            return false;
        }

        if (context.ActionService.ExecuteGcd(stepAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = stepAction.Name;
            context.Debug.BuffState = $"Dance step: {stepAction.Name}";
            return true;
        }

        return false;
    }

    private bool TryDanceFinish(ITerpsichoreContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine which finish based on step count
        // Standard Step = 2 steps, Technical Step = 4 steps
        var completedSteps = context.StepIndex;

        // Check if we've completed all required steps
        // Technical Step requires 4 steps
        if (completedSteps >= 4 && level >= DNCActions.TechnicalFinish.MinLevel)
        {
            if (context.ActionService.IsActionReady(DNCActions.TechnicalFinish.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DNCActions.TechnicalFinish, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.TechnicalFinish.Name;
                    context.Debug.BuffState = "Technical Finish";
                    return true;
                }
            }
        }

        // Standard Step requires 2 steps
        if (completedSteps >= 2)
        {
            if (context.ActionService.IsActionReady(DNCActions.StandardFinish.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DNCActions.StandardFinish, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.StandardFinish.Name;
                    context.Debug.BuffState = "Standard Finish";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Dance Initiation

    private bool TryTechnicalStep(ITerpsichoreContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.TechnicalStep.MinLevel)
            return false;

        // Don't start if already dancing
        if (context.IsDancing)
            return false;

        // Check if Technical Step is ready
        if (!context.ActionService.IsActionReady(DNCActions.TechnicalStep.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DNCActions.TechnicalStep, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.TechnicalStep.Name;
            context.Debug.BuffState = "Technical Step";
            return true;
        }

        return false;
    }

    private bool TryStandardStep(ITerpsichoreContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.StandardStep.MinLevel)
            return false;

        // Don't start if already dancing
        if (context.IsDancing)
            return false;

        // Check if Standard Step is ready
        if (!context.ActionService.IsActionReady(DNCActions.StandardStep.ActionId))
            return false;

        // Don't use Standard Step if Technical Step is coming up soon (within 5s)
        if (level >= DNCActions.TechnicalStep.MinLevel)
        {
            var techCd = context.ActionService.GetCooldownRemaining(DNCActions.TechnicalStep.ActionId);
            if (techCd > 0 && techCd < 5f)
            {
                context.Debug.BuffState = "Holding Standard for Technical";
                return false;
            }
        }

        if (context.ActionService.ExecuteOgcd(DNCActions.StandardStep, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.StandardStep.Name;
            context.Debug.BuffState = "Standard Step";
            return true;
        }

        return false;
    }

    #endregion

    #region Burst Buffs

    private bool TryDevilment(ITerpsichoreContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.Devilment.MinLevel)
            return false;

        // Don't reapply if already active
        if (context.HasDevilment)
            return false;

        // Use after Technical Finish for optimal burst
        // Or if Technical Step not available
        bool shouldUse = context.HasTechnicalFinish ||
                         level < DNCActions.TechnicalStep.MinLevel ||
                         !context.ActionService.IsActionReady(DNCActions.TechnicalStep.ActionId);

        if (!shouldUse)
        {
            context.Debug.BuffState = "Waiting for Technical Finish";
            return false;
        }

        if (!context.ActionService.IsActionReady(DNCActions.Devilment.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DNCActions.Devilment, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Devilment.Name;
            context.Debug.BuffState = "Devilment";
            return true;
        }

        return false;
    }

    private bool TryFlourish(ITerpsichoreContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.Flourish.MinLevel)
            return false;

        // Don't use during dance
        if (context.IsDancing)
            return false;

        // Use during burst window (Devilment or Technical active)
        bool shouldUse = context.HasDevilment || context.HasTechnicalFinish;

        // Or use if buffs won't be up for a while
        if (!shouldUse)
        {
            var devilmentCd = context.ActionService.GetCooldownRemaining(DNCActions.Devilment.ActionId);
            shouldUse = devilmentCd > 30f;
        }

        if (!shouldUse)
            return false;

        // Don't overcap procs - wait if we have symmetry/flow
        if (context.HasSilkenSymmetry || context.HasSilkenFlow)
        {
            context.Debug.BuffState = "Consume procs before Flourish";
            return false;
        }

        if (!context.ActionService.IsActionReady(DNCActions.Flourish.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DNCActions.Flourish, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Flourish.Name;
            context.Debug.BuffState = "Flourish";
            return true;
        }

        return false;
    }

    #endregion

    #region Fan Dance oGCDs

    private bool TryFanDanceIV(ITerpsichoreContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.FanDanceIV.MinLevel)
            return false;

        if (!context.HasFourfoldFanDance)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.FanDanceIV.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DNCActions.FanDanceIV, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.FanDanceIV.Name;
            context.Debug.BuffState = "Fan Dance IV";
            return true;
        }

        return false;
    }

    private bool TryFanDanceIII(ITerpsichoreContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.FanDanceIII.MinLevel)
            return false;

        if (!context.HasThreefoldFanDance)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.FanDanceIII.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DNCActions.FanDanceIII, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.FanDanceIII.Name;
            context.Debug.BuffState = "Fan Dance III";
            return true;
        }

        return false;
    }

    private bool TryFanDance(ITerpsichoreContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.FanDance.MinLevel)
            return false;

        if (context.Feathers == 0)
            return false;

        // Dump at 4 feathers to prevent overcap, or during burst
        bool shouldUse = context.Feathers >= 4 ||
                         context.HasDevilment ||
                         context.HasTechnicalFinish;

        // Hold 3 for burst if not in burst window
        if (!shouldUse && context.Feathers <= 3)
        {
            context.Debug.BuffState = $"Holding feathers ({context.Feathers}/4)";
            return false;
        }

        // Count enemies for AoE decision
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // Use Fan Dance II for AoE (3+ targets)
        if (enemyCount >= 3 && level >= DNCActions.FanDanceII.MinLevel)
        {
            if (context.ActionService.IsActionReady(DNCActions.FanDanceII.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(DNCActions.FanDanceII, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.FanDanceII.Name;
                    context.Debug.BuffState = $"Fan Dance II ({context.Feathers} feathers)";
                    return true;
                }
            }
        }

        // Single target Fan Dance I
        if (!context.ActionService.IsActionReady(DNCActions.FanDance.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DNCActions.FanDance, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.FanDance.Name;
            context.Debug.BuffState = $"Fan Dance ({context.Feathers} feathers)";
            return true;
        }

        return false;
    }

    #endregion
}
