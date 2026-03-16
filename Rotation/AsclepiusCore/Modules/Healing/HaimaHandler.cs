using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Haima for Sage. Priority 35 in the oGCD list.
/// </summary>
public sealed class HaimaHandler : IHealingHandler
{
    private static readonly string[] _haimaAlternatives =
    {
        "Taurochole (heal + 10% mit)",
        "E.Diagnosis (GCD shield)",
        "Panhaima (AoE version)",
    };

    public int Priority => 35;
    public string Name => "Haima";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryHaima(context);

    private bool TryHaima(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableHaima)
            return false;

        if (player.Level < SGEActions.Haima.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Haima.ActionId))
        {
            context.Debug.HaimaState = "On CD";
            return false;
        }

        // Haima is best for tanks taking consistent damage
        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null)
        {
            context.Debug.HaimaState = "No tank";
            return false;
        }

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(tank.EntityId, context.PartyCoordinationService))
        {
            context.Debug.HaimaState = "Skipped (reserved)";
            return false;
        }

        var hpPercent = tank.MaxHp > 0 ? (float)tank.CurrentHp / tank.MaxHp : 1f;

        // Don't use if tank already has Haima
        if (AsclepiusStatusHelper.HasHaima(tank))
        {
            context.Debug.HaimaState = "Already has Haima";
            return false;
        }

        // Check if tank buster is imminent - use proactively
        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out var busterSource);

        // Use if tank buster is coming or tank HP is low
        if (hpPercent > config.HaimaThreshold && !tankBusterImminent)
        {
            context.Debug.HaimaState = $"Tank at {hpPercent:P0}";
            return false;
        }

        var action = SGEActions.Haima;
        if (context.ActionService.ExecuteOgcd(action, tank.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate for shield value
            context.HealingCoordination.TryReserveTarget(
                tank.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Haima";
            context.Debug.HaimaState = "Executing";
            context.Debug.HaimaTarget = tank.Name?.TextValue ?? "Unknown";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var tankName = tank.Name?.TextValue ?? "Unknown";

                var shortReason = tankBusterImminent
                    ? $"Haima on {tankName} - tankbuster incoming!"
                    : $"Haima on {tankName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Tank HP: {hpPercent:P0}",
                    tankBusterImminent ? "Tankbuster imminent!" : $"Threshold: {config.HaimaThreshold:P0}",
                    "300 potency shield x5 stacks",
                    "Shield refreshes when broken",
                    "120s cooldown",
                };

                var alternatives = _haimaAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Haima",
                    Category = "Healing",
                    TargetName = tankName,
                    ShortReason = shortReason,
                    DetailedReason = $"Haima placed on tank {tankName} at {hpPercent:P0} HP. {(tankBusterImminent ? "Tankbuster detected - Haima will absorb multiple hits!" : "Proactive shield for tank damage.")} Provides 5 stacks of 300 potency shields that refresh when consumed. Perfect for sustained tank damage or multi-hit tankbusters!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Haima is AMAZING for multi-hit tankbusters! Each time the shield breaks, a new one appears (up to 5 times). It heals for any remaining shield value when it expires. Pre-place before tankbusters!",
                    ConceptId = SgeConcepts.HaimaUsage,
                    Priority = tankBusterImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
