using System;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Handles Sacred Soil for Scholar. Priority 30 in the oGCD list.
/// Costs 1 Aetherflow stack.
/// </summary>
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

    public bool TryExecute(IAthenaContext context, bool isMoving)
        => TrySacredSoil(context);

    private bool TrySacredSoil(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableSacredSoil)
            return false;

        if (player.Level < SCHActions.SacredSoil.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.SacredSoil.ActionId))
            return false;

        // Respect Aetherflow reserve
        if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
            return false;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Timeline-aware: proactively place before raidwides
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Burst awareness: Deploy Sacred Soil proactively before burst windows
        // Mit + regen provides sustained healing during high-damage DPS phases
        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.PreferShieldsBeforeBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            // Deploy Sacred Soil 3-8 seconds before burst (similar to raidwide logic)
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
            {
                burstImminent = true;
            }
        }

        // Use if party HP is low OR raidwide is imminent OR burst is imminent
        if (avgHp > config.SacredSoilThreshold && !raidwideImminent && !burstImminent)
            return false;

        // Count party members in range
        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= SCHActions.SacredSoil.RadiusSquared)
                membersInRange++;
        }

        if (membersInRange < config.SacredSoilMinTargets)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.PlanningState = "Sacred Soil skipped (remote mit)";
            return false;
        }

        // Check if another Olympus healer already has a ground effect in this area
        if (partyCoord?.WouldOverlapWithRemoteGroundEffect(
            player.Position,
            SCHActions.SacredSoil.ActionId,
            coordConfig.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.PlanningState = "Sacred Soil skipped (area covered)";
            return false;
        }

        // Place at player's position (ground target at self)
        var action = SCHActions.SacredSoil;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.AetherflowService.ConsumeStack();
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Sacred Soil";
            partyCoord?.OnCooldownUsed(action.ActionId, 30_000);
            // Broadcast ground effect placement to other Olympus instances
            partyCoord?.OnGroundEffectPlaced(action.ActionId, player.Position);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                string trigger;
                if (raidwideImminent) trigger = "Raidwide imminent";
                else if (burstImminent) trigger = "DPS burst window imminent";
                else trigger = $"Party HP low ({avgHp:P0})";

                var shortReason = $"Sacred Soil - {trigger}";

                var factors = new[]
                {
                    trigger,
                    $"Party avg HP: {avgHp:P0}",
                    $"Members in range: {membersInRange}",
                    $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                    "10% damage reduction + HoT (at 78+)",
                };

                var alternatives = _sacredSoilAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Sacred Soil",
                    Category = "Defensive",
                    TargetName = "Ground",
                    ShortReason = shortReason,
                    DetailedReason = $"Sacred Soil placed for {membersInRange} party members. {trigger}. Sacred Soil provides 10% damage reduction and at level 78+ adds a healing-over-time effect (100 potency per tick). Cost 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining). Best used proactively before damage hits.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Sacred Soil is one of SCH's best mitigation tools. At 78+, the HoT makes it extremely valuable. Place it before raidwides, not after!",
                    ConceptId = SchConcepts.SacredSoilUsage,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
