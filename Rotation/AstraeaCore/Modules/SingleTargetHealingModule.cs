using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Handles single-target healing for Astrologian: Essential Dignity, Celestial Intersection,
/// Aspected Benefic, and Benefic II/Benefic.
/// </summary>
public sealed class SingleTargetHealingModule
{
    private static readonly string[] _essentialDignityAlternatives =
    {
        "Celestial Intersection (heal + shield)",
        "Benefic II (GCD heal)",
        "Save charge for emergency",
    };

    private static readonly string[] _celestialIntersectionAlternatives =
    {
        "Essential Dignity (emergency heal)",
        "Aspected Benefic (GCD + regen)",
        "Save charge for tank damage",
    };

    private static readonly string[] _aspectedBeneficAlternatives =
    {
        "Essential Dignity (oGCD, emergency)",
        "Celestial Intersection (oGCD)",
        "Benefic II (higher potency, has cast time)",
    };

    private static readonly string[] _beneficAlternatives =
    {
        "Essential Dignity (oGCD emergency)",
        "Aspected Benefic (instant, adds regen)",
        "Celestial Intersection (oGCD)",
    };

    /// <summary>Tries EssentialDignity then CelestialIntersection. Does not check CanExecuteOgcd.</summary>
    public bool TryOgcd(IAstraeaContext context)
    {
        if (TryEssentialDignity(context))
            return true;
        if (TryCelestialIntersection(context))
            return true;
        return false;
    }

    /// <summary>Tries AspectedBenefic then SingleTargetHeal. Does not check CanExecuteGcd.</summary>
    public bool TryGcd(IAstraeaContext context, bool isMoving)
    {
        if (TryAspectedBenefic(context))
            return true;
        if (!isMoving && TrySingleTargetHeal(context))
            return true;
        return false;
    }

    private bool TryEssentialDignity(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableEssentialDignity)
            return false;

        if (player.Level < ASTActions.EssentialDignity.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.EssentialDignity.ActionId))
            return false;

        var target = context.PartyHelper.FindEssentialDignityTarget(player, config.EssentialDignityThreshold);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var action = ASTActions.EssentialDignity;
        var hpPercent = context.PartyHelper.GetHpPercent(target);

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate (scales with low HP)
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.EssentialDignityState = "Used";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var isEmergency = hpPercent < 0.3f;

                var shortReason = isEmergency
                    ? $"Emergency Dignity on {targetName} at {hpPercent:P0}!"
                    : $"Essential Dignity on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.EssentialDignityThreshold:P0}",
                    "Potency scales up to 1100 at low HP!",
                    "2 charges, 40s recharge",
                    "oGCD - can weave without clipping",
                };

                var alternatives = _essentialDignityAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Essential Dignity",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Essential Dignity on {targetName} at {hpPercent:P0} HP. ED's potency scales from 400 at high HP to 1100 at very low HP, making it most efficient on low HP targets. {(isEmergency ? "Target was in critical condition!" : "Used proactively before HP dropped further.")} 2 charges with 40s recharge - don't sit on max charges!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Essential Dignity is most efficient at low HP! Don't panic use it at 80% - wait until 50% or below for maximum value. But don't let anyone die holding charges either.",
                    ConceptId = AstConcepts.EssentialDignityUsage,
                    Priority = isEmergency ? ExplanationPriority.Critical : ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

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

                var alternatives = _celestialIntersectionAlternatives;

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
                    Alternatives = alternatives,
                    Tip = "Celestial Intersection is excellent for tanks - the shield helps with auto-attacks and tankbusters. For non-tanks, it's a free oGCD heal + regen. Don't sit on charges!",
                    ConceptId = AstConcepts.CelestialIntersectionUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

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

                var alternatives = _aspectedBeneficAlternatives;

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
                    Alternatives = alternatives,
                    Tip = "Aspected Benefic is instant cast - your go-to heal while moving! The regen is great value. Check that the target doesn't already have the regen before refreshing.",
                    ConceptId = AstConcepts.AspectedBeneficUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TrySingleTargetHeal(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableBenefic && !config.EnableBeneficII)
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
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

                var alternatives = _beneficAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = action.Name,
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"{action.Name} on {targetName} at {hpPercent:P0} HP. {(isBeneficII ? "Benefic II provides 800 potency - AST's strongest single-target GCD heal." : "Benefic provides 500 potency - basic healing.")} Remember: oGCD heals are 'free' - exhaust those before using GCD heals when possible!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = isBeneficII
                        ? "Benefic II is your strongest GCD heal, but it costs a GCD. Make sure you've used Essential Dignity, Celestial Intersection, and Exaltation first!"
                        : "Benefic is weak. At level 26+, prefer Benefic II for serious healing. Save Benefic for when you need to conserve MP or only need a small top-off.",
                    ConceptId = AstConcepts.EmergencyHealing,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
