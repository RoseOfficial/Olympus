using Olympus.Data;
using Olympus.Models;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services;

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

    public HealingPriority Priority => HealingPriority.LilyCapPrevention;
    public string Name => "LilyCapPrevention";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
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

    private bool TryExecuteAfflatusSolace(ApolloContext context, bool isMoving)
    {
        if (!context.Configuration.Healing.EnableAfflatusSolace)
            return false;

        // Find anyone with ANY damage (even 1 HP missing)
        var target = context.PartyHelper.FindLowestHpPartyMember(context.Player, healAmount: 1);
        if (target is null)
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
            context.Debug.PlannedAction = $"Afflatus Solace (Lily cap prevention)";
            context.Debug.PlanningState = "Lily Cap Prevention";
            context.ActionTracker.LogAttempt(action.ActionId, target.Name?.TextValue ?? "Unknown",
                target.CurrentHp, ActionResult.Success, context.Player.Level);

            var hpPercent = context.PartyHelper.GetHpPercent(target);
            context.LogHealDecision(
                target.Name?.TextValue ?? "Unknown",
                hpPercent,
                action.Name,
                healAmount,
                $"Lily cap prevention ({context.LilyCount}/3 Lilies, {context.BloodLilyCount}/3 Blood)");
        }
        else
        {
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }

    private bool TryExecuteAfflatusRapture(ApolloContext context, bool isMoving)
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
            context.ActionTracker.LogAttempt(action.ActionId, "Party",
                0, ActionResult.Success, context.Player.Level);

            context.LogHealDecision(
                "Party",
                1.0f, // Party-wide
                action.Name,
                healAmount,
                $"Lily cap prevention AoE ({context.LilyCount}/3 Lilies, {context.BloodLilyCount}/3 Blood)");
        }
        else
        {
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }
}
