using System;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

/// <summary>
/// Burst-alignment tests for WHM BuffModule: PoM and Assize pooling.
/// </summary>
public class BuffModuleBurstTests : IDisposable
{
    public void Dispose() => BurstHoldHelper.ModifierKeys = null;

    private static Mock<IBurstWindowService> MakeBurst(bool isInBurst, bool isImminent)
    {
        var svc = new Mock<IBurstWindowService>();
        svc.Setup(b => b.IsInBurstWindow).Returns(isInBurst);
        svc.Setup(b => b.IsBurstImminent(It.IsAny<float>())).Returns(isImminent);
        svc.Setup(b => b.SecondsUntilNextBurst).Returns(5f);
        return svc;
    }

    private static (IApolloContext context, Mock<IActionService> actionService) MakePomContext(
        bool enableBurstPooling = true)
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Buffs.EnablePresenceOfMind = true;
        config.Buffs.DelayPoMForRaise = false;
        config.Buffs.StackPoMWithAssize = false;
        config.HealerShared.EnableBurstPooling = enableBurstPooling;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.PresenceOfMind.ActionId)).Returns(true);

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        return (context, actionService);
    }

    private static (IApolloContext context, Mock<IActionService> actionService) MakeAssizeContext(
        bool enableBurstPooling = true)
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.EnableAssize = true;
        config.HealerShared.EnableBurstPooling = enableBurstPooling;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WHMActions.Assize.ActionId)).Returns(true);

        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        return (context, actionService);
    }

    // -----------------------------------------------------------------------
    // PoM: burst imminent -> not pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void PoM_BurstImminent_Held()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new BuffModule(burstSvc.Object);
        var (context, actionService) = MakePomContext();
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.PresenceOfMind.ActionId);
    }

    // -----------------------------------------------------------------------
    // PoM: in burst -> pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void PoM_InBurst_Fires()
    {
        var burstSvc = MakeBurst(isInBurst: true, isImminent: false);
        var module = new BuffModule(burstSvc.Object);
        var (context, actionService) = MakePomContext();
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.PresenceOfMind.ActionId);
    }

    // -----------------------------------------------------------------------
    // PoM: burst imminent BUT pooling disabled -> fires immediately
    // -----------------------------------------------------------------------
    [Fact]
    public void PoM_BurstImminent_EnableBurstPoolingFalse_Fires()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new BuffModule(burstSvc.Object);
        var (context, actionService) = MakePomContext(enableBurstPooling: false);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.PresenceOfMind.ActionId);
    }

    // -----------------------------------------------------------------------
    // PoM: no burst service -> fires (backward compat with null service)
    // -----------------------------------------------------------------------
    [Fact]
    public void PoM_NoBurstService_Fires()
    {
        var module = new BuffModule(); // no burst service
        var (context, actionService) = MakePomContext();
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.PresenceOfMind.ActionId);
    }

    // -----------------------------------------------------------------------
    // Assize: burst imminent -> not pushed (10s threshold)
    // -----------------------------------------------------------------------
    [Fact]
    public void Assize_BurstImminent_Held()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new BuffModule(burstSvc.Object);
        var (context, actionService) = MakeAssizeContext();
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.Assize.ActionId);
    }

    // -----------------------------------------------------------------------
    // Assize: in burst -> pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void Assize_InBurst_Fires()
    {
        var burstSvc = MakeBurst(isInBurst: true, isImminent: false);
        var module = new BuffModule(burstSvc.Object);
        var (context, actionService) = MakeAssizeContext();
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.Assize.ActionId);
    }

    // -----------------------------------------------------------------------
    // Assize: burst imminent BUT pooling disabled -> fires
    // -----------------------------------------------------------------------
    [Fact]
    public void Assize_BurstImminent_BurstPoolingDisabled_Fires()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new BuffModule(burstSvc.Object);
        var (context, actionService) = MakeAssizeContext(enableBurstPooling: false);
        var scheduler = SchedulerFactory.CreateForTest(actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == WHMActions.Assize.ActionId);
    }
}
