using Moq;
using Olympus;
using Olympus.Config;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Timeline;
using Olympus.Timeline.Models;
using Xunit;

namespace Olympus.Tests.Rotation.Common.Helpers;

public class MechanicCastGateTests
{
    private static Mock<IRotationContext> BuildContext(
        bool timelineActive = true,
        float confidence = 0.95f,
        MechanicPrediction? raidwide = null,
        MechanicPrediction? tankbuster = null,
        bool enableGate = true,
        bool enablePredictions = true,
        float threshold = 0.8f,
        bool timelineNull = false)
    {
        var config = new Configuration();
        config.Timeline.EnableMechanicAwareCasting = enableGate;
        config.Timeline.EnableTimelinePredictions = enablePredictions;
        config.Timeline.TimelineConfidenceThreshold = threshold;

        var ctx = new Mock<IRotationContext>();
        ctx.SetupGet(c => c.Configuration).Returns(config);

        if (timelineNull)
        {
            ctx.SetupGet(c => c.TimelineService).Returns((ITimelineService?)null);
        }
        else
        {
            var timeline = new Mock<ITimelineService>();
            timeline.SetupGet(t => t.IsActive).Returns(timelineActive);
            timeline.SetupGet(t => t.Confidence).Returns(confidence);
            timeline.SetupGet(t => t.NextRaidwide).Returns(raidwide);
            timeline.SetupGet(t => t.NextTankBuster).Returns(tankbuster);
            ctx.SetupGet(c => c.TimelineService).Returns(timeline.Object);
        }

        return ctx;
    }

    [Fact]
    public void InstantCast_ReturnsFalse()
    {
        var ctx = BuildContext(raidwide: new MechanicPrediction { SecondsUntil = 0.1f });
        Assert.False(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 0f));
    }

    [Fact]
    public void GateDisabled_ReturnsFalse()
    {
        var ctx = BuildContext(enableGate: false, raidwide: new MechanicPrediction { SecondsUntil = 1f });
        Assert.False(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void TimelineNull_ReturnsFalse()
    {
        var ctx = BuildContext(timelineNull: true);
        Assert.False(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void TimelineInactive_ReturnsFalse()
    {
        var ctx = BuildContext(timelineActive: false, raidwide: new MechanicPrediction { SecondsUntil = 1f });
        Assert.False(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void ConfidenceBelowThreshold_ReturnsFalse()
    {
        var ctx = BuildContext(confidence: 0.5f, threshold: 0.8f, raidwide: new MechanicPrediction { SecondsUntil = 1f });
        Assert.False(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void RaidwideWithinDeadline_ReturnsTrue()
    {
        var ctx = BuildContext(raidwide: new MechanicPrediction { SecondsUntil = 2.0f });
        Assert.True(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void RaidwideOutsideDeadline_ReturnsFalse()
    {
        var ctx = BuildContext(raidwide: new MechanicPrediction { SecondsUntil = 5.0f });
        Assert.False(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void RaidwideAlreadyPassed_ReturnsFalse()
    {
        var ctx = BuildContext(raidwide: new MechanicPrediction { SecondsUntil = -0.5f });
        Assert.False(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void TankbusterWithinDeadline_ReturnsTrue()
    {
        var ctx = BuildContext(tankbuster: new MechanicPrediction { SecondsUntil = 1.5f });
        Assert.True(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.0f));
    }

    [Fact]
    public void TankbusterOutsideDeadline_ReturnsFalse()
    {
        var ctx = BuildContext(tankbuster: new MechanicPrediction { SecondsUntil = 5.0f });
        Assert.False(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void BothMechanicsPresent_EitherTriggersBlock()
    {
        var ctx = BuildContext(
            raidwide: new MechanicPrediction { SecondsUntil = 10f },
            tankbuster: new MechanicPrediction { SecondsUntil = 1f });
        Assert.True(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void NeitherMechanicPresent_ReturnsFalse()
    {
        var ctx = BuildContext(raidwide: null, tankbuster: null);
        Assert.False(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void PredictionsDisabled_ReturnsFalse()
    {
        var ctx = BuildContext(enablePredictions: false, raidwide: new MechanicPrediction { SecondsUntil = 1f });
        Assert.False(MechanicCastGate.ShouldBlock(ctx.Object, castTime: 2.5f));
    }

    [Fact]
    public void FormatBlockedState_RaidwideCloser_ShowsRaidwide()
    {
        var ctx = BuildContext(
            raidwide: new MechanicPrediction { SecondsUntil = 1.5f },
            tankbuster: new MechanicPrediction { SecondsUntil = 3.0f });
        var state = MechanicCastGate.FormatBlockedState(ctx.Object);
        Assert.StartsWith("Held cast (raidwide", state);
    }

    [Fact]
    public void FormatBlockedState_TankbusterOnly_ShowsTankbuster()
    {
        var ctx = BuildContext(
            raidwide: null,
            tankbuster: new MechanicPrediction { SecondsUntil = 2.0f });
        var state = MechanicCastGate.FormatBlockedState(ctx.Object);
        Assert.StartsWith("Held cast (tank buster", state);
    }

    [Fact]
    public void FormatBlockedState_NullTimeline_ReturnsFallback()
    {
        var ctx = BuildContext(timelineNull: true);
        var state = MechanicCastGate.FormatBlockedState(ctx.Object);
        Assert.Equal("Held cast (mechanic)", state);
    }
}
