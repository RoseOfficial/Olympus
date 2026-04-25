using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Assize as a healing oGCD when party needs healing.
/// </summary>
public sealed class AssizeHealingHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.AssizeHealing;
    public string Name => "AssizeHealing";

    private static readonly string[] _assizeHealingAlternatives =
    {
        "Hold for DPS burst window",
        "Use Medica II for HoT instead",
        "Use Afflatus Rapture (builds Blood Lily)",
    };

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Healing.EnableAssizeHealing) return;
        if (player.Level < WHMActions.Assize.MinLevel) return;
        if (!context.ActionService.IsActionReady(WHMActions.Assize.ActionId)) return;

        var (avgHpPercent, _, injuredCount) = context.PartyHealthMetrics;
        var shouldUseForHealing = injuredCount >= config.Healing.AssizeHealingMinTargets &&
                                  avgHpPercent < config.Healing.AssizeHealingHpThreshold;
        if (!shouldUseForHealing) return;

        var capturedAvgHp = avgHpPercent;
        var capturedInjuredCount = injuredCount;

        scheduler.PushOgcd(ApolloAbilities.AssizeHeal, player.GameObjectId, priority: (int)Priority,
            onDispatched: _ =>
            {
                context.Debug.AssizeState = $"Healing mode ({capturedInjuredCount} injured, {capturedAvgHp:P0} avg)";
                context.Debug.PlannedAction = WHMActions.Assize.Name;

                context.LogOgcdDecision(
                    $"{capturedInjuredCount} party members",
                    capturedAvgHp,
                    "Assize",
                    $"Healing mode - {capturedInjuredCount} injured, avg HP {capturedAvgHp:P0}");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Party heal - {capturedInjuredCount} injured, {capturedAvgHp:P0} avg HP";

                    var factors = new[]
                    {
                        $"Party average HP: {capturedAvgHp:P0}",
                        $"Injured count: {capturedInjuredCount}",
                        $"Min targets threshold: {config.Healing.AssizeHealingMinTargets}",
                        $"HP threshold: {config.Healing.AssizeHealingHpThreshold:P0}",
                        "Also deals damage and restores MP",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.Assize.ActionId,
                        ActionName = "Assize",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Assize is a 400 potency party heal that also deals damage and restores 500 MP. Used because {capturedInjuredCount} party members are injured and average HP ({capturedAvgHp:P0}) is below the threshold ({config.Healing.AssizeHealingHpThreshold:P0}). This provides triple value: healing, damage, and MP.",
                        Factors = factors,
                        Alternatives = _assizeHealingAlternatives,
                        Tip = "Assize heals, damages, and restores MP - try to use it when it provides value in all three areas!",
                        ConceptId = WhmConcepts.AssizeUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }
}
