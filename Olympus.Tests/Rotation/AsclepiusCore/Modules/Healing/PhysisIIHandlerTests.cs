using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class PhysisIIHandlerTests
{
    private readonly PhysisIIHandler _handler = new();

    [Fact]
    public void TryExecute_WhenConditionsMet_ExecutesPhysisII()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 2;
        config.Sage.PhysisIIThreshold = 0.90f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.65f, 0.55f, 3)); // avgHp=65%, 3 injured

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.PhysisII.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.PhysisII.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            addersgallStacks: 0); // PhysisII is free (no Addersgall cost)

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.PhysisII.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhysisII = false;

        var context = AsclepiusTestContext.Create(
            config: config,
            canExecuteOgcd: true,
            addersgallStacks: 0);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.PhysisII.ActionId)).Returns(false);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true,
            addersgallStacks: 0);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_WhenNotEnoughInjured_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.PhysisIIThreshold = 0.90f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.65f, 0.55f, 2)); // only 2 injured, threshold is 3

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.PhysisII.ActionId)).Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            addersgallStacks: 0);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_WhenAboveHpThreshold_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 2;
        config.Sage.PhysisIIThreshold = 0.70f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.80f, 0.70f, 3)); // avgHp=80% > threshold 70%

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.PhysisII.ActionId)).Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            addersgallStacks: 0);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
    }
}
