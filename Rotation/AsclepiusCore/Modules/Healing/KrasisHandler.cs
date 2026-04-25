using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

public sealed class KrasisHandler : IHealingHandler
{
    public int Priority => 55;
    public string Name => "Krasis";

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableKrasis) return;
        if (player.Level < SGEActions.Krasis.MinLevel) return;
        if (!context.ActionService.IsActionReady(SGEActions.Krasis.ActionId)) { context.Debug.KrasisState = "On CD"; return; }

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null) { context.Debug.KrasisState = "No target"; return; }
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) { context.Debug.KrasisState = "Skipped (reserved)"; return; }

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.KrasisThreshold) { context.Debug.KrasisState = $"Target at {hpPercent:P0}"; return; }
        if (AsclepiusStatusHelper.HasKrasis(target)) { context.Debug.KrasisState = "Already has Krasis"; return; }

        var capturedTarget = target;
        var capturedHpPercent = hpPercent;
        var action = SGEActions.Krasis;

        scheduler.PushOgcd(AsclepiusAbilities.Krasis, target.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                var healAmount = 1000;
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Krasis";
                context.Debug.KrasisState = "Executing";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Krasis",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = $"Krasis on {targetName} at {capturedHpPercent:P0} - boosting heals",
                        DetailedReason = $"Krasis placed on {targetName} at {capturedHpPercent:P0} HP. Provides a 20% healing received buff for 10 seconds. Use before your biggest heals to maximize their effectiveness!",
                        Factors = new[]
                        {
                            $"Target HP: {capturedHpPercent:P0}",
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
            });
    }
}
