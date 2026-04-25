using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

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

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePanhaima) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.PanhaimaState = "Skipped (remote mit)";
            return;
        }

        if (coordConfig.EnableHealerBurstAwareness && coordConfig.DelayMitigationsDuringBurst && partyCoord != null)
        {
            var (avgHpCheck, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpCheck > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                context.Debug.PanhaimaState = $"Delayed (burst active)";
                return;
            }
        }

        if (player.Level < SGEActions.Panhaima.MinLevel) return;
        if (!context.ActionService.IsActionReady(SGEActions.Panhaima.ActionId)) { context.Debug.PanhaimaState = "On CD"; return; }

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out var raidwideSource);

        if (avgHp > config.PanhaimaThreshold && !raidwideImminent) { context.Debug.PanhaimaState = $"Avg HP {avgHp:P0}"; return; }

        var capturedAvgHp = avgHp;
        var capturedRaidwideImminent = raidwideImminent;
        var action = SGEActions.Panhaima;

        scheduler.PushOgcd(AsclepiusAbilities.Panhaima, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Panhaima";
                context.Debug.PanhaimaState = "Executing";
                partyCoord?.OnCooldownUsed(action.ActionId, 120_000);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = capturedRaidwideImminent
                        ? "Panhaima - raidwide incoming!"
                        : $"Panhaima - party at {capturedAvgHp:P0}";

                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        capturedRaidwideImminent ? "Raidwide imminent!" : $"Threshold: {config.PanhaimaThreshold:P0}",
                        "200 potency shield x5 stacks (party-wide)",
                        "Shields refresh when broken",
                        "120s cooldown",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Panhaima",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Panhaima placed on party at {capturedAvgHp:P0} avg HP. {(capturedRaidwideImminent ? "Raidwide detected - shields will absorb incoming damage!" : "Proactive party shielding.")} Provides 5 stacks of 200 potency shields to ALL party members that refresh when consumed. Amazing for multi-hit raidwides!",
                        Factors = factors,
                        Alternatives = _panhaimaAlternatives,
                        Tip = "Panhaima is the AoE version of Haima! Use it before multi-hit raidwides where the party will take repeated damage. Any remaining shield value heals when it expires. Excellent for prog where damage patterns are unknown.",
                        ConceptId = SgeConcepts.PanhaimaUsage,
                        Priority = capturedRaidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });
                }
            });
    }
}
