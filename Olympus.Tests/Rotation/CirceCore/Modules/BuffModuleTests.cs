using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.CirceCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.CirceCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CirceTestContext.Create(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_OgcdNotReady_ReturnsFalse()
    {
        var context = CirceTestContext.Create(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_FlecheReady_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Fleche.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            flecheReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Fleche.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ContreSixteReady_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.ContreSixte.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            flecheReady: false,    // Fleche not ready — falls through to ContreSixte
            contreSixteReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.ContreSixte.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ViceOfThorns_WithThornedFlourish_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.ViceOfThorns.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            flecheReady: false,
            contreSixteReady: false,
            hasThornedFlourish: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.ViceOfThorns.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Embolden_WhenReady_NoEmboldenActive_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Embolden.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            flecheReady: false,
            contreSixteReady: false,
            hasThornedFlourish: false,
            hasPrefulgenceReady: false,
            emboldenReady: true,
            hasEmbolden: false,
            canStartMeleeCombo: true,    // In position to use Embolden before melee
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Embolden.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_NoBuffsNeeded_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            flecheReady: false,
            contreSixteReady: false,
            hasThornedFlourish: false,
            hasPrefulgenceReady: false,
            emboldenReady: false,
            manaficationReady: false,
            corpsACorpsCharges: 0,
            engagementCharges: 0,
            accelerationCharges: 0,
            lucidDreamingReady: false,
            actionService: actionService);

        Assert.False(_module.TryExecute(context, isMoving: false));
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

    private static Mock<ITargetingService> CreateTargetingWithEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);
        return targeting;
    }

    #endregion
}
