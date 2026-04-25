using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Debuff;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles debuff cleansing with Esuna for Sage.
/// </summary>
public sealed class EsunaHandler : IHealingHandler
{
    public int Priority => 5;
    public string Name => "Esuna";

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.RoleActions.EnableEsuna) { context.Debug.EsunaState = "Disabled"; return; }
        if (player.Level < RoleActions.Esuna.MinLevel) { context.Debug.EsunaState = $"Level {player.Level} < {RoleActions.Esuna.MinLevel}"; return; }
        if (player.CurrentMp < RoleActions.Esuna.MpCost) { context.Debug.EsunaState = $"MP {player.CurrentMp} < {RoleActions.Esuna.MpCost}"; return; }

        var (target, statusId, priority) = EsunaHelper.FindBestTarget(
            player, context.PartyHelper.GetAllPartyMembers(player), context.DebuffDetectionService);
        if (target is null) { context.Debug.EsunaState = "No target"; context.Debug.EsunaTarget = "None"; return; }

        if (priority != DebuffPriority.Lethal && (int)priority > config.RoleActions.EsunaPriorityThreshold)
        {
            context.Debug.EsunaState = $"Priority {priority} > threshold {config.RoleActions.EsunaPriorityThreshold}";
            return;
        }

        if (isMoving && !context.HasSwiftcast) { context.Debug.EsunaState = "Moving (no Swiftcast)"; return; }

        var partyCoord = context.PartyCoordinationService;
        var targetEntityId = (uint)target.GameObjectId;
        if (partyCoord?.IsCleanseTargetReservedByOther(targetEntityId) == true)
        {
            context.Debug.EsunaState = "Reserved by other";
            return;
        }

        if (partyCoord != null && !partyCoord.ReserveCleanseTarget(targetEntityId, statusId, RoleActions.Esuna.ActionId, (int)priority))
        {
            context.Debug.EsunaState = "Failed to reserve";
            return;
        }

        var capturedTargetEntityId = targetEntityId;
        context.Debug.EsunaTarget = target.Name?.TextValue ?? "Unknown";
        context.Debug.EsunaState = $"Cleansing {priority} debuff";

        scheduler.PushGcd(AsclepiusAbilities.Esuna, target.GameObjectId, priority: Priority,
            onDispatched: _ => { });
    }
}
