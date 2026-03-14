using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Rotation.CalliopeCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.CalliopeCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CalliopeTestContext.Create(inCombat: false, canExecuteGcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_CannotExecuteGcd_ReturnsFalse()
    {
        var context = CalliopeTestContext.Create(inCombat: true, canExecuteGcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    #region Resonant Arrow (Barrage Follow-up)

    [Fact]
    public void TryExecute_ResonantArrow_FiresWhenReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.ResonantArrow.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.ResonantArrow.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 96,
            hasResonantArrowReady: true,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.ResonantArrow.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ResonantArrow_SkipsWhenNotReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.BurstShot.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 96,
            hasResonantArrowReady: false, // No proc
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.ResonantArrow.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Apex Arrow

    [Fact]
    public void TryExecute_ApexArrow_FiresAt100SoulVoice()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.ApexArrow.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.ApexArrow.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 80,
            soulVoice: 100,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.ApexArrow.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ApexArrow_FiresAt80_DuringBurst()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // RS not on cooldown (can't fire)
        actionService.Setup(x => x.IsActionReady(BRDActions.RagingStrikes.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(BRDActions.ApexArrow.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.ApexArrow.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 80,
            soulVoice: 80,
            hasRagingStrikes: true, // In burst window
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.ApexArrow.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ApexArrow_SkipsWhenBelow80SoulVoice()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        // Setup filler so something fires
        actionService.Setup(x => x.IsActionReady(BRDActions.BurstShot.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 80,
            soulVoice: 60, // Below threshold
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.ApexArrow.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region DoT Management

    [Fact]
    public void TryExecute_AppliesStormbiteWhenMissing()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.Stormbite.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.Stormbite.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 64,
            hasStormbite: false, // DoT not applied
            hasCausticBite: true,
            causticBiteRemaining: 30f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.Stormbite.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_AppliesCausticBiteWhenMissing()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.CausticBite.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.CausticBite.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 64,
            hasStormbite: true, // Stormbite applied
            stormbiteRemaining: 30f,
            hasCausticBite: false, // CausticBite missing
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.CausticBite.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_IronJaws_RefreshesDotsInWindow()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.IronJaws.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.IronJaws.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 56,
            hasCausticBite: true,
            causticBiteRemaining: 5f, // Within refresh window (3-7s)
            hasStormbite: true,
            stormbiteRemaining: 5f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.IronJaws.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    /// <summary>
    /// Regression: DoT should NOT be refreshed if a DoT is missing (use the individual application instead).
    /// Iron Jaws requires BOTH DoTs to be on target.
    /// </summary>
    [Fact]
    public void TryExecute_IronJaws_SkipsWhenOnlyOneDoTPresent()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Stormbite apply should get called instead
        actionService.Setup(x => x.IsActionReady(BRDActions.Stormbite.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 64,
            hasCausticBite: true,
            causticBiteRemaining: 5f,
            hasStormbite: false, // One DoT missing
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.IronJaws.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Refulgent Arrow (Hawk's Eye Proc)

    [Fact]
    public void TryExecute_RefulgentArrow_FiresWhenHawksEyeActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.RefulgentArrow.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.RefulgentArrow.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 70,
            hasHawksEye: true,
            hasCausticBite: true,
            causticBiteRemaining: 30f,
            hasStormbite: true,
            stormbiteRemaining: 30f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.RefulgentArrow.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Filler GCD

    [Fact]
    public void TryExecute_Filler_UsesBurstShot_WhenNoHigherPriority()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(BRDActions.BurstShot.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BRDActions.BurstShot.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CalliopeTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            hasHawksEye: false,
            hasCausticBite: true,
            causticBiteRemaining: 30f,
            hasStormbite: true,
            stormbiteRemaining: 30f,
            soulVoice: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.BurstShot.ActionId),
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
