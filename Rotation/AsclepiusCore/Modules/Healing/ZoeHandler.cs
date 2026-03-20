using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Zoe for Sage. Priority 60 in the oGCD list.
/// </summary>
public sealed class ZoeHandler : IHealingHandler
{
    public int Priority => 60;
    public string Name => "Zoe";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryZoe(context);

    private bool TryZoe(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableZoe)
            return false;

        if (player.Level < SGEActions.Zoe.MinLevel)
            return false;

        // Already have Zoe active
        if (context.HasZoe)
        {
            context.Debug.ZoeState = "Active";
            return false;
        }

        if (!context.ActionService.IsActionReady(SGEActions.Zoe.ActionId))
        {
            context.Debug.ZoeState = "On CD";
            return false;
        }

        // Use Zoe before a big heal
        var (avgHp, lowestHp, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Use when someone is critically low and we'll need a big heal
        if (lowestHp > config.DiagnosisThreshold)
        {
            context.Debug.ZoeState = $"Lowest HP {lowestHp:P0}";
            return false;
        }

        var action = SGEActions.Zoe;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Zoe";
            context.Debug.ZoeState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Zoe",
                    Category = "Healing",
                    TargetName = "Self (buff)",
                    ShortReason = $"Zoe - preparing 50% boosted GCD heal (lowest: {lowestHp:P0})",
                    DetailedReason = $"Zoe activated to boost the next GCD heal by 50%. Party member at {lowestHp:P0} HP - the boosted heal will provide much more recovery. Zoe works on Diagnosis, Prognosis, Pneuma, and Eukrasian heals!",
                    Factors = new[]
                    {
                        $"Lowest HP: {lowestHp:P0}",
                        "50% potency boost on next GCD heal",
                        "90s cooldown",
                        "Works on: Diagnosis, Prognosis, Pneuma, E.Diagnosis, E.Prognosis",
                    },
                    Alternatives = new[]
                    {
                        "Krasis (20% healing received buff)",
                        "Direct heal without buff",
                        "oGCD heals instead",
                    },
                    Tip = "Zoe is a 50% boost to your next GCD heal! Best paired with Pneuma (600 potency → 900 potency party heal!) or E.Prognosis for massive party shields. Don't waste it on small heals!",
                    ConceptId = SgeConcepts.ZoeUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
