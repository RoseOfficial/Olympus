using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class EarthlyStarPlacementHandler : IHealingHandler
{
    public int Priority => 50;
    public string Name => "EarthlyStarPlacement";

    private static readonly string[] _alternatives =
    {
        "Wait for better timing",
        "Place on self instead of tank",
        "Save for emergency healing",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
        => TryEarthlyStarPlacement(context);

    private bool TryEarthlyStarPlacement(IAstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableEarthlyStar)
            return false;

        if (config.StarPlacement == Config.EarthlyStarPlacementStrategy.Manual)
            return false;

        if (player.Level < ASTActions.EarthlyStar.MinLevel)
            return false;

        // Don't place if star is already active
        if (context.IsStarPlaced)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.EarthlyStar.ActionId))
            return false;

        // Timeline-aware: proactively place before raidwides
        // Earthly Star needs ~10s to mature for full Giant Dominance potency
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration,
            out _,
            windowSeconds: 12f); // Longer window for Star maturation

        // Burst awareness: Place Earthly Star proactively before burst windows
        // Star needs ~10s to mature, so place 8-12s before burst
        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.PreferShieldsBeforeBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            // Place Star 8-12 seconds before burst for maturation
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 8f && burstState.SecondsUntilBurst <= 12f)
            {
                burstImminent = true;
            }
        }

        // Only place proactively if raidwide or burst is imminent
        // Otherwise, rely on reactive placement when party HP drops
        if (!raidwideImminent && !burstImminent)
        {
            // Reactive placement: only place if party needs healing
            var (avgHp, _, _) = context.PartyHealthMetrics;
            if (avgHp > config.EarthlyStarDetonateThreshold)
                return false;
        }

        // Determine placement position (Earthly Star is ground-targeted, not entity-targeted)
        var targetPosition = player.Position;
        var targetName = "Self";

        if (config.StarPlacement == Config.EarthlyStarPlacementStrategy.OnMainTank)
        {
            var tank = context.PartyHelper.FindTankInParty(player);
            if (tank != null)
            {
                targetPosition = tank.Position;
                targetName = tank.Name.TextValue;
            }
        }

        // Check if another Olympus healer already has a ground effect in this area
        if (partyCoord?.WouldOverlapWithRemoteGroundEffect(
            targetPosition,
            ASTActions.EarthlyStar.ActionId,
            coordConfig.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.EarthlyStarState = "Skipped (area covered)";
            return false;
        }

        var action = ASTActions.EarthlyStar;
        if (context.ActionService.ExecuteGroundTargetedOgcd(action, targetPosition))
        {
            // Notify service for state tracking
            context.EarthlyStarService.OnStarPlaced(targetPosition);

            // Broadcast ground effect placement to other Olympus instances
            partyCoord?.OnGroundEffectPlaced(action.ActionId, targetPosition);

            context.Debug.PlannedAction = action.Name;
            context.Debug.EarthlyStarState = "Placed";
            var reason = raidwideImminent ? "Raidwide imminent" : (burstImminent ? "Burst imminent" : "Reactive");
            context.LogEarthlyStarDecision("Placed", $"{config.StarPlacement} ({targetName}) - {reason}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var (avgHp, _, _) = context.PartyHealthMetrics;
                var shortReason = raidwideImminent
                    ? $"Earthly Star placed - raidwide in ~10s!"
                    : burstImminent
                        ? $"Earthly Star placed - burst phase in ~10s"
                        : $"Earthly Star placed at {targetName}";

                var factors = new[]
                {
                    $"Placement: {config.StarPlacement} ({targetName})",
                    reason,
                    "Needs 10s to mature for Giant Dominance",
                    "Mature: 720 potency heal + 720 damage",
                    "Immature: 360 potency heal + 360 damage",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Earthly Star",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Earthly Star placed at {targetName}'s position. {reason}. Star needs 10 seconds to mature into Giant Dominance (720 potency heal + damage). Placing proactively ensures it's ready when the party needs healing. {config.StarPlacement} strategy places star where it will hit the most party members.",
                    Factors = factors,
                    Alternatives = _alternatives,
                    Tip = "Earthly Star is AST's strongest AoE heal when mature. Place it ~10s before you need it! Don't sit on cooldown - even immature detonation is better than not using it.",
                    ConceptId = AstConcepts.EarthlyStarPlacement,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });

                context.TrainingService?.RecordConceptApplication(AstConcepts.EarthlyStarPlacement, wasSuccessful: raidwideImminent || burstImminent, raidwideImminent ? "Proactive raidwide placement" : burstImminent ? "Burst window placement" : "Reactive placement");
            }

            return true;
        }

        return false;
    }
}
