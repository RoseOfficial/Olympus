using Olympus.Rotation.CirceCore.Abilities;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Rotation.CirceCore.Modules;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.CirceCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Circe (Red Mage) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class CirceTests
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
    public void CirceDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new CirceDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void CirceDebugState_CanBeModified()
    {
        var debugState = new CirceDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Verholy",
            DamageState = "Melee finisher",
            BlackMana = 80,
            WhiteMana = 85,
            ManaStacks = 3,
            IsInMeleeCombo = true
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Verholy", debugState.PlannedAction);
        Assert.Equal("Melee finisher", debugState.DamageState);
        Assert.Equal(80, debugState.BlackMana);
        Assert.Equal(85, debugState.WhiteMana);
        Assert.True(debugState.IsInMeleeCombo);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void CirceContext_StoresPlayerReference()
    {
        var context = CirceTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void CirceContext_TracksCombatState()
    {
        var context = CirceTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = CirceTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void CirceContext_TracksGcdState()
    {
        var context = CirceTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = CirceTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void CirceContext_HasDebugState()
    {
        var context = CirceTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void CirceContext_ConfigurationIsAccessible()
    {
        var config = CirceTestContext.CreateDefaultRdmConfiguration();
        config.RedMage.ManaImbalanceThreshold = 20;

        var context = CirceTestContext.Create(config: config);

        Assert.Equal(20, context.Configuration.RedMage.ManaImbalanceThreshold);
    }

    [Fact]
    public void CirceContext_ManaStateIsAccessible()
    {
        var context = CirceTestContext.Create(blackMana: 60, whiteMana: 65, manaStacks: 2);
        Assert.Equal(60, context.BlackMana);
        Assert.Equal(65, context.WhiteMana);
        Assert.Equal(2, context.ManaStacks);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = CirceTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void DamageModule_CollectCandidates_NoTarget_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        // Default targetingService returns null from FindEnemy; module exits before any push.
        var context = CirceTestContext.Create(inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = CirceTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_Embolden_PushedAtPriority2_WhenConditionsMet()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        // canStartMeleeCombo: true bypasses the LowerMana < 40 guard in TryPushEmbolden.
        var context = CirceTestContext.Create(
            inCombat: true,
            level: 58,
            emboldenReady: true,
            hasEmbolden: false,
            canStartMeleeCombo: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == CirceAbilities.Embolden && c.Priority == 2);
    }

    [Fact]
    public void BuffModule_CollectCandidates_Embolden_NotPushed_WhenToggleOff()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var config = CirceTestContext.CreateDefaultRdmConfiguration();
        config.RedMage.EnableEmbolden = false;
        var context = CirceTestContext.Create(
            inCombat: true,
            level: 58,
            emboldenReady: true,
            hasEmbolden: false,
            canStartMeleeCombo: true,
            config: config);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == CirceAbilities.Embolden);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_EmboldenEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.RedMage.EnableEmbolden);
    }

    [Fact]
    public void Configuration_ManaImbalanceThreshold_DefaultIs30()
    {
        var config = new Configuration();
        Assert.Equal(30, config.RedMage.ManaImbalanceThreshold);
    }

    #endregion
}
