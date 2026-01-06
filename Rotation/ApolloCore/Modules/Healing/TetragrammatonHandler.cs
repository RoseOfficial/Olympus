using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles oGCD single-target healing with Tetragrammaton.
/// Features charge management and dynamic overheal thresholds.
/// </summary>
public sealed class TetragrammatonHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.Tetragrammaton;
    public string Name => "Tetragrammaton";

    public bool TryExecute(ApolloContext context, bool isMoving)
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
            var hpPercent = context.PartyHelper.GetHpPercent(target);
            var chargeInfo = $"{currentCharges}/{maxCharges} charges";

            if (isAtMaxCharges)
            {
                context.Debug.PlannedAction = $"Tetragrammaton ({chargeInfo}, avoiding cap)";
                context.LogOgcdDecision(
                    target.Name?.TextValue ?? "Unknown",
                    hpPercent,
                    "Tetragrammaton",
                    $"At max charges - using to avoid cap ({chargeInfo})");
            }
            else if (isSpike)
            {
                context.Debug.PlannedAction = $"Tetragrammaton (spike mode, {overhealMultiplier:F1}x overheal allowed)";
                context.LogOgcdDecision(
                    target.Name?.TextValue ?? "Unknown",
                    hpPercent,
                    "Tetragrammaton",
                    $"Spike mode - {overhealMultiplier:F1}x overheal allowed ({chargeInfo})");
            }
            else
            {
                context.LogOgcdDecision(
                    target.Name?.TextValue ?? "Unknown",
                    hpPercent,
                    "Tetragrammaton",
                    $"Standard oGCD heal ({chargeInfo})");
            }
            return true;
        }

        return false;
    }
}
