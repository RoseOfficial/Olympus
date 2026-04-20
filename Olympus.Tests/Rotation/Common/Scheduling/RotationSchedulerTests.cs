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

    [Fact]
    public void Dispatch_ProcBuffMissing_SkipsCandidate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.PlayerHasStatus(It.IsAny<uint>())).Returns(false);
        var scheduler = Build(actionService);

        var behavior = TestBehaviors.InstantGcd(actionId: 5001) with { ProcBuff = 9999 };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("ProcBuff"));
    }

    [Fact]
    public void Dispatch_ProcBuffPresent_AdvancesPastProcGate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.PlayerHasStatus(9999)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        var behavior = TestBehaviors.InstantGcd(actionId: 5002) with { ProcBuff = 9999 };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
    }

    [Fact]
    public void Dispatch_ComboStepPredicateFalse_SkipsCandidate()
    {
        var jobGauges = new Mock<IJobGauges>();
        var scheduler = Build(jobGauges: jobGauges);

        var behavior = TestBehaviors.InstantGcd(actionId: 6001) with { ComboStep = _ => false };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("ComboStep"));
    }

    [Fact]
    public void Dispatch_ComboStepThrows_RecordsErrorAndSkips()
    {
        var jobGauges = new Mock<IJobGauges>();
        var errorMetrics = new Mock<Olympus.Services.IErrorMetricsService>();
        var scheduler = new RotationScheduler(
            new Mock<IActionService>().Object,
            jobGauges.Object,
            new Configuration(),
            null,
            errorMetrics.Object);

        var behavior = TestBehaviors.InstantGcd(actionId: 6002) with
        {
            ComboStep = _ => throw new System.InvalidOperationException("bad gauge")
        };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        errorMetrics.Verify(
            x => x.RecordError("Scheduler", It.Is<string>(s => s.Contains("ComboStep"))),
            Times.Once);
    }

    [Fact]
    public void Dispatch_AdjustedProbeMismatch_SkipsCandidate()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.GetAdjustedActionId(100u)).Returns(200u);
        var scheduler = Build(actionService);

        var behavior = TestBehaviors.InstantGcd(actionId: 300) with { AdjustedActionProbe = 100u };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.False(result.Dispatched);
        Assert.Contains(result.GateFailReasons, r => r.Contains("Adjusted"));
    }

    [Fact]
    public void Dispatch_AdjustedProbeMatches_AdvancesPastProbe()
    {
        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.GetAdjustedActionId(100u)).Returns(300u);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        var behavior = TestBehaviors.InstantGcd(actionId: 300) with { AdjustedActionProbe = 100u };
        var ctx = CreateContextWithPlayerLevel(80);
        scheduler.PushGcd(behavior, targetId: 1, priority: 10);

        var result = scheduler.DispatchGcd(ctx);

        Assert.True(result.Dispatched);
    }

    [Fact]
    public void Dispatch_LevelReplacementApplies_UsesUpgradedActionForDispatch()
    {
        var baseAction = new ActionDefinition
        {
            ActionId = 7001,
            Name = "Nebula",
            MinLevel = 38,
            Category = ActionCategory.oGCD,
            TargetType = ActionTargetType.Self,
            CastTime = 0f,
            RecastTime = 120f,
            Range = 0f,
        };
        var upgraded = new ActionDefinition
        {
            ActionId = 7002,
            Name = "GreatNebula",
            MinLevel = 92,
            Category = ActionCategory.oGCD,
            TargetType = ActionTargetType.Self,
            CastTime = 0f,
            RecastTime = 120f,
            Range = 0f,
        };

        var actionService = new Mock<IActionService>();
        actionService.Setup(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == 7002),
            It.IsAny<ulong>())).Returns(true);
        var scheduler = Build(actionService);

        var behavior = new AbilityBehavior
        {
            Action = baseAction,
            LevelReplacements = new[] { ((byte)92, upgraded) },
        };
        var ctx = CreateContextWithPlayerLevel(100);
        scheduler.PushOgcd(behavior, targetId: 0, priority: 10);

        var result = scheduler.DispatchOgcd(ctx);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == 7002),
            It.IsAny<ulong>()), Times.Once);
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
