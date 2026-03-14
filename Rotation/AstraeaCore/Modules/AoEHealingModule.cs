using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Handles AoE healing for Astrologian: Celestial Opposition and Helios/AspectedHelios/HeliosConjunction.
/// </summary>
public sealed class AoEHealingModule
{
    private static readonly string[] _celestialOppositionAlternatives =
    {
        "Earthly Star (higher potency if mature)",
        "Helios Conjunction (GCD AoE)",
        "Save for predictable raidwide",
    };

    private static readonly string[] _aoeHealAlternatives =
    {
        "Celestial Opposition (oGCD, free)",
        "Earthly Star detonation (if placed)",
        "Horoscope detonation (if active)",
    };

    /// <summary>Tries CelestialOpposition. Does not check CanExecuteOgcd.</summary>
    public bool TryOgcd(AstraeaContext context)
    {
        return TryCelestialOpposition(context);
    }

    /// <summary>Tries AoEHeal. Caller guards with !isMoving. Does not check CanExecuteGcd.</summary>
    public bool TryGcd(AstraeaContext context)
    {
        return TryAoEHeal(context);
    }

    private bool TryCelestialOpposition(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableCelestialOpposition)
            return false;

        if (player.Level < ASTActions.CelestialOpposition.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.CelestialOpposition.ActionId))
            return false;

        if (!ShouldUseAoEHeal(context))
            return false;

        var action = ASTActions.CelestialOpposition;

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
        {
            context.Debug.CelestialOppositionState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.CelestialOppositionState = "Used";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var (avgHp, _, injured) = context.PartyHealthMetrics;

                var shortReason = $"Celestial Opposition - {injured} injured at {avgHp:P0} avg";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injured}",
                    "200 potency heal + 15s regen",
                    "60s cooldown",
                    "oGCD - free healing",
                };

                var alternatives = _celestialOppositionAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Celestial Opposition",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Celestial Opposition used on {injured} injured party members at {avgHp:P0} average HP. Provides 200 potency instant heal plus a 15s regen (100 potency/tick). Free oGCD AoE heal on 60s cooldown - excellent value!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Celestial Opposition is a free AoE heal + regen. Use it liberally! The regen continues ticking even while you DPS. Don't hold it for emergencies - that's what Essential Dignity is for.",
                    ConceptId = AstConcepts.CelestialOppositionUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryAoEHeal(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableHelios && !config.EnableAspectedHelios)
            return false;

        if (!ShouldUseAoEHeal(context))
            return false;

        // Choose appropriate AoE heal based on level and config
        ActionDefinition? action = null;

        // Helios Conjunction (level 96) - upgraded Aspected Helios
        if (config.EnableAspectedHelios && player.Level >= ASTActions.HeliosConjunction.MinLevel)
        {
            action = ASTActions.HeliosConjunction;
        }
        // Aspected Helios (level 42)
        else if (config.EnableAspectedHelios && player.Level >= ASTActions.AspectedHelios.MinLevel)
        {
            action = ASTActions.AspectedHelios;
        }
        // Basic Helios (level 10)
        else if (config.EnableHelios && player.Level >= ASTActions.Helios.MinLevel)
        {
            action = ASTActions.Helios;
        }

        if (action == null)
            return false;

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        var castTimeMs = (int)(action.CastTime * 1000);
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, castTimeMs))
        {
            context.Debug.AoEHealState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.AoEHealState = "Casting";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var (avgHp, _, injured) = context.PartyHealthMetrics;
                var hasRegen = action == ASTActions.AspectedHelios || action == ASTActions.HeliosConjunction;

                var shortReason = $"{action.Name} - {injured} injured at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injured}",
                    $"Action: {action.Name}",
                    hasRegen ? "Includes 15s regen" : "Direct heal only",
                    "GCD heal - uses a GCD",
                };

                var alternatives = _aoeHealAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = action.Name,
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"{action.Name} cast on {injured} injured party members at {avgHp:P0} average HP. {(hasRegen ? "Provides direct healing plus a 15s regen for sustained recovery." : "Direct healing with no regen.")} Remember: oGCD heals like Celestial Opposition are 'free' - use them first before GCD heals when possible!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = hasRegen
                        ? "Aspected Helios/Helios Conjunction adds a regen - great value! But always check if oGCD heals can handle it first."
                        : "Basic Helios is pure healing. Consider using Aspected Helios for the regen if available.",
                    ConceptId = AstConcepts.AspectedHeliosUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private static bool ShouldUseAoEHeal(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        var (count, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, 0);
        var (avgHp, _, _) = context.PartyHealthMetrics;

        // Timeline-aware: use before raidwides
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Use if party needs healing OR raidwide is imminent
        return (avgHp <= config.AoEHealThreshold && count >= config.AoEHealMinTargets) || raidwideImminent;
    }
}
