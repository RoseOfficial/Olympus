using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific buff module.
/// Extends base buff logic with Lightspeed for movement-heavy phases.
/// </summary>
public sealed class BuffModule : BaseBuffModule<IAstraeaContext>, IAstraeaModule
{
    public override string Name => "Buff";

    // Training explanation arrays
    private static readonly string[] _lightspeedAlternatives =
    {
        "Save for upcoming movement mechanic",
        "Save for emergency raise (instant Ascend)",
        "Use Swiftcast for single instant spell",
    };

    #region Base Class Overrides - Configuration

    protected override bool IsLucidDreamingEnabled(IAstraeaContext context) =>
        context.Configuration.Astrologian.EnableLucidDreaming;

    protected override ActionDefinition GetLucidDreamingAction() =>
        ASTActions.LucidDreaming;

    protected override bool HasLucidDreaming(IAstraeaContext context) =>
        AstraeaStatusHelper.HasLucidDreaming(context.Player);

    protected override float GetLucidDreamingThreshold(IAstraeaContext context) =>
        context.Configuration.Astrologian.LucidDreamingThreshold;

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetLucidState(IAstraeaContext context, string state) =>
        context.Debug.LucidState = state;

    protected override void SetPlannedAction(IAstraeaContext context, string action) =>
        context.Debug.PlannedAction = action;

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// AST requires combat for buff usage.
    /// </summary>
    protected override bool RequiresCombat => true;

    /// <summary>
    /// AST-specific buffs: Lightspeed for instant casts.
    /// Called before Lucid Dreaming.
    /// </summary>
    protected override bool TryJobSpecificBuffs(IAstraeaContext context, bool isMoving)
    {
        if (TryLightspeed(context, isMoving))
            return true;

        return false;
    }

    #endregion

    #region AST-Specific Methods

    private bool TryLightspeed(IAstraeaContext context, bool isMoving)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableLightspeed)
            return false;

        if (player.Level < ASTActions.Lightspeed.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(ASTActions.Lightspeed.ActionId))
            return false;

        if (context.HasLightspeed)
            return false;

        // Usage depends on strategy
        bool shouldUse = config.LightspeedStrategy switch
        {
            Config.LightspeedUsageStrategy.OnCooldown => true,
            Config.LightspeedUsageStrategy.SaveForMovement => isMoving,
            Config.LightspeedUsageStrategy.SaveForRaise => false, // Handled by resurrection module
            _ => false
        };

        if (!shouldUse)
            return false;

        if (context.ActionService.ExecuteOgcd(ASTActions.Lightspeed, player.GameObjectId))
        {
            SetPlannedAction(context, ASTActions.Lightspeed.Name);
            context.Debug.LightspeedState = "Active";

            // Training mode: capture explanation
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
                    isMoving ? "Currently moving" : "Not moving",
                    "All GCDs become instant for 15s",
                    "60s cooldown",
                    "Great for movement-heavy phases",
                };

                var alternatives = _lightspeedAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = ASTActions.Lightspeed.ActionId,
                    ActionName = "Lightspeed",
                    Category = "Buff",
                    TargetName = "Self",
                    ShortReason = shortReason,
                    DetailedReason = $"Lightspeed activated ({config.LightspeedStrategy} strategy). {(isMoving ? "Currently moving - Lightspeed allows full GCD usage while mobile." : "Used proactively for instant casts.")} For 15 seconds, all GCDs are instant. This is AST's main tool for maintaining uptime during movement-heavy mechanics!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Lightspeed is amazing for movement! Use it during mechanics that require constant repositioning. Also great for emergency raises - Ascend becomes instant. Don't hold it too long - 60s cooldown means you can use it liberally.",
                    ConceptId = AstConcepts.LightspeedUsage,
                    Priority = ExplanationPriority.Normal,
                });

                context.TrainingService?.RecordConceptApplication(AstConcepts.LightspeedUsage, wasSuccessful: true, "Lightspeed activated");
            }

            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(IAstraeaContext context)
    {
        var player = context.Player;
        var mpPercent = player.MaxMp > 0 ? (float)player.CurrentMp / player.MaxMp : 1f;
        context.Debug.LucidState = mpPercent < context.Configuration.Astrologian.LucidDreamingThreshold
            ? $"Low MP ({mpPercent:P0})"
            : $"OK ({mpPercent:P0})";

        context.Debug.LightspeedState = context.HasLightspeed ? "Active" : "Idle";
    }
}
