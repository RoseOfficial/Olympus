using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

public sealed class PhysisIIHandler : IHealingHandler
{
    private static readonly string[] _physisIIAlternatives =
    {
        "Kerachole (regen + mit, costs Addersgall)",
        "Ixochole (instant heal, costs Addersgall)",
        "Holos (emergency heal + shield + mit)",
    };

    public int Priority => 25;
    public string Name => "PhysisII";

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhysisII) return;
        if (player.Level < SGEActions.PhysisII.MinLevel) return;
        if (!context.ActionService.IsActionReady(SGEActions.PhysisII.ActionId)) { context.Debug.PhysisIIState = "On CD"; return; }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (injuredCount < config.AoEHealMinTargets) { context.Debug.PhysisIIState = $"{injuredCount} injured"; return; }
        if (avgHp > config.PhysisIIThreshold) { context.Debug.PhysisIIState = $"Avg HP {avgHp:P0}"; return; }

        var capturedAvgHp = avgHp;
        var capturedInjuredCount = injuredCount;
        var action = SGEActions.PhysisII;

        scheduler.PushOgcd(AsclepiusAbilities.PhysisII, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Physis II";
                context.Debug.PhysisIIState = "Executing";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Physis II - {capturedInjuredCount} injured at {capturedAvgHp:P0}";
                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Injured count: {capturedInjuredCount}",
                        "100 potency regen/tick (15s)",
                        "10% healing received buff",
                        "60s cooldown, free (no cost)",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Physis II",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Physis II used on {capturedInjuredCount} injured party members at {capturedAvgHp:P0} average HP. Provides 100 potency regen/tick for 15 seconds PLUS 10% healing received buff. This is FREE (no Addersgall cost) - use it liberally for sustained party healing!",
                        Factors = factors,
                        Alternatives = _physisIIAlternatives,
                        Tip = "Physis II is FREE healing! The 10% healing received buff also boosts your other heals. Use it early in damage phases - the regen ticks will heal over time while you DPS.",
                        ConceptId = SgeConcepts.PhysisUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }
}
