using Olympus.Config;
using Xunit;

namespace Olympus.Tests.Config;

public class SharedConfigTests
{
    [Fact]
    public void CasterShared_Defaults()
    {
        var c = new CasterSharedConfig();
        Assert.True(c.EnableLucidDreaming);
        Assert.Equal(0.70f, c.LucidDreamingMpThreshold);
    }

    [Fact]
    public void CasterShared_LucidThreshold_ClampedTo01()
    {
        var c = new CasterSharedConfig { LucidDreamingMpThreshold = 1.5f };
        Assert.Equal(1f, c.LucidDreamingMpThreshold);
        c.LucidDreamingMpThreshold = -0.2f;
        Assert.Equal(0f, c.LucidDreamingMpThreshold);
    }

    [Fact]
    public void MeleeShared_Defaults()
    {
        var m = new MeleeSharedConfig();
        Assert.True(m.EnableSecondWind);
        Assert.Equal(0.50f, m.SecondWindHpThreshold);
        Assert.True(m.EnableBloodbath);
        Assert.Equal(0.85f, m.BloodbathHpThreshold);
        Assert.True(m.EnableTrueNorth);
    }

    [Fact]
    public void TankShared_Defaults()
    {
        var t = new TankSharedConfig();
        Assert.True(t.EnableRampart);
        Assert.Equal(0.85f, t.RampartHpThreshold);
    }
}
