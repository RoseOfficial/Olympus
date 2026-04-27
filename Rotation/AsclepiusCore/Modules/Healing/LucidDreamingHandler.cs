using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common.RoleActionHelpers;
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

        if (!config.HealerShared.EnableLucidDreaming) { context.Debug.LucidState = "Disabled"; return; }

        var preCallMp = context.Player.MaxMp > 0 ? (float)context.Player.CurrentMp / context.Player.MaxMp : 1f;

        RoleActionPushers.TryPushLucidDreaming(
            context, scheduler, AsclepiusAbilities.LucidDreaming,
            mpThresholdPct: config.HealerShared.LucidDreamingThreshold,
            priority: 70,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.LucidDreaming.Name;
                context.Debug.LucidState = $"Lucid Dreaming (MP {preCallMp:P0})";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = RoleActions.LucidDreaming.ActionId,
                        ActionName = "Lucid Dreaming",
                        Category = "Resource",
                        TargetName = "Self",
                        ShortReason = $"Lucid Dreaming at {preCallMp:P0} MP",
                        DetailedReason = $"Lucid Dreaming activated at {preCallMp:P0} MP (threshold: {config.HealerShared.LucidDreamingThreshold:P0}). Restores 3850 MP over 21 seconds. SGE is less MP-dependent than other healers (Addersgall heals restore MP!), but Lucid is still important for GCD heals and raises.",
                        Factors = new[]
                        {
                            $"Current MP: {preCallMp:P0}",
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

        // Update LucidState even when not pushed (for UI visibility)
        if (preCallMp > config.HealerShared.LucidDreamingThreshold)
            context.Debug.LucidState = $"MP {preCallMp:P0}";
    }
}
