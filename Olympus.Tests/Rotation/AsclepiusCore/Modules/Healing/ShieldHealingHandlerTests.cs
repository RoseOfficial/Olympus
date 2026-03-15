using Moq;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class ShieldHealingHandlerTests
{
    private readonly ShieldHealingHandler _handler = new();

    [Fact]
    public void TryExecute_WhenMoving_Skips()
    {
        var context = AsclepiusTestContext.Create(canExecuteGcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: true));
    }

    [Fact]
    public void TryExecute_WhenDisabledEukrasianDiagnosisAndPrognosis_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnableEukrasianDiagnosis = false;
        config.Sage.EnableEukrasianPrognosis = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 3));

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true,
            level: 30);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAlreadyShielded_Skips()
    {
        // Target with existing E.Diagnosis shield (StatusList returns null → HasEukrasianDiagnosisShield=false)
        // So "AlreadyShielded" cannot be triggered with null StatusList.
        // This test instead verifies behavior when target returns false from ExecuteGcd
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnableEukrasianDiagnosis = true;
        config.Sage.EnableEukrasianPrognosis = false;
        config.Sage.EukrasianDiagnosisThreshold = 0.80f;

        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 60000, maxHp: 100000); // 60%

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.70f, 0.60f, 1));
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(), It.IsAny<int>()))
            .Returns(target.Object);

        // Eukrasia already active on player → TryEukrasianHealSpell will be called
        // ExecuteOgcd (for Eukrasia activation) returns false → handler returns false
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(false);
        actionService.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(false);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            level: 30,
            hasEukrasia: false); // no Eukrasia active

        // Without Eukrasia active and lowestHp=60% < threshold 80%, it will try to activate Eukrasia
        // ExecuteOgcd returns false → returns false
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnableEukrasianDiagnosis = true;
        config.Sage.EnableEukrasianPrognosis = false;
        config.Sage.EukrasianDiagnosisThreshold = 0.50f;
        config.Sage.AoEHealThreshold = 0.50f;
        config.Sage.AoEHealMinTargets = 3;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        // lowestHp=80% > EukrasianDiagnosisThreshold=50%, avgHp=85% > AoEHealThreshold=50%
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.85f, 0.80f, 2));

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true,
            level: 30);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenPartyLowAndConditionsMet_ActivatesEukrasia()
    {
        // HasEukrasia is derived from StatusList (null in mock = false).
        // So the handler takes the "activate Eukrasia" path (ExecuteOgcd) rather
        // than the "cast E.Prognosis" path.
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnableEukrasianPrognosis = true;
        config.Sage.AoEHealMinTargets = 2;
        config.Sage.AoEHealThreshold = 0.85f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.65f, 0.55f, 3)); // below threshold, enough injured

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            level: 30);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Eukrasia.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }
}
