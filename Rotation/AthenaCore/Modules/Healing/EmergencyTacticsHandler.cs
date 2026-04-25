using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

public sealed class EmergencyTacticsHandler : IHealingHandler
{
    public int Priority => 40;
    public string Name => "EmergencyTactics";

    private static readonly string[] _emergencyTacticsAlternatives =
    {
        "Lustrate (uses Aetherflow)",
        "Wait for shield to break",
        "Use Physick (no shield component)",
    };

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableEmergencyTactics) return;
        if (player.Level < SCHActions.EmergencyTactics.MinLevel) return;
        if (context.StatusHelper.HasEmergencyTactics(player)) return;
        if (!context.ActionService.IsActionReady(SCHActions.EmergencyTactics.ActionId)) return;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null) return;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.EmergencyTacticsThreshold) return;
        if (!context.StatusHelper.HasGalvanize(target)) return;

        var action = SCHActions.EmergencyTactics;
        var capturedTarget = target;
        var capturedHpPercent = hpPercent;

        scheduler.PushOgcd(AthenaAbilities.EmergencyTactics, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Emergency Tactics";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var shortReason = $"Emergency Tactics - {targetName} already shielded";
                    var factors = new[]
                    {
                        $"Target HP: {capturedHpPercent:P0}",
                        $"Threshold: {config.EmergencyTacticsThreshold:P0}",
                        "Target has Galvanize (shield)",
                        "Converts next shield spell to pure heal",
                        "Prevents shield overwrite waste",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Emergency Tactics",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Emergency Tactics before healing {targetName} at {capturedHpPercent:P0}. Target already has Galvanize shield, so using Adloquium would overwrite it (wasting the shield). Emergency Tactics converts the shield portion to healing, getting full value from the spell.",
                        Factors = factors,
                        Alternatives = _emergencyTacticsAlternatives,
                        Tip = "Emergency Tactics prevents shield waste when the target already has a shield. It's also useful when you need raw healing instead of shields after a raidwide.",
                        ConceptId = SchConcepts.EmergencyTacticsUsage,
                        Priority = ExplanationPriority.Normal,
                    });

                    context.TrainingService.RecordConceptApplication(SchConcepts.EmergencyTacticsUsage, wasSuccessful: true);
                }
            });
    }
}
