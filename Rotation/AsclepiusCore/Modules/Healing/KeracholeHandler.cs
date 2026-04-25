using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

public sealed class KeracholeHandler : IHealingHandler
{
    private static readonly string[] _keracholeAlternatives =
    {
        "Ixochole (instant AoE heal)",
        "Taurochole (single-target version)",
        "Physis II (AoE HoT only)",
    };

    public int Priority => 20;
    public string Name => "Kerachole";

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableKerachole) return;
        if (player.Level < SGEActions.Kerachole.MinLevel) return;
        if (context.AddersgallStacks < 1) { context.Debug.KeracholeState = "No Addersgall"; return; }
        if (!context.ActionService.IsActionReady(SGEActions.Kerachole.ActionId)) { context.Debug.KeracholeState = "On CD"; return; }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out var raidwideSource);

        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness && coordConfig.PreferShieldsBeforeBurst && partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
                burstImminent = true;
        }

        if (!raidwideImminent && !burstImminent && injuredCount < 2) { context.Debug.KeracholeState = $"{injuredCount} injured"; return; }
        if (!raidwideImminent && !burstImminent && avgHp > config.KeracholeThreshold) { context.Debug.KeracholeState = $"Avg HP {avgHp:P0}"; return; }

        var action = SGEActions.Kerachole;

        if (coordConfig.EnableGroundEffectCoordination &&
            partyCoord?.WouldOverlapWithRemoteGroundEffect(
                player.Position, action.ActionId, coordConfig.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.KeracholeState = "Skipped (area covered)";
            return;
        }

        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
        {
            context.Debug.KeracholeState = "Skipped (remote AOE reserved)";
            return;
        }

        var capturedAvgHp = avgHp;
        var capturedInjuredCount = injuredCount;
        var capturedStacks = context.AddersgallStacks;
        var capturedRaidwideImminent = raidwideImminent;
        var capturedBurstImminent = burstImminent;

        scheduler.PushOgcd(AsclepiusAbilities.Kerachole, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                partyCoord?.OnGroundEffectPlaced(action.ActionId, player.Position);

                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Kerachole";
                context.Debug.KeracholeState = "Executing";
                context.LogAddersgallDecision(action.Name, capturedStacks, $"Party regen + mit");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var trigger = capturedRaidwideImminent ? "Raidwide imminent" : capturedBurstImminent ? "Burst phase imminent" : "Party needs healing";
                    var shortReason = $"Kerachole - {trigger} ({capturedStacks} stacks)";
                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Injured count: {capturedInjuredCount}",
                        trigger,
                        $"Addersgall stacks: {capturedStacks}",
                        "100 potency regen + 10% mit (15s)",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Kerachole",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Kerachole placed with {capturedStacks} Addersgall stacks. {trigger}. Creates a 15s healing zone with 100 potency regen/tick AND 10% damage reduction. This is SGE's best sustained party healing tool - use it proactively before raidwides!",
                        Factors = factors,
                        Alternatives = _keracholeAlternatives,
                        Tip = "Kerachole is AMAZING value - regen + mitigation in one! Place it BEFORE damage hits so the party has mitigation when the raidwide lands, then benefits from regen for recovery. Shares CD with Taurochole.",
                        ConceptId = SgeConcepts.KeracholeUsage,
                        Priority = ExplanationPriority.High,
                    });
                }
            });
    }
}
