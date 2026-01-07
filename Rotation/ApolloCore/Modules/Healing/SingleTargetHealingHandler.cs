using Olympus.Data;
using Olympus.Models;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles single-target GCD healing (Cure, Cure II, etc.).
/// Uses triage to select the best target and HealingSpellSelector for optimal spell.
/// </summary>
public sealed class SingleTargetHealingHandler : IHealingHandler
{
    private const byte CureMinLevel = 2;

    public HealingPriority Priority => HealingPriority.SingleHeal;
    public string Name => "SingleTargetHeal";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing)
            return false;

        if (player.Level < CureMinLevel)
            return false;

        // Use damage intake triage if enabled, otherwise fall back to lowest HP
        // Pass ShieldTrackingService for shield-aware triage scoring
        var target = config.Healing.UseDamageIntakeTriage
            ? context.PartyHelper.FindMostEndangeredPartyMember(
                player, context.DamageIntakeService, 0, context.DamageTrendService, context.ShieldTrackingService)
            : context.PartyHelper.FindLowestHpPartyMember(player);

        if (target is null)
            return false;

        // Skip if another handler is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId))
            return false;

        // Co-healer awareness: Skip if co-healer has pending heal covering most of missing HP
        if (config.Healing.EnableCoHealerAwareness && context.CoHealerDetectionService?.HasCoHealer == true)
        {
            var coHealerPendingHeals = context.CoHealerDetectionService.CoHealerPendingHeals;
            if (coHealerPendingHeals.TryGetValue(target.EntityId, out var pendingHeal))
            {
                var missingHp = target.MaxHp - target.CurrentHp;
                var pendingHealPercent = missingHp > 0 ? (float)pendingHeal / missingHp : 1f;

                // Skip if co-healer's pending heal covers enough of the missing HP
                if (pendingHealPercent >= config.Healing.CoHealerPendingHealThreshold)
                    return false;
            }
        }

        var hasRegen = StatusHelper.HasRegenActive(target, out var regenRemaining);
        var isInMpConservation = context.MpForecastService.IsInConservationMode;

        var (action, healAmount) = context.HealingSpellSelector.SelectBestSingleHeal(
            player, target, context.CanExecuteOgcd, context.HasFreecure, hasRegen, regenRemaining, isInMpConservation);
        if (action is null)
            return false;

        if (isMoving && action.CastTime > 0)
            return false;

        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
        var healAmountRaw = action.EstimateHealAmountRaw(mind, det, wd, player.Level);
        context.Debug.LastHealAmount = healAmount;
        context.Debug.LastHealStats = $"MND:{mind} DET:{det} WD:{wd} Lv:{player.Level} Pot:{action.HealPotency}";
        context.HpPredictionService.RegisterPendingHeal(target.EntityId, healAmount);

        if (action.HealPotency > 0)
            context.CombatEventService.RegisterPredictionForCalibration(healAmountRaw);

        bool success;
        if (action.Category == ActionCategory.oGCD)
        {
            success = context.ActionService.ExecuteOgcd(action, target.GameObjectId);
        }
        else
        {
            if (action.MpCost >= 1000 && ThinAirHelper.ShouldWaitForThinAir(context))
            {
                context.HpPredictionService.ClearPendingHeals();
                return false;
            }

            success = context.ActionService.ExecuteGcd(action, target.GameObjectId);
        }

        if (success)
        {
            // Reserve target to prevent other handlers from double-healing
            context.HealingCoordination.TryReserveTarget(target.EntityId);

            var thinAirNote = context.HasThinAir ? " + Thin Air" : "";
            context.Debug.PlannedAction = action.Name + thinAirNote;
            context.Debug.PlanningState = "Single Heal";
            context.ActionTracker.LogAttempt(action.ActionId, target.Name?.TextValue ?? "Unknown", target.CurrentHp, ActionResult.Success, player.Level);

            // Log healing decision
            var hpPercent = context.PartyHelper.GetHpPercent(target);
            var conservationNote = isInMpConservation ? ", MP conservation" : "";
            var freecureNote = context.HasFreecure ? ", Freecure proc" : "";
            context.LogHealDecision(
                target.Name?.TextValue ?? "Unknown",
                hpPercent,
                action.Name,
                healAmount,
                $"Single-target{conservationNote}{freecureNote}{thinAirNote}");
        }
        else
        {
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }
}
