using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.NyxCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteGcd: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void TryExecute_InCombat_NoTargetAnywhere_ReturnsFalse()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = CreateContext(inCombat: true, canExecuteGcd: true, canExecuteOgcd: false,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("No target", context.Debug.DamageState);
    }

    [Fact]
    public void TryExecute_InCombat_TargetOutOfMeleeRange_GcdReady_ExecutesUnmend()
    {
        // Arrange — target is within 20y but outside 5y melee range
        var enemy = CreateMockEnemy(objectId: 12345UL);
        var targeting = MockBuilders.CreateMockTargetingService();

        // Melee range search (5f) → null
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.Is<float>(r => r <= 5f),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        // Engage range search (20f) → enemy found
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.Is<float>(r => r > 5f),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Unmend.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRKActions.Unmend.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false, // no GCD in flight yet
            level: 15,
            targetingService: targeting,
            actionService: actionService);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.Unmend.ActionId),
            enemy.Object.GameObjectId), Times.Once);
    }

    [Fact]
    public void TryExecute_InCombat_TargetOutOfMeleeRange_OgcdReady_ExecutesShadowstride()
    {
        // Arrange — GCD already in flight (weave window), try gap close
        var enemy = CreateMockEnemy(objectId: 12345UL);
        var targeting = MockBuilders.CreateMockTargetingService();

        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.Is<float>(r => r <= 5f),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.Is<float>(r => r > 5f),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Shadowstride.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRKActions.Shadowstride.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: false, // GCD rolling
            canExecuteOgcd: true, // weave window open
            level: 54,
            targetingService: targeting,
            actionService: actionService);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.Shadowstride.ActionId),
            enemy.Object.GameObjectId), Times.Once);
    }

    [Fact]
    public void TryExecute_InCombat_TargetOutOfMeleeRange_BelowUnmendLevel_NoAction()
    {
        // Level 10 — below Unmend (Lv.15) and Shadowstride (Lv.54)
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();

        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.Is<float>(r => r <= 5f),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.Is<float>(r => r > 5f),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 10,
            targetingService: targeting,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        // Can't act — below level for all ranged options
        Assert.False(result);
        actionService.Verify(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    #region Helpers

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static INyxContext CreateContext(
        bool inCombat,
        bool canExecuteGcd,
        bool canExecuteOgcd,
        byte level = 80,
        Mock<ITargetingService>? targetingService = null,
        Mock<IActionService>? actionService = null)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        var config = MockBuilders.CreateDefaultConfiguration();

        var mock = new Mock<INyxContext>();

        // Base state
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(canExecuteGcd);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);

        // DRK-specific — safe defaults
        mock.Setup(x => x.BloodGauge).Returns(0);
        mock.Setup(x => x.CurrentMp).Returns(10000);
        mock.Setup(x => x.MaxMp).Returns(10000);
        mock.Setup(x => x.HasEnoughMpForTbn).Returns(true);
        mock.Setup(x => x.HasEnoughMpForEdge).Returns(true);
        mock.Setup(x => x.HasDarkside).Returns(false);
        mock.Setup(x => x.DarksideRemaining).Returns(0f);
        mock.Setup(x => x.HasDelirium).Returns(false);
        mock.Setup(x => x.DeliriumStacks).Returns(0);
        mock.Setup(x => x.HasDarkArts).Returns(false);
        mock.Setup(x => x.HasScornfulEdge).Returns(false);
        mock.Setup(x => x.ComboStep).Returns(0);

        // Debug state (mutable — use real object)
        var debugState = new NyxDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        // Default: no enemies in range unless the caller's targetingService overrides this
        targetingService.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);

        return mock.Object;
    }

    #endregion
}
