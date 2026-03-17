using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific buff module.
/// Extends base buff logic with Dissipation for Aetherflow management.
/// </summary>
public sealed class BuffModule : BaseBuffModule<IAthenaContext>, IAthenaModule
{
    public override string Name => "Buff"; // SCH uses "Buff" instead of "Buffs"

    // Training explanation arrays
    private static readonly string[] _dissipationAlternatives =
    {
        "Wait for Aetherflow to come off cooldown",
        "Use Aetherflow first (if available)",
        "Don't Dissipate if fairy needed soon",
    };

    #region Base Class Overrides - Configuration

    protected override bool IsLucidDreamingEnabled(IAthenaContext context) =>
        context.Configuration.Scholar.EnableLucidDreaming;

    protected override ActionDefinition GetLucidDreamingAction() =>
        RoleActions.LucidDreaming;

    protected override bool HasLucidDreaming(IAthenaContext context) =>
        AthenaStatusHelper.HasLucidDreaming(context.Player);

    protected override float GetLucidDreamingThreshold(IAthenaContext context) =>
        context.Configuration.Scholar.LucidDreamingThreshold;

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetLucidState(IAthenaContext context, string state) =>
        context.Debug.LucidState = state;

    protected override void SetPlannedAction(IAthenaContext context, string action) =>
        context.Debug.PlannedAction = action;

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// SCH requires combat for buff usage.
    /// </summary>
    protected override bool RequiresCombat => true;

    /// <summary>
    /// SCH-specific buffs: Dissipation (sacrifice fairy for Aetherflow).
    /// Called after Lucid Dreaming since it's more situational.
    /// </summary>
    protected override bool TryJobSpecificUtilities(IAthenaContext context, bool isMoving)
    {
        if (TryDissipation(context))
            return true;

        return false;
    }

    #endregion

    #region SCH-Specific Methods

    private bool TryDissipation(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableDissipation)
            return false;

        if (player.Level < SCHActions.Dissipation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Dissipation.ActionId))
            return false;

        // Don't use if fairy is not present (need Eos to sacrifice)
        if (!context.FairyStateManager.IsFairyAvailable)
            return false;

        // Don't use if we already have stacks (would waste it)
        if (context.AetherflowService.CurrentStacks > 0)
            return false;

        // Check fairy gauge - don't waste high gauge
        if (context.FairyGaugeService.CurrentGauge > config.DissipationMaxFairyGauge)
            return false;

        // Check party health - only use when party is healthy
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp < config.DissipationSafePartyHp)
            return false;

        if (context.ActionService.ExecuteOgcd(SCHActions.Dissipation, player.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.Dissipation.Name);
            context.Debug.PlanningState = "Dissipation";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var fairyGauge = context.FairyGaugeService.CurrentGauge;

                var shortReason = $"Dissipation - need Aetherflow, fairy gauge low ({fairyGauge})";

                var factors = new[]
                {
                    $"Aetherflow stacks: 0 (need more)",
                    $"Fairy gauge: {fairyGauge}/100",
                    $"Max gauge for Dissipation: {config.DissipationMaxFairyGauge}",
                    $"Party avg HP: {avgHp:P0} (safe to sacrifice fairy)",
                    "Grants 3 Aetherflow stacks + 20% healing buff",
                };

                var alternatives = _dissipationAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = SCHActions.Dissipation.ActionId,
                    ActionName = "Dissipation",
                    Category = "Resource Management",
                    TargetName = null,
                    ShortReason = shortReason,
                    DetailedReason = $"Dissipation to gain 3 Aetherflow stacks. Fairy gauge was low ({fairyGauge}/100), so minimal loss. Party HP at {avgHp:P0} is safe enough to temporarily lose fairy healing. Also grants 20% healing magic buff for 30s. Fairy returns automatically after 30s.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Dissipation is a trade-off: lose fairy for 30s but gain 3 Aetherflow + 20% healing buff. Use when party is stable and fairy gauge is low. Don't use if you need Whispering Dawn or Fey Blessing soon!",
                    ConceptId = SchConcepts.DissipationUsage,
                    Priority = ExplanationPriority.Normal,
                });

                context.TrainingService.RecordConceptApplication(SchConcepts.DissipationUsage, wasSuccessful: true);
            }

            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(IAthenaContext context)
    {
        var player = context.Player;
        var mpPercent = player.MaxMp > 0 ? (float)player.CurrentMp / player.MaxMp : 1f;
        context.Debug.LucidState = mpPercent < context.Configuration.Scholar.LucidDreamingThreshold
            ? $"Low MP ({mpPercent:P0})"
            : $"OK ({mpPercent:P0})";
    }
}
