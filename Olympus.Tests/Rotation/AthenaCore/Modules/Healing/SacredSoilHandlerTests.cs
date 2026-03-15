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

public class SacredSoilHandlerTests
{
    private readonly SacredSoilHandler _handler = new();

    [Fact]
    public void TryExecute_WhenPartyLowAndAetherflowAvailable_ExecutesSacredSoil()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.SacredSoilThreshold = 0.75f;
        config.Scholar.SacredSoilMinTargets = 2;
        config.Scholar.AetherflowReserve = 0;

        // Two injured members within range (position default is 0,0,0)
        var m1 = MockBuilders.CreateMockBattleChara(currentHp: 50000, maxHp: 100000);
        var m2 = MockBuilders.CreateMockBattleChara(currentHp: 55000, maxHp: 100000);
        var partyHelper = new TestableAthenaPartyHelper(new[] { m1.Object, m2.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.SacredSoil.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SCHActions.SacredSoil.ActionId),
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
            It.Is<ActionDefinition>(a => a.ActionId == SCHActions.SacredSoil.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSacredSoil = false;
        var context = AthenaTestContext.Create(config: config, canExecuteOgcd: true, aetherflowStacks: 3);
        Assert.False(_handler.TryExecute(context, isMoving: false));
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
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AetherflowReserve = 0;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.SacredSoil.ActionId)).Returns(false);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true,
            aetherflowStacks: 3);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenPartyAboveThreshold_Skips()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.SacredSoilThreshold = 0.75f;
        config.Scholar.AetherflowReserve = 0;

        // Healthy party above threshold, no raidwide/burst imminent
        var m1 = MockBuilders.CreateMockBattleChara(currentHp: 90000, maxHp: 100000);
        var m2 = MockBuilders.CreateMockBattleChara(currentHp: 90000, maxHp: 100000);
        var partyHelper = new TestableAthenaPartyHelper(new[] { m1.Object, m2.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SCHActions.SacredSoil.ActionId)).Returns(true);

        var aetherflowService = AthenaTestContext.CreateMockAetherflowService(currentStacks: 3);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflowService,
            canExecuteOgcd: true,
            aetherflowStacks: 3);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
