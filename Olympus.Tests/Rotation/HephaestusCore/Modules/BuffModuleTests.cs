using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HephaestusCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        var context = CreateContext(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_NoMercy_FiresWhenReady()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(GNBActions.NoMercy.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == GNBActions.NoMercy.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasTankStance: true,
            hasNoMercy: false,
            cartridges: 2,      // enough cartridges for burst
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == GNBActions.NoMercy.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_NoMercy_SkipsIfAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasTankStance: true,
            hasNoMercy: true,
            noMercyRemaining: 15f,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == GNBActions.NoMercy.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_RoyalGuard_TankStance_ActivatedWhenMissing()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // RoyalGuard ready; all other actions not ready
        actionService.Setup(x => x.IsActionReady(GNBActions.RoyalGuard.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(GNBActions.NoMercy.ActionId)).Returns(false);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == GNBActions.RoyalGuard.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasTankStance: false,   // no Royal Guard
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == GNBActions.RoyalGuard.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_NoMercy_BelowMinLevel_DoesNotFire()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 1,   // far below No Mercy's MinLevel
            hasTankStance: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == GNBActions.NoMercy.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #region Helpers

    private static IHephaestusContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        byte level = 100,
        bool hasTankStance = true,
        bool hasNoMercy = false,
        float noMercyRemaining = 0f,
        int cartridges = 0,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteOgcd: canExecuteOgcd);
        config ??= HephaestusTestContext.CreateDefaultGunbreakerConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IHephaestusContext>();

        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        // GNB-specific state
        mock.Setup(x => x.HasTankStance).Returns(hasTankStance);
        mock.Setup(x => x.HasRoyalGuard).Returns(hasTankStance);
        mock.Setup(x => x.HasNoMercy).Returns(hasNoMercy);
        mock.Setup(x => x.NoMercyRemaining).Returns(noMercyRemaining);
        mock.Setup(x => x.Cartridges).Returns(cartridges);
        mock.Setup(x => x.CanUseGnashingFang).Returns(cartridges >= 1);
        mock.Setup(x => x.CanUseDoubleDown).Returns(cartridges >= 2);
        mock.Setup(x => x.IsInGnashingFangCombo).Returns(false);
        mock.Setup(x => x.GnashingFangStep).Returns(0);
        mock.Setup(x => x.HasAnyContinuationReady).Returns(false);
        mock.Setup(x => x.IsReadyToRip).Returns(false);
        mock.Setup(x => x.IsReadyToTear).Returns(false);
        mock.Setup(x => x.IsReadyToGouge).Returns(false);
        mock.Setup(x => x.IsReadyToBlast).Returns(false);
        mock.Setup(x => x.IsReadyToReign).Returns(false);

        var debugState = new HephaestusDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    #endregion
}
