using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HephaestusCore.Modules;

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
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = CreateContext(inCombat: true, canExecuteGcd: true, canExecuteOgcd: false,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("No target", context.Debug.DamageState);
    }

    [Fact]
    public void TryExecute_InCombat_MeleeTarget_ComboStep0_ExecutesKeenEdge()
    {
        var enemy = CreateMockEnemy(objectId: 12345UL);
        var targeting = MockBuilders.CreateMockTargetingService();

        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == GNBActions.KeenEdge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 80,
            comboStep: 0,
            targetingService: targeting,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == GNBActions.KeenEdge.ActionId),
            enemy.Object.GameObjectId), Times.Once);
    }

    [Fact]
    public void TryExecute_InCombat_TargetOutOfMeleeRange_GcdReady_ExecutesLightningShot()
    {
        var enemy = CreateMockEnemy(objectId: 12345UL);
        var targeting = MockBuilders.CreateMockTargetingService();

        // Melee range (FindEnemyForAction) → null
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<uint>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        // Wide range (FindEnemy) → enemy found
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(GNBActions.LightningShot.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == GNBActions.LightningShot.ActionId),
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
            It.Is<ActionDefinition>(a => a.ActionId == GNBActions.LightningShot.ActionId),
            enemy.Object.GameObjectId), Times.Once);
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

    private static IHephaestusContext CreateContext(
        bool inCombat,
        bool canExecuteGcd,
        bool canExecuteOgcd,
        byte level = 80,
        int comboStep = 0,
        uint lastComboAction = 0,
        int cartridges = 0,
        int gnashingFangStep = 0,
        Mock<ITargetingService>? targetingService = null,
        Mock<IActionService>? actionService = null)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Tank.EnableDamage = true;

        var mock = new Mock<IHephaestusContext>();

        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(canExecuteGcd);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);

        // GNB-specific — safe defaults
        mock.Setup(x => x.Cartridges).Returns(cartridges);
        mock.Setup(x => x.CanUseGnashingFang).Returns(cartridges >= 1);
        mock.Setup(x => x.CanUseDoubleDown).Returns(cartridges >= 2);
        mock.Setup(x => x.HasMaxCartridges).Returns(cartridges >= 3);
        mock.Setup(x => x.HasNoMercy).Returns(false);
        mock.Setup(x => x.NoMercyRemaining).Returns(0f);
        mock.Setup(x => x.IsInGnashingFangCombo).Returns(gnashingFangStep > 0 && gnashingFangStep < 3);
        mock.Setup(x => x.GnashingFangStep).Returns(gnashingFangStep);
        mock.Setup(x => x.HasAnyContinuationReady).Returns(false);
        mock.Setup(x => x.IsReadyToRip).Returns(false);
        mock.Setup(x => x.IsReadyToTear).Returns(false);
        mock.Setup(x => x.IsReadyToGouge).Returns(false);
        mock.Setup(x => x.IsReadyToBlast).Returns(false);
        mock.Setup(x => x.IsReadyToBrand).Returns(false);
        mock.Setup(x => x.IsReadyToReign).Returns(false);
        mock.Setup(x => x.HasSonicBreakDot).Returns(false);
        mock.Setup(x => x.HasBowShockDot).Returns(false);
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);
        mock.Setup(x => x.ComboTimeRemaining).Returns(30f);

        var debugState = new HephaestusDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        targetingService.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);

        return mock.Object;
    }

    #endregion
}
