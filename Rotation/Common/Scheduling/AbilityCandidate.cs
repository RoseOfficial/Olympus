using System;
using System.Numerics;
using Olympus.Rotation.Common;

namespace Olympus.Rotation.Common.Scheduling;

/// <summary>
/// Internal queue entry. Modules construct these via <c>RotationScheduler.Push*</c>.
/// </summary>
internal readonly struct AbilityCandidate
{
    public required AbilityBehavior Behavior { get; init; }
    public required ulong TargetId { get; init; }
    public required int Priority { get; init; }
    public required int InsertionOrder { get; init; }
    public Action<IRotationContext>? OnDispatched { get; init; }

    /// <summary>
    /// Ground-target position. When set, dispatch routes through
    /// <c>ExecuteGroundTargetedOgcd</c> instead of <c>ExecuteOgcd</c>.
    /// Only valid for oGCD candidates.
    /// </summary>
    public Vector3? GroundPosition { get; init; }
}
