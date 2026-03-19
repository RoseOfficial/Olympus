using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HephaestusCore.Modules;

public class MitigationModuleTests
{
    private readonly MitigationModule _module = new();

    [Fact]
    public void TryExecute_MitigationDisabled_ReturnsFalse()
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableMitigation = false;

        var context = CreateContext(inCombat: true, canExecuteOgcd: true, config: config);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Disabled", context.Debug.MitigationState);
    }

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = CreateContext(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
        Assert.Equal("Not in combat", context.Debug.MitigationState);
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        var context = CreateContext(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_EmergencySuperbolide_CriticallyLowHp_ReturnsTrue()
    {
        // HP at 10% — Superbolide should fire
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(GNBActions.Superbolide.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == GNBActions.Superbolide.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 5000,    // 10% of 50000
            maxHp: 50000,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == GNBActions.Superbolide.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Superbolide_HealthyHp_SkipsSuperbolide()
    {
        // HP at 50% — Superbolide threshold is 15%, should not fire
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Nothing ready
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 25000,   // 50%
            maxHp: 50000,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == GNBActions.Superbolide.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_FullHp_NoMitigationFires()
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.MitigationThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            currentHp: 50000,   // 100% HP
            maxHp: 50000,
            config: config,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
    }

    #region Helpers

    private static HephaestusContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        uint currentHp = 50000,
        uint maxHp = 50000,
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return HephaestusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            currentHp: currentHp,
            maxHp: maxHp,
            inCombat: inCombat,
            canExecuteOgcd: canExecuteOgcd);
    }

    #endregion
}
