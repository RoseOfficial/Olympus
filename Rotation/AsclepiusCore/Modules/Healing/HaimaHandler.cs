using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

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

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableHaima) return;
        if (player.Level < SGEActions.Haima.MinLevel) return;
        if (!context.ActionService.IsActionReady(SGEActions.Haima.ActionId)) { context.Debug.HaimaState = "On CD"; return; }

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null) { context.Debug.HaimaState = "No tank"; return; }
        if (context.HealingCoordination.IsTargetReserved(tank.EntityId, context.PartyCoordinationService)) { context.Debug.HaimaState = "Skipped (reserved)"; return; }

        var hpPercent = tank.MaxHp > 0 ? (float)tank.CurrentHp / tank.MaxHp : 1f;
        if (AsclepiusStatusHelper.HasHaima(tank)) { context.Debug.HaimaState = "Already has Haima"; return; }

        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out var busterSource);

        if (hpPercent > config.HaimaThreshold && !tankBusterImminent) { context.Debug.HaimaState = $"Tank at {hpPercent:P0}"; return; }

        var capturedTank = tank;
        var capturedHpPercent = hpPercent;
        var capturedTankBusterImminent = tankBusterImminent;
        var action = SGEActions.Haima;

        scheduler.PushOgcd(AsclepiusAbilities.Haima, tank.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                var healAmount = action.HealPotency * 10;
                context.HealingCoordination.TryReserveTarget(
                    capturedTank.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Haima";
                context.Debug.HaimaState = "Executing";
                context.Debug.HaimaTarget = capturedTank.Name?.TextValue ?? "Unknown";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var tankName = capturedTank.Name?.TextValue ?? "Unknown";
                    var shortReason = capturedTankBusterImminent
                        ? $"Haima on {tankName} - tankbuster incoming!"
                        : $"Haima on {tankName} at {capturedHpPercent:P0}";

                    var factors = new[]
                    {
                        $"Tank HP: {capturedHpPercent:P0}",
                        capturedTankBusterImminent ? "Tankbuster imminent!" : $"Threshold: {config.HaimaThreshold:P0}",
                        "300 potency shield x5 stacks",
                        "Shield refreshes when broken",
                        "120s cooldown",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Haima",
                        Category = "Healing",
                        TargetName = tankName,
                        ShortReason = shortReason,
                        DetailedReason = $"Haima placed on tank {tankName} at {capturedHpPercent:P0} HP. {(capturedTankBusterImminent ? "Tankbuster detected - Haima will absorb multiple hits!" : "Proactive shield for tank damage.")} Provides 5 stacks of 300 potency shields that refresh when consumed. Perfect for sustained tank damage or multi-hit tankbusters!",
                        Factors = factors,
                        Alternatives = _haimaAlternatives,
                        Tip = "Haima is AMAZING for multi-hit tankbusters! Each time the shield breaks, a new one appears (up to 5 times). It heals for any remaining shield value when it expires. Pre-place before tankbusters!",
                        ConceptId = SgeConcepts.HaimaUsage,
                        Priority = capturedTankBusterImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });
                }
            });
    }
}
