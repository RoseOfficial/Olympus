using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
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

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (isMoving) return;

        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableBenefic && !config.EnableBeneficII) return;

        var target = context.Configuration.Healing.UseDamageIntakeTriage
            ? context.PartyHelper.FindMostEndangeredPartyMember(
                player, context.DamageIntakeService, 0, context.DamageTrendService, context.ShieldTrackingService)
            : context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null) return;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        ActionDefinition? action = null;
        AbilityBehavior? behavior = null;

        if (config.EnableBeneficII && player.Level >= ASTActions.BeneficII.MinLevel && hpPercent <= config.BeneficIIThreshold)
        {
            action = ASTActions.BeneficII;
            behavior = AstraeaAbilities.BeneficII;
        }
        else if (config.EnableBenefic && hpPercent <= config.BeneficThreshold)
        {
            action = ASTActions.Benefic;
            behavior = AstraeaAbilities.Benefic;
        }

        if (action == null || behavior == null) return;

        if (CoHealerAwarenessHelper.CoHealerWillCover(
                context.Configuration.Healing.EnableCoHealerAwareness,
                context.CoHealerDetectionService,
                target,
                context.Configuration.Healing.CoHealerPendingHealThreshold))
            return;

        var capturedAction = action;
        var capturedTarget = target;
        var capturedHpPercent = hpPercent;

        scheduler.PushGcd(behavior, target.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                var healAmount = capturedAction.HealPotency * 10;
                var castTimeMs = (int)(capturedAction.CastTime * 1000);
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, healAmount, capturedAction.ActionId, castTimeMs);

                context.Debug.PlannedAction = capturedAction.Name;
                context.Debug.SingleHealState = capturedAction.Name;

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var isBeneficII = capturedAction == ASTActions.BeneficII;
                    var shortReason = $"{capturedAction.Name} on {targetName} at {capturedHpPercent:P0}";
                    var factors = new[]
                    {
                        $"Target HP: {capturedHpPercent:P0}",
                        isBeneficII ? $"Threshold: {config.BeneficIIThreshold:P0}" : $"Threshold: {config.BeneficThreshold:P0}",
                        isBeneficII ? "800 potency (high healing)" : "500 potency (basic healing)",
                        "GCD heal with cast time",
                        "Use oGCDs first when possible",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = capturedAction.ActionId,
                        ActionName = capturedAction.Name,
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"{capturedAction.Name} on {targetName} at {capturedHpPercent:P0} HP. {(isBeneficII ? "Benefic II provides 800 potency - AST's strongest single-target GCD heal." : "Benefic provides 500 potency - basic healing.")} Remember: oGCD heals are 'free' - exhaust those before using GCD heals when possible!",
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
            });
    }
}
