using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.HephaestusCore.Modules;


public class BuffModuleCollectCandidatesTests
{
    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void CollectCandidates_NoMercy_PushedAtPriority1_WhenReadyAndInBurstWindow()
    {
        // Burst window active (IsInBurstWindow=true) so ShouldHoldForBurst returns false
        var burstService = new Mock<IBurstWindowService>();
        burstService.Setup(x => x.IsInBurstWindow).Returns(true);
        burstService.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(GNBActions.NoMercy.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(GNBActions.Bloodfest.ActionId)).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            inCombat: true,
            hasNoMercy: false,
            cartridges: 2,
            actionService: actionService);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Single(queue);
        Assert.Equal(GnbAbilities.NoMercy, queue[0].Behavior);
        Assert.Equal(1, queue[0].Priority);
    }

    [Fact]
    public void CollectCandidates_NoMercy_NotPushed_WhenShouldHoldForBurst()
    {
        // Imminent but not yet active: ShouldHoldForBurst returns true -> should not push.
        // cartridges=2 also disables Bloodfest (maxBenefit < 2), so only NoMercy runs.
        var burstService = new Mock<IBurstWindowService>();
        burstService.Setup(x => x.IsInBurstWindow).Returns(false);
        burstService.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(GNBActions.NoMercy.ActionId)).Returns(true);
        // Bloodfest also blocked so debug state is stable after NoMercy sets it
        actionService.Setup(x => x.IsActionReady(GNBActions.Bloodfest.ActionId)).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // cartridges=2: Bloodfest maxBenefit=1 < 2 so it sets a different debug string.
        // Use cartridges=0: Bloodfest maxBenefit=3 passes cap check, but NoMercy-hold gate
        // fires first; then Bloodfest IsActionReady returns false so it doesn't set any state.
        var context = CreateContext(
            inCombat: true,
            hasNoMercy: false,
            cartridges: 2,
            actionService: actionService);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        // Nothing pushed to the queue
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void CollectCandidates_Bloodfest_PushedAtPriority2_WhenLowCartridgesAndNoMercyActive()
    {
        // HasNoMercy=true, cartridges=0 -> maxBenefit=3 >= 2, NM hold bypass skipped
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(GNBActions.NoMercy.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(GNBActions.Bloodfest.ActionId)).Returns(true);
        actionService.Setup(x => x.GetCooldownRemaining(GNBActions.NoMercy.ActionId)).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            inCombat: true,
            hasNoMercy: true,
            cartridges: 0,
            actionService: actionService);

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Single(queue);
        Assert.Equal(GnbAbilities.Bloodfest, queue[0].Behavior);
        Assert.Equal(2, queue[0].Priority);
    }

    [Fact]
    public void CollectCandidates_EnableNoMercyFalse_PushesNothing()
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableNoMercy = false;
        config.Tank.EnableBloodfest = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            inCombat: true,
            hasNoMercy: false,
            cartridges: 2,
            config: config,
            actionService: actionService);

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    #region Helpers

    private static IHephaestusContext CreateContext(
        bool inCombat,
        bool hasNoMercy = false,
        float noMercyRemaining = 0f,
        int cartridges = 0,
        byte level = 100,
        Configuration? config = null,
        Mock<IActionService>? actionService = null)
    {
        config ??= HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.GetCooldownRemaining(GNBActions.NoMercy.ActionId)).Returns(20f);

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IHephaestusContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(false);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.HasNoMercy).Returns(hasNoMercy);
        mock.Setup(x => x.NoMercyRemaining).Returns(noMercyRemaining);
        mock.Setup(x => x.Cartridges).Returns(cartridges);
        mock.Setup(x => x.Debug).Returns(new HephaestusDebugState());

        return mock.Object;
    }

    #endregion
}
