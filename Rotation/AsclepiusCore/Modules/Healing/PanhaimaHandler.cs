using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Panhaima for Sage. Priority 40 in the oGCD list.
/// </summary>
public sealed class PanhaimaHandler : IHealingHandler
{
    private static readonly string[] _panhaimaAlternatives =
    {
        "Holos (heal + shield + mit)",
        "Kerachole (regen + mit)",
        "E.Prognosis (GCD party shield)",
    };

    public int Priority => 40;
    public string Name => "Panhaima";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryPanhaima(context);

    private bool TryPanhaima(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePanhaima)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.PanhaimaState = "Skipped (remote mit)";
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
                context.Debug.PanhaimaState = $"Delayed (burst active)";
                return false;
            }
        }

        if (player.Level < SGEActions.Panhaima.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Panhaima.ActionId))
        {
            context.Debug.PanhaimaState = "On CD";
            return false;
        }

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Check if raidwide is imminent - use proactively for AoE shields
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out var raidwideSource);

        // Panhaima is a 2-minute CD - save for raidwides
        // Use if raidwide is coming or party HP is low
        if (avgHp > config.PanhaimaThreshold && !raidwideImminent)
        {
            context.Debug.PanhaimaState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Panhaima;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Panhaima";
            context.Debug.PanhaimaState = "Executing";
            partyCoord?.OnCooldownUsed(action.ActionId, 120_000);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = raidwideImminent
                    ? "Panhaima - raidwide incoming!"
                    : $"Panhaima - party at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    raidwideImminent ? "Raidwide imminent!" : $"Threshold: {config.PanhaimaThreshold:P0}",
                    "200 potency shield x5 stacks (party-wide)",
                    "Shields refresh when broken",
                    "120s cooldown",
                };

                var alternatives = _panhaimaAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Panhaima",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Panhaima placed on party at {avgHp:P0} avg HP. {(raidwideImminent ? "Raidwide detected - shields will absorb incoming damage!" : "Proactive party shielding.")} Provides 5 stacks of 200 potency shields to ALL party members that refresh when consumed. Amazing for multi-hit raidwides!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Panhaima is the AoE version of Haima! Use it before multi-hit raidwides where the party will take repeated damage. Any remaining shield value heals when it expires. Excellent for prog where damage patterns are unknown.",
                    ConceptId = SgeConcepts.PanhaimaUsage,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
