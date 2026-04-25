using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

public sealed class PrognosisHandler : IHealingHandler
{
    public int Priority => 30;
    public string Name => "Prognosis";

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (isMoving) return;

        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePrognosis) return;
        if (player.Level < SGEActions.Prognosis.MinLevel) return;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        if (injuredCount < config.AoEHealMinTargets) { context.Debug.AoEStatus = $"{injuredCount} < {config.AoEHealMinTargets} injured"; return; }
        if (avgHp > config.AoEHealThreshold) { context.Debug.AoEStatus = $"Avg HP {avgHp:P0}"; return; }

        var action = SGEActions.Prognosis;
        var castTimeMs = (int)(action.CastTime * 1000);
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, castTimeMs))
        {
            context.Debug.AoEStatus = "Skipped (remote AOE reserved)";
            return;
        }

        var capturedAvgHp = avgHp;
        var capturedInjuredCount = injuredCount;

        scheduler.PushGcd(AsclepiusAbilities.Prognosis, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Prognosis";
                context.Debug.AoEStatus = "Executing";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Prognosis",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = $"Prognosis - {capturedInjuredCount} injured at {capturedAvgHp:P0}",
                        DetailedReason = $"Prognosis cast for {capturedInjuredCount} injured party members at {capturedAvgHp:P0} average HP. This is SGE's basic GCD party heal - use when oGCD options are exhausted and you need raw healing throughput.",
                        Factors = new[]
                        {
                            $"Party avg HP: {capturedAvgHp:P0}",
                            $"Injured count: {capturedInjuredCount}",
                            "300 potency AoE heal",
                            "2s cast time",
                            "800 MP cost",
                        },
                        Alternatives = new[]
                        {
                            "Ixochole (oGCD, instant, Addersgall)",
                            "Kerachole (oGCD regen + mit, Addersgall)",
                            "E.Prognosis (instant shield)",
                        },
                        Tip = "Prognosis is your fallback AoE heal when oGCDs are exhausted. It has a cast time, so prefer instant options like Ixochole or E.Prognosis when available. Only hard-cast when you truly need the raw healing!",
                        ConceptId = SgeConcepts.EmergencyHealing,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }
}
