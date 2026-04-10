using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Debuff;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles debuff cleansing with Esuna for Sage.
/// Uses priority-based debuff detection for lethal and high-priority debuffs.
/// </summary>
public sealed class EsunaHandler : IHealingHandler
{
    public int Priority => 5;
    public string Name => "Esuna";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.RoleActions.EnableEsuna)
        {
            context.Debug.EsunaState = "Disabled";
            return false;
        }

        if (player.Level < RoleActions.Esuna.MinLevel)
        {
            context.Debug.EsunaState = $"Level {player.Level} < {RoleActions.Esuna.MinLevel}";
            return false;
        }

        if (player.CurrentMp < RoleActions.Esuna.MpCost)
        {
            context.Debug.EsunaState = $"MP {player.CurrentMp} < {RoleActions.Esuna.MpCost}";
            return false;
        }

        var (target, statusId, priority) = EsunaHelper.FindBestTarget(
            player, context.PartyHelper.GetAllPartyMembers(player), context.DebuffDetectionService);

        if (target is null)
        {
            context.Debug.EsunaState = "No target";
            context.Debug.EsunaTarget = "None";
            return false;
        }

        if (priority != DebuffPriority.Lethal && (int)priority > config.RoleActions.EsunaPriorityThreshold)
        {
            context.Debug.EsunaState = $"Priority {priority} > threshold {config.RoleActions.EsunaPriorityThreshold}";
            return false;
        }

        if (isMoving && !context.HasSwiftcast)
        {
            context.Debug.EsunaState = "Moving (no Swiftcast)";
            return false;
        }

        var partyCoord = context.PartyCoordinationService;
        var targetEntityId = (uint)target.GameObjectId;
        if (partyCoord?.IsCleanseTargetReservedByOther(targetEntityId) == true)
        {
            context.Debug.EsunaState = "Reserved by other";
            return false;
        }

        if (partyCoord != null && !partyCoord.ReserveCleanseTarget(targetEntityId, statusId, RoleActions.Esuna.ActionId, (int)priority))
        {
            context.Debug.EsunaState = "Failed to reserve";
            return false;
        }

        var targetName = target.Name?.TextValue ?? "Unknown";
        context.Debug.EsunaTarget = targetName;
        context.Debug.EsunaState = $"Cleansing {priority} debuff";

        if (context.ActionService.ExecuteGcd(RoleActions.Esuna, target.GameObjectId))
            return true;

        partyCoord?.ClearCleanseReservation(targetEntityId);
        return false;
    }
}
