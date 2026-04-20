using System;
using Olympus.Rotation.Common;

namespace Olympus.Rotation.Common.Scheduling;

/// <summary>
/// Internal queue entry. Modules construct these via <c>RotationScheduler.Push*</c>.
/// </summary>
public readonly struct AbilityCandidate
{
    public required AbilityBehavior Behavior { get; init; }
    public required ulong TargetId { get; init; }
    public required int Priority { get; init; }
    public required int InsertionOrder { get; init; }
    public Action<IRotationContext>? OnDispatched { get; init; }
}
