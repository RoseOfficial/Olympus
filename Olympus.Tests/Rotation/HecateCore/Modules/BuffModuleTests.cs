using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Rotation.HecateCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HecateCore.Modules;

public class BuffModuleTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = HecateTestContext.Create(inCombat: false, canExecuteOgcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_OgcdNotReady_ReturnsFalse()
    {
        var context = HecateTestContext.Create(inCombat: true, canExecuteOgcd: false);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_AmplifierReady_WithPolyglotRoom_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Amplifier.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            amplifierReady: true,
            polyglotStacks: 1,       // Room to gain (max is 3 at Lv.100)
            isEnochianActive: true,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Amplifier.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_AmplifierReady_AtPolyglotCap_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            amplifierReady: true,
            polyglotStacks: 3,       // At cap — should not use
            isEnochianActive: true,
            actionService: actionService);

        Assert.False(_module.TryExecute(context, isMoving: false));
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Amplifier.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LeyLinesReady_InAstralFire_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Amplifier not ready
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.LeyLines.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            amplifierReady: false,
            leyLinesReady: true,
            hasLeyLines: false,
            inAstralFire: true,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.LeyLines.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_LeyLinesReady_WhileMoving_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            amplifierReady: false,
            leyLinesReady: true,
            hasLeyLines: false,
            inAstralFire: true,
            actionService: actionService);

        // Moving — should not use Ley Lines
        Assert.False(_module.TryExecute(context, isMoving: true));
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.LeyLines.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_TriplecastReady_IsMoving_NoInstantCast_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Triplecast.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            amplifierReady: false,
            leyLinesReady: false,
            triplecastCharges: 1,
            triplecastStacks: 0,
            hasInstantCast: false,
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: true);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Triplecast.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_ManafontReady_InFirePhase_LowMp_Fires()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Manafont.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            amplifierReady: false,
            leyLinesReady: false,
            triplecastCharges: 0,
            manafontReady: true,
            inAstralFire: true,
            currentMp: 800,  // Below 1600 threshold
            actionService: actionService);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Manafont.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_NoBuffsNeeded_ReturnsFalse()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteOgcd: true,
            amplifierReady: false,
            leyLinesReady: false,
            triplecastCharges: 0,
            manafontReady: false,
            actionService: actionService);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }
}
