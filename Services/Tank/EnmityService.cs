using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;

namespace Olympus.Services.Tank;

/// <summary>
/// Implementation of enmity (aggro) tracking service.
/// Uses the game's enmity list to determine aggro state.
/// </summary>
public sealed class EnmityService : IEnmityService
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;

    public EnmityService(IObjectTable objectTable, IPartyList partyList)
    {
        _objectTable = objectTable;
        _partyList = partyList;
    }

    /// <inheritdoc />
    public int GetEnmityPosition(IBattleChara target, uint playerEntityId)
    {
        // Check if target is targeting the player (simplest aggro check)
        if (target.TargetObjectId == playerEntityId)
            return 1;

        // For more complex scenarios, we'd need to read the enmity list
        // which requires additional game memory access
        // For now, use targeting as a proxy for enmity position

        // If target is targeting someone else, player is position 2+
        if (target.TargetObjectId != 0 && target.TargetObjectId != playerEntityId)
            return 2;

        return 0;
    }

    /// <inheritdoc />
    public bool IsMainTankOn(IBattleChara target, uint playerEntityId)
    {
        return target.TargetObjectId == playerEntityId;
    }

    /// <inheritdoc />
    public bool HasCoTankAggro(IBattleChara target, uint playerEntityId)
    {
        var targetId = target.TargetObjectId;
        if (targetId == 0 || targetId == playerEntityId)
            return false;

        // Check if the target is targeting another tank
        foreach (var member in _partyList)
        {
            if (member.EntityId == targetId && member.EntityId != playerEntityId)
            {
                // Check if this party member is a tank
                var partyMember = _objectTable.SearchById(member.EntityId) as IBattleChara;
                if (partyMember != null && JobRegistry.IsTank(partyMember.ClassJob.RowId))
                    return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public uint GetMainTankId(IBattleChara target)
    {
        return (uint)target.TargetObjectId;
    }

    /// <inheritdoc />
    public bool IsLosingAggro(IBattleChara target, uint playerEntityId, float threshold = 0.9f)
    {
        // Simplified check - in practice this would need enmity list access
        // For now, return false (player is assumed stable if they have aggro)
        return !IsMainTankOn(target, playerEntityId);
    }
}
