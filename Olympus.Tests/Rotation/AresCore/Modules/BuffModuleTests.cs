using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.AresCore.Modules;

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
    public void TryExecute_InnerRelease_WhenSurgingTempestActive_HighGauge_ReturnsTrue()
    {
        // Inner Release fires when: Surging Tempest active, gauge >= 50
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WARActions.InnerRelease.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WARActions.InnerRelease.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasSurgingTempest: true,
            surgingTempestRemaining: 20f,
            beastGauge: 60,
            hasInnerRelease: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.InnerRelease.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_InnerRelease_WhenAlreadyActive_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasSurgingTempest: true,
            beastGauge: 60,
            hasInnerRelease: true, // Already active
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        // Inner Release should not be re-activated
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.InnerRelease.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_InnerRelease_WithoutSurgingTempest_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasSurgingTempest: false, // No Surging Tempest
            beastGauge: 60,
            hasInnerRelease: false,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.InnerRelease.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_InnerRelease_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasSurgingTempest: true,
            beastGauge: 60,
            hasInnerRelease: false,
            level: 60, // Below InnerRelease min level (70)
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.InnerRelease.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Infuriate_LowGauge_ReturnsTrue()
    {
        // Infuriate fires when gauge is <= 50 and action is ready
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WARActions.InnerRelease.ActionId)).Returns(false); // IR not ready
        actionService.Setup(x => x.IsActionReady(WARActions.Infuriate.ActionId)).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(WARActions.Infuriate.ActionId)).Returns(1u); // 1 charge available
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WARActions.Infuriate.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasSurgingTempest: true,
            beastGauge: 30, // Low gauge — room for Infuriate
            hasInnerRelease: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.Infuriate.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Infuriate_GaugeTooHigh_Skips()
    {
        // Gauge > 50 — Infuriate would overcap
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(WARActions.InnerRelease.ActionId)).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasSurgingTempest: false,
            beastGauge: 70, // Too high
            hasInnerRelease: false,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.Infuriate.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #region Helpers

    private static IAresContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        bool hasSurgingTempest = false,
        float surgingTempestRemaining = 0f,
        int beastGauge = 0,
        bool hasInnerRelease = false,
        int innerReleaseStacks = 0,
        bool hasNascentChaos = false,
        bool hasDefiance = false,
        byte level = 100,
        Mock<IActionService>? actionService = null)
    {
        actionService ??= MockBuilders.CreateMockActionService(canExecuteOgcd: canExecuteOgcd);

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var config = AresTestContext.CreateDefaultWarriorConfiguration();

        var mock = new Mock<IAresContext>();

        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(canExecuteOgcd);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(MockBuilders.CreateMockTargetingService().Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);

        // WAR-specific buff state
        mock.Setup(x => x.BeastGauge).Returns(beastGauge);
        mock.Setup(x => x.HasSurgingTempest).Returns(hasSurgingTempest);
        mock.Setup(x => x.SurgingTempestRemaining).Returns(surgingTempestRemaining);
        mock.Setup(x => x.HasInnerRelease).Returns(hasInnerRelease);
        mock.Setup(x => x.InnerReleaseStacks).Returns(innerReleaseStacks);
        mock.Setup(x => x.HasNascentChaos).Returns(hasNascentChaos);
        mock.Setup(x => x.HasDefiance).Returns(hasDefiance);
        mock.Setup(x => x.HasTankStance).Returns(hasDefiance);

        var tankCooldownService = new Mock<ITankCooldownService>();
        mock.Setup(x => x.TankCooldownService).Returns(tankCooldownService.Object);

        var debugState = new AresDebugState();
        mock.Setup(x => x.Debug).Returns(debugState);

        return mock.Object;
    }

    #endregion
}
