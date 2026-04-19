using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Kerachole for Sage: AoE regen + mitigation oGCD.
/// Priority 20 in the oGCD list.
/// </summary>
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

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryKerachole(context);

    private bool TryKerachole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableKerachole)
            return false;

        if (player.Level < SGEActions.Kerachole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.KeracholeState = "No Addersgall";
            return false;
        }

        // Check cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Kerachole.ActionId))
        {
            context.Debug.KeracholeState = "On CD";
            return false;
        }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Check if raidwide is imminent - use proactively for mit + regen
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration,
            out var raidwideSource);

        // Burst awareness: Deploy Kerachole proactively before burst windows
        // Regen + mit provides sustained healing during high-damage DPS phases
        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.PreferShieldsBeforeBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            // Deploy Kerachole 3-8 seconds before burst (similar to raidwide logic)
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
            {
                burstImminent = true;
            }
        }

        // Kerachole is best value - use it liberally for regen + mit
        // If raidwide or burst is coming, use even if party is at high HP
        if (!raidwideImminent && !burstImminent && injuredCount < 2)
        {
            context.Debug.KeracholeState = $"{injuredCount} injured";
            return false;
        }

        if (!raidwideImminent && !burstImminent && avgHp > config.KeracholeThreshold)
        {
            context.Debug.KeracholeState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Kerachole;

        // Check if another Olympus healer already has a ground effect in this area
        // Kerachole creates a healing zone - avoid stacking with other ground effects
        if (coordConfig.EnableGroundEffectCoordination &&
            partyCoord?.WouldOverlapWithRemoteGroundEffect(
                player.Position,
                action.ActionId,
                coordConfig.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.KeracholeState = "Skipped (area covered)";
            return false;
        }

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
        {
            context.Debug.KeracholeState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            // Broadcast ground effect placement to other Olympus instances
            partyCoord?.OnGroundEffectPlaced(action.ActionId, player.Position);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Kerachole";
            context.Debug.KeracholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"Party regen + mit");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var stacks = context.AddersgallStacks;
                var trigger = raidwideImminent ? "Raidwide imminent" : burstImminent ? "Burst phase imminent" : "Party needs healing";

                var shortReason = $"Kerachole - {trigger} ({stacks} stacks)";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    trigger,
                    $"Addersgall stacks: {stacks}",
                    "100 potency regen + 10% mit (15s)",
                };

                var alternatives = _keracholeAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Kerachole",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Kerachole placed with {stacks} Addersgall stacks. {trigger}. Creates a 15s healing zone with 100 potency regen/tick AND 10% damage reduction. This is SGE's best sustained party healing tool - use it proactively before raidwides!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Kerachole is AMAZING value - regen + mitigation in one! Place it BEFORE damage hits so the party has mitigation when the raidwide lands, then benefits from regen for recovery. Shares CD with Taurochole.",
                    ConceptId = SgeConcepts.KeracholeUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }
}
