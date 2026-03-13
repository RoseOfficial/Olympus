using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Olympus.Rotation.ApolloCore.Helpers;

/// <summary>
/// Helper methods for distance calculations in FFXIV.
/// Uses squared distance internally for performance (avoids sqrt).
/// </summary>
public static class DistanceHelper
{
    /// <summary>
    /// Checks if two positions are within the specified range.
    /// </summary>
    /// <param name="from">Source position.</param>
    /// <param name="to">Target position.</param>
    /// <param name="range">Maximum range in yalms.</param>
    /// <returns>True if within range, false otherwise.</returns>
    public static bool IsInRange(Vector3 from, Vector3 to, float range)
        => Vector3.DistanceSquared(from, to) <= range * range;

    /// <summary>
    /// Checks if two game objects are within the specified range.
    /// </summary>
    /// <param name="from">Source object.</param>
    /// <param name="to">Target object.</param>
    /// <param name="range">Maximum range in yalms.</param>
    /// <returns>True if within range, false otherwise.</returns>
    public static bool IsInRange(IGameObject from, IGameObject to, float range)
        => IsInRange(from.Position, to.Position, range);

    /// <summary>
    /// Gets the squared distance between two positions.
    /// Useful when comparing distances without needing the actual value.
    /// </summary>
    public static float DistanceSquared(Vector3 from, Vector3 to)
        => Vector3.DistanceSquared(from, to);

    /// <summary>
    /// Checks whether an action can reach a target using the game's native range check.
    /// Uses ActionManager.GetActionInRangeOrLoS which accounts for both player and enemy hitbox radii,
    /// exactly as the game does — more accurate than manual distance math.
    /// Returns true when result is 0 (in range + LoS) or 565 (in range, wrong facing).
    /// </summary>
    public static unsafe bool IsActionInRange(uint actionId, IGameObject player, IGameObject target)
    {
        try
        {
            var playerStruct = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)player.Address;
            var targetStruct = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)target.Address;
            if (playerStruct == null || targetStruct == null) return false;
            var result = ActionManager.GetActionInRangeOrLoS(actionId, playerStruct, targetStruct);
            return result is 0 or 565;
        }
        catch
        {
            return false;
        }
    }
}
