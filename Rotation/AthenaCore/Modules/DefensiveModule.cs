using System.Numerics;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific defensive module.
/// Extends base defensive logic with Expedient and Deployment Tactics for shield spreading.
/// </summary>
public sealed class DefensiveModule : BaseDefensiveModule<AthenaContext>, IAthenaModule
{
    #region Base Class Overrides - Debug State

    protected override void SetDefensiveState(AthenaContext context, string state) =>
        context.Debug.PlanningState = state;

    protected override void SetPlannedAction(AthenaContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(AthenaContext context) =>
        context.PartyHelper.CalculatePartyHealthMetrics(context.Player);

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// SCH-specific defensives: Expedient and Deployment Tactics.
    /// </summary>
    protected override bool TryJobSpecificDefensives(AthenaContext context, bool isMoving)
    {
        // Priority 1: Expedient (party-wide mitigation + speed)
        if (TryExpedient(context))
            return true;

        // Priority 2: Deployment Tactics (spread shield to party)
        if (TryDeploymentTactics(context))
            return true;

        return false;
    }

    #endregion

    #region SCH-Specific Methods

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
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
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

        if (context.ActionService.ExecuteOgcd(SCHActions.Expedient, player.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.Expedient.Name);
            SetDefensiveState(context, "Expedient");
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

        if (context.ActionService.ExecuteOgcd(SCHActions.DeploymentTactics, deployTarget.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.DeploymentTactics.Name);
            SetDefensiveState(context, $"Deploy ({beneficiaries} targets)");
            return true;
        }

        return false;
    }

    #endregion
}
