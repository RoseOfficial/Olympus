using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

public sealed class ExcogitationHandler : IHealingHandler
{
    public int Priority => 15;
    public string Name => "Excogitation";

    private static readonly string[] _excogitationAlternatives =
    {
        "Lustrate (immediate heal, same cost)",
        "Save Aetherflow for Indomitability (AoE)",
        "GCD heal (Adloquium for shield)",
    };

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableExcogitation) return;
        if (player.Level < SCHActions.Excogitation.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.Excogitation.ActionId)) return;

        var hasRecitation = context.StatusHelper.HasRecitation(player);
        if (!hasRecitation && context.AetherflowService.CurrentStacks <= config.AetherflowReserve) return;

        var target = context.PartyHelper.FindExcogitationTarget(player);
        if (target == null) return;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);

        if (hpPercent > config.ExcogitationThreshold && !tankBusterImminent) return;

        var action = SCHActions.Excogitation;
        var capturedTarget = target;
        var capturedHpPercent = hpPercent;
        var capturedTankBusterImminent = tankBusterImminent;
        var capturedHasRecitation = hasRecitation;

        scheduler.PushOgcd(AthenaAbilities.Excogitation, target.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                if (!capturedHasRecitation)
                    context.AetherflowService.ConsumeStack();

                var healAmount = action.HealPotency * 10;
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Excogitation";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var shortReason = capturedTankBusterImminent
                        ? $"Excog on {targetName} before tankbuster!"
                        : $"Excog on {targetName} at {capturedHpPercent:P0}";

                    var factors = new[]
                    {
                        $"Target HP: {capturedHpPercent:P0}",
                        $"Threshold: {config.ExcogitationThreshold:P0}",
                        capturedTankBusterImminent ? "Tank buster imminent!" : "No incoming damage predicted",
                        capturedHasRecitation ? "Recitation active (guaranteed crit, free)" : $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                        "Auto-triggers at 50% HP or lower",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Excogitation",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Excogitation on {targetName} at {capturedHpPercent:P0} HP. {(capturedTankBusterImminent ? "Tank buster detected - proactive Excog provides safety net. " : "")}Excog triggers automatically when target drops below 50% HP, providing a 800 potency heal. {(capturedHasRecitation ? "Recitation made this free and guaranteed critical!" : $"Cost 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining).")}",
                        Factors = factors,
                        Alternatives = _excogitationAlternatives,
                        Tip = "Excogitation is SCH's best tank maintenance tool. Apply before damage for automatic healing. Pair with Recitation for massive crit heals!",
                        ConceptId = SchConcepts.ExcogitationUsage,
                        Priority = capturedTankBusterImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });

                    context.TrainingService.RecordConceptApplication(SchConcepts.ExcogitationUsage, wasSuccessful: true);
                }
            });
    }
}
