using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

public class PrePullEukrasianPrognosisTests
{
    private static Mock<IActionService> EukrasiaReadyService()
    {
        var svc = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        svc.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
                It.IsAny<ulong>()))
           .Returns(true);
        return svc;
    }

    // Phase 1: countdown <= 6s, Eukrasia not active -> ExecuteOgcd(Eukrasia) called directly.
    // The Eukrasia direct-dispatch path bypasses the scheduler's CanExecuteOgcd gate
    // (CLAUDE.md "SGE Eukrasia timing" invariant, preserved byte-for-byte from ShieldHealingHandler).
    [Fact]
    public void HealingModule_PrePullEukrasia_DirectDispatchesEukrasiaWhenCountdownAtOrBelow6()
    {
        var svc = EukrasiaReadyService();
        var context = AsclepiusTestContext.Create(
            inCombat: false,
            countdownRemaining: 5f,
            canExecuteGcd: true,
            hasEukrasia: false,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        svc.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    // Phase 1: countdown above 6s -> Eukrasia NOT dispatched.
    [Fact]
    public void HealingModule_PrePullEukrasia_NoDispatchWhenCountdownAbove6()
    {
        var svc = EukrasiaReadyService();
        var context = AsclepiusTestContext.Create(
            inCombat: false,
            countdownRemaining: 8f,
            canExecuteGcd: true,
            hasEukrasia: false,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        svc.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    // Phase 1 toggle: EnablePrePullActions disabled -> no dispatch.
    [Fact]
    public void HealingModule_PrePullEukrasia_NoDispatchWhenPrePullDisabled()
    {
        var cfg = AsclepiusTestContext.CreateDefaultSageConfiguration();
        cfg.PrePull.EnablePrePullActions = false;
        var svc = EukrasiaReadyService();
        var context = AsclepiusTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 5f,
            canExecuteGcd: true,
            hasEukrasia: false,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        svc.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    // Per-job toggle: Sage.EnableEukrasianPrognosis disabled -> Eukrasia not dispatched.
    [Fact]
    public void HealingModule_PrePullEukrasia_NoDispatchWhenEukrasianPrognosisToggleOff()
    {
        var cfg = AsclepiusTestContext.CreateDefaultSageConfiguration();
        cfg.Sage.EnableEukrasianPrognosis = false;
        var svc = EukrasiaReadyService();
        var context = AsclepiusTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 5f,
            canExecuteGcd: true,
            hasEukrasia: false,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        svc.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    // Note: Phase 2 (HasEukrasia=true -> push EukrasianPrognosis to GCD queue) is not
    // unit-testable. AsclepiusContext.HasEukrasia reads AsclepiusStatusHelper.HasEukrasia(Player)
    // which iterates Player.StatusList (Dalamud native struct); mock players have StatusList=null
    // so HasEukrasia always returns false in tests. See ShieldHealingHandlerSchedulerTests.cs
    // lines 66-70 for the identical precedent. The critical Phase 1 carve-out (direct-dispatch
    // Eukrasia when not active) is fully covered above.
}
