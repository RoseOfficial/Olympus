using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.CirceCore.Abilities;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Party;

namespace Olympus.Rotation.CirceCore.Modules;

/// <summary>
/// RDM resurrection module (scheduler-driven).
/// Handles Verraise using Dualcast or Swiftcast for instant casts.
/// RDM hardcast Jolt → Dualcast proc → instant Verraise.
/// </summary>
public sealed class ResurrectionModule : ICirceModule
{
    public int Priority => 15;
    public string Name => "Resurrection";

    private const ushort RaiseStatusId = 148;

    public bool TryExecute(ICirceContext context, bool isMoving) => false;

    public void UpdateDebugState(ICirceContext context) { }

    public void CollectCandidates(ICirceContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.Configuration.RedMage.EnableVerraise) return;
        var player = context.Player;
        if (player.Level < RDMActions.Verraise.MinLevel) return;
        if (player.CurrentMp < 2400) return;

        var deadTarget = FindDeadPartyMember(context);
        if (deadTarget == null) return;

        var partyCoord = context.PartyCoordinationService;
        if (partyCoord?.IsRaiseTargetReservedByOther((uint)deadTarget.GameObjectId) == true)
        {
            context.Debug.PlanningState = "Raise reserved by other";
            return;
        }

        // Swiftcast first if no instant available
        if (!context.HasDualcast && !context.HasSwiftcast && context.SwiftcastReady)
        {
            scheduler.PushOgcd(CirceAbilities.Swiftcast, player.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = "Swiftcast (for Verraise)";
                });
        }

        // Verraise (only when instant)
        if (context.HasDualcast || context.HasSwiftcast)
        {
            if (partyCoord?.ReserveRaiseTarget((uint)deadTarget.GameObjectId, RDMActions.Verraise.ActionId, 0, usingSwiftcast: true) == false)
            {
                context.Debug.PlanningState = "Failed to reserve raise target";
                return;
            }

            scheduler.PushGcd(CirceAbilities.Verraise, deadTarget.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    var targetName = deadTarget.Name?.TextValue ?? "Unknown";
                    var method = context.HasDualcast ? "Dualcast" : "Swiftcast";
                    context.Debug.PlannedAction = $"Verraise ({method})";
                    context.Debug.PlanningState = $"Raising {targetName}";
                });
        }
    }

    private IBattleChara? FindDeadPartyMember(ICirceContext context)
    {
        var player = context.Player;
        var rangeSquared = RDMActions.Verraise.RangeSquared;

        foreach (var member in context.PartyHelper.GetAllPartyMembers(player, includeDead: true))
        {
            if (member.EntityId == player.EntityId) continue;
            if (!member.IsDead) continue;
            if (HasRaiseStatus(member)) continue;
            if (Vector3.DistanceSquared(player.Position, member.Position) > rangeSquared) continue;
            return member;
        }
        return null;
    }

    private static bool HasRaiseStatus(IBattleChara chara)
    {
        if (chara.StatusList == null) return false;
        foreach (var status in chara.StatusList)
            if (status.StatusId == RaiseStatusId) return true;
        return false;
    }
}
