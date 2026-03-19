using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Rotation.ThemisCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ThemisCore.Modules;

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
    public void TryExecute_InCombat_TargetOutOfMeleeRange_GcdReady_ExecutesShieldLob()
    {
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
        actionService.Setup(x => x.IsActionReady(PLDActions.ShieldLob.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == PLDActions.ShieldLob.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 15,
            targetingService: targeting,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.ShieldLob.ActionId),
            enemy.Object.GameObjectId), Times.Once);
    }

    // Regression: HolySpirit is a Lv.64 PLD-exclusive spell. At level 60 (pre-Stormblood
    // progression), even when Requiescat is active the magic phase must not attempt to cast
    // HolySpirit — doing so would result in a silent fail from the game and waste the GCD slot.
    [Fact]
    public void TryExecute_HolySpirit_BelowMinLevel_DoesNotUse()
    {
        var enemy = CreateMockEnemy(objectId: 12345UL);
        var targeting = MockBuilders.CreateMockTargetingService();

        // Target within melee range
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        // All actions not ready to keep the test focused
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        // Level 60 — HolySpirit requires Lv.64
        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 60,
            targetingService: targeting,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        // HolySpirit must never be attempted at level 60
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.HolySpirit.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_InCombat_TargetOutOfMeleeRange_OgcdReady_ExecutesIntervene()
    {
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
        actionService.Setup(x => x.IsActionReady(PLDActions.Intervene.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PLDActions.Intervene.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 74, // Intervene min level
            targetingService: targeting,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.Intervene.ActionId),
            enemy.Object.GameObjectId), Times.Once);
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

    private static ThemisContext CreateContext(
        bool inCombat,
        bool canExecuteGcd,
        bool canExecuteOgcd,
        byte level = 80,
        Mock<ITargetingService>? targetingService = null,
        Mock<IActionService>? actionService = null)
    {
        return ThemisTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
