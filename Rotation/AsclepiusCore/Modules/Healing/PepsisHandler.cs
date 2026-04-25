using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

public sealed class PepsisHandler : IHealingHandler
{
    private static readonly string[] _pepsisAlternatives =
    {
        "Let shields absorb damage naturally",
        "Use other heals instead",
        "Re-shield for future damage",
    };

    public int Priority => 45;
    public string Name => "Pepsis";

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePepsis) return;
        if (player.Level < SGEActions.Pepsis.MinLevel) return;
        if (!context.ActionService.IsActionReady(SGEActions.Pepsis.ActionId)) { context.Debug.PepsisState = "On CD"; return; }

        var shieldedCount = 0;
        foreach (var member in context.PartyHelper.GetAllPartyMembers(player))
        {
            if (AsclepiusStatusHelper.HasEukrasianDiagnosisShield(member) ||
                AsclepiusStatusHelper.HasEukrasianPrognosisShield(member))
            {
                shieldedCount++;
            }
        }

        if (shieldedCount < config.AoEHealMinTargets) { context.Debug.PepsisState = $"{shieldedCount} shielded"; return; }

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.PepsisThreshold) { context.Debug.PepsisState = $"Avg HP {avgHp:P0}"; return; }

        var capturedAvgHp = avgHp;
        var capturedShieldedCount = shieldedCount;
        var action = SGEActions.Pepsis;

        scheduler.PushOgcd(AsclepiusAbilities.Pepsis, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Pepsis";
                context.Debug.PepsisState = "Executing";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Pepsis - converting {capturedShieldedCount} shields to heals";
                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Shielded members: {capturedShieldedCount}",
                        "450 potency heal per E.Diagnosis shield",
                        "540 potency heal per E.Prognosis shield",
                        "Consumes shields instantly",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Pepsis",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Pepsis converted {capturedShieldedCount} Eukrasian shields into healing. Party at {capturedAvgHp:P0} avg HP. E.Diagnosis shields become 450 potency heals, E.Prognosis shields become 540 potency heals. Great when shields won't be consumed by incoming damage but healing is needed!",
                        Factors = factors,
                        Alternatives = _pepsisAlternatives,
                        Tip = "Pepsis is situational but powerful! If you've applied shields but damage has already passed, use Pepsis to convert those shields into healing. Also useful in emergencies - shield then immediately Pepsis for GCD heal + instant heal combo.",
                        ConceptId = SgeConcepts.PepsisUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }
}
