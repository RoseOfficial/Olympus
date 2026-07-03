using Moq;
using Olympus.Rotation.AresCore.Abilities;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AresCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Ares (Warrior) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class AresTests
{
    #region Module Priority Tests

    [Fact]
    public void ModulePriorities_EnmityIsHighestPriority()
    {
        var enmity = new EnmityModule();
        var mitigation = new MitigationModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(enmity.Priority < mitigation.Priority);
        Assert.True(enmity.Priority < buff.Priority);
        Assert.True(enmity.Priority < damage.Priority);
    }

    [Fact]
    public void ModulePriorities_MitigationBeforeBuffs()
    {
        var mitigation = new MitigationModule();
        var buff = new BuffModule();

        Assert.True(mitigation.Priority < buff.Priority);
    }

    [Fact]
    public void ModulePriorities_BuffsBeforeDamage()
    {
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void AllModules_HaveCorrectExpectedPriorities()
    {
        Assert.Equal(5, new EnmityModule().Priority);
        Assert.Equal(10, new MitigationModule().Priority);
        Assert.Equal(20, new BuffModule().Priority);
        Assert.Equal(30, new DamageModule().Priority);
    }

    [Fact]
    public void AllModules_HaveExpectedNames()
    {
        Assert.Equal("Enmity", new EnmityModule().Name);
        Assert.Equal("Mitigation", new MitigationModule().Name);
        Assert.Equal("Buff", new BuffModule().Name);
        Assert.Equal("Damage", new DamageModule().Name);
    }

    #endregion

    #region DebugState Tests

    [Fact]
    public void AresDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new AresDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.MitigationState);
        Assert.Equal("", debugState.BuffState);
        Assert.Equal("", debugState.EnmityState);
    }

    [Fact]
    public void AresDebugState_CanBeModified()
    {
        var debugState = new AresDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Heavy Swing",
            DamageState = "Combo step 1"
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Heavy Swing", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void AresContext_StoresPlayerReference()
    {
        var context = AresTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void AresContext_TracksCombatState()
    {
        var context = AresTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = AresTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void AresContext_TracksGcdState()
    {
        var context = AresTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = AresTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void AresContext_HasDebugState()
    {
        var context = AresTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void AresContext_ConfigurationIsAccessible()
    {
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.MitigationThreshold = 0.75f;

        var context = AresTestContext.Create(config: config);

        Assert.Equal(0.75f, context.Configuration.Tank.MitigationThreshold);
    }

    [Fact]
    public void AresContext_BeastGaugeIsAccessible()
    {
        var context = AresTestContext.Create(beastGauge: 50);
        Assert.Equal(50, context.BeastGauge);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = AresTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void DamageModule_CollectCandidates_DamageDisabled_PushesNothing()
    {
        var module = new DamageModule();
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.EnableDamage = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = AresTestContext.Create(config: config, inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.DamageState);
    }

    [Fact]
    public void DamageModule_CollectCandidates_InCombatWithTarget_PushesHeavySwingAtPriority7()
    {
        var enemy = new Moq.Mock<Dalamud.Game.ClientState.Objects.Types.IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99UL);
        enemy.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<uint>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = AresTestContext.CreateMock(
            inCombat: true,
            beastGauge: 0,
            hasSurgingTempest: true,
            surgingTempestRemaining: 30f,
            comboStep: 0,
            actionService: actionService,
            targetingService: targetingService);

        new DamageModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c => c.Behavior == AresAbilities.HeavySwing && c.Priority == 7);
    }

    [Fact]
    public void MitigationModule_CollectCandidates_MitigationDisabled_PushesNothing()
    {
        var module = new MitigationModule();
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.EnableMitigation = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = AresTestContext.Create(config: config, inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.MitigationState);
    }

    [Fact]
    public void MitigationModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new MitigationModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = AresTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.MitigationState);
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = AresTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void BuffModule_CollectCandidates_AutoTankStance_PushesDefianceAtPriority1()
    {
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.AutoTankStance = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = AresTestContext.Create(inCombat: true, config: config, actionService: actionService);

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == AresAbilities.Defiance && c.Priority == 1);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_DefaultBeastGaugeCap_Is90()
    {
        var config = new Configuration();
        Assert.Equal(90, config.Tank.BeastGaugeCap);
    }

    [Fact]
    public void Configuration_MitigationEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Tank.EnableMitigation);
    }

    #endregion
}
