using System;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific defensive module.
/// Extends base defensive logic with Expedient and Deployment Tactics for shield spreading.
/// </summary>
public sealed class DefensiveModule : BaseDefensiveModule<IAthenaContext>, IAthenaModule
{
    // Training explanation arrays
    private static readonly string[] _expedientAlternatives =
    {
        "Sacred Soil (uses Aetherflow)",
        "Fey Illumination (5% magic mit)",
        "Save for movement-heavy phase",
    };

    private static readonly string[] _deploymentTacticsAlternatives =
    {
        "Succor (direct party shield)",
        "Wait for better crit shield",
        "Sacred Soil (mitigation instead)",
    };

    #region Base Class Overrides - Debug State

    protected override void SetDefensiveState(IAthenaContext context, string state) =>
        context.Debug.PlanningState = state;

    protected override void SetPlannedAction(IAthenaContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(IAthenaContext context) =>
        context.PartyHelper.CalculatePartyHealthMetrics(context.Player);

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// SCH-specific defensives: Expedient and Deployment Tactics.
    /// </summary>
    protected override bool TryJobSpecificDefensives(IAthenaContext context, bool isMoving)
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

    private bool TryExpedient(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableExpedient)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            SetDefensiveState(context, "Expedient skipped (remote mit)");
            return false;
        }

        // Burst awareness: Delay mitigations during burst windows unless emergency
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var (avgHpCheck, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpCheck > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                SetDefensiveState(context, $"Expedient delayed (burst active)");
                return false;
            }
        }

        if (player.Level < SCHActions.Expedient.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Expedient.ActionId))
            return false;

        // Check party health - use when party is taking significant damage
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Proactive raidwide path: deploy before predicted raidwides even if HP threshold not yet met
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        if (avgHp > config.ExpedientThreshold && !raidwideImminent)
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
            partyCoord?.OnCooldownUsed(SCHActions.Expedient.ActionId, 120_000);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Expedient - party HP {avgHp:P0}, {membersInRange} in range";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Threshold: {config.ExpedientThreshold:P0}",
                    $"Members in range: {membersInRange}",
                    "10% damage reduction (20s)",
                    "Sprint effect for movement",
                };

                var alternatives = _expedientAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = SCHActions.Expedient.ActionId,
                    ActionName = "Expedient",
                    Category = "Defensive",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Expedient for {membersInRange} party members at {avgHp:P0} average HP. Provides 10% damage reduction and sprint effect for 20 seconds. Great for both mitigation and movement-heavy mechanics!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Expedient is SCH's unique party mitigation + mobility tool. Use it for raidwides or movement-heavy mechanics. The sprint doesn't break on damage!",
                    ConceptId = SchConcepts.ExpedientUsage,
                    Priority = ExplanationPriority.High,
                });

                context.TrainingService.RecordConceptApplication(SchConcepts.ExpedientUsage, wasSuccessful: true);
            }

            return true;
        }

        return false;
    }

    private bool TryDeploymentTactics(IAthenaContext context)
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

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = deployTarget.Name?.TextValue ?? "Unknown";
                var shortReason = $"Deployment Tactics from {targetName} to {beneficiaries} allies";

                var factors = new[]
                {
                    $"Shield source: {targetName}",
                    $"Targets receiving shield: {beneficiaries}",
                    $"Min targets setting: {config.DeploymentMinTargets}",
                    "Spreads Galvanize to nearby allies",
                    "Critical shields spread crit value!",
                };

                var alternatives = _deploymentTacticsAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = SCHActions.DeploymentTactics.ActionId,
                    ActionName = "Deployment Tactics",
                    Category = "Defensive",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Deployment Tactics spread Galvanize from {targetName} to {beneficiaries} nearby party members. This is highly efficient when spreading a crit Adloquium (crit shield value spreads too!). Great for pre-shielding the party before raidwides.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "For maximum value, Adlo a target and hope for crit (Catalyze), then Deploy to spread the massive shield to the party. Best used before predictable raidwides!",
                    ConceptId = SchConcepts.DeploymentTactics,
                    Priority = ExplanationPriority.Normal,
                });

                context.TrainingService.RecordConceptApplication(SchConcepts.DeploymentTactics, wasSuccessful: true);
            }

            return true;
        }

        return false;
    }

    #endregion
}
