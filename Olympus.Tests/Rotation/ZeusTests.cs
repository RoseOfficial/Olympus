using Olympus.Rotation.ZeusCore.Context;
using Olympus.Rotation.ZeusCore.Modules;
using Olympus.Tests.Rotation.ZeusCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Zeus (Dragoon) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class ZeusTests
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
    public void ZeusDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new ZeusDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
        Assert.Equal("", debugState.ComboState);
    }

    [Fact]
    public void ZeusDebugState_CanBeModified()
    {
        var debugState = new ZeusDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "True Thrust",
            DamageState = "Combo step 1",
            IsLifeOfDragonActive = true,
            FirstmindsFocus = 2
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("True Thrust", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
        Assert.True(debugState.IsLifeOfDragonActive);
        Assert.Equal(2, debugState.FirstmindsFocus);
    }

    [Fact]
    public void ZeusDebugState_FormatComboState_ReturnsPlaceholder_WhenNoCombo()
    {
        var result = ZeusDebugState.FormatComboState(0, 0);
        Assert.Equal("[---]", result);
    }

    [Fact]
    public void ZeusDebugState_FormatLifeState_ReturnsNoEyes_WhenInactive()
    {
        var debugState = new ZeusDebugState();
        Assert.Equal("No Eyes", debugState.FormatLifeState());
    }

    #endregion

    #region Context Tests

    [Fact]
    public void ZeusContext_StoresPlayerReference()
    {
        var context = ZeusTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void ZeusContext_TracksCombatState()
    {
        var context = ZeusTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = ZeusTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void ZeusContext_TracksGcdState()
    {
        var context = ZeusTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = ZeusTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void ZeusContext_HasDebugState()
    {
        var context = ZeusTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void ZeusContext_ConfigurationIsAccessible()
    {
        var config = ZeusTestContext.CreateDefaultDragoonConfiguration();
        config.Dragoon.EnableLanceCharge = false;

        var context = ZeusTestContext.Create(config: config);

        Assert.False(context.Configuration.Dragoon.EnableLanceCharge);
    }

    [Fact]
    public void ZeusContext_FirstmindsFocusIsAccessible()
    {
        var context = ZeusTestContext.Create(firstmindsFocus: 2);
        Assert.Equal(2, context.FirstmindsFocus);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = ZeusTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenCannotExecuteGcd()
    {
        var module = new DamageModule();
        var context = ZeusTestContext.Create(inCombat: true, canExecuteGcd: false, canExecuteOgcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = ZeusTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_LanceChargeEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Dragoon.EnableLanceCharge);
    }

    [Fact]
    public void Configuration_DefaultAoEMinTargets_Is3()
    {
        var config = new Configuration();
        Assert.Equal(3, config.Dragoon.AoEMinTargets);
    }

    #endregion
}
