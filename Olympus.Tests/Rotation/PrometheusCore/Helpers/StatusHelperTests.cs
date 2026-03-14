using Olympus.Data;
using Olympus.Rotation.PrometheusCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.PrometheusCore.Helpers;

/// <summary>
/// Tests for PrometheusStatusHelper utility methods.
/// Null-guard tests exercise the BaseStatusHelper null-safe path.
/// </summary>
public class StatusHelperTests
{
    private readonly PrometheusStatusHelper _helper = new();

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_SelfBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            MCHActions.StatusIds.Reassembled,
            MCHActions.StatusIds.Overheated,
            MCHActions.StatusIds.Hypercharged,
            MCHActions.StatusIds.FullMetalMachinist,
            MCHActions.StatusIds.ExcavatorReady,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_TargetDebuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            MCHActions.StatusIds.Wildfire,
            MCHActions.StatusIds.Bioblaster,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, MCHActions.StatusIds.Reassembled);
        Assert.NotEqual(0u, MCHActions.StatusIds.Overheated);
        Assert.NotEqual(0u, MCHActions.StatusIds.Hypercharged);
        Assert.NotEqual(0u, MCHActions.StatusIds.FullMetalMachinist);
        Assert.NotEqual(0u, MCHActions.StatusIds.ExcavatorReady);
        Assert.NotEqual(0u, MCHActions.StatusIds.Wildfire);
        Assert.NotEqual(0u, MCHActions.StatusIds.Bioblaster);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_Reassembled_MatchesGameData()
    {
        Assert.Equal(851u, MCHActions.StatusIds.Reassembled);
    }

    [Fact]
    public void StatusId_Overheated_MatchesGameData()
    {
        Assert.Equal(2688u, MCHActions.StatusIds.Overheated);
    }

    [Fact]
    public void StatusId_Wildfire_MatchesGameData()
    {
        Assert.Equal(861u, MCHActions.StatusIds.Wildfire);
    }

    [Fact]
    public void StatusId_Bioblaster_MatchesGameData()
    {
        Assert.Equal(1866u, MCHActions.StatusIds.Bioblaster);
    }

    [Fact]
    public void StatusId_ExcavatorReady_MatchesGameData()
    {
        Assert.Equal(3865u, MCHActions.StatusIds.ExcavatorReady);
    }

    #endregion

    #region Has* Self Buff Methods — Null StatusList Guard Tests

    [Fact]
    public void HasReassemble_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasReassemble(mock.Object));
    }

    [Fact]
    public void HasHypercharged_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasHypercharged(mock.Object));
    }

    [Fact]
    public void HasFullMetalMachinist_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFullMetalMachinist(mock.Object));
    }

    [Fact]
    public void HasExcavatorReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasExcavatorReady(mock.Object));
    }

    [Fact]
    public void HasTactician_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTactician(mock.Object));
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
    public void GetReassembleRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetReassembleRemaining(mock.Object));
    }

    #endregion

    #region MCHActions Lookup Helpers

    [Theory]
    [InlineData(100, 7411u)]  // HeatedSplitShot at level 100
    [InlineData(54, 7411u)]   // HeatedSplitShot at level 54
    [InlineData(53, 2866u)]   // SplitShot before 54
    public void GetComboStarter_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = MCHActions.GetComboStarter(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 36978u)]  // BlazingShot at level 100
    [InlineData(68, 36978u)]   // BlazingShot at level 68
    [InlineData(67, 7410u)]    // HeatBlast before 68
    public void GetOverheatedGcd_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = MCHActions.GetOverheatedGcd(level, aoe: false);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 16501u)]  // AutomatonQueen at level 100
    [InlineData(80, 16501u)]   // AutomatonQueen at level 80
    [InlineData(79, 2864u)]    // RookAutoturret before 80
    public void GetPetSummon_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = MCHActions.GetPetSummon(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 16500u)]  // AirAnchor at level 100
    [InlineData(76, 16500u)]   // AirAnchor at level 76
    [InlineData(75, 2872u)]    // HotShot before 76
    public void GetAirAnchor_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = MCHActions.GetAirAnchor(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 36979u)]  // DoubleCheck at level 100
    [InlineData(92, 36979u)]   // DoubleCheck at level 92
    [InlineData(91, 2874u)]    // GaussRound before 92
    public void GetGaussRound_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = MCHActions.GetGaussRound(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    #endregion
}
