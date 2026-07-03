using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.ThanatosCore.Abilities;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Rotation.ThanatosCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.ThanatosCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Thanatos (Reaper) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class ThanatosTests
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
    public void ThanatosDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new ThanatosDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void ThanatosDebugState_CanBeModified()
    {
        var debugState = new ThanatosDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Slice",
            DamageState = "Combo step 1",
            Soul = 80,
            IsEnshrouded = true
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Slice", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
        Assert.Equal(80, debugState.Soul);
        Assert.True(debugState.IsEnshrouded);
    }

    [Fact]
    public void ThanatosDebugState_GetEnshroudState_ReturnsNotEnshrouded_WhenInactive()
    {
        var debugState = new ThanatosDebugState();
        Assert.Equal("Not Enshrouded", debugState.GetEnshroudState());
    }

    [Fact]
    public void ThanatosDebugState_GetGaugeState_ContainsSoulAndShroud()
    {
        var debugState = new ThanatosDebugState { Soul = 60, Shroud = 40 };
        var state = debugState.GetGaugeState();
        Assert.Contains("Soul: 60", state);
        Assert.Contains("Shroud: 40", state);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void ThanatosContext_StoresPlayerReference()
    {
        var context = ThanatosTestContext.Create(level: 90);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)90, context.Player.Level);
    }

    [Fact]
    public void ThanatosContext_TracksCombatState()
    {
        var context = ThanatosTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = ThanatosTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void ThanatosContext_TracksGcdState()
    {
        var context = ThanatosTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = ThanatosTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void ThanatosContext_HasDebugState()
    {
        var context = ThanatosTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void ThanatosContext_ConfigurationIsAccessible()
    {
        var config = ThanatosTestContext.CreateDefaultReaperConfiguration();
        config.Reaper.EnableArcaneCircle = false;

        var context = ThanatosTestContext.Create(config: config);

        Assert.False(context.Configuration.Reaper.EnableArcaneCircle);
    }

    [Fact]
    public void ThanatosContext_SoulGaugeIsAccessible()
    {
        var context = ThanatosTestContext.Create(soul: 80);
        Assert.Equal(80, context.Soul);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_PushesNothing_WhenNotInCombat()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThanatosTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void DamageModule_CollectCandidates_PushesNothing_WhenNoTarget()
    {
        // Default targeting mock returns null for FindEnemyForAction.
        // The module gates all pushes behind the target check.
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThanatosTestContext.Create(inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_PushesNothing_WhenNotInCombat()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThanatosTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_PushesArcaneCircle_AtPriority1_WhenDeathsDesignActive()
    {
        // Gate conditions: EnableArcaneCircle, level >= 72, !HasArcaneCircle,
        // IsActionReady, !ShouldHoldForBurst (null service), HasDeathsDesign.
        // BuffModule uses player.GameObjectId as target; no enemy target needed.
        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThanatosTestContext.Create(
            actionService: actionService,
            level: 100,
            inCombat: true,
            hasArcaneCircle: false,
            hasDeathsDesign: true);

        var module = new BuffModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == ThanatosAbilities.ArcaneCircle && c.Priority == 1);
    }

    [Fact]
    public void BuffModule_CollectCandidates_DoesNotPushArcaneCircle_WhenToggleDisabled()
    {
        // EnableArcaneCircle = false triggers the module guard inside TryPushArcaneCircle.
        var config = ThanatosTestContext.CreateDefaultReaperConfiguration();
        config.Reaper.EnableArcaneCircle = false;

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThanatosTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            hasArcaneCircle: false,
            hasDeathsDesign: true);

        var module = new BuffModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == ThanatosAbilities.ArcaneCircle);
    }

    [Fact]
    public void BuffModule_CollectCandidates_DoesNotPushArcaneCircle_WhenDeathsDesignMissing()
    {
        // HasDeathsDesign = false (default) causes TryPushArcaneCircle to return early.
        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThanatosTestContext.Create(
            actionService: actionService,
            level: 100,
            inCombat: true,
            hasArcaneCircle: false,
            hasDeathsDesign: false);

        var module = new BuffModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == ThanatosAbilities.ArcaneCircle);
    }

    [Fact]
    public void DamageModule_CollectCandidates_PushesPerfectio_AtPriority1_WhenProcActive()
    {
        // Perfectio: EnablePerfectio (default true), level >= 100, HasPerfectioParata, IsActionReady.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThanatosTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            hasPerfectioParata: true);

        var module = new DamageModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == ThanatosAbilities.Perfectio && c.Priority == 1);
    }

    [Fact]
    public void DamageModule_CollectCandidates_DoesNotPushPerfectio_WhenNoProcBuff()
    {
        // HasPerfectioParata = false (default) means the proc gate blocks the push.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThanatosTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            hasPerfectioParata: false);

        var module = new DamageModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == ThanatosAbilities.Perfectio);
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_ArcaneCircleEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Reaper.EnableArcaneCircle);
    }

    [Fact]
    public void Configuration_DefaultSoulMinGauge_Is50()
    {
        var config = new Configuration();
        Assert.Equal(50, config.Reaper.SoulMinGauge);
    }

    #endregion
}
