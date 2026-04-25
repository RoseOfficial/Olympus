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
/// Handles oGCD single-target healing with Tetragrammaton.
/// Features charge management and dynamic overheal thresholds.
/// </summary>
public sealed class TetragrammatonHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.Tetragrammaton;
    public string Name => "Tetragrammaton";

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.Tetragrammaton, config,
            c => c.EnableHealing && c.Healing.EnableTetragrammaton))
            return;

        var currentCharges = context.ActionService.GetCurrentCharges(WHMActions.Tetragrammaton.ActionId);
        var maxCharges = context.ActionService.GetMaxCharges(WHMActions.Tetragrammaton.ActionId, 0);
        var isAtMaxCharges = currentCharges >= maxCharges && maxCharges > 0;

        var target = config.Healing.UseDamageIntakeTriage
            ? context.PartyHelper.FindMostEndangeredPartyMember(player, context.DamageIntakeService, 0, context.DamageTrendService)
            : context.PartyHelper.FindLowestHpPartyMember(player);

        if (target is null) return;
        if (HealerPartyHelper.HasNoHealStatus(target)) return;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;
        if (!DistanceHelper.IsInRange(player, target, WHMActions.Tetragrammaton.Range)) return;

        var predictedHp = context.HpPredictionService.GetPredictedHp(target.EntityId, target.CurrentHp, target.MaxHp);
        var missingHp = (int)(target.MaxHp - predictedHp);
        if (missingHp <= 0) return;

        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
        var healAmount = WHMActions.Tetragrammaton.EstimateHealAmount(mind, det, wd, player.Level);

        var overhealMultiplier = 1.5f;
        var isSpike = false;

        if (isAtMaxCharges)
        {
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

        if (healAmount > missingHp * overhealMultiplier) return;

        var capturedTarget = target;
        var capturedIsAtMaxCharges = isAtMaxCharges;
        var capturedIsSpike = isSpike;
        var capturedOverhealMultiplier = overhealMultiplier;
        var capturedCurrentCharges = currentCharges;
        var capturedMaxCharges = maxCharges;
        var capturedHealAmount = healAmount;
        var capturedMissingHp = missingHp;

        scheduler.PushOgcd(ApolloAbilities.Tetragrammaton, target.GameObjectId, priority: (int)Priority,
            onDispatched: _ =>
            {
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, capturedHealAmount, WHMActions.Tetragrammaton.ActionId, 0);
                context.HpPredictionService.RegisterPendingHeal(capturedTarget.EntityId, capturedHealAmount);

                var hpPercent = context.PartyHelper.GetHpPercent(capturedTarget);
                var chargeInfo = $"{capturedCurrentCharges}/{capturedMaxCharges} charges";
                var targetName = capturedTarget.Name?.TextValue ?? "Unknown";

                if (capturedIsAtMaxCharges)
                {
                    context.Debug.PlannedAction = $"Tetragrammaton ({chargeInfo}, avoiding cap)";
                    context.LogOgcdDecision(targetName, hpPercent, "Tetragrammaton", $"At max charges - using to avoid cap ({chargeInfo})");
                }
                else if (capturedIsSpike)
                {
                    context.Debug.PlannedAction = $"Tetragrammaton (spike mode, {capturedOverhealMultiplier:F1}x overheal allowed)";
                    context.LogOgcdDecision(targetName, hpPercent, "Tetragrammaton", $"Spike mode - {capturedOverhealMultiplier:F1}x overheal allowed ({chargeInfo})");
                }
                else
                {
                    context.LogOgcdDecision(targetName, hpPercent, "Tetragrammaton", $"Standard oGCD heal ({chargeInfo})");
                }

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = capturedIsAtMaxCharges
                        ? $"Avoiding cap - {targetName} at {hpPercent:P0}"
                        : capturedIsSpike
                            ? $"Damage spike - {targetName} at {hpPercent:P0}"
                            : $"oGCD heal - {targetName} at {hpPercent:P0}";

                    var factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
                        $"Missing HP: {capturedMissingHp:N0}",
                        $"Heal amount: {capturedHealAmount:N0}",
                        $"Charges: {chargeInfo}",
                        capturedIsAtMaxCharges ? "At max charges - avoiding waste" :
                        capturedIsSpike ? $"Damage spike imminent - {capturedOverhealMultiplier:F1}x overheal allowed" :
                        $"Standard overheal limit: {capturedOverhealMultiplier:F1}x",
                    };

                    var alternatives = capturedIsAtMaxCharges
                        ? new[] { "Waste charge regeneration by holding", "Use on tank for mitigation value" }
                        : capturedIsSpike
                            ? new[] { "Wait for HP to drop (risky)", "Use GCD heal instead (slower)" }
                            : new[] { "Save for emergency", "Use Cure II instead (GCD)" };

                    var tip = capturedIsAtMaxCharges
                        ? "Don't let Tetragrammaton sit at max charges - you're wasting free healing!"
                        : capturedIsSpike
                            ? "During damage spikes, it's okay to overheal - better safe than dead."
                            : "Tetragrammaton is free healing - use it before expensive GCD heals.";

                    var conceptId = capturedIsAtMaxCharges ? WhmConcepts.OgcdWeaving : WhmConcepts.TetragrammatonUsage;
                    var trainingPriority = capturedIsSpike ? ExplanationPriority.High : ExplanationPriority.Normal;

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.Tetragrammaton.ActionId,
                        ActionName = "Tetragrammaton",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Tetragrammaton is a 700 potency instant heal with charges. Used on {targetName} at {hpPercent:P0} HP (missing {capturedMissingHp:N0}). {(capturedIsAtMaxCharges ? "Used to avoid wasting charge regeneration." : capturedIsSpike ? "Used proactively due to incoming damage spike." : "Standard efficient oGCD usage.")}",
                        Factors = factors,
                        Alternatives = alternatives,
                        Tip = tip,
                        ConceptId = conceptId,
                        Priority = trainingPriority,
                    });
                }
            });
    }
}
