using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Helpers for player-status-driven rotation safety gates.
/// Owns the forced-movement detection path that lets damage modules suppress
/// cast-time GCD execution while the player is under Forward/Backward/Left/Right
/// March or similar involuntary-movement debuffs.
/// </summary>
public static class PlayerSafetyHelper
{
    /// <summary>
    /// Pure predicate over the forced-movement status ID list.
    /// Exposed for unit testing — the full <see cref="IsForcedMovementActive"/>
    /// path cannot be tested because Dalamud's StatusList is a native struct.
    /// </summary>
    public static bool IsForcedMovementStatusId(uint statusId) =>
        FFXIVConstants.ForcedMovementStatusIds.Contains(statusId);

    /// <summary>
    /// Returns true if the player has any forced-movement debuff active.
    /// Guards against null player and null StatusList — IBattleChara.StatusList
    /// can be null mid-frame for despawning actors and in unit-test mocks.
    /// </summary>
    public static bool IsForcedMovementActive(IBattleChara? player)
    {
        if (player?.StatusList == null)
            return false;

        foreach (var status in player.StatusList)
        {
            if (status == null)
                continue;
            if (FFXIVConstants.ForcedMovementStatusIds.Contains(status.StatusId))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Pure predicate over the stand-still punisher status ID list (Pyretic family).
    /// </summary>
    public static bool IsStandStillPunisherStatusId(uint statusId) =>
        FFXIVConstants.StandStillPunisherStatusIds.Contains(statusId);

    /// <summary>
    /// Returns true if the player has a Pyretic-style "any action kills you" debuff active.
    /// Used to halt all rotation/healing module execution until the debuff resolves.
    /// </summary>
    public static bool IsStandStillPunisherActive(IBattleChara? player)
    {
        if (player?.StatusList == null)
            return false;

        foreach (var status in player.StatusList)
        {
            if (status == null)
                continue;
            if (FFXIVConstants.StandStillPunisherStatusIds.Contains(status.StatusId))
                return true;
        }

        return false;
    }
}
