using System;
using System.Collections.Generic;
using System.Numerics;
using Olympus.Services.Movement;
using Olympus.Services.Movement.Geometry;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class SafeEdgeSolverTests
{
    private static IReadOnlyList<TrackedAOE> Threats(params (Vector2 origin, AOEShape shape)[] entries)
    {
        var list = new List<TrackedAOE>();
        foreach (var (origin, shape) in entries)
            list.Add(new TrackedAOE(0, origin, 0f, shape, DateTime.UtcNow.AddSeconds(2)));
        return list;
    }

    [Fact]
    public void Sample16_ProducesCircleOfCandidates()
    {
        var center = new Vector2(0, 0);
        var candidates = SafeEdgeSolver.SampleCandidates(center, numSamples: 16, radius: 5f);
        var list = new List<Vector2>(candidates);
        Assert.Equal(16, list.Count);
        foreach (var p in list)
            Assert.Equal(5f, Vector2.Distance(p, center), precision: 3);
    }

    [Fact]
    public void Score_ClearCandidate_ReturnsThreatCount()
    {
        var threats = Threats((new Vector2(0, 0), new AOEShapeCircle(3f)));
        var score = SafeEdgeSolver.Score(new Vector2(10, 0), threats, _ => false);
        Assert.Equal(1f, score);
    }

    [Fact]
    public void Score_BlockedPath_ReturnsLargeNegative()
    {
        var threats = Threats((new Vector2(0, 0), new AOEShapeCircle(3f)));
        var score = SafeEdgeSolver.Score(new Vector2(10, 0), threats, _ => true);
        Assert.True(score <= -100f);
    }

    [Fact]
    public void Score_CandidateInsideThreat_ScoresZero()
    {
        var threats = Threats((new Vector2(0, 0), new AOEShapeCircle(5f)));
        var score = SafeEdgeSolver.Score(new Vector2(2, 0), threats, _ => false);
        Assert.Equal(0f, score);
    }

    [Fact]
    public void Score_MultipleThreats_CountsCleared()
    {
        var threats = Threats(
            (new Vector2(0, 0), new AOEShapeCircle(3f)),
            (new Vector2(20, 0), new AOEShapeCircle(3f)));
        var score = SafeEdgeSolver.Score(new Vector2(10, 0), threats, _ => false);
        Assert.Equal(2f, score);
    }
}
