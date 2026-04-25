using Olympus.Data;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Scholar;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Scheduler-push tests for FairyModule. The distinctive behavior is that fairy
/// summon pushes at priority 0 — beating Resurrection (1-2) — so a despawned
/// fairy gets resummoned before any other action fires.
/// </summary>
public class FairyModuleSchedulerTests
{
    private readonly FairyModule _module = new();

    [Fact]
    public void CollectCandidates_FairyDespawned_PushesSummonEosAtPriorityZero()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AutoSummonFairy = true;

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(state: FairyState.None);
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            inCombat: true,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        var summonCandidate = Assert.Single(queue, c => c.Behavior.Action.ActionId == SCHActions.SummonEos.ActionId);
        Assert.Equal(0, summonCandidate.Priority);
    }

    [Fact]
    public void CollectCandidates_FairyAlreadyPresent_DoesNotPushSummon()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AutoSummonFairy = true;

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(state: FairyState.Eos);
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            inCombat: true,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.SummonEos.ActionId);
    }

    [Fact]
    public void CollectCandidates_FairyDespawnedWhileMoving_DoesNotPushSummon()
    {
        // SummonEos has a cast time, so it cannot be pushed while moving.
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AutoSummonFairy = true;

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(state: FairyState.None);
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            inCombat: true,
            canExecuteGcd: true,
            isMoving: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: true);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.SummonEos.ActionId);
    }

    [Fact]
    public void CollectCandidates_FairyDespawnedDuringDissipation_DoesNotPushSummon()
    {
        // Dissipation deliberately sacrifices the fairy for 30s — don't summon during it.
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AutoSummonFairy = true;

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(state: FairyState.Dissipated);
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            inCombat: true,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.SummonEos.ActionId);
    }

    [Fact]
    public void CollectCandidates_AutoSummonDisabled_DoesNotPushSummon()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AutoSummonFairy = false;

        var fairyStateManager = AthenaTestContext.CreateMockFairyStateManager(state: FairyState.None);
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            fairyStateManager: fairyStateManager,
            level: 100,
            inCombat: true,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.SummonEos.ActionId);
    }
}
