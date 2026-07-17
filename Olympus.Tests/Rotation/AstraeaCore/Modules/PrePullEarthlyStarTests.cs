using Moq;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

public class PrePullEarthlyStarTests
{
    private static Mock<IActionService> StarReadyService()
    {
        var svc = MockBuilders.CreateMockActionService();
        svc.Setup(a => a.IsActionReady(ASTActions.EarthlyStar.ActionId)).Returns(true);
        return svc;
    }

    // 1. Countdown <= 5s, star not placed, action ready -> EarthlyStar in oGCD queue.
    [Fact]
    public void HealingModule_PrePullEarthlyStar_PushesGroundOgcdWhenCountdownAtOrBelow5()
    {
        var svc = StarReadyService();
        var context = AstraeaTestContext.Create(
            inCombat: false,
            countdownRemaining: 4f,
            isStarPlaced: false,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == ASTActions.EarthlyStar.ActionId);
    }

    // 2. Star already placed -> no push (same positive setup; only isStarPlaced differs).
    [Fact]
    public void HealingModule_PrePullEarthlyStar_NoPushWhenStarAlreadyPlaced()
    {
        var svc = StarReadyService();
        var context = AstraeaTestContext.Create(
            inCombat: false,
            countdownRemaining: 4f,
            isStarPlaced: true,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == ASTActions.EarthlyStar.ActionId);
    }

    // 3. EnablePrePullActions disabled -> no push (same positive setup; only toggle differs).
    [Fact]
    public void HealingModule_PrePullEarthlyStar_NoPushWhenPrePullDisabled()
    {
        var cfg = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        cfg.PrePull.EnablePrePullActions = false;
        var svc = StarReadyService();
        var context = AstraeaTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 4f,
            isStarPlaced: false,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == ASTActions.EarthlyStar.ActionId);
    }

    // 4. Astrologian.EnableEarthlyStar disabled -> no push (same positive setup; only per-job toggle differs).
    [Fact]
    public void HealingModule_PrePullEarthlyStar_NoPushWhenEarthlyStarToggleOff()
    {
        var cfg = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        cfg.Astrologian.EnableEarthlyStar = false;
        var svc = StarReadyService();
        var context = AstraeaTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 4f,
            isStarPlaced: false,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == ASTActions.EarthlyStar.ActionId);
    }
}
