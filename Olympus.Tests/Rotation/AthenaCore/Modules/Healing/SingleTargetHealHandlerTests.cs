using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules.Healing;

public class SingleTargetHealHandlerTests
{
    private readonly SingleTargetHealHandler _handler = new();

    [Fact]
    public void TryExecute_WhenTankLowAndNotMoving_ExecutesSingleTargetHeal()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AdloquiumThreshold = 0.80f;

        var tank = MockBuilders.CreateMockBattleChara(currentHp: 40000, maxHp: 100000); // 40%
        var partyHelper = new TestableAthenaPartyHelper(new[] { tank.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        // TrySingleTargetHeal selects the appropriate GCD spell
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenMoving_Skips()
    {
        var context = AthenaTestContext.Create(canExecuteGcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: true));
    }

    [Fact]
    public void TryExecute_WhenHealingDisabled_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableAdloquium = false;
        config.Scholar.EnablePhysick = false;
        var context = AthenaTestContext.Create(config: config, canExecuteGcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenTargetAboveThreshold_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AdloquiumThreshold = 0.65f;
        config.Scholar.PhysickThreshold = 0.50f;

        // Healthy member at 80% — above both thresholds
        var healthyMember = MockBuilders.CreateMockBattleChara(currentHp: 80000, maxHp: 100000);
        var partyHelper = new TestableAthenaPartyHelper(new[] { healthyMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_WhenNoPartyMembers_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        var partyHelper = new TestableAthenaPartyHelper(System.Array.Empty<Dalamud.Game.ClientState.Objects.Types.IBattleChara>());

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
