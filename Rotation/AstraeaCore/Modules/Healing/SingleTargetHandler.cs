using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class SingleTargetHandler : IHealingHandler
{
    public int Priority => 50;
    public string Name => "SingleTarget";

    private static readonly string[] _alternatives =
    {
        "Essential Dignity (oGCD emergency)",
        "Aspected Benefic (instant, adds regen)",
        "Celestial Intersection (oGCD)",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
    {
        if (isMoving) return false;
        return TrySingleTargetHeal(context);
    }

    private bool TrySingleTargetHeal(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableBenefic && !config.EnableBeneficII)
            return false;

        var target = context.Configuration.Healing.UseDamageIntakeTriage
            ? context.PartyHelper.FindMostEndangeredPartyMember(
                player, context.DamageIntakeService, 0, context.DamageTrendService, context.ShieldTrackingService)
            : context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        // Choose between Benefic II and Benefic
        ActionDefinition? action = null;

        // Benefic II (level 26) for higher healing
        if (config.EnableBeneficII && player.Level >= ASTActions.BeneficII.MinLevel && hpPercent <= config.BeneficIIThreshold)
        {
            action = ASTActions.BeneficII;
        }
        // Fall back to Benefic
        else if (config.EnableBenefic && hpPercent <= config.BeneficThreshold)
        {
            action = ASTActions.Benefic;
        }

        if (action == null)
            return false;

        // Co-healer awareness: skip the GCD heal when another healer's pending cast will cover the target.
        if (CoHealerAwarenessHelper.CoHealerWillCover(
                context.Configuration.Healing.EnableCoHealerAwareness,
                context.CoHealerDetectionService,
                target,
                context.Configuration.Healing.CoHealerPendingHealThreshold))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            var castTimeMs = (int)(action.CastTime * 1000);
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, castTimeMs);

            context.Debug.PlannedAction = action.Name;
            context.Debug.SingleHealState = action.Name;

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var isBeneficII = action == ASTActions.BeneficII;

                var shortReason = $"{action.Name} on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    isBeneficII ? $"Threshold: {config.BeneficIIThreshold:P0}" : $"Threshold: {config.BeneficThreshold:P0}",
                    isBeneficII ? "800 potency (high healing)" : "500 potency (basic healing)",
                    "GCD heal with cast time",
                    "Use oGCDs first when possible",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = action.Name,
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"{action.Name} on {targetName} at {hpPercent:P0} HP. {(isBeneficII ? "Benefic II provides 800 potency - AST's strongest single-target GCD heal." : "Benefic provides 500 potency - basic healing.")} Remember: oGCD heals are 'free' - exhaust those before using GCD heals when possible!",
                    Factors = factors,
                    Alternatives = _alternatives,
                    Tip = isBeneficII
                        ? "Benefic II is your strongest GCD heal, but it costs a GCD. Make sure you've used Essential Dignity, Celestial Intersection, and Exaltation first!"
                        : "Benefic is weak. At level 26+, prefer Benefic II for serious healing. Save Benefic for when you need to conserve MP or only need a small top-off.",
                    ConceptId = AstConcepts.EmergencyHealing,
                    Priority = ExplanationPriority.Normal,
                });

                context.TrainingService?.RecordConceptApplication(AstConcepts.EmergencyHealing, wasSuccessful: true, isBeneficII ? "Benefic II GCD heal" : "Benefic GCD heal");
            }

            return true;
        }

        return false;
    }
}
