using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Handles Excogitation for Scholar. Priority 15 in the oGCD list.
/// Costs 1 Aetherflow stack (free with Recitation).
/// </summary>
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

    public bool TryExecute(IAthenaContext context, bool isMoving)
        => TryExcogitation(context);

    private bool TryExcogitation(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableExcogitation)
            return false;

        if (player.Level < SCHActions.Excogitation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Excogitation.ActionId))
            return false;

        // Skip if no Aetherflow (unless Recitation is active)
        if (!context.StatusHelper.HasRecitation(player))
        {
            if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
                return false;
        }

        var target = context.PartyHelper.FindExcogitationTarget(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        // Timeline-aware: proactively use before tank busters
        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration,
            out _);

        // Use if HP is low OR tank buster is imminent
        if (hpPercent > config.ExcogitationThreshold && !tankBusterImminent)
            return false;

        var action = SCHActions.Excogitation;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            var hasRecitation = context.StatusHelper.HasRecitation(player);
            if (!hasRecitation)
                context.AetherflowService.ConsumeStack();

            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Excogitation";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var shortReason = tankBusterImminent
                    ? $"Excog on {targetName} before tankbuster!"
                    : $"Excog on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.ExcogitationThreshold:P0}",
                    tankBusterImminent ? "Tank buster imminent!" : "No incoming damage predicted",
                    hasRecitation ? "Recitation active (guaranteed crit, free)" : $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                    "Auto-triggers at 50% HP or lower",
                };

                var alternatives = _excogitationAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Excogitation",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Excogitation on {targetName} at {hpPercent:P0} HP. {(tankBusterImminent ? "Tank buster detected - proactive Excog provides safety net. " : "")}Excog triggers automatically when target drops below 50% HP, providing a 800 potency heal. {(hasRecitation ? "Recitation made this free and guaranteed critical!" : $"Cost 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining).")}",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Excogitation is SCH's best tank maintenance tool. Apply before damage for automatic healing. Pair with Recitation for massive crit heals!",
                    ConceptId = SchConcepts.ExcogitationUsage,
                    Priority = tankBusterImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });

                context.TrainingService.RecordConceptApplication(SchConcepts.ExcogitationUsage, wasSuccessful: true);
            }

            return true;
        }

        return false;
    }
}
