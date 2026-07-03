using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.TerpsichoreCore.Abilities;
using Olympus.Rotation.TerpsichoreCore.Context;
using Olympus.Rotation.TerpsichoreCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.TerpsichoreCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Terpsichore (Dancer) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class TerpsichoreTests
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
    public void TerpsichoreDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new TerpsichoreDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void TerpsichoreDebugState_CanBeModified()
    {
        var debugState = new TerpsichoreDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Saber Dance",
            DamageState = "Esprit spender",
            Esprit = 80,
            Feathers = 3,
            IsDancing = false
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Saber Dance", debugState.PlannedAction);
        Assert.Equal("Esprit spender", debugState.DamageState);
        Assert.Equal(80, debugState.Esprit);
        Assert.Equal(3, debugState.Feathers);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void TerpsichoreContext_StoresPlayerReference()
    {
        var context = TerpsichoreTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void TerpsichoreContext_TracksCombatState()
    {
        var context = TerpsichoreTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = TerpsichoreTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void TerpsichoreContext_TracksGcdState()
    {
        var context = TerpsichoreTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = TerpsichoreTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void TerpsichoreContext_HasDebugState()
    {
        var context = TerpsichoreTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void TerpsichoreContext_ConfigurationIsAccessible()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.SaberDanceMinGauge = 60;

        var context = TerpsichoreTestContext.Create(config: config);

        Assert.Equal(60, context.Configuration.Dancer.SaberDanceMinGauge);
    }

    [Fact]
    public void TerpsichoreContext_EspritGaugeIsAccessible()
    {
        var context = TerpsichoreTestContext.Create(esprit: 70);
        Assert.Equal(70, context.Esprit);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = TerpsichoreTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void DamageModule_CollectCandidates_IsDancing_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = TerpsichoreTestContext.Create(inCombat: true, isDancing: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Dancing...", context.Debug.DamageState);
    }

    [Fact]
    public void DamageModule_CollectCandidates_StarfallDance_Pushed_AtPriority1_WhenProcActive()
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
        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            isDancing: false,
            hasFlourishingStarfall: true,
            targetingService: targeting,
            actionService: actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == TerpsichoreAbilities.StarfallDance && c.Priority == 1);
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesStandardStep_PreCombatAutomation()
    {
        var module = new BuffModule();

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = TerpsichoreTestContext.Create(
            inCombat: false,
            isDancing: false,
            hasStandardFinish: false,
            actionService: actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == TerpsichoreAbilities.StandardStep && c.Priority == 4);
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void BuffModule_CollectCandidates_StandardStep_Disabled_NotPushed_WhenNotInCombat()
    {
        var module = new BuffModule();

        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableStandardStep = false;

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = TerpsichoreTestContext.Create(
            inCombat: false,
            isDancing: false,
            hasStandardFinish: false,
            actionService: actionService,
            config: config);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Empty(scheduler.InspectGcdQueue());
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_StandardStepEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Dancer.EnableStandardStep);
    }

    [Fact]
    public void Configuration_SaberDanceMinGauge_DefaultIs50()
    {
        var config = new Configuration();
        Assert.Equal(50, config.Dancer.SaberDanceMinGauge);
    }

    #endregion
}
