using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules.Healing;

public class AoEHealHandlerTests
{
    private readonly AoEHealHandler _handler = new();

    [Fact]
    public void TryExecute_WhenPartyLowEnoughInjured_ExecutesSuccor()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.AoEHealThreshold = 0.80f;
        config.Scholar.AoEHealMinTargets = 2;

        // 3 party members at ~65% HP — all below the 80% threshold
        var m1 = MockBuilders.CreateMockBattleChara(currentHp: 60000, maxHp: 100000);
        var m2 = MockBuilders.CreateMockBattleChara(currentHp: 65000, maxHp: 100000);
        var m3 = MockBuilders.CreateMockBattleChara(currentHp: 70000, maxHp: 100000);
        var partyHelper = new TestableAthenaPartyHelper(new IBattleChara[]
        {
            m1.Object, m2.Object, m3.Object
        });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SCHActions.Succor.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == SCHActions.Succor.ActionId),
            It.IsAny<ulong>()), Times.Once);
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
        config.Scholar.EnableSuccor = false;
        var context = AthenaTestContext.Create(config: config, canExecuteGcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
