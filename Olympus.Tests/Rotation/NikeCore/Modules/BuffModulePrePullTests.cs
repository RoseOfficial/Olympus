using Moq;
using Olympus.Data;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.NikeCore.Modules;

/// <summary>
/// Verifies BuffModule pushes Meikyo Shisui to the oGCD queue during the pre-pull countdown.
///
/// DISCRIMINATION NOTE: The positive test asserts "Pre-pull: Meikyo Shisui" in the debug
/// state. All negative tests assert "Not in combat" in the debug state -- a buggy always-firing
/// gate would set the "Pre-pull: ..." string and fail the Equal assertion.
/// </summary>
public class BuffModulePrePullTests
{
    private static Configuration MakeConfig(bool enablePrePull = true, bool enableMeikyo = true)
    {
        var cfg = NikeTestContext.CreateDefaultSamuraiConfiguration();
        cfg.PrePull.EnablePrePullActions = enablePrePull;
        cfg.Samurai.EnableMeikyoShisui = enableMeikyo;
        // Disable all in-combat oGCDs so they cannot push pre-combat candidates.
        cfg.Samurai.EnableShoha = false;
        cfg.Samurai.EnableZanshin = false;
        cfg.Samurai.EnableIkishoten = false;
        return cfg;
    }

    [Fact]
    public void MeikyoShisui_PushedToOgcdQueue_WhenWithinCountdownWindow()
    {
        var config = MakeConfig();
        var debugState = new NikeDebugState();
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SAMActions.MeikyoShisui.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            hasMeikyoShisui: false,
            debugState: debugState,
            countdownRemaining: 8f);

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SAMActions.MeikyoShisui.ActionId);
        Assert.Contains("Pre-pull", debugState.BuffState);
    }

    /// <summary>
    /// Negative -- same setup as positive except countdownRemaining is outside the 9s window.
    /// Debug state must be "Not in combat" (else branch ran, not the pre-pull path).
    /// A buggy always-firing gate would set "Pre-pull: Meikyo Shisui" and fail the Equal.
    /// </summary>
    [Fact]
    public void MeikyoShisui_NotPushed_WhenOutsideCountdownWindow()
    {
        var config = MakeConfig();
        var debugState = new NikeDebugState();
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SAMActions.MeikyoShisui.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            hasMeikyoShisui: false,
            debugState: debugState,
            countdownRemaining: 15f);  // only variable vs positive (15f > 9s window)

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SAMActions.MeikyoShisui.ActionId);
        Assert.Equal("Not in combat", debugState.BuffState); // else branch ran
    }

    /// <summary>
    /// Toggle-off companion -- countdown within window but EnableMeikyoShisui is false.
    /// </summary>
    [Fact]
    public void MeikyoShisui_NotPushed_WhenToggleDisabled()
    {
        var config = MakeConfig(enableMeikyo: false); // only variable vs positive
        var debugState = new NikeDebugState();
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SAMActions.MeikyoShisui.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            hasMeikyoShisui: false,
            debugState: debugState,
            countdownRemaining: 8f);    // same as positive

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SAMActions.MeikyoShisui.ActionId);
        Assert.Equal("Not in combat", debugState.BuffState); // else branch ran
    }
}
