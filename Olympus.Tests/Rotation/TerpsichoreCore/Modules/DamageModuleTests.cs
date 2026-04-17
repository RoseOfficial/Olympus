using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.TerpsichoreCore.Context;
using Olympus.Rotation.TerpsichoreCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.TerpsichoreCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = TerpsichoreTestContext.Create(inCombat: false, canExecuteGcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_CannotExecuteGcd_ReturnsFalse()
    {
        var context = TerpsichoreTestContext.Create(inCombat: true, canExecuteGcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenDancing_ReturnsFalse()
    {
        // DamageModule yields to BuffModule when dancing
        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isDancing: true); // Dancing → PreExecuteChecks returns false
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    #region Saber Dance

    [Fact]
    public void TryExecute_SaberDance_FiresAt85Esprit_Overcap()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.SaberDance.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.SaberDance.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isDancing: false,
            esprit: 85, // At overcap threshold
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasDanceOfTheDawnReady: false,
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasFlourishingFinish: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.SaberDance.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SaberDance_FiresAt50Esprit_DuringDevilment()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.SaberDance.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.SaberDance.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isDancing: false,
            esprit: 50, // Low esprit but in burst window
            hasDevilment: true, // In burst
            hasTechnicalFinish: false,
            hasDanceOfTheDawnReady: false,
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasFlourishingFinish: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.SaberDance.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SaberDance_SkipsAt60Esprit_OutsideBurst()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Setup filler so something fires
        actionService.Setup(x => x.IsActionReady(DNCActions.Cascade.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isDancing: false,
            esprit: 60, // Below 80 and not in burst
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasDanceOfTheDawnReady: false,
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasFlourishingFinish: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.SaberDance.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Starfall Dance

    [Fact]
    public void TryExecute_StarfallDance_FiresWhenFlourishingStarfallActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.StarfallDance.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.StarfallDance.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 90,
            isDancing: false,
            hasFlourishingStarfall: true, // Proc active
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.StarfallDance.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Proc Consumers

    [Fact]
    public void TryExecute_Fountainfall_FiresWhenSilkenFlowActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.Fountainfall.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Fountainfall.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isDancing: false,
            esprit: 0,
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasFlourishingFinish: false,
            hasDanceOfTheDawnReady: false,
            hasSilkenFlow: true, // Fountainfall proc
            hasSilkenSymmetry: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Fountainfall.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ReverseCascade_FiresWhenSilkenSymmetryActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.ReverseCascade.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.ReverseCascade.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isDancing: false,
            esprit: 0,
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasFlourishingFinish: false,
            hasDanceOfTheDawnReady: false,
            hasSilkenFlow: false,
            hasSilkenSymmetry: true, // ReverseCascade proc
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.ReverseCascade.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Filler

    [Fact]
    public void TryExecute_Cascade_FiresWhenNoHigherPriority()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.Cascade.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Cascade.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isDancing: false,
            esprit: 0,
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasFlourishingFinish: false,
            hasDanceOfTheDawnReady: false,
            hasSilkenFlow: false,
            hasSilkenSymmetry: false,
            comboStep: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Cascade.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

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
