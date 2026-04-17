using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Diagnosis for Sage. Priority 40 in the GCD list.
/// </summary>
public sealed class DiagnosisHandler : IHealingHandler
{
    public int Priority => 40;
    public string Name => "Diagnosis";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        if (isMoving) return false; // cast time
        return TryDiagnosis(context);
    }

    private bool TryDiagnosis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;

        if (!config.EnableDiagnosis)
            return false;

        var player = context.Player;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.DiagnosisThreshold)
            return false;

        // Co-healer awareness: skip if another healer's pending cast will already cover this target
        if (CoHealerAwarenessHelper.CoHealerWillCover(
                context.Configuration.Healing.EnableCoHealerAwareness,
                context.CoHealerDetectionService,
                target,
                context.Configuration.Healing.CoHealerPendingHealThreshold))
            return false;

        var action = SGEActions.Diagnosis;
        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            var castTimeMs = (int)(action.CastTime * 1000);
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, castTimeMs);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Diagnosis";
            context.LogHealDecision(target.Name?.TextValue ?? "Unknown", hpPercent, action.Name, action.HealPotency, "Low HP");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Diagnosis",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = $"Diagnosis on {targetName} at {hpPercent:P0} (GCD heal)",
                    DetailedReason = $"Diagnosis cast on {targetName} at {hpPercent:P0} HP. This is SGE's basic GCD single-target heal - a fallback when Addersgall heals aren't available. Has a cast time, so prefer oGCDs when possible.",
                    Factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
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

            return true;
        }

        return false;
    }
}
