using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class AspectedBeneficHandler : IHealingHandler
{
    public int Priority => 40;
    public string Name => "AspectedBenefic";

    private static readonly string[] _alternatives =
    {
        "Essential Dignity (oGCD, emergency)",
        "Celestial Intersection (oGCD)",
        "Benefic II (higher potency, has cast time)",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TryAspectedBenefic(context); // instant cast — no isMoving guard

    private bool TryAspectedBenefic(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableAspectedBenefic)
            return false;

        if (player.Level < ASTActions.AspectedBenefic.MinLevel)
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.AspectedBeneficThreshold)
            return false;

        // Skip if target already has Aspected Benefic regen
        if (context.StatusHelper.HasAspectedBenefic(target))
            return false;

        var action = ASTActions.AspectedBenefic;
        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.SingleHealState = "Aspected Benefic";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";

                var shortReason = $"Aspected Benefic on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.AspectedBeneficThreshold:P0}",
                    "Instant cast (can use while moving!)",
                    "250 potency heal + 15s regen",
                    "Target didn't have regen already",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Aspected Benefic",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Aspected Benefic on {targetName} at {hpPercent:P0} HP. Instant cast GCD heal (250 potency) plus a 15s regen. Great for healing on the move! Target didn't already have the regen, so full value.",
                    Factors = factors,
                    Alternatives = _alternatives,
                    Tip = "Aspected Benefic is instant cast - your go-to heal while moving! The regen is great value. Check that the target doesn't already have the regen before refreshing.",
                    ConceptId = AstConcepts.AspectedBeneficUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
