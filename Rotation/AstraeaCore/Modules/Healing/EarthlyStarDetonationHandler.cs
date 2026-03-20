using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class EarthlyStarDetonationHandler : IHealingHandler
{
    public int Priority => 40;
    public string Name => "EarthlyStarDetonation";

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TryEarthlyStarDetonation(context);

    private bool TryEarthlyStarDetonation(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableEarthlyStar)
            return false;

        if (!context.IsStarPlaced)
            return false;

        if (player.Level < ASTActions.StellarDetonation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.StellarDetonation.ActionId))
            return false;

        var (avgHp, lowestHp, injured) = context.PartyHealthMetrics;

        // Check if star is mature (Giant Dominance)
        bool isMature = context.IsStarMature;

        // Timeline-aware: check for imminent raidwide
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Determine if we should detonate
        bool shouldDetonate = false;

        if (isMature)
        {
            // Mature star: detonate when party needs healing OR raidwide is imminent
            if (avgHp <= config.EarthlyStarDetonateThreshold || injured >= config.EarthlyStarMinTargets || raidwideImminent)
                shouldDetonate = true;
        }
        else if (!config.WaitForGiantDominance)
        {
            // Immature star allowed: detonate if party needs healing or raidwide imminent
            if (avgHp <= config.EarthlyStarDetonateThreshold || injured >= config.EarthlyStarMinTargets || raidwideImminent)
                shouldDetonate = true;
        }
        else
        {
            // Emergency detonation even if immature
            if (avgHp <= config.EarthlyStarEmergencyThreshold)
                shouldDetonate = true;
        }

        if (!shouldDetonate)
            return false;

        var action = ASTActions.StellarDetonation;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            // Notify service that star was detonated
            context.EarthlyStarService.OnStarDetonated();

            context.Debug.PlannedAction = action.Name;
            context.Debug.EarthlyStarState = isMature ? "Detonated (Mature)" : "Detonated (Immature)";
            context.LogEarthlyStarDecision("Detonated", isMature ? "Mature star, party needs healing" : "Emergency detonate");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                string trigger;
                if (raidwideImminent) trigger = "Raidwide imminent";
                else if (avgHp <= config.EarthlyStarEmergencyThreshold) trigger = "Emergency HP";
                else trigger = $"Party HP low ({avgHp:P0})";

                var shortReason = isMature
                    ? $"Giant Dominance detonated - {trigger}"
                    : $"Immature Star detonated - {trigger}";

                var factors = new[]
                {
                    isMature ? "Star MATURE (Giant Dominance)" : "Star immature",
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injured}",
                    trigger,
                    isMature ? "720 potency heal + 720 damage" : "360 potency heal + 360 damage",
                };

                var alternatives = new[]
                {
                    isMature ? "Detonation is optimal when mature" : "Wait for maturation (if safe)",
                    "Let star expire naturally",
                    "Use other oGCDs first",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Stellar Detonation",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Detonated Earthly Star ({(isMature ? "Giant Dominance, 720 potency" : "immature, 360 potency")}). Party avg HP at {avgHp:P0} with {injured} injured. {trigger}. {(isMature ? "Mature star provides maximum healing value!" : "Detonated early due to urgent healing need.")}",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = isMature
                        ? "Perfect timing! Mature Earthly Star is AST's biggest AoE heal. Always aim for Giant Dominance when possible."
                        : "Sometimes you have to detonate early. An immature heal is better than letting the party die!",
                    ConceptId = AstConcepts.EarthlyStarMaturation,
                    Priority = isMature ? ExplanationPriority.High : ExplanationPriority.Normal,
                });

                context.TrainingService?.RecordConceptApplication(AstConcepts.EarthlyStarMaturation, wasSuccessful: isMature, isMature ? "Giant Dominance detonation" : "Early detonation");
            }

            return true;
        }

        return false;
    }
}
