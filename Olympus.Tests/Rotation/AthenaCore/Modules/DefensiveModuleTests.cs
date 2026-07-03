using Olympus.Data;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Olympus.Tests.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Scheduler-push tests for Scholar DefensiveModule.
/// Covers Expedient (party mitigation + sprint) and Deployment Tactics (shield spreading).
/// </summary>
public class DefensiveModuleTests
{
    private readonly DefensiveModule _module = new();

    #region Module Properties

    [Fact]
    public void Priority_Is20()
    {
        Assert.Equal(20, _module.Priority);
    }

    [Fact]
    public void Name_IsDefensive()
    {
        Assert.Equal("Defensive", _module.Name);
    }

    #endregion

    #region Expedient Tests

    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        // DefensiveModule.CollectCandidates gates all candidates on context.InCombat
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExpedient = true;
        config.Scholar.EnableDeploymentTactics = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteOgcd: true,
            inCombat: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void CollectCandidates_ExpedientDisabled_PushesNothing()
    {
        // Toggle gate: EnableExpedient = false must prevent the candidate from being queued
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExpedient = false;
        config.Scholar.EnableDeploymentTactics = false;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(healthyCount: 1, injuredCount: 5, config: config);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteOgcd: true,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.Expedient.ActionId);
    }

    [Fact]
    public void CollectCandidates_Expedient_PartyHealthy_PushesNothing()
    {
        // HP threshold gate: avg party HP = 96%, above the 60% ExpedientThreshold — must not fire
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExpedient = true;
        config.Scholar.ExpedientThreshold = 0.60f;
        config.Scholar.EnableDeploymentTactics = false;

        // 8 members at 96% HP; avg HP well above threshold
        var partyHelper = AthenaTestContext.CreatePartyWithInjured(healthyCount: 8, injuredCount: 0, config: config);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Expedient.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteOgcd: true,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.Expedient.ActionId);
    }

    [Fact]
    public void CollectCandidates_Expedient_BelowLevel90_PushesNothing()
    {
        // Level gate: Expedient.MinLevel = 90; level 89 must be rejected
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExpedient = true;
        config.Scholar.ExpedientThreshold = 0.60f;
        config.Scholar.EnableDeploymentTactics = false;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(healthyCount: 1, injuredCount: 5, config: config);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Expedient.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 89,
            canExecuteOgcd: true,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.Expedient.ActionId);
    }

    [Fact]
    public void CollectCandidates_Expedient_ActionNotReady_PushesNothing()
    {
        // Cooldown gate: IsActionReady returns false — must not push even when party is injured
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExpedient = true;
        config.Scholar.ExpedientThreshold = 0.60f;
        config.Scholar.EnableDeploymentTactics = false;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(healthyCount: 1, injuredCount: 5, config: config);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Expedient.ActionId)).Returns(false);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteOgcd: true,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.Expedient.ActionId);
    }

    [Fact]
    public void CollectCandidates_Expedient_PartyInjured_PushesAtPriority75()
    {
        // Happy path: 1 healthy (96%) + 5 injured (50%) gives avg ~57.7%, below the 60%
        // threshold. All 6 members share Vector3.Zero with the player so all are in range.
        // Expedient must be pushed to the oGCD queue at priority 75.
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExpedient = true;
        config.Scholar.ExpedientThreshold = 0.60f;
        config.Scholar.EnableDeploymentTactics = false;

        var partyHelper = AthenaTestContext.CreatePartyWithInjured(healthyCount: 1, injuredCount: 5, config: config);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.Expedient.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteOgcd: true,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.Expedient.ActionId && c.Priority == 75);
    }

    #endregion

    #region Deployment Tactics Tests

    [Fact]
    public void CollectCandidates_DeploymentTacticsDisabled_PushesNothing()
    {
        // Toggle gate: EnableDeploymentTactics = false must suppress the candidate
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableDeploymentTactics = false;
        config.Scholar.EnableExpedient = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteOgcd: true,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.DeploymentTactics.ActionId);
    }

    #endregion
}
