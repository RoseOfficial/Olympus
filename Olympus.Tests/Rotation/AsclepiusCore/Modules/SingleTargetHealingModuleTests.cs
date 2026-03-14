using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Tests for SingleTargetHealingModule (Druochole, Taurochole, Diagnosis).
/// Tests call sub-module methods directly without going through HealingModule.TryExecute.
/// CanExecuteOgcd/CanExecuteGcd guards live in the parent coordinator, not in sub-modules.
/// </summary>
public class SingleTargetHealingModuleTests
{
    private readonly SingleTargetHealingModule _module = new();

    #region Druochole Tests (Addersgall single-target heal)

    [Fact]
    public void TryOgcd_Druochole_NoAddersgall_SkipsDruochole()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.75f;
        config.Sage.AddersgallReserve = 0;

        var lowHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 20000, maxHp: 50000); // 40% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 0); // Empty

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 0,
            canExecuteOgcd: true);

        _module.TryOgcd(context);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryOgcd_Druochole_TargetAboveThreshold_SkipsDruochole()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.55f; // 55% threshold

        var healthyTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 40000, maxHp: 50000); // 80% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: healthyTarget.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallStacks: 3,
            canExecuteOgcd: true,
            level: 90);

        _module.TryOgcd(context);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryOgcd_Druochole_TargetBelowThreshold_ExecutesDruochole()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.75f;
        config.Sage.AddersgallReserve = 0;
        config.Sage.EnableTaurochole = false;

        var lowHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 20000, maxHp: 50000); // 40% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);
        partyHelper.Setup(x => x.GetHpPercent(lowHpTarget.Object)).Returns(0.40f);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.40f, 0.40f, 1));
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                lowHpTarget.Object.GameObjectId))
            .Returns(true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 2);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 2,
            canExecuteOgcd: true,
            level: 90);

        var result = _module.TryOgcd(context);

        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                lowHpTarget.Object.GameObjectId),
            Times.Once);
    }

    [Fact]
    public void TryOgcd_Druochole_LevelTooLow_SkipsDruochole()
    {
        // Druochole requires level 45
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.75f;

        var lowHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 20000, maxHp: 50000);

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallStacks: 3,
            canExecuteOgcd: true,
            level: 40); // Below Druochole level 45

        _module.TryOgcd(context);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Diagnosis Tests (GCD single-target)

    [Fact]
    public void TryGcd_Diagnosis_TargetAboveThreshold_SkipsDiagnosis()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.75f;
        config.Sage.EnableEukrasianDiagnosis = false;
        config.Sage.EnableEukrasianPrognosis = false;

        // Target at 90% HP — above 75% threshold
        var highHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 45000, maxHp: 50000);

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: highHpTarget.Object);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.90f, 0.90f, 0));

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 0);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 0,
            canExecuteGcd: true,
            level: 90);

        var result = _module.TryGcd(context);

        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Diagnosis.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryGcd_Diagnosis_TargetBelowThreshold_ExecutesDiagnosis()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DiagnosisThreshold = 0.75f;
        config.Sage.EnableEukrasianDiagnosis = false;
        config.Sage.EnableEukrasianPrognosis = false;

        var lowHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 20000, maxHp: 50000); // 40% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.40f, 0.40f, 1));

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 0);
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Diagnosis.ActionId),
                lowHpTarget.Object.GameObjectId))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 0,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 90);

        var result = _module.TryGcd(context);

        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Diagnosis.ActionId),
                lowHpTarget.Object.GameObjectId),
            Times.Once);
    }

    #endregion
}
