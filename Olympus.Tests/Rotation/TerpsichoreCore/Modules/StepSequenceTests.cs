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

/// <summary>
/// Tests for the complete dance step sequence flow:
/// 1. Dance steps execute in order while dancing
/// 2. Technical Finish fires after all 4 steps complete
/// 3. Tillana fires after Technical Finish when Flourishing Finish is active
/// </summary>
public class StepSequenceTests
{
    private readonly BuffModule _buffModule = new();
    private readonly DamageModule _damageModule = new();

    #region Dance Step Execution While Dancing

    [Theory]
    [InlineData((byte)DNCActions.DanceStep.Emboite)]
    [InlineData((byte)DNCActions.DanceStep.Entrechat)]
    [InlineData((byte)DNCActions.DanceStep.Jete)]
    [InlineData((byte)DNCActions.DanceStep.Pirouette)]
    public void DanceStep_WhenDancingWithPendingStep_ExecutesCorrectStep(byte step)
    {
        var stepAction = DNCActions.GetStepAction(step)!;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == stepAction.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            isDancing: true,
            stepIndex: 0,
            currentStep: step,
            actionService: actionService);

        var result = _buffModule.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == stepAction.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void DanceStep_WhenDancingButGcdNotReady_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: false,
            isDancing: true,
            stepIndex: 1,
            currentStep: (byte)DNCActions.DanceStep.Jete,
            actionService: actionService);

        var result = _buffModule.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Technical Finish After 4 Steps

    [Fact]
    public void TechnicalFinish_After4Steps_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.TechnicalFinish.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.TechnicalFinish.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 70, // TechnicalFinish MinLevel
            isDancing: true,
            stepIndex: 4, // All 4 steps done
            currentStep: 0, // No more steps pending
            actionService: actionService);

        var result = _buffModule.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.TechnicalFinish.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TechnicalFinish_After4Steps_OnCooldown_FallsBackToStandardFinish()
    {
        // Edge case: Technical Finish not ready, but Standard is (stepIndex >= 2)
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.TechnicalFinish.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DNCActions.StandardFinish.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.StandardFinish.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 70,
            isDancing: true,
            stepIndex: 4,
            currentStep: 0,
            actionService: actionService);

        var result = _buffModule.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.StandardFinish.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Tillana After Technical Finish

    [Fact]
    public void Tillana_WithFlourishingFinish_Fires()
    {
        // After Technical Finish, Flourishing Finish buff grants Tillana
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.Tillana.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Tillana.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 82, // Tillana MinLevel
            isDancing: false, // Finished dancing
            hasFlourishingFinish: true, // Proc from Technical Finish
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasDanceOfTheDawnReady: false,
            esprit: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _damageModule.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Tillana.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void Tillana_WithoutFlourishingFinish_DoesNotFire()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.Tillana.ActionId)).Returns(true);
        // Setup filler so module can proceed
        actionService.Setup(x => x.IsActionReady(DNCActions.Cascade.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 82,
            isDancing: false,
            hasFlourishingFinish: false, // No proc
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasDanceOfTheDawnReady: false,
            esprit: 0,
            hasSilkenFlow: false,
            hasSilkenSymmetry: false,
            comboStep: 0,
            actionService: actionService,
            targetingService: targeting);

        _damageModule.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Tillana.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void Tillana_BelowMinLevel_DoesNotFire()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.Cascade.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 70, // Below Tillana MinLevel of 82
            isDancing: false,
            hasFlourishingFinish: true, // Has proc but below level
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasDanceOfTheDawnReady: false,
            esprit: 0,
            hasSilkenFlow: false,
            hasSilkenSymmetry: false,
            comboStep: 0,
            actionService: actionService,
            targetingService: targeting);

        _damageModule.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Tillana.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void Tillana_Disabled_DoesNotFire()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableTillana = false;

        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.Cascade.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = TerpsichoreTestContext.Create(
            config: config,
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 100,
            isDancing: false,
            hasFlourishingFinish: true,
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasDanceOfTheDawnReady: false,
            esprit: 0,
            hasSilkenFlow: false,
            hasSilkenSymmetry: false,
            comboStep: 0,
            actionService: actionService,
            targetingService: targeting);

        _damageModule.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Tillana.ActionId),
            It.IsAny<ulong>()), Times.Never);
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
