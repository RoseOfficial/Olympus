using Moq;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class PneumaHandlerTests
{
    private readonly PneumaHandler _handler = new();

    [Fact]
    public void TryExecute_WhenMoving_Skips()
    {
        var context = AsclepiusTestContext.Create(canExecuteGcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: true));
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePneuma = false;

        var context = AsclepiusTestContext.Create(config: config, canExecuteGcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SGEActions.Pneuma.ActionId)).Returns(false);

        var context = AsclepiusTestContext.Create(
            actionService: actionService,
            canExecuteGcd: true,
            level: 90);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenNotEnoughInjured_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.PneumaThreshold = 0.60f;
        config.Sage.AoEHealMinTargets = 3;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        // avgHp=90% > threshold 60%, injuredCount=2 < 3
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.90f, 0.85f, 2));

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SGEActions.Pneuma.ActionId)).Returns(true);

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(100ul);
        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            level: 90);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenPartyLowAndEnemyInRange_ExecutesPneuma()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.PneumaThreshold = 0.80f;
        config.Sage.AoEHealMinTargets = 2;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.55f, 0.45f, 3)); // below threshold, enough injured

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SGEActions.Pneuma.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Pneuma.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(100ul);
        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            targetingService: targetingService,
            canExecuteGcd: true,
            level: 90);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Pneuma.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }
}
