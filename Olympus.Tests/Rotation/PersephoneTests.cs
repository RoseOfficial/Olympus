using Olympus.Rotation.PersephoneCore.Abilities;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Rotation.PersephoneCore.Modules;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.PersephoneCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Persephone (Summoner) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class PersephoneTests
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
    public void PersephoneDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new PersephoneDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void PersephoneDebugState_CanBeModified()
    {
        var debugState = new PersephoneDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Astral Impulse",
            DamageState = "Bahamut phase",
            IsBahamutActive = true,
            DemiSummonGcdsRemaining = 4,
            AttunementStacks = 2
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Astral Impulse", debugState.PlannedAction);
        Assert.Equal("Bahamut phase", debugState.DamageState);
        Assert.True(debugState.IsBahamutActive);
        Assert.Equal(4, debugState.DemiSummonGcdsRemaining);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void PersephoneContext_StoresPlayerReference()
    {
        var context = PersephoneTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void PersephoneContext_TracksCombatState()
    {
        var context = PersephoneTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = PersephoneTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void PersephoneContext_TracksGcdState()
    {
        var context = PersephoneTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = PersephoneTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void PersephoneContext_HasDebugState()
    {
        var context = PersephoneTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void PersephoneContext_ConfigurationIsAccessible()
    {
        var config = PersephoneTestContext.CreateDefaultSmnConfiguration();
        config.Summoner.AetherflowReserve = 1;

        var context = PersephoneTestContext.Create(config: config);

        Assert.Equal(1, context.Configuration.Summoner.AetherflowReserve);
    }

    [Fact]
    public void PersephoneContext_DemiSummonStateIsAccessible()
    {
        var context = PersephoneTestContext.Create(isBahamutActive: true, demiSummonGcdsRemaining: 4);
        Assert.True(context.IsBahamutActive);
        Assert.Equal(4, context.DemiSummonGcdsRemaining);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = PersephoneTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void DamageModule_CollectCandidates_NoTarget_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        // Default targetingService returns null from FindEnemy; target null check fires before
        // the pet-summon branch, so both queues stay empty.
        var context = PersephoneTestContext.Create(inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = PersephoneTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_SearingLight_PushedAtPriority3_WhenDemiActive()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = PersephoneTestContext.Create(
            inCombat: true,
            level: 66,
            searingLightReady: true,
            hasSearingLight: false,
            isDemiSummonActive: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == PersephoneAbilities.SearingLight && c.Priority == 3);
    }

    [Fact]
    public void BuffModule_CollectCandidates_SearingLight_NotPushed_WhenToggleOff()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var config = PersephoneTestContext.CreateDefaultSmnConfiguration();
        config.Summoner.EnableSearingLight = false;
        var context = PersephoneTestContext.Create(
            inCombat: true,
            level: 66,
            searingLightReady: true,
            hasSearingLight: false,
            isDemiSummonActive: true,
            config: config);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == PersephoneAbilities.SearingLight);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_SearingLightEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Summoner.EnableSearingLight);
    }

    [Fact]
    public void Configuration_AetherflowReserve_DefaultIsZero()
    {
        var config = new Configuration();
        Assert.Equal(0, config.Summoner.AetherflowReserve);
    }

    #endregion
}
