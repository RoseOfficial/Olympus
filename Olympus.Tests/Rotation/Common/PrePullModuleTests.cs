using Moq;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Pull;
using Xunit;

namespace Olympus.Tests.Rotation.Common;

public class PrePullModuleTests
{
    private static (PrePullModule sut, Mock<IPullIntentService> intent,
                    Mock<IRotationContext> ctx) Make()
    {
        var intent = new Mock<IPullIntentService>();
        var ctx = new Mock<IRotationContext>();
        var sut = new PrePullModule(intent.Object);
        return (sut, intent, ctx);
    }

    [Fact]
    public void TryDispatch_returns_false_when_intent_is_None_and_skips_all_candidates()
    {
        var (sut, intent, ctx) = Make();
        intent.Setup(i => i.Current).Returns(PullIntent.None);

        var c = new Mock<IPrePullCandidate>();
        c.Setup(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>())).Returns(true);
        sut.Register(c.Object);

        var result = sut.TryDispatch(Olympus.Data.JobRegistry.Warrior, ctx.Object);

        Assert.False(result);
        c.Verify(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>()), Times.Never);
    }

    [Fact]
    public void TryDispatch_returns_true_when_a_candidate_dispatches()
    {
        var (sut, intent, ctx) = Make();
        intent.Setup(i => i.Current).Returns(PullIntent.Imminent);

        var c = new Mock<IPrePullCandidate>();
        c.Setup(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>())).Returns(true);
        sut.Register(c.Object);

        var result = sut.TryDispatch(Olympus.Data.JobRegistry.Warrior, ctx.Object);

        Assert.True(result);
    }

    [Fact]
    public void TryDispatch_stops_at_first_candidate_that_dispatches()
    {
        var (sut, intent, ctx) = Make();
        intent.Setup(i => i.Current).Returns(PullIntent.Active);

        var first = new Mock<IPrePullCandidate>();
        first.Setup(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>())).Returns(true);
        var second = new Mock<IPrePullCandidate>();
        second.Setup(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>())).Returns(true);
        sut.Register(first.Object);
        sut.Register(second.Object);

        var result = sut.TryDispatch(Olympus.Data.JobRegistry.Warrior, ctx.Object);

        Assert.True(result);
        first.Verify(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>()), Times.Once);
        second.Verify(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>()), Times.Never);
    }

    [Fact]
    public void TryDispatch_continues_to_next_candidate_when_one_returns_false()
    {
        var (sut, intent, ctx) = Make();
        intent.Setup(i => i.Current).Returns(PullIntent.Imminent);

        var first = new Mock<IPrePullCandidate>();
        first.Setup(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>())).Returns(false);
        var second = new Mock<IPrePullCandidate>();
        second.Setup(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>())).Returns(true);
        sut.Register(first.Object);
        sut.Register(second.Object);

        var result = sut.TryDispatch(Olympus.Data.JobRegistry.Warrior, ctx.Object);

        Assert.True(result);
        first.Verify(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>()), Times.Once);
        second.Verify(x => x.TryDispatch(It.IsAny<uint>(), It.IsAny<IRotationContext>()), Times.Once);
    }
}
