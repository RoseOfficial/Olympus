using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.PrometheusCore.Context;
using Olympus.Rotation.PrometheusCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.PrometheusCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    private static IPrometheusContext CreateContext(
        bool inCombat = true,
        bool canExecuteOgcd = true,
        byte level = 100,
        int heat = 0,
        int battery = 0,
        bool isOverheated = false,
        bool isQueenActive = false,
        bool hasWildfire = false,
        bool hasReassemble = false,
        bool hasFullMetalMachinist = false,
        bool hasExcavatorReady = false,
        int drillCharges = 0,
        int reassembleCharges = 0,
        int gaussRoundCharges = 0,
        int ricochetCharges = 0,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return PrometheusTestContext.Create(
            inCombat: inCombat,
            canExecuteGcd: false,
            canExecuteOgcd: canExecuteOgcd,
            level: level,
            heat: heat,
            battery: battery,
            isOverheated: isOverheated,
            isQueenActive: isQueenActive,
            hasWildfire: hasWildfire,
            hasReassemble: hasReassemble,
            hasFullMetalMachinist: hasFullMetalMachinist,
            hasExcavatorReady: hasExcavatorReady,
            drillCharges: drillCharges,
            reassembleCharges: reassembleCharges,
            gaussRoundCharges: gaussRoundCharges,
            ricochetCharges: ricochetCharges,
            actionService: actionService,
            targetingService: targetingService);
    }

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = PrometheusTestContext.Create(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        var context = PrometheusTestContext.Create(inCombat: true, canExecuteOgcd: false);
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

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("No target", context.Debug.BuffState);
    }

    #region Wildfire

    [Fact]
    public void TryExecute_Wildfire_FiresWhenOverheated()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(MCHActions.Wildfire.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Wildfire.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            isOverheated: true,
            hasWildfire: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Wildfire.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Wildfire_FiresWhenHeat50AndHyperchargeReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(MCHActions.Hypercharge.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(MCHActions.Wildfire.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Wildfire.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            heat: 50,
            hasWildfire: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Wildfire.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Wildfire_SkipsWhenAlreadyActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            isOverheated: true,
            hasWildfire: true, // Already active
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Wildfire.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Hypercharge

    [Fact]
    public void TryExecute_Hypercharge_FiresWhenHeat50_NoToolsReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // All tools NOT ready (so Hypercharge won't be blocked)
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(MCHActions.Hypercharge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Hypercharge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            heat: 50,
            isOverheated: false,
            drillCharges: 0, // Drill not ready
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Hypercharge.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Hypercharge_BlockedByDrillReady()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(MCHActions.Drill.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            heat: 50,
            isOverheated: false,
            drillCharges: 1, // Drill has charges
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Hypercharge.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Hypercharge_SkipsWhenAlreadyOverheated()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            heat: 50,
            isOverheated: true, // Already overheated
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Hypercharge.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Hypercharge_SkipsWhenHeatBelow50()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            heat: 49, // Below threshold
            isOverheated: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Hypercharge.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Automaton Queen

    [Fact]
    public void TryExecute_AutomatonQueen_SummonsAt100Battery()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(MCHActions.AutomatonQueen.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.AutomatonQueen.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            battery: 100, // Full battery
            isQueenActive: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.AutomatonQueen.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_AutomatonQueen_SummonsAt90Battery()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(MCHActions.AutomatonQueen.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.AutomatonQueen.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            battery: 90, // Near max
            isQueenActive: false,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
    }

    [Fact]
    public void TryExecute_AutomatonQueen_SkipsWhenQueenAlreadyActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            battery: 100,
            isQueenActive: true, // Already active
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.AutomatonQueen.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_AutomatonQueen_SkipsWhenBatteryBelow50()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            battery: 40, // Below minimum
            isQueenActive: false,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.AutomatonQueen.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Gauss Round / Ricochet

    [Fact]
    public void TryExecute_GaussRound_FiresDuringOverheated()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(MCHActions.DoubleCheck.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.DoubleCheck.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            canExecuteOgcd: true,
            level: 100,
            isOverheated: true,
            gaussRoundCharges: 1,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.DoubleCheck.ActionId),
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
