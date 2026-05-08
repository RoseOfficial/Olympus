using System.Collections.Generic;
using Moq;
using Olympus.Services.Movement;
using Olympus.Services.Movement.Probes;
using Olympus.Tests.Services.Movement.Mocks;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class BossCombatDetectorTests
{
    [Theory]
    [InlineData((byte)1, false)]
    [InlineData((byte)2, false)]
    [InlineData((byte)4, true)]
    [InlineData((byte)6, true)]
    [InlineData((byte)8, false)]
    public void IsBossClass_RankInSet_ReturnsTrue(byte rank, bool expected)
    {
        var bossRanks = new HashSet<byte> { 4, 6 };
        Assert.Equal(expected, BossCombatDetector.IsBossClass(rank, bossRanks));
    }

    [Fact]
    public void Update_NoCombatTargets_IsBossEngagedFalse()
    {
        var probe = new Mock<IBNpcRankProbe>();
        var d = new TestableBossCombatDetector(probe.Object);
        d.Update();
        Assert.False(d.IsBossEngaged);
    }

    [Fact]
    public void Update_BossRankInCombat_IsBossEngagedTrue()
    {
        var probe = new Mock<IBNpcRankProbe>();
        probe.Setup(p => p.GetRank(0xABCD)).Returns((byte)4);
        var d = new TestableBossCombatDetector(probe.Object);
        d.CombatTargets.Add((0xABCD, true, true));
        d.Update();
        Assert.True(d.IsBossEngaged);
    }

    [Fact]
    public void Update_TrashRankInCombat_IsBossEngagedFalse()
    {
        var probe = new Mock<IBNpcRankProbe>();
        probe.Setup(p => p.GetRank(0xABCD)).Returns((byte)2);
        var d = new TestableBossCombatDetector(probe.Object);
        d.CombatTargets.Add((0xABCD, true, true));
        d.Update();
        Assert.False(d.IsBossEngaged);
    }

    [Fact]
    public void Update_BossRankNotInCombat_IsBossEngagedFalse()
    {
        var probe = new Mock<IBNpcRankProbe>();
        probe.Setup(p => p.GetRank(0xABCD)).Returns((byte)4);
        var d = new TestableBossCombatDetector(probe.Object);
        d.CombatTargets.Add((0xABCD, false, false));
        d.Update();
        Assert.False(d.IsBossEngaged);
    }
}
