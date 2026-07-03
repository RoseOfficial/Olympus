using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.CalliopeCore.Abilities;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Rotation.CalliopeCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.CalliopeCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Calliope (Bard) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class CalliopeTests
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
    public void CalliopeDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new CalliopeDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void CalliopeDebugState_CanBeModified()
    {
        var debugState = new CalliopeDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Burst Shot",
            DamageState = "Filler GCD",
            SoulVoice = 80,
            Repertoire = 2
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Burst Shot", debugState.PlannedAction);
        Assert.Equal("Filler GCD", debugState.DamageState);
        Assert.Equal(80, debugState.SoulVoice);
        Assert.Equal(2, debugState.Repertoire);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void CalliopeContext_StoresPlayerReference()
    {
        var context = CalliopeTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void CalliopeContext_TracksCombatState()
    {
        var context = CalliopeTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = CalliopeTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void CalliopeContext_TracksGcdState()
    {
        var context = CalliopeTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = CalliopeTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void CalliopeContext_HasDebugState()
    {
        var context = CalliopeTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void CalliopeContext_ConfigurationIsAccessible()
    {
        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Bard.ApexArrowMinGauge = 90;

        var context = CalliopeTestContext.Create(config: config);

        Assert.Equal(90, context.Configuration.Bard.ApexArrowMinGauge);
    }

    [Fact]
    public void CalliopeContext_SoulVoiceIsAccessible()
    {
        var context = CalliopeTestContext.Create(soulVoice: 75);
        Assert.Equal(75, context.SoulVoice);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = CalliopeTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void DamageModule_CollectCandidates_ResonantArrow_Pushed_AtPriority1_WhenProcActive()
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
        var context = CalliopeTestContext.Create(
            inCombat: true,
            hasResonantArrowReady: true,
            targetingService: targeting,
            actionService: actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == CalliopeAbilities.ResonantArrow && c.Priority == 1);
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = CalliopeTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void BuffModule_CollectCandidates_SongRotation_Disabled_SongNotPushed()
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

        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Bard.EnableSongRotation = false;

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CalliopeTestContext.Create(
            inCombat: true,
            noSongActive: true,
            targetingService: targeting,
            actionService: actionService,
            config: config);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == CalliopeAbilities.WanderersMinuet);
        Assert.DoesNotContain(ogcd, c => c.Behavior == CalliopeAbilities.MagesBallad);
        Assert.DoesNotContain(ogcd, c => c.Behavior == CalliopeAbilities.ArmysPaeon);
    }

    [Fact]
    public void BuffModule_CollectCandidates_WanderersMinuet_Pushed_AtPriority2_WhenNoSong()
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
        var context = CalliopeTestContext.Create(
            inCombat: true,
            noSongActive: true,
            targetingService: targeting,
            actionService: actionService);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == CalliopeAbilities.WanderersMinuet && c.Priority == 2);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_AoERotationEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Bard.EnableAoERotation);
    }

    [Fact]
    public void Configuration_ApexArrowMinGauge_DefaultIs80()
    {
        var config = new Configuration();
        Assert.Equal(80, config.Bard.ApexArrowMinGauge);
    }

    #endregion
}
