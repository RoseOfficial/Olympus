using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Assize as a healing oGCD when party needs healing.
/// Balances DPS value against healing value - triggers when multiple party members are injured.
/// </summary>
public sealed class AssizeHealingHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.AssizeHealing;
    public string Name => "AssizeHealing";

    // Training explanation arrays
    private static readonly string[] _assizeHealingAlternatives =
    {
        "Hold for DPS burst window",
        "Use Medica II for HoT instead",
        "Use Afflatus Rapture (builds Blood Lily)",
    };

    public bool TryExecute(IApolloContext context, bool isMoving)
    {
        if (!context.InCombat) return false;

        var config = context.Configuration;
        var player = context.Player;

        // Check if Assize healing mode is enabled
        if (!config.EnableHealing || !config.Healing.EnableAssizeHealing)
            return false;

        if (player.Level < WHMActions.Assize.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.Assize.ActionId))
            return false;

        // Check party health conditions
        var (avgHpPercent, _, injuredCount) = context.PartyHealthMetrics;

        // Need enough injured targets AND party HP below threshold
        var shouldUseForHealing = injuredCount >= config.Healing.AssizeHealingMinTargets &&
                                  avgHpPercent < config.Healing.AssizeHealingHpThreshold;

        if (!shouldUseForHealing)
            return false;

        // Execute Assize for healing
        if (ActionExecutor.ExecuteOgcd(context, WHMActions.Assize, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp,
            $"Assize (healing: {injuredCount} injured, {avgHpPercent:P0} avg HP)"))
        {
            context.Debug.AssizeState = $"Healing mode ({injuredCount} injured, {avgHpPercent:P0} avg)";

            // Log the healing decision
            context.LogOgcdDecision(
                $"{injuredCount} party members",
                avgHpPercent,
                "Assize",
                $"Healing mode - {injuredCount} injured, avg HP {avgHpPercent:P0}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Party heal - {injuredCount} injured, {avgHpPercent:P0} avg HP";

                var factors = new[]
                {
                    $"Party average HP: {avgHpPercent:P0}",
                    $"Injured count: {injuredCount}",
                    $"Min targets threshold: {config.Healing.AssizeHealingMinTargets}",
                    $"HP threshold: {config.Healing.AssizeHealingHpThreshold:P0}",
                    "Also deals damage and restores MP",
                };

                var alternatives = _assizeHealingAlternatives;

                var tip = "Assize heals, damages, and restores MP - try to use it when it provides value in all three areas!";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = WHMActions.Assize.ActionId,
                    ActionName = "Assize",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Assize is a 400 potency party heal that also deals damage and restores 500 MP. Used because {injuredCount} party members are injured and average HP ({avgHpPercent:P0}) is below the threshold ({config.Healing.AssizeHealingHpThreshold:P0}). This provides triple value: healing, damage, and MP.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = WhmConcepts.AssizeUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
