using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific buff module (scheduler-driven).
/// Pushes Lucid Dreaming + Dissipation candidates.
/// </summary>
public sealed class BuffModule : BaseBuffModule<IAthenaContext>, IAthenaModule
{
    public override string Name => "Buff";

    private static readonly string[] _dissipationAlternatives =
    {
        "Wait for Aetherflow to come off cooldown",
        "Use Aetherflow first (if available)",
        "Don't Dissipate if fairy needed soon",
    };

    protected override bool IsLucidDreamingEnabled(IAthenaContext context) =>
        context.Configuration.HealerShared.EnableLucidDreaming;

    protected override ActionDefinition GetLucidDreamingAction() => RoleActions.LucidDreaming;

    protected override bool HasLucidDreaming(IAthenaContext context) =>
        AthenaStatusHelper.HasLucidDreaming(context.Player);

    protected override float GetLucidDreamingThreshold(IAthenaContext context) =>
        context.Configuration.HealerShared.LucidDreamingThreshold;

    protected override void SetLucidState(IAthenaContext context, string state) =>
        context.Debug.LucidState = state;

    protected override void SetPlannedAction(IAthenaContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override bool RequiresCombat => true;
    protected override bool TryJobSpecificUtilities(IAthenaContext context, bool isMoving) => false;

    public override bool TryExecute(IAthenaContext context, bool isMoving) => false;

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) return;

        TryPushLucidDreaming(context, scheduler);
        TryPushDissipation(context, scheduler);
    }

    public override void UpdateDebugState(IAthenaContext context)
    {
        var player = context.Player;
        var mpPercent = player.MaxMp > 0 ? (float)player.CurrentMp / player.MaxMp : 1f;
        context.Debug.LucidState = mpPercent < context.Configuration.HealerShared.LucidDreamingThreshold
            ? $"Low MP ({mpPercent:P0})"
            : $"OK ({mpPercent:P0})";
    }

    private void TryPushLucidDreaming(IAthenaContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var lucidAction = RoleActions.LucidDreaming;

        if (!IsLucidDreamingEnabled(context)) return;
        if (player.Level < lucidAction.MinLevel) return;
        if (!context.ActionService.IsActionReady(lucidAction.ActionId)) return;
        if (HasLucidDreaming(context)) return;
        if (!ShouldUseLucidDreaming(context)) return;

        scheduler.PushOgcd(AthenaAbilities.LucidDreaming, player.GameObjectId, priority: 200,
            onDispatched: _ =>
            {
                SetPlannedAction(context, lucidAction.Name);
                SetLucidState(context, "Lucid Dreaming");
            });
    }

    private void TryPushDissipation(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableDissipation) return;
        if (player.Level < SCHActions.Dissipation.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.Dissipation.ActionId)) return;
        if (!context.FairyStateManager.IsFairyAvailable) return;
        if (context.AetherflowService.CurrentStacks > 0) return;
        if (context.FairyGaugeService.CurrentGauge > config.DissipationMaxFairyGauge) return;

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp < config.DissipationSafePartyHp) return;

        var capturedAvgHp = avgHp;
        var capturedFairyGauge = context.FairyGaugeService.CurrentGauge;

        scheduler.PushOgcd(AthenaAbilities.Dissipation, player.GameObjectId, priority: 210,
            onDispatched: _ =>
            {
                SetPlannedAction(context, SCHActions.Dissipation.Name);
                context.Debug.PlanningState = "Dissipation";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Dissipation - need Aetherflow, fairy gauge low ({capturedFairyGauge})";
                    var factors = new[]
                    {
                        $"Aetherflow stacks: 0 (need more)",
                        $"Fairy gauge: {capturedFairyGauge}/100",
                        $"Max gauge for Dissipation: {config.DissipationMaxFairyGauge}",
                        $"Party avg HP: {capturedAvgHp:P0} (safe to sacrifice fairy)",
                        "Grants 3 Aetherflow stacks + 20% healing buff",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = SCHActions.Dissipation.ActionId,
                        ActionName = "Dissipation",
                        Category = "Resource Management",
                        TargetName = null,
                        ShortReason = shortReason,
                        DetailedReason = $"Dissipation to gain 3 Aetherflow stacks. Fairy gauge was low ({capturedFairyGauge}/100), so minimal loss. Party HP at {capturedAvgHp:P0} is safe enough to temporarily lose fairy healing. Also grants 20% healing magic buff for 30s. Fairy returns automatically after 30s.",
                        Factors = factors,
                        Alternatives = _dissipationAlternatives,
                        Tip = "Dissipation is a trade-off: lose fairy for 30s but gain 3 Aetherflow + 20% healing buff. Use when party is stable and fairy gauge is low.",
                        ConceptId = SchConcepts.DissipationUsage,
                        Priority = ExplanationPriority.Normal,
                    });

                    context.TrainingService.RecordConceptApplication(SchConcepts.DissipationUsage, wasSuccessful: true);
                }
            });
    }
}
