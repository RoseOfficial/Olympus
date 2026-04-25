using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific buff module (scheduler-driven).
/// </summary>
public sealed class BuffModule : BaseBuffModule<IAstraeaContext>, IAstraeaModule
{
    public override string Name => "Buff";

    private static readonly string[] _lightspeedAlternatives =
    {
        "Save for upcoming movement mechanic",
        "Save for emergency raise (instant Ascend)",
        "Use Swiftcast for single instant spell",
    };

    protected override bool IsLucidDreamingEnabled(IAstraeaContext context) =>
        context.Configuration.HealerShared.EnableLucidDreaming;
    protected override ActionDefinition GetLucidDreamingAction() => RoleActions.LucidDreaming;
    protected override bool HasLucidDreaming(IAstraeaContext context) => AstraeaStatusHelper.HasLucidDreaming(context.Player);
    protected override float GetLucidDreamingThreshold(IAstraeaContext context) => context.Configuration.HealerShared.LucidDreamingThreshold;
    protected override void SetLucidState(IAstraeaContext context, string state) => context.Debug.LucidState = state;
    protected override void SetPlannedAction(IAstraeaContext context, string action) => context.Debug.PlannedAction = action;
    protected override bool RequiresCombat => true;
    protected override bool TryJobSpecificBuffs(IAstraeaContext context, bool isMoving) => false;

    public override bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) return;

        TryPushLightspeed(context, scheduler, isMoving);
        TryPushLucidDreaming(context, scheduler);
    }

    public override void UpdateDebugState(IAstraeaContext context)
    {
        var player = context.Player;
        var mpPercent = player.MaxMp > 0 ? (float)player.CurrentMp / player.MaxMp : 1f;
        context.Debug.LucidState = mpPercent < context.Configuration.HealerShared.LucidDreamingThreshold
            ? $"Low MP ({mpPercent:P0})"
            : $"OK ({mpPercent:P0})";
        context.Debug.LightspeedState = context.HasLightspeed ? "Active" : "Idle";
    }

    private void TryPushLightspeed(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableLightspeed) return;
        if (player.Level < ASTActions.Lightspeed.MinLevel) return;
        if (!context.ActionService.IsActionReady(ASTActions.Lightspeed.ActionId)) return;
        if (context.HasLightspeed) return;

        bool shouldUse = config.LightspeedStrategy switch
        {
            LightspeedUsageStrategy.OnCooldown => true,
            LightspeedUsageStrategy.SaveForMovement => isMoving,
            LightspeedUsageStrategy.SaveForRaise => false,
            _ => false
        };
        if (!shouldUse) return;

        var capturedIsMoving = isMoving;

        scheduler.PushOgcd(AstraeaAbilities.LightspeedBuff, player.GameObjectId, priority: 195,
            onDispatched: _ =>
            {
                SetPlannedAction(context, ASTActions.Lightspeed.Name);
                context.Debug.LightspeedState = "Active";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var strategyReason = config.LightspeedStrategy switch
                    {
                        LightspeedUsageStrategy.OnCooldown => "using on cooldown for maximum uptime",
                        LightspeedUsageStrategy.SaveForMovement => "movement detected",
                        _ => "manual usage",
                    };

                    var shortReason = $"Lightspeed - {strategyReason}";
                    var factors = new[]
                    {
                        $"Strategy: {config.LightspeedStrategy}",
                        capturedIsMoving ? "Currently moving" : "Not moving",
                        "All GCDs become instant for 15s",
                        "60s cooldown",
                        "Great for movement-heavy phases",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = ASTActions.Lightspeed.ActionId,
                        ActionName = "Lightspeed",
                        Category = "Buff",
                        TargetName = "Self",
                        ShortReason = shortReason,
                        DetailedReason = $"Lightspeed activated ({config.LightspeedStrategy} strategy). {(capturedIsMoving ? "Currently moving - Lightspeed allows full GCD usage while mobile." : "Used proactively for instant casts.")} For 15 seconds, all GCDs are instant.",
                        Factors = factors,
                        Alternatives = _lightspeedAlternatives,
                        Tip = "Lightspeed is amazing for movement! Use it during mechanics that require constant repositioning. Also great for emergency raises - Ascend becomes instant.",
                        ConceptId = AstConcepts.LightspeedUsage,
                        Priority = ExplanationPriority.Normal,
                    });

                    context.TrainingService?.RecordConceptApplication(AstConcepts.LightspeedUsage, wasSuccessful: true, "Lightspeed activated");
                }
            });
    }

    private void TryPushLucidDreaming(IAstraeaContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var lucidAction = RoleActions.LucidDreaming;

        if (!IsLucidDreamingEnabled(context)) return;
        if (player.Level < lucidAction.MinLevel) return;
        if (!context.ActionService.IsActionReady(lucidAction.ActionId)) return;
        if (HasLucidDreaming(context)) return;
        if (!ShouldUseLucidDreaming(context)) return;

        scheduler.PushOgcd(AstraeaAbilities.LucidDreaming, player.GameObjectId, priority: 200,
            onDispatched: _ =>
            {
                SetPlannedAction(context, lucidAction.Name);
                SetLucidState(context, "Lucid Dreaming");
            });
    }
}
