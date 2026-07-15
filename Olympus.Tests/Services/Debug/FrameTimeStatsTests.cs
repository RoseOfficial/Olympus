using Olympus.Services.Debug;
using Xunit;

namespace Olympus.Tests.Services.Debug;

public class FrameTimeStatsTests
{
    [Fact]
    public void Empty_ReturnsZeros()
    {
        var s = new FrameTimeStats();
        Assert.Equal(0, s.LastMs);
        Assert.Equal(0, s.P95Ms);
    }

    [Fact]
    public void Record_TracksLastAndP95()
    {
        var s = new FrameTimeStats();
        for (var i = 1; i <= 100; i++) s.Record(i); // 1..100 ms
        Assert.Equal(100, s.LastMs);
        Assert.True(s.P95Ms >= 94 && s.P95Ms <= 96, $"P95 was {s.P95Ms}");
    }

    [Fact]
    public void Record_WrapsRingBuffer()
    {
        var s = new FrameTimeStats();
        for (var i = 0; i < 500; i++) s.Record(2.0);
        s.Record(50.0);
        Assert.Equal(50.0, s.LastMs);
        Assert.Equal(2.0, s.P95Ms, 1); // one outlier in 120 samples sits above P95
    }
}
