using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Handles Recitation combo for Scholar. Priority 10 in the oGCD list.
/// No Aetherflow cost. Sets up guaranteed crit heal on next applicable spell.
/// </summary>
public sealed class RecitationHandler : IHealingHandler
{
    public int Priority => 10;
    public string Name => "Recitation";

    private static readonly string[] _recitationAlternatives =
    {
        "Use Recitation with different follow-up",
        "Save for emergency (guaranteed crit heal)",
        "Hold for raidwide (Recitation + Indom)",
    };

    public bool TryExecute(IAthenaContext context, bool isMoving)
        => TryRecitationCombo(context);

    private bool TryRecitationCombo(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableRecitation)
            return false;

        if (player.Level < SCHActions.Recitation.MinLevel)
            return false;

        // Already have Recitation active - handled by other methods
        if (context.StatusHelper.HasRecitation(player))
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Recitation.ActionId))
            return false;

        // Check if we have a good target for the follow-up
        bool shouldUseRecitation = config.RecitationPriority switch
        {
            RecitationPriority.Excogitation => ShouldUseExcogitation(context),
            RecitationPriority.Indomitability => ShouldUseIndomitability(context),
            RecitationPriority.Adloquium => ShouldUseSingleTargetHeal(context),
            RecitationPriority.Succor => ShouldUseAoEHeal(context),
            _ => false
        };

        if (!shouldUseRecitation)
            return false;

        var action = SCHActions.Recitation;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Recitation";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var followUp = config.RecitationPriority switch
                {
                    RecitationPriority.Excogitation => "Excogitation",
                    RecitationPriority.Indomitability => "Indomitability",
                    RecitationPriority.Adloquium => "Adloquium",
                    RecitationPriority.Succor => "Succor",
                    _ => "Unknown"
                };

                var shortReason = $"Recitation for guaranteed crit {followUp}";

                var factors = new[]
                {
                    $"Next ability: {followUp} (configured priority)",
                    "Guarantees critical heal on next applicable spell",
                    "No Aetherflow cost when paired with Aetherflow abilities",
                    $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                };

                var alternatives = _recitationAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Recitation",
                    Category = "Healing",
                    TargetName = null,
                    ShortReason = shortReason,
                    DetailedReason = $"Recitation guarantees a critical heal on the next Adloquium, Succor, Indomitability, or Excogitation. Also removes Aetherflow cost. Planning to follow with {followUp} for maximum value.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Recitation is best used before Excogitation (crit Excog) or before raidwides with Indomitability. The free Aetherflow cost is a nice bonus!",
                    ConceptId = SchConcepts.RecitationUsage,
                    Priority = ExplanationPriority.Normal,
                });

                context.TrainingService.RecordConceptApplication(SchConcepts.RecitationUsage, wasSuccessful: true);
            }

            return true;
        }

        return false;
    }

    private static bool ShouldUseExcogitation(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (player.Level < SCHActions.Excogitation.MinLevel)
            return false;

        var target = context.PartyHelper.FindExcogitationTarget(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        return hpPercent <= config.ExcogitationThreshold;
    }

    private static bool ShouldUseIndomitability(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        return avgHp <= config.AoEHealThreshold && injuredCount >= config.AoEHealMinTargets;
    }

    private static bool ShouldUseSingleTargetHeal(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        return hpPercent <= config.AdloquiumThreshold;
    }

    private static bool ShouldUseAoEHeal(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var (count, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, 0);
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Timeline-aware: pre-shield before raidwides
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Use if party needs healing OR raidwide is imminent (for pre-shielding)
        return (avgHp <= config.AoEHealThreshold && count >= config.AoEHealMinTargets) || raidwideImminent;
    }
}
