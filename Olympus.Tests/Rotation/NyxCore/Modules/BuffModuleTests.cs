using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.NyxCore.Modules;

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
    public void TryExecute_BloodWeapon_FiresWhenReady()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Grit (tank stance) not needed — HasGrit=true so stance won't be applied
        actionService.Setup(x => x.IsActionReady(DRKActions.BloodWeapon.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRKActions.BloodWeapon.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasTankStance: true,
            hasBloodWeapon: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.BloodWeapon.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_BloodWeapon_SkipsIfAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasTankStance: true,
            hasBloodWeapon: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.BloodWeapon.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Delirium_FiresWithDarkside()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // BloodWeapon already active so it won't fire, Delirium is next
        actionService.Setup(x => x.IsActionReady(DRKActions.BloodWeapon.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRKActions.Delirium.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRKActions.Delirium.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasTankStance: true,
            hasBloodWeapon: true,       // already active — skip BloodWeapon
            hasDelirium: false,
            hasDarkside: true,
            darksideRemaining: 20f,
            bloodGauge: 60,             // enough gauge for Delirium
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.Delirium.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Delirium_SkipsWithoutDarkside()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRKActions.BloodWeapon.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRKActions.Delirium.ActionId)).Returns(true);

        // No Darkside active and timer < 5s — should skip Delirium
        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasTankStance: true,
            hasBloodWeapon: true,
            hasDelirium: false,
            hasDarkside: false,
            darksideRemaining: 0f,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.Delirium.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Grit_TankStance_ActivatedWhenMissing()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Grit ready; all other actions not ready
        actionService.Setup(x => x.IsActionReady(DRKActions.Grit.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(DRKActions.BloodWeapon.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRKActions.Delirium.ActionId)).Returns(false);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRKActions.Grit.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasTankStance: false,   // no Grit
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.Grit.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #region Helpers

    private static INyxContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        byte level = 100,
        bool hasTankStance = true,
        bool hasBloodWeapon = false,
        float bloodWeaponRemaining = 0f,
        bool hasDelirium = false,
        int deliriumStacks = 0,
        bool hasDarkside = false,
        float darksideRemaining = 0f,
        int bloodGauge = 0,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteOgcd: canExecuteOgcd);
        config ??= NyxTestContext.CreateDefaultDarkKnightConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<INyxContext>();

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

        // DRK-specific state
        mock.Setup(x => x.HasTankStance).Returns(hasTankStance);
        mock.Setup(x => x.HasGrit).Returns(hasTankStance);
        mock.Setup(x => x.HasBloodWeapon).Returns(hasBloodWeapon);
        mock.Setup(x => x.BloodWeaponRemaining).Returns(bloodWeaponRemaining);
        mock.Setup(x => x.HasDelirium).Returns(hasDelirium);
        mock.Setup(x => x.DeliriumStacks).Returns(deliriumStacks);
        mock.Setup(x => x.HasDarkside).Returns(hasDarkside);
        mock.Setup(x => x.DarksideRemaining).Returns(darksideRemaining);
        mock.Setup(x => x.BloodGauge).Returns(bloodGauge);
        mock.Setup(x => x.HasLivingShadow).Returns(false);

        var debugState = new NyxDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    #endregion
}
