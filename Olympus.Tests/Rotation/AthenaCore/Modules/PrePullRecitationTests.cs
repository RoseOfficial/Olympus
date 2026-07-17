using Moq;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

public class PrePullRecitationTests
{
    private static Mock<IActionService> RecitationReadyService()
    {
        var svc = MockBuilders.CreateMockActionService();
        svc.Setup(a => a.IsActionReady(SCHActions.Recitation.ActionId)).Returns(true);
        return svc;
    }

    // 1. Countdown <= 10s, Recitation ready, not already active -> pushed to oGCD queue.
    // HasRecitation reads Player.StatusList (null in tests) so it always returns false,
    // meaning the "already active" guard never blocks here -- the positive test is valid.
    [Fact]
    public void HealingModule_PrePullRecitation_PushesOgcdWhenCountdownAtOrBelow10()
    {
        var svc = RecitationReadyService();
        var context = AthenaTestContext.Create(
            inCombat: false,
            countdownRemaining: 9f,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Recitation);
    }

    // 2. Countdown above 10s -> no push (same setup; only countdown differs).
    [Fact]
    public void HealingModule_PrePullRecitation_NoPushWhenCountdownAbove10()
    {
        var svc = RecitationReadyService();
        var context = AthenaTestContext.Create(
            inCombat: false,
            countdownRemaining: 12f,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Recitation);
    }

    // 3. EnablePrePullActions disabled -> no push (same positive setup; only toggle differs).
    [Fact]
    public void HealingModule_PrePullRecitation_NoPushWhenPrePullDisabled()
    {
        var cfg = AthenaTestContext.CreateDefaultScholarConfiguration();
        cfg.PrePull.EnablePrePullActions = false;
        var svc = RecitationReadyService();
        var context = AthenaTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 9f,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Recitation);
    }
}
