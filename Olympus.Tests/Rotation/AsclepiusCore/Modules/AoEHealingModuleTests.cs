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
/// Tests for AoEHealingModule (Ixochole, Kerachole, PhysisII, Pneuma, Prognosis).
/// Tests call sub-module methods directly without going through HealingModule.TryExecute.
/// CanExecuteOgcd/CanExecuteGcd guards live in the parent coordinator, not in sub-modules.
/// </summary>
public class AoEHealingModuleTests
{
    private readonly AoEHealingModule _module = new();

    #region Prognosis Tests (GCD AoE heal)

    [Fact]
    public void TryGcdPrognosis_NotEnoughInjured_SkipsPrognosis()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.AoEHealThreshold = 0.80f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.70f, 0.50f, 2)); // Only 2 injured, need 3

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 90);

        _module.TryGcdPrognosis(context);

        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryGcdPrognosis_EnoughInjured_ExecutesPrognosis()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.AoEHealThreshold = 0.80f;
        config.Sage.AddersgallReserve = 0;
        config.Sage.EnablePneuma = false;
        config.Sage.EnableEukrasianPrognosis = false;
        config.Sage.EnableEukrasianDiagnosis = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.65f, 0.50f, 4)); // 4 injured at 65% avg — triggers Prognosis
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((IBattleChara?)null);
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 0);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 0,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 90);

        var result = _module.TryGcdPrognosis(context);

        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

    #region PhysisII Tests (Free oGCD)

    [Fact]
    public void TryOgcd_PhysisII_Disabled_SkipsPhysisII()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhysisII = false;
        // Disable other AoE heals so only PhysisII path is relevant
        config.Sage.EnableIxochole = false;
        config.Sage.EnableKerachole = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 4));

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 90);

        _module.TryOgcd(context);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.PhysisII.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryOgcd_PhysisII_LevelTooLow_SkipsPhysisII()
    {
        // PhysisII requires level 60
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhysisII = true;
        config.Sage.PhysisIIThreshold = 0.80f;
        config.Sage.EnableIxochole = false;
        config.Sage.EnableKerachole = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 4));

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.PhysisII.ActionId)).Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 55); // Below PhysisII level 60

        _module.TryOgcd(context);

        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.PhysisII.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion
}
