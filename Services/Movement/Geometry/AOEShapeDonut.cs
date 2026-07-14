using System;
using System.Numerics;

namespace Olympus.Services.Movement.Geometry;

/// <summary>
/// Annular AoE: ring between <see cref="InnerRadius"/> and <see cref="OuterRadius"/>.
/// The inner disc is safe; the outer ring is the danger zone.
/// </summary>
public sealed class AOEShapeDonut : AOEShape
{
    public float InnerRadius { get; }
    public float OuterRadius { get; }

    public AOEShapeDonut(float innerRadius, float outerRadius)
    {
        InnerRadius = innerRadius;
        OuterRadius = outerRadius;
    }

    public override bool Contains(Vector2 origin, float rotationRadians, Vector2 point)
    {
        var distSq = Vector2.DistanceSquared(origin, point);
        return distSq >= InnerRadius * InnerRadius && distSq <= OuterRadius * OuterRadius;
    }

    public override bool ContainsExpanded(Vector2 origin, float rotationRadians, Vector2 point, float marginYalms)
    {
        var distSq = Vector2.DistanceSquared(origin, point);
        // Expand outward (outer grows) and inward (safe center shrinks).
        var innerExpanded = MathF.Max(0f, InnerRadius - marginYalms);
        var outerExpanded = OuterRadius + marginYalms;
        return distSq >= innerExpanded * innerExpanded && distSq <= outerExpanded * outerExpanded;
    }
}
