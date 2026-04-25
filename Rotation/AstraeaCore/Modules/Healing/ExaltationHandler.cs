using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class ExaltationHandler : IHealingHandler
{
    public int Priority => 25;
    public string Name => "Exaltation";

    private static readonly string[] _alternatives =
    {
        "Celestial Intersection (immediate shield)",
        "Essential Dignity (emergency heal)",
        "Save for predictable tankbuster",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableExaltation) return;
        if (player.Level < ASTActions.Exaltation.MinLevel) return;
        if (!context.ActionService.IsActionReady(ASTActions.Exaltation.ActionId)) return;

        var target = context.PartyHelper.FindExaltationTarget(player);
        if (target == null) return;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.ExaltationThreshold) return;

        var action = ASTActions.Exaltation;
        var capturedTarget = target;
        var capturedHpPercent = hpPercent;

        scheduler.PushOgcd(AstraeaAbilities.Exaltation, target.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                var healAmount = action.HealPotency * 10;
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                context.Debug.PlannedAction = action.Name;
                context.Debug.ExaltationState = "Used";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var isTank = JobRegistry.IsTank(capturedTarget.ClassJob.RowId);
                    var shortReason = $"Exaltation on {targetName} - {(isTank ? "tankbuster prep" : "damage reduction")}";
                    var factors = new[]
                    {
                        $"Target HP: {capturedHpPercent:P0}",
                        $"Threshold: {config.ExaltationThreshold:P0}",
                        "10% damage reduction for 8s",
                        "500 potency heal after 8s",
                        "60s cooldown",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Exaltation",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Exaltation on {targetName} at {capturedHpPercent:P0} HP. Provides 10% damage reduction for 8 seconds, then heals for 500 potency. {(isTank ? "Excellent for tankbusters - the mitigation reduces incoming damage, then the delayed heal tops them off!" : "Good defensive utility even on non-tanks during heavy damage phases.")}",
                        Factors = factors,
                        Alternatives = _alternatives,
                        Tip = "Exaltation is best used proactively on tanks before tankbusters. The 10% mitigation + delayed heal combo is very efficient. Time it so the heal lands when the target actually needs it!",
                        ConceptId = AstConcepts.ExaltationUsage,
                        Priority = isTank ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });

                    context.TrainingService?.RecordConceptApplication(AstConcepts.ExaltationUsage, wasSuccessful: true, isTank ? "Tankbuster mitigation" : "Damage reduction applied");
                }
            });
    }
}
