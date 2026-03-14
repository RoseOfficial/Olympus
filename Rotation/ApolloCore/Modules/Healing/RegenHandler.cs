using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Regen HoT maintenance with tank priority.
/// Supports dynamic threshold based on damage rate.
/// </summary>
public sealed class RegenHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.Regen;
    public string Name => "Regen";

    // Training explanation arrays
    private static readonly string[] _regenAlternatives =
    {
        "Wait for HP to drop further",
        "Use direct heal instead (if urgent)",
        "Let co-healer handle it",
    };

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Healing.EnableRegen)
            return false;

        if (player.Level < WHMActions.Regen.MinLevel)
            return false;

        // Calculate dynamic Regen threshold based on party damage state
        var regenHpThreshold = GetDynamicRegenThreshold(context);

        var target = context.PartyHelper.FindRegenTarget(player, regenHpThreshold, FFXIVConstants.RegenRefreshThreshold);
        if (target is null)
            return false;

        // Skip if another handler is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId))
            return false;

        if (isMoving && WHMActions.Regen.CastTime > 0)
            return false;

        var targetName = target.Name?.TextValue ?? "Unknown";
        if (ActionExecutor.ExecuteGcd(context, WHMActions.Regen, target.GameObjectId,
            targetName, target.CurrentHp, "Regen",
            appendThinAirNote: false))
        {
            // Reserve target to prevent other handlers from double-healing
            context.HealingCoordination.TryReserveTarget(target.EntityId);

            var thresholdNote = regenHpThreshold > FFXIVConstants.RegenHpThreshold
                ? $" (dynamic {regenHpThreshold:P0})"
                : "";
            context.Debug.PlannedAction = $"Regen (tank priority{thresholdNote})";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var hpPercent = context.PartyHelper.GetHpPercent(target);
                var isTank = JobRegistry.IsTank(target.ClassJob.RowId);
                var isDynamicThreshold = regenHpThreshold > FFXIVConstants.RegenHpThreshold;

                var shortReason = isTank
                    ? $"Regen on tank {targetName}"
                    : $"Regen on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"HP threshold: {regenHpThreshold:P0}" + (isDynamicThreshold ? " (raised due to high damage)" : ""),
                    isTank ? "Tank priority - keeping Regen active" : "Non-tank target needing HoT",
                    $"Regen duration: 18s",
                    $"Regen potency: 250 per tick (every 3s)",
                };

                var alternatives = _regenAlternatives;

                var tip = isTank
                    ? "Keep Regen rolling on the tank - it's efficient healing that lets you cast damage spells!"
                    : "Regen is MP-efficient for sustained healing - use it on anyone taking consistent damage.";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = WHMActions.Regen.ActionId,
                    ActionName = "Regen",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Regen applied to {targetName} at {hpPercent:P0} HP. {(isTank ? "As the tank, they take consistent damage and benefit most from Regen's sustained healing. " : "")}{(isDynamicThreshold ? "HP threshold was raised due to high party damage rate. " : "")}Regen heals for 250 potency every 3 seconds over 18 seconds.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = WhmConcepts.RegenMaintenance,
                    Priority = ExplanationPriority.Low,
                });
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates the dynamic Regen HP threshold based on damage patterns.
    /// During high-damage phases, Regen is applied at a higher threshold.
    /// </summary>
    private static float GetDynamicRegenThreshold(ApolloContext context)
    {
        var config = context.Configuration;

        // If dynamic threshold disabled, use default
        if (!config.Healing.EnableDynamicRegenThreshold)
            return FFXIVConstants.RegenHpThreshold;

        // Check if anyone is taking high damage
        var partyDamageRate = context.DamageIntakeService.GetPartyDamageRate(3f);

        // If party is taking significant damage, use higher threshold
        if (partyDamageRate >= config.Healing.RegenHighDamageDpsThreshold)
            return config.Healing.RegenHighDamageThreshold;

        return FFXIVConstants.RegenHpThreshold;
    }
}
