using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.IrisCore.Abilities;
using Olympus.Rotation.IrisCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.IrisCore.Modules;

/// <summary>
/// Tests for the PCT DamageModule pre-pull hardcast branch.
/// Rainbow Drip (cast 4.0s, threshold 4.5s) is pushed at priority 5 when the
/// countdown is within the cast window. All four gate paths are covered.
/// Note: IrisAbilities.RainbowDrip carries a Toggle=EnableRainbowDrip evaluated
/// at dispatch time; InspectGcdQueue shows all pushed candidates before dispatch.
/// </summary>
public class DamageModulePrePullTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void PrePullHardcast_WithinThreshold_PushesRainbowDripAtPriority5()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var context = IrisTestContext.Create(
            inCombat: false,
            countdownRemaining: 4.4f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.RainbowDrip && c.Priority == 5);
    }

    [Fact]
    public void PrePullHardcast_AboveThreshold_DoesNotPushRainbowDrip()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var context = IrisTestContext.Create(
            inCombat: false,
            countdownRemaining: 4.6f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.RainbowDrip);
    }

    [Fact]
    public void PrePullHardcast_NoTarget_DoesNotPushRainbowDrip()
    {
        var context = IrisTestContext.Create(
            inCombat: false,
            countdownRemaining: 4.4f);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.RainbowDrip);
    }

    [Fact]
    public void PrePullHardcast_ToggleOff_DoesNotPushRainbowDrip()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.PrePull.EnablePrePullActions = false;

        var context = IrisTestContext.Create(
            config: config,
            inCombat: false,
            countdownRemaining: 4.4f,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.RainbowDrip);
    }

    /// <summary>
    /// When all motifs are already painted, TryPushPrepaintMotif pushes nothing and Rainbow Drip
    /// fires as the sole GCD candidate. Verifies the motif-first priority ordering resolves
    /// correctly once the pre-pull painting sequence completes.
    /// </summary>
    [Fact]
    public void PrePullHardcast_AllMotifsPainted_PushesRainbowDrip()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.GetUserEnemyTarget()).Returns(enemy.Object);

        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.PrepaintMotifs = true;
        config.Pictomancer.EnableCreatureMotif = true;
        config.Pictomancer.EnableWeaponMotif = true;
        config.Pictomancer.EnableLandscapeMotif = true;

        // All motifs already painted: NeedsCreatureMotif/NeedsWeaponMotif/NeedsLandscapeMotif all false
        var context = IrisTestContext.Create(
            config: config,
            inCombat: false,
            countdownRemaining: 4.4f,
            needsCreatureMotif: false,
            needsWeaponMotif: false,
            needsLandscapeMotif: false,
            targetingService: targeting);

        var scheduler = SchedulerFactory.CreateForTest();
        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.RainbowDrip && c.Priority == 5);
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        return mock;
    }
}
