using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles single-target oGCD heals for Sage: Druochole and Taurochole.
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

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        TryPushDruochole(context, scheduler);
        TryPushTaurochole(context, scheduler);
    }

    private void TryPushDruochole(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        if (!config.EnableDruochole) return;

        var player = context.Player;
        if (player.Level < SGEActions.Druochole.MinLevel) return;
        if (context.AddersgallStacks < 1) { context.Debug.DruocholeState = "No Addersgall"; return; }
        if (context.AddersgallStacks <= config.AddersgallReserve) { context.Debug.DruocholeState = $"Reserved ({config.AddersgallReserve})"; return; }

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null) { context.Debug.DruocholeState = "No target"; return; }
        if (HealerPartyHelper.HasNoHealStatus(target)) { context.Debug.DruocholeState = "Skipped (invuln/delayed heal)"; return; }
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) { context.Debug.DruocholeState = "Skipped (reserved)"; return; }

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.DruocholeThreshold) { context.Debug.DruocholeState = $"{hpPercent:P0} > {config.DruocholeThreshold:P0}"; return; }

        var capturedTarget = target;
        var capturedHpPercent = hpPercent;
        var capturedStacks = context.AddersgallStacks;
        var action = SGEActions.Druochole;

        scheduler.PushOgcd(AsclepiusAbilities.Druochole, target.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                var healAmount = action.HealPotency * 10;
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Druochole";
                context.Debug.DruocholeState = "Executing";
                context.LogAddersgallDecision(action.Name, capturedStacks, $"Target at {capturedHpPercent:P0}");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var shortReason = $"Druochole on {targetName} at {capturedHpPercent:P0} ({capturedStacks} stacks)";
                    var factors = new[]
                    {
                        $"Target HP: {capturedHpPercent:P0}",
                        $"Threshold: {config.DruocholeThreshold:P0}",
                        $"Addersgall stacks: {capturedStacks}",
                        "600 potency oGCD heal",
                        "Restores 7% MP (700 MP)",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Druochole",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Druochole used on {targetName} at {capturedHpPercent:P0} HP with {capturedStacks} Addersgall stacks. 600 potency oGCD heal plus 7% MP restoration. This is SGE's primary Addersgall single-target heal - efficient and free (restores MP!). Use freely when Addersgall is available.",
                        Factors = factors,
                        Alternatives = _druocholeAlternatives,
                        Tip = "Druochole is your bread-and-butter heal! It costs Addersgall but RESTORES MP, making it very efficient. Don't hoard Addersgall - use it! Stacks regenerate automatically.",
                        ConceptId = SgeConcepts.DruocholeUsage,
                        Priority = capturedHpPercent < 0.3f ? ExplanationPriority.Critical : ExplanationPriority.High,
                    });
                }
            });
    }

    private void TryPushTaurochole(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableTaurochole) return;
        if (player.Level < SGEActions.Taurochole.MinLevel) return;
        if (context.AddersgallStacks < 1) { context.Debug.TaurocholeState = "No Addersgall"; return; }
        if (!context.ActionService.IsActionReady(SGEActions.Taurochole.ActionId)) { context.Debug.TaurocholeState = "On CD"; return; }

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null) { context.Debug.TaurocholeState = "No tank"; return; }
        if (context.HealingCoordination.IsTargetReserved(tank.EntityId, context.PartyCoordinationService)) { context.Debug.TaurocholeState = "Skipped (reserved)"; return; }

        var hpPercent = tank.MaxHp > 0 ? (float)tank.CurrentHp / tank.MaxHp : 1f;
        if (hpPercent > config.TaurocholeThreshold) { context.Debug.TaurocholeState = $"Tank at {hpPercent:P0}"; return; }
        if (AsclepiusStatusHelper.HasKerachole(tank)) { context.Debug.TaurocholeState = "Already has mit"; return; }

        var capturedTank = tank;
        var capturedHpPercent = hpPercent;
        var capturedStacks = context.AddersgallStacks;
        var action = SGEActions.Taurochole;

        // Taurochole has slightly higher priority than Druochole within this handler (matches legacy first-true-wins ordering)
        scheduler.PushOgcd(AsclepiusAbilities.Taurochole, tank.GameObjectId, priority: Priority + 1,
            onDispatched: _ =>
            {
                var healAmount = action.HealPotency * 10;
                context.HealingCoordination.TryReserveTarget(
                    capturedTank.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Taurochole";
                context.Debug.TaurocholeState = "Executing";
                context.LogAddersgallDecision(action.Name, capturedStacks, $"Tank at {capturedHpPercent:P0}");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var tankName = capturedTank.Name?.TextValue ?? "Unknown";
                    var shortReason = $"Taurochole on {tankName} at {capturedHpPercent:P0} - heal + 10% mit";
                    var factors = new[]
                    {
                        $"Tank HP: {capturedHpPercent:P0}",
                        $"Threshold: {config.TaurocholeThreshold:P0}",
                        $"Addersgall stacks: {capturedStacks}",
                        "700 potency heal + 10% mit (15s)",
                        "Shares 45s CD with Kerachole",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Taurochole",
                        Category = "Healing",
                        TargetName = tankName,
                        ShortReason = shortReason,
                        DetailedReason = $"Taurochole used on tank {tankName} at {capturedHpPercent:P0} HP with {capturedStacks} Addersgall stacks. 700 potency heal PLUS 10% damage reduction for 15 seconds. Perfect for tank healing + tankbuster mitigation. Shares a 45s CD with Kerachole - plan which you need more!",
                        Factors = factors,
                        Alternatives = _taurocholeAlternatives,
                        Tip = "Taurochole is your best tank heal! The 10% mitigation is fantastic for tankbusters. Remember it shares a 45s cooldown with Kerachole - if you need party mitigation, save for Kerachole instead.",
                        ConceptId = SgeConcepts.TaurocholeUsage,
                        Priority = ExplanationPriority.High,
                    });
                }
            });
    }
}
