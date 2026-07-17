using Moq;
using Olympus.Data;
using Olympus.Rotation.PrometheusCore.Context;
using Olympus.Rotation.PrometheusCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.PrometheusCore.Modules;

/// <summary>
/// Verifies BuffModule pushes Reassemble to the oGCD queue during the pre-pull countdown.
///
/// DISCRIMINATION NOTE: MCH has no pre-existing pre-combat oGCD push (Peloton is gated
/// on isMoving=true; tests use isMoving=false). Negative tests assert
/// Debug.BuffState == "Not in combat" -- a buggy always-firing gate would set
/// "Pre-pull: Reassemble" and fail the Equal assertion.
/// </summary>
public class BuffModuleReassemblePrePullTests
{
    private static Configuration MakeConfig(bool enablePrePull = true, bool enableReassemble = true)
    {
        var cfg = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        cfg.PrePull.EnablePrePullActions = enablePrePull;
        cfg.Machinist.EnableReassemble = enableReassemble;
        // Disable all in-combat oGCDs to isolate the pre-pull path.
        cfg.Machinist.EnableWildfire = false;
        cfg.Machinist.EnableBarrelStabilizer = false;
        cfg.Machinist.EnableHypercharge = false;
        cfg.Machinist.EnableGaussRicochet = false;
        return cfg;
    }

    [Fact]
    public void Reassemble_PushedToOgcdQueue_WhenWithinCountdownWindow()
    {
        var config = MakeConfig();
        var debugState = new PrometheusDebugState();
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(MCHActions.Reassemble.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            isMoving: false,
            hasReassemble: false,
            reassembleCharges: 1,
            debugState: debugState,
            countdownRemaining: 4f);

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == MCHActions.Reassemble.ActionId);
        Assert.Contains("Pre-pull", debugState.BuffState);
    }

    /// <summary>
    /// Negative -- countdownRemaining outside the 5s window.
    /// Debug state must be "Not in combat" (else branch ran).
    /// </summary>
    [Fact]
    public void Reassemble_NotPushed_WhenOutsideCountdownWindow()
    {
        var config = MakeConfig();
        var debugState = new PrometheusDebugState();
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(MCHActions.Reassemble.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            isMoving: false,
            hasReassemble: false,
            reassembleCharges: 1,
            debugState: debugState,
            countdownRemaining: 10f);  // only discriminating variable vs positive (10f > 5s)

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == MCHActions.Reassemble.ActionId);
        Assert.Equal("Not in combat", debugState.BuffState); // else branch ran
    }

    /// <summary>
    /// Toggle-off companion -- countdown within window but EnableReassemble is false.
    /// </summary>
    [Fact]
    public void Reassemble_NotPushed_WhenToggleDisabled()
    {
        var config = MakeConfig(enableReassemble: false); // only discriminating variable
        var debugState = new PrometheusDebugState();
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(MCHActions.Reassemble.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PrometheusTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            isMoving: false,
            hasReassemble: false,
            reassembleCharges: 1,
            debugState: debugState,
            countdownRemaining: 4f);    // same as positive

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == MCHActions.Reassemble.ActionId);
        Assert.Equal("Not in combat", debugState.BuffState); // else branch ran
    }
}
