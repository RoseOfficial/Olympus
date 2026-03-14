using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Tests for Scholar DamageModule logic.
/// Covers Bio/Biolysis DoT uptime, Broil GCD filler, Chain Stratagem, and Energy Drain.
/// </summary>
public class DamageModuleTests
{
    private readonly DamageModule _module;

    public DamageModuleTests()
    {
        _module = new DamageModule();
    }

    private static Mock<IBattleNpc> CreateMockEnemy(uint id = 99u)
    {
        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(e => e.EntityId).Returns(id);
        enemy.Setup(e => e.GameObjectId).Returns((ulong)id);
        enemy.Setup(e => e.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return enemy;
    }

    #region Module Properties

    [Fact]
    public void Priority_Is50()
    {
        Assert.Equal(50, _module.Priority);
    }

    [Fact]
    public void Name_IsDamage()
    {
        Assert.Equal("Damage", _module.Name);
    }

    #endregion

    #region Damage Disabled Tests

    [Fact]
    public void TryExecute_DamageDisabled_ReturnsFalse()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableAetherflow = false;

        var enemy = CreateMockEnemy();
        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            inCombat: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region DoT Level Progression Tests

    [Fact]
    public void GetDotForLevel_Level2_ReturnsBio()
    {
        var action = SCHActions.GetDotForLevel(2);
        Assert.Equal(SCHActions.Bio.ActionId, action.ActionId);
    }

    [Fact]
    public void GetDotForLevel_Level26_ReturnsBioII()
    {
        var action = SCHActions.GetDotForLevel(26);
        Assert.Equal(SCHActions.BioII.ActionId, action.ActionId);
    }

    [Fact]
    public void GetDotForLevel_Level72_ReturnsBiolysis()
    {
        var action = SCHActions.GetDotForLevel(72);
        Assert.Equal(SCHActions.Biolysis.ActionId, action.ActionId);
    }

    [Fact]
    public void GetDotForLevel_Level100_ReturnsBiolysis()
    {
        var action = SCHActions.GetDotForLevel(100);
        Assert.Equal(SCHActions.Biolysis.ActionId, action.ActionId);
    }

    #endregion

    #region Chain Stratagem Tests

    [Fact]
    public void TryExecute_ChainStratagemDisabled_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableBanefulImpaction = false;

        var enemy = CreateMockEnemy();
        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.ChainStratagem.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.ChainStratagem.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_ChainStratagem_Ready_WithTarget_Executes()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableChainStratagem = true;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableBanefulImpaction = false;

        var enemy = CreateMockEnemy();
        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.ChainStratagem.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.ChainStratagem.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.ChainStratagem.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Energy Drain Tests

    [Fact]
    public void TryExecute_EnergyDrainDisabled_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableBanefulImpaction = false;

        var enemy = CreateMockEnemy();
        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(3);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            aetherflowService: aetherflowService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true,
            aetherflowStacks: 3);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.EnergyDrain.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_EnergyDrain_NoAetherflowStacks_DoesNotFire()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableEnergyDrain = true;
        config.Scholar.AetherflowStrategy = AetherflowUsageStrategy.AggressiveDps;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableBanefulImpaction = false;

        var enemy = CreateMockEnemy();
        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SCHActions.EnergyDrain.ActionId)).Returns(true);

        // Zero stacks — cannot use Energy Drain
        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(0);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            aetherflowService: aetherflowService,
            level: 100,
            canExecuteGcd: false,
            canExecuteOgcd: true,
            inCombat: true,
            aetherflowStacks: 0);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.EnergyDrain.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Movement Damage Tests

    [Fact]
    public void TryExecute_Moving_Level100_UsesRuinII()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSingleTargetDamage = true;
        config.Scholar.EnableRuinII = true;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableBanefulImpaction = false;

        var enemy = CreateMockEnemy();
        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.RuinII.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100, // Level 100 has RuinII
            canExecuteGcd: true,
            canExecuteOgcd: false,
            inCombat: true,
            isMoving: true);

        // Moving should use Ruin II (instant cast)
        var result = _module.TryExecute(context, isMoving: true);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SCHActions.RuinII.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion
}
