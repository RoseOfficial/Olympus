using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;

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
}
