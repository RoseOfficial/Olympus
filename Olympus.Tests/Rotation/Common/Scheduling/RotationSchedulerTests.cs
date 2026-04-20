using Dalamud.Plugin.Services;
using Moq;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Action;
using Xunit;

namespace Olympus.Tests.Rotation.Common.Scheduling;

public class RotationSchedulerTests
{
    private static RotationScheduler Build(
        Mock<IActionService>? actionService = null,
        Mock<IJobGauges>? jobGauges = null,
        Configuration? config = null)
    {
        actionService ??= new Mock<IActionService>();
        jobGauges ??= new Mock<IJobGauges>();
        config ??= new Configuration();
        return new RotationScheduler(actionService.Object, jobGauges.Object, config);
    }

    [Fact]
    public void Dispatch_EmptyQueue_ReturnsNotDispatched()
    {
        var scheduler = Build();
        var ctx = new Mock<IRotationContext>().Object;

        var gcdResult = scheduler.DispatchGcd(ctx);
        var ogcdResult = scheduler.DispatchOgcd(ctx);

        Assert.False(gcdResult.Dispatched);
        Assert.False(ogcdResult.Dispatched);
    }

    [Fact]
    public void Reset_ClearsBothQueues()
    {
        var scheduler = Build();
        var behavior = TestBehaviors.InstantGcd();
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);
        scheduler.PushOgcd(behavior, targetId: 1, priority: 10);

        scheduler.Reset();

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void Push_AddsCandidatesToTheCorrectQueue()
    {
        var scheduler = Build();
        var behavior = TestBehaviors.InstantGcd();

        scheduler.PushGcd(behavior, targetId: 123, priority: 5);
        scheduler.PushOgcd(behavior, targetId: 456, priority: 1);

        Assert.Single(scheduler.InspectGcdQueue());
        Assert.Single(scheduler.InspectOgcdQueue());
        Assert.Equal(123ul, scheduler.InspectGcdQueue()[0].TargetId);
        Assert.Equal(456ul, scheduler.InspectOgcdQueue()[0].TargetId);
    }
}
