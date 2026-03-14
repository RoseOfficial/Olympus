using Olympus.Data;
using Olympus.Rotation.NyxCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.NyxCore.Helpers;

/// <summary>
/// Tests for NyxStatusHelper utility methods.
/// </summary>
public class StatusHelperTests
{
    private readonly NyxStatusHelper _helper;

    public StatusHelperTests()
    {
        _helper = new NyxStatusHelper();
    }

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_CoreBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            DRKActions.StatusIds.Grit,
            DRKActions.StatusIds.BloodWeapon,
            DRKActions.StatusIds.Delirium,
            DRKActions.StatusIds.DarkArts,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_DefensiveBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            DRKActions.StatusIds.LivingDead,
            DRKActions.StatusIds.WalkingDead,
            DRKActions.StatusIds.TheBlackestNight,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, DRKActions.StatusIds.Grit);
        Assert.NotEqual(0u, DRKActions.StatusIds.BloodWeapon);
        Assert.NotEqual(0u, DRKActions.StatusIds.Delirium);
        Assert.NotEqual(0u, DRKActions.StatusIds.DarkArts);
        Assert.NotEqual(0u, DRKActions.StatusIds.LivingDead);
        Assert.NotEqual(0u, DRKActions.StatusIds.WalkingDead);
        Assert.NotEqual(0u, DRKActions.StatusIds.TheBlackestNight);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_Grit_MatchesGameData()
    {
        Assert.Equal(743u, DRKActions.StatusIds.Grit);
    }

    [Fact]
    public void StatusId_BloodWeapon_MatchesGameData()
    {
        Assert.Equal(742u, DRKActions.StatusIds.BloodWeapon);
    }

    [Fact]
    public void StatusId_Delirium_MatchesGameData()
    {
        Assert.Equal(1972u, DRKActions.StatusIds.Delirium);
    }

    [Fact]
    public void StatusId_DarkArts_MatchesGameData()
    {
        Assert.Equal(1894u, DRKActions.StatusIds.DarkArts);
    }

    [Fact]
    public void StatusId_LivingDead_MatchesGameData()
    {
        Assert.Equal(810u, DRKActions.StatusIds.LivingDead);
    }

    [Fact]
    public void StatusId_WalkingDead_MatchesGameData()
    {
        Assert.Equal(811u, DRKActions.StatusIds.WalkingDead);
    }

    [Fact]
    public void StatusId_TheBlackestNight_MatchesGameData()
    {
        Assert.Equal(1178u, DRKActions.StatusIds.TheBlackestNight);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    [Fact]
    public void HasGrit_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasGrit(mock.Object));
    }

    [Fact]
    public void HasBloodWeapon_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasBloodWeapon(mock.Object));
    }

    [Fact]
    public void HasDelirium_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasDelirium(mock.Object));
    }

    [Fact]
    public void HasDarkArts_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasDarkArts(mock.Object));
    }

    [Fact]
    public void HasLivingDead_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasLivingDead(mock.Object));
    }

    [Fact]
    public void HasWalkingDead_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasWalkingDead(mock.Object));
    }

    [Fact]
    public void HasTheBlackestNight_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasTheBlackestNight(mock.Object));
    }

    [Fact]
    public void HasActiveMitigation_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasActiveMitigation(mock.Object));
    }

    [Fact]
    public void GetBloodWeaponRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.Equal(0f, _helper.GetBloodWeaponRemaining(mock.Object));
    }

    [Fact]
    public void HasScornfulEdge_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasScornfulEdge(mock.Object));
    }

    [Fact]
    public void HasLivingShadow_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasLivingShadow(mock.Object));
    }

    [Fact]
    public void HasSaltedEarth_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasSaltedEarth(mock.Object));
    }

    [Fact]
    public void HasShadowWall_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasShadowWall(mock.Object));
    }

    [Fact]
    public void HasDarkMind_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasDarkMind(mock.Object));
    }

    [Fact]
    public void HasDarkMissionary_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasDarkMissionary(mock.Object));
    }

    [Fact]
    public void HasOblation_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasOblation(mock.Object));
    }

    [Fact]
    public void HasRampart_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasRampart(mock.Object));
    }

    [Fact]
    public void GetDeliriumStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.Equal(0, _helper.GetDeliriumStacks(mock.Object));
    }

    #endregion
}
