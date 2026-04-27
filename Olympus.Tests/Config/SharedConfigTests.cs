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
        Assert.Equal(0.70f, c.LucidDreamingThreshold);
    }

    [Fact]
    public void CasterShared_LucidThreshold_ClampedTo01()
    {
        var c = new CasterSharedConfig { LucidDreamingThreshold = 1.5f };
        Assert.Equal(1f, c.LucidDreamingThreshold);
        c.LucidDreamingThreshold = -0.2f;
        Assert.Equal(0f, c.LucidDreamingThreshold);
    }

    [Fact]
    public void MeleeShared_Defaults()
    {
        var m = new MeleeSharedConfig();
        Assert.True(m.EnableSecondWind);
        Assert.Equal(0.50f, m.SecondWindHpThreshold);
        Assert.True(m.EnableBloodbath);
        Assert.Equal(0.85f, m.BloodbathHpThreshold);
    }

    [Fact]
    public void MeleeShared_SecondWindThreshold_ClampedTo01()
    {
        var m = new MeleeSharedConfig { SecondWindHpThreshold = 1.5f };
        Assert.Equal(1f, m.SecondWindHpThreshold);
        m.SecondWindHpThreshold = -0.2f;
        Assert.Equal(0f, m.SecondWindHpThreshold);
    }

    [Fact]
    public void MeleeShared_BloodbathThreshold_ClampedTo01()
    {
        var m = new MeleeSharedConfig { BloodbathHpThreshold = 1.5f };
        Assert.Equal(1f, m.BloodbathHpThreshold);
        m.BloodbathHpThreshold = -0.2f;
        Assert.Equal(0f, m.BloodbathHpThreshold);
    }

}
