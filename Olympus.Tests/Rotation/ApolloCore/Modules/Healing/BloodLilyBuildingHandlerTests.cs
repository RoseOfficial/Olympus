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
/// Tests for BloodLilyBuildingHandler — prioritizes Lily heals when close to Afflatus Misery.
/// Uses a mock IApolloContext to control LilyCount and BloodLilyCount (which read game memory
/// in the real context and cannot be set in unit tests).
/// </summary>
public class BloodLilyBuildingHandlerTests
{
    private readonly BloodLilyBuildingHandler _handler = new();

    /// <summary>
    /// Creates a mock IApolloContext with controllable lily gauge state.
    /// </summary>
    private static Mock<IApolloContext> CreateMockContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        IBattleChara? lowestHpMember = null,
        bool inCombat = true,
        byte level = 90,
        int lilyCount = 1,
        int bloodLilyCount = 2)
    {
        config ??= new Configuration
        {
            Enabled = true,
            EnableHealing = true,
        };
        config.Healing.EnableAggressiveLilyFlush = true;
        config.Healing.EnableAfflatusSolace = true;
        config.Healing.EnableAfflatusRapture = true;
        config.Healing.LilyStrategy = LilyGenerationStrategy.Balanced;

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

    #region Happy Path — Blood Lily Building Fires

    [Fact]
    public void TryExecute_At2BloodLilies_InjuredTarget_ExecutesSolace()
    {
        // 2 Blood Lilies, 1 Lily available, injured target below threshold
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 35000, maxHp: 50000); // 70% HP

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.AfflatusSolace.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var mockCtx = CreateMockContext(
            actionService: actionService,
            lowestHpMember: target.Object,
            lilyCount: 1,
            bloodLilyCount: 2);

        mockCtx.Setup(x => x.PartyHelper.GetHpPercent(It.IsAny<IBattleChara>())).Returns(0.70f);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.AfflatusSolace.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Config Disabled

    [Fact]
    public void TryExecute_AggressiveLilyFlushDisabled_ReturnsFalse()
    {
        var config = new Configuration { Enabled = true, EnableHealing = true };
        config.Healing.EnableAggressiveLilyFlush = false;

        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 35000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            config: config,
            lowestHpMember: target.Object,
            lilyCount: 1,
            bloodLilyCount: 2);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_LilyStrategyDisabled_ReturnsFalse()
    {
        var config = new Configuration { Enabled = true, EnableHealing = true };
        config.Healing.EnableAggressiveLilyFlush = true;
        config.Healing.LilyStrategy = LilyGenerationStrategy.Disabled;

        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 35000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            config: config,
            lowestHpMember: target.Object,
            lilyCount: 1,
            bloodLilyCount: 2);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Negative Cases

    [Fact]
    public void TryExecute_BloodLiliesBelow2_ReturnsFalse()
    {
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 35000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            lowestHpMember: target.Object,
            lilyCount: 1,
            bloodLilyCount: 1); // Only 1 Blood Lily, need 2

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_NoLiliesAvailable_ReturnsFalse()
    {
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 35000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            lowestHpMember: target.Object,
            lilyCount: 0, // No Lilies to spend
            bloodLilyCount: 2);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 35000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            lowestHpMember: target.Object,
            inCombat: false,
            lilyCount: 1,
            bloodLilyCount: 2);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_BelowMinLevel_ReturnsFalse()
    {
        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 35000, maxHp: 50000);

        var mockCtx = CreateMockContext(
            lowestHpMember: target.Object,
            level: 30, // Below level 52 for Afflatus Solace
            lilyCount: 1,
            bloodLilyCount: 2);

        var result = _handler.TryExecute(mockCtx.Object, isMoving: false);

        Assert.False(result);
    }

    #endregion
}
