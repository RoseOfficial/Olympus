using Olympus.Data;
using Olympus.Rotation.HephaestusCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HephaestusCore.Helpers;

/// <summary>
/// Tests for HephaestusStatusHelper utility methods.
/// </summary>
public class StatusHelperTests
{
    private readonly HephaestusStatusHelper _helper;

    public StatusHelperTests()
    {
        _helper = new HephaestusStatusHelper();
    }

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_CoreBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            GNBActions.StatusIds.RoyalGuard,
            GNBActions.StatusIds.NoMercy,
            GNBActions.StatusIds.Superbolide,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_ContinuationReady_AreDistinct()
    {
        var ids = new uint[]
        {
            GNBActions.StatusIds.ReadyToRip,
            GNBActions.StatusIds.ReadyToTear,
            GNBActions.StatusIds.ReadyToGouge,
            GNBActions.StatusIds.ReadyToBlast,
            GNBActions.StatusIds.ReadyToReign,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, GNBActions.StatusIds.RoyalGuard);
        Assert.NotEqual(0u, GNBActions.StatusIds.NoMercy);
        Assert.NotEqual(0u, GNBActions.StatusIds.Superbolide);
        Assert.NotEqual(0u, GNBActions.StatusIds.ReadyToRip);
        Assert.NotEqual(0u, GNBActions.StatusIds.ReadyToTear);
        Assert.NotEqual(0u, GNBActions.StatusIds.ReadyToGouge);
        Assert.NotEqual(0u, GNBActions.StatusIds.ReadyToBlast);
        Assert.NotEqual(0u, GNBActions.StatusIds.ReadyToReign);
        Assert.NotEqual(0u, GNBActions.StatusIds.Nebula);
        Assert.NotEqual(0u, GNBActions.StatusIds.GreatNebula);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_RoyalGuard_MatchesGameData()
    {
        Assert.Equal(1833u, GNBActions.StatusIds.RoyalGuard);
    }

    [Fact]
    public void StatusId_NoMercy_MatchesGameData()
    {
        Assert.Equal(1831u, GNBActions.StatusIds.NoMercy);
    }

    [Fact]
    public void StatusId_Superbolide_MatchesGameData()
    {
        Assert.Equal(1836u, GNBActions.StatusIds.Superbolide);
    }

    [Fact]
    public void StatusId_ReadyToRip_MatchesGameData()
    {
        Assert.Equal(1842u, GNBActions.StatusIds.ReadyToRip);
    }

    [Fact]
    public void StatusId_ReadyToReign_MatchesGameData()
    {
        Assert.Equal(3840u, GNBActions.StatusIds.ReadyToReign);
    }

    [Fact]
    public void StatusId_Nebula_MatchesGameData()
    {
        Assert.Equal(1834u, GNBActions.StatusIds.Nebula);
    }

    [Fact]
    public void StatusId_GreatNebula_MatchesGameData()
    {
        Assert.Equal(3838u, GNBActions.StatusIds.GreatNebula);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    [Fact]
    public void HasRoyalGuard_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasRoyalGuard(mock.Object));
    }

    [Fact]
    public void HasNoMercy_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasNoMercy(mock.Object));
    }

    [Fact]
    public void HasSuperbolide_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasSuperbolide(mock.Object));
    }

    [Fact]
    public void HasReadyToRip_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasReadyToRip(mock.Object));
    }

    [Fact]
    public void HasReadyToTear_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasReadyToTear(mock.Object));
    }

    [Fact]
    public void HasReadyToGouge_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasReadyToGouge(mock.Object));
    }

    [Fact]
    public void HasNebula_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasNebula(mock.Object));
    }

    [Fact]
    public void HasActiveMitigation_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasActiveMitigation(mock.Object));
    }

    [Fact]
    public void GetNoMercyRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.Equal(0f, _helper.GetNoMercyRemaining(mock.Object));
    }

    #endregion
}
