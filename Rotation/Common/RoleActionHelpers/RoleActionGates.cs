using Olympus.Data;
using Olympus.Rotation.Common;

namespace Olympus.Rotation.Common.RoleActionHelpers;

/// <summary>
/// Pure-bool eligibility gates for rotation-integrated role actions.
/// Trigger logic stays in the rotation; the gate only checks level + cooldown.
///
/// Toggle delegates live on the per-rotation <see cref="Olympus.Models.Action.AbilityBehavior"/>
/// and are consulted by the scheduler at dispatch time. Callers may also gate
/// on a per-job toggle before invoking these helpers.
/// </summary>
public static class RoleActionGates
{
    /// <summary>
    /// True iff Swiftcast is leveled and ready to fire (cooldown matured).
    /// </summary>
    public static bool SwiftcastReady(IRotationContext ctx)
    {
        if (ctx.Player.Level < RoleActions.Swiftcast.MinLevel) return false;
        if (!ctx.ActionService.IsActionReady(RoleActions.Swiftcast.ActionId)) return false;
        return true;
    }

    /// <summary>
    /// True iff True North is leveled and at least one charge is available.
    /// True North is a 2-charge ability with a 45s recast per charge;
    /// <see cref="Olympus.Services.Action.IActionService.IsActionReady"/> already
    /// accounts for charge availability.
    /// </summary>
    public static bool TrueNorthReady(IRotationContext ctx)
    {
        if (ctx.Player.Level < RoleActions.TrueNorth.MinLevel) return false;
        if (!ctx.ActionService.IsActionReady(RoleActions.TrueNorth.ActionId)) return false;
        return true;
    }
}
