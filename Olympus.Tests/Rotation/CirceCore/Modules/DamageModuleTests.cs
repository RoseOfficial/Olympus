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

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CirceTestContext.Create(inCombat: false, canExecuteGcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_GcdNotReady_ReturnsFalse()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_NoTarget_ReturnsFalse()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_Resolution_WhenResolutionReady_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Resolution.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isResolutionReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Resolution.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Scorch_WhenScorchReady_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Scorch.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isResolutionReady: false,
            isScorchReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Scorch.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_GrandImpact_WhenReady_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.GrandImpact.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isResolutionReady: false,
            isScorchReady: false,
            isGrandImpactReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.GrandImpact.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_EnchantedRiposte_WhenCanStartMeleeCombo_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.EnchantedRiposte.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isResolutionReady: false,
            isScorchReady: false,
            isGrandImpactReady: false,
            isFinisherReady: false,
            isInMeleeCombo: false,
            canStartMeleeCombo: true,    // Both mana >= 50
            blackMana: 50,
            whiteMana: 50,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.EnchantedRiposte.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Jolt3_AsFiller_WithDualcast_NoProcs_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Jolt3.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // Dualcast active, no procs — should cast Jolt3 as dualcast filler
        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isResolutionReady: false,
            isScorchReady: false,
            isGrandImpactReady: false,
            isFinisherReady: false,
            isInMeleeCombo: false,
            canStartMeleeCombo: false,
            hasDualcast: true,
            hasVerfire: false,
            hasVerstone: false,
            hasAnyProc: false,
            blackMana: 30,
            whiteMana: 30,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Jolt3.ActionId),
            It.IsAny<ulong>()), Times.Once);
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
