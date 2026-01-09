using System.Numerics;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Context;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Handles defensive and mitigation abilities for Scholar.
/// Includes Expedient, Deployment Tactics, and proactive shielding.
/// </summary>
public sealed class DefensiveModule : IAthenaModule
{
    public int Priority => 20; // After healing, before buffs
    public string Name => "Defensive";

    public bool TryExecute(AthenaContext context, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!context.InCombat)
            return false;

        if (!context.CanExecuteOgcd)
            return false;

        // Priority 1: Expedient (party-wide mitigation + speed)
        if (TryExpedient(context))
            return true;

        // Priority 2: Deployment Tactics (spread shield to party)
        if (TryDeploymentTactics(context))
            return true;

        return false;
    }

    public void UpdateDebugState(AthenaContext context)
    {
        // Defensive state tracking
    }

    private bool TryExpedient(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableExpedient)
            return false;

        if (player.Level < SCHActions.Expedient.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Expedient.ActionId))
            return false;

        // Check party health - use when party is taking significant damage
        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.ExpedientThreshold)
            return false;

        // Need multiple party members in range
        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= SCHActions.Expedient.RadiusSquared)
                membersInRange++;
        }

        if (membersInRange < 3)
            return false;

        var action = SCHActions.Expedient;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Expedient";
            return true;
        }

        return false;
    }

    private bool TryDeploymentTactics(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableDeploymentTactics)
            return false;

        if (player.Level < SCHActions.DeploymentTactics.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.DeploymentTactics.ActionId))
            return false;

        // Find a target with Galvanize to spread
        var deployTarget = context.PartyHelper.FindDeploymentTarget(player);
        if (deployTarget == null)
            return false;

        // Count party members who would benefit (don't have shields)
        int beneficiaries = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (member.EntityId == deployTarget.EntityId)
                continue;

            if (Vector3.DistanceSquared(deployTarget.Position, member.Position) > SCHActions.DeploymentTactics.RadiusSquared)
                continue;

            if (!context.StatusHelper.HasGalvanize(member))
                beneficiaries++;
        }

        if (beneficiaries < config.DeploymentMinTargets)
            return false;

        var action = SCHActions.DeploymentTactics;
        if (context.ActionService.ExecuteOgcd(action, deployTarget.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = $"Deploy ({beneficiaries} targets)";
            return true;
        }

        return false;
    }
}
