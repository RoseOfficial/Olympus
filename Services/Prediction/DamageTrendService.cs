using System;
using System.Collections.Generic;

namespace Olympus.Services.Prediction;

/// <summary>
/// Represents a recorded spike event for pattern detection.
/// </summary>
internal readonly struct SpikeEvent
{
    public readonly float Timestamp;
    public readonly int DamageAmount;

    public SpikeEvent(float timestamp, int damageAmount)
    {
        Timestamp = timestamp;
        DamageAmount = damageAmount;
    }
}

/// <summary>
/// Analyzes damage intake trends over time for proactive cooldown decisions.
/// Wraps DamageIntakeService and provides trend analysis capabilities.
/// </summary>
public sealed class DamageTrendService : IDamageTrendService
{
    private readonly IDamageIntakeService _damageIntakeService;

    // Thresholds for trend classification
    private const float StableThresholdPercent = 0.15f;  // +/-15% = stable
    private const float SpikingThresholdPercent = 0.50f; // +50% or more = spiking
    private const float MinDamageRateForTrend = 50f;     // Min DPS to consider for trends

    // Spike pattern detection
    private const int MaxSpikeHistoryPerEntity = 10;     // Keep last 10 spikes per entity
    private const float SpikeHistoryWindowSeconds = 60f; // Clear spikes older than 60s
    private const float MinPatternIntervalSeconds = 3f;  // Minimum interval for pattern detection
    private const float MaxPatternIntervalSeconds = 30f; // Maximum interval for pattern detection
    private const float IntervalTolerancePercent = 0.25f; // 25% tolerance for interval matching

    // Spike history per entity (circular buffer style)
    private readonly Dictionary<uint, List<SpikeEvent>> _spikeHistory = new();

    // Timer for spike timestamps (seconds since service started)
    private float _currentTime = 0f;

    public DamageTrendService(IDamageIntakeService damageIntakeService)
    {
        _damageIntakeService = damageIntakeService;
    }

    // Spike detection state per entity
    private readonly Dictionary<uint, float> _lastSpikeTimes = new();
    private const float MinSpikeCooldown = 2.0f; // Don't record multiple spikes within 2 seconds
    private const float SpikeDamageRateThreshold = 1000f; // DPS threshold to consider a spike

    /// <summary>
    /// Updates the internal timer and automatically detects spikes.
    /// Should be called each frame with delta time.
    /// </summary>
    /// <param name="deltaSeconds">Time since last frame.</param>
    /// <param name="partyEntityIds">Entity IDs of party members to check for spikes.</param>
    public void Update(float deltaSeconds, IEnumerable<uint> partyEntityIds)
    {
        _currentTime += deltaSeconds;

        // Auto-detect spikes for party members
        foreach (var entityId in partyEntityIds)
        {
            DetectAndRecordSpike(entityId);
        }

        CleanupOldSpikes();
    }

    /// <summary>
    /// Legacy method for backwards compatibility.
    /// </summary>
    public void UpdateTime(float deltaSeconds)
    {
        _currentTime += deltaSeconds;
        CleanupOldSpikes();
    }

    /// <summary>
    /// Checks if an entity is experiencing a damage spike and records it for pattern detection.
    /// </summary>
    private void DetectAndRecordSpike(uint entityId)
    {
        // Check cooldown to avoid recording the same spike multiple times
        if (_lastSpikeTimes.TryGetValue(entityId, out var lastSpikeTime))
        {
            if (_currentTime - lastSpikeTime < MinSpikeCooldown)
                return;
        }

        // Get current damage rate and check for spike
        var currentRate = _damageIntakeService.GetDamageRate(entityId, 1f); // Last 1 second
        var previousRate = _damageIntakeService.GetDamageRate(entityId, 3f); // Last 3 seconds

        // Detect spike: damage rate spiked significantly in the last second
        var isSpike = currentRate > SpikeDamageRateThreshold &&
                      (previousRate < 100f || currentRate > previousRate * 2);

        if (isSpike)
        {
            RecordSpikeEvent(entityId, (int)currentRate);
            _lastSpikeTimes[entityId] = _currentTime;
        }
    }

    /// <inheritdoc />
    public DamageTrend GetPartyDamageTrend(float windowSeconds = 10f)
    {
        // Use party damage rate from underlying service
        var currentRate = _damageIntakeService.GetPartyDamageRate(windowSeconds / 2);
        var previousRate = GetPreviousPeriodPartyRate(windowSeconds);

        return ClassifyTrend(currentRate, previousRate);
    }

    /// <inheritdoc />
    public DamageTrend GetEntityDamageTrend(uint entityId, float windowSeconds = 10f)
    {
        // Compare recent damage rate to previous period
        var halfWindow = windowSeconds / 2;
        var currentRate = _damageIntakeService.GetDamageRate(entityId, halfWindow);
        var previousRate = GetPreviousPeriodRate(entityId, windowSeconds);

        return ClassifyTrend(currentRate, previousRate);
    }

    /// <inheritdoc />
    public bool IsDamageSpikeImminent(float confidenceThreshold = 0.8f)
    {
        // Check party-wide damage trend
        var partyTrend = GetPartyDamageTrend(5f);

        // Spike is imminent if:
        // 1. Party damage trend is Spiking with high confidence
        // 2. OR damage is Increasing rapidly
        if (partyTrend == DamageTrend.Spiking)
            return true;

        if (partyTrend == DamageTrend.Increasing)
        {
            // Check if the increase is significant enough
            var currentRate = _damageIntakeService.GetPartyDamageRate(2.5f);
            var previousRate = _damageIntakeService.GetPartyDamageRate(5f);

            if (previousRate > MinDamageRateForTrend)
            {
                var increaseRatio = currentRate / previousRate;
                // If damage doubled in last 2.5 seconds, spike is imminent
                return increaseRatio >= (1.0f + confidenceThreshold);
            }
        }

        return false;
    }

    /// <inheritdoc />
    public float GetDamageAcceleration(uint entityId, float windowSeconds = 5f)
    {
        var halfWindow = windowSeconds / 2;

        // Current DPS (recent half)
        var currentRate = _damageIntakeService.GetDamageRate(entityId, halfWindow);

        // Previous DPS (earlier half)
        var previousRate = GetPreviousPeriodRate(entityId, windowSeconds);

        // Acceleration = (current - previous) / time
        // Positive = increasing, Negative = decreasing
        return (currentRate - previousRate) / halfWindow;
    }

    /// <inheritdoc />
    public float GetCurrentDamageRate(uint entityId, float windowSeconds = 3f)
    {
        return _damageIntakeService.GetDamageRate(entityId, windowSeconds);
    }

    /// <inheritdoc />
    public float GetSpikeSeverity(float avgPartyHpPercent)
    {
        // If no spike detected, severity is 0
        if (!IsDamageSpikeImminent(0.8f))
            return 0f;

        // Get the current damage trend for additional context
        var trend = GetPartyDamageTrend(5f);

        // Base severity from trend type
        var baseSeverity = trend switch
        {
            DamageTrend.Spiking => 0.6f,    // Major spike
            DamageTrend.Increasing => 0.4f, // Building damage
            _ => 0.2f                        // Generic spike detection without clear trend
        };

        // Factor in party HP state - lower HP = higher severity
        // At 100% HP, multiplier is 0.5 (half severity)
        // At 50% HP, multiplier is 1.0 (full severity)
        // At 0% HP, multiplier is 1.5 (critical severity)
        var hpMultiplier = 1.5f - avgPartyHpPercent;

        // Calculate final severity
        var severity = baseSeverity * hpMultiplier;

        // Also factor in raw damage rate for additional context
        var currentRate = _damageIntakeService.GetPartyDamageRate(3f);
        if (currentRate > 5000f)  // Very high damage intake
        {
            severity += 0.2f;
        }
        else if (currentRate > 3000f)  // High damage intake
        {
            severity += 0.1f;
        }

        return severity;
    }

    /// <summary>
    /// Gets the damage rate from the previous period (before the recent half-window).
    /// This allows comparing current damage to recent-past damage for trend detection.
    /// </summary>
    private float GetPreviousPeriodRate(uint entityId, float totalWindowSeconds)
    {
        // Get full window damage
        var fullWindowDamage = _damageIntakeService.GetRecentDamageIntake(entityId, totalWindowSeconds);

        // Get recent half window damage
        var halfWindow = totalWindowSeconds / 2;
        var recentDamage = _damageIntakeService.GetRecentDamageIntake(entityId, halfWindow);

        // Previous period damage = full - recent
        var previousDamage = fullWindowDamage - recentDamage;

        // Return as rate (DPS)
        return previousDamage / halfWindow;
    }

    /// <summary>
    /// Gets the party damage rate from the previous period.
    /// </summary>
    private float GetPreviousPeriodPartyRate(float totalWindowSeconds)
    {
        // Full window rate
        var fullRate = _damageIntakeService.GetPartyDamageRate(totalWindowSeconds);

        // Recent half rate
        var halfWindow = totalWindowSeconds / 2;
        var recentRate = _damageIntakeService.GetPartyDamageRate(halfWindow);

        // Previous period = (fullRate * totalWindow - recentRate * halfWindow) / halfWindow
        var fullDamage = fullRate * totalWindowSeconds;
        var recentDamage = recentRate * halfWindow;
        var previousDamage = fullDamage - recentDamage;

        return previousDamage / halfWindow;
    }

    /// <summary>
    /// Classifies the trend based on current vs previous damage rates.
    /// </summary>
    private static DamageTrend ClassifyTrend(float currentRate, float previousRate)
    {
        // If very little damage, consider stable
        if (currentRate < MinDamageRateForTrend && previousRate < MinDamageRateForTrend)
            return DamageTrend.Stable;

        // Avoid division by zero
        if (previousRate < 1f)
        {
            // No previous damage - if current is significant, it's spiking
            return currentRate >= MinDamageRateForTrend ? DamageTrend.Spiking : DamageTrend.Stable;
        }

        var changeRatio = currentRate / previousRate;

        // Classify based on change ratio
        if (changeRatio >= 1.0f + SpikingThresholdPercent)
            return DamageTrend.Spiking;

        if (changeRatio >= 1.0f + StableThresholdPercent)
            return DamageTrend.Increasing;

        if (changeRatio <= 1.0f - StableThresholdPercent)
            return DamageTrend.Decreasing;

        return DamageTrend.Stable;
    }

    /// <inheritdoc />
    public void RecordSpikeEvent(uint entityId, int damageAmount)
    {
        if (!_spikeHistory.TryGetValue(entityId, out var history))
        {
            history = new List<SpikeEvent>(MaxSpikeHistoryPerEntity);
            _spikeHistory[entityId] = history;
        }

        // Add new spike
        history.Add(new SpikeEvent(_currentTime, damageAmount));

        // Trim to max history size
        while (history.Count > MaxSpikeHistoryPerEntity)
        {
            history.RemoveAt(0);
        }
    }

    /// <inheritdoc />
    public (float secondsUntilSpike, float confidence) PredictNextSpike(uint entityId)
    {
        if (!_spikeHistory.TryGetValue(entityId, out var history) || history.Count < 3)
        {
            // Need at least 3 spikes to detect a pattern
            return (float.MaxValue, 0f);
        }

        // Calculate intervals between consecutive spikes
        var intervals = new List<float>();
        for (int i = 1; i < history.Count; i++)
        {
            var interval = history[i].Timestamp - history[i - 1].Timestamp;
            if (interval >= MinPatternIntervalSeconds && interval <= MaxPatternIntervalSeconds)
            {
                intervals.Add(interval);
            }
        }

        if (intervals.Count < 2)
        {
            return (float.MaxValue, 0f);
        }

        // Find the most common interval (with tolerance)
        var (patternInterval, matchCount) = FindDominantInterval(intervals);

        if (patternInterval < MinPatternIntervalSeconds)
        {
            return (float.MaxValue, 0f);
        }

        // Calculate confidence based on how many intervals match the pattern
        var confidence = (float)matchCount / intervals.Count;

        // Minimum confidence threshold
        if (confidence < 0.5f)
        {
            return (float.MaxValue, 0f);
        }

        // Predict next spike based on last spike time + pattern interval
        var lastSpikeTime = history[^1].Timestamp;
        var predictedNextSpike = lastSpikeTime + patternInterval;
        var secondsUntilSpike = predictedNextSpike - _currentTime;

        // If predicted spike is in the past, it might be imminent now
        // or pattern may have broken - reduce confidence
        if (secondsUntilSpike < 0)
        {
            // Check if we're within tolerance of when spike should have happened
            if (secondsUntilSpike > -patternInterval * IntervalTolerancePercent)
            {
                // Spike might be happening now or very soon
                secondsUntilSpike = 0.5f; // Assume imminent
                confidence *= 0.8f; // Reduce confidence slightly
            }
            else
            {
                // Pattern likely broken
                return (float.MaxValue, 0f);
            }
        }

        // Boost confidence for very consistent patterns
        if (matchCount >= 3 && confidence >= 0.8f)
        {
            confidence = Math.Min(1f, confidence + 0.1f);
        }

        return (secondsUntilSpike, confidence);
    }

    /// <summary>
    /// Finds the dominant (most common) interval in the list.
    /// </summary>
    private static (float interval, int count) FindDominantInterval(List<float> intervals)
    {
        var bestInterval = 0f;
        var bestCount = 0;

        for (int i = 0; i < intervals.Count; i++)
        {
            var candidate = intervals[i];
            var count = 1; // Start with self

            // Count how many other intervals match within tolerance
            for (int j = 0; j < intervals.Count; j++)
            {
                if (i == j) continue;

                var diff = Math.Abs(intervals[j] - candidate);
                var tolerance = candidate * IntervalTolerancePercent;

                if (diff <= tolerance)
                {
                    count++;
                }
            }

            if (count > bestCount)
            {
                bestCount = count;
                bestInterval = candidate;
            }
        }

        // If we found a dominant pattern, calculate the average interval
        if (bestCount >= 2)
        {
            var sum = 0f;
            var matchedCount = 0;

            for (int i = 0; i < intervals.Count; i++)
            {
                var diff = Math.Abs(intervals[i] - bestInterval);
                var tolerance = bestInterval * IntervalTolerancePercent;

                if (diff <= tolerance)
                {
                    sum += intervals[i];
                    matchedCount++;
                }
            }

            bestInterval = sum / matchedCount;
        }

        return (bestInterval, bestCount);
    }

    /// <summary>
    /// Removes spike events older than the history window.
    /// </summary>
    private void CleanupOldSpikes()
    {
        var cutoffTime = _currentTime - SpikeHistoryWindowSeconds;

        foreach (var kvp in _spikeHistory)
        {
            var history = kvp.Value;
            history.RemoveAll(spike => spike.Timestamp < cutoffTime);
        }
    }
}
