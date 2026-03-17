using Moq;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Rotation.ThemisCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.ThemisCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Themis (Paladin) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class ThemisTests
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
    public void ThemisDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new ThemisDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.MitigationState);
        Assert.Equal("", debugState.BuffState);
        Assert.Equal("", debugState.EnmityState);
    }

    [Fact]
    public void ThemisDebugState_CanBeModified()
    {
        var debugState = new ThemisDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Fast Blade",
            DamageState = "Combo step 1"
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Fast Blade", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void ThemisContext_StoresPlayerReference()
    {
        var context = ThemisTestContext.Create();
        Assert.NotNull(context.Player);
    }

    [Fact]
    public void ThemisContext_TracksCombatState()
    {
        var context = ThemisTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = ThemisTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void ThemisContext_TracksGcdState()
    {
        var context = ThemisTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = ThemisTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void ThemisContext_HasDebugState()
    {
        var context = ThemisTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void ThemisContext_ConfigurationIsAccessible()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.MitigationThreshold = 0.75f;

        var context = ThemisTestContext.Create(config: config);

        Assert.Equal(0.75f, context.Configuration.Tank.MitigationThreshold);
    }

    [Fact]
    public void ThemisContext_ManaIsAccessible()
    {
        var context = ThemisTestContext.Create(currentMp: 8000, maxMp: 10000);
        Assert.Equal(8000u, context.Player.CurrentMp);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = ThemisTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenDamageDisabled()
    {
        var module = new DamageModule();
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableDamage = false;

        var enemy = new Moq.Mock<Dalamud.Game.ClientState.Objects.Types.IBattleNpc>();
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<uint>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var context = ThemisTestContext.Create(
            config: config,
            targetingService: targetingService,
            inCombat: true,
            canExecuteGcd: true);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void MitigationModule_ReturnsFalse_WhenMitigationDisabled()
    {
        var module = new MitigationModule();
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableMitigation = false;

        var context = ThemisTestContext.Create(config: config, inCombat: true);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = ThemisTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_DefaultClemencyThreshold_Is30Percent()
    {
        var config = new Configuration();
        Assert.Equal(0.30f, config.Tank.ClemencyThreshold);
    }

    [Fact]
    public void Configuration_MitigationEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Tank.EnableMitigation);
    }

    #endregion
}
