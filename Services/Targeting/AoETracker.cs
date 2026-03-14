using System;
using System.Collections.Generic;
using System.Numerics;
using Olympus.Models;

namespace Olympus.Services.Targeting;

/// <summary>
/// Tracks predicted vs actual AoE hits for the Smart AoE debug tab.
/// </summary>
public sealed class AoETracker
{
    private const int MaxHistory = 50;
    private readonly List<AoEPrediction> _history = new(MaxHistory + 1);

    public IReadOnlyList<AoEPrediction> History => _history;

    public int TotalPredictions { get; private set; }
    public int TotalCorrect { get; private set; }
    public float AccuracyRate => TotalPredictions > 0 ? (float)TotalCorrect / TotalPredictions * 100f : 0f;

    // Last computed result (for overlay drawing)
    public AoEResult? LastResult { get; set; }
    public Vector3 LastPlayerPosition { get; set; }

    public void RecordPrediction(string actionName, AoEShape shape, int predictedHits, Vector3 playerPos, float angle)
    {
        var entry = new AoEPrediction
        {
            Timestamp = DateTime.Now,
            ActionName = actionName,
            Shape = shape,
            PredictedHits = predictedHits,
            PlayerPosition = playerPos,
            Angle = angle,
        };
        _history.Add(entry);
        if (_history.Count > MaxHistory)
            _history.RemoveAt(0);
    }

    public void RecordActual(int actualHits)
    {
        if (_history.Count == 0) return;
        var last = _history[^1];
        last.ActualHits = actualHits;
        last.Resolved = true;
        _history[^1] = last;

        TotalPredictions++;
        if (last.PredictedHits == actualHits)
            TotalCorrect++;
    }

    public void Reset()
    {
        _history.Clear();
        TotalPredictions = 0;
        TotalCorrect = 0;
        LastResult = null;
    }

    public class AoEPrediction
    {
        public DateTime Timestamp { get; init; }
        public string ActionName { get; init; } = "";
        public AoEShape Shape { get; init; }
        public int PredictedHits { get; set; }
        public int ActualHits { get; set; }
        public bool Resolved { get; set; }
        public Vector3 PlayerPosition { get; init; }
        public float Angle { get; init; }
    }
}
