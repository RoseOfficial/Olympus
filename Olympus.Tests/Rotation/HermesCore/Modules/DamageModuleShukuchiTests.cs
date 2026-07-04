using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.HermesCore.Abilities;
using Olympus.Rotation.HermesCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.HermesCore.Modules;

/// <summary>
/// Tests for Hermes (NIN) DamageModule.TryPushShukuchi. Shukuchi is a
/// ground-targeted gap closer with 2 charges, gated by AutoShukuchi
/// (player-agency toggle, default false). Does not fire during a mudra sequence.
/// </summary>
public class DamageModuleShukuchiTests
{
    private readonly DamageModule _module = new();

    // -----------------------------------------------------------------------
    // Happy path
    // -----------------------------------------------------------------------

    [Fact]
    public void Shukuchi_PushedAtPriority2_WhenOutOfMeleeRange_AndAutoShukuchiEnabled()
    {
        var enemy = CreateMockEnemy();
        var targeting = SetupTargetingOutOfRange(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        // Shukuchi has 2 charges; return 1 to satisfy the charge gate.
        actionService.Setup(x => x.GetCurrentCharges(NINActions.Shukuchi.ActionId)).Returns(1u);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.AutoShukuchi = true;
        config.Ninja.EnableShukuchi = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HermesTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 40,
            isMudraActive: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == HermesAbilities.Shukuchi && c.Priority == 2);
    }

    // -----------------------------------------------------------------------
    // Toggle-off
    // -----------------------------------------------------------------------

    [Fact]
    public void Shukuchi_NotPushed_WhenAutoShukuchiDisabled()
    {
        var enemy = CreateMockEnemy();
        var targeting = SetupTargetingOutOfRange(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(NINActions.Shukuchi.ActionId)).Returns(1u);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.AutoShukuchi = false; // player-agency gate off
        config.Ninja.EnableShukuchi = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HermesTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 40,
            isMudraActive: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == HermesAbilities.Shukuchi);
    }

    // -----------------------------------------------------------------------
    // Level gate
    // -----------------------------------------------------------------------

    [Fact]
    public void Shukuchi_NotPushed_WhenLevelBelowMinLevel()
    {
        var enemy = CreateMockEnemy();
        var targeting = SetupTargetingOutOfRange(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(NINActions.Shukuchi.ActionId)).Returns(1u);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.AutoShukuchi = true;
        config.Ninja.EnableShukuchi = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HermesTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: (byte)(NINActions.Shukuchi.MinLevel - 1),
            isMudraActive: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == HermesAbilities.Shukuchi);
    }

    // -----------------------------------------------------------------------
    // Mudra-sequence gate
    // -----------------------------------------------------------------------

    [Fact]
    public void Shukuchi_NotPushed_WhenMudraSequenceIsActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = SetupTargetingOutOfRange(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(NINActions.Shukuchi.ActionId)).Returns(1u);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.AutoShukuchi = true;
        config.Ninja.EnableShukuchi = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HermesTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 40,
            isMudraActive: true); // mudra sequence in progress

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == HermesAbilities.Shukuchi);
    }

    // -----------------------------------------------------------------------
    // Charge gate
    // -----------------------------------------------------------------------

    [Fact]
    public void Shukuchi_NotPushed_WhenNoChargesAvailable()
    {
        var enemy = CreateMockEnemy();
        var targeting = SetupTargetingOutOfRange(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        // Default MockBuilders.CreateMockActionService returns 0u for GetCurrentCharges —
        // but set explicitly for clarity.
        actionService.Setup(x => x.GetCurrentCharges(NINActions.Shukuchi.ActionId)).Returns(0u);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.AutoShukuchi = true;
        config.Ninja.EnableShukuchi = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HermesTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 40,
            isMudraActive: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == HermesAbilities.Shukuchi);
    }

    // -----------------------------------------------------------------------
    // Not pushed when in melee range
    // -----------------------------------------------------------------------

    [Fact]
    public void Shukuchi_NotPushed_WhenInMeleeRange()
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
        actionService.Setup(x => x.GetCurrentCharges(NINActions.Shukuchi.ActionId)).Returns(1u);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.AutoShukuchi = true;
        config.Ninja.EnableShukuchi = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HermesTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 40,
            isMudraActive: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == HermesAbilities.Shukuchi);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Sets up a targeting mock where FindEnemyForAction returns null (out of
    /// melee range) and FindEnemy returns the given enemy (within 20y).
    /// </summary>
    private static Mock<ITargetingService> SetupTargetingOutOfRange(Mock<IBattleNpc> enemy)
    {
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

        return targeting;
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }
}
