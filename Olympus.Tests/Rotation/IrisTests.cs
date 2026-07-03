using Olympus.Rotation.IrisCore.Abilities;
using Olympus.Rotation.IrisCore.Context;
using Olympus.Rotation.IrisCore.Modules;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.IrisCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Iris (Pictomancer) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class IrisTests
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
        // Iris (PCT) has different priorities: Buff=30, Damage=50
        Assert.Equal(30, new BuffModule().Priority);
        Assert.Equal(50, new DamageModule().Priority);
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
    public void IrisDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new IrisDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void IrisDebugState_CanBeModified()
    {
        var debugState = new IrisDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Holy in White",
            DamageState = "Paint spender",
            PaletteGauge = 75,
            HasCreatureCanvas = true,
            IsInHammerCombo = false
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Holy in White", debugState.PlannedAction);
        Assert.Equal("Paint spender", debugState.DamageState);
        Assert.Equal(75, debugState.PaletteGauge);
        Assert.True(debugState.HasCreatureCanvas);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void IrisContext_StoresPlayerReference()
    {
        var context = IrisTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void IrisContext_TracksCombatState()
    {
        var context = IrisTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = IrisTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void IrisContext_TracksGcdState()
    {
        var context = IrisTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = IrisTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void IrisContext_HasDebugState()
    {
        var context = IrisTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void IrisContext_ConfigurationIsAccessible()
    {
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.HolyMinPalette = 75;

        var context = IrisTestContext.Create(config: config);

        Assert.Equal(75, context.Configuration.Pictomancer.HolyMinPalette);
    }

    [Fact]
    public void IrisContext_PaletteGaugeIsAccessible()
    {
        var context = IrisTestContext.Create(paletteGauge: 80, canUseSubtractivePalette: true);
        Assert.Equal(80, context.PaletteGauge);
        Assert.True(context.CanUseSubtractivePalette);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_NoMotifNeeds_PushesNothing()
    {
        // With all motif needs false (default), the pre-combat prepaint path pushes nothing.
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            inCombat: false,
            needsCreatureMotif: false,
            needsWeaponMotif: false,
            needsLandscapeMotif: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_LandscapeNeeded_PushesStarrySkyMotif()
    {
        // When a landscape motif is needed pre-combat, the prepaint path pushes StarrySkyMotif.
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            inCombat: false,
            level: 70,
            needsLandscapeMotif: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.StarrySkyMotif && c.Priority == 1);
    }

    [Fact]
    public void DamageModule_CollectCandidates_NoTarget_PushesNothing()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        // Default targetingService returns null from FindEnemy; module exits before any push.
        var context = IrisTestContext.Create(inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_StarryMuse_PushedAtPriority2_WhenConditionsMet()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        // hasLandscapeCanvas: true satisfies the canvas guard inside TryPushStarryMuse.
        var context = IrisTestContext.Create(
            inCombat: true,
            level: 70,
            starryMuseReady: true,
            hasLandscapeCanvas: true,
            hasStarryMuse: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == IrisAbilities.StarryMuse && c.Priority == 2);
    }

    [Fact]
    public void BuffModule_CollectCandidates_StarryMuse_NotPushed_WhenToggleOff()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnableStarryMuse = false;
        var context = IrisTestContext.Create(
            inCombat: true,
            level: 70,
            starryMuseReady: true,
            hasLandscapeCanvas: true,
            hasStarryMuse: false,
            config: config);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == IrisAbilities.StarryMuse);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_StarryMuseEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Pictomancer.EnableStarryMuse);
    }

    [Fact]
    public void Configuration_HolyMinPalette_DefaultIs50()
    {
        var config = new Configuration();
        Assert.Equal(50, config.Pictomancer.HolyMinPalette);
    }

    #endregion
}
