using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.PersephoneCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.PersephoneCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = PersephoneTestContext.Create(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_OgcdNotReady_ReturnsFalse()
    {
        var context = PersephoneTestContext.Create(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_EnkindleBahamut_DuringBahamutPhase_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SMNActions.EnkindleBahamut.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            isBahamutActive: true,
            isDemiSummonActive: true,
            enkindleReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.EnkindleBahamut.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_MountainBuster_WithTitansFavor_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SMNActions.MountainBuster.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            isDemiSummonActive: false,   // Not in demi phase
            enkindleReady: false,
            astralFlowReady: false,
            hasTitansFavor: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.MountainBuster.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SearingLight_DuringBahamutPhase_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SMNActions.SearingLight.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            isDemiSummonActive: true,
            isBahamutActive: true,
            enkindleReady: false,
            astralFlowReady: false,
            hasTitansFavor: false,
            searingLightReady: true,
            hasSearingLight: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.SearingLight.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SearingLight_NotInDemiPhase_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            isDemiSummonActive: false,   // Not in demi phase
            enkindleReady: false,
            astralFlowReady: false,
            hasTitansFavor: false,
            searingLightReady: true,
            hasSearingLight: false,
            actionService: actionService);

        Assert.False(_module.TryExecute(context, isMoving: false));
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.SearingLight.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_EnergyDrain_WhenAetherflowEmpty_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SMNActions.EnergyDrain.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            isDemiSummonActive: false,
            enkindleReady: false,
            astralFlowReady: false,
            hasTitansFavor: false,
            searingLightReady: false,
            hasSearingLight: false,
            energyDrainReady: true,
            hasAetherflow: false,         // Empty aetherflow
            aetherflowStacks: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.EnergyDrain.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_EnergyDrain_WhenAetherflowAvailable_ReturnsFalse()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            isDemiSummonActive: false,
            enkindleReady: false,
            astralFlowReady: false,
            hasTitansFavor: false,
            searingLightReady: false,
            hasSearingLight: false,
            energyDrainReady: true,
            hasAetherflow: true,          // Already have aetherflow
            aetherflowStacks: 2,
            actionService: actionService,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.EnergyDrain.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_NoBuffsNeeded_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            isDemiSummonActive: false,
            enkindleReady: false,
            astralFlowReady: false,
            hasTitansFavor: false,
            searingLightReady: false,
            hasSearingLight: false,
            energyDrainReady: false,
            hasAetherflow: false,
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
