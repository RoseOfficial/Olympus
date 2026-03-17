using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Rotation.EchidnaCore.Modules;
using Olympus.Tests.Rotation.EchidnaCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Echidna (Viper) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class EchidnaTests
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
    public void EchidnaDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new EchidnaDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void EchidnaDebugState_CanBeModified()
    {
        var debugState = new EchidnaDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Steel Fangs",
            DamageState = "Combo step 1",
            SerpentOffering = 50,
            IsReawakened = true
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Steel Fangs", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
        Assert.Equal(50, debugState.SerpentOffering);
        Assert.True(debugState.IsReawakened);
    }

    [Fact]
    public void EchidnaDebugState_GetReawakenState_ReturnsNotReawakened_WhenInactive()
    {
        var debugState = new EchidnaDebugState();
        Assert.Equal("Not Reawakened", debugState.GetReawakenState());
    }

    [Fact]
    public void EchidnaDebugState_GetGaugeState_ContainsOfferingsAndCoils()
    {
        var debugState = new EchidnaDebugState { SerpentOffering = 70, RattlingCoils = 2 };
        var state = debugState.GetGaugeState();
        Assert.Contains("Offerings: 70", state);
        Assert.Contains("Coils: 2", state);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void EchidnaContext_StoresPlayerReference()
    {
        var context = EchidnaTestContext.Create(level: 100);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)100, context.Player.Level);
    }

    [Fact]
    public void EchidnaContext_TracksCombatState()
    {
        var context = EchidnaTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = EchidnaTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void EchidnaContext_TracksGcdState()
    {
        var context = EchidnaTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = EchidnaTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void EchidnaContext_HasDebugState()
    {
        var context = EchidnaTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void EchidnaContext_ConfigurationIsAccessible()
    {
        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableReawaken = false;

        var context = EchidnaTestContext.Create(config: config);

        Assert.False(context.Configuration.Viper.EnableReawaken);
    }

    [Fact]
    public void EchidnaContext_SerpentOfferingIsAccessible()
    {
        var context = EchidnaTestContext.Create(serpentOffering: 70);
        Assert.Equal(70, context.SerpentOffering);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = EchidnaTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenCannotExecuteGcd()
    {
        var module = new DamageModule();
        var context = EchidnaTestContext.Create(inCombat: true, canExecuteGcd: false, canExecuteOgcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = EchidnaTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_ReawakenEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Viper.EnableReawaken);
    }

    [Fact]
    public void Configuration_DefaultRattlingCoilMinStacks_Is1()
    {
        var config = new Configuration();
        Assert.Equal(1, config.Viper.RattlingCoilMinStacks);
    }

    #endregion
}
