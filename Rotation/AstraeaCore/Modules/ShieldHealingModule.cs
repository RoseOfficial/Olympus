using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Handles shield/mitigation healing for Astrologian: Exaltation.
/// </summary>
public sealed class ShieldHealingModule
{
    private static readonly string[] _exaltationAlternatives =
    {
        "Celestial Intersection (immediate shield)",
        "Essential Dignity (emergency heal)",
        "Save for predictable tankbuster",
    };

    /// <summary>Tries Exaltation. Does not check CanExecuteOgcd.</summary>
    public bool TryOgcd(IAstraeaContext context)
    {
        return TryExaltation(context);
    }

    private bool TryExaltation(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableExaltation)
            return false;

        if (player.Level < ASTActions.Exaltation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Exaltation.ActionId))
            return false;

        var target = context.PartyHelper.FindExaltationTarget(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.ExaltationThreshold)
            return false;

        var action = ASTActions.Exaltation;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            // Exaltation heals after 8 seconds for 500 potency
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.ExaltationState = "Used";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var isTank = JobRegistry.IsTank(target.ClassJob.RowId);

                var shortReason = $"Exaltation on {targetName} - {(isTank ? "tankbuster prep" : "damage reduction")}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.ExaltationThreshold:P0}",
                    "10% damage reduction for 8s",
                    "500 potency heal after 8s",
                    "60s cooldown",
                };

                var alternatives = _exaltationAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Exaltation",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Exaltation on {targetName} at {hpPercent:P0} HP. Provides 10% damage reduction for 8 seconds, then heals for 500 potency. {(isTank ? "Excellent for tankbusters - the mitigation reduces incoming damage, then the delayed heal tops them off!" : "Good defensive utility even on non-tanks during heavy damage phases.")}",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Exaltation is best used proactively on tanks before tankbusters. The 10% mitigation + delayed heal combo is very efficient. Time it so the heal lands when the target actually needs it!",
                    ConceptId = AstConcepts.ExaltationUsage,
                    Priority = isTank ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
