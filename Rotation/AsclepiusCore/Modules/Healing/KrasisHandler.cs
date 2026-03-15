using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Krasis for Sage. Priority 55 in the oGCD list.
/// </summary>
public sealed class KrasisHandler : IHealingHandler
{
    public int Priority => 55;
    public string Name => "Krasis";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryKrasis(context);

    private bool TryKrasis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableKrasis)
            return false;

        if (player.Level < SGEActions.Krasis.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Krasis.ActionId))
        {
            context.Debug.KrasisState = "On CD";
            return false;
        }

        // Find a target that needs healing boost
        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
        {
            context.Debug.KrasisState = "No target";
            return false;
        }

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
        {
            context.Debug.KrasisState = "Skipped (reserved)";
            return false;
        }

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.KrasisThreshold)
        {
            context.Debug.KrasisState = $"Target at {hpPercent:P0}";
            return false;
        }

        // Don't stack with existing Krasis
        if (AsclepiusStatusHelper.HasKrasis(target))
        {
            context.Debug.KrasisState = "Already has Krasis";
            return false;
        }

        var action = SGEActions.Krasis;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target - Krasis increases healing received on this target
            var healAmount = 1000; // Krasis boosts heals, rough estimate for coordination
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Krasis";
            context.Debug.KrasisState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Krasis",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = $"Krasis on {targetName} at {hpPercent:P0} - boosting heals",
                    DetailedReason = $"Krasis placed on {targetName} at {hpPercent:P0} HP. Provides a 20% healing received buff for 10 seconds. Use before your biggest heals to maximize their effectiveness!",
                    Factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
                        $"Threshold: {config.KrasisThreshold:P0}",
                        "20% healing received buff (10s)",
                        "60s cooldown",
                    },
                    Alternatives = new[]
                    {
                        "Direct heals without buff",
                        "Zoe (50% buff for next GCD heal)",
                        "Wait for natural healing",
                    },
                    Tip = "Krasis increases ALL healing the target receives by 20% for 10 seconds. This includes your co-healer's heals and even the target's self-heals! Great for tanks taking heavy damage.",
                    ConceptId = SgeConcepts.KrasisUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
