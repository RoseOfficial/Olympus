using Dalamud.Game.Text;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Services;
using Olympus.Services.Consumables;
using Olympus.Services.Content;
using Olympus.Services.Pull;
using Xunit;

namespace Olympus.Tests.Services;

public class ConsumableServiceTests
{
    private static (ConsumableService sut, Mock<IBurstWindowService> burst,
                    Mock<IPullIntentService> intent, ConsumablesConfig config,
                    Mock<IHighEndContentService> highEnd, Mock<IInventoryProbe> bag,
                    Mock<ITinctureCooldownProbe> cd) Make()
    {
        var burst = new Mock<IBurstWindowService>();
        var intent = new Mock<IPullIntentService>();
        var highEnd = new Mock<IHighEndContentService>();
        var bag = new Mock<IInventoryProbe>();
        var cd = new Mock<ITinctureCooldownProbe>();
        var config = new ConsumablesConfig { EnableAutoTincture = true };
        var sut = new ConsumableService(config, intent.Object, highEnd.Object,
                                         bag.Object, cd.Object, chatGui: null);
        return (sut, burst, intent, config, highEnd, bag, cd);
    }

    [Fact]
    public void TryGetTinctureForJob_returns_HQ_when_HQ_is_in_inventory()
    {
        var (sut, _, _, _, _, bag, _) = Make();
        bag.Setup(b => b.GetItemCount(ConsumableIds.TinctureOfStrength_NQ + ConsumableIds.HqOffset))
           .Returns(2u);
        bag.Setup(b => b.GetItemCount(ConsumableIds.TinctureOfStrength_NQ)).Returns(1u);

        var ok = sut.TryGetTinctureForJob(JobRegistry.Warrior, out var id, out var isHq);

        Assert.True(ok);
        Assert.Equal(ConsumableIds.TinctureOfStrength_NQ, id);
        Assert.True(isHq);
    }

    [Fact]
    public void TryGetTinctureForJob_falls_back_to_NQ_when_HQ_is_empty()
    {
        var (sut, _, _, _, _, bag, _) = Make();
        bag.Setup(b => b.GetItemCount(ConsumableIds.TinctureOfStrength_NQ + ConsumableIds.HqOffset))
           .Returns(0u);
        bag.Setup(b => b.GetItemCount(ConsumableIds.TinctureOfStrength_NQ)).Returns(3u);

        var ok = sut.TryGetTinctureForJob(JobRegistry.Warrior, out var id, out var isHq);

        Assert.True(ok);
        Assert.Equal(ConsumableIds.TinctureOfStrength_NQ, id);
        Assert.False(isHq);
    }

    [Fact]
    public void TryGetTinctureForJob_returns_false_when_bag_is_empty()
    {
        var (sut, _, _, _, _, bag, _) = Make();
        bag.Setup(b => b.GetItemCount(It.IsAny<uint>())).Returns(0u);

        var ok = sut.TryGetTinctureForJob(JobRegistry.Warrior, out _, out _);

        Assert.False(ok);
    }

    [Theory]
    [InlineData(JobRegistry.Paladin, ConsumableIds.TinctureOfStrength_NQ)]
    [InlineData(JobRegistry.Ninja, ConsumableIds.TinctureOfDexterity_NQ)]
    [InlineData(JobRegistry.BlackMage, ConsumableIds.TinctureOfIntelligence_NQ)]
    [InlineData(JobRegistry.WhiteMage, ConsumableIds.TinctureOfMind_NQ)]
    [InlineData(JobRegistry.Sage, ConsumableIds.TinctureOfMind_NQ)]
    public void TryGetTinctureForJob_picks_correct_stat(uint jobId, uint expectedNqId)
    {
        var (sut, _, _, _, _, bag, _) = Make();
        bag.Setup(b => b.GetItemCount(expectedNqId)).Returns(1u);

        var ok = sut.TryGetTinctureForJob(jobId, out var id, out var isHq);

        Assert.True(ok);
        Assert.Equal(expectedNqId, id);
        Assert.False(isHq);
    }

    [Fact]
    public void ShouldUseTinctureNow_returns_false_when_master_toggle_is_off()
    {
        var (sut, burst, intent, config, highEnd, bag, cd) = Make();
        config.EnableAutoTincture = false;
        highEnd.Setup(h => h.IsHighEndZone).Returns(true);
        burst.Setup(b => b.IsInBurstWindow).Returns(true);
        bag.Setup(b => b.GetItemCount(It.IsAny<uint>())).Returns(1u);

        Assert.False(sut.ShouldUseTinctureNow(burst.Object, inCombat: true, prePullPhase: false));
    }

    [Fact]
    public void ShouldUseTinctureNow_returns_false_when_zone_is_not_high_end()
    {
        var (sut, burst, intent, _, highEnd, bag, cd) = Make();
        highEnd.Setup(h => h.IsHighEndZone).Returns(false);
        burst.Setup(b => b.IsInBurstWindow).Returns(true);
        bag.Setup(b => b.GetItemCount(It.IsAny<uint>())).Returns(1u);

        Assert.False(sut.ShouldUseTinctureNow(burst.Object, inCombat: true, prePullPhase: false));
    }

    [Fact]
    public void ShouldUseTinctureNow_returns_false_when_burst_not_active_or_imminent()
    {
        var (sut, burst, intent, _, highEnd, bag, cd) = Make();
        highEnd.Setup(h => h.IsHighEndZone).Returns(true);
        burst.Setup(b => b.IsInBurstWindow).Returns(false);
        burst.Setup(b => b.IsBurstImminent(It.IsAny<float>())).Returns(false);
        bag.Setup(b => b.GetItemCount(It.IsAny<uint>())).Returns(1u);

        Assert.False(sut.ShouldUseTinctureNow(burst.Object, inCombat: true, prePullPhase: false));
    }

    [Fact]
    public void ShouldUseTinctureNow_PrePull_requires_PullIntent_not_None()
    {
        var (sut, burst, intent, _, highEnd, bag, cd) = Make();
        highEnd.Setup(h => h.IsHighEndZone).Returns(true);
        burst.Setup(b => b.IsInBurstWindow).Returns(true);
        bag.Setup(b => b.GetItemCount(It.IsAny<uint>())).Returns(1u);
        intent.Setup(i => i.Current).Returns(PullIntent.None);

        Assert.False(sut.ShouldUseTinctureNow(burst.Object, inCombat: false, prePullPhase: true));

        intent.Setup(i => i.Current).Returns(PullIntent.Imminent);
        Assert.True(sut.ShouldUseTinctureNow(burst.Object, inCombat: false, prePullPhase: true));
    }

    [Fact]
    public void ShouldUseTinctureNow_InCombat_requires_inCombat_true()
    {
        var (sut, burst, intent, _, highEnd, bag, cd) = Make();
        highEnd.Setup(h => h.IsHighEndZone).Returns(true);
        burst.Setup(b => b.IsInBurstWindow).Returns(true);
        bag.Setup(b => b.GetItemCount(It.IsAny<uint>())).Returns(1u);

        Assert.False(sut.ShouldUseTinctureNow(burst.Object, inCombat: false, prePullPhase: false));
        Assert.True(sut.ShouldUseTinctureNow(burst.Object, inCombat: true, prePullPhase: false));
    }

    [Fact]
    public void ShouldUseTinctureNow_returns_false_when_tincture_on_cooldown()
    {
        var (sut, burst, intent, _, highEnd, bag, cd) = Make();
        highEnd.Setup(h => h.IsHighEndZone).Returns(true);
        burst.Setup(b => b.IsInBurstWindow).Returns(true);
        bag.Setup(b => b.GetItemCount(It.IsAny<uint>())).Returns(1u);
        cd.Setup(c => c.GetTinctureCooldownRemaining()).Returns(120f);

        Assert.False(sut.ShouldUseTinctureNow(burst.Object, inCombat: true, prePullPhase: false));
    }
}
