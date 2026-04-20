using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.CirceCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Timeline;
using Olympus.Timeline.Models;

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
        // We pass base Riposte (7504) to UseAction; the game upgrades server-side to
        // Enchanted Riposte (7527) when mana qualifies. UseAction rejects the Enchanted
        // replacement IDs for the mid-combo steps, so we use base IDs throughout.
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Riposte.ActionId),
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
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Riposte.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_MeleeComboStep1_FiresBaseZwerchhauNotEnchanted()
    {
        // Regression: UseAction rejects the Enchanted replacement ID (7528), silently dropping
        // the combo after Enchanted Riposte. Passing base Zwerchhau (7512) lets the game upgrade
        // server-side. Verify we do NOT call ExecuteGcd with the Enchanted replacement ID.
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Zwerchhau.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isResolutionReady: false,
            isScorchReady: false,
            isGrandImpactReady: false,
            isFinisherReady: false,
            isInMeleeCombo: true,
            meleeComboStep: 1, // Zwerchhau next
            blackMana: 30,
            whiteMana: 30,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Zwerchhau.ActionId),
            It.IsAny<ulong>()), Times.Once);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.EnchantedZwerchhau.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_MeleeComboStep2_FiresBaseRedoublementNotEnchanted()
    {
        // Regression: UseAction rejects Enchanted Redoublement replacement ID (7529).
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Redoublement.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isResolutionReady: false,
            isScorchReady: false,
            isGrandImpactReady: false,
            isFinisherReady: false,
            isInMeleeCombo: true,
            meleeComboStep: 2, // Redoublement next
            blackMana: 15,
            whiteMana: 15,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Redoublement.ActionId),
            It.IsAny<ulong>()), Times.Once);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.EnchantedRedoublement.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Jolt3_AsFiller_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Jolt3.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // No dualcast, no acceleration, no procs — should hardcast Jolt3 as filler
        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isResolutionReady: false,
            isScorchReady: false,
            isGrandImpactReady: false,
            isFinisherReady: false,
            isInMeleeCombo: false,
            canStartMeleeCombo: false,
            hasDualcast: false,
            hasAcceleration: false,
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

    [Fact]
    public void JoltIII_BlockedWhenRaidwideImminent()
    {
        // Arrange: raidwide in 1.5s, Jolt III cast = 2.0s → deadline 2.5s → should block
        var config = CirceTestContext.CreateDefaultRdmConfiguration();
        config.Timeline.EnableMechanicAwareCasting = true;
        config.Timeline.EnableTimelinePredictions = true;
        config.Timeline.TimelineConfidenceThreshold = 0.8f;

        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var timelineMock = new Mock<ITimelineService>();
        timelineMock.Setup(x => x.IsActive).Returns(true);
        timelineMock.Setup(x => x.Confidence).Returns(0.9f);
        timelineMock.Setup(x => x.NextRaidwide).Returns(
            new MechanicPrediction(1.5f, TimelineEntryType.Raidwide, "Exaflare", 0.9f));

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        // Put module in Jolt III filler path: no special states, no instant casts, no procs
        var context = CirceTestContext.Create(
            config: config,
            inCombat: true,
            canExecuteGcd: true,
            isResolutionReady: false,
            isScorchReady: false,
            isGrandImpactReady: false,
            isFinisherReady: false,
            isInMeleeCombo: false,
            canStartMeleeCombo: false,
            hasDualcast: false,
            hasAcceleration: false,
            hasSwiftcast: false,
            hasInstantCast: false,
            hasVerfire: false,
            hasVerstone: false,
            hasAnyProc: false,
            blackMana: 30,
            whiteMana: 30,
            actionService: actionService,
            targetingService: targeting,
            timelineService: timelineMock.Object);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert: gate blocks Jolt III, so ExecuteGcd must never fire
        actionService.Verify(
            x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()),
            Times.Never);
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
