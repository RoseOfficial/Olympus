using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Handles Indomitability for Scholar. Priority 25 in the oGCD list.
/// Costs 1 Aetherflow stack (free with Recitation).
/// </summary>
public sealed class IndomitabilityHandler : IHealingHandler
{
    public int Priority => 25;
    public string Name => "Indomitability";

    private static readonly string[] _indomitabilityAlternatives =
    {
        "Succor (GCD, adds shields)",
        "Whispering Dawn (fairy HoT)",
        "Fey Blessing (fairy burst)",
    };

    public bool TryExecute(IAthenaContext context, bool isMoving)
        => TryIndomitability(context);

    private bool TryIndomitability(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableIndomitability)
            return false;

        if (player.Level < SCHActions.Indomitability.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Indomitability.ActionId))
            return false;

        // Skip if no Aetherflow (unless Recitation is active)
        if (!context.StatusHelper.HasRecitation(player))
        {
            if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
                return false;
        }

        if (!ShouldUseIndomitability(context))
            return false;

        var action = SCHActions.Indomitability;

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
        {
            context.Debug.IndomitabilityState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            var hasRecitation = context.StatusHelper.HasRecitation(player);
            if (!hasRecitation)
                context.AetherflowService.ConsumeStack();

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Indomitability";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

                var shortReason = $"Indomitability - {injuredCount} injured, avg HP {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    hasRecitation ? "Recitation active (guaranteed crit, free)" : $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                    "400 potency AoE heal",
                    "oGCD - can weave without clipping",
                };

                var alternatives = _indomitabilityAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Indomitability",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Indomitability to heal {injuredCount} party members at {avgHp:P0} average HP. 400 potency AoE heal, instant oGCD. {(hasRecitation ? "Recitation made this free and guaranteed critical!" : $"Cost 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining).")} Best used after raidwides when multiple party members are injured.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Indomitability is your primary AoE oGCD heal. Pair with Recitation for burst healing. Use after raidwides rather than before (shields go before, heals go after).",
                    ConceptId = SchConcepts.IndomitabilityUsage,
                    Priority = avgHp < 0.5f ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private static bool ShouldUseIndomitability(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        return avgHp <= config.AoEHealThreshold && injuredCount >= config.AoEHealMinTargets;
    }
}
