using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles single-target oGCD heals for Sage: Druochole and Taurochole.
/// Priority 10 in the oGCD list.
/// </summary>
public sealed class SingleTargetOgcdHandler : IHealingHandler
{
    private static readonly string[] _druocholeAlternatives =
    {
        "Taurochole (if tank, adds 10% mit)",
        "Diagnosis (GCD, save Addersgall)",
        "Kardia healing (passive)",
    };

    private static readonly string[] _taurocholeAlternatives =
    {
        "Druochole (no mit, no shared CD)",
        "Kerachole (AoE version)",
        "Haima (multi-hit shield)",
    };

    public int Priority => 10;
    public string Name => "SingleTargetOgcd";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        if (TryDruochole(context)) return true;
        if (TryTaurochole(context)) return true;
        return false;
    }

    private bool TryDruochole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (player.Level < SGEActions.Druochole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.DruocholeState = "No Addersgall";
            return false;
        }

        // Reserve stacks if configured
        if (context.AddersgallStacks <= config.AddersgallReserve)
        {
            context.Debug.DruocholeState = $"Reserved ({config.AddersgallReserve})";
            return false;
        }

        // Find target needing healing
        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
        {
            context.Debug.DruocholeState = "No target";
            return false;
        }

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
        {
            context.Debug.DruocholeState = "Skipped (reserved)";
            return false;
        }

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.DruocholeThreshold)
        {
            context.Debug.DruocholeState = $"{hpPercent:P0} > {config.DruocholeThreshold:P0}";
            return false;
        }

        var action = SGEActions.Druochole;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Druochole";
            context.Debug.DruocholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"Target at {hpPercent:P0}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var stacks = context.AddersgallStacks;

                var shortReason = $"Druochole on {targetName} at {hpPercent:P0} ({stacks} stacks)";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.DruocholeThreshold:P0}",
                    $"Addersgall stacks: {stacks}",
                    "600 potency oGCD heal",
                    "Restores 7% MP (700 MP)",
                };

                var alternatives = _druocholeAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Druochole",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Druochole used on {targetName} at {hpPercent:P0} HP with {stacks} Addersgall stacks. 600 potency oGCD heal plus 7% MP restoration. This is SGE's primary Addersgall single-target heal - efficient and free (restores MP!). Use freely when Addersgall is available.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Druochole is your bread-and-butter heal! It costs Addersgall but RESTORES MP, making it very efficient. Don't hoard Addersgall - use it! Stacks regenerate automatically.",
                    ConceptId = SgeConcepts.DruocholeUsage,
                    Priority = hpPercent < 0.3f ? ExplanationPriority.Critical : ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryTaurochole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableTaurochole)
            return false;

        if (player.Level < SGEActions.Taurochole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.TaurocholeState = "No Addersgall";
            return false;
        }

        // Check cooldown (shares with Kerachole)
        if (!context.ActionService.IsActionReady(SGEActions.Taurochole.ActionId))
        {
            context.Debug.TaurocholeState = "On CD";
            return false;
        }

        // Taurochole is best for tanks needing healing + mitigation
        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null)
        {
            context.Debug.TaurocholeState = "No tank";
            return false;
        }

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(tank.EntityId, context.PartyCoordinationService))
        {
            context.Debug.TaurocholeState = "Skipped (reserved)";
            return false;
        }

        var hpPercent = tank.MaxHp > 0 ? (float)tank.CurrentHp / tank.MaxHp : 1f;
        if (hpPercent > config.TaurocholeThreshold)
        {
            context.Debug.TaurocholeState = $"Tank at {hpPercent:P0}";
            return false;
        }

        // Don't use if tank already has Kerachole/Taurochole mit
        if (AsclepiusStatusHelper.HasKerachole(tank))
        {
            context.Debug.TaurocholeState = "Already has mit";
            return false;
        }

        var action = SGEActions.Taurochole;
        if (context.ActionService.ExecuteOgcd(action, tank.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                tank.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Taurochole";
            context.Debug.TaurocholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"Tank at {hpPercent:P0}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var tankName = tank.Name?.TextValue ?? "Unknown";
                var stacks = context.AddersgallStacks;

                var shortReason = $"Taurochole on {tankName} at {hpPercent:P0} - heal + 10% mit";

                var factors = new[]
                {
                    $"Tank HP: {hpPercent:P0}",
                    $"Threshold: {config.TaurocholeThreshold:P0}",
                    $"Addersgall stacks: {stacks}",
                    "700 potency heal + 10% mit (15s)",
                    "Shares 45s CD with Kerachole",
                };

                var alternatives = _taurocholeAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Taurochole",
                    Category = "Healing",
                    TargetName = tankName,
                    ShortReason = shortReason,
                    DetailedReason = $"Taurochole used on tank {tankName} at {hpPercent:P0} HP with {stacks} Addersgall stacks. 700 potency heal PLUS 10% damage reduction for 15 seconds. Perfect for tank healing + tankbuster mitigation. Shares a 45s CD with Kerachole - plan which you need more!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Taurochole is your best tank heal! The 10% mitigation is fantastic for tankbusters. Remember it shares a 45s cooldown with Kerachole - if you need party mitigation, save for Kerachole instead.",
                    ConceptId = SgeConcepts.TaurocholeUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }
}
