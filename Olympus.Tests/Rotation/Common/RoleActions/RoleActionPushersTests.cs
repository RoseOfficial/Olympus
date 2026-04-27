using Moq;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.RoleActions;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;
using OlympusData = Olympus.Data;

namespace Olympus.Tests.Rotation.Common.RoleActions;

public class RoleActionPushersLucidTests
{
    private static AbilityBehavior LucidBehavior() => new()
    {
        Action = OlympusData.RoleActions.LucidDreaming,
        Toggle = _ => true,
    };

    private static (Mock<IRotationContext> ctx, Mock<Olympus.Services.Action.IActionService> actionService) BuildContext(
        byte playerLevel,
        uint currentMp,
        uint maxMp,
        bool actionReady = true)
    {
        var player = MockBuilders.CreateMockPlayerCharacter(
            level: playerLevel,
            currentMp: currentMp,
            maxMp: maxMp);
        player.SetupGet(p => p.GameObjectId).Returns(123ul);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(a => a.IsActionReady(OlympusData.RoleActions.LucidDreaming.ActionId)).Returns(actionReady);

        var ctx = new Mock<IRotationContext>();
        ctx.SetupGet(c => c.Player).Returns(player.Object);
        ctx.SetupGet(c => c.ActionService).Returns(actionService.Object);

        return (ctx, actionService);
    }

    [Fact]
    public void Skips_When_Level_Too_Low()
    {
        var (ctx, _) = BuildContext(
            playerLevel: (byte)(OlympusData.RoleActions.LucidDreaming.MinLevel - 1),
            currentMp: 5_000,
            maxMp: 10_000);
        var scheduler = SchedulerFactory.CreateForTest();

        RoleActionPushers.TryPushLucidDreaming(ctx.Object, scheduler, LucidBehavior(), 0.70f, 100);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void Skips_When_OnCooldown()
    {
        var (ctx, _) = BuildContext(playerLevel: 90, currentMp: 5_000, maxMp: 10_000, actionReady: false);
        var scheduler = SchedulerFactory.CreateForTest();

        RoleActionPushers.TryPushLucidDreaming(ctx.Object, scheduler, LucidBehavior(), 0.70f, 100);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void Skips_When_Mp_Above_Threshold()
    {
        // 85% MP, threshold 70% -- should not push
        var (ctx, _) = BuildContext(playerLevel: 90, currentMp: 8_500, maxMp: 10_000);
        var scheduler = SchedulerFactory.CreateForTest();

        RoleActionPushers.TryPushLucidDreaming(ctx.Object, scheduler, LucidBehavior(), 0.70f, 100);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void Pushes_When_All_Gates_Pass()
    {
        // 50% MP, threshold 70% -- should push
        var (ctx, _) = BuildContext(playerLevel: 90, currentMp: 5_000, maxMp: 10_000);
        var scheduler = SchedulerFactory.CreateForTest();

        RoleActionPushers.TryPushLucidDreaming(ctx.Object, scheduler, LucidBehavior(), 0.70f, 100);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Single(queue);
        Assert.Equal(100, queue[0].Priority);
        Assert.Equal(OlympusData.RoleActions.LucidDreaming.ActionId, queue[0].Behavior.Action.ActionId);
    }

    [Fact]
    public void Invokes_OnDispatched_Callback_Through_Scheduler_Queue()
    {
        var (ctx, _) = BuildContext(playerLevel: 90, currentMp: 5_000, maxMp: 10_000);
        var scheduler = SchedulerFactory.CreateForTest();
        var dispatched = false;

        RoleActionPushers.TryPushLucidDreaming(ctx.Object, scheduler, LucidBehavior(), 0.70f, 100,
            onDispatched: _ => dispatched = true);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Single(queue);
        // Invoke the callback directly to verify it was wired through correctly
        queue[0].OnDispatched?.Invoke(ctx.Object);
        Assert.True(dispatched);
    }
}
