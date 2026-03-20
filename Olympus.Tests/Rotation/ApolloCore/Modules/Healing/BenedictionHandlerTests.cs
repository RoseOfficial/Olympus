using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Tests for BenedictionHandler — WHM emergency full-heal oGCD.
/// Validates emergency threshold, config gate, and target selection.
/// </summary>
public class BenedictionHandlerTests
{
    private readonly BenedictionHandler _handler = new();

    #region Happy Path — Emergency Benediction

    [Fact]
    public void TryExecute_TargetBelowEmergencyThreshold_ExecutesBenediction()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.BenedictionEmergencyThreshold = 0.30f;

        // Target at 20% HP — below 30% emergency threshold
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 10000, maxHp: 50000);

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);
        partyHelper.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>())).Returns(0.20f);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(WHMActions.Benediction.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Benediction.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Benediction.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Config Disabled

    [Fact]
    public void TryExecute_BenedictionDisabled_ReturnsFalse()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.EnableBenediction = false;

        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 5000, maxHp: 50000); // 10% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);
        partyHelper.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>())).Returns(0.10f);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(WHMActions.Benediction.ActionId)).Returns(true);

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Benediction.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_HealingDisabled_ReturnsFalse()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = false;

        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 5000, maxHp: 50000);

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);
        partyHelper.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>())).Returns(0.10f);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(WHMActions.Benediction.ActionId)).Returns(true);

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Benediction.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Negative Cases

    [Fact]
    public void TryExecute_TargetAboveThreshold_ReturnsFalse()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.BenedictionEmergencyThreshold = 0.30f;
        config.Healing.EnableProactiveBenediction = false; // Disable proactive to test emergency only

        // Target at 80% HP — above 30% emergency threshold
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 40000, maxHp: 50000);

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);
        partyHelper.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>())).Returns(0.80f);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(WHMActions.Benediction.ActionId)).Returns(true);

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Benediction.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_OnCooldown_ReturnsFalse()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.BenedictionEmergencyThreshold = 0.30f;

        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 5000, maxHp: 50000);

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);
        partyHelper.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>())).Returns(0.10f);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false, canExecuteOgcd: true);
        // Benediction on cooldown
        actionService.Setup(a => a.IsActionReady(WHMActions.Benediction.ActionId)).Returns(false);

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Benediction.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_NoInjuredTarget_ReturnsFalse()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();

        // No injured party member
        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: null);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(WHMActions.Benediction.ActionId)).Returns(true);

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Benediction.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion
}
