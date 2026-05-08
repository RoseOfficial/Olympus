using System.Numerics;

namespace Olympus.Services.Movement.Geometry;

/// <summary>
/// Abstract AoE geometry primitive. Each subclass implements containment for a specific shape kind
/// (circle, cone, rectangle, donut, cross). Ported from BossMod's AOE shape model.
/// </summary>
public abstract class AOEShape
{
    /// <summary>
    /// Returns true if <paramref name="point"/> is inside the shape positioned at
    /// <paramref name="origin"/> with the caster facing <paramref name="rotationRadians"/>.
    /// </summary>
    public abstract bool Contains(Vector2 origin, float rotationRadians, Vector2 point);
}
