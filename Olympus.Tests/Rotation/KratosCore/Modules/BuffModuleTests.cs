using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Rotation.KratosCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.KratosCore.Modules;

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

    #region Riddle of Fire

    [Fact]
    public void TryExecute_RiddleOfFire_FiresWhenDisciplinedFistActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.RiddleOfFire.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfFire.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDisciplinedFist: true,
            hasRiddleOfFire: false,
            level: 100,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfFire.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_RiddleOfFire_SkipsWhenAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDisciplinedFist: true,
            hasRiddleOfFire: true, // Already active
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfFire.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_RiddleOfFire_SkipsWhenNoDisciplinedFist()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.RiddleOfFire.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDisciplinedFist: false, // No DisciplinedFist
            hasRiddleOfFire: false,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfFire.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_RiddleOfFire_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasDisciplinedFist: true,
            hasRiddleOfFire: false,
            level: 67, // Below RiddleOfFire MinLevel (68)
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfFire.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Brotherhood

    [Fact]
    public void TryExecute_Brotherhood_FiresWhenRiddleOfFireActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // RiddleOfFire not applicable (already active so it skips to Brotherhood)
        actionService.Setup(x => x.IsActionReady(MNKActions.RiddleOfFire.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(MNKActions.Brotherhood.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.Brotherhood.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasRiddleOfFire: true, // RoF active → Brotherhood fires
            hasBrotherhood: false,
            level: 100,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.Brotherhood.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Brotherhood_SkipsWhenAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasBrotherhood: true, // Already active
            hasRiddleOfFire: true,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.Brotherhood.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_Brotherhood_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasBrotherhood: false,
            hasRiddleOfFire: true,
            level: 69, // Below Brotherhood MinLevel (70)
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.Brotherhood.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Perfect Balance

    [Fact]
    public void TryExecute_PerfectBalance_FiresDuringRiddleOfFire()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.RiddleOfFire.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(MNKActions.Brotherhood.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(MNKActions.PerfectBalance.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.PerfectBalance.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasRiddleOfFire: true, // RoF active → PB fires
            hasPerfectBalance: false,
            beastChakraCount: 0, // No chakra accumulated
            level: 100,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.PerfectBalance.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_PerfectBalance_SkipsWhenAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasPerfectBalance: true, // Already active
            hasRiddleOfFire: true,
            beastChakraCount: 0,
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.PerfectBalance.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_PerfectBalance_SkipsWhenBeastChakraFull()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.PerfectBalance.ActionId)).Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasPerfectBalance: false,
            hasRiddleOfFire: true,
            beastChakraCount: 3, // Already 3 chakra — skip (about to Blitz)
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.PerfectBalance.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_PerfectBalance_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasPerfectBalance: false,
            hasRiddleOfFire: true,
            beastChakraCount: 0,
            level: 49, // Below PerfectBalance MinLevel (50)
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.PerfectBalance.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Riddle of Wind

    [Fact]
    public void TryExecute_RiddleOfWind_FiresWhenReady()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(MNKActions.RiddleOfFire.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(MNKActions.Brotherhood.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(MNKActions.PerfectBalance.ActionId)).Returns(false);
        actionService.Setup(x => x.IsActionReady(MNKActions.RiddleOfWind.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfWind.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasRiddleOfWind: false,
            hasBrotherhood: true, // Brotherhood active prevents that branch
            hasPerfectBalance: true, // PB active skips that branch
            level: 100,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfWind.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_RiddleOfWind_SkipsWhenAlreadyActive()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasRiddleOfWind: true, // Already active
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfWind.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_RiddleOfWind_BelowMinLevel_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            inCombat: true,
            canExecuteOgcd: true,
            hasRiddleOfWind: false,
            level: 71, // Below RiddleOfWind MinLevel (72)
            actionService: actionService);

        _module.TryExecute(context, isMoving: false);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfWind.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Helpers

    private static IKratosContext CreateContext(
        bool inCombat,
        bool canExecuteOgcd,
        byte level = 100,
        bool hasDisciplinedFist = true,
        bool hasRiddleOfFire = false,
        bool hasBrotherhood = false,
        bool hasPerfectBalance = false,
        bool hasRiddleOfWind = false,
        int beastChakraCount = 0,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null)
    {
        return KratosTestContext.Create(
            level: level,
            inCombat: inCombat,
            canExecuteOgcd: canExecuteOgcd,
            hasDisciplinedFist: hasDisciplinedFist,
            hasRiddleOfFire: hasRiddleOfFire,
            hasBrotherhood: hasBrotherhood,
            hasPerfectBalance: hasPerfectBalance,
            hasRiddleOfWind: hasRiddleOfWind,
            beastChakraCount: beastChakraCount,
            actionService: actionService,
            targetingService: targetingService);
    }

    #endregion
}
