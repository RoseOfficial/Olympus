using Olympus.Data;
using Olympus.Rotation.ThemisCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ThemisCore.Helpers;

/// <summary>
/// Tests for ThemisStatusHelper utility methods.
/// </summary>
public class StatusHelperTests
{
    private readonly ThemisStatusHelper _helper;

    public StatusHelperTests()
    {
        _helper = new ThemisStatusHelper();
    }

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_CoreBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            PLDActions.StatusIds.IronWill,
            PLDActions.StatusIds.FightOrFlight,
            PLDActions.StatusIds.Requiescat,
            PLDActions.StatusIds.SwordOath,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_DefensiveBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            PLDActions.StatusIds.HallowedGround,
            PLDActions.StatusIds.Sentinel,
            PLDActions.StatusIds.Guardian,
            PLDActions.StatusIds.Bulwark,
            PLDActions.StatusIds.Sheltron,
            PLDActions.StatusIds.HolySheltron,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, PLDActions.StatusIds.IronWill);
        Assert.NotEqual(0u, PLDActions.StatusIds.FightOrFlight);
        Assert.NotEqual(0u, PLDActions.StatusIds.Requiescat);
        Assert.NotEqual(0u, PLDActions.StatusIds.SwordOath);
        Assert.NotEqual(0u, PLDActions.StatusIds.HallowedGround);
        Assert.NotEqual(0u, PLDActions.StatusIds.Sentinel);
        Assert.NotEqual(0u, PLDActions.StatusIds.Rampart);
        Assert.NotEqual(0u, PLDActions.StatusIds.GoringBladeDot);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_IronWill_MatchesGameData()
    {
        Assert.Equal(79u, PLDActions.StatusIds.IronWill);
    }

    [Fact]
    public void StatusId_FightOrFlight_MatchesGameData()
    {
        Assert.Equal(76u, PLDActions.StatusIds.FightOrFlight);
    }

    [Fact]
    public void StatusId_Requiescat_MatchesGameData()
    {
        Assert.Equal(1368u, PLDActions.StatusIds.Requiescat);
    }

    [Fact]
    public void StatusId_SwordOath_MatchesGameData()
    {
        Assert.Equal(1902u, PLDActions.StatusIds.SwordOath);
    }

    [Fact]
    public void StatusId_HallowedGround_MatchesGameData()
    {
        Assert.Equal(82u, PLDActions.StatusIds.HallowedGround);
    }

    [Fact]
    public void StatusId_GoringBladeDot_MatchesGameData()
    {
        Assert.Equal(725u, PLDActions.StatusIds.GoringBladeDot);
    }

    #endregion

    #region Lookup Helpers — Level Progression

    [Fact]
    public void GetComboFinisher_Level60_ReturnsRoyalAuthority()
    {
        var action = PLDActions.GetComboFinisher(60);
        Assert.Equal(PLDActions.RoyalAuthority.ActionId, action.ActionId);
    }

    [Fact]
    public void GetComboFinisher_Level26_ReturnsRageOfHalone()
    {
        var action = PLDActions.GetComboFinisher(26);
        Assert.Equal(PLDActions.RageOfHalone.ActionId, action.ActionId);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    [Fact]
    public void HasIronWill_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasIronWill(mock.Object));
    }

    [Fact]
    public void HasFightOrFlight_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasFightOrFlight(mock.Object));
    }

    [Fact]
    public void HasRequiescat_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasRequiescat(mock.Object));
    }

    [Fact]
    public void HasSwordOath_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasSwordOath(mock.Object));
    }

    [Fact]
    public void HasHallowedGround_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasHallowedGround(mock.Object));
    }

    [Fact]
    public void HasSentinel_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasSentinel(mock.Object));
    }

    [Fact]
    public void HasRampart_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasRampart(mock.Object));
    }

    [Fact]
    public void HasSheltron_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasSheltron(mock.Object));
    }

    [Fact]
    public void HasActiveMitigation_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasActiveMitigation(mock.Object));
    }

    [Fact]
    public void GetFightOrFlightRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.Equal(0f, _helper.GetFightOrFlightRemaining(mock.Object));
    }

    [Fact]
    public void GetGoringBladeRemaining_NullTarget_ReturnsZero()
    {
        var result = _helper.GetGoringBladeRemaining(null, 1u);
        Assert.Equal(0f, result);
    }

    [Fact]
    public void HasBulwark_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasBulwark(mock.Object));
    }

    [Fact]
    public void HasArmsLength_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasArmsLength(mock.Object));
    }

    [Fact]
    public void GetRequiescatStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.Equal(0, _helper.GetRequiescatStacks(mock.Object));
    }

    [Fact]
    public void GetSwordOathStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.Equal(0, _helper.GetSwordOathStacks(mock.Object));
    }

    #endregion
}
