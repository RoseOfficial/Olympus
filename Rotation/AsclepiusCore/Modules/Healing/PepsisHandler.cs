using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Pepsis for Sage. Priority 45 in the oGCD list.
/// </summary>
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

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryPepsis(context);

    private bool TryPepsis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePepsis)
            return false;

        if (player.Level < SGEActions.Pepsis.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Pepsis.ActionId))
        {
            context.Debug.PepsisState = "On CD";
            return false;
        }

        // Count party members with Eukrasian shields
        var shieldedCount = 0;
        foreach (var member in context.PartyHelper.GetAllPartyMembers(player))
        {
            if (AsclepiusStatusHelper.HasEukrasianDiagnosisShield(member) ||
                AsclepiusStatusHelper.HasEukrasianPrognosisShield(member))
            {
                shieldedCount++;
            }
        }

        if (shieldedCount < config.AoEHealMinTargets)
        {
            context.Debug.PepsisState = $"{shieldedCount} shielded";
            return false;
        }

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.PepsisThreshold)
        {
            context.Debug.PepsisState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Pepsis;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Pepsis";
            context.Debug.PepsisState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Pepsis - converting {shieldedCount} shields to heals";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Shielded members: {shieldedCount}",
                    "450 potency heal per E.Diagnosis shield",
                    "540 potency heal per E.Prognosis shield",
                    "Consumes shields instantly",
                };

                var alternatives = _pepsisAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Pepsis",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Pepsis converted {shieldedCount} Eukrasian shields into healing. Party at {avgHp:P0} avg HP. E.Diagnosis shields become 450 potency heals, E.Prognosis shields become 540 potency heals. Great when shields won't be consumed by incoming damage but healing is needed!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Pepsis is situational but powerful! If you've applied shields but damage has already passed, use Pepsis to convert those shields into healing. Also useful in emergencies - shield then immediately Pepsis for GCD heal + instant heal combo.",
                    ConceptId = SgeConcepts.PepsisUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
