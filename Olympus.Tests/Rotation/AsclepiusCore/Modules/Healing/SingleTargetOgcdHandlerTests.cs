using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class SingleTargetOgcdHandlerTests
{
    private readonly SingleTargetOgcdHandler _handler = new();

    [Fact]
    public void TryExecute_WhenDruocholeReady_AndTankLow_ExecutesDruochole()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.80f;

        var tank = MockBuilders.CreateMockBattleChara(currentHp: 40000, maxHp: 100000); // 40%

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<int>()))
            .Returns(tank.Object);
        partyHelper.Setup(x => x.GetHpPercent(tank.Object)).Returns(0.40f);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Druochole.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            addersgallStacks: 1);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenNoAddersgall_Skips()
    {
        var context = AsclepiusTestContext.Create(
            canExecuteOgcd: true,
            addersgallStacks: 0);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_WhenNoTargetNeedsHealing_Skips()
    {
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((Dalamud.Game.ClientState.Objects.Types.IBattleChara?)null);

        var context = AsclepiusTestContext.Create(
            partyHelper: partyHelper,
            canExecuteOgcd: true,
            addersgallStacks: 2);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
    }
}
