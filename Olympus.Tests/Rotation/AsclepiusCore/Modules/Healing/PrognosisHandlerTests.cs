using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class PrognosisHandlerTests
{
    private readonly PrognosisHandler _handler = new();

    [Fact]
    public void TryExecute_WhenMoving_Skips()
    {
        var context = AsclepiusTestContext.Create(canExecuteGcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: true));
    }

    [Fact]
    public void TryExecute_WhenPartyBelowThresholdAndEnoughInjured_ExecutesPrognosis()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealThreshold = 0.80f;
        config.Sage.AoEHealMinTargets = 2;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.55f, 0.45f, 3)); // below threshold, 3 injured

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            level: 10);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealThreshold = 0.70f;
        config.Sage.AoEHealMinTargets = 2;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.85f, 0.80f, 3)); // avgHp=85% > threshold=70%

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true,
            level: 10);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenNotEnoughInjured_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealThreshold = 0.80f;
        config.Sage.AoEHealMinTargets = 4;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.60f, 0.50f, 2)); // only 2 injured < 4 required

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true,
            level: 10);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
