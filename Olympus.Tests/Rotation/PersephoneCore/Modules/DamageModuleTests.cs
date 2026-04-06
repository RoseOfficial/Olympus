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

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = PersephoneTestContext.Create(inCombat: false, canExecuteGcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_GcdNotReady_ReturnsFalse()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var context = PersephoneTestContext.Create(
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

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_DemiSummonGcd_DuringBahamutPhase_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Demi-summon GCDs now bypass ExecuteGcd and use ActionManager directly
        // (base Ruin III action, game replaces to Astral Impulse during Bahamut).
        // In unit tests without a real ActionManager, the unsafe path falls through
        // to filler which executes the base Ruin III via mocked ExecuteGcd.
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SMNActions.Ruin3.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isBahamutActive: true,
            isDemiSummonActive: true,
            demiSummonTimer: 12f,
            demiSummonGcdsRemaining: 4,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.Ruin3.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_RubyRite_DuringIfritAttunement_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SMNActions.RubyRite.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isDemiSummonActive: false,
            isIfritAttuned: true,
            currentAttunement: 1,
            attunementStacks: 2,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.RubyRite.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TopazRite_DuringTitanAttunement_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SMNActions.TopazRite.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isDemiSummonActive: false,
            isTitanAttuned: true,
            currentAttunement: 2,
            attunementStacks: 4,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.TopazRite.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SummonTitan_WhenPrimalsAvailable_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SMNActions.SummonTitan.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isDemiSummonActive: false,
            isIfritAttuned: false,
            isTitanAttuned: false,
            isGarudaAttuned: false,
            attunementStacks: 0,
            primalsAvailable: 3,
            canSummonTitan: true,
            canSummonGaruda: true,
            canSummonIfrit: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.SummonTitan.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Ruin4_WithFurtherRuin_BetweenPhases_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SMNActions.Ruin4.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isDemiSummonActive: false,
            isIfritAttuned: false,
            isTitanAttuned: false,
            isGarudaAttuned: false,
            attunementStacks: 0,
            primalsAvailable: 0,    // No primals — between phases
            hasFurtherRuin: true,
            furtherRuinRemaining: 3f,   // About to expire
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.Ruin4.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Ruin3_AsFiller_BetweenPhases_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SMNActions.Ruin3.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PersephoneTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isDemiSummonActive: false,
            isIfritAttuned: false,
            isTitanAttuned: false,
            isGarudaAttuned: false,
            attunementStacks: 0,
            primalsAvailable: 0,
            hasFurtherRuin: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.Ruin3.ActionId),
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
