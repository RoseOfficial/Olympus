using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Services.Scholar;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Scheduler-push tests for LustrateHandler.
/// Key gates: EnableLustrate toggle, level >= 45, IsActionReady,
/// AetherflowStacks > AetherflowReserve, target found, !HasNoHealStatus,
/// !IsTargetReserved, hpPercent &lt;= LustrateThreshold.
/// </summary>
public class LustrateHandlerTests
{
    private readonly LustrateHandler _handler = new();

    [Fact]
    public void CollectCandidates_HappyPath_PushesLustrateAtPriority20()
    {
        // Arrange: target at 50% HP, 3 aetherflow stacks with reserve 1 (3 > 1 = proceed)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = true;
        config.Scholar.LustrateThreshold = 0.55f;
        config.Scholar.AetherflowReserve = 1;

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);

        var aetherflow = AthenaTestContext.CreateMockAetherflowService(currentStacks: 3);
        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflow,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: Lustrate candidate pushed to oGCD queue at priority 20
        var ogcd = scheduler.InspectOgcdQueue();
        var candidate = Assert.Single(ogcd, c => c.Behavior == AthenaAbilities.Lustrate);
        Assert.Equal(20, candidate.Priority);
        Assert.Equal(injured.Object.GameObjectId, candidate.TargetId);
    }

    [Fact]
    public void CollectCandidates_Disabled_DoesNotPush()
    {
        // Arrange: EnableLustrate = false
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = false;

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: nothing pushed
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Lustrate);
    }

    [Fact]
    public void CollectCandidates_LevelTooLow_DoesNotPush()
    {
        // Arrange: level 44 (Lustrate requires level 45)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = true;

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 44,  // below MinLevel 45
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: level gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Lustrate);
    }

    [Fact]
    public void CollectCandidates_InsufficientAetherflow_DoesNotPush()
    {
        // Arrange: stacks == reserve (1 == 1), gate requires stacks > reserve
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = true;
        config.Scholar.LustrateThreshold = 0.55f;
        config.Scholar.AetherflowReserve = 1;

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);

        // stacks <= reserve: 1 <= 1 — handler returns early
        var aetherflow = AthenaTestContext.CreateMockAetherflowService(currentStacks: 1);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflow,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: aetherflow gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Lustrate);
    }

    [Fact]
    public void CollectCandidates_TargetHpAboveThreshold_DoesNotPush()
    {
        // Arrange: target at 80% HP > LustrateThreshold (55%) — handler skips
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = true;
        config.Scholar.LustrateThreshold = 0.55f;
        config.Scholar.AetherflowReserve = 1;

        // 80% HP: found by FindLowestHpPartyMember (not full) but above threshold
        var member = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 40000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { member.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);

        var aetherflow = AthenaTestContext.CreateMockAetherflowService(currentStacks: 3);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflow,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: HP threshold gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Lustrate);
    }

    [Fact]
    public void CollectCandidates_NoPartyTarget_DoesNotPush()
    {
        // Arrange: empty party — FindLowestHpPartyMember returns null
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = true;

        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara>(), config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: no target found — nothing pushed
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Lustrate);
    }
}
