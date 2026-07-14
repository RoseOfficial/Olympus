using System;
using System.Collections.Generic;

namespace Olympus.Rotation.Common.Scheduling;

/// <summary>
/// Outcome of a single <c>DispatchGcd</c> or <c>DispatchOgcd</c> call.
/// </summary>
public sealed class SchedulerDispatchResult
{
    /// <summary>
    /// Singleton empty result for the no-candidates-in-queue path.
    /// <c>Array.Empty&lt;string&gt;()</c> is used for <c>GateFailReasons</c>
    /// so the shared instance cannot be mutated via a downcast to <c>IList&lt;string&gt;</c>.
    /// </summary>
    public static readonly SchedulerDispatchResult Empty = new()
    {
        Dispatched = false,
        Winner = null,
        GateFailReasons = Array.Empty<string>(),
    };

    /// <summary>True if a candidate was dispatched this frame.</summary>
    public required bool Dispatched { get; init; }

    /// <summary>The winning behavior, or null if nothing dispatched.</summary>
    public AbilityBehavior? Winner { get; init; }

    /// <summary>
    /// For each non-winner candidate, a short explanation of why it didn't fire
    /// (e.g., "JugularRip: ProcBuff", "BlastingZone: Cooldown 3.2s").
    /// Bounded: last 16 entries max to avoid unbounded growth on queue blow-ups.
    /// </summary>
    public required IReadOnlyList<string> GateFailReasons { get; init; }
}
