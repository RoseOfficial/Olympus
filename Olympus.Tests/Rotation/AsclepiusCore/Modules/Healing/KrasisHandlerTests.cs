using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class KrasisHandlerTests
{
    private readonly KrasisHandler _handler = new();

    [Fact]
    public void TryExecute_WhenTargetLowAndReady_ExecutesKrasis()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.KrasisThreshold = 0.80f;

        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 30000, maxHp: 100000); // 30%

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<int>()))
            .Returns(target.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Krasis.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Krasis.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 86, // Krasis min level
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Krasis.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnableKrasis = false;

        var context = AsclepiusTestContext.Create(config: config, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 30000, maxHp: 100000);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<int>()))
            .Returns(target.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Krasis.ActionId)).Returns(false);

        var context = AsclepiusTestContext.Create(
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 86);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenTargetAboveThreshold_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.KrasisThreshold = 0.50f;

        // Target at 80% — above threshold
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 80000, maxHp: 100000);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<int>()))
            .Returns(target.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Krasis.ActionId)).Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 86);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
