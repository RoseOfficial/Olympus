using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Lily cap prevention by forcing Lily spells when Lilies are at 3/3.
/// </summary>
public sealed class LilyCapPreventionHandler : IHealingHandler
{
    private const int MaxLilies = 3;
    private const int AfflatusSolaceMinLevel = 52;
    private const int AfflatusRaptureMinLevel = 76;

    private static readonly string[] _solaceCapAlternatives =
    {
        "Afflatus Rapture (if multiple injured)",
        "Nothing (but wastes Lily regen)",
    };

    private static readonly string[] _raptureCapAlternatives =
    {
        "Afflatus Solace (if only one injured)",
        "Nothing (but wastes Lily regen)",
    };

    public HealingPriority Priority => HealingPriority.LilyCapPrevention;
    public string Name => "LilyCapPrevention";

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (context.LilyCount < MaxLilies) return;
        if (!config.EnableHealing || !config.Healing.EnableLilyCapPrevention) return;
        if (player.Level < AfflatusSolaceMinLevel) return;

        if (player.Level >= AfflatusRaptureMinLevel)
        {
            var injuredInRange = context.PartyHelper.CountInjuredInAoERange(
                player, WHMActions.AfflatusRapture.Radius, 0.99f);
            if (injuredInRange >= 2 && TryPushAfflatusRapture(context, scheduler)) return;
        }

        TryPushAfflatusSolace(context, scheduler);
    }

    private bool TryPushAfflatusSolace(IApolloContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Healing.EnableAfflatusSolace) return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(context.Player, healAmount: 1);
        if (target is null) return false;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId)) return false;
        if (!DistanceHelper.IsInRange(context.Player, target, WHMActions.AfflatusSolace.Range)) return false;

        var action = WHMActions.AfflatusSolace;
        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(context.Player.Level);
        var healAmount = action.EstimateHealAmount(mind, det, wd, context.Player.Level);

        var capturedTarget = target;
        var capturedHealAmount = healAmount;

        scheduler.PushGcd(ApolloAbilities.AfflatusSolace, target.GameObjectId, priority: (int)Priority,
            onDispatched: _ =>
            {
                context.HealingCoordination.TryReserveTarget(capturedTarget.EntityId);
                context.HpPredictionService.RegisterPendingHeal(capturedTarget.EntityId, capturedHealAmount);

                var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                context.Debug.PlannedAction = $"Afflatus Solace (Lily cap prevention)";
                context.Debug.PlanningState = "Lily Cap Prevention";

                var hpPercent = context.PartyHelper.GetHpPercent(capturedTarget);
                context.LogHealDecision(targetName, hpPercent, action.Name, capturedHealAmount,
                    $"Lily cap prevention ({context.LilyCount}/3 Lilies, {context.BloodLilyCount}/3 Blood)");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Lily cap! Using Solace on {targetName} to avoid waste";
                    var factors = new[]
                    {
                        $"Lilies: {context.LilyCount}/3 (CAPPED!)",
                        $"Blood Lilies: {context.BloodLilyCount}/3",
                        $"Target HP: {hpPercent:P0}",
                        "Lilies regenerate every 20 seconds",
                        "Capped Lilies = wasted regeneration",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Afflatus Solace",
                        Category = "Resource Management",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Afflatus Solace on {targetName} to prevent Lily cap. At 3/3 Lilies, you're wasting the free Lily regeneration (one every 20 seconds). Used on {targetName} at {hpPercent:P0} HP. This also builds toward Blood Lily ({context.BloodLilyCount}/3).",
                        Factors = factors,
                        Alternatives = _solaceCapAlternatives,
                        Tip = "Never cap Lilies! Each wasted Lily regeneration is a free GCD heal you're throwing away.",
                        ConceptId = WhmConcepts.LilyManagement,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });

        return true;
    }

    private bool TryPushAfflatusRapture(IApolloContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Healing.EnableAfflatusRapture) return false;

        var action = WHMActions.AfflatusRapture;
        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(context.Player.Level);
        var healAmount = action.EstimateHealAmount(mind, det, wd, context.Player.Level);

        var capturedHealAmount = healAmount;

        scheduler.PushGcd(ApolloAbilities.AfflatusRapture, context.Player.GameObjectId, priority: (int)Priority,
            onDispatched: _ =>
            {
                foreach (var member in context.PartyHelper.GetPartyMembers(context.Player))
                {
                    if (member.CurrentHp < member.MaxHp)
                        context.HpPredictionService.RegisterPendingHeal(member.EntityId, capturedHealAmount);
                }

                context.Debug.PlannedAction = $"Afflatus Rapture (Lily cap prevention)";
                context.Debug.PlanningState = "Lily Cap Prevention (AoE)";

                context.LogHealDecision("Party", 1.0f, action.Name, capturedHealAmount,
                    $"Lily cap prevention AoE ({context.LilyCount}/3 Lilies, {context.BloodLilyCount}/3 Blood)");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var injuredCount = 0;
                    foreach (var member in context.PartyHelper.GetPartyMembers(context.Player))
                    {
                        if (member.CurrentHp < member.MaxHp) injuredCount++;
                    }

                    var shortReason = $"Lily cap! Using Rapture on {injuredCount} injured to avoid waste";
                    var factors = new[]
                    {
                        $"Lilies: {context.LilyCount}/3 (CAPPED!)",
                        $"Blood Lilies: {context.BloodLilyCount}/3",
                        $"Injured count: {injuredCount}",
                        "Lilies regenerate every 20 seconds",
                        "AoE more efficient with multiple injured",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Afflatus Rapture",
                        Category = "Resource Management",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Afflatus Rapture to prevent Lily cap. At 3/3 Lilies, you're wasting the free Lily regeneration (one every 20 seconds). {injuredCount} party members had some damage, making AoE more efficient than Solace. This also builds toward Blood Lily ({context.BloodLilyCount}/3).",
                        Factors = factors,
                        Alternatives = _raptureCapAlternatives,
                        Tip = "When capped on Lilies with multiple injured, Rapture is better than Solace - you get more total healing value!",
                        ConceptId = WhmConcepts.AfflatusRaptureUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });

        return true;
    }
}
