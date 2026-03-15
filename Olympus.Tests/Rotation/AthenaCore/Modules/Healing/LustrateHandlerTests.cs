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

public class LustrateHandlerTests
{
    private readonly LustrateHandler _handler = new();

    [Fact]
    public void TryExecute_WhenBelowThresholdAndAetherflowAvailable_ExecutesLustrate()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.LustrateThreshold = 0.55f;
        config.Scholar.AetherflowReserve = 0;

        var injuredMember = MockBuilders.CreateMockBattleChara(currentHp: 30000, maxHp: 100000); // 30%

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SCHActions.Lustrate.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var partyHelper = new TestableAthenaPartyHelper(new[] { injuredMember.Object });
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
            It.Is<ActionDefinition>(a => a.ActionId == SCHActions.Lustrate.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenNoAetherflow_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AetherflowReserve = 0;

        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(currentStacks: 0);

        var context = AthenaTestContext.Create(
            config: config,
            aetherflowService: aetherflowService,
            canExecuteOgcd: true,
            aetherflowStacks: 0);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAetherflowAtReserve_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AetherflowReserve = 1;

        // Exactly at reserve (1 stack, reserve = 1, so stacks <= reserve)
        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(currentStacks: 1);

        var context = AthenaTestContext.Create(
            config: config,
            aetherflowService: aetherflowService,
            canExecuteOgcd: true,
            aetherflowStacks: 1);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AetherflowReserve = 0;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(false);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true,
            aetherflowStacks: 3);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.LustrateThreshold = 0.55f;
        config.Scholar.AetherflowReserve = 0;

        // Party member above threshold
        var healthyMember = MockBuilders.CreateMockBattleChara(currentHp: 80000, maxHp: 100000); // 80%

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);

        var partyHelper = new TestableAthenaPartyHelper(new[] { healthyMember.Object });
        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(currentStacks: 2);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflowService,
            canExecuteOgcd: true,
            aetherflowStacks: 2);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
