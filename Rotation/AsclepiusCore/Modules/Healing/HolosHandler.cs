using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Holos for Sage. Priority 30 in the oGCD list.
/// </summary>
public sealed class HolosHandler : IHealingHandler
{
    private static readonly string[] _holosAlternatives =
    {
        "Ixochole (AoE heal, 30s CD)",
        "Kerachole (AoE regen + mit, 45s CD)",
        "Panhaima (AoE multi-hit shields, 120s CD)",
    };

    public int Priority => 30;
    public string Name => "Holos";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryHolos(context);

    private bool TryHolos(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableHolos)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.HolosState = "Skipped (remote mit)";
            return false;
        }

        // Burst awareness: Delay mitigations during burst windows unless emergency
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var (avgHpCheck, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpCheck > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                context.Debug.HolosState = $"Delayed (burst active)";
                return false;
            }
        }

        if (player.Level < SGEActions.Holos.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Holos.ActionId))
        {
            context.Debug.HolosState = "On CD";
            return false;
        }

        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Holos is a 2-minute CD - save for emergencies
        if (lowestHp > config.HolosThreshold)
        {
            context.Debug.HolosState = $"Lowest HP {lowestHp:P0}";
            return false;
        }

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.HolosState = $"{injuredCount} injured";
            return false;
        }

        var action = SGEActions.Holos;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Holos";
            context.Debug.HolosState = "Executing";
            partyCoord?.OnCooldownUsed(action.ActionId, 120_000);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Holos - emergency heal ({lowestHp:P0} lowest, {injuredCount} injured)";

                var factors = new[]
                {
                    $"Lowest HP: {lowestHp:P0}",
                    $"Threshold: {config.HolosThreshold:P0}",
                    $"Injured count: {injuredCount}",
                    "300 potency heal + shield + 10% mit (20s)",
                    "120s cooldown - big emergency button",
                };

                var alternatives = _holosAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Holos",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Holos used as emergency response. Party at {avgHp:P0} avg HP with lowest at {lowestHp:P0}. Provides 300 potency heal + 300 potency shield + 10% damage reduction for 20 seconds. This is SGE's panic button - save it for real emergencies!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Holos is your 2-minute panic button! It does everything: heals, shields, AND mitigates. Save it for when things go wrong, or use proactively for massive incoming damage you know about.",
                    ConceptId = SgeConcepts.HolosUsage,
                    Priority = ExplanationPriority.Critical,
                });
            }

            return true;
        }

        return false;
    }
}
