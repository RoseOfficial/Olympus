using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Services.Party;

namespace Olympus.Rotation.CirceCore.Modules;

/// <summary>
/// RDM resurrection module.
/// Handles Verraise using Dualcast or Swiftcast for instant casts.
/// RDM is unique: hardcast Jolt → Dualcast proc → instant Verraise.
/// </summary>
public sealed class ResurrectionModule : ICirceModule
{
    public int Priority => 15; // Before DamageModule (30) but after BuffModule (20)
    public string Name => "Resurrection";

    /// <summary>
    /// Raise status ID — indicates a pending resurrection on the target.
    /// </summary>
    private const ushort RaiseStatusId = 148;

    public bool TryExecute(ICirceContext context, bool isMoving)
    {
        if (!context.Configuration.RedMage.EnableVerraise)
            return false;

        var player = context.Player;

        if (player.Level < RDMActions.Verraise.MinLevel)
            return false;

        // Verraise costs 2400 MP
        if (player.CurrentMp < 2400)
            return false;

        var deadTarget = FindDeadPartyMember(context);
        if (deadTarget == null)
            return false;

        // Check if another Olympus instance is already raising this target
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord?.IsRaiseTargetReservedByOther((uint)deadTarget.GameObjectId) == true)
        {
            context.Debug.PlanningState = "Raise reserved by other";
            return false;
        }

        // oGCD: Use Swiftcast if we don't have Dualcast and Swiftcast is ready
        if (context.CanExecuteOgcd && !context.HasDualcast && !context.HasSwiftcast)
        {
            if (context.SwiftcastReady)
            {
                if (context.ActionService.ExecuteOgcd(RoleActions.Swiftcast, player.GameObjectId))
                {
                    context.Debug.PlannedAction = "Swiftcast (for Verraise)";
                    return true;
                }
            }
        }

        // GCD: Cast Verraise if we have an instant-cast proc (Dualcast or Swiftcast)
        if (context.CanExecuteGcd)
        {
            if (context.HasDualcast || context.HasSwiftcast)
            {
                // Reserve the target
                if (partyCoord?.ReserveRaiseTarget((uint)deadTarget.GameObjectId, RDMActions.Verraise.ActionId, 0, usingSwiftcast: true) == false)
                {
                    context.Debug.PlanningState = "Failed to reserve raise target";
                    return false;
                }

                if (context.ActionService.ExecuteGcd(RDMActions.Verraise, deadTarget.GameObjectId))
                {
                    var targetName = deadTarget.Name?.TextValue ?? "Unknown";
                    var method = context.HasDualcast ? "Dualcast" : "Swiftcast";
                    context.Debug.PlannedAction = $"Verraise ({method})";
                    context.Debug.PlanningState = $"Raising {targetName}";
                    return true;
                }

                // Clear reservation on failure
                partyCoord?.ClearRaiseReservation((uint)deadTarget.GameObjectId);
            }
        }

        return false;
    }

    public void UpdateDebugState(ICirceContext context)
    {
        // Debug state updated during TryExecute
    }

    /// <summary>
    /// Finds a dead party member that doesn't already have a raise pending.
    /// </summary>
    private IBattleChara? FindDeadPartyMember(ICirceContext context)
    {
        var player = context.Player;
        var rangeSquared = RDMActions.Verraise.RangeSquared; // 30y range

        foreach (var member in context.PartyHelper.GetAllPartyMembers(player, includeDead: true))
        {
            if (member.EntityId == player.EntityId)
                continue;

            if (!member.IsDead)
                continue;

            // Skip if already has Raise pending
            if (HasRaiseStatus(member))
                continue;

            // Range check
            if (Vector3.DistanceSquared(player.Position, member.Position) > rangeSquared)
                continue;

            return member;
        }

        return null;
    }

    /// <summary>
    /// Checks if a target already has a pending raise status.
    /// </summary>
    private static bool HasRaiseStatus(IBattleChara chara)
    {
        if (chara.StatusList == null)
            return false;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == RaiseStatusId)
                return true;
        }

        return false;
    }
}
