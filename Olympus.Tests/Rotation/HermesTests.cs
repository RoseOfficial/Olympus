using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Modules;
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
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = HermesTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenCannotExecuteGcd()
    {
        var module = new DamageModule();
        var context = HermesTestContext.Create(inCombat: true, canExecuteGcd: false, canExecuteOgcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void NinjutsuModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new NinjutsuModule();
        var context = HermesTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = HermesTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
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
