using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;
using DamageModule = Olympus.Rotation.AstraeaCore.Modules.DamageModule;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

/// <summary>
/// Tests for the AST DamageModule pre-pull hardcast branch.
/// At level 90, GetDamageGcdForLevel returns Fall Malefic (ActionId=25871, cast 1.5s,
/// threshold 2.0s). The inline AbilityBehavior is checked by ActionId since no
/// static behavior is referenced. All four gate paths are covered.
/// </summary>
public class DamageModulePrePullTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void PrePullHardcast_WithinThreshold_PushesFallMaleficAtPriority5()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var context = AstraeaTestContext.Create(
            level: 90,
            inCombat: false,
            countdownRemaining: 1.9f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior.Action.ActionId == ASTActions.FallMalefic.ActionId && c.Priority == 5);
    }

    [Fact]
    public void PrePullHardcast_AboveThreshold_DoesNotPushFallMalefic()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var context = AstraeaTestContext.Create(
            level: 90,
            inCombat: false,
            countdownRemaining: 2.1f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior.Action.ActionId == ASTActions.FallMalefic.ActionId);
    }

    [Fact]
    public void PrePullHardcast_NoTarget_DoesNotPushFallMalefic()
    {
        var context = AstraeaTestContext.Create(
            level: 90,
            inCombat: false,
            countdownRemaining: 1.9f);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior.Action.ActionId == ASTActions.FallMalefic.ActionId);
    }

    [Fact]
    public void PrePullHardcast_ToggleOff_DoesNotPushFallMalefic()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.PrePull.EnablePrePullActions = false;

        var context = AstraeaTestContext.Create(
            config: config,
            level: 90,
            inCombat: false,
            countdownRemaining: 1.9f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior.Action.ActionId == ASTActions.FallMalefic.ActionId);
    }

    [Fact]
    public void PrePullHardcast_DoesNotPush_WhenDamageDisabled()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableSingleTargetDamage = false;

        var context = AstraeaTestContext.Create(
            config: config,
            level: 90,
            inCombat: false,
            countdownRemaining: 1.9f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior.Action.ActionId == ASTActions.FallMalefic.ActionId);
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        return mock;
    }
}
