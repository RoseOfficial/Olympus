using Olympus.Rotation.NikeCore.Context;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Tests.Rotation.NikeCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Nike (Samurai) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class NikeTests
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
    public void NikeDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new NikeDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void NikeDebugState_CanBeModified()
    {
        var debugState = new NikeDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Hakaze",
            DamageState = "Combo step 1",
            Kenki = 50,
            HasMeikyoShisui = true
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Hakaze", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
        Assert.Equal(50, debugState.Kenki);
        Assert.True(debugState.HasMeikyoShisui);
    }

    [Fact]
    public void NikeDebugState_GetGaugeSummary_ContainsKenki()
    {
        var debugState = new NikeDebugState { Kenki = 75, Meditation = 3 };
        var summary = debugState.GetGaugeSummary();
        Assert.Contains("Kenki:75", summary);
        Assert.Contains("Med:3", summary);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void NikeContext_StoresPlayerReference()
    {
        var context = NikeTestContext.Create(level: 90);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)90, context.Player.Level);
    }

    [Fact]
    public void NikeContext_TracksCombatState()
    {
        var context = NikeTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = NikeTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void NikeContext_TracksGcdState()
    {
        var context = NikeTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = NikeTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void NikeContext_HasDebugState()
    {
        var context = NikeTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void NikeContext_ConfigurationIsAccessible()
    {
        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.EnableMeikyoShisui = false;

        var context = NikeTestContext.Create(config: config);

        Assert.False(context.Configuration.Samurai.EnableMeikyoShisui);
    }

    [Fact]
    public void NikeContext_KenkiIsAccessible()
    {
        var context = NikeTestContext.Create(kenki: 75);
        Assert.Equal(75, context.Kenki);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = NikeTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenCannotExecuteGcd()
    {
        var module = new DamageModule();
        var context = NikeTestContext.Create(inCombat: true, canExecuteGcd: false, canExecuteOgcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = NikeTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_MeikyoShisuiEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Samurai.EnableMeikyoShisui);
    }

    [Fact]
    public void Configuration_DefaultKenkiMinGauge_Is25()
    {
        var config = new Configuration();
        Assert.Equal(25, config.Samurai.KenkiMinGauge);
    }

    #endregion
}
