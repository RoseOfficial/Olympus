using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class AoEHealingHandler : IHealingHandler
{
    public int Priority => 30;
    public string Name => "AoEHealing";

    private static readonly string[] _alternatives =
    {
        "Celestial Opposition (oGCD, free)",
        "Earthly Star detonation (if placed)",
        "Horoscope detonation (if active)",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
    {
        if (isMoving) return false;
        return TryAoEHeal(context);
    }

    private bool TryAoEHeal(IAstraeaContext context)
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
                    Alternatives = _alternatives,
                    Tip = hasRegen
                        ? "Aspected Helios/Helios Conjunction adds a regen - great value! But always check if oGCD heals can handle it first."
                        : "Basic Helios is pure healing. Consider using Aspected Helios for the regen if available.",
                    ConceptId = AstConcepts.AspectedHeliosUsage,
                    Priority = ExplanationPriority.Normal,
                });

                context.TrainingService?.RecordConceptApplication(AstConcepts.AspectedHeliosUsage, wasSuccessful: true, hasRegen ? "AoE heal with regen" : "AoE heal");
            }

            return true;
        }

        return false;
    }

    private static bool ShouldUseAoEHeal(IAstraeaContext context)
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
