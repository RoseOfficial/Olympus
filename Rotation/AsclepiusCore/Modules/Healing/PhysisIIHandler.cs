using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Physis II for Sage: free AoE oGCD HoT + healing buff.
/// Priority 25 in the oGCD list.
/// </summary>
public sealed class PhysisIIHandler : IHealingHandler
{
    private static readonly string[] _physisIIAlternatives =
    {
        "Kerachole (regen + mit, costs Addersgall)",
        "Ixochole (instant heal, costs Addersgall)",
        "Holos (emergency heal + shield + mit)",
    };

    public int Priority => 25;
    public string Name => "PhysisII";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryPhysisII(context);

    private bool TryPhysisII(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhysisII)
            return false;

        if (player.Level < SGEActions.PhysisII.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.PhysisII.ActionId))
        {
            context.Debug.PhysisIIState = "On CD";
            return false;
        }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.PhysisIIState = $"{injuredCount} injured";
            return false;
        }

        if (avgHp > config.PhysisIIThreshold)
        {
            context.Debug.PhysisIIState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.PhysisII;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Physis II";
            context.Debug.PhysisIIState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Physis II - {injuredCount} injured at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    "100 potency regen/tick (15s)",
                    "10% healing received buff",
                    "60s cooldown, free (no cost)",
                };

                var alternatives = _physisIIAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Physis II",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Physis II used on {injuredCount} injured party members at {avgHp:P0} average HP. Provides 100 potency regen/tick for 15 seconds PLUS 10% healing received buff. This is FREE (no Addersgall cost) - use it liberally for sustained party healing!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Physis II is FREE healing! The 10% healing received buff also boosts your other heals. Use it early in damage phases - the regen ticks will heal over time while you DPS.",
                    ConceptId = SgeConcepts.PhysisUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
