using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Pneuma for Sage. Priority 10 in the GCD list.
/// </summary>
public sealed class PneumaHandler : IHealingHandler
{
    private static readonly string[] _pneumaAlternatives =
    {
        "Save for better timing",
        "Use for pure DPS (skip if party healthy)",
        "Ixochole + Dosis (separate heal and damage)",
    };

    public int Priority => 10;
    public string Name => "Pneuma";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        if (isMoving) return false; // cast time
        return TryPneuma(context);
    }

    private bool TryPneuma(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePneuma)
            return false;

        if (player.Level < SGEActions.Pneuma.MinLevel)
        {
            context.Debug.PneumaState = "Level too low";
            return false;
        }

        // Pneuma has a 2-minute cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Pneuma.ActionId))
        {
            context.Debug.PneumaState = "On CD";
            return false;
        }

        // Check if we have a target
        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SGEActions.Pneuma.Range,
            player);

        if (enemy == null)
        {
            context.Debug.PneumaState = "No enemy";
            return false;
        }

        // Use Pneuma when party needs healing AND we can hit an enemy
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.PneumaThreshold && injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.PneumaState = $"Party HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Pneuma;
        if (context.ActionService.ExecuteGcd(action, enemy.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Pneuma";
            context.Debug.PneumaState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Pneuma - {injuredCount} injured, enemy in range";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    "330 potency damage line AoE",
                    "600 potency party heal",
                    "120s cooldown",
                };

                var alternatives = _pneumaAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Pneuma",
                    Category = "Healing",
                    TargetName = "Enemy/Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Pneuma used with {injuredCount} injured party members and enemy in range. Deals 330 potency damage in a line AND heals party for 600 potency. This is SGE's signature ability - massive healing that also does damage! Perfect timing when party needs healing and you can hit enemies.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Pneuma is INSANE value when you need healing! It's a 600 potency party heal that ALSO deals damage. Time it so you can hit enemies while the party needs healing. Don't hold it too long - 2 minute cooldown is still short!",
                    ConceptId = SgeConcepts.PneumaUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }
}
