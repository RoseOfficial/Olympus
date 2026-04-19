using Olympus.Config;
using Xunit;

namespace Olympus.Tests.Config;

public class TimelineConfigTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var cfg = new TimelineConfig();
        Assert.True(cfg.EnableTimelinePredictions);
        Assert.Equal(0.8f, cfg.TimelineConfidenceThreshold);
        Assert.True(cfg.EnableMechanicAwareCasting);
    }

    [Theory]
    [InlineData(0.3f, 0.5f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(0.75f, 0.75f)]
    [InlineData(1.0f, 1.0f)]
    [InlineData(1.5f, 1.0f)]
    public void ConfidenceThreshold_Clamps(float input, float expected)
    {
        var cfg = new TimelineConfig { TimelineConfidenceThreshold = input };
        Assert.Equal(expected, cfg.TimelineConfidenceThreshold);
    }
}
