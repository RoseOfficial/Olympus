using System;
using System.Collections.Generic;

namespace Olympus.Services.Resource;

/// <summary>
/// Tracks MP usage patterns and provides forecasting for intelligent resource management.
/// </summary>
public sealed class MpForecastService : IMpForecastService
{
    // FFXIV MP regeneration constants
    private const float BaseMpRegenPercent = 0.02f; // 2% MP regen per tick (3 seconds)
    private const float LucidDreamingBonusPercent = 0.055f; // 5.5% per tick (550 potency / 10000)
    private const float TickInterval = 3.0f; // Server ticks every 3 seconds

    // Tracking window for consumption analysis
    private const float TrackingWindowSeconds = 30f;
    private const int MaxTrackedExpenditures = 20;

    // Conservation mode thresholds
    private const float ConservationMpThreshold = 0.30f; // Enter conservation below 30% MP
    private const float ConservationExitThreshold = 0.50f; // Exit conservation above 50% MP

    // MP expenditure tracking
    private readonly Queue<(DateTime Time, int Amount)> _recentExpenditures = new();
    private DateTime _lastUpdateTime = DateTime.UtcNow;

    // Current state
    private int _currentMp;
    private int _maxMp = 10000;
    private bool _hasLucidDreaming;
    private bool _isInConservationMode;

    /// <inheritdoc />
    public int CurrentMp => _currentMp;

    /// <inheritdoc />
    public float MpPercent => _maxMp > 0 ? (float)_currentMp / _maxMp : 1f;

    /// <inheritdoc />
    public int MaxMp => _maxMp;

    /// <inheritdoc />
    public bool IsLucidDreamingActive => _hasLucidDreaming;

    /// <inheritdoc />
    public bool IsInConservationMode => _isInConservationMode;

    /// <inheritdoc />
    public void Update(int currentMp, int maxMp, bool hasLucidDreaming)
    {
        _currentMp = currentMp;
        _maxMp = maxMp > 0 ? maxMp : 10000;
        _hasLucidDreaming = hasLucidDreaming;
        _lastUpdateTime = DateTime.UtcNow;

        // Prune old expenditures
        PruneOldExpenditures();

        // Update conservation mode state with hysteresis
        UpdateConservationMode();
    }

    /// <inheritdoc />
    public void RecordMpExpenditure(int mpCost)
    {
        if (mpCost <= 0)
            return;

        _recentExpenditures.Enqueue((DateTime.UtcNow, mpCost));

        // Limit queue size
        while (_recentExpenditures.Count > MaxTrackedExpenditures)
            _recentExpenditures.Dequeue();
    }

    /// <inheritdoc />
    public float GetMpRegenRate()
    {
        // Base regen: 2% of max MP per 3-second tick
        var baseRegenPerSecond = (_maxMp * BaseMpRegenPercent) / TickInterval;

        // Lucid Dreaming bonus: 5.5% of max MP per tick
        var lucidBonus = _hasLucidDreaming
            ? (_maxMp * LucidDreamingBonusPercent) / TickInterval
            : 0f;

        return baseRegenPerSecond + lucidBonus;
    }

    /// <inheritdoc />
    public float GetMpConsumptionRate()
    {
        if (_recentExpenditures.Count == 0)
            return 0f;

        var now = DateTime.UtcNow;
        var totalMp = 0;
        var oldestTime = now;

        foreach (var (time, amount) in _recentExpenditures)
        {
            var age = (now - time).TotalSeconds;
            if (age <= TrackingWindowSeconds)
            {
                totalMp += amount;
                if (time < oldestTime)
                    oldestTime = time;
            }
        }

        if (totalMp == 0)
            return 0f;

        var windowDuration = (float)(now - oldestTime).TotalSeconds;
        return windowDuration > 0.1f ? totalMp / windowDuration : 0f;
    }

    /// <inheritdoc />
    public float GetNetMpRate()
    {
        return GetMpRegenRate() - GetMpConsumptionRate();
    }

    /// <inheritdoc />
    public float SecondsUntilOom(int reserveMp = 2400)
    {
        var netRate = GetNetMpRate();

        // If gaining MP or not consuming, we won't run out
        if (netRate >= 0)
            return float.MaxValue;

        // Calculate how much MP we can spend before hitting reserve
        var spendableMp = _currentMp - reserveMp;
        if (spendableMp <= 0)
            return 0f; // Already at or below reserve

        // Time = MP / consumption rate (negative rate, so divide by absolute value)
        return spendableMp / -netRate;
    }

    /// <inheritdoc />
    public float GetTimeUntilMpBelowThreshold(int thresholdMp)
    {
        var netRate = GetNetMpRate();

        // If gaining MP or stable, won't reach threshold
        if (netRate >= 0)
            return float.MaxValue;

        // Calculate MP to lose before hitting threshold
        var mpToLose = _currentMp - thresholdMp;
        if (mpToLose <= 0)
            return 0f; // Already at or below threshold

        // Time = MP to lose / consumption rate (negative rate)
        return mpToLose / -netRate;
    }

    /// <summary>
    /// Removes expenditures older than the tracking window.
    /// </summary>
    private void PruneOldExpenditures()
    {
        var now = DateTime.UtcNow;
        while (_recentExpenditures.Count > 0)
        {
            var (time, _) = _recentExpenditures.Peek();
            if ((now - time).TotalSeconds > TrackingWindowSeconds)
                _recentExpenditures.Dequeue();
            else
                break;
        }
    }

    /// <summary>
    /// Updates conservation mode with hysteresis to prevent oscillation.
    /// </summary>
    private void UpdateConservationMode()
    {
        var mpPercent = MpPercent;
        var netRate = GetNetMpRate();

        if (_isInConservationMode)
        {
            // Exit conservation mode when MP is high enough and stable/gaining
            if (mpPercent >= ConservationExitThreshold && netRate >= 0)
                _isInConservationMode = false;
        }
        else
        {
            // Enter conservation mode when MP is low and dropping
            if (mpPercent < ConservationMpThreshold && netRate < 0)
                _isInConservationMode = true;
        }
    }
}
