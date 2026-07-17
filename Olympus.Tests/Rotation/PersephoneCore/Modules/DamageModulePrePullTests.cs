using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.PersephoneCore.Abilities;
using Olympus.Rotation.PersephoneCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.PersephoneCore.Modules;

/// <summary>
/// Tests for the SMN DamageModule pre-pull hardcast branch.
/// Ruin III (cast 1.5s, threshold 2.0s) is pushed at priority 5 when the
/// countdown is within the cast window. All four gate paths are covered.
/// Note: PersephoneAbilities.Ruin3 carries a Toggle=EnableRuin evaluated at
/// dispatch time; InspectGcdQueue shows all pushed candidates before dispatch.
/// </summary>
public class DamageModulePrePullTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void PrePullHardcast_WithinThreshold_PushesRuin3AtPriority5()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var context = PersephoneTestContext.Create(
            inCombat: false,
            countdownRemaining: 1.9f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == PersephoneAbilities.Ruin3 && c.Priority == 5);
    }

    [Fact]
    public void PrePullHardcast_AboveThreshold_DoesNotPushRuin3()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var context = PersephoneTestContext.Create(
            inCombat: false,
            countdownRemaining: 2.1f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == PersephoneAbilities.Ruin3);
    }

    [Fact]
    public void PrePullHardcast_NoTarget_DoesNotPushRuin3()
    {
        var context = PersephoneTestContext.Create(
            inCombat: false,
            countdownRemaining: 1.9f);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == PersephoneAbilities.Ruin3);
    }

    [Fact]
    public void PrePullHardcast_ToggleOff_DoesNotPushRuin3()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var config = PersephoneTestContext.CreateDefaultSmnConfiguration();
        config.PrePull.EnablePrePullActions = false;

        var context = PersephoneTestContext.Create(
            config: config,
            inCombat: false,
            countdownRemaining: 1.9f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == PersephoneAbilities.Ruin3);
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        return mock;
    }
}
