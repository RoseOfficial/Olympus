using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Rotation.PersephoneCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.PersephoneCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Persephone (Summoner) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class PersephoneTests
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
    public void PersephoneDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new PersephoneDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void PersephoneDebugState_CanBeModified()
    {
        var debugState = new PersephoneDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Astral Impulse",
            DamageState = "Bahamut phase",
            IsBahamutActive = true,
            DemiSummonGcdsRemaining = 4,
            AttunementStacks = 2
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Astral Impulse", debugState.PlannedAction);
        Assert.Equal("Bahamut phase", debugState.DamageState);
        Assert.True(debugState.IsBahamutActive);
        Assert.Equal(4, debugState.DemiSummonGcdsRemaining);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void PersephoneContext_StoresPlayerReference()
    {
        var context = PersephoneTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void PersephoneContext_TracksCombatState()
    {
        var context = PersephoneTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = PersephoneTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void PersephoneContext_TracksGcdState()
    {
        var context = PersephoneTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = PersephoneTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void PersephoneContext_HasDebugState()
    {
        var context = PersephoneTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void PersephoneContext_ConfigurationIsAccessible()
    {
        var config = PersephoneTestContext.CreateDefaultSmnConfiguration();
        config.Summoner.AetherflowReserve = 1;

        var context = PersephoneTestContext.Create(config: config);

        Assert.Equal(1, context.Configuration.Summoner.AetherflowReserve);
    }

    [Fact]
    public void PersephoneContext_DemiSummonStateIsAccessible()
    {
        var context = PersephoneTestContext.Create(isBahamutActive: true, demiSummonGcdsRemaining: 4);
        Assert.True(context.IsBahamutActive);
        Assert.Equal(4, context.DemiSummonGcdsRemaining);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = PersephoneTestContext.Create(inCombat: false);

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

        var context = PersephoneTestContext.Create(
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
        var context = PersephoneTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_SearingLightEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Summoner.EnableSearingLight);
    }

    [Fact]
    public void Configuration_AetherflowReserve_DefaultIsZero()
    {
        var config = new Configuration();
        Assert.Equal(0, config.Summoner.AetherflowReserve);
    }

    #endregion
}
