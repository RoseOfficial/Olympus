using Olympus.Data;
using Olympus.Rotation.TerpsichoreCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.TerpsichoreCore.Helpers;

/// <summary>
/// Tests for TerpsichoreStatusHelper utility methods.
/// Null-guard tests exercise the BaseStatusHelper null-safe path.
/// </summary>
public class StatusHelperTests
{
    private readonly TerpsichoreStatusHelper _helper = new();

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_ProcBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            DNCActions.StatusIds.SilkenSymmetry,
            DNCActions.StatusIds.SilkenFlow,
            DNCActions.StatusIds.FlourishingSymmetry,
            DNCActions.StatusIds.FlourishingFlow,
            DNCActions.StatusIds.ThreefoldFanDance,
            DNCActions.StatusIds.FourfoldFanDance,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_BurstBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            DNCActions.StatusIds.Devilment,
            DNCActions.StatusIds.TechnicalFinish,
            DNCActions.StatusIds.StandardFinish,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_HighLevelProcs_AreDistinct()
    {
        var ids = new uint[]
        {
            DNCActions.StatusIds.LastDanceReady,
            DNCActions.StatusIds.FinishingMoveReady,
            DNCActions.StatusIds.DanceOfTheDawnReady,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, DNCActions.StatusIds.SilkenSymmetry);
        Assert.NotEqual(0u, DNCActions.StatusIds.SilkenFlow);
        Assert.NotEqual(0u, DNCActions.StatusIds.ThreefoldFanDance);
        Assert.NotEqual(0u, DNCActions.StatusIds.FourfoldFanDance);
        Assert.NotEqual(0u, DNCActions.StatusIds.Devilment);
        Assert.NotEqual(0u, DNCActions.StatusIds.TechnicalFinish);
        Assert.NotEqual(0u, DNCActions.StatusIds.StandardFinish);
        Assert.NotEqual(0u, DNCActions.StatusIds.FlourishingFinish);
        Assert.NotEqual(0u, DNCActions.StatusIds.FlourishingStarfall);
        Assert.NotEqual(0u, DNCActions.StatusIds.LastDanceReady);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_SilkenSymmetry_MatchesGameData()
    {
        Assert.Equal(2693u, DNCActions.StatusIds.SilkenSymmetry);
    }

    [Fact]
    public void StatusId_Devilment_MatchesGameData()
    {
        Assert.Equal(1825u, DNCActions.StatusIds.Devilment);
    }

    [Fact]
    public void StatusId_TechnicalFinish_MatchesGameData()
    {
        Assert.Equal(1822u, DNCActions.StatusIds.TechnicalFinish);
    }

    [Fact]
    public void StatusId_StandardFinish_MatchesGameData()
    {
        Assert.Equal(1821u, DNCActions.StatusIds.StandardFinish);
    }

    [Fact]
    public void StatusId_ClosedPosition_MatchesGameData()
    {
        Assert.Equal(1823u, DNCActions.StatusIds.ClosedPosition);
    }

    #endregion

    #region Has* Buff Methods — Null StatusList Guard Tests

    [Fact]
    public void HasSilkenSymmetry_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasSilkenSymmetry(mock.Object));
    }

    [Fact]
    public void HasSilkenFlow_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasSilkenFlow(mock.Object));
    }

    [Fact]
    public void HasThreefoldFanDance_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasThreefoldFanDance(mock.Object));
    }

    [Fact]
    public void HasFourfoldFanDance_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFourfoldFanDance(mock.Object));
    }

    [Fact]
    public void HasFlourishingFinish_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFlourishingFinish(mock.Object));
    }

    [Fact]
    public void HasFlourishingStarfall_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFlourishingStarfall(mock.Object));
    }

    [Fact]
    public void HasDevilment_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasDevilment(mock.Object));
    }

    [Fact]
    public void HasStandardFinish_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasStandardFinish(mock.Object));
    }

    [Fact]
    public void HasTechnicalFinish_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTechnicalFinish(mock.Object));
    }

    [Fact]
    public void HasLastDanceReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasLastDanceReady(mock.Object));
    }

    [Fact]
    public void HasFinishingMoveReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFinishingMoveReady(mock.Object));
    }

    [Fact]
    public void HasDanceOfTheDawnReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasDanceOfTheDawnReady(mock.Object));
    }

    [Fact]
    public void HasClosedPosition_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasClosedPosition(mock.Object));
    }

    [Fact]
    public void HasShieldSamba_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasShieldSamba(mock.Object));
    }

    [Fact]
    public void HasImprovisation_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasImprovisation(mock.Object));
    }

    [Fact]
    public void HasArmsLength_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasArmsLength(mock.Object));
    }

    [Fact]
    public void HasPeloton_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasPeloton(mock.Object));
    }

    [Fact]
    public void GetDevilmentRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetDevilmentRemaining(mock.Object));
    }

    [Fact]
    public void HasDancePartnerFrom_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasDancePartnerFrom(mock.Object, 0u));
    }

    #endregion

    #region DNCActions Lookup Helpers

    [Theory]
    [InlineData(1, 15999u)] // Emboite
    [InlineData(2, 16000u)] // Entrechat
    [InlineData(3, 16001u)] // Jete
    [InlineData(4, 16002u)] // Pirouette
    public void GetStepAction_ReturnsCorrectActionForStep(byte step, uint expectedActionId)
    {
        var action = DNCActions.GetStepAction(step);
        Assert.NotNull(action);
        Assert.Equal(expectedActionId, action!.ActionId);
    }

    [Fact]
    public void GetStepAction_ReturnsNull_ForInvalidStep()
    {
        var action = DNCActions.GetStepAction(0);
        Assert.Null(action);
    }

    [Fact]
    public void GetStepAction_ReturnsNull_ForStepFive()
    {
        var action = DNCActions.GetStepAction(5);
        Assert.Null(action);
    }

    #endregion
}
