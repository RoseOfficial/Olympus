using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules.Healing;

public class ProtractionHandlerTests
{
    private readonly ProtractionHandler _handler = new();

    [Fact]
    public void TryExecute_WhenBelowThresholdAndOffCooldown_ExecutesProtraction()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.ProtractionThreshold = 0.80f;

        var injuredMember = MockBuilders.CreateMockBattleChara(currentHp: 50000, maxHp: 100000); // 50%
        var partyHelper = new TestableAthenaPartyHelper(new[] { injuredMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.Protraction.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SCHActions.Protraction.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SCHActions.Protraction.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableProtraction = false;
        var context = AthenaTestContext.Create(config: config, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.Protraction.ActionId)).Returns(false);

        var context = AthenaTestContext.Create(
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.ProtractionThreshold = 0.80f;

        // Healthy member above threshold
        var healthyMember = MockBuilders.CreateMockBattleChara(currentHp: 90000, maxHp: 100000); // 90%
        var partyHelper = new TestableAthenaPartyHelper(new[] { healthyMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.Protraction.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
