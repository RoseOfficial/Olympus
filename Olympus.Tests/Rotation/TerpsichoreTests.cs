using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.TerpsichoreCore.Context;
using Olympus.Rotation.TerpsichoreCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.TerpsichoreCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Terpsichore (Dancer) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class TerpsichoreTests
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
    public void TerpsichoreDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new TerpsichoreDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void TerpsichoreDebugState_CanBeModified()
    {
        var debugState = new TerpsichoreDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Saber Dance",
            DamageState = "Esprit spender",
            Esprit = 80,
            Feathers = 3,
            IsDancing = false
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Saber Dance", debugState.PlannedAction);
        Assert.Equal("Esprit spender", debugState.DamageState);
        Assert.Equal(80, debugState.Esprit);
        Assert.Equal(3, debugState.Feathers);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void TerpsichoreContext_StoresPlayerReference()
    {
        var context = TerpsichoreTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void TerpsichoreContext_TracksCombatState()
    {
        var context = TerpsichoreTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = TerpsichoreTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void TerpsichoreContext_TracksGcdState()
    {
        var context = TerpsichoreTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = TerpsichoreTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void TerpsichoreContext_HasDebugState()
    {
        var context = TerpsichoreTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void TerpsichoreContext_ConfigurationIsAccessible()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.SaberDanceMinGauge = 60;

        var context = TerpsichoreTestContext.Create(config: config);

        Assert.Equal(60, context.Configuration.Dancer.SaberDanceMinGauge);
    }

    [Fact]
    public void TerpsichoreContext_EspritGaugeIsAccessible()
    {
        var context = TerpsichoreTestContext.Create(esprit: 70);
        Assert.Equal(70, context.Esprit);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = TerpsichoreTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenDancing()
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

        var context = TerpsichoreTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            isDancing: true,
            targetingService: targetingService);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = TerpsichoreTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_StandardStepEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Dancer.EnableStandardStep);
    }

    [Fact]
    public void Configuration_SaberDanceMinGauge_DefaultIs50()
    {
        var config = new Configuration();
        Assert.Equal(50, config.Dancer.SaberDanceMinGauge);
    }

    #endregion
}
