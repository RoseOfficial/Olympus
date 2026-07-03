using Moq;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThemisCore.Abilities;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Rotation.ThemisCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.ThemisCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Themis (Paladin) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class ThemisTests
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
    public void ThemisDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new ThemisDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.MitigationState);
        Assert.Equal("", debugState.BuffState);
        Assert.Equal("", debugState.EnmityState);
    }

    [Fact]
    public void ThemisDebugState_CanBeModified()
    {
        var debugState = new ThemisDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Fast Blade",
            DamageState = "Combo step 1"
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Fast Blade", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void ThemisContext_StoresPlayerReference()
    {
        var context = ThemisTestContext.Create();
        Assert.NotNull(context.Player);
    }

    [Fact]
    public void ThemisContext_TracksCombatState()
    {
        var context = ThemisTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = ThemisTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void ThemisContext_TracksGcdState()
    {
        var context = ThemisTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = ThemisTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void ThemisContext_HasDebugState()
    {
        var context = ThemisTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void ThemisContext_ConfigurationIsAccessible()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.MitigationThreshold = 0.75f;

        var context = ThemisTestContext.Create(config: config);

        Assert.Equal(0.75f, context.Configuration.Tank.MitigationThreshold);
    }

    [Fact]
    public void ThemisContext_ManaIsAccessible()
    {
        var context = ThemisTestContext.Create(currentMp: 8000, maxMp: 10000);
        Assert.Equal(8000u, context.Player.CurrentMp);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void DamageModule_CollectCandidates_DamageDisabled_PushesNothing()
    {
        var module = new DamageModule();
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableDamage = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(config: config, inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.DamageState);
    }

    [Fact]
    public void MitigationModule_CollectCandidates_MitigationDisabled_PushesNothing()
    {
        var module = new MitigationModule();
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableMitigation = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(config: config, inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.MitigationState);
    }

    [Fact]
    public void MitigationModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new MitigationModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.MitigationState);
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void BuffModule_CollectCandidates_AutoTankStance_PushesIronWillAtPriority1()
    {
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.AutoTankStance = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(inCombat: true, config: config, actionService: actionService);

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == ThemisAbilities.IronWill && c.Priority == 1);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_DefaultClemencyThreshold_Is30Percent()
    {
        var config = new Configuration();
        Assert.Equal(0.30f, config.Tank.ClemencyThreshold);
    }

    [Fact]
    public void Configuration_MitigationEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Tank.EnableMitigation);
    }

    #endregion
}
