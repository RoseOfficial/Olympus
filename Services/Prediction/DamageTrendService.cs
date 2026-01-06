using System;

namespace Olympus.Services.Prediction;

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

    public DamageTrendService(IDamageIntakeService damageIntakeService)
    {
        _damageIntakeService = damageIntakeService;
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
}
