using System;
using System.Collections.Generic;
using System.Numerics;
using Olympus.Services.Movement;
using Olympus.Services.Movement.Geometry;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class EnemyAOECastTrackerTests
{
    private sealed class TestableTracker : EnemyAOECastTracker
    {
        public TestableTracker() : base(log: null!, objectTable: null!, clientState: null!) { }

        public new void HandleCastStarted(ulong casterId, byte castType, float effectRange,
            float xAxisModifier, string omenPath, float casterHitboxRadius,
            Vector2 origin, float rotation, float castTimeRemainingSeconds)
            => base.HandleCastStarted(casterId, castType, effectRange, xAxisModifier, omenPath,
                casterHitboxRadius, origin, rotation, castTimeRemainingSeconds);

        public new void HandleCastFinished(ulong casterId) => base.HandleCastFinished(casterId);
        public new void PruneStaleEntries(DateTime now) => base.PruneStaleEntries(now);
        public new void ClearAll() => base.ClearAll();
    }

    [Fact]
    public void HandleCastStarted_KnownShape_AddsToActiveAOEs()
    {
        var t = new TestableTracker();
        t.HandleCastStarted(casterId: 100, castType: 2, effectRange: 5, xAxisModifier: 0,
            omenPath: "", casterHitboxRadius: 0.5f, origin: new Vector2(0, 0), rotation: 0,
            castTimeRemainingSeconds: 3f);
        Assert.Single(t.ActiveAOEs);
        Assert.Equal(100ul, t.ActiveAOEs[0].CasterId);
        Assert.IsType<AOEShapeCircle>(t.ActiveAOEs[0].Shape);
    }

    [Fact]
    public void HandleCastStarted_UnknownShape_DoesNotAdd()
    {
        var t = new TestableTracker();
        t.HandleCastStarted(100, castType: 99, 5, 0, "", 0.5f, new Vector2(0, 0), 0, 3f);
        Assert.Empty(t.ActiveAOEs);
    }

    [Fact]
    public void HandleCastStarted_RaidwideRange_DoesNotAdd()
    {
        var t = new TestableTracker();
        t.HandleCastStarted(100, castType: 2, effectRange: 30, 0, "", 0.5f, new Vector2(0, 0), 0, 3f);
        Assert.Empty(t.ActiveAOEs);
    }

    [Fact]
    public void HandleCastFinished_RemovesEntry()
    {
        var t = new TestableTracker();
        t.HandleCastStarted(100, 2, 5, 0, "", 0.5f, new Vector2(0, 0), 0, 3f);
        t.HandleCastFinished(100);
        Assert.Empty(t.ActiveAOEs);
    }

    [Fact]
    public void PruneStaleEntries_RemovesPastResolveTime()
    {
        var t = new TestableTracker();
        t.HandleCastStarted(100, 2, 5, 0, "", 0.5f, new Vector2(0, 0), 0, castTimeRemainingSeconds: 1f);
        var pruneAt = DateTime.UtcNow.AddSeconds(2);
        t.PruneStaleEntries(pruneAt);
        Assert.Empty(t.ActiveAOEs);
    }

    [Fact]
    public void ClearAll_EmptiesActiveAOEs()
    {
        var t = new TestableTracker();
        t.HandleCastStarted(100, 2, 5, 0, "", 0.5f, new Vector2(0, 0), 0, 3f);
        t.HandleCastStarted(101, 2, 5, 0, "", 0.5f, new Vector2(5, 0), 0, 3f);
        t.ClearAll();
        Assert.Empty(t.ActiveAOEs);
    }
}
