using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific buff module.
/// Extends base buff logic with Lightspeed for movement-heavy phases.
/// </summary>
public sealed class BuffModule : BaseBuffModule<AstraeaContext>, IAstraeaModule
{
    public override string Name => "Buff";

    #region Base Class Overrides - Configuration

    protected override bool IsLucidDreamingEnabled(AstraeaContext context) =>
        context.Configuration.Astrologian.EnableLucidDreaming;

    protected override ActionDefinition GetLucidDreamingAction() =>
        ASTActions.LucidDreaming;

    protected override bool HasLucidDreaming(AstraeaContext context) =>
        context.StatusHelper.HasLucidDreaming(context.Player);

    protected override float GetLucidDreamingThreshold(AstraeaContext context) =>
        context.Configuration.Astrologian.LucidDreamingThreshold;

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetLucidState(AstraeaContext context, string state) =>
        context.Debug.LucidState = state;

    protected override void SetPlannedAction(AstraeaContext context, string action) =>
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
    protected override bool TryJobSpecificBuffs(AstraeaContext context, bool isMoving)
    {
        if (TryLightspeed(context, isMoving))
            return true;

        return false;
    }

    #endregion

    #region AST-Specific Methods

    private bool TryLightspeed(AstraeaContext context, bool isMoving)
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
            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(AstraeaContext context)
    {
        var player = context.Player;
        var mpPercent = player.MaxMp > 0 ? (float)player.CurrentMp / player.MaxMp : 1f;
        context.Debug.LucidState = mpPercent < context.Configuration.Astrologian.LucidDreamingThreshold
            ? $"Low MP ({mpPercent:P0})"
            : $"OK ({mpPercent:P0})";

        context.Debug.LightspeedState = context.HasLightspeed ? "Active" : "Idle";
    }
}
