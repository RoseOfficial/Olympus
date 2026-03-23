using System;
using Olympus.Config;
using Olympus.Timeline;
using Olympus.Timeline.Models;

namespace Olympus.Services.Tank;

/// <summary>
/// Implementation of tank cooldown planning service.
/// Determines when to use defensive cooldowns based on HP, damage intake, and configuration.
/// </summary>
public sealed class TankCooldownService : ITankCooldownService
{
    private readonly TankConfig _config;
    private readonly ITimelineService? _timelineService;

    private float _currentHpPercent;
    private float _currentDamageRate;
    private float _recentDamagePeak;
    private DateTime _lastPeakTime = DateTime.MinValue;

    public TankCooldownService(TankConfig config, ITimelineService? timelineService = null)
    {
        _config = config;
        _timelineService = timelineService;
    }

    /// <inheritdoc />
    public void Update(float hpPercent, float incomingDamageRate)
    {
        _currentHpPercent = hpPercent;
        _currentDamageRate = incomingDamageRate;

        // Track damage peaks for tank buster detection
        // Clear stale peak data after 10s so old spikes don't persist indefinitely
        if ((DateTime.UtcNow - _lastPeakTime).TotalSeconds > 10)
        {
            _recentDamagePeak = 0f;
            _lastPeakTime = DateTime.UtcNow;
        }

        // Record a new peak when the current rate exceeds the tracked peak
        if (incomingDamageRate > _recentDamagePeak)
        {
            _recentDamagePeak = incomingDamageRate;
            _lastPeakTime = DateTime.UtcNow;
        }
    }

    /// <inheritdoc />
    public bool ShouldUseMitigation(float hpPercent, float incomingDamageRate, bool hasActiveMitigation)
    {
        if (!_config.EnableMitigation)
            return false;

        // Don't stack mitigation if we already have one active (unless critically low HP)
        if (hasActiveMitigation && hpPercent > 0.30f)
            return false;

        // Use mitigation below threshold
        if (hpPercent < _config.MitigationThreshold)
            return true;

        // Use mitigation if taking significant damage
        if (incomingDamageRate > 500f && hpPercent < 0.85f)
            return true;

        return false;
    }

    /// <inheritdoc />
    public bool ShouldUseMajorCooldown(float hpPercent, float incomingDamageRate)
    {
        if (!_config.EnableMitigation)
            return false;

        // Major cooldowns for tank busters (high damage spikes)
        // Use when damage rate is very high or HP is critically low
        if (incomingDamageRate > 1000f || hpPercent < 0.40f)
            return true;

        // Use on cooldown if configured and taking damage
        if (_config.UseRampartOnCooldown && incomingDamageRate > 200f)
            return true;

        // Timeline-informed tank buster prediction
        // ShouldHoldCooldowns = IsSoon (<=8s) && IsHighConfidence (>=0.8)
        if (_timelineService is { IsActive: true } &&
            _timelineService.NextTankBuster?.ShouldHoldCooldowns == true)
            return true;

        return false;
    }

    /// <inheritdoc />
    public bool ShouldUseShortCooldown(float hpPercent, int gaugeValue, int minGauge)
    {
        if (!_config.EnableMitigation)
            return false;

        // Don't use if not enough gauge
        if (gaugeValue < minGauge)
            return false;

        // Use short cooldowns more liberally
        // Use when below threshold and have enough gauge
        if (hpPercent < _config.MitigationThreshold && gaugeValue >= _config.SheltronMinGauge)
            return true;

        // Use at max gauge to prevent waste
        if (gaugeValue >= 100 && hpPercent < 0.90f)
            return true;

        return false;
    }
}
