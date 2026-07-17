using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.HecateCore.Abilities;
using Olympus.Rotation.HecateCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.HecateCore.Modules;

/// <summary>
/// Tests for the BLM DamageModule pre-pull hardcast branch.
/// Fire III (cast 3.5s, threshold 4.0s) is pushed at priority 5 when the
/// countdown is within the cast window. All four gate paths are covered.
/// </summary>
public class DamageModulePrePullTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void PrePullHardcast_WithinThreshold_PushesFire3AtPriority5()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var context = HecateTestContext.Create(
            inCombat: false,
            countdownRemaining: 3.9f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == HecateAbilities.Fire3 && c.Priority == 5);
    }

    [Fact]
    public void PrePullHardcast_TooLate_DoesNotPushFire3()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var context = HecateTestContext.Create(
            inCombat: false,
            countdownRemaining: 4.1f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == HecateAbilities.Fire3);
    }

    [Fact]
    public void PrePullHardcast_NoTarget_DoesNotPushFire3()
    {
        var context = HecateTestContext.Create(
            inCombat: false,
            countdownRemaining: 3.9f);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == HecateAbilities.Fire3);
    }

    [Fact]
    public void PrePullHardcast_ToggleOff_DoesNotPushFire3()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.PrePull.EnablePrePullActions = false;

        var context = HecateTestContext.Create(
            config: config,
            inCombat: false,
            countdownRemaining: 3.9f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == HecateAbilities.Fire3);
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        return mock;
    }
}
