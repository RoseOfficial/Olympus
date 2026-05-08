using System.Numerics;

namespace Olympus.Services.Movement.Geometry;

/// <summary>
/// Circular AoE. Includes the boundary (&lt;= radius).
/// </summary>
public sealed class AOEShapeCircle : AOEShape
{
    public float Radius { get; }

    public AOEShapeCircle(float radius)
    {
        Radius = radius;
    }

    public override bool Contains(Vector2 origin, float rotationRadians, Vector2 point)
    {
        return Vector2.DistanceSquared(origin, point) <= Radius * Radius;
    }
}
