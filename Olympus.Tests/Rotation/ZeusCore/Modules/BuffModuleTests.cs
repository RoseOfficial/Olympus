using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Rotation.ZeusCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ZeusCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.BuffState);
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        var context = CreateContext(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    #region Life Surge

    [Fact]
    public void TryExecute_LifeSurge_FiresWhenFangAndClawBared()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.LifeSurge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LifeSurge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLifeSurge: false,
            hasFangAndClawBared: true,
            level: 100,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LifeSurge.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_LifeSurge_FiresWhenWheelInMotion()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.LifeSurge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LifeSurge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLifeSurge: false,
            hasWheelInMotion: true,
            level: 100,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LifeSurge.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_LifeSurge_FiresAfterVorpalThrust()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.LifeSurge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LifeSurge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLifeSurge: false,
            lastComboAction: DRGActions.VorpalThrust.ActionId,
            comboTimeRemaining: 20f,
            level: 100,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LifeSurge.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_LifeSurge_SkipsWhenAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLifeSurge: true, // Already active
            hasFangAndClawBared: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LifeSurge.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LifeSurge_SkipsWhenNoProcReady()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.LifeSurge.ActionId)).Returns(true);

        // No procs, no relevant last combo action
        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLifeSurge: false,
            hasFangAndClawBared: false,
            hasWheelInMotion: false,
            lastComboAction: 0,
            comboTimeRemaining: 0f,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LifeSurge.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LifeSurge_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLifeSurge: false,
            hasFangAndClawBared: true,
            level: 5, // Below LifeSurge MinLevel (6)
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LifeSurge.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Lance Charge

    [Fact]
    public void TryExecute_LanceCharge_FiresWhenPowerSurgeActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Life Surge not applicable (no proc ready)
        actionService.Setup(x => x.IsActionReady(DRGActions.LifeSurge.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.LanceCharge.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LanceCharge.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLanceCharge: false,
            hasPowerSurge: true,
            level: 100,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LanceCharge.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_LanceCharge_SkipsWhenAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLanceCharge: true, // Already active
            hasPowerSurge: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LanceCharge.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LanceCharge_SkipsWhenNoPowerSurge()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.LifeSurge.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.LanceCharge.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLanceCharge: false,
            hasPowerSurge: false, // No Power Surge
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LanceCharge.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LanceCharge_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLanceCharge: false,
            hasPowerSurge: true,
            level: 25, // Below LanceCharge MinLevel (30)
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.LanceCharge.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Battle Litany

    [Fact]
    public void TryExecute_BattleLitany_FiresWhenLanceChargeActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(DRGActions.LifeSurge.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.LanceCharge.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRGActions.BattleLitany.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == DRGActions.BattleLitany.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasLanceCharge: true, // Lance Charge active → Battle Litany fires
            hasBattleLitany: false,
            level: 100,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.BattleLitany.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_BattleLitany_SkipsWhenAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasBattleLitany: true, // Already active
            hasLanceCharge: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.BattleLitany.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_BattleLitany_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasBattleLitany: false,
            hasLanceCharge: true,
            level: 50, // Below BattleLitany MinLevel (52)
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.BattleLitany.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Helpers

    private static IZeusContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        byte level = 100,
        bool hasLifeSurge = false,
        bool hasFangAndClawBared = false,
        bool hasWheelInMotion = false,
        uint lastComboAction = 0,
        float comboTimeRemaining = 0f,
        bool hasLanceCharge = false,
        bool hasPowerSurge = false,
        float lanceChargeRemaining = 0f,
        bool hasBattleLitany = false,
        float battleLitanyRemaining = 0f,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return ZeusTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteOgcd: canExecuteOgcd,
            hasLifeSurge: hasLifeSurge,
            hasFangAndClawBared: hasFangAndClawBared,
            hasWheelInMotion: hasWheelInMotion,
            lastComboAction: lastComboAction,
            comboTimeRemaining: comboTimeRemaining,
            hasLanceCharge: hasLanceCharge,
            hasPowerSurge: hasPowerSurge,
            lanceChargeRemaining: lanceChargeRemaining,
            hasBattleLitany: hasBattleLitany,
            battleLitanyRemaining: battleLitanyRemaining,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
