using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Lucid Dreaming for Sage. Priority 70 in the oGCD list.
/// </summary>
public sealed class LucidDreamingHandler : IHealingHandler
{
    public int Priority => 70;
    public string Name => "LucidDreaming";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryLucidDreaming(context);

    private bool TryLucidDreaming(IAsclepiusContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Sage.EnableLucidDreaming)
        {
            context.Debug.LucidState = "Disabled";
            return false;
        }

        if (player.Level < RoleActions.LucidDreaming.MinLevel)
            return false;

        if (AsclepiusStatusHelper.HasLucidDreaming(player))
        {
            context.Debug.LucidState = "Already active";
            return false;
        }

        if (!context.ActionService.IsActionReady(RoleActions.LucidDreaming.ActionId))
        {
            context.Debug.LucidState = "On CD";
            return false;
        }

        var mpPercent = (float)player.CurrentMp / player.MaxMp;
        if (mpPercent > config.Sage.LucidDreamingThreshold)
        {
            context.Debug.LucidState = $"MP {mpPercent:P0}";
            return false;
        }

        var action = RoleActions.LucidDreaming;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Lucid Dreaming";
            context.Debug.LucidState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Lucid Dreaming",
                    Category = "Resource",
                    TargetName = "Self",
                    ShortReason = $"Lucid Dreaming at {mpPercent:P0} MP",
                    DetailedReason = $"Lucid Dreaming activated at {mpPercent:P0} MP (threshold: {config.Sage.LucidDreamingThreshold:P0}). Restores 3850 MP over 21 seconds. SGE is less MP-dependent than other healers (Addersgall heals restore MP!), but Lucid is still important for GCD heals and raises.",
                    Factors = new[]
                    {
                        $"Current MP: {mpPercent:P0}",
                        $"Threshold: {config.Sage.LucidDreamingThreshold:P0}",
                        "3850 MP over 21s",
                        "60s cooldown",
                    },
                    Alternatives = new[]
                    {
                        "Use Addersgall heals (restore 700 MP each)",
                        "Wait for natural MP regen",
                        "Accept MP constraints",
                    },
                    Tip = "SGE has the best MP economy of all healers! Addersgall heals (Druochole, Kerachole, etc.) actually RESTORE 700 MP. Use Lucid mainly for GCD heals and raises. Don't panic about MP as SGE!",
                    ConceptId = SgeConcepts.AddersgallManagement,
                    Priority = ExplanationPriority.Low,
                });
            }

            return true;
        }

        return false;
    }
}
