using Moq;
using Olympus.Data;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Rotation.ThanatosCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ThanatosCore.Modules;

/// <summary>
/// Verifies DamageModule pushes Soulsow to the GCD queue during the pre-pull countdown.
/// Soulsow is gated on EnableHarvestMoon because Soulsow is only useful as a Harvest Moon
/// prerequisite, and both should follow the same opt-in toggle.
/// Keyed on countdown so it never fires while idling out of combat between pulls.
///
/// DISCRIMINATION NOTE: RPR has no pre-existing pre-combat push. Negative tests assert
/// Debug.DamageState == "Not in combat" -- a buggy always-firing gate would set
/// "Pre-pull: Soulsow (countdown)" and fail the Equal assertion.
/// </summary>
public class DamageModuleSoulsowTests
{
    private static Configuration MakeConfig(bool enablePrePull = true, bool enableHarvestMoon = true)
    {
        var cfg = ThanatosTestContext.CreateDefaultReaperConfiguration();
        cfg.PrePull.EnablePrePullActions = enablePrePull;
        cfg.Reaper.EnableHarvestMoon = enableHarvestMoon;
        return cfg;
    }

    [Fact]
    public void Soulsow_PushedToGcdQueue_WhenWithinCountdownWindow()
    {
        var config = MakeConfig();
        var debugState = new ThanatosDebugState();
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Soulsow.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = ThanatosTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            hasSoulsow: false,
            debugState: debugState,
            countdownRemaining: 6f);

        new DamageModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == RPRActions.Soulsow.ActionId);
        Assert.Contains("Pre-pull", debugState.DamageState);
    }

    /// <summary>
    /// Negative -- no countdown (null). Only discriminating variable vs positive.
    /// Debug state must be "Not in combat" (else branch ran).
    /// </summary>
    [Fact]
    public void Soulsow_NotPushed_WhenNoCountdown()
    {
        var config = MakeConfig();
        var debugState = new ThanatosDebugState();
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Soulsow.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = ThanatosTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            hasSoulsow: false,
            debugState: debugState,
            countdownRemaining: null);  // only discriminating variable vs positive

        new DamageModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == RPRActions.Soulsow.ActionId);
        Assert.Equal("Not in combat", debugState.DamageState); // else branch ran
    }

    /// <summary>
    /// Toggle-off companion -- countdown within window but EnableHarvestMoon is false.
    /// </summary>
    [Fact]
    public void Soulsow_NotPushed_WhenHarvestMoonDisabled()
    {
        var config = MakeConfig(enableHarvestMoon: false); // only discriminating variable
        var debugState = new ThanatosDebugState();
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Soulsow.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = ThanatosTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            hasSoulsow: false,
            debugState: debugState,
            countdownRemaining: 6f);    // same as positive

        new DamageModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == RPRActions.Soulsow.ActionId);
        Assert.Equal("Not in combat", debugState.DamageState); // else branch ran
    }
}
