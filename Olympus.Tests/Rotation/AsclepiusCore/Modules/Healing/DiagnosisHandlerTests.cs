using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class DiagnosisHandlerTests
{
    private readonly DiagnosisHandler _handler = new();

    [Fact]
    public void TryExecute_WhenMoving_Skips()
    {
        var context = AsclepiusTestContext.Create(canExecuteGcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: true));
    }

    [Fact]
    public void TryExecute_WhenTargetLow_ExecutesDiagnosis()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DiagnosisThreshold = 0.80f;
        config.Healing.UseDamageIntakeTriage = false;

        // target at 40% — below threshold
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 40000, maxHp: 100000);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<int>()))
            .Returns(target.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Diagnosis.ActionId),
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
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Diagnosis.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenDisabledViaThreshold_Skips()
    {
        // TryDiagnosis doesn't check EnableDiagnosis but does check DiagnosisThreshold
        // Target above threshold means skip
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DiagnosisThreshold = 0.30f;

        // target at 80% — above threshold
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 80000, maxHp: 100000);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<int>()))
            .Returns(target.Object);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true,
            level: 10);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DiagnosisThreshold = 0.50f;

        // target at 75% — above threshold
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 75000, maxHp: 100000);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<int>()))
            .Returns(target.Object);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true,
            level: 10);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenNoTarget_Skips()
    {
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((Dalamud.Game.ClientState.Objects.Types.IBattleChara?)null);

        var context = AsclepiusTestContext.Create(
            partyHelper: partyHelper,
            canExecuteGcd: true,
            level: 10);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
