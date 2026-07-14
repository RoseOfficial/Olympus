using System;
using System.Numerics;

namespace Olympus.Services.Movement.Geometry;

/// <summary>
/// Conical AoE: sector of a circle defined by radius and half-angle, oriented along caster facing.
/// </summary>
public sealed class AOEShapeCone : AOEShape
{
    public float Radius { get; }
    public float HalfAngleRadians { get; }

    public AOEShapeCone(float radius, float halfAngleRadians)
    {
        Radius = radius;
        HalfAngleRadians = halfAngleRadians;
    }

    public override bool Contains(Vector2 origin, float rotationRadians, Vector2 point)
    {
        var offset = point - origin;
        var distSq = offset.LengthSquared();
        if (distSq > Radius * Radius)
            return false;
        if (distSq == 0f)
            return true;

        var dist = MathF.Sqrt(distSq);
        // FFXIV convention: rotation=0 is South (+Z world = +Y in the XZ 2D plane).
        // forward = (sin r, cos r) matches BossMod's WDir(Sin, Cos) in (X,Z) space.
        var forward = new Vector2(MathF.Sin(rotationRadians), MathF.Cos(rotationRadians));
        var dir = offset / dist;
        var dot = Vector2.Dot(forward, dir);
        return dot >= MathF.Cos(HalfAngleRadians);
    }

    public override bool ContainsExpanded(Vector2 origin, float rotationRadians, Vector2 point, float marginYalms)
    {
        var offset = point - origin;
        var distSq = offset.LengthSquared();
        var r = Radius + marginYalms;
        if (distSq > r * r)
            return false;
        if (distSq == 0f)
            return true;

        var dist = MathF.Sqrt(distSq);
        var forward = new Vector2(MathF.Sin(rotationRadians), MathF.Cos(rotationRadians));
        var dir = offset / dist;
        var dot = Vector2.Dot(forward, dir);
        return dot >= MathF.Cos(HalfAngleRadians);
    }
}
