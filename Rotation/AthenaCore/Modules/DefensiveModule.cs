using System;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific defensive module (scheduler-driven).
/// Handles Expedient (party mit + sprint) and Deployment Tactics (shield spread).
/// </summary>
public sealed class DefensiveModule : BaseDefensiveModule<IAthenaContext>, IAthenaModule
{
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

    protected override void SetDefensiveState(IAthenaContext context, string state) =>
        context.Debug.PlanningState = state;

    protected override void SetPlannedAction(IAthenaContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(IAthenaContext context) =>
        context.PartyHelper.CalculatePartyHealthMetrics(context.Player);

    protected override bool TryJobSpecificDefensives(IAthenaContext context, bool isMoving) => false;

    public override bool TryExecute(IAthenaContext context, bool isMoving) => false;

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) return;

        TryPushExpedient(context, scheduler);
        TryPushDeploymentTactics(context, scheduler);
    }

    private void TryPushExpedient(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableExpedient) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            SetDefensiveState(context, "Expedient skipped (remote mit)");
            return;
        }

        if (coordConfig.EnableHealerBurstAwareness && coordConfig.DelayMitigationsDuringBurst && partyCoord != null)
        {
            var (avgHpCheck, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpCheck > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                SetDefensiveState(context, "Expedient delayed (burst active)");
                return;
            }
        }

        if (player.Level < SCHActions.Expedient.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.Expedient.ActionId)) return;

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);
        if (avgHp > config.ExpedientThreshold && !raidwideImminent) return;

        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= SCHActions.Expedient.RadiusSquared)
                membersInRange++;
        }
        if (membersInRange < 3) return;

        var capturedAvgHp = avgHp;
        var capturedMembersInRange = membersInRange;

        scheduler.PushOgcd(AthenaAbilities.Expedient, player.GameObjectId, priority: 75,
            onDispatched: _ =>
            {
                SetPlannedAction(context, SCHActions.Expedient.Name);
                SetDefensiveState(context, "Expedient");
                partyCoord?.OnCooldownUsed(SCHActions.Expedient.ActionId, 120_000);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Expedient - party HP {capturedAvgHp:P0}, {capturedMembersInRange} in range";
                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Threshold: {config.ExpedientThreshold:P0}",
                        $"Members in range: {capturedMembersInRange}",
                        "10% damage reduction (20s)",
                        "Sprint effect for movement",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = SCHActions.Expedient.ActionId,
                        ActionName = "Expedient",
                        Category = "Defensive",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Expedient for {capturedMembersInRange} party members at {capturedAvgHp:P0} average HP. Provides 10% damage reduction and sprint effect for 20 seconds. Great for both mitigation and movement-heavy mechanics!",
                        Factors = factors,
                        Alternatives = _expedientAlternatives,
                        Tip = "Expedient is SCH's unique party mitigation + mobility tool. Use it for raidwides or movement-heavy mechanics. The sprint doesn't break on damage!",
                        ConceptId = SchConcepts.ExpedientUsage,
                        Priority = ExplanationPriority.High,
                    });

                    context.TrainingService.RecordConceptApplication(SchConcepts.ExpedientUsage, wasSuccessful: true);
                }
            });
    }

    private void TryPushDeploymentTactics(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableDeploymentTactics) return;
        if (player.Level < SCHActions.DeploymentTactics.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.DeploymentTactics.ActionId)) return;

        var deployTarget = context.PartyHelper.FindDeploymentTarget(player);
        if (deployTarget == null) return;

        int beneficiaries = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (member.EntityId == deployTarget.EntityId) continue;
            if (Vector3.DistanceSquared(deployTarget.Position, member.Position) > SCHActions.DeploymentTactics.RadiusSquared) continue;
            if (!context.StatusHelper.HasGalvanize(member)) beneficiaries++;
        }
        if (beneficiaries < config.DeploymentMinTargets) return;

        var capturedTarget = deployTarget;
        var capturedBeneficiaries = beneficiaries;

        scheduler.PushOgcd(AthenaAbilities.DeploymentTactics, deployTarget.GameObjectId, priority: 80,
            onDispatched: _ =>
            {
                SetPlannedAction(context, SCHActions.DeploymentTactics.Name);
                SetDefensiveState(context, $"Deploy ({capturedBeneficiaries} targets)");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var shortReason = $"Deployment Tactics from {targetName} to {capturedBeneficiaries} allies";
                    var factors = new[]
                    {
                        $"Shield source: {targetName}",
                        $"Targets receiving shield: {capturedBeneficiaries}",
                        $"Min targets setting: {config.DeploymentMinTargets}",
                        "Spreads Galvanize to nearby allies",
                        "Critical shields spread crit value!",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = SCHActions.DeploymentTactics.ActionId,
                        ActionName = "Deployment Tactics",
                        Category = "Defensive",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Deployment Tactics spread Galvanize from {targetName} to {capturedBeneficiaries} nearby party members. This is highly efficient when spreading a crit Adloquium (crit shield value spreads too!). Great for pre-shielding the party before raidwides.",
                        Factors = factors,
                        Alternatives = _deploymentTacticsAlternatives,
                        Tip = "For maximum value, Adlo a target and hope for crit (Catalyze), then Deploy to spread the massive shield to the party. Best used before predictable raidwides!",
                        ConceptId = SchConcepts.DeploymentTactics,
                        Priority = ExplanationPriority.Normal,
                    });

                    context.TrainingService.RecordConceptApplication(SchConcepts.DeploymentTactics, wasSuccessful: true);
                }
            });
    }
}
