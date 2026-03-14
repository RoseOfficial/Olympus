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

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = TerpsichoreTestContext.Create(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_WhenNotDancing_ReturnsFalse()
    {
        var context = TerpsichoreTestContext.Create(inCombat: true, canExecuteOgcd: false, isDancing: false);
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

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            isDancing: false,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    #region Dance Step Execution

    [Fact]
    public void TryExecute_DanceStep_ExecutesCurrentStep_WhenDancing()
    {
        // During a dance, steps are executed as GCDs (no oGCD needed)
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Emboite.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            isDancing: true,
            stepIndex: 0,
            currentStep: (byte)DNCActions.DanceStep.Emboite, // Step 1 = Emboite
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Emboite.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_DanceFinish_ExecutesStandardFinish_After2Steps()
    {
        // After 2 steps completed (stepIndex >= 2), execute StandardFinish
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.StandardFinish.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.StandardFinish.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            isDancing: true,
            stepIndex: 2, // 2 steps done
            currentStep: 0, // No more steps to execute
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.StandardFinish.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_DanceFinish_ExecutesTechnicalFinish_After4Steps()
    {
        // After 4 steps completed (stepIndex >= 4), execute TechnicalFinish
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
            level: 70, // TechnicalFinish MinLevel = 70
            isDancing: true,
            stepIndex: 4, // All 4 steps done
            currentStep: 0, // No more steps
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.TechnicalFinish.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Technical Step

    [Fact]
    public void TryExecute_TechnicalStep_FiresWhenReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.TechnicalStep.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.TechnicalStep.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 70, // TechnicalStep MinLevel = 70
            isDancing: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.TechnicalStep.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_TechnicalStep_SkipsWhenAlreadyDancing()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(DNCActions.TechnicalStep.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: true,
            level: 70,
            isDancing: true, // Already dancing
            currentStep: (byte)DNCActions.DanceStep.Emboite, // Mid-dance
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.TechnicalStep.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Standard Step

    [Fact]
    public void TryExecute_StandardStep_FiresWhenReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // TechnicalStep not ready (not blocking StandardStep)
        actionService.Setup(x => x.IsActionReady(DNCActions.TechnicalStep.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DNCActions.StandardStep.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.StandardStep.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 15, // StandardStep MinLevel = 15, below TechnicalStep (70)
            isDancing: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.StandardStep.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Devilment

    [Fact]
    public void TryExecute_Devilment_FiresAfterTechnicalFinish()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Technical step not ready (so Devilment precondition passes)
        actionService.Setup(x => x.IsActionReady(DNCActions.TechnicalStep.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DNCActions.Devilment.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Devilment.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 62, // Devilment MinLevel
            isDancing: false,
            hasTechnicalFinish: true, // Technical finish just applied
            hasDevilment: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Devilment.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Devilment_SkipsWhenAlreadyActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 100,
            isDancing: false,
            hasTechnicalFinish: true,
            hasDevilment: true, // Already active
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Devilment.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Fan Dance

    [Fact]
    public void TryExecute_FanDance_FiresAt4Feathers()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(DNCActions.FanDance.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.FanDance.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 100,
            isDancing: false,
            feathers: 4, // At cap — must spend
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasThreefoldFanDance: false,
            hasFourfoldFanDance: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.FanDance.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_FanDance_FiresDuringBurst_At2Feathers()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(DNCActions.FanDance.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DNCActions.FanDance.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 100,
            isDancing: false,
            feathers: 2, // Below cap but during burst
            hasDevilment: true, // In burst window
            hasThreefoldFanDance: false,
            hasFourfoldFanDance: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.FanDance.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_FanDance_HoldsWhenNotAtCapAndNotInBurst()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 100,
            isDancing: false,
            feathers: 2, // Below cap
            hasDevilment: false, // Not in burst
            hasTechnicalFinish: false,
            hasThreefoldFanDance: false,
            hasFourfoldFanDance: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.FanDance.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Flourish

    [Fact]
    public void TryExecute_Flourish_SkipsWhenSilkenSymmetryActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(DNCActions.Flourish.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            level: 100,
            isDancing: false,
            hasDevilment: true,
            hasSilkenSymmetry: true, // Existing proc — don't overcap
            hasSilkenFlow: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Flourish.ActionId),
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
