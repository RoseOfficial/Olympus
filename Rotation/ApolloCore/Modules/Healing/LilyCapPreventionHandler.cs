using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Lily cap prevention by forcing Lily spells when Lilies are at 3/3.
/// This prevents wasting Lily regeneration (1 Lily every 20 seconds).
/// </summary>
public sealed class LilyCapPreventionHandler : IHealingHandler
{
    // Lily gauge caps at 3, regenerates 1 every 20 seconds
    private const int MaxLilies = 3;
    private const int AfflatusSolaceMinLevel = 52;
    private const int AfflatusRaptureMinLevel = 76;

    // Training explanation arrays
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

    public bool TryExecute(IApolloContext context, bool isMoving)
    {
        if (!context.InCombat) return false;

        var config = context.Configuration;
        var player = context.Player;

        // Only activate when Lilies are capped
        if (context.LilyCount < MaxLilies)
            return false;

        if (!config.EnableHealing || !config.Healing.EnableLilyCapPrevention)
            return false;

        if (player.Level < AfflatusSolaceMinLevel)
            return false;

        // Try AoE first (Afflatus Rapture) if multiple injured
        if (player.Level >= AfflatusRaptureMinLevel)
        {
            var injuredInRange = context.PartyHelper.CountInjuredInAoERange(
                player, WHMActions.AfflatusRapture.Radius, 0.99f); // 99% HP threshold

            if (injuredInRange >= 2)
            {
                if (TryExecuteAfflatusRapture(context, isMoving))
                    return true;
            }
        }

        // Fall back to single-target (Afflatus Solace)
        return TryExecuteAfflatusSolace(context, isMoving);
    }

    private bool TryExecuteAfflatusSolace(IApolloContext context, bool isMoving)
    {
        if (!context.Configuration.Healing.EnableAfflatusSolace)
            return false;

        // Find anyone with ANY damage (even 1 HP missing)
        var target = context.PartyHelper.FindLowestHpPartyMember(context.Player, healAmount: 1);
        if (target is null)
            return false;

        // Skip if another handler is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId))
            return false;

        if (!DistanceHelper.IsInRange(context.Player, target, WHMActions.AfflatusSolace.Range))
            return false;

        var action = WHMActions.AfflatusSolace;
        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(context.Player.Level);
        var healAmount = action.EstimateHealAmount(mind, det, wd, context.Player.Level);

        context.HpPredictionService.RegisterPendingHeal(target.EntityId, healAmount);

        var targetName = target.Name?.TextValue ?? "Unknown";
        var success = context.ActionService.ExecuteGcd(action, target.GameObjectId);
        if (success)
        {
            // Reserve target to prevent other handlers from double-healing
            context.HealingCoordination.TryReserveTarget(target.EntityId);

            context.Debug.PlannedAction = $"Afflatus Solace (Lily cap prevention)";
            context.Debug.PlanningState = "Lily Cap Prevention";

            var hpPercent = context.PartyHelper.GetHpPercent(target);
            context.LogHealDecision(
                targetName,
                hpPercent,
                action.Name,
                healAmount,
                $"Lily cap prevention ({context.LilyCount}/3 Lilies, {context.BloodLilyCount}/3 Blood)");

            // Training mode: capture explanation
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

                var alternatives = _solaceCapAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Afflatus Solace",
                    Category = "Resource Management",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Afflatus Solace on {targetName} to prevent Lily cap. At 3/3 Lilies, you're wasting the free Lily regeneration (one every 20 seconds). Used on {targetName} at {hpPercent:P0} HP. This also builds toward Blood Lily ({context.BloodLilyCount}/3).",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Never cap Lilies! Each wasted Lily regeneration is a free GCD heal you're throwing away.",
                    ConceptId = WhmConcepts.LilyManagement,
                    Priority = ExplanationPriority.Normal,
                });
            }
        }
        else
        {
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }

    private bool TryExecuteAfflatusRapture(IApolloContext context, bool isMoving)
    {
        if (!context.Configuration.Healing.EnableAfflatusRapture)
            return false;

        var action = WHMActions.AfflatusRapture;
        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(context.Player.Level);
        var healAmount = action.EstimateHealAmount(mind, det, wd, context.Player.Level);

        // Register pending heals for all party members
        foreach (var member in context.PartyHelper.GetPartyMembers(context.Player))
        {
            if (member.CurrentHp < member.MaxHp)
            {
                context.HpPredictionService.RegisterPendingHeal(member.EntityId, healAmount);
            }
        }

        var success = context.ActionService.ExecuteGcd(action, context.Player.GameObjectId);
        if (success)
        {
            context.Debug.PlannedAction = $"Afflatus Rapture (Lily cap prevention)";
            context.Debug.PlanningState = "Lily Cap Prevention (AoE)";

            context.LogHealDecision(
                "Party",
                1.0f, // Party-wide
                action.Name,
                healAmount,
                $"Lily cap prevention AoE ({context.LilyCount}/3 Lilies, {context.BloodLilyCount}/3 Blood)");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var injuredCount = 0;
                foreach (var member in context.PartyHelper.GetPartyMembers(context.Player))
                {
                    if (member.CurrentHp < member.MaxHp)
                        injuredCount++;
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

                var alternatives = _raptureCapAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Afflatus Rapture",
                    Category = "Resource Management",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Afflatus Rapture to prevent Lily cap. At 3/3 Lilies, you're wasting the free Lily regeneration (one every 20 seconds). {injuredCount} party members had some damage, making AoE more efficient than Solace. This also builds toward Blood Lily ({context.BloodLilyCount}/3).",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "When capped on Lilies with multiple injured, Rapture is better than Solace - you get more total healing value!",
                    ConceptId = WhmConcepts.AfflatusRaptureUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }
        }
        else
        {
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }
}
