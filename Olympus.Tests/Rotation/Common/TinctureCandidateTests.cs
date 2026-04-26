using Moq;
using Olympus.Data;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Consumables;
using Xunit;

namespace Olympus.Tests.Rotation.Common;

public class TinctureCandidateTests
{
    [Fact]
    public void TryDispatch_passes_prePullPhase_true_to_dispatcher()
    {
        var dispatcher = new Mock<ITinctureDispatcher>();
        dispatcher.Setup(d => d.TryDispatch(It.IsAny<uint>(), It.IsAny<bool>(), true)).Returns(true);

        var ctx = new Mock<IRotationContext>();
        ctx.SetupGet(c => c.InCombat).Returns(false);

        var sut = new TinctureCandidate(dispatcher.Object);
        var result = sut.TryDispatch(JobRegistry.Warrior, ctx.Object);

        Assert.True(result);
        dispatcher.Verify(d => d.TryDispatch(JobRegistry.Warrior, false, true), Times.Once);
    }

    [Fact]
    public void TryDispatch_returns_false_when_dispatcher_returns_false()
    {
        var dispatcher = new Mock<ITinctureDispatcher>();
        dispatcher.Setup(d => d.TryDispatch(It.IsAny<uint>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(false);

        var ctx = new Mock<IRotationContext>();
        ctx.SetupGet(c => c.InCombat).Returns(false);

        var sut = new TinctureCandidate(dispatcher.Object);
        var result = sut.TryDispatch(JobRegistry.Warrior, ctx.Object);

        Assert.False(result);
    }
}
