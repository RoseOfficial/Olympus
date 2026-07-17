using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Tests.Rotation.AthenaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

public class PrePullAdloquiumTests
{
    // Note: the positive case (HasRecitation=true -> Adloquium pushed) is not unit-testable.
    // AthenaContext.HasRecitation reads AthenaStatusHelper.HasRecitation(Player) which iterates
    // Player.StatusList (a Dalamud native struct). Mock players have StatusList = null, so
    // HasRecitation always returns false in tests. See ShieldHealingHandlerSchedulerTests.cs
    // lines 66-70 for the identical precedent on SGE.
    // The negative tests below still discriminate correctly because they return before the
    // HasRecitation gate is reached (countdown-gate and toggle-gate fire first).

    // 1. Countdown above 8s -> no push (returns at countdown gate, before HasRecitation).
    [Fact]
    public void HealingModule_PrePullAdloquium_NoPushWhenCountdownAbove8()
    {
        var context = AthenaTestContext.Create(
            inCombat: false,
            countdownRemaining: 9f);
        var scheduler = SchedulerFactory.CreateForTest();

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == AthenaAbilities.Adloquium);
    }

    // 2. EnablePrePullActions disabled -> no push (same setup; only toggle differs).
    [Fact]
    public void HealingModule_PrePullAdloquium_NoPushWhenPrePullDisabled()
    {
        var cfg = AthenaTestContext.CreateDefaultScholarConfiguration();
        cfg.PrePull.EnablePrePullActions = false;
        var context = AthenaTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 7f);
        var scheduler = SchedulerFactory.CreateForTest();

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == AthenaAbilities.Adloquium);
    }

    // 3. Scholar.EnableAdloquium disabled -> no push (returns before HasRecitation gate).
    [Fact]
    public void HealingModule_PrePullAdloquium_NoPushWhenAdloquiumToggleOff()
    {
        var cfg = AthenaTestContext.CreateDefaultScholarConfiguration();
        cfg.Scholar.EnableAdloquium = false;
        var context = AthenaTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 7f);
        var scheduler = SchedulerFactory.CreateForTest();

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == AthenaAbilities.Adloquium);
    }
}
