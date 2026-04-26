using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Consumables;
using Xunit;

namespace Olympus.Tests.Services;

public class TinctureDispatcherTests
{
    private static (TinctureDispatcher sut, Mock<IConsumableService> consumables,
                    Mock<IBurstWindowService> burst, Mock<IActionService> actions) Make()
    {
        var consumables = new Mock<IConsumableService>();
        var burst = new Mock<IBurstWindowService>();
        var actions = new Mock<IActionService>();
        var clientState = new Mock<IClientState>();
        clientState.Setup(x => x.LocalPlayer).Returns((Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter?)null);
        var sut = new TinctureDispatcher(consumables.Object, burst.Object, actions.Object, clientState.Object);
        return (sut, consumables, burst, actions);
    }

    [Fact]
    public void TryDispatch_returns_false_and_does_not_call_ExecuteItem_when_ShouldUseTinctureNow_is_false()
    {
        var (sut, c, b, a) = Make();
        c.Setup(x => x.ShouldUseTinctureNow(It.IsAny<IBurstWindowService>(), It.IsAny<bool>(), It.IsAny<bool>()))
         .Returns(false);

        var result = sut.TryDispatch(JobRegistry.Warrior, inCombat: true, prePullPhase: false);

        Assert.False(result);
        a.Verify(x => x.ExecuteItem(It.IsAny<uint>(), It.IsAny<bool>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryDispatch_returns_false_and_warns_when_inventory_is_empty()
    {
        var (sut, c, b, a) = Make();
        c.Setup(x => x.ShouldUseTinctureNow(It.IsAny<IBurstWindowService>(), It.IsAny<bool>(), It.IsAny<bool>()))
         .Returns(true);
        uint outId; bool outHq;
        c.Setup(x => x.TryGetTinctureForJob(JobRegistry.Warrior, out outId, out outHq))
         .Returns(false);

        var result = sut.TryDispatch(JobRegistry.Warrior, inCombat: true, prePullPhase: false);

        Assert.False(result);
        c.Verify(x => x.OnTinctureSkippedDueToEmptyBag(JobRegistry.Warrior), Times.Once);
        a.Verify(x => x.ExecuteItem(It.IsAny<uint>(), It.IsAny<bool>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryDispatch_calls_ExecuteItem_with_HQ_when_ConsumableService_returns_HQ()
    {
        var (sut, c, b, a) = Make();
        c.Setup(x => x.ShouldUseTinctureNow(It.IsAny<IBurstWindowService>(), true, false)).Returns(true);
        uint id = ConsumableIds.TinctureOfStrength_NQ;
        bool hq = true;
        c.Setup(x => x.TryGetTinctureForJob(JobRegistry.Warrior, out id, out hq)).Returns(true);
        a.Setup(x => x.ExecuteItem(ConsumableIds.TinctureOfStrength_NQ, true, 0ul)).Returns(true);

        var result = sut.TryDispatch(JobRegistry.Warrior, inCombat: true, prePullPhase: false);

        Assert.True(result);
        a.Verify(x => x.ExecuteItem(ConsumableIds.TinctureOfStrength_NQ, true, 0ul), Times.Once);
    }

    [Fact]
    public void TryDispatch_returns_false_when_ExecuteItem_returns_false()
    {
        var (sut, c, b, a) = Make();
        c.Setup(x => x.ShouldUseTinctureNow(It.IsAny<IBurstWindowService>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(true);
        uint id = ConsumableIds.TinctureOfStrength_NQ;
        bool hq = false;
        c.Setup(x => x.TryGetTinctureForJob(JobRegistry.Warrior, out id, out hq)).Returns(true);
        a.Setup(x => x.ExecuteItem(It.IsAny<uint>(), It.IsAny<bool>(), It.IsAny<ulong>())).Returns(false);

        var result = sut.TryDispatch(JobRegistry.Warrior, inCombat: true, prePullPhase: false);

        Assert.False(result);
    }
}
