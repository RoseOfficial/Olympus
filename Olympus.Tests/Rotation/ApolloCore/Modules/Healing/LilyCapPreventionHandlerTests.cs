using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Rotation.Common;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Tests for LilyCapPreventionHandler — forces Lily spells when Lilies are at 3/3.
/// Uses a mock IApolloContext to control LilyCount (which reads game memory
/// in the real context and cannot be set in unit tests).
/// </summary>
public class LilyCapPreventionHandlerTests
{
    private readonly LilyCapPreventionHandler _handler = new();

    /// <summary>
    /// Creates a mock IApolloContext with controllable lily gauge state.
    /// </summary>
    private static Mock<IApolloContext> CreateMockContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        IBattleChara? lowestHpMember = null,
        bool inCombat = true,
        byte level = 90,
        int lilyCount = 3,
        int bloodLilyCount = 0)
    {
        config ??= new Configuration
        {
            Enabled = true,
            EnableHealing = true,
        };
        config.Healing.EnableLilyCapPrevention = true;
        config.Healing.EnableAfflatusSolace = true;
        config.Healing.EnableAfflatusRapture = true;

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        actionService ??= MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowestHpMember);
        var hpPrediction = MockBuilders.CreateMockHpPredictionService();
        var playerStats = MockBuilders.CreateMockPlayerStatsService();

        var mock = new Mock<IApolloContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.PartyHelper).Returns(partyHelper.Object);
        mock.Setup(x => x.HpPredictionService).Returns(hpPrediction.Object);
        mock.Setup(x => x.PlayerStatsService).Returns(playerStats.Object);
        mock.Setup(x => x.LilyCount).Returns(lilyCount);
        mock.Setup(x => x.BloodLilyCount).Returns(bloodLilyCount);
        mock.Setup(x => x.Debug).Returns(new DebugState());
        mock.Setup(x => x.HealingCoordination).Returns(new HealingCoordinationState());
        mock.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);

        return mock;
    }

    #region Happy Path — Lily Cap Prevention Fires

    [Fact]
    public void TryExecute_At3Lilies_InjuredTarget_ExecutesSolace()
    {
        // 3/3 Lilies, injured target available
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 49000, maxHp: 50000); // 98% HP — only slightly injured

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.AfflatusSolace.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var mockCtx = CreateMockContext(
            actionService: actionService,
            lowestHpMember: target.Object,
            lilyCount: 3,
            bloodLilyCount: 0);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.AfflatusSolace.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Config Disabled

    [Fact]
    public void TryExecute_CapPreventionDisabled_ReturnsFalse()
    {
        var config = new Configuration { Enabled = true, EnableHealing = true };
        config.Healing.EnableLilyCapPrevention = false;

        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 49000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            config: config,
            lowestHpMember: target.Object,
            lilyCount: 3);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_HealingDisabled_ReturnsFalse()
    {
        var config = new Configuration { Enabled = true, EnableHealing = false };
        config.Healing.EnableLilyCapPrevention = true;

        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 49000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            config: config,
            lowestHpMember: target.Object,
            lilyCount: 3);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Negative Cases

    [Fact]
    public void TryExecute_LiliesNotCapped_ReturnsFalse()
    {
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 49000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            lowestHpMember: target.Object,
            lilyCount: 2); // Not at cap (3)

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 49000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            lowestHpMember: target.Object,
            inCombat: false,
            lilyCount: 3);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_BelowMinLevel_ReturnsFalse()
    {
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 49000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            lowestHpMember: target.Object,
            level: 30, // Below level 52 for Afflatus Solace
            lilyCount: 3);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_NoInjuredTarget_ReturnsFalse()
    {
        // No party member with any damage
        var mockCtx = CreateMockContext(
            lowestHpMember: null, // FindLowestHpPartyMember returns null
            lilyCount: 3);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    #endregion
}
