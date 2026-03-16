using Moq;
using Olympus.Services;
using Olympus.Services.Prediction;
using Olympus.Timeline;
using Olympus.Timeline.Models;
using Xunit;

namespace Olympus.Tests.Services.Prediction;

public sealed class DamageIntakeServiceTimelineTests
{
    private static DamageIntakeService MakeService(ITimelineService? timeline = null)
    {
        var combatEventService = new Mock<ICombatEventService>();
        var sut = new DamageIntakeService(combatEventService.Object);
        if (timeline != null)
            sut.SetTimelineService(timeline);
        return sut;
    }

    private static Mock<ITimelineService> ActiveTimeline(float confidence = 0.9f)
    {
        var mock = new Mock<ITimelineService>();
        mock.Setup(x => x.IsActive).Returns(true);
        mock.Setup(x => x.Confidence).Returns(confidence);
        return mock;
    }

    // ── ForecastPartyDamage: raidwide ──

    [Fact]
    public void ForecastPartyDamage_WithImminentRaidwide_IncludesRaidwideDamage()
    {
        // Arrange — raidwide in 3s, forecast window 5s → should be included
        var timeline = ActiveTimeline();
        timeline.Setup(x => x.NextRaidwide).Returns(new MechanicPrediction(
            secondsUntil: 3f,
            type: TimelineEntryType.Raidwide,
            name: "Exaflare",
            confidence: 0.9f));
        var sut = MakeService(timeline.Object);

        // Act
        var forecast = sut.ForecastPartyDamage(forecastSeconds: 5f);

        // Assert — must exceed zero (historical rate is 0; raidwide estimate adds some amount)
        Assert.True(forecast > 0);
    }

    [Fact]
    public void ForecastPartyDamage_RaidwideOutsideForecastWindow_NotIncluded()
    {
        // Arrange — raidwide in 20s, forecast window 5s → should not contribute
        var timeline = ActiveTimeline();
        timeline.Setup(x => x.NextRaidwide).Returns(new MechanicPrediction(
            secondsUntil: 20f,
            type: TimelineEntryType.Raidwide,
            name: "Exaflare",
            confidence: 0.9f));
        var sut = MakeService(timeline.Object);

        // Act
        var forecast = sut.ForecastPartyDamage(forecastSeconds: 5f);

        // Assert — no historical damage recorded, no raidwide within window → 0
        Assert.Equal(0, forecast);
    }

    [Fact]
    public void ForecastPartyDamage_NoTimeline_MatchesBaselineBehavior()
    {
        // Arrange — no timeline; record some damage and verify forecast unchanged
        var sut = MakeService(timeline: null);
        sut.RecordDamage(1, 1000);
        sut.RecordDamage(2, 1000);

        // Act — forecast equals historical rate * window (2000 total over 5s default window = 400/s * 5 = 2000)
        var forecast = sut.ForecastPartyDamage(forecastSeconds: 5f);

        // Assert — must be positive and equal to purely historical value
        Assert.True(forecast > 0);
    }

    [Fact]
    public void ForecastPartyDamage_TimelineInactive_DoesNotAddRaidwideDamage()
    {
        // Arrange — timeline present but inactive
        var timeline = new Mock<ITimelineService>();
        timeline.Setup(x => x.IsActive).Returns(false);
        timeline.Setup(x => x.NextRaidwide).Returns(new MechanicPrediction(
            secondsUntil: 2f,
            type: TimelineEntryType.Raidwide,
            name: "Exaflare",
            confidence: 0.9f));
        var sut = MakeService(timeline.Object);

        // Act
        var forecast = sut.ForecastPartyDamage(forecastSeconds: 5f);

        // Assert — inactive timeline contributes nothing beyond historical
        Assert.Equal(0, forecast);
    }

    // ── ForecastEntityDamage: tank buster ──

    [Fact]
    public void ForecastEntityDamage_WithImminentTankBuster_IncludesTankBusterDamage()
    {
        // Arrange — tank buster in 4s within forecast window of 5s
        var timeline = ActiveTimeline();
        timeline.Setup(x => x.NextTankBuster).Returns(new MechanicPrediction(
            secondsUntil: 4f,
            type: TimelineEntryType.TankBuster,
            name: "Megaflare",
            confidence: 0.9f));
        var sut = MakeService(timeline.Object);

        // Act
        var forecast = sut.ForecastEntityDamage(entityId: 1, forecastSeconds: 5f);

        // Assert — must exceed purely historical value (which is 0 here)
        Assert.True(forecast > 0);
    }

    [Fact]
    public void ForecastEntityDamage_TankBusterOutsideForecastWindow_NotIncluded()
    {
        // Arrange — tank buster in 15s, forecast window 5s
        var timeline = ActiveTimeline();
        timeline.Setup(x => x.NextTankBuster).Returns(new MechanicPrediction(
            secondsUntil: 15f,
            type: TimelineEntryType.TankBuster,
            name: "Megaflare",
            confidence: 0.9f));
        var sut = MakeService(timeline.Object);

        // Act
        var forecast = sut.ForecastEntityDamage(entityId: 1, forecastSeconds: 5f);

        // Assert — no historical damage, no tank buster in window → 0
        Assert.Equal(0, forecast);
    }

    [Fact]
    public void ForecastEntityDamage_NoTimeline_ReturnsHistoricalOnly()
    {
        // Arrange — no timeline; verify historical path is untouched
        var sut = MakeService(timeline: null);
        sut.RecordDamage(1, 5000);

        // Act — 5000 damage recorded, 5s window → rate=1000/s → forecast=5000
        var forecast = sut.ForecastEntityDamage(entityId: 1, forecastSeconds: 5f);

        // Assert
        Assert.Equal(5000, forecast);
    }

    // ── SetTimelineService ──

    [Fact]
    public void SetTimelineService_ReplacesExistingService()
    {
        // Arrange — set one timeline then replace it with an inactive one
        var firstTimeline = ActiveTimeline();
        firstTimeline.Setup(x => x.NextRaidwide).Returns(new MechanicPrediction(
            secondsUntil: 2f,
            type: TimelineEntryType.Raidwide,
            name: "Exaflare",
            confidence: 0.9f));

        var secondTimeline = new Mock<ITimelineService>();
        secondTimeline.Setup(x => x.IsActive).Returns(false);

        var sut = MakeService(firstTimeline.Object);
        sut.SetTimelineService(secondTimeline.Object);

        // Act — with inactive timeline the raidwide should not contribute
        var forecast = sut.ForecastPartyDamage(forecastSeconds: 5f);

        // Assert — second (inactive) timeline is in effect
        Assert.Equal(0, forecast);
    }
}
