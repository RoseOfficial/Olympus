using Olympus.Rotation.KratosCore.Context;
using Olympus.Rotation.KratosCore.Modules;
using Olympus.Tests.Rotation.KratosCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Kratos (Monk) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class KratosTests
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
    public void KratosDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new KratosDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
        Assert.Equal("", debugState.BeastChakraState);
    }

    [Fact]
    public void KratosDebugState_CanBeModified()
    {
        var debugState = new KratosDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Bootshine",
            DamageState = "Opo-opo form",
            Chakra = 5,
            HasRiddleOfFire = true
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Bootshine", debugState.PlannedAction);
        Assert.Equal("Opo-opo form", debugState.DamageState);
        Assert.Equal(5, debugState.Chakra);
        Assert.True(debugState.HasRiddleOfFire);
    }

    [Fact]
    public void KratosDebugState_FormatBeastChakra_ReturnsAllEmpty_WhenNone()
    {
        var result = KratosDebugState.FormatBeastChakra(0, 0, 0);
        Assert.Equal("[---]", result);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void KratosContext_StoresPlayerReference()
    {
        var context = KratosTestContext.Create(level: 90);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)90, context.Player.Level);
    }

    [Fact]
    public void KratosContext_TracksCombatState()
    {
        var context = KratosTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = KratosTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void KratosContext_TracksGcdState()
    {
        var context = KratosTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = KratosTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void KratosContext_HasDebugState()
    {
        var context = KratosTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void KratosContext_ConfigurationIsAccessible()
    {
        var config = KratosTestContext.CreateDefaultMonkConfiguration();
        config.Monk.EnableRiddleOfFire = false;

        var context = KratosTestContext.Create(config: config);

        Assert.False(context.Configuration.Monk.EnableRiddleOfFire);
    }

    [Fact]
    public void KratosContext_ChakraIsAccessible()
    {
        var context = KratosTestContext.Create(chakra: 5);
        Assert.Equal(5, context.Chakra);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = KratosTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenCannotExecuteGcd()
    {
        var module = new DamageModule();
        var context = KratosTestContext.Create(inCombat: true, canExecuteGcd: false, canExecuteOgcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = KratosTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_RiddleOfFireEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Monk.EnableRiddleOfFire);
    }

    [Fact]
    public void Configuration_DefaultChakraMinGauge_Is5()
    {
        var config = new Configuration();
        Assert.Equal(5, config.Monk.ChakraMinGauge);
    }

    #endregion
}
