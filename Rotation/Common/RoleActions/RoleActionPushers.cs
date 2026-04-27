using System;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.Common.RoleActions;

/// <summary>
/// Static push helpers for self-contained role actions whose gating shape is
/// "fire when threshold X met and not on cooldown" with no rotation-specific
/// trigger logic. Replaces the duplicated TryPushXxx methods that previously
/// lived in each rotation's BuffModule / DamageModule / MitigationModule.
///
/// Apollo (WHM) Lucid Dreaming is a documented exception — it uses a
/// predictive MP-forecast path that stays in <c>ApolloCore</c>.
///
/// The helpers do not write to <c>ctx.Debug</c>; callers pass debug writes
/// in the <c>onDispatched</c> callback because <c>IRotationContext</c> does
/// not expose Debug at the interface level.
/// </summary>
public static class RoleActionPushers
{
    /// <summary>
    /// Pushes Lucid Dreaming if MP percentage falls below the supplied threshold,
    /// the action is off cooldown, and the player meets level requirements.
    /// </summary>
    /// <param name="ctx">Rotation context.</param>
    /// <param name="scheduler">Scheduler queue.</param>
    /// <param name="behavior">Per-rotation AbilityBehavior carrying the toggle delegate.</param>
    /// <param name="mpThresholdPct">Fire when MpPercent is below this value (0.0 to 1.0).</param>
    /// <param name="priority">Scheduler priority for the push.</param>
    /// <param name="onDispatched">Optional callback invoked when the candidate dispatches.</param>
    public static void TryPushLucidDreaming(
        IRotationContext ctx,
        RotationScheduler scheduler,
        AbilityBehavior behavior,
        float mpThresholdPct,
        int priority,
        Action<IRotationContext>? onDispatched = null)
    {
        var player = ctx.Player;
        if (player.Level < Data.RoleActions.LucidDreaming.MinLevel) return;
        if (!ctx.ActionService.IsActionReady(Data.RoleActions.LucidDreaming.ActionId)) return;

        var mpPct = player.MaxMp > 0 ? (float)player.CurrentMp / player.MaxMp : 1f;
        if (mpPct >= mpThresholdPct) return;

        scheduler.PushOgcd(behavior, player.GameObjectId, priority, onDispatched);
    }
}
