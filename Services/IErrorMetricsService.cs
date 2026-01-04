using System;
using System.Collections.Generic;

namespace Olympus.Services;

/// <summary>
/// Tracks suppressed errors for debugging without spamming logs.
/// </summary>
public interface IErrorMetricsService
{
    /// <summary>
    /// Records an error occurrence in a category.
    /// </summary>
    /// <param name="category">Error category (e.g., "SafeGameAccess", "Apollo.Execute").</param>
    /// <param name="message">Error message or context.</param>
    void RecordError(string category, string message);

    /// <summary>
    /// Gets all recorded error metrics.
    /// </summary>
    IReadOnlyDictionary<string, ErrorMetric> GetMetrics();

    /// <summary>
    /// Gets total error count across all categories.
    /// </summary>
    int TotalErrorCount { get; }

    /// <summary>
    /// Clears all recorded error metrics.
    /// </summary>
    void Clear();
}

/// <summary>
/// Represents aggregated error metrics for a single category.
/// </summary>
public sealed record ErrorMetric
{
    /// <summary>Total count of errors in this category.</summary>
    public int Count { get; init; }

    /// <summary>When the first error in this category occurred.</summary>
    public DateTime FirstOccurrence { get; init; }

    /// <summary>When the most recent error in this category occurred.</summary>
    public DateTime LastOccurrence { get; init; }

    /// <summary>The most recent error message.</summary>
    public string LastMessage { get; init; } = string.Empty;
}
