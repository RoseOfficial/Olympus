using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Regen HoT maintenance with tank priority.
/// </summary>
public sealed class RegenHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.Regen;
    public string Name => "Regen";

    private static readonly string[] _regenAlternatives =
    {
        "Wait for HP to drop further",
        "Use direct heal instead (if urgent)",
        "Let co-healer handle it",
    };

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Healing.EnableRegen) return;
        if (player.Level < WHMActions.Regen.MinLevel) return;

        var tankRegenThreshold = DynamicRegenThresholdHelper.GetEffectiveThreshold(
            config.Healing, context.DamageIntakeService, FFXIVConstants.RegenHpThreshold);
        var nonTankRegenThreshold = FFXIVConstants.RegenNonTankHpThreshold;

        var target = context.PartyHelper.FindRegenTarget(player, tankRegenThreshold, nonTankRegenThreshold, FFXIVConstants.RegenRefreshThreshold);
        if (target is null) return;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId)) return;
        if (isMoving && WHMActions.Regen.CastTime > 0) return;

        var capturedTarget = target;
        var capturedTankRegenThreshold = tankRegenThreshold;
        var capturedNonTankRegenThreshold = nonTankRegenThreshold;

        scheduler.PushGcd(ApolloAbilities.Regen, target.GameObjectId, priority: (int)Priority,
            onDispatched: _ =>
            {
                context.HealingCoordination.TryReserveTarget(capturedTarget.EntityId);

                var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                var isTankTarget = JobRegistry.IsTank(capturedTarget.ClassJob.RowId);
                var usedThreshold = isTankTarget ? capturedTankRegenThreshold : capturedNonTankRegenThreshold;
                var thresholdNote = usedThreshold > FFXIVConstants.RegenHpThreshold
                    ? $" (dynamic {usedThreshold:P0})"
                    : "";
                context.Debug.PlannedAction = $"Regen ({(isTankTarget ? "tank" : targetName)}{thresholdNote})";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var hpPercent = context.PartyHelper.GetHpPercent(capturedTarget);
                    var isDynamicThreshold = isTankTarget && usedThreshold > FFXIVConstants.RegenHpThreshold;

                    var shortReason = isTankTarget
                        ? $"Regen on tank {targetName}"
                        : $"Regen on {targetName} at {hpPercent:P0}";

                    var factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
                        $"HP threshold: {usedThreshold:P0}" + (isDynamicThreshold ? " (raised due to high damage)" : ""),
                        isTankTarget ? "Tank priority - keeping Regen active" : "Non-tank target needing HoT",
                        $"Regen duration: 18s",
                        $"Regen potency: 250 per tick (every 3s)",
                    };

                    var tip = isTankTarget
                        ? "Keep Regen rolling on the tank - it's efficient healing that lets you cast damage spells!"
                        : "Regen is MP-efficient for sustained healing - use it on anyone taking consistent damage.";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.Regen.ActionId,
                        ActionName = "Regen",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Regen applied to {targetName} at {hpPercent:P0} HP. {(isTankTarget ? "As the tank, they take consistent damage and benefit most from Regen's sustained healing. " : "")}{(isDynamicThreshold ? "HP threshold was raised due to high party damage rate. " : "")}Regen heals for 250 potency every 3 seconds over 18 seconds.",
                        Factors = factors,
                        Alternatives = _regenAlternatives,
                        Tip = tip,
                        ConceptId = WhmConcepts.RegenMaintenance,
                        Priority = ExplanationPriority.Low,
                    });
                }
            });
    }
}
