using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Handles Protraction for Scholar. Priority 35 in the oGCD list.
/// No Aetherflow cost. Increases max HP and restores the same amount.
/// </summary>
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

    public bool TryExecute(IAthenaContext context, bool isMoving)
        => TryProtraction(context);

    private bool TryProtraction(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableProtraction)
            return false;

        if (player.Level < SCHActions.Protraction.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Protraction.ActionId))
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.ProtractionThreshold)
            return false;

        var action = SCHActions.Protraction;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            // Protraction increases max HP and heals by 10% - estimate as a moderate heal
            var healAmount = 1000; // Rough estimate for 10% max HP heal
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Protraction";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var shortReason = $"Protraction on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.ProtractionThreshold:P0}",
                    "Increases max HP by 10%",
                    "Restores HP equal to the increase",
                    "10s duration, enhances healing received",
                };

                var alternatives = _protractionAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Protraction",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Protraction on {targetName} at {hpPercent:P0} HP. Protraction increases max HP by 10% and heals for the same amount. The 10s buff also increases healing received, making follow-up heals more effective. Free oGCD with no resource cost.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Protraction is a free oGCD that effectively heals and buffs healing received. Great before big damage on a single target!",
                    ConceptId = SchConcepts.EmergencyHealing,
                    Priority = ExplanationPriority.Normal,
                });

                context.TrainingService.RecordConceptApplication(SchConcepts.EmergencyHealing, wasSuccessful: true);
            }

            return true;
        }

        return false;
    }
}
