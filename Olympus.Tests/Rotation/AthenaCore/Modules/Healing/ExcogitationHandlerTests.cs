using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Services.Scholar;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules.Healing;

public class ExcogitationHandlerTests
{
    private readonly ExcogitationHandler _handler = new();

    [Fact]
    public void TryExecute_WhenTankLowAndAetherflowAvailable_ExecutesExcogitation()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.ExcogitationThreshold = 0.80f;
        config.Scholar.AetherflowReserve = 0;

        var tank = MockBuilders.CreateMockBattleChara(currentHp: 40000, maxHp: 100000);
        var partyHelper = new TestableAthenaPartyHelper(new[] { tank.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.Excogitation.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SCHActions.Excogitation.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(currentStacks: 2);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflowService,
            canExecuteOgcd: true,
            aetherflowStacks: 2);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SCHActions.Excogitation.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenNoAetherflowAndNoRecitation_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AetherflowReserve = 0;
        var context = AthenaTestContext.Create(
            config: config,
            canExecuteOgcd: true,
            aetherflowStacks: 0);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableExcogitation = false;
        var context = AthenaTestContext.Create(config: config, canExecuteOgcd: true, aetherflowStacks: 3);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.Excogitation.ActionId)).Returns(false);

        var context = AthenaTestContext.Create(
            actionService: actionService,
            canExecuteOgcd: true,
            aetherflowStacks: 3);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.ExcogitationThreshold = 0.80f;
        config.Scholar.AetherflowReserve = 0;

        // FindExcogitationTarget returns a target at 90% HP — above threshold
        var tank = MockBuilders.CreateMockBattleChara(currentHp: 90000, maxHp: 100000);
        var partyHelper = new TestableAthenaPartyHelper(new[] { tank.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.Excogitation.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            aetherflowStacks: 2);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
