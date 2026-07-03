using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Scheduler-push tests for ExcogitationHandler.
/// Key gates: EnableExcogitation toggle, level >= 62, IsActionReady,
/// AetherflowStacks > AetherflowReserve (unless Recitation active),
/// target found via FindExcogitationTarget, !IsTargetReserved,
/// hpPercent &lt;= ExcogitationThreshold OR tankBusterImminent.
/// </summary>
public class ExcogitationHandlerTests
{
    private readonly ExcogitationHandler _handler = new();

    [Fact]
    public void CollectCandidates_HappyPath_PushesExcogitationAtPriority15()
    {
        // Arrange: member at 50% HP (below 70% so FindExcogitationTarget picks them,
        // and below default ExcogitationThreshold of 0.85f)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExcogitation = true;
        config.Scholar.ExcogitationThreshold = 0.85f;
        config.Scholar.AetherflowReserve = 1;

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Excogitation.ActionId)).Returns(true);

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

        // Assert: Excogitation pushed to oGCD queue at priority 15
        var ogcd = scheduler.InspectOgcdQueue();
        var candidate = Assert.Single(ogcd, c => c.Behavior == AthenaAbilities.Excogitation);
        Assert.Equal(15, candidate.Priority);
        Assert.Equal(injured.Object.GameObjectId, candidate.TargetId);
    }

    [Fact]
    public void CollectCandidates_Disabled_DoesNotPush()
    {
        // Arrange: EnableExcogitation = false
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExcogitation = false;

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Excogitation.ActionId)).Returns(true);

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

        // Assert: toggle gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Excogitation);
    }

    [Fact]
    public void CollectCandidates_LevelTooLow_DoesNotPush()
    {
        // Arrange: level 61 (Excogitation requires level 62)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExcogitation = true;

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Excogitation.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 61,  // below MinLevel 62
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: level gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Excogitation);
    }

    [Fact]
    public void CollectCandidates_InsufficientAetherflowNoRecitation_DoesNotPush()
    {
        // Arrange: stacks == reserve (1 == 1), no Recitation active
        // Handler gate: !hasRecitation && stacks <= reserve => return
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExcogitation = true;
        config.Scholar.ExcogitationThreshold = 0.85f;
        config.Scholar.AetherflowReserve = 1;

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Excogitation.ActionId)).Returns(true);

        // stacks=1, reserve=1: 1 <= 1 — triggers the gate (no Recitation in test = StatusList null = false)
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
            c => c.Behavior == AthenaAbilities.Excogitation);
    }

    [Fact]
    public void CollectCandidates_TargetHpAboveThresholdNoTankBuster_DoesNotPush()
    {
        // Arrange: threshold set low (0.60f), member at 65% HP
        // FindExcogitationTarget finds them (65% < 70%), but threshold gate blocks (65% > 60%)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExcogitation = true;
        config.Scholar.ExcogitationThreshold = 0.60f;  // lower than default for this test
        config.Scholar.AetherflowReserve = 1;

        // 65% HP: found by ExcogitationTarget (< 70%) but above threshold (> 60%)
        var member = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 32500, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { member.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Excogitation.ActionId)).Returns(true);

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

        // Assert: HP above configured threshold and no tank buster — gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Excogitation);
    }
}
