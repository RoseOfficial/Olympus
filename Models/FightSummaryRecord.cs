using System;
using System.Collections.Generic;

namespace Olympus.Models;

public sealed class FightSummaryRecord
{
    public DateTime Timestamp { get; init; }
    public uint JobId { get; init; }
    public required string ZoneName { get; init; }
    public TimeSpan Duration { get; init; }
    public float GcdUptimePercent { get; init; }
    public float EstimatedDps { get; init; }
    public int? FflogsPercentile { get; init; }
    public required string Grade { get; init; }
    public int DeathCount { get; init; }
    public IReadOnlyList<FightCallout> Callouts { get; init; } = Array.Empty<FightCallout>();
}
