using System;
using Olympus.Config;

namespace Olympus.Services.Tank;

/// <summary>
/// Implementation of tank cooldown planning service.
/// Determines when to use defensive cooldowns based on HP, damage intake, and configuration.
/// </summary>
public sealed class TankCooldownService : ITankCooldownService
{
    private readonly TankConfig _config;

    private float _currentHpPercent;
    private float _currentDamageRate;
    private float _recentDamagePeak;
    private DateTime _lastPeakTime = DateTime.MinValue;

    public TankCooldownService(TankConfig config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public void Update(float hpPercent, float incomingDamageRate)
    {
        _currentHpPercent = hpPercent;
        _currentDamageRate = incomingDamageRate;

        // Track damage peaks for tank buster detection
        if (incomingDamageRate > _recentDamagePeak || (DateTime.UtcNow - _lastPeakTime).TotalSeconds > 10)
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
