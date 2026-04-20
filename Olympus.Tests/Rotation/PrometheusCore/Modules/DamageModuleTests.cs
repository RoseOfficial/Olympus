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
using Olympus.Timeline;
using Olympus.Timeline.Models;

namespace Olympus.Tests.Rotation.PrometheusCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = PrometheusTestContext.Create(inCombat: false, canExecuteGcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_CannotExecuteGcd_ReturnsFalse()
    {
        var context = PrometheusTestContext.Create(inCombat: true, canExecuteGcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    #region Overheated State

    [Fact]
    public void TryExecute_Overheated_UsesBlazingShot_AtLevel68Plus()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MCHActions.BlazingShot.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.BlazingShot.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PrometheusTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 68,
            isOverheated: true,
            overheatRemaining: 5f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.BlazingShot.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Overheated_UsesHeatBlast_BeforeLevel68()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MCHActions.HeatBlast.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.HeatBlast.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PrometheusTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 67,
            isOverheated: true,
            overheatRemaining: 5f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.HeatBlast.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Tool Actions

    [Fact]
    public void TryExecute_Drill_FiresWhenReady_WithCharges()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MCHActions.Drill.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Drill.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PrometheusTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isOverheated: false,
            hasFullMetalMachinist: false,
            hasExcavatorReady: false,
            drillCharges: 1, // Drill has charges
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Drill.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_AirAnchor_FiresWhenReady_BatteryBelow80()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(MCHActions.AirAnchor.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.AirAnchor.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PrometheusTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isOverheated: false,
            battery: 60, // Well below 80 cap
            hasFullMetalMachinist: false,
            hasExcavatorReady: false,
            drillCharges: 0, // No drill
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.AirAnchor.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_AirAnchor_BlockedWhenBatteryAbove80()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Setup combo fallback so something fires
        actionService.Setup(x => x.IsActionReady(MCHActions.HeatedSplitShot.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = PrometheusTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isOverheated: false,
            battery: 85, // Above 80 cap — Air Anchor would overcap
            hasFullMetalMachinist: false,
            hasExcavatorReady: false,
            drillCharges: 0,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.AirAnchor.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_ChainSaw_FiresWhenReady_BatteryBelow80()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(MCHActions.ChainSaw.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.ChainSaw.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PrometheusTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isOverheated: false,
            battery: 60,
            hasFullMetalMachinist: false,
            hasExcavatorReady: false,
            drillCharges: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.ChainSaw.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ChainSaw_BlockedWhenBatteryAbove80()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Setup combo fallback
        actionService.Setup(x => x.IsActionReady(MCHActions.HeatedSplitShot.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var context = PrometheusTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isOverheated: false,
            battery: 85,
            hasFullMetalMachinist: false,
            hasExcavatorReady: false,
            drillCharges: 0,
            actionService: actionService,
            targetingService: targeting);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.ChainSaw.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Proc Actions

    [Fact]
    public void TryExecute_FullMetalField_FiresWhenProcActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MCHActions.FullMetalField.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.FullMetalField.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PrometheusTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isOverheated: false,
            hasFullMetalMachinist: true, // Proc active
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.FullMetalField.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Excavator_FiresWhenProcActive()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MCHActions.Excavator.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Excavator.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PrometheusTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isOverheated: false,
            hasFullMetalMachinist: false,
            hasExcavatorReady: true, // Proc active
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.Excavator.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Combo

    [Fact]
    public void TryExecute_Combo_UsesHeatedSplitShot_WhenNoHigherPriority()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MCHActions.HeatedSplitShot.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.HeatedSplitShot.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = PrometheusTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isOverheated: false,
            hasFullMetalMachinist: false,
            hasExcavatorReady: false,
            battery: 0,
            drillCharges: 0,
            comboStep: 0,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == MCHActions.HeatedSplitShot.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region MechanicCastGate wiring

    /// <summary>
    /// Smoke test: MechanicCastGate is wired at every ExecuteGcd call site.
    /// All MCH GCDs have CastTime=0 so the gate is a practical no-op at cap,
    /// but this verifies the module completes without exception when a timeline
    /// with an imminent raidwide is active, and that ExecuteGcd still fires
    /// normally (because CastTime=0 means ShouldBlock returns false).
    /// </summary>
    [Fact]
    public void CastTimeGateWired_Smoke_FillerFiresEvenWithImminentRaidwide()
    {
        // Arrange: timeline active with imminent raidwide in 1.5s
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Timeline.EnableMechanicAwareCasting = true;
        config.Timeline.EnableTimelinePredictions = true;
        config.Timeline.TimelineConfidenceThreshold = 0.8f;

        var timelineMock = new Mock<ITimelineService>();
        timelineMock.Setup(x => x.IsActive).Returns(true);
        timelineMock.Setup(x => x.Confidence).Returns(0.9f);
        timelineMock.Setup(x => x.NextRaidwide).Returns(
            new MechanicPrediction(1.5f, TimelineEntryType.Raidwide, "Exaflare", 0.9f));

        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(MCHActions.HeatedSplitShot.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.HeatedSplitShot.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // All MCH GCDs are instant (CastTime=0), so the gate does NOT block.
        // Filler path (HeatedSplitShot) should still execute.
        var context = PrometheusTestContext.Create(
            config: config,
            inCombat: true,
            canExecuteGcd: true,
            level: 100,
            isOverheated: false,
            hasFullMetalMachinist: false,
            hasExcavatorReady: false,
            battery: 0,
            drillCharges: 0,
            comboStep: 0,
            actionService: actionService,
            targetingService: targeting,
            timelineService: timelineMock.Object);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: gate does not block instants, so filler fires normally
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == MCHActions.HeatedSplitShot.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
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
