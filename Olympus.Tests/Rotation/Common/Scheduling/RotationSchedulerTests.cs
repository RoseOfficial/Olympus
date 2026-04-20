using Dalamud.Plugin.Services;
using Moq;
using Olympus.Models.Action;
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

    [Fact]
    public void Dispatch_LevelTooLow_SkipsCandidate()
    {
        var actionService = new Mock<IActionService>();
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantGcd(actionId: 3001, minLevel: 90);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("Level"));
    }

    [Fact]
    public void Dispatch_LevelMatches_AdvancesPastLevelGate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);
        var behavior = TestBehaviors.InstantGcd(actionId: 3002, minLevel: 60);

        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
    }

    [Fact]
    public void Dispatch_ToggleReturnsFalse_SkipsCandidate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var config = new Configuration();
        var scheduler = Build(actionService, config: config);

        var behavior = TestBehaviors.InstantGcd(actionId: 4001) with { Toggle = _ => false };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("Toggle"));
    }

    private static IRotationContext CreateContextWithPlayerLevel(byte level)
    {
        var mock = new Mock<IRotationContext>();
        var player = new Mock<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>();
        player.Setup(p => p.Level).Returns(level);
        mock.Setup(c => c.Player).Returns(player.Object);
        mock.Setup(c => c.Configuration).Returns(new Configuration());
        return mock.Object;
    }
}
