using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Scheduling;
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

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableEarthlyStar) return;
        if (config.StarPlacement == EarthlyStarPlacementStrategy.Manual) return;
        if (player.Level < ASTActions.EarthlyStar.MinLevel) return;
        if (context.IsStarPlaced) return;
        if (!context.ActionService.IsActionReady(ASTActions.EarthlyStar.ActionId)) return;

        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration,
            out _, windowSeconds: 12f);

        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness && coordConfig.PreferShieldsBeforeBurst && partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 8f && burstState.SecondsUntilBurst <= 12f)
                burstImminent = true;
        }

        if (!raidwideImminent && !burstImminent)
        {
            var (avgHp, _, _) = context.PartyHealthMetrics;
            if (avgHp > config.EarthlyStarDetonateThreshold) return;
        }

        var targetPosition = player.Position;
        var targetName = "Self";

        if (config.StarPlacement == EarthlyStarPlacementStrategy.OnMainTank)
        {
            var tank = context.PartyHelper.FindTankInParty(player);
            if (tank != null)
            {
                targetPosition = tank.Position;
                targetName = tank.Name.TextValue;
            }
        }

        if (partyCoord?.WouldOverlapWithRemoteGroundEffect(
            targetPosition, ASTActions.EarthlyStar.ActionId,
            coordConfig.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.EarthlyStarState = "Skipped (area covered)";
            return;
        }

        var capturedPosition = targetPosition;
        var capturedTargetName = targetName;
        var capturedRaidwideImminent = raidwideImminent;
        var capturedBurstImminent = burstImminent;
        var action = ASTActions.EarthlyStar;

        scheduler.PushGroundTargetedOgcd(AstraeaAbilities.EarthlyStar, targetPosition, priority: Priority,
            onDispatched: _ =>
            {
                context.EarthlyStarService.OnStarPlaced(capturedPosition);
                partyCoord?.OnGroundEffectPlaced(action.ActionId, capturedPosition);

                context.Debug.PlannedAction = action.Name;
                context.Debug.EarthlyStarState = "Placed";
                var reason = capturedRaidwideImminent ? "Raidwide imminent" : (capturedBurstImminent ? "Burst imminent" : "Reactive");
                context.LogEarthlyStarDecision("Placed", $"{config.StarPlacement} ({capturedTargetName}) - {reason}");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var (avgHp, _, _) = context.PartyHealthMetrics;
                    var shortReason = capturedRaidwideImminent
                        ? $"Earthly Star placed - raidwide in ~10s!"
                        : capturedBurstImminent
                            ? $"Earthly Star placed - burst phase in ~10s"
                            : $"Earthly Star placed at {capturedTargetName}";

                    var factors = new[]
                    {
                        $"Placement: {config.StarPlacement} ({capturedTargetName})",
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
                        TargetName = capturedTargetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Earthly Star placed at {capturedTargetName}'s position. {reason}. Star needs 10 seconds to mature into Giant Dominance (720 potency heal + damage). Placing proactively ensures it's ready when the party needs healing. {config.StarPlacement} strategy places star where it will hit the most party members.",
                        Factors = factors,
                        Alternatives = _alternatives,
                        Tip = "Earthly Star is AST's strongest AoE heal when mature. Place it ~10s before you need it! Don't sit on cooldown - even immature detonation is better than not using it.",
                        ConceptId = AstConcepts.EarthlyStarPlacement,
                        Priority = capturedRaidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });

                    context.TrainingService?.RecordConceptApplication(AstConcepts.EarthlyStarPlacement, wasSuccessful: capturedRaidwideImminent || capturedBurstImminent, capturedRaidwideImminent ? "Proactive raidwide placement" : capturedBurstImminent ? "Burst window placement" : "Reactive placement");
                }
            });
    }
}
