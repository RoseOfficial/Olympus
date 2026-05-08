using System.Numerics;

namespace Olympus.Services.Movement.Probes;

/// <summary>
/// Wraps <c>BGCollisionModule.RaycastMaterialFilter</c> for testability. Returns true if a straight-line path
/// from <paramref name="origin"/> to <paramref name="destination"/> is obstructed by world geometry.
/// </summary>
public interface IBGCollisionProbe
{
    bool IsPathBlocked(Vector3 origin, Vector3 destination);
}
