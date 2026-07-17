using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.CirceCore.Abilities;
using Olympus.Rotation.CirceCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.CirceCore.Modules;

/// <summary>
/// Tests for the RDM DamageModule pre-pull hardcast branch.
/// Verthunder III (cast 5.0s, threshold 5.5s) is pushed at priority 5 when
/// the countdown is within the cast window. All four gate paths are covered.
/// </summary>
public class DamageModulePrePullTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void PrePullHardcast_WithinThreshold_PushesVerthunder3AtPriority5()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var context = CirceTestContext.Create(
            inCombat: false,
            countdownRemaining: 5.4f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == CirceAbilities.Verthunder3 && c.Priority == 5);
    }

    [Fact]
    public void PrePullHardcast_TooLate_DoesNotPushVerthunder3()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var context = CirceTestContext.Create(
            inCombat: false,
            countdownRemaining: 5.6f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == CirceAbilities.Verthunder3);
    }

    [Fact]
    public void PrePullHardcast_NoTarget_DoesNotPushVerthunder3()
    {
        var context = CirceTestContext.Create(
            inCombat: false,
            countdownRemaining: 5.4f);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == CirceAbilities.Verthunder3);
    }

    [Fact]
    public void PrePullHardcast_ToggleOff_DoesNotPushVerthunder3()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var config = CirceTestContext.CreateDefaultRdmConfiguration();
        config.PrePull.EnablePrePullActions = false;

        var context = CirceTestContext.Create(
            config: config,
            inCombat: false,
            countdownRemaining: 5.4f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == CirceAbilities.Verthunder3);
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        return mock;
    }
}
