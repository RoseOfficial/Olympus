using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

public sealed class DiagnosisHandler : IHealingHandler
{
    public int Priority => 40;
    public string Name => "Diagnosis";

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (isMoving) return;

        var config = context.Configuration.Sage;
        if (!config.EnableDiagnosis) return;

        var player = context.Player;

        var target = context.Configuration.Healing.UseDamageIntakeTriage
            ? context.PartyHelper.FindMostEndangeredPartyMember(
                player, context.DamageIntakeService, 0, context.DamageTrendService, context.ShieldTrackingService)
            : context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null) return;

        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.DiagnosisThreshold) return;

        if (CoHealerAwarenessHelper.CoHealerWillCover(
                context.Configuration.Healing.EnableCoHealerAwareness,
                context.CoHealerDetectionService,
                target,
                context.Configuration.Healing.CoHealerPendingHealThreshold))
            return;

        var action = SGEActions.Diagnosis;
        var capturedTarget = target;
        var capturedHpPercent = hpPercent;
        var capturedHealAmount = action.HealPotency * 10;
        var capturedCastTimeMs = (int)(action.CastTime * 1000);

        scheduler.PushGcd(AsclepiusAbilities.Diagnosis, target.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, capturedHealAmount, action.ActionId, capturedCastTimeMs);

                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Diagnosis";
                context.LogHealDecision(capturedTarget.Name?.TextValue ?? "Unknown", capturedHpPercent, action.Name, action.HealPotency, "Low HP");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Diagnosis",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = $"Diagnosis on {targetName} at {capturedHpPercent:P0} (GCD heal)",
                        DetailedReason = $"Diagnosis cast on {targetName} at {capturedHpPercent:P0} HP. This is SGE's basic GCD single-target heal - a fallback when Addersgall heals aren't available. Has a cast time, so prefer oGCDs when possible.",
                        Factors = new[]
                        {
                            $"Target HP: {capturedHpPercent:P0}",
                            "450 potency heal",
                            "1.5s cast time",
                            "700 MP cost",
                        },
                        Alternatives = new[]
                        {
                            "Druochole (oGCD, instant, restores MP)",
                            "Taurochole (oGCD for tanks, adds mit)",
                            "E.Diagnosis (instant shield)",
                            "Kardia passive healing",
                        },
                        Tip = "Diagnosis is your fallback single-target heal. You should rarely need it because Druochole (oGCD, restores MP!) is almost always better. Only use Diagnosis when Addersgall is empty and Rhizomata is on cooldown.",
                        ConceptId = SgeConcepts.EmergencyHealing,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }
}
