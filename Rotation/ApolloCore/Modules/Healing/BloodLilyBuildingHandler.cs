using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Blood Lily building by preferring Lily heals when close to Afflatus Misery.
/// </summary>
public sealed class BloodLilyBuildingHandler : IHealingHandler
{
    private static readonly string[] _solaceBuildAlternatives =
    {
        "Cure II (doesn't build Blood Lily)",
        "Wait for lower HP target (more healing efficiency)",
        "Save Lilies for emergencies",
    };

    private static readonly string[] _raptureBuildAlternatives =
    {
        "Afflatus Solace (single target)",
        "Medica II (doesn't build Blood Lily)",
        "Save Lilies for emergencies",
    };

    private const int BloodLilyBuildingThreshold = 2;
    private const int AfflatusSolaceMinLevel = 52;
    private const int AfflatusRaptureMinLevel = 76;

    private const float AggressiveHpThreshold = 0.85f;
    private const float BalancedHpThreshold = 0.80f;
    private const float ConservativeHpThreshold = 0.70f;

    public HealingPriority Priority => HealingPriority.BloodLilyBuilding;
    public string Name => "BloodLilyBuilding";

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Healing.EnableAggressiveLilyFlush) return;
        if (config.Healing.LilyStrategy == LilyGenerationStrategy.Disabled) return;
        if (context.BloodLilyCount < BloodLilyBuildingThreshold) return;
        if (context.LilyCount < 1) return;
        if (player.Level < AfflatusSolaceMinLevel) return;

        var hpThreshold = config.Healing.LilyStrategy switch
        {
            LilyGenerationStrategy.Aggressive => AggressiveHpThreshold,
            LilyGenerationStrategy.Conservative => ConservativeHpThreshold,
            _ => BalancedHpThreshold
        };

        if (player.Level >= AfflatusRaptureMinLevel)
        {
            var injuredInRange = context.PartyHelper.CountInjuredInAoERange(
                player, WHMActions.AfflatusRapture.Radius, hpThreshold);
            if (injuredInRange >= 2 && TryPushAfflatusRapture(context, scheduler, hpThreshold)) return;
        }

        TryPushAfflatusSolace(context, scheduler, hpThreshold);
    }

    private bool TryPushAfflatusSolace(IApolloContext context, RotationScheduler scheduler, float hpThreshold)
    {
        if (!context.Configuration.Healing.EnableAfflatusSolace) return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(context.Player);
        if (target is null) return false;

        var targetHpPercent = context.PartyHelper.GetHpPercent(target);
        if (targetHpPercent >= hpThreshold) return false;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId)) return false;
        if (!DistanceHelper.IsInRange(context.Player, target, WHMActions.AfflatusSolace.Range)) return false;

        var action = WHMActions.AfflatusSolace;
        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(context.Player.Level);
        var healAmount = action.EstimateHealAmount(mind, det, wd, context.Player.Level);

        var capturedTarget = target;
        var capturedHealAmount = healAmount;
        var capturedTargetHpPercent = targetHpPercent;
        var capturedHpThreshold = hpThreshold;

        scheduler.PushGcd(ApolloAbilities.AfflatusSolace, target.GameObjectId, priority: (int)Priority,
            onDispatched: _ =>
            {
                context.HealingCoordination.TryReserveTarget(capturedTarget.EntityId);
                context.HpPredictionService.RegisterPendingHeal(capturedTarget.EntityId, capturedHealAmount);

                var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                context.Debug.PlannedAction = $"Afflatus Solace (Blood Lily building)";
                context.Debug.PlanningState = "Blood Lily Building";
                context.Debug.MiseryState = $"Building ({context.BloodLilyCount}/3 Blood Lilies)";

                context.LogHealDecision(targetName, capturedTargetHpPercent, action.Name, capturedHealAmount,
                    $"Blood Lily building ({context.BloodLilyCount}/3 Blood, {context.LilyCount}/3 Lilies - next Lily unlock Misery)");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var strategy = context.Configuration.Healing.LilyStrategy.ToString();
                    var shortReason = $"Blood Lily building - {context.BloodLilyCount}/3 Blood, healing {targetName}";
                    var factors = new[]
                    {
                        $"Blood Lilies: {context.BloodLilyCount}/3 (need 3 for Misery)",
                        $"Lilies: {context.LilyCount}/3",
                        $"Target HP: {capturedTargetHpPercent:P0}",
                        $"HP threshold: {capturedHpThreshold:P0} ({strategy} strategy)",
                        "Using Lily heal builds toward Afflatus Misery (1240p AoE damage)",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Afflatus Solace",
                        Category = "Resource Management",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Afflatus Solace on {targetName} to build toward Afflatus Misery. Currently at {context.BloodLilyCount}/3 Blood Lilies - one more Lily heal will unlock Misery (1240 potency AoE damage). Strategy: {strategy}. Target was at {capturedTargetHpPercent:P0} HP, below the {capturedHpThreshold:P0} threshold.",
                        Factors = factors,
                        Alternatives = _solaceBuildAlternatives,
                        Tip = "When at 2 Blood Lilies, prioritize Lily heals over regular GCDs to unlock Misery faster - it's a huge DPS gain!",
                        ConceptId = WhmConcepts.BloodLilyBuilding,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });

        return true;
    }

    private bool TryPushAfflatusRapture(IApolloContext context, RotationScheduler scheduler, float hpThreshold)
    {
        if (!context.Configuration.Healing.EnableAfflatusRapture) return false;

        var injuredCount = context.PartyHelper.CountInjuredInAoERange(
            context.Player, WHMActions.AfflatusRapture.Radius, hpThreshold);
        if (injuredCount < 2) return false;

        var action = WHMActions.AfflatusRapture;
        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(context.Player.Level);
        var healAmount = action.EstimateHealAmount(mind, det, wd, context.Player.Level);

        var capturedHealAmount = healAmount;
        var capturedInjuredCount = injuredCount;
        var capturedHpThreshold = hpThreshold;

        scheduler.PushGcd(ApolloAbilities.AfflatusRapture, context.Player.GameObjectId, priority: (int)Priority,
            onDispatched: _ =>
            {
                foreach (var member in context.PartyHelper.GetPartyMembers(context.Player))
                {
                    if (context.PartyHelper.GetHpPercent(member) < capturedHpThreshold)
                        context.HpPredictionService.RegisterPendingHeal(member.EntityId, capturedHealAmount);
                }

                context.Debug.PlannedAction = $"Afflatus Rapture (Blood Lily building)";
                context.Debug.PlanningState = "Blood Lily Building (AoE)";
                context.Debug.MiseryState = $"Building ({context.BloodLilyCount}/3 Blood Lilies)";

                context.LogHealDecision("Party", 1.0f, action.Name, capturedHealAmount,
                    $"Blood Lily building AoE ({context.BloodLilyCount}/3 Blood, {context.LilyCount}/3 Lilies - {capturedInjuredCount} injured)");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var strategy = context.Configuration.Healing.LilyStrategy.ToString();
                    var shortReason = $"Blood Lily building AoE - {context.BloodLilyCount}/3 Blood, {capturedInjuredCount} injured";
                    var factors = new[]
                    {
                        $"Blood Lilies: {context.BloodLilyCount}/3 (need 3 for Misery)",
                        $"Lilies: {context.LilyCount}/3",
                        $"Injured count: {capturedInjuredCount}",
                        $"HP threshold: {capturedHpThreshold:P0} ({strategy} strategy)",
                        "AoE Lily heal builds Blood Lily while healing multiple targets",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Afflatus Rapture",
                        Category = "Resource Management",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Afflatus Rapture to build toward Afflatus Misery while healing {capturedInjuredCount} party members. Currently at {context.BloodLilyCount}/3 Blood Lilies - one more Lily heal will unlock Misery (1240 potency AoE damage). Strategy: {strategy}.",
                        Factors = factors,
                        Alternatives = _raptureBuildAlternatives,
                        Tip = "Rapture is better than Solace for Blood Lily building when multiple targets need healing - you get both healing efficiency and gauge progress!",
                        ConceptId = WhmConcepts.AfflatusRaptureUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });

        return true;
    }
}
