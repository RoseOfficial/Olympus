using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class SynastryHandler : IHealingHandler
{
    public int Priority => 45;
    public string Name => "Synastry";

    private static readonly string[] _alternatives =
    {
        "Direct heal the target instead",
        "Save for tankbuster sequences",
        "Use on different target",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TrySynastry(context);

    private bool TrySynastry(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableSynastry)
            return false;

        if (player.Level < ASTActions.Synastry.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Synastry.ActionId))
            return false;

        // Already have Synastry active
        if (context.HasSynastry)
            return false;

        var target = context.PartyHelper.FindSynastryTarget(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.SynastryThreshold)
            return false;

        var action = ASTActions.Synastry;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target - Synastry mirrors heals to this target
            var healAmount = 1000; // Synastry itself doesn't heal, but reserves the target for coordination
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.SynastryState = "Active";
            context.Debug.SynastryTarget = target.Name?.TextValue ?? string.Empty;

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var isTank = JobRegistry.IsTank(target.ClassJob.RowId);

                var shortReason = $"Synastry on {targetName} - sustained healing phase";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.SynastryThreshold:P0}",
                    isTank ? "Tank target - great for sustained tankbuster recovery" : "Non-tank target",
                    "40% of single-target heals mirrored",
                    "20s duration, 120s cooldown",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Synastry",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Synastry linked to {targetName} at {hpPercent:P0} HP. For the next 20 seconds, 40% of all your single-target heals will be mirrored to {targetName}. {(isTank ? "Excellent for sustained tank healing - heal anyone and the tank gets topped off too!" : "Useful during heavy damage phases.")}",
                    Factors = factors,
                    Alternatives = _alternatives,
                    Tip = "Synastry is great when you need to heal multiple people but want to keep the tank topped. Link it to the tank, then heal whoever needs it - the tank gets healed too! Best for sustained damage phases.",
                    ConceptId = AstConcepts.SynastryUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
