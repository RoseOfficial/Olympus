using Moq;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.NyxCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Nyx (Dark Knight) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class NyxTests
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
    public void NyxDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new NyxDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.MitigationState);
        Assert.Equal("", debugState.BuffState);
        Assert.Equal("", debugState.EnmityState);
    }

    [Fact]
    public void NyxDebugState_CanBeModified()
    {
        var debugState = new NyxDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Hard Slash",
            DamageState = "Combo step 1"
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Hard Slash", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void NyxContext_StoresPlayerReference()
    {
        var context = NyxTestContext.Create();
        Assert.NotNull(context.Player);
    }

    [Fact]
    public void NyxContext_TracksCombatState()
    {
        var context = NyxTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = NyxTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void NyxContext_TracksGcdState()
    {
        var context = NyxTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = NyxTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void NyxContext_HasDebugState()
    {
        var context = NyxTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void NyxContext_ConfigurationIsAccessible()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.MitigationThreshold = 0.75f;

        var context = NyxTestContext.Create(config: config);

        Assert.Equal(0.75f, context.Configuration.Tank.MitigationThreshold);
    }

    [Fact]
    public void NyxContext_BloodGaugeIsAccessible()
    {
        var context = NyxTestContext.Create(bloodGauge: 60);
        Assert.Equal(60, context.BloodGauge);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = NyxTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void DamageModule_CollectCandidates_DamageDisabled_PushesNothing()
    {
        var module = new DamageModule();
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDamage = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = NyxTestContext.Create(config: config, inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.DamageState);
    }

    [Fact]
    public void MitigationModule_CollectCandidates_MitigationDisabled_PushesNothing()
    {
        var module = new MitigationModule();
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableMitigation = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = NyxTestContext.Create(config: config, inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.MitigationState);
    }

    [Fact]
    public void MitigationModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new MitigationModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = NyxTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.MitigationState);
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = NyxTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void BuffModule_CollectCandidates_AutoTankStance_PushesGritAtPriority1()
    {
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.AutoTankStance = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NyxTestContext.Create(inCombat: true, config: config, actionService: actionService);

        new BuffModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.Grit && c.Priority == 1);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_DefaultTBNThreshold_Is80Percent()
    {
        var config = new Configuration();
        Assert.Equal(0.80f, config.Tank.TBNThreshold);
    }

    [Fact]
    public void Configuration_MitigationEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Tank.EnableMitigation);
    }

    #endregion
}
