using System;
using System.Collections.Generic;
using System.Numerics;
using Olympus.Services.Movement.Geometry;

namespace Olympus.Services.Movement;

/// <summary>
/// Tracks active enemy AoE casts and their decoded geometry. Subscribers query
/// <see cref="ActiveAOEs"/> each frame to evaluate threats against the local player.
/// </summary>
public interface IEnemyAOECastTracker
{
    IReadOnlyList<TrackedAOE> ActiveAOEs { get; }
}

/// <summary>
/// A single active enemy cast with decoded geometry.
/// </summary>
public readonly record struct TrackedAOE(
    ulong CasterId,
    Vector2 Origin,
    float RotationRadians,
    AOEShape Shape,
    DateTime ResolveAt);
