namespace Olympus.Services.Training;

using System;

/// <summary>
/// The outcome of validating a player's rotation decision.
/// </summary>
public enum ValidationOutcome
{
    /// <summary>
    /// The decision was optimal - no better choice existed.
    /// </summary>
    Optimal,

    /// <summary>
    /// The decision was acceptable - not optimal but reasonable.
    /// </summary>
    Acceptable,

    /// <summary>
    /// The decision was suboptimal - a better choice existed.
    /// </summary>
    Suboptimal,
}

/// <summary>
/// Result of validating a rotation decision against optimal play.
/// </summary>
public sealed class DecisionResult
{
    /// <summary>
    /// When this decision was validated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// The action that was actually used.
    /// </summary>
    public uint ActualActionId { get; init; }

    /// <summary>
    /// Human-readable name of the action that was used.
    /// </summary>
    public string ActualActionName { get; init; } = string.Empty;

    /// <summary>
    /// The optimal action that should have been used (null if decision was optimal).
    /// </summary>
    public uint? OptimalActionId { get; init; }

    /// <summary>
    /// Human-readable name of the optimal action (null if decision was optimal).
    /// </summary>
    public string? OptimalActionName { get; init; }

    /// <summary>
    /// The validation outcome.
    /// </summary>
    public ValidationOutcome Outcome { get; init; } = ValidationOutcome.Optimal;

    /// <summary>
    /// Symbol representation of the outcome.
    /// </summary>
    public string Symbol => Outcome switch
    {
        ValidationOutcome.Optimal => "✓",
        ValidationOutcome.Acceptable => "≈",
        ValidationOutcome.Suboptimal => "✗",
        _ => "?",
    };

    /// <summary>
    /// Brief explanation of the validation result.
    /// </summary>
    public string ShortExplanation { get; init; } = string.Empty;

    /// <summary>
    /// Detailed explanation including what would have been better (for suboptimal decisions).
    /// </summary>
    public string DetailedExplanation { get; init; } = string.Empty;

    /// <summary>
    /// Why this decision was suboptimal and what would have been better.
    /// Only populated for suboptimal decisions.
    /// </summary>
    public string? WhatWouldBeBetter { get; init; }

    /// <summary>
    /// The concept ID this decision relates to.
    /// </summary>
    public string? ConceptId { get; init; }

    /// <summary>
    /// Potency difference compared to optimal choice (negative = lost potency).
    /// Only populated when calculable.
    /// </summary>
    public int? PotencyDifference { get; init; }

    /// <summary>
    /// Gauge efficiency impact (e.g., wasted gauge, overcapped resources).
    /// Only populated when relevant.
    /// </summary>
    public string? GaugeImpact { get; init; }

    /// <summary>
    /// Cooldown efficiency impact (e.g., drift time, missed uses).
    /// Only populated when relevant.
    /// </summary>
    public string? CooldownImpact { get; init; }

    /// <summary>
    /// Target name if the decision involved targeting.
    /// </summary>
    public string? TargetName { get; init; }

    /// <summary>
    /// Context about the game state when this decision was made.
    /// </summary>
    public string? ContextInfo { get; init; }

    /// <summary>
    /// Creates an optimal decision result.
    /// </summary>
    public static DecisionResult Optimal(
        uint actionId,
        string actionName,
        string explanation,
        string? conceptId = null) => new()
        {
            ActualActionId = actionId,
            ActualActionName = actionName,
            Outcome = ValidationOutcome.Optimal,
            ShortExplanation = explanation,
            DetailedExplanation = explanation,
            ConceptId = conceptId,
        };

    /// <summary>
    /// Creates an acceptable decision result.
    /// </summary>
    public static DecisionResult Acceptable(
        uint actionId,
        string actionName,
        uint optimalActionId,
        string optimalActionName,
        string explanation,
        string whatWouldBeBetter,
        string? conceptId = null) => new()
        {
            ActualActionId = actionId,
            ActualActionName = actionName,
            OptimalActionId = optimalActionId,
            OptimalActionName = optimalActionName,
            Outcome = ValidationOutcome.Acceptable,
            ShortExplanation = explanation,
            DetailedExplanation = $"{explanation} {whatWouldBeBetter}",
            WhatWouldBeBetter = whatWouldBeBetter,
            ConceptId = conceptId,
        };

    /// <summary>
    /// Creates a suboptimal decision result.
    /// </summary>
    public static DecisionResult Suboptimal(
        uint actionId,
        string actionName,
        uint optimalActionId,
        string optimalActionName,
        string explanation,
        string whatWouldBeBetter,
        string? conceptId = null,
        int? potencyDifference = null) => new()
        {
            ActualActionId = actionId,
            ActualActionName = actionName,
            OptimalActionId = optimalActionId,
            OptimalActionName = optimalActionName,
            Outcome = ValidationOutcome.Suboptimal,
            ShortExplanation = explanation,
            DetailedExplanation = $"{explanation} {whatWouldBeBetter}",
            WhatWouldBeBetter = whatWouldBeBetter,
            ConceptId = conceptId,
            PotencyDifference = potencyDifference,
        };
}

/// <summary>
/// Tracks optimal decision rate statistics for a concept.
/// </summary>
public sealed class DecisionStatistics
{
    /// <summary>
    /// The concept ID being tracked.
    /// </summary>
    public string ConceptId { get; init; } = string.Empty;

    /// <summary>
    /// Total number of decisions made for this concept.
    /// </summary>
    public int TotalDecisions { get; set; }

    /// <summary>
    /// Number of optimal decisions.
    /// </summary>
    public int OptimalDecisions { get; set; }

    /// <summary>
    /// Number of acceptable decisions.
    /// </summary>
    public int AcceptableDecisions { get; set; }

    /// <summary>
    /// Number of suboptimal decisions.
    /// </summary>
    public int SuboptimalDecisions { get; set; }

    /// <summary>
    /// Optimal decision rate (0.0 to 1.0).
    /// </summary>
    public float OptimalRate => TotalDecisions > 0 ? (float)OptimalDecisions / TotalDecisions : 0f;

    /// <summary>
    /// Acceptable-or-better decision rate (0.0 to 1.0).
    /// </summary>
    public float AcceptableOrBetterRate => TotalDecisions > 0
        ? (float)(OptimalDecisions + AcceptableDecisions) / TotalDecisions
        : 0f;

    /// <summary>
    /// Record a new decision result.
    /// </summary>
    public void RecordDecision(ValidationOutcome outcome)
    {
        TotalDecisions++;
        switch (outcome)
        {
            case ValidationOutcome.Optimal:
                OptimalDecisions++;
                break;
            case ValidationOutcome.Acceptable:
                AcceptableDecisions++;
                break;
            case ValidationOutcome.Suboptimal:
                SuboptimalDecisions++;
                break;
        }
    }

    /// <summary>
    /// Reset all statistics.
    /// </summary>
    public void Reset()
    {
        TotalDecisions = 0;
        OptimalDecisions = 0;
        AcceptableDecisions = 0;
        SuboptimalDecisions = 0;
    }
}
