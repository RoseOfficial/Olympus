using System;
using System.Numerics;

namespace Olympus.Services.Movement.Geometry;

/// <summary>
/// Cross AoE: two perpendicular rectangles, each extending <see cref="Length"/> from origin in
/// both directions (so total span is 2 * Length on each axis), with total width 2 * <see cref="HalfWidth"/>.
/// </summary>
public sealed class AOEShapeCross : AOEShape
{
    public float Length { get; }
    public float HalfWidth { get; }

    public AOEShapeCross(float length, float halfWidth)
    {
        Length = length;
        HalfWidth = halfWidth;
    }

    public override bool Contains(Vector2 origin, float rotationRadians, Vector2 point)
    {
        var offset = point - origin;
        var forward = new Vector2(MathF.Cos(rotationRadians), MathF.Sin(rotationRadians));
        var orthogonal = new Vector2(-forward.Y, forward.X);
        var alongForward = Vector2.Dot(offset, forward);
        var alongOrthogonal = Vector2.Dot(offset, orthogonal);

        bool inForwardArm = MathF.Abs(alongForward) <= Length && MathF.Abs(alongOrthogonal) <= HalfWidth;
        bool inOrthogonalArm = MathF.Abs(alongOrthogonal) <= Length && MathF.Abs(alongForward) <= HalfWidth;
        return inForwardArm || inOrthogonalArm;
    }
}
