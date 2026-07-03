using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.PrometheusCore.Abilities;
using Olympus.Rotation.PrometheusCore.Context;
using Olympus.Rotation.PrometheusCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.PrometheusCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Prometheus (Machinist) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class PrometheusTests
{
    #region Module Priority Tests

    [Fact]
    public void ModulePriorities_BuffBeforeDamage()
    {
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void AllModules_HaveCorrectExpectedPriorities()
    {
        Assert.Equal(20, new BuffModule().Priority);
        Assert.Equal(30, new DamageModule().Priority);
    }

    [Fact]
    public void AllModules_HaveExpectedNames()
    {
        Assert.Equal("Buff", new BuffModule().Name);
        Assert.Equal("Damage", new DamageModule().Name);
    }

    #endregion

    #region DebugState Tests

    [Fact]
    public void PrometheusDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new PrometheusDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void PrometheusDebugState_CanBeModified()
    {
        var debugState = new PrometheusDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Drill",
            DamageState = "Tool on cooldown",
            Heat = 75,
            Battery = 60,
            IsOverheated = false
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Drill", debugState.PlannedAction);
        Assert.Equal("Tool on cooldown", debugState.DamageState);
        Assert.Equal(75, debugState.Heat);
        Assert.Equal(60, debugState.Battery);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void PrometheusContext_StoresPlayerReference()
    {
        var context = PrometheusTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void PrometheusContext_TracksCombatState()
    {
        var context = PrometheusTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = PrometheusTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void PrometheusContext_TracksGcdState()
    {
        var context = PrometheusTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = PrometheusTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void PrometheusContext_HasDebugState()
    {
        var context = PrometheusTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void PrometheusContext_ConfigurationIsAccessible()
    {
        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.HeatMinGauge = 60;

        var context = PrometheusTestContext.Create(config: config);

        Assert.Equal(60, context.Configuration.Machinist.HeatMinGauge);
    }

    [Fact]
    public void PrometheusContext_HeatGaugeIsAccessible()
    {
        var context = PrometheusTestContext.Create(heat: 75);
        Assert.Equal(75, context.Heat);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = PrometheusTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void DamageModule_CollectCandidates_Drill_Pushed_AtPriority4_WhenChargesAvailable()
    {
        var module = new DamageModule();

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99999UL);
        enemy.Setup(x => x.CurrentHp).Returns(10000u);
        enemy.Setup(x => x.MaxHp).Returns(10000u);
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = PrometheusTestContext.Create(
            inCombat: true,
            isOverheated: false,
            hasFullMetalMachinist: false,
            hasExcavatorReady: false,
            drillCharges: 1,
            targetingService: targeting,
            actionService: actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == PrometheusAbilities.Drill && c.Priority == 4);
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = PrometheusTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void BuffModule_CollectCandidates_Wildfire_Disabled_NotPushed()
    {
        var module = new BuffModule();

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99999UL);
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(0);

        var config = PrometheusTestContext.CreateDefaultMachinistConfiguration();
        config.Machinist.EnableWildfire = false;

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = PrometheusTestContext.Create(
            inCombat: true,
            isOverheated: true,
            heat: 50,
            targetingService: targeting,
            actionService: actionService,
            config: config);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == PrometheusAbilities.Wildfire);
    }

    [Fact]
    public void BuffModule_CollectCandidates_Hypercharge_Pushed_AtPriority4_WhenHeatReady()
    {
        var module = new BuffModule();

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99999UL);
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(0);

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = PrometheusTestContext.Create(
            inCombat: true,
            heat: 50,
            isOverheated: false,
            targetingService: targeting,
            actionService: actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == PrometheusAbilities.Hypercharge && c.Priority == 4);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_WildfireEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Machinist.EnableWildfire);
    }

    [Fact]
    public void Configuration_HeatMinGauge_DefaultIs50()
    {
        var config = new Configuration();
        Assert.Equal(50, config.Machinist.HeatMinGauge);
    }

    #endregion
}
