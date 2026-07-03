using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.HermesCore.Abilities;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.HermesCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Hermes (Ninja) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class HermesTests
{
    #region Module Priority Tests

    [Fact]
    public void ModulePriorities_NinjutsuIsHighestPriority()
    {
        var ninjutsu = new NinjutsuModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(ninjutsu.Priority < buff.Priority);
        Assert.True(ninjutsu.Priority < damage.Priority);
    }

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
        Assert.Equal(10, new NinjutsuModule().Priority);
        Assert.Equal(20, new BuffModule().Priority);
        Assert.Equal(30, new DamageModule().Priority);
    }

    [Fact]
    public void AllModules_HaveExpectedNames()
    {
        Assert.Equal("Ninjutsu", new NinjutsuModule().Name);
        Assert.Equal("Buff", new BuffModule().Name);
        Assert.Equal("Damage", new DamageModule().Name);
    }

    #endregion

    #region DebugState Tests

    [Fact]
    public void HermesDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new HermesDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
        Assert.Equal("", debugState.NinjutsuState);
    }

    [Fact]
    public void HermesDebugState_CanBeModified()
    {
        var debugState = new HermesDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Spinning Edge",
            NinjutsuState = "Building mudra",
            Ninki = 50,
            IsMudraActive = true
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Spinning Edge", debugState.PlannedAction);
        Assert.Equal("Building mudra", debugState.NinjutsuState);
        Assert.Equal(50, debugState.Ninki);
        Assert.True(debugState.IsMudraActive);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void HermesContext_StoresPlayerReference()
    {
        var context = HermesTestContext.Create(level: 90);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)90, context.Player.Level);
    }

    [Fact]
    public void HermesContext_TracksCombatState()
    {
        var context = HermesTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = HermesTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void HermesContext_TracksGcdState()
    {
        var context = HermesTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = HermesTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void HermesContext_HasDebugState()
    {
        var context = HermesTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void HermesContext_ConfigurationIsAccessible()
    {
        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.EnableNinjutsu = false;

        var context = HermesTestContext.Create(config: config);

        Assert.False(context.Configuration.Ninja.EnableNinjutsu);
    }

    [Fact]
    public void HermesContext_NinkiIsAccessible()
    {
        var context = HermesTestContext.Create(ninki: 60);
        Assert.Equal(60, context.Ninki);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var context = HermesTestContext.Create(inCombat: false);
        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void DamageModule_CollectCandidates_PhantomKamaitachi_PushedAtPriority3_WhenReady()
    {
        // Phantom Kamaitachi is pushed to the GCD queue at priority 3 when the proc is active.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.EnablePhantomKamaitachi = true;

        var context = HermesTestContext.Create(
            config: config,
            inCombat: true,
            actionService: actionService,
            targetingService: targeting,
            hasPhantomKamaitachiReady: true,
            level: 100);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var module = new DamageModule();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == HermesAbilities.PhantomKamaitachi && c.Priority == 3);
    }

    [Fact]
    public void NinjutsuModule_CollectCandidates_NotInCombat_SetsStateAndPushesNothing()
    {
        // NinjutsuModule sets NinjutsuState and returns before reaching any native code.
        // Mudra/ninjutsu execution bypasses IActionService so the scheduler queues stay empty.
        var module = new NinjutsuModule();
        var context = HermesTestContext.Create(inCombat: false);
        var scheduler = SchedulerFactory.CreateForTest();
        var debug = context.Debug;

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Equal("Not in combat", debug.NinjutsuState);
        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var context = HermesTestContext.Create(inCombat: false);
        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_MudraActive_PushesNothing()
    {
        // When a mudra sequence is in progress, BuffModule returns immediately.
        var module = new BuffModule();
        var context = HermesTestContext.Create(inCombat: true, isMudraActive: true);
        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_Kassatsu_PushedAtPriority3_WhenReady()
    {
        // Kassatsu: enabled (config), level met, HasKassatsu=false, action ready.
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.EnableKassatsu = true;

        var context = HermesTestContext.Create(
            config: config,
            inCombat: true,
            actionService: actionService,
            hasKassatsu: false,
            level: 100);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var module = new BuffModule();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == HermesAbilities.Kassatsu && c.Priority == 3);
    }

    [Fact]
    public void BuffModule_CollectCandidates_Kassatsu_NotPushed_WhenAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.EnableKassatsu = true;

        var context = HermesTestContext.Create(
            config: config,
            inCombat: true,
            actionService: actionService,
            hasKassatsu: true);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var module = new BuffModule();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == HermesAbilities.Kassatsu);
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
    public void Configuration_NinjutsuEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Ninja.EnableNinjutsu);
    }

    [Fact]
    public void Configuration_DefaultNinkiMinGauge_Is50()
    {
        var config = new Configuration();
        Assert.Equal(50, config.Ninja.NinkiMinGauge);
    }

    #endregion
}
