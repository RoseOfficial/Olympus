using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.NikeCore.Abilities;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.NikeCore.Modules;

/// <summary>
/// Tests for Nike (SAM) DamageModule.TryPushGyoten. Gyoten is an out-of-melee
/// gap closer gated by AutoGyoten (player-agency toggle, default false) and a
/// Kenki reserve so spenders are never starved.
/// </summary>
public class DamageModuleGyotenTests
{
    private readonly DamageModule _module = new();

    // Gyoten costs 10 Kenki; default KenkiMinGauge is 25 → need >= 35 total.
    private const int SufficientKenki = 50;

    // -----------------------------------------------------------------------
    // Happy path
    // -----------------------------------------------------------------------

    [Fact]
    public void Gyoten_PushedAtPriority2_WhenOutOfMeleeRange_AndAutoGyotenEnabled()
    {
        // No in-range enemy → FindEnemyForAction returns null.
        // FindEnemy (20y fallback) returns the engage target.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting
            .Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting
            .Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        safetyMock.Setup(x => x.ShouldBlockGapCloser(It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>()))
            .Returns(false);
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.AutoGyoten = true;
        config.Samurai.EnableGyoten = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 54,
            kenki: SufficientKenki);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.Gyoten && c.Priority == 2);
    }

    // -----------------------------------------------------------------------
    // Toggle-off
    // -----------------------------------------------------------------------

    [Fact]
    public void Gyoten_NotPushed_WhenAutoGyotenDisabled()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting
            .Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting
            .Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        safetyMock.Setup(x => x.ShouldBlockGapCloser(It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>()))
            .Returns(false);
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.AutoGyoten = false; // player-agency gate off
        config.Samurai.EnableGyoten = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 54,
            kenki: SufficientKenki);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.Gyoten);
    }

    // -----------------------------------------------------------------------
    // Level gate
    // -----------------------------------------------------------------------

    [Fact]
    public void Gyoten_NotPushed_WhenLevelBelowMinLevel()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting
            .Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting
            .Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        safetyMock.Setup(x => x.ShouldBlockGapCloser(It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>()))
            .Returns(false);
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.AutoGyoten = true;
        config.Samurai.EnableGyoten = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: (byte)(SAMActions.Gyoten.MinLevel - 1),
            kenki: SufficientKenki);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.Gyoten);
    }

    // -----------------------------------------------------------------------
    // Kenki reserve gate
    // -----------------------------------------------------------------------

    [Fact]
    public void Gyoten_NotPushed_WhenKenkiTooLowToPreserveSpenderReserve()
    {
        // Default KenkiMinGauge = 25 → need 35 total; provide only 34.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting
            .Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting
            .Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        safetyMock.Setup(x => x.ShouldBlockGapCloser(It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>()))
            .Returns(false);
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.AutoGyoten = true;
        config.Samurai.EnableGyoten = true;
        // KenkiMinGauge defaults to 25; 10 (cost) + 25 (reserve) = 35 required.

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 54,
            kenki: 34); // one below the threshold

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.Gyoten);
    }

    [Fact]
    public void Gyoten_NotPushed_WhenIsActionReadyReturnsFalse()
    {
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting
            .Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting
            .Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        safetyMock.Setup(x => x.ShouldBlockGapCloser(It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>()))
            .Returns(false);
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.AutoGyoten = true;
        config.Samurai.EnableGyoten = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 54,
            kenki: SufficientKenki);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.Gyoten);
    }

    // -----------------------------------------------------------------------
    // Not pushed when in melee range (target found normally)
    // -----------------------------------------------------------------------

    [Fact]
    public void Gyoten_NotPushed_WhenInMeleeRange()
    {
        // FindEnemyForAction returns a target — player is in melee range.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting
            .Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.AutoGyoten = true;
        config.Samurai.EnableGyoten = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 54,
            kenki: SufficientKenki);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.Gyoten);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }
}
