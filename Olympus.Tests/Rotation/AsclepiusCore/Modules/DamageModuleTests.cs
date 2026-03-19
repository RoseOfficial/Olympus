using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Tests for Sage DamageModule logic.
/// Covers Dosis (level-gated), Eukrasian Dosis DoT, Phlegma charges, and Toxikon (Addersting).
/// </summary>
public class DamageModuleTests
{
    private readonly DamageModule _module;

    public DamageModuleTests()
    {
        _module = new DamageModule();
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
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            level: 90);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Dosis Level-gating Tests

    [Fact]
    public void TryExecute_Dosis_Level1_UsesDosis()
    {
        // Arrange: Level 1 gets base Dosis
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = false; // Disable DoT to simplify
        config.Sage.EnablePhlegma = false;
        config.Sage.EnableToxikon = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);
        // No enemy needing DoT
        targetingService.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Dosis.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            inCombat: true,
            level: 1);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: SGE DamageModule.BlocksOnExecution = false, so result is always false
        // even when an action fires. Verify the action was executed.
        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Dosis.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_Dosis_Level82_UsesDosisIII()
    {
        // Arrange: Level 82+ gets DosisIII
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = false;
        config.Sage.EnablePhlegma = false;
        config.Sage.EnableToxikon = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.EntityId).Returns(100u);
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);
        targetingService.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.DosisIII.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            inCombat: true,
            level: 82);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: SGE DamageModule.BlocksOnExecution = false, so result is always false
        // even when an action fires. Verify the action was executed.
        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.DosisIII.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_Dosis_Moving_SkipsDosis()
    {
        // Arrange: Dosis has a cast time (1.5s), cannot use while moving.
        // Psyche is explicitly disabled to prevent oGCD false-negative: if Psyche fired
        // while moving it would mask the test intent (no instant-cast GCDs should fire).
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = false;
        config.Sage.EnablePhlegma = false;
        config.Sage.EnableToxikon = false; // No instant casts
        config.Sage.EnablePsyche = false;  // No oGCD damage ability either

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            level: 90);

        // Act: moving prevents Dosis
        var result = _module.TryExecute(context, isMoving: true);

        // Assert: Dosis not executed
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a =>
                    a.ActionId == SGEActions.Dosis.ActionId ||
                    a.ActionId == SGEActions.DosisII.ActionId ||
                    a.ActionId == SGEActions.DosisIII.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
        // Assert: Psyche not executed (disabled by config and oGCD not available)
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Psyche.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Eukrasian Dosis (DoT) Tests

    [Fact]
    public void TryExecute_DoTDisabled_SkipsDoT()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = false; // DoT disabled

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);
        targetingService.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            canExecuteOgcd: true,
            level: 90);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert: no Eukrasia activated for DoT
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_DoT_EnemyNeedsDoT_ActivatesEukrasia()
    {
        // Arrange: enemy needs DoT, Eukrasia oGCD should fire first
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = true;
        config.Sage.EnablePhlegma = false;
        config.Sage.EnablePsyche = false;
        config.Sage.EnableToxikon = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);
        targetingService.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object); // Enemy needs DoT

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            canExecuteOgcd: true,
            inCombat: true,
            hasEukrasia: false, // Eukrasia not active yet
            level: 90);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: SGE DamageModule.BlocksOnExecution = false, so result is always false.
        // Verify Eukrasia oGCD was activated for the DoT sequence.
        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_DoT_DotAlreadyActive_FallsBackToDosis()
    {
        // Arrange: DoT is active on the enemy (no target needing DoT), no oGCD available.
        // With DoT maintained and Eukrasia not active, the module falls through to single-target Dosis.
        // Note: context.HasEukrasia reads via the static AsclepiusStatusHelper (not the injected service),
        // so the Eukrasia-active branch cannot be directly exercised in unit tests; this test covers
        // the fall-through path where DoT is maintained and Dosis is cast instead.
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = true;
        config.Sage.EnablePhlegma = false;
        config.Sage.EnablePsyche = false;
        config.Sage.EnableToxikon = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);
        // DoT already active — no enemy needs DoT refresh
        targetingService.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.DosisIII.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            inCombat: true,
            level: 82);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: SGE DamageModule.BlocksOnExecution = false, so result is always false.
        // Verify DosisIII was cast as the fallback single-target action.
        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.DosisIII.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_DoT_LevelBelowEukrasia_SkipsDoT()
    {
        // Arrange: Eukrasian Dosis requires level 30
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = true;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);
        targetingService.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            canExecuteOgcd: true,
            level: 29); // Below E.Dosis level 30

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert: No Eukrasia activation for DoT
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Phlegma Tests

    [Fact]
    public void TryExecute_Phlegma_Disabled_SkipsPhlegma()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = false;
        config.Sage.EnablePhlegma = false;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            level: 82);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a =>
                    a.ActionId == SGEActions.Phlegma.ActionId ||
                    a.ActionId == SGEActions.PhlegmaIII.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Phlegma_LevelTooLow_SkipsPhlegma()
    {
        // Arrange: Phlegma requires level 26
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = false;
        config.Sage.EnablePhlegma = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteGcd: true,
            level: 25); // Below Phlegma level 26

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Phlegma.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Toxikon Tests (Addersting)

    [Fact]
    public void TryExecute_Toxikon_NoAddersting_SkipsToxikon()
    {
        // Arrange: Toxikon requires Addersting
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = false;
        config.Sage.EnablePhlegma = false;
        config.Sage.EnableToxikon = true;

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var addersting = AsclepiusTestContext.CreateMockAdderstingService(currentStacks: 0); // No Addersting

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            adderstingService: addersting,
            adderstingStacks: 0,
            canExecuteGcd: true,
            level: 66);

        // Act - note: Toxikon only fires while moving
        _module.TryExecute(context, isMoving: true);

        // Assert
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a =>
                    a.ActionId == SGEActions.Toxikon.ActionId ||
                    a.ActionId == SGEActions.ToxikonII.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Toxikon_HasAddersting_Moving_ExecutesToxikon()
    {
        // Arrange: Toxikon fires while moving with Addersting available
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = false;
        config.Sage.EnablePhlegma = false;
        config.Sage.EnablePsyche = false;
        config.Sage.EnableToxikon = true;
        config.Sage.AoEDamageMinTargets = 99; // Disable AoE

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(100ul);

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.ToxikonII.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var addersting = AsclepiusTestContext.CreateMockAdderstingService(currentStacks: 2);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            adderstingService: addersting,
            adderstingStacks: 2,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            inCombat: true,
            level: 82); // ToxikonII at 82

        // Act: moving
        var result = _module.TryExecute(context, isMoving: true);

        // Assert: SGE DamageModule.BlocksOnExecution = false, so result is always false.
        // Verify ToxikonII was cast as movement damage.
        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.ToxikonII.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

    #region No Target Tests

    [Fact]
    public void TryExecute_NoEnemy_ReturnsFalse()
    {
        // Arrange: no enemy in range
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = true;

        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => null); // No enemy

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            level: 90);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    #endregion
}
