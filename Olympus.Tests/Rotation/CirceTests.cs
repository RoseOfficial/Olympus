using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Rotation.CirceCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
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
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = CirceTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenGcdNotReady()
    {
        var module = new DamageModule();

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99999UL);
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targetingService.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);

        var context = CirceTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: false,
            targetingService: targetingService);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = CirceTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
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
