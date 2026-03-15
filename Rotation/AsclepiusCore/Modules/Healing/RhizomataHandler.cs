using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Rhizomata for Sage. Priority 50 in the oGCD list.
/// </summary>
public sealed class RhizomataHandler : IHealingHandler
{
    public int Priority => 50;
    public string Name => "Rhizomata";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryRhizomata(context);

    private bool TryRhizomata(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableRhizomata)
            return false;

        if (player.Level < SGEActions.Rhizomata.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Rhizomata.ActionId))
        {
            context.Debug.RhizomataState = "On CD";
            return false;
        }

        // Don't overcap Addersgall
        if (context.AddersgallStacks >= 3)
        {
            context.Debug.RhizomataState = "At max stacks";
            return false;
        }

        // Use proactively to prevent overcapping
        if (config.PreventAddersgallCap && context.AddersgallStacks >= 2 && context.AddersgallTimer < 5f)
        {
            // About to cap, use Rhizomata to bank a stack
            var action = SGEActions.Rhizomata;
            if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Rhizomata";
                context.Debug.RhizomataState = "Preventing cap";

                // Training mode: capture explanation
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var stacks = context.AddersgallStacks;
                    var timer = context.AddersgallTimer;

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.Now,
                        ActionId = action.ActionId,
                        ActionName = "Rhizomata",
                        Category = "Resource",
                        TargetName = "Self",
                        ShortReason = $"Rhizomata - preventing Addersgall cap ({stacks}/3, {timer:F1}s)",
                        DetailedReason = $"Rhizomata used to prevent Addersgall overcap. Currently at {stacks}/3 stacks with {timer:F1}s until next natural regen. Using Rhizomata now banks an extra stack that won't be lost.",
                        Factors = new[]
                        {
                            $"Current stacks: {stacks}/3",
                            $"Timer to next regen: {timer:F1}s",
                            "Would overcap if not used",
                            "90s cooldown",
                        },
                        Alternatives = new[]
                        {
                            "Spend Addersgall first (Druochole, Kerachole, etc.)",
                            "Accept losing the stack",
                        },
                        Tip = "Rhizomata grants a free Addersgall stack on a 90s CD. Use it when you're at 2 stacks and about to regen naturally, or when you're empty and need healing resources!",
                        ConceptId = SgeConcepts.RhizomataUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }

                return true;
            }
        }

        // Use when low on Addersgall
        if (context.AddersgallStacks == 0)
        {
            var action = SGEActions.Rhizomata;
            if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Rhizomata";
                context.Debug.RhizomataState = "Out of Addersgall";

                // Training mode: capture explanation
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.Now,
                        ActionId = action.ActionId,
                        ActionName = "Rhizomata",
                        Category = "Resource",
                        TargetName = "Self",
                        ShortReason = "Rhizomata - out of Addersgall!",
                        DetailedReason = "Rhizomata used because Addersgall is empty. This provides an immediate stack for emergency healing options like Druochole, Taurochole, Ixochole, or Kerachole.",
                        Factors = new[]
                        {
                            "Addersgall: 0/3",
                            "Emergency resource generation",
                            "90s cooldown",
                        },
                        Alternatives = new[]
                        {
                            "Wait for natural regen (20s)",
                            "Use non-Addersgall heals (Physis, Holos)",
                        },
                        Tip = "Don't be afraid to use Rhizomata when empty! It's a 90s CD that gives you instant access to your best heals. Better to have it available when you need healing!",
                        ConceptId = SgeConcepts.RhizomataUsage,
                        Priority = ExplanationPriority.High,
                    });
                }

                return true;
            }
        }

        context.Debug.RhizomataState = "Saving";
        return false;
    }
}
