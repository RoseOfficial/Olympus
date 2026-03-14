using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Rotation.ThanatosCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ThanatosCore.Modules;

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

    #region Arcane Circle

    [Fact]
    public void TryExecute_ArcaneCircle_FiresWhenDeathsDesignActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.ArcaneCircle.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.ArcaneCircle.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            hasArcaneCircle: false,
            hasDeathsDesign: true,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.ArcaneCircle.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ArcaneCircle_SkipsWhenAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasArcaneCircle: true, // Already active
            arcaneCircleRemaining: 15f,
            hasDeathsDesign: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.ArcaneCircle.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_ArcaneCircle_SkipsWithoutDeathsDesign()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.ArcaneCircle.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasArcaneCircle: false,
            hasDeathsDesign: false, // No debuff on target
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.ArcaneCircle.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_ArcaneCircle_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 71, // Below ArcaneCircle MinLevel (72)
            hasArcaneCircle: false,
            hasDeathsDesign: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.ArcaneCircle.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Enshroud

    [Fact]
    public void TryExecute_Enshroud_FiresDuringArcaneCircle()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.ArcaneCircle.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(RPRActions.Enshroud.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Enshroud.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            shroud: 50,           // Enough Shroud
            isEnshrouded: false,
            hasSoulReaver: false,
            hasArcaneCircle: true, // Arcane Circle active → shouldEnshroud = true
            hasDeathsDesign: true,
            deathsDesignRemaining: 20f,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Enshroud.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Enshroud_FiresWhenShroudNearCap()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.ArcaneCircle.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(RPRActions.Enshroud.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Enshroud.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 100,
            shroud: 90,           // Near cap → shouldEnshroud = true
            isEnshrouded: false,
            hasSoulReaver: false,
            hasArcaneCircle: false,
            hasDeathsDesign: true,
            deathsDesignRemaining: 20f,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Enshroud.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Enshroud_SkipsWhenAlreadyEnshrouded()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            shroud: 50,
            isEnshrouded: true, // Already in Enshroud
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Enshroud.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Enshroud_SkipsWhenInSoulReaver()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Enshroud.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            shroud: 50,
            isEnshrouded: false,
            hasSoulReaver: true, // In Soul Reaver — can't Enshroud
            hasArcaneCircle: true,
            hasDeathsDesign: true,
            deathsDesignRemaining: 20f,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Enshroud.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Enshroud_SkipsWhenInsufficientShroud()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.Enshroud.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            shroud: 40, // Below 50 minimum
            isEnshrouded: false,
            hasSoulReaver: false,
            hasArcaneCircle: true,
            hasDeathsDesign: true,
            deathsDesignRemaining: 20f,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Enshroud.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Enshroud_SkipsWhenDeathsDesignLow()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RPRActions.ArcaneCircle.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(RPRActions.Enshroud.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            shroud: 90, // Near cap
            isEnshrouded: false,
            hasSoulReaver: false,
            hasArcaneCircle: false,
            hasDeathsDesign: true,
            deathsDesignRemaining: 8f, // Below 10s threshold
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Enshroud.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Enshroud_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            level: 79, // Below Enshroud MinLevel (80)
            shroud: 50,
            isEnshrouded: false,
            hasDeathsDesign: true,
            deathsDesignRemaining: 20f,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.Enshroud.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Helpers

    private static IThanatosContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        byte level = 100,
        int shroud = 0,
        int soul = 0,
        bool isEnshrouded = false,
        float enshroudTimer = 0f,
        bool hasSoulReaver = false,
        bool hasArcaneCircle = false,
        float arcaneCircleRemaining = 0f,
        bool hasDeathsDesign = false,
        float deathsDesignRemaining = 0f,
        Mock<IActionService>? actionService = null)
    {
        return ThanatosTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteOgcd: canExecuteOgcd,
            soul: soul,
            shroud: shroud,
            isEnshrouded: isEnshrouded,
            enshroudTimer: enshroudTimer,
            hasSoulReaver: hasSoulReaver,
            hasArcaneCircle: hasArcaneCircle,
            arcaneCircleRemaining: arcaneCircleRemaining,
            hasDeathsDesign: hasDeathsDesign,
            deathsDesignRemaining: deathsDesignRemaining,
            actionService: actionService);
    }

    #endregion
}
