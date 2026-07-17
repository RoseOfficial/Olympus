using System.Linq;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Tests.Rotation.AstraeaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

public class PrePullDrawTests
{
    // 1. No card held, countdown <= 5s -> AstralDraw or UmbralDraw in oGCD queue.
    [Fact]
    public void CardModule_PrePullDraw_PushesDrawWhenCountdownAtOrBelow5AndNoCardHeld()
    {
        var context = AstraeaTestContext.Create(
            inCombat: false,
            countdownRemaining: 4f,
            hasCard: false);
        var scheduler = SchedulerFactory.CreateForTest();

        new CardModule().CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.True(
            queue.Any(c => c.Behavior == AstraeaAbilities.AstralDraw) ||
            queue.Any(c => c.Behavior == AstraeaAbilities.UmbralDraw),
            "Expected AstralDraw or UmbralDraw in oGCD queue");
    }

    // 2. Card already held -> no draw push (same positive setup; only hasCard differs).
    [Fact]
    public void CardModule_PrePullDraw_NoPushWhenCardAlreadyHeld()
    {
        var context = AstraeaTestContext.Create(
            inCombat: false,
            countdownRemaining: 4f,
            hasCard: true);
        var scheduler = SchedulerFactory.CreateForTest();

        new CardModule().CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.False(
            queue.Any(c => c.Behavior == AstraeaAbilities.AstralDraw) ||
            queue.Any(c => c.Behavior == AstraeaAbilities.UmbralDraw));
    }

    // 3. EnablePrePullActions disabled -> no push (same positive setup; only toggle differs).
    [Fact]
    public void CardModule_PrePullDraw_NoPushWhenPrePullDisabled()
    {
        var cfg = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        cfg.PrePull.EnablePrePullActions = false;
        var context = AstraeaTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 4f,
            hasCard: false);
        var scheduler = SchedulerFactory.CreateForTest();

        new CardModule().CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.False(
            queue.Any(c => c.Behavior == AstraeaAbilities.AstralDraw) ||
            queue.Any(c => c.Behavior == AstraeaAbilities.UmbralDraw));
    }
}
