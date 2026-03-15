using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Handles Emergency Tactics for Scholar. Priority 40 in the oGCD list.
/// No resource cost. Converts the next shield spell to a pure heal.
/// </summary>
public sealed class EmergencyTacticsHandler : IHealingHandler
{
    public int Priority => 40;
    public string Name => "EmergencyTactics";

    private static readonly string[] _emergencyTacticsAlternatives =
    {
        "Lustrate (uses Aetherflow)",
        "Wait for shield to break",
        "Use Physick (no shield component)",
    };

    public bool TryExecute(IAthenaContext context, bool isMoving)
        => TryEmergencyTactics(context);

    private bool TryEmergencyTactics(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableEmergencyTactics)
            return false;

        if (player.Level < SCHActions.EmergencyTactics.MinLevel)
            return false;

        // Already have Emergency Tactics active
        if (context.StatusHelper.HasEmergencyTactics(player))
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.EmergencyTactics.ActionId))
            return false;

        // Use when we need raw healing, not shields
        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.EmergencyTacticsThreshold)
            return false;

        // Check if target already has Galvanize (shield would be wasted)
        if (context.StatusHelper.HasGalvanize(target))
        {
            var action = SCHActions.EmergencyTactics;
            if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Emergency Tactics";

                // Training mode: capture explanation
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = target.Name?.TextValue ?? "Unknown";
                    var shortReason = $"Emergency Tactics - {targetName} already shielded";

                    var factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
                        $"Threshold: {config.EmergencyTacticsThreshold:P0}",
                        "Target has Galvanize (shield)",
                        "Converts next shield spell to pure heal",
                        "Prevents shield overwrite waste",
                    };

                    var alternatives = _emergencyTacticsAlternatives;

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.Now,
                        ActionId = action.ActionId,
                        ActionName = "Emergency Tactics",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Emergency Tactics before healing {targetName} at {hpPercent:P0}. Target already has Galvanize shield, so using Adloquium would overwrite it (wasting the shield). Emergency Tactics converts the shield portion to healing, getting full value from the spell.",
                        Factors = factors,
                        Alternatives = alternatives,
                        Tip = "Emergency Tactics prevents shield waste when the target already has a shield. It's also useful when you need raw healing instead of shields after a raidwide.",
                        ConceptId = SchConcepts.EmergencyTacticsUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }

                return true;
            }
        }

        return false;
    }
}
