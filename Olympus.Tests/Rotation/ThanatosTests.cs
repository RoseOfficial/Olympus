using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Rotation.ThanatosCore.Modules;
using Olympus.Tests.Rotation.ThanatosCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Thanatos (Reaper) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class ThanatosTests
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
    public void ThanatosDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new ThanatosDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void ThanatosDebugState_CanBeModified()
    {
        var debugState = new ThanatosDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Slice",
            DamageState = "Combo step 1",
            Soul = 80,
            IsEnshrouded = true
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Slice", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
        Assert.Equal(80, debugState.Soul);
        Assert.True(debugState.IsEnshrouded);
    }

    [Fact]
    public void ThanatosDebugState_GetEnshroudState_ReturnsNotEnshrouded_WhenInactive()
    {
        var debugState = new ThanatosDebugState();
        Assert.Equal("Not Enshrouded", debugState.GetEnshroudState());
    }

    [Fact]
    public void ThanatosDebugState_GetGaugeState_ContainsSoulAndShroud()
    {
        var debugState = new ThanatosDebugState { Soul = 60, Shroud = 40 };
        var state = debugState.GetGaugeState();
        Assert.Contains("Soul: 60", state);
        Assert.Contains("Shroud: 40", state);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void ThanatosContext_StoresPlayerReference()
    {
        var context = ThanatosTestContext.Create(level: 90);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)90, context.Player.Level);
    }

    [Fact]
    public void ThanatosContext_TracksCombatState()
    {
        var context = ThanatosTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = ThanatosTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void ThanatosContext_TracksGcdState()
    {
        var context = ThanatosTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = ThanatosTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void ThanatosContext_HasDebugState()
    {
        var context = ThanatosTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void ThanatosContext_ConfigurationIsAccessible()
    {
        var config = ThanatosTestContext.CreateDefaultReaperConfiguration();
        config.Reaper.EnableArcaneCircle = false;

        var context = ThanatosTestContext.Create(config: config);

        Assert.False(context.Configuration.Reaper.EnableArcaneCircle);
    }

    [Fact]
    public void ThanatosContext_SoulGaugeIsAccessible()
    {
        var context = ThanatosTestContext.Create(soul: 80);
        Assert.Equal(80, context.Soul);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = ThanatosTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenCannotExecuteGcd()
    {
        var module = new DamageModule();
        var context = ThanatosTestContext.Create(inCombat: true, canExecuteGcd: false, canExecuteOgcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = ThanatosTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_ArcaneCircleEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Reaper.EnableArcaneCircle);
    }

    [Fact]
    public void Configuration_DefaultSoulMinGauge_Is50()
    {
        var config = new Configuration();
        Assert.Equal(50, config.Reaper.SoulMinGauge);
    }

    #endregion
}
