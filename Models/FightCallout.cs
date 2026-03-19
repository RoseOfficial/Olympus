using System;

namespace Olympus.Models;

public enum CalloutSeverity { Good, Warning, Critical }

public enum CalloutCategory { Drift, Waste, Downtime, BurstAlignment, RoleActions, Deaths, DoT }

public sealed class FightCallout
{
    public CalloutSeverity Severity { get; init; }
    public CalloutCategory Category { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public TimeSpan? FightTimestamp { get; init; }
    public float? EstimatedPotencyLoss { get; init; }
}
