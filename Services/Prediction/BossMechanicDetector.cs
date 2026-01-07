using System;
using System.Collections.Generic;
using Olympus.Config;

namespace Olympus.Services.Prediction;

/// <summary>
/// Detects boss mechanic patterns (raidwides, tank busters) for proactive healing.
/// Uses interval-based pattern detection similar to DamageTrendService.
/// </summary>
public sealed class BossMechanicDetector : IBossMechanicDetector
{
    private readonly HealingConfig _config;
    private readonly ICombatEventService _combatEventService;
    private readonly IDamageIntakeService _damageIntakeService;

    // Pattern detection constants
    private const int MaxEventHistory = 20;
    private const float EventHistoryWindowSeconds = 180f; // 3 minutes
    private const float MinPatternIntervalSeconds = 10f;  // Minimum interval for pattern detection
    private const float MaxPatternIntervalSeconds = 90f;  // Maximum interval for pattern detection
    private const float IntervalTolerancePercent = 0.25f; // 25% tolerance for interval matching

    // Raidwide tracking
    private readonly List<RaidwideEvent> _raidwideHistory = new();
    private float _lastRaidwideTime = float.MinValue;
    private float _detectedRaidwideInterval = 0f;
    private float _raidwideIntervalConfidence = 0f;
    private float _lastRaidwideDamagePercent = 0f;

    // Tank buster tracking (per tank)
    private readonly Dictionary<uint, List<TankBusterEvent>> _tankBusterHistory = new();
    private readonly Dictionary<uint, float> _detectedTankBusterIntervals = new();
    private readonly Dictionary<uint, float> _tankBusterConfidences = new();
    private uint _lastTankBusterTarget = 0;
    private float _lastTankBusterTime = float.MinValue;
    private int _lastTankBusterDamage = 0;

    // Timer (seconds since combat started)
    private float _currentTime = 0f;

    private record RaidwideEvent(float Timestamp, float DamagePercent, int AffectedCount);
    private record TankBusterEvent(float Timestamp, int DamageAmount, float DamagePercent);

    public BossMechanicDetector(
        HealingConfig config,
        ICombatEventService combatEventService,
        IDamageIntakeService damageIntakeService)
    {
        _config = config;
        _combatEventService = combatEventService;
        _damageIntakeService = damageIntakeService;
    }

    public bool IsRaidwideImminent
    {
        get
        {
            if (!_config.EnableMechanicAwareness) return false;
            var prediction = PredictedRaidwide;
            return prediction != null && prediction.SecondsUntil <= _config.MechanicPreparationWindow;
        }
    }

    public bool IsTankBusterImminent
    {
        get
        {
            if (!_config.EnableMechanicAwareness) return false;
            var prediction = PredictedTankBuster;
            return prediction != null && prediction.SecondsUntil <= _config.MechanicPreparationWindow;
        }
    }

    public float SecondsUntilNextRaidwide
    {
        get
        {
            var prediction = PredictedRaidwide;
            return prediction?.SecondsUntil ?? float.MaxValue;
        }
    }

    public float SecondsUntilNextTankBuster
    {
        get
        {
            var prediction = PredictedTankBuster;
            return prediction?.SecondsUntil ?? float.MaxValue;
        }
    }

    public RaidwidePrediction? PredictedRaidwide
    {
        get
        {
            if (!_config.EnableMechanicAwareness) return null;
            if (_raidwideIntervalConfidence < _config.MechanicPatternConfidence) return null;
            if (_detectedRaidwideInterval <= 0) return null;

            // Calculate time until next raidwide based on detected interval
            var timeSinceLast = _currentTime - _lastRaidwideTime;
            var timeUntilNext = _detectedRaidwideInterval - timeSinceLast;

            if (timeUntilNext <= 0) return null; // Already passed predicted time

            return new RaidwidePrediction(
                timeUntilNext,
                _raidwideIntervalConfidence,
                _lastRaidwideDamagePercent,
                _detectedRaidwideInterval);
        }
    }

    public TankBusterPrediction? PredictedTankBuster
    {
        get
        {
            if (!_config.EnableMechanicAwareness) return null;
            if (_lastTankBusterTarget == 0) return null;

            // Find the tank with the best detected pattern
            uint bestTank = 0;
            float bestConfidence = 0f;
            float bestInterval = 0f;

            foreach (var kvp in _tankBusterConfidences)
            {
                if (kvp.Value > bestConfidence && kvp.Value >= _config.MechanicPatternConfidence)
                {
                    bestConfidence = kvp.Value;
                    bestTank = kvp.Key;
                    bestInterval = _detectedTankBusterIntervals.GetValueOrDefault(kvp.Key);
                }
            }

            if (bestTank == 0 || bestInterval <= 0) return null;

            // Get last tank buster time for this tank
            if (!_tankBusterHistory.TryGetValue(bestTank, out var history) || history.Count == 0)
                return null;

            var lastEvent = history[^1];
            var timeSinceLast = _currentTime - lastEvent.Timestamp;
            var timeUntilNext = bestInterval - timeSinceLast;

            if (timeUntilNext <= 0) return null;

            return new TankBusterPrediction(
                timeUntilNext,
                bestConfidence,
                lastEvent.DamageAmount,
                bestTank);
        }
    }

    public float SecondsSinceLastRaidwide => _lastRaidwideTime > float.MinValue
        ? _currentTime - _lastRaidwideTime
        : float.MaxValue;

    public float SecondsSinceLastTankBuster => _lastTankBusterTime > float.MinValue
        ? _currentTime - _lastTankBusterTime
        : float.MaxValue;

    public void Update()
    {
        // Increment time based on frame time (assumed ~16ms = 0.016s)
        // In practice, this should be called with actual delta time from framework
        _currentTime += 0.016f;

        CleanupOldEvents();
    }

    public void RecordRaidwideDamage(int affectedCount, float averageDamagePercent)
    {
        if (!_config.EnableMechanicAwareness) return;
        if (affectedCount < _config.RaidwideMinTargets) return;
        if (averageDamagePercent < _config.RaidwideMinDamagePercent) return;

        var timestamp = _currentTime;
        _raidwideHistory.Add(new RaidwideEvent(timestamp, averageDamagePercent, affectedCount));

        // Trim history
        while (_raidwideHistory.Count > MaxEventHistory)
            _raidwideHistory.RemoveAt(0);

        // Update last raidwide tracking
        _lastRaidwideTime = timestamp;
        _lastRaidwideDamagePercent = averageDamagePercent;

        // Attempt to detect interval pattern
        DetectRaidwidePattern();
    }

    public void RecordTankBusterDamage(uint tankEntityId, float damagePercent, int damageAmount)
    {
        if (!_config.EnableMechanicAwareness) return;
        if (damagePercent < _config.TankBusterMinDamagePercent) return;

        var timestamp = _currentTime;

        if (!_tankBusterHistory.TryGetValue(tankEntityId, out var history))
        {
            history = new List<TankBusterEvent>();
            _tankBusterHistory[tankEntityId] = history;
        }

        history.Add(new TankBusterEvent(timestamp, damageAmount, damagePercent));

        // Trim history
        while (history.Count > MaxEventHistory)
            history.RemoveAt(0);

        // Update last tank buster tracking
        _lastTankBusterTarget = tankEntityId;
        _lastTankBusterTime = timestamp;
        _lastTankBusterDamage = damageAmount;

        // Attempt to detect interval pattern for this tank
        DetectTankBusterPattern(tankEntityId);
    }

    public void Clear()
    {
        _raidwideHistory.Clear();
        _tankBusterHistory.Clear();
        _detectedTankBusterIntervals.Clear();
        _tankBusterConfidences.Clear();

        _lastRaidwideTime = float.MinValue;
        _detectedRaidwideInterval = 0f;
        _raidwideIntervalConfidence = 0f;

        _lastTankBusterTarget = 0;
        _lastTankBusterTime = float.MinValue;
        _currentTime = 0f;
    }

    private void DetectRaidwidePattern()
    {
        if (_raidwideHistory.Count < 2)
        {
            _detectedRaidwideInterval = 0f;
            _raidwideIntervalConfidence = 0f;
            return;
        }

        // Calculate intervals between raidwides
        var intervals = new List<float>();
        for (int i = 1; i < _raidwideHistory.Count; i++)
        {
            var interval = _raidwideHistory[i].Timestamp - _raidwideHistory[i - 1].Timestamp;
            if (interval >= MinPatternIntervalSeconds && interval <= MaxPatternIntervalSeconds)
                intervals.Add(interval);
        }

        if (intervals.Count < 1)
        {
            _detectedRaidwideInterval = 0f;
            _raidwideIntervalConfidence = 0f;
            return;
        }

        // Find the most common interval (mode) with tolerance
        var (bestInterval, matchCount) = FindBestInterval(intervals);

        _detectedRaidwideInterval = bestInterval;
        _raidwideIntervalConfidence = (float)matchCount / intervals.Count;
    }

    private void DetectTankBusterPattern(uint tankEntityId)
    {
        if (!_tankBusterHistory.TryGetValue(tankEntityId, out var history) || history.Count < 2)
        {
            _detectedTankBusterIntervals[tankEntityId] = 0f;
            _tankBusterConfidences[tankEntityId] = 0f;
            return;
        }

        // Calculate intervals between tank busters
        var intervals = new List<float>();
        for (int i = 1; i < history.Count; i++)
        {
            var interval = history[i].Timestamp - history[i - 1].Timestamp;
            if (interval >= MinPatternIntervalSeconds && interval <= MaxPatternIntervalSeconds)
                intervals.Add(interval);
        }

        if (intervals.Count < 1)
        {
            _detectedTankBusterIntervals[tankEntityId] = 0f;
            _tankBusterConfidences[tankEntityId] = 0f;
            return;
        }

        var (bestInterval, matchCount) = FindBestInterval(intervals);

        _detectedTankBusterIntervals[tankEntityId] = bestInterval;
        _tankBusterConfidences[tankEntityId] = (float)matchCount / intervals.Count;
    }

    private (float BestInterval, int MatchCount) FindBestInterval(List<float> intervals)
    {
        if (intervals.Count == 0)
            return (0f, 0);

        if (intervals.Count == 1)
            return (intervals[0], 1);

        // Try each interval as the reference and count matches
        float bestInterval = 0f;
        int bestMatchCount = 0;

        foreach (var referenceInterval in intervals)
        {
            var matchCount = 0;
            foreach (var interval in intervals)
            {
                var tolerance = referenceInterval * IntervalTolerancePercent;
                if (Math.Abs(interval - referenceInterval) <= tolerance)
                    matchCount++;
            }

            if (matchCount > bestMatchCount)
            {
                bestMatchCount = matchCount;
                bestInterval = referenceInterval;
            }
        }

        // Average the matching intervals for better accuracy
        if (bestMatchCount > 0)
        {
            var sum = 0f;
            var count = 0;
            foreach (var interval in intervals)
            {
                var tolerance = bestInterval * IntervalTolerancePercent;
                if (Math.Abs(interval - bestInterval) <= tolerance)
                {
                    sum += interval;
                    count++;
                }
            }
            if (count > 0)
                bestInterval = sum / count;
        }

        return (bestInterval, bestMatchCount);
    }

    private void CleanupOldEvents()
    {
        var cutoff = _currentTime - EventHistoryWindowSeconds;

        _raidwideHistory.RemoveAll(e => e.Timestamp < cutoff);

        foreach (var history in _tankBusterHistory.Values)
        {
            history.RemoveAll(e => e.Timestamp < cutoff);
        }
    }
}
