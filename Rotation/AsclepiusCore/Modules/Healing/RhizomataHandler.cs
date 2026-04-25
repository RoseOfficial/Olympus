using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

public sealed class RhizomataHandler : IHealingHandler
{
    public int Priority => 50;
    public string Name => "Rhizomata";

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableRhizomata) return;
        if (player.Level < SGEActions.Rhizomata.MinLevel) return;
        if (!context.ActionService.IsActionReady(SGEActions.Rhizomata.ActionId)) { context.Debug.RhizomataState = "On CD"; return; }
        if (context.AddersgallStacks >= 3) { context.Debug.RhizomataState = "At max stacks"; return; }

        var action = SGEActions.Rhizomata;
        var capturedStacks = context.AddersgallStacks;
        var capturedTimer = context.AddersgallTimer;

        // Preventing cap path
        if (config.PreventAddersgallCap && context.AddersgallStacks >= 2 && context.AddersgallTimer < 5f)
        {
            scheduler.PushOgcd(AsclepiusAbilities.Rhizomata, player.GameObjectId, priority: Priority,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.PlanningState = "Rhizomata";
                    context.Debug.RhizomataState = "Preventing cap";

                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.UtcNow,
                            ActionId = action.ActionId,
                            ActionName = "Rhizomata",
                            Category = "Resource",
                            TargetName = "Self",
                            ShortReason = $"Rhizomata - preventing Addersgall cap ({capturedStacks}/3, {capturedTimer:F1}s)",
                            DetailedReason = $"Rhizomata used to prevent Addersgall overcap. Currently at {capturedStacks}/3 stacks with {capturedTimer:F1}s until next natural regen. Using Rhizomata now banks an extra stack that won't be lost.",
                            Factors = new[]
                            {
                                $"Current stacks: {capturedStacks}/3",
                                $"Timer to next regen: {capturedTimer:F1}s",
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
                });
            return;
        }

        // Out of Addersgall path
        if (context.AddersgallStacks == 0)
        {
            scheduler.PushOgcd(AsclepiusAbilities.Rhizomata, player.GameObjectId, priority: Priority,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.PlanningState = "Rhizomata";
                    context.Debug.RhizomataState = "Out of Addersgall";

                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.UtcNow,
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
                });
            return;
        }

        context.Debug.RhizomataState = "Saving";
    }
}
