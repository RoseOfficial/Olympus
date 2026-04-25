using System;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

public sealed class SacredSoilHandler : IHealingHandler
{
    public int Priority => 30;
    public string Name => "SacredSoil";

    private static readonly string[] _sacredSoilAlternatives =
    {
        "Succor (GCD shield, no mitigation)",
        "Expedient (sprint + mitigation)",
        "Save Aetherflow for Indomitability",
    };

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableSacredSoil) return;
        if (player.Level < SCHActions.SacredSoil.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.SacredSoil.ActionId)) return;
        if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve) return;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);

        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness && coordConfig.PreferShieldsBeforeBurst && partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
                burstImminent = true;
        }

        if (avgHp > config.SacredSoilThreshold && !raidwideImminent && !burstImminent) return;

        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= SCHActions.SacredSoil.RadiusSquared)
                membersInRange++;
        }

        if (membersInRange < config.SacredSoilMinTargets) return;

        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.PlanningState = "Sacred Soil skipped (remote mit)";
            return;
        }

        if (partyCoord?.WouldOverlapWithRemoteGroundEffect(
            player.Position, SCHActions.SacredSoil.ActionId, coordConfig.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.PlanningState = "Sacred Soil skipped (area covered)";
            return;
        }

        var action = SCHActions.SacredSoil;
        var capturedAvgHp = avgHp;
        var capturedMembersInRange = membersInRange;
        var capturedRaidwideImminent = raidwideImminent;
        var capturedBurstImminent = burstImminent;
        var capturedPosition = player.Position;

        scheduler.PushOgcd(AthenaAbilities.SacredSoil, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.AetherflowService.ConsumeStack();
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Sacred Soil";
                partyCoord?.OnCooldownUsed(action.ActionId, 30_000);
                partyCoord?.OnGroundEffectPlaced(action.ActionId, capturedPosition);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    string trigger;
                    if (capturedRaidwideImminent) trigger = "Raidwide imminent";
                    else if (capturedBurstImminent) trigger = "DPS burst window imminent";
                    else trigger = $"Party HP low ({capturedAvgHp:P0})";

                    var shortReason = $"Sacred Soil - {trigger}";
                    var factors = new[]
                    {
                        trigger,
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Members in range: {capturedMembersInRange}",
                        $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                        "10% damage reduction + HoT (at 78+)",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Sacred Soil",
                        Category = "Defensive",
                        TargetName = "Ground",
                        ShortReason = shortReason,
                        DetailedReason = $"Sacred Soil placed for {capturedMembersInRange} party members. {trigger}. Sacred Soil provides 10% damage reduction and at level 78+ adds a healing-over-time effect (100 potency per tick). Cost 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining). Best used proactively before damage hits.",
                        Factors = factors,
                        Alternatives = _sacredSoilAlternatives,
                        Tip = "Sacred Soil is one of SCH's best mitigation tools. At 78+, the HoT makes it extremely valuable. Place it before raidwides, not after!",
                        ConceptId = SchConcepts.SacredSoilUsage,
                        Priority = capturedRaidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });

                    context.TrainingService.RecordConceptApplication(SchConcepts.SacredSoilUsage, wasSuccessful: true);
                }
            });
    }
}
