using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Sage;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Tests for KardiaModule behavior.
/// Covers initial placement, Soteria, Philosophia, and Kardia swap logic.
/// </summary>
public class KardiaModuleTests
{
    private readonly KardiaModule _module;

    public KardiaModuleTests()
    {
        _module = new KardiaModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is3()
    {
        Assert.Equal(3, _module.Priority);
    }

    [Fact]
    public void Name_IsKardia()
    {
        Assert.Equal("Kardia", _module.Name);
    }

    #endregion

    #region Kardia Placement Tests

    [Fact]
    public void TryExecute_AutoKardiaDisabled_DoesNotPlaceKardia()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AutoKardia = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            hasKardiaPlaced: false,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Kardia.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_KardiaAlreadyPlaced_DoesNotReplace()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AutoKardia = true;
        config.Sage.EnableSoteria = false;
        config.Sage.EnablePhilosophia = false;
        config.Sage.KardiaSwapEnabled = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            hasKardiaPlaced: true,       // Already placed
            kardiaTargetId: 99ul,
            canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: no new placement
        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Kardia.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_KardiaNotPlaced_TankInParty_PlacesOnTank()
    {
        // Arrange
        var tank = MockBuilders.CreateMockBattleChara(entityId: 10u);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns(tank.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Kardia.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var kardiaManager = AsclepiusTestContext.CreateMockKardiaManager(hasKardia: false);

        var context = AsclepiusTestContext.Create(
            config: AsclepiusTestContext.CreateDefaultSageConfiguration(),
            partyHelper: partyHelper,
            actionService: actionService,
            kardiaManager: kardiaManager,
            hasKardiaPlaced: false,
            canExecuteOgcd: true,
            level: 10); // Level 10 >= Kardia min level 4

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Kardia.ActionId),
                tank.Object.GameObjectId),
            Times.Once);
    }

    [Fact]
    public void TryExecute_KardiaNotPlaced_LevelTooLow_DoesNotPlace()
    {
        // Arrange: Kardia requires level 4
        var tank = MockBuilders.CreateMockBattleChara(entityId: 10u);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns(tank.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            partyHelper: partyHelper,
            actionService: actionService,
            hasKardiaPlaced: false,
            canExecuteOgcd: true,
            level: 3); // Below Kardia level 4

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Kardia.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_KardiaNotPlaced_CannotExecuteOgcd_DoesNotPlace()
    {
        // Arrange
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: false);

        var context = AsclepiusTestContext.Create(
            actionService: actionService,
            hasKardiaPlaced: false,
            canExecuteOgcd: false); // Cannot weave

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Soteria Tests

    [Fact]
    public void TryExecute_SoteriaDisabled_SkipsSoteria()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AutoKardia = false;   // Skip placement
        config.Sage.EnableSoteria = false;
        config.Sage.EnablePhilosophia = false;
        config.Sage.KardiaSwapEnabled = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            hasKardiaPlaced: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Soteria.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_SoteriaEnabled_KardiaTargetAboveThreshold_SkipsSoteria()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AutoKardia = false;
        config.Sage.EnableSoteria = true;
        config.Sage.SoteriaThreshold = 0.65f;
        config.Sage.EnablePhilosophia = false;
        config.Sage.KardiaSwapEnabled = false;

        var kardiaTarget = MockBuilders.CreateMockBattleChara(
            entityId: 10u,
            currentHp: 40000,
            maxHp: 50000); // 80% HP, above 65% threshold

        var partyHelper = MockBuilders.CreateMockPartyHelper(
            partyMembers: new List<IBattleChara> { kardiaTarget.Object });
        partyHelper.Setup(x => x.GetAllPartyMembers(It.IsAny<IPlayerCharacter>(), It.IsAny<bool>()))
            .Returns(new List<IBattleChara> { kardiaTarget.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var kardiaManager = AsclepiusTestContext.CreateMockKardiaManager(
            hasKardia: true,
            kardiaTargetId: 10ul);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            kardiaManager: kardiaManager,
            hasKardiaPlaced: true,
            kardiaTargetId: 10ul,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Soteria.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Kardia Swap Tests

    [Fact]
    public void TryExecute_KardiaSwapDisabled_DoesNotSwap()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AutoKardia = false;
        config.Sage.EnableSoteria = false;
        config.Sage.EnablePhilosophia = false;
        config.Sage.KardiaSwapEnabled = false; // Swap disabled

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            hasKardiaPlaced: true,
            canExecuteOgcd: true,
            canSwapKardia: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryExecute_KardiaSwapEnabled_CannotSwap_DoesNotSwap()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AutoKardia = false;
        config.Sage.EnableSoteria = false;
        config.Sage.EnablePhilosophia = false;
        config.Sage.KardiaSwapEnabled = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            hasKardiaPlaced: true,
            canExecuteOgcd: true,
            canSwapKardia: false); // Swap on cooldown

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Kardia.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_KardiaOnNonTank_SwapsBackToTank()
    {
        // Arrange: Kardia is placed on a DPS (entity 10), tank is entity 20.
        // TryEnsureKardiaOnTank should move Kardia to the tank.
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AutoKardia = false; // Disable initial placement path
        config.Sage.EnableSoteria = false;
        config.Sage.EnablePhilosophia = false;

        var tank = MockBuilders.CreateMockBattleChara(entityId: 20u);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns(tank.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Kardia.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var kardiaManager = AsclepiusTestContext.CreateMockKardiaManager(
            hasKardia: true,
            kardiaTargetId: 10ul); // Currently on entity 10 (not the tank)

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            kardiaManager: kardiaManager,
            hasKardiaPlaced: true,
            kardiaTargetId: 10ul,
            canSwapKardia: true,
            canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: should swap to tank (entity 20)
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Kardia.ActionId),
                tank.Object.GameObjectId),
            Times.Once);
    }

    #endregion
}
