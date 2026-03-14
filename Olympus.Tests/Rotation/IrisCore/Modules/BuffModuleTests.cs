using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.IrisCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.IrisCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = IrisTestContext.Create(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_OgcdNotReady_ReturnsFalse()
    {
        var context = IrisTestContext.Create(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_Madeen_WhenMadeenReady_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.RetributionOfTheMadeen.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.RetributionOfTheMadeen.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_MogOfTheAges_WhenMogReady_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.MogOfTheAges.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,   // Madeen not ready — uses Mog instead
            mogReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.MogOfTheAges.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_StarryMuse_WithLandscapeCanvas_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarryMuse.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: true,
            hasLandscapeCanvas: true,
            hasStarryMuse: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarryMuse.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_StarryMuse_WithoutLandscapeCanvas_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: true,
            hasLandscapeCanvas: false,   // No landscape — cannot use Starry Muse
            hasStarryMuse: false,
            actionService: actionService);

        Assert.False(_module.TryExecute(context, isMoving: false));
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarryMuse.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LivingMuse_WithCreatureCanvas_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // PomMuse is the default at Lv.100 with Pom creature motif
        actionService.Setup(x => x.ExecuteOgcd(
                It.IsAny<ActionDefinition>(),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: true,
            hasCreatureCanvas: true,
            creatureMotifType: PCTActions.CreatureMotifType.Pom,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.IsAny<ActionDefinition>(),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_NoBuffsNeeded_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            madeenReady: false,
            mogReady: false,
            starryMuseReady: false,
            livingMuseReady: false,
            strikingMuseReady: false,
            subtractivePaletteReady: false,
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
