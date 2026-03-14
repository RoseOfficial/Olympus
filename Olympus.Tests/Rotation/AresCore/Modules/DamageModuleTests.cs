using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.AresCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteGcd: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void TryExecute_InCombat_NoTargetAnywhere_ReturnsFalse()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = CreateContext(inCombat: true, canExecuteGcd: true, canExecuteOgcd: false,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("No target", context.Debug.DamageState);
    }

    [Fact]
    public void TryExecute_InCombat_TargetOutOfMeleeRange_GcdReady_ExecutesTomahawk()
    {
        var enemy = CreateMockEnemy(objectId: 12345UL);
        var targeting = MockBuilders.CreateMockTargetingService();

        // Melee range (FindEnemyForAction via context) → null
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        // Engage range (20f) → found
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.Is<float>(r => r > 5f),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(WARActions.Tomahawk.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WARActions.Tomahawk.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 15,
            targetingService: targeting,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.Tomahawk.ActionId),
            enemy.Object.GameObjectId), Times.Once);
    }

    [Fact]
    public void TryExecute_InCombat_GcdReady_ExecutesHeavySwing()
    {
        var enemy = CreateMockEnemy();
        var targeting = SetupMeleeRangeTargeting(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(WARActions.HeavySwing.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WARActions.HeavySwing.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            comboStep: 0,
            targetingService: targeting,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.HeavySwing.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ComboStep2_AfterHeavySwing_ExecutesMaim()
    {
        var enemy = CreateMockEnemy();
        var targeting = SetupMeleeRangeTargeting(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(WARActions.Maim.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WARActions.Maim.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 10,
            comboStep: 1,
            lastComboAction: WARActions.HeavySwing.ActionId,
            targetingService: targeting,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.Maim.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_FellCleave_DuringInnerRelease_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = SetupMeleeRangeTargeting(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(WARActions.FellCleave.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WARActions.FellCleave.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 70,
            beastGauge: 0, // No gauge, but Inner Release makes it free
            hasInnerRelease: true,
            innerReleaseStacks: 3,
            targetingService: targeting,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
    }

    [Fact]
    public void TryExecute_InnerChaos_WhenNascentChaosActive_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = SetupMeleeRangeTargeting(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(WARActions.InnerChaos.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WARActions.InnerChaos.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 80,
            hasNascentChaos: true,
            enemyCount: 1,
            targetingService: targeting,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.InnerChaos.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_PrimalRend_WhenRendReadyActive_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = SetupMeleeRangeTargeting(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(WARActions.PrimalRend.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WARActions.PrimalRend.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 90,
            hasPrimalRendReady: true,
            targetingService: targeting,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.PrimalRend.ActionId),
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

    private static Mock<ITargetingService> SetupMeleeRangeTargeting(Mock<IBattleNpc>? enemy = null)
    {
        enemy ??= CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();

        // FindEnemyForAction → melee range enemy
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        // FindEnemy → same enemy
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        return targeting;
    }

    private static IAresContext CreateContext(
        bool inCombat,
        bool canExecuteGcd,
        bool canExecuteOgcd,
        byte level = 100,
        int comboStep = 0,
        uint lastComboAction = 0,
        int beastGauge = 0,
        bool hasInnerRelease = false,
        int innerReleaseStacks = 0,
        bool hasNascentChaos = false,
        bool hasPrimalRendReady = false,
        bool hasPrimalRuinationReady = false,
        bool hasSurgingTempest = true,
        float surgingTempestRemaining = 30f,
        int enemyCount = 1,
        Mock<ITargetingService>? targetingService = null,
        Mock<IActionService>? actionService = null)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteGcd: canExecuteGcd, canExecuteOgcd: canExecuteOgcd);

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var config = AresTestContext.CreateDefaultWarriorConfiguration();

        var mock = new Mock<IAresContext>();

        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(canExecuteGcd);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);

        // WAR-specific state
        mock.Setup(x => x.BeastGauge).Returns(beastGauge);
        mock.Setup(x => x.HasInnerRelease).Returns(hasInnerRelease);
        mock.Setup(x => x.InnerReleaseStacks).Returns(innerReleaseStacks);
        mock.Setup(x => x.HasNascentChaos).Returns(hasNascentChaos);
        mock.Setup(x => x.HasPrimalRendReady).Returns(hasPrimalRendReady);
        mock.Setup(x => x.HasPrimalRuinationReady).Returns(hasPrimalRuinationReady);
        mock.Setup(x => x.HasSurgingTempest).Returns(hasSurgingTempest);
        mock.Setup(x => x.SurgingTempestRemaining).Returns(surgingTempestRemaining);
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);

        targetingService.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemyCount);

        var debugState = new AresDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    #endregion
}
