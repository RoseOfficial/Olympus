using Moq;
using Olympus.Config;
using Olympus.Services.Tank;
using Olympus.Timeline;
using Olympus.Timeline.Models;
using Xunit;

namespace Olympus.Tests.Services.Tank;

public sealed class TankCooldownServiceTimelineTests
{
    private static TankConfig DefaultConfig() => new() { EnableMitigation = true };

    private static Mock<ITimelineService> ActiveTimeline(float confidence = 0.9f)
    {
        var mock = new Mock<ITimelineService>();
        mock.Setup(x => x.IsActive).Returns(true);
        mock.Setup(x => x.Confidence).Returns(confidence);
        return mock;
    }

    // ── ShouldUseMajorCooldown: timeline tank buster (ShouldHoldCooldowns path) ──

    [Fact]
    public void ShouldUseMajorCooldown_TankBusterSoonHighConfidence_ReturnsTrue()
    {
        // Arrange — tank buster IsSoon (<=8s) and IsHighConfidence (>=0.8), so ShouldHoldCooldowns=true
        var timeline = ActiveTimeline();
        timeline.Setup(x => x.NextTankBuster).Returns(new MechanicPrediction(
            secondsUntil: 5f,
            type: TimelineEntryType.TankBuster,
            name: "Megaflare",
            confidence: 0.9f));
        var sut = new TankCooldownService(DefaultConfig(), timeline.Object);

        // Act
        var result = sut.ShouldUseMajorCooldown(hpPercent: 0.95f, incomingDamageRate: 0f);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseMajorCooldown_TankBusterSoonLowConfidence_ReturnsFalse()
    {
        // Arrange — soon but low confidence (0.5), so ShouldHoldCooldowns=false
        var timeline = ActiveTimeline(confidence: 0.5f);
        timeline.Setup(x => x.NextTankBuster).Returns(new MechanicPrediction(
            secondsUntil: 5f,
            type: TimelineEntryType.TankBuster,
            name: "Megaflare",
            confidence: 0.5f));
        timeline.Setup(x => x.IsMechanicImminent(TimelineEntryType.TankBuster, 8f)).Returns(false);
        var sut = new TankCooldownService(DefaultConfig(), timeline.Object);

        // Act
        var result = sut.ShouldUseMajorCooldown(hpPercent: 0.95f, incomingDamageRate: 0f);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldUseMajorCooldown_TankBusterNotSoon_ReturnsFalse()
    {
        // Arrange — high confidence but >8s away, so IsSoon=false, ShouldHoldCooldowns=false
        var timeline = ActiveTimeline();
        timeline.Setup(x => x.NextTankBuster).Returns(new MechanicPrediction(
            secondsUntil: 20f,
            type: TimelineEntryType.TankBuster,
            name: "Megaflare",
            confidence: 0.9f));
        timeline.Setup(x => x.IsMechanicImminent(TimelineEntryType.TankBuster, 8f)).Returns(false);
        var sut = new TankCooldownService(DefaultConfig(), timeline.Object);

        // Act
        var result = sut.ShouldUseMajorCooldown(hpPercent: 0.95f, incomingDamageRate: 0f);

        // Assert
        Assert.False(result);
    }

    // ── ShouldUseMajorCooldown: IsMechanicImminent (wider window fallback) ──

    [Fact]
    public void ShouldUseMajorCooldown_IsMechanicImminentWithin8s_ReturnsTrue()
    {
        // Arrange — NextTankBuster is null but IsMechanicImminent returns true
        var timeline = ActiveTimeline();
        timeline.Setup(x => x.NextTankBuster).Returns((MechanicPrediction?)null);
        timeline.Setup(x => x.IsMechanicImminent(TimelineEntryType.TankBuster, 8f)).Returns(true);
        var sut = new TankCooldownService(DefaultConfig(), timeline.Object);

        // Act
        var result = sut.ShouldUseMajorCooldown(hpPercent: 0.95f, incomingDamageRate: 0f);

        // Assert
        Assert.True(result);
    }

    // ── ShouldUseMajorCooldown: timeline inactive / null ──

    [Fact]
    public void ShouldUseMajorCooldown_TimelineInactive_FallsBackToExistingLogic()
    {
        // Arrange — timeline inactive; existing logic triggers on high damage rate
        var timeline = new Mock<ITimelineService>();
        timeline.Setup(x => x.IsActive).Returns(false);
        var sut = new TankCooldownService(DefaultConfig(), timeline.Object);

        // Act — high damage triggers existing threshold regardless of timeline
        var result = sut.ShouldUseMajorCooldown(hpPercent: 0.95f, incomingDamageRate: 1500f);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseMajorCooldown_NullTimeline_FallsBackToExistingLogic()
    {
        // Arrange — no timeline injected
        var sut = new TankCooldownService(DefaultConfig(), timelineService: null);

        // Act
        var result = sut.ShouldUseMajorCooldown(hpPercent: 0.95f, incomingDamageRate: 1500f);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldUseMajorCooldown_NullTimeline_LowDamage_ReturnsFalse()
    {
        // Arrange — no timeline, low damage, high HP — baseline should not trigger
        var sut = new TankCooldownService(DefaultConfig(), timelineService: null);

        // Act
        var result = sut.ShouldUseMajorCooldown(hpPercent: 0.95f, incomingDamageRate: 50f);

        // Assert
        Assert.False(result);
    }

    // ── Mitigation disabled ──

    [Fact]
    public void ShouldUseMajorCooldown_MitigationDisabled_ReturnsFalse_EvenWithTankBuster()
    {
        var config = new TankConfig { EnableMitigation = false };
        var timeline = ActiveTimeline();
        timeline.Setup(x => x.NextTankBuster).Returns(new MechanicPrediction(
            secondsUntil: 3f,
            type: TimelineEntryType.TankBuster,
            name: "Megaflare",
            confidence: 0.95f));
        var sut = new TankCooldownService(config, timeline.Object);

        var result = sut.ShouldUseMajorCooldown(hpPercent: 0.95f, incomingDamageRate: 0f);

        Assert.False(result);
    }
}
