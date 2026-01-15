using Olympus.Config;
using Olympus.Data;
using Olympus.Models;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Blood Lily building by preferring Lily heals when close to Afflatus Misery.
/// When at 2 Blood Lilies, prefers Afflatus Solace/Rapture over regular heals to
/// build toward Misery (1240 potency AoE damage) faster.
/// </summary>
/// <remarks>
/// Blood Lily mechanics:
/// - 3 Lily heals (Afflatus Solace/Rapture) = 1 Blood Lily
/// - 3 Blood Lilies = Afflatus Misery ready
/// - This handler activates when at 2 Blood Lilies (one more Lily heal to unlock Misery)
/// </remarks>
public sealed class BloodLilyBuildingHandler : IHealingHandler
{
    // Blood Lily thresholds
    private const int BloodLilyBuildingThreshold = 2; // Activate when at 2 Blood Lilies
    private const int AfflatusSolaceMinLevel = 52;
    private const int AfflatusRaptureMinLevel = 76;

    // Minimum HP thresholds for Blood Lily building
    // More aggressive than cap prevention - we're building for DPS, so use on more injured targets
    private const float AggressiveHpThreshold = 0.85f;
    private const float BalancedHpThreshold = 0.80f;
    private const float ConservativeHpThreshold = 0.70f;

    public HealingPriority Priority => HealingPriority.BloodLilyBuilding;
    public string Name => "BloodLilyBuilding";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        // Check if Blood Lily building is enabled
        if (!config.EnableHealing || !config.Healing.EnableAggressiveLilyFlush)
            return false;

        // Strategy must not be Disabled
        if (config.Healing.LilyStrategy == LilyGenerationStrategy.Disabled)
            return false;

        // Only activate when at 2 Blood Lilies (close to Misery)
        if (context.BloodLilyCount < BloodLilyBuildingThreshold)
            return false;

        // Need at least 1 Lily to spend
        if (context.LilyCount < 1)
            return false;

        if (player.Level < AfflatusSolaceMinLevel)
            return false;

        // Determine HP threshold based on strategy
        var hpThreshold = config.Healing.LilyStrategy switch
        {
            LilyGenerationStrategy.Aggressive => AggressiveHpThreshold,
            LilyGenerationStrategy.Conservative => ConservativeHpThreshold,
            _ => BalancedHpThreshold
        };

        // Try AoE first (Afflatus Rapture) if multiple injured
        if (player.Level >= AfflatusRaptureMinLevel)
        {
            var injuredInRange = context.PartyHelper.CountInjuredInAoERange(
                player, WHMActions.AfflatusRapture.Radius, hpThreshold);

            if (injuredInRange >= 2)
            {
                if (TryExecuteAfflatusRapture(context, isMoving, hpThreshold))
                    return true;
            }
        }

        // Fall back to single-target (Afflatus Solace)
        return TryExecuteAfflatusSolace(context, isMoving, hpThreshold);
    }

    private bool TryExecuteAfflatusSolace(ApolloContext context, bool isMoving, float hpThreshold)
    {
        if (!context.Configuration.Healing.EnableAfflatusSolace)
            return false;

        // Find lowest HP party member
        var target = context.PartyHelper.FindLowestHpPartyMember(context.Player);
        if (target is null)
            return false;

        // Skip if target HP is too high (not actually injured enough for Blood Lily building)
        var targetHpPercent = context.PartyHelper.GetHpPercent(target);
        if (targetHpPercent >= hpThreshold)
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

        var success = context.ActionService.ExecuteGcd(action, target.GameObjectId);
        if (success)
        {
            context.HealingCoordination.TryReserveTarget(target.EntityId);

            context.Debug.PlannedAction = $"Afflatus Solace (Blood Lily building)";
            context.Debug.PlanningState = "Blood Lily Building";
            context.Debug.MiseryState = $"Building ({context.BloodLilyCount}/3 Blood Lilies)";
            context.ActionTracker.LogAttempt(action.ActionId, target.Name?.TextValue ?? "Unknown",
                target.CurrentHp, ActionResult.Success, context.Player.Level);

            context.LogHealDecision(
                target.Name?.TextValue ?? "Unknown",
                targetHpPercent,
                action.Name,
                healAmount,
                $"Blood Lily building ({context.BloodLilyCount}/3 Blood, {context.LilyCount}/3 Lilies - next Lily unlock Misery)");
        }
        else
        {
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }

    private bool TryExecuteAfflatusRapture(ApolloContext context, bool isMoving, float hpThreshold)
    {
        if (!context.Configuration.Healing.EnableAfflatusRapture)
            return false;

        // Verify there are actually injured party members
        var injuredCount = context.PartyHelper.CountInjuredInAoERange(
            context.Player, WHMActions.AfflatusRapture.Radius, hpThreshold);

        if (injuredCount < 2)
            return false;

        var action = WHMActions.AfflatusRapture;
        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(context.Player.Level);
        var healAmount = action.EstimateHealAmount(mind, det, wd, context.Player.Level);

        // Register pending heals for injured party members
        foreach (var member in context.PartyHelper.GetPartyMembers(context.Player))
        {
            if (context.PartyHelper.GetHpPercent(member) < hpThreshold)
            {
                context.HpPredictionService.RegisterPendingHeal(member.EntityId, healAmount);
            }
        }

        var success = context.ActionService.ExecuteGcd(action, context.Player.GameObjectId);
        if (success)
        {
            context.Debug.PlannedAction = $"Afflatus Rapture (Blood Lily building)";
            context.Debug.PlanningState = "Blood Lily Building (AoE)";
            context.Debug.MiseryState = $"Building ({context.BloodLilyCount}/3 Blood Lilies)";
            context.ActionTracker.LogAttempt(action.ActionId, "Party",
                0, ActionResult.Success, context.Player.Level);

            context.LogHealDecision(
                "Party",
                1.0f,
                action.Name,
                healAmount,
                $"Blood Lily building AoE ({context.BloodLilyCount}/3 Blood, {context.LilyCount}/3 Lilies - {injuredCount} injured)");
        }
        else
        {
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }
}
