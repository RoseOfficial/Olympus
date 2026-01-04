using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Olympus.Services;

/// <summary>
/// Tracks suppressed errors for debugging without spamming logs.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public sealed class ErrorMetricsService : IErrorMetricsService
{
    private readonly ConcurrentDictionary<string, ErrorMetric> _errors = new();

    /// <inheritdoc />
    public int TotalErrorCount => _errors.Values.Sum(e => e.Count);

    /// <inheritdoc />
    public void RecordError(string category, string message)
    {
        var now = DateTime.UtcNow;

        _errors.AddOrUpdate(
            category,
            // Add new entry
            _ => new ErrorMetric
            {
                Count = 1,
                FirstOccurrence = now,
                LastOccurrence = now,
                LastMessage = message
            },
            // Update existing entry
            (_, existing) => new ErrorMetric
            {
                Count = existing.Count + 1,
                FirstOccurrence = existing.FirstOccurrence,
                LastOccurrence = now,
                LastMessage = message
            });
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ErrorMetric> GetMetrics()
    {
        return new Dictionary<string, ErrorMetric>(_errors);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _errors.Clear();
    }

    /// <summary>
    /// Gets a formatted summary of all errors for debug display.
    /// </summary>
    public string GetSummary()
    {
        if (_errors.IsEmpty)
            return "No errors recorded";

        var lines = _errors
            .OrderByDescending(e => e.Value.Count)
            .Select(e => $"{e.Key}: {e.Value.Count}x (last: {e.Value.LastMessage})");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Gets error count for a specific category.
    /// </summary>
    public int GetErrorCount(string category)
    {
        return _errors.TryGetValue(category, out var metric) ? metric.Count : 0;
    }
}
