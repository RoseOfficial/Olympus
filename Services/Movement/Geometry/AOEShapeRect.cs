using System;
using System.Numerics;

namespace Olympus.Services.Movement.Geometry;

/// <summary>
/// Rectangular line AoE: extends forward from origin along caster facing for <see cref="Length"/>,
/// with total width 2 * <see cref="HalfWidth"/>. The rectangle starts at the caster (no length behind).
/// </summary>
public sealed class AOEShapeRect : AOEShape
{
    public float Length { get; }
    public float HalfWidth { get; }

    public AOEShapeRect(float length, float halfWidth)
    {
        Length = length;
        HalfWidth = halfWidth;
    }

    public override bool Contains(Vector2 origin, float rotationRadians, Vector2 point)
    {
        var offset = point - origin;
        // FFXIV convention: rotation=0 is South (+Z world = +Y in the XZ 2D plane).
        var forward = new Vector2(MathF.Sin(rotationRadians), MathF.Cos(rotationRadians));
        var alongForward = Vector2.Dot(offset, forward);
        if (alongForward < 0f || alongForward > Length)
            return false;
        var orthogonal = new Vector2(-forward.Y, forward.X);
        var alongOrthogonal = MathF.Abs(Vector2.Dot(offset, orthogonal));
        return alongOrthogonal <= HalfWidth;
    }

    public override bool ContainsExpanded(Vector2 origin, float rotationRadians, Vector2 point, float marginYalms)
    {
        var offset = point - origin;
        var forward = new Vector2(MathF.Sin(rotationRadians), MathF.Cos(rotationRadians));
        var alongForward = Vector2.Dot(offset, forward);
        if (alongForward < -marginYalms || alongForward > Length + marginYalms)
            return false;
        var orthogonal = new Vector2(-forward.Y, forward.X);
        var alongOrthogonal = MathF.Abs(Vector2.Dot(offset, orthogonal));
        return alongOrthogonal <= HalfWidth + marginYalms;
    }
}
