using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

public sealed class LucidDreamingHandler : IHealingHandler
{
    public int Priority => 70;
    public string Name => "LucidDreaming";

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.HealerShared.EnableLucidDreaming) { context.Debug.LucidState = "Disabled"; return; }
        if (player.Level < RoleActions.LucidDreaming.MinLevel) return;
        if (AsclepiusStatusHelper.HasLucidDreaming(player)) { context.Debug.LucidState = "Already active"; return; }
        if (!context.ActionService.IsActionReady(RoleActions.LucidDreaming.ActionId)) { context.Debug.LucidState = "On CD"; return; }

        var mpPercent = (float)player.CurrentMp / player.MaxMp;
        if (mpPercent > config.HealerShared.LucidDreamingThreshold) { context.Debug.LucidState = $"MP {mpPercent:P0}"; return; }

        var capturedMpPercent = mpPercent;
        var action = RoleActions.LucidDreaming;

        scheduler.PushOgcd(AsclepiusAbilities.LucidDreaming, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Lucid Dreaming";
                context.Debug.LucidState = "Executing";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Lucid Dreaming",
                        Category = "Resource",
                        TargetName = "Self",
                        ShortReason = $"Lucid Dreaming at {capturedMpPercent:P0} MP",
                        DetailedReason = $"Lucid Dreaming activated at {capturedMpPercent:P0} MP (threshold: {config.HealerShared.LucidDreamingThreshold:P0}). Restores 3850 MP over 21 seconds. SGE is less MP-dependent than other healers (Addersgall heals restore MP!), but Lucid is still important for GCD heals and raises.",
                        Factors = new[]
                        {
                            $"Current MP: {capturedMpPercent:P0}",
                            $"Threshold: {config.HealerShared.LucidDreamingThreshold:P0}",
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
            });
    }
}
