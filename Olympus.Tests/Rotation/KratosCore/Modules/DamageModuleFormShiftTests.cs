using Moq;
using Olympus.Data;
using Olympus.Rotation.KratosCore.Abilities;
using Olympus.Rotation.KratosCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.KratosCore.Modules;

/// <summary>
/// Verifies DamageModule pushes Form Shift to the GCD queue during the pre-pull countdown.
/// Form Shift is a GCD -- AllowPreCombatOgcdDispatch is NOT needed for this.
///
/// DISCRIMINATION: Negative tests set chakra: 4 and also assert Contains(Meditation) in
/// the GCD queue. Meditation is pushed by the else-if branch that only runs when Form Shift's
/// gate fails. If the gate were always-true, Form Shift would crowd out Meditation and the
/// Contains(Meditation) assertion would catch the bug. If the entire pre-combat block were
/// accidentally removed, Contains(Meditation) would also fail (inCombat=false causes the
/// in-combat code to skip Meditation).
/// </summary>
public class DamageModuleFormShiftTests
{
    private static Configuration MakeConfig(bool enablePrePull = true, bool enableFormShift = true)
    {
        var cfg = KratosTestContext.CreateDefaultMonkConfiguration();
        cfg.PrePull.EnablePrePullActions = enablePrePull;
        cfg.Monk.EnablePreCombatFormShift = enableFormShift;
        return cfg;
    }

    [Fact]
    public void FormShift_PushedToGcdQueue_WhenWithinCountdownWindow()
    {
        var config = MakeConfig();
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.FormShift.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = KratosTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            hasFormlessFist: false,
            countdownRemaining: 7f);

        new DamageModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == MNKActions.FormShift.ActionId);
    }

    /// <summary>
    /// Negative -- countdownRemaining outside the 8s window. chakra: 4 so Meditation
    /// is the fallback push. Asserting Contains(Meditation) proves the module's pre-combat
    /// block ran AND the else-if branch executed -- a broken gate that always fires FormShift
    /// would exclude Meditation and fail.
    /// </summary>
    [Fact]
    public void FormShift_NotPushed_WhenOutsideCountdownWindow()
    {
        var config = MakeConfig();
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.FormShift.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(MNKActions.Meditation.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = KratosTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            hasFormlessFist: false,
            chakra: 4,              // enables Meditation fallback
            countdownRemaining: 15f); // only discriminating variable vs positive (15f > 8s)

        new DamageModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == MNKActions.FormShift.ActionId);
        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == MNKActions.Meditation.ActionId); // pre-combat ran
    }

    /// <summary>
    /// Toggle-off companion -- countdown within window but EnablePreCombatFormShift is false.
    /// Meditation is the fallback. Both assertions discriminate against a buggy always-firing gate.
    /// </summary>
    [Fact]
    public void FormShift_NotPushed_WhenToggleDisabled()
    {
        var config = MakeConfig(enableFormShift: false); // only discriminating variable
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.FormShift.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(MNKActions.Meditation.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = KratosTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,
            hasFormlessFist: false,
            chakra: 4,              // enables Meditation fallback
            countdownRemaining: 7f); // same as positive

        new DamageModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == MNKActions.FormShift.ActionId);
        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == MNKActions.Meditation.ActionId); // pre-combat ran
    }
}
