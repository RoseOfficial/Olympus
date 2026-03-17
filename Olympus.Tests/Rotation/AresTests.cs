using Moq;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AresCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Ares (Warrior) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class AresTests
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
    public void AresDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new AresDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.MitigationState);
        Assert.Equal("", debugState.BuffState);
        Assert.Equal("", debugState.EnmityState);
    }

    [Fact]
    public void AresDebugState_CanBeModified()
    {
        var debugState = new AresDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Heavy Swing",
            DamageState = "Combo step 1"
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Heavy Swing", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void AresContext_StoresPlayerReference()
    {
        var context = AresTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void AresContext_TracksCombatState()
    {
        var context = AresTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = AresTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void AresContext_TracksGcdState()
    {
        var context = AresTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = AresTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void AresContext_HasDebugState()
    {
        var context = AresTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void AresContext_ConfigurationIsAccessible()
    {
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.MitigationThreshold = 0.75f;

        var context = AresTestContext.Create(config: config);

        Assert.Equal(0.75f, context.Configuration.Tank.MitigationThreshold);
    }

    [Fact]
    public void AresContext_BeastGaugeIsAccessible()
    {
        var context = AresTestContext.Create(beastGauge: 50);
        Assert.Equal(50, context.BeastGauge);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = AresTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenDamageDisabled()
    {
        var module = new DamageModule();
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.EnableDamage = false;

        var enemy = new Moq.Mock<Dalamud.Game.ClientState.Objects.Types.IBattleNpc>();
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<uint>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var context = AresTestContext.Create(
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
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.EnableMitigation = false;

        var context = AresTestContext.Create(config: config, inCombat: true);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = AresTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_DefaultBeastGaugeCap_Is90()
    {
        var config = new Configuration();
        Assert.Equal(90, config.Tank.BeastGaugeCap);
    }

    [Fact]
    public void Configuration_MitigationEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Tank.EnableMitigation);
    }

    #endregion
}
