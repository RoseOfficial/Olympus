using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class CelestialIntersectionHandler : IHealingHandler
{
    public int Priority => 15;
    public string Name => "CelestialIntersection";

    private static readonly string[] _alternatives =
    {
        "Essential Dignity (emergency heal)",
        "Aspected Benefic (GCD + regen)",
        "Save charge for tank damage",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TryCelestialIntersection(context);

    private bool TryCelestialIntersection(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableCelestialIntersection)
            return false;

        if (player.Level < ASTActions.CelestialIntersection.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.CelestialIntersection.ActionId))
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.CelestialIntersectionThreshold)
            return false;

        var action = ASTActions.CelestialIntersection;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.CelestialIntersectionState = "Used";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var isTank = JobRegistry.IsTank(target.ClassJob.RowId);

                var shortReason = $"Celestial Intersection on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.CelestialIntersectionThreshold:P0}",
                    isTank ? "Tank target - will get shield" : "Non-tank - heal + regen",
                    "2 charges, 30s recharge",
                    "oGCD - weave without GCD clip",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Celestial Intersection",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Celestial Intersection on {targetName} at {hpPercent:P0} HP. {(isTank ? "Tank target receives 400 potency shield (great for tankbusters!)." : "Non-tank target receives 200 potency heal + 15s regen.")} 2 charges with 30s recharge - keep using them to maximize value!",
                    Factors = factors,
                    Alternatives = _alternatives,
                    Tip = "Celestial Intersection is excellent for tanks - the shield helps with auto-attacks and tankbusters. For non-tanks, it's a free oGCD heal + regen. Don't sit on charges!",
                    ConceptId = AstConcepts.CelestialIntersectionUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
