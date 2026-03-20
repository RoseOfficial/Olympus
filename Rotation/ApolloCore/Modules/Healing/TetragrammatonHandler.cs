using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles oGCD single-target healing with Tetragrammaton.
/// Features charge management and dynamic overheal thresholds.
/// </summary>
public sealed class TetragrammatonHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.Tetragrammaton;
    public string Name => "Tetragrammaton";

    public bool TryExecute(IApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.Tetragrammaton, config,
            c => c.EnableHealing && c.Healing.EnableTetragrammaton))
            return false;

        // Get charge information for smarter usage
        var currentCharges = context.ActionService.GetCurrentCharges(WHMActions.Tetragrammaton.ActionId);
        var maxCharges = context.ActionService.GetMaxCharges(WHMActions.Tetragrammaton.ActionId, 0);
        var isAtMaxCharges = currentCharges >= maxCharges && maxCharges > 0;

        // Use damage intake triage if enabled, otherwise fall back to lowest HP
        var target = config.Healing.UseDamageIntakeTriage
            ? context.PartyHelper.FindMostEndangeredPartyMember(player, context.DamageIntakeService, 0, context.DamageTrendService)
            : context.PartyHelper.FindLowestHpPartyMember(player);

        if (target is null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        if (!DistanceHelper.IsInRange(player, target, WHMActions.Tetragrammaton.Range))
            return false;

        var predictedHp = context.HpPredictionService.GetPredictedHp(target.EntityId, target.CurrentHp, target.MaxHp);
        var missingHp = (int)(target.MaxHp - predictedHp);

        if (missingHp <= 0)
            return false;

        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
        var healAmount = WHMActions.Tetragrammaton.EstimateHealAmount(mind, det, wd, player.Level);

        // Dynamic overheal threshold based on charge count and damage spike status
        // At max charges: Be more liberal with usage to avoid wasting charge regen (2.5x)
        // Normal (1 charge): Reject if overheal > 1.5x missing HP
        // During spike: Allow up to 2.0x (configurable) to save lives
        var overhealMultiplier = 1.5f;
        var isSpike = false;

        if (isAtMaxCharges)
        {
            // At max charges, use more freely to avoid wasting charge regen
            overhealMultiplier = 2.5f;
        }
        else if (config.Healing.EnableDynamicTetragrammatonOverheal)
        {
            isSpike = context.DamageTrendService.IsDamageSpikeImminent(0.8f);
            if (isSpike)
            {
                overhealMultiplier = config.Healing.TetragrammatonSpikeOverhealMultiplier;
            }
        }

        if (healAmount > missingHp * overhealMultiplier)
            return false;

        if (ActionExecutor.ExecuteHealingOgcd(context, WHMActions.Tetragrammaton, target.GameObjectId,
            target.EntityId, target.Name?.TextValue ?? "Unknown", target.CurrentHp, healAmount))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, WHMActions.Tetragrammaton.ActionId, 0);

            var hpPercent = context.PartyHelper.GetHpPercent(target);
            var chargeInfo = $"{currentCharges}/{maxCharges} charges";

            var targetName = target.Name?.TextValue ?? "Unknown";

            if (isAtMaxCharges)
            {
                context.Debug.PlannedAction = $"Tetragrammaton ({chargeInfo}, avoiding cap)";
                context.LogOgcdDecision(
                    targetName,
                    hpPercent,
                    "Tetragrammaton",
                    $"At max charges - using to avoid cap ({chargeInfo})");
            }
            else if (isSpike)
            {
                context.Debug.PlannedAction = $"Tetragrammaton (spike mode, {overhealMultiplier:F1}x overheal allowed)";
                context.LogOgcdDecision(
                    targetName,
                    hpPercent,
                    "Tetragrammaton",
                    $"Spike mode - {overhealMultiplier:F1}x overheal allowed ({chargeInfo})");
            }
            else
            {
                context.LogOgcdDecision(
                    targetName,
                    hpPercent,
                    "Tetragrammaton",
                    $"Standard oGCD heal ({chargeInfo})");
            }

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = isAtMaxCharges
                    ? $"Avoiding cap - {targetName} at {hpPercent:P0}"
                    : isSpike
                        ? $"Damage spike - {targetName} at {hpPercent:P0}"
                        : $"oGCD heal - {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Missing HP: {missingHp:N0}",
                    $"Heal amount: {healAmount:N0}",
                    $"Charges: {chargeInfo}",
                    isAtMaxCharges ? "At max charges - avoiding waste" :
                    isSpike ? $"Damage spike imminent - {overhealMultiplier:F1}x overheal allowed" :
                    $"Standard overheal limit: {overhealMultiplier:F1}x",
                };

                var alternatives = isAtMaxCharges
                    ? new[] { "Waste charge regeneration by holding", "Use on tank for mitigation value" }
                    : isSpike
                        ? new[] { "Wait for HP to drop (risky)", "Use GCD heal instead (slower)" }
                        : new[] { "Save for emergency", "Use Cure II instead (GCD)" };

                var tip = isAtMaxCharges
                    ? "Don't let Tetragrammaton sit at max charges - you're wasting free healing!"
                    : isSpike
                        ? "During damage spikes, it's okay to overheal - better safe than dead."
                        : "Tetragrammaton is free healing - use it before expensive GCD heals.";

                var conceptId = isAtMaxCharges ? WhmConcepts.OgcdWeaving : WhmConcepts.TetragrammatonUsage;
                var priority = isSpike ? ExplanationPriority.High : ExplanationPriority.Normal;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = WHMActions.Tetragrammaton.ActionId,
                    ActionName = "Tetragrammaton",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Tetragrammaton is a 700 potency instant heal with charges. Used on {targetName} at {hpPercent:P0} HP (missing {missingHp:N0}). {(isAtMaxCharges ? "Used to avoid wasting charge regeneration." : isSpike ? "Used proactively due to incoming damage spike." : "Standard efficient oGCD usage.")}",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = conceptId,
                    Priority = priority,
                });
            }

            return true;
        }

        return false;
    }
}
