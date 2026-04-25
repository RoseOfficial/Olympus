using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

public sealed class ProtractionHandler : IHealingHandler
{
    public int Priority => 35;
    public string Name => "Protraction";

    private static readonly string[] _protractionAlternatives =
    {
        "Lustrate (direct heal)",
        "Excogitation (proactive)",
        "Adloquium (shield + heal)",
    };

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableProtraction) return;
        if (player.Level < SCHActions.Protraction.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.Protraction.ActionId)) return;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null) return;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.ProtractionThreshold) return;

        var action = SCHActions.Protraction;
        var capturedTarget = target;
        var capturedHpPercent = hpPercent;

        scheduler.PushOgcd(AthenaAbilities.Protraction, target.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                var healAmount = 1000;
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Protraction";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var shortReason = $"Protraction on {targetName} at {capturedHpPercent:P0}";
                    var factors = new[]
                    {
                        $"Target HP: {capturedHpPercent:P0}",
                        $"Threshold: {config.ProtractionThreshold:P0}",
                        "Increases max HP by 10%",
                        "Restores HP equal to the increase",
                        "10s duration, enhances healing received",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Protraction",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Protraction on {targetName} at {capturedHpPercent:P0} HP. Protraction increases max HP by 10% and heals for the same amount. The 10s buff also increases healing received, making follow-up heals more effective. Free oGCD with no resource cost.",
                        Factors = factors,
                        Alternatives = _protractionAlternatives,
                        Tip = "Protraction is a free oGCD that effectively heals and buffs healing received. Great before big damage on a single target!",
                        ConceptId = SchConcepts.EmergencyHealing,
                        Priority = ExplanationPriority.Normal,
                    });

                    context.TrainingService.RecordConceptApplication(SchConcepts.EmergencyHealing, wasSuccessful: true);
                }
            });
    }
}
