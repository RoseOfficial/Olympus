using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AresCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.AresCore.Helpers;

/// <summary>
/// Tests for AresStatusHelper utility methods.
/// AresStatusHelper is sealed — status behavior is controlled by setting up
/// StatusList on mock characters (or passing null to exercise null-guard paths).
/// </summary>
public class StatusHelperTests
{
    private readonly AresStatusHelper _helper;

    public StatusHelperTests()
    {
        _helper = new AresStatusHelper();
    }

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_TankStance_NonZero()
    {
        Assert.NotEqual(0u, WARActions.StatusIds.Defiance);
    }

    [Fact]
    public void StatusIds_DamageBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            WARActions.StatusIds.SurgingTempest,
            WARActions.StatusIds.Berserk,
            WARActions.StatusIds.InnerRelease,
            WARActions.StatusIds.NascentChaos,
            WARActions.StatusIds.PrimalRendReady,
            WARActions.StatusIds.PrimalRuinationReady,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_DefensiveBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            WARActions.StatusIds.Holmgang,
            WARActions.StatusIds.Vengeance,
            WARActions.StatusIds.Damnation,
            WARActions.StatusIds.RawIntuition,
            WARActions.StatusIds.Bloodwhetting,
            WARActions.StatusIds.ThrillOfBattle,
            WARActions.StatusIds.Rampart,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, WARActions.StatusIds.Defiance);
        Assert.NotEqual(0u, WARActions.StatusIds.SurgingTempest);
        Assert.NotEqual(0u, WARActions.StatusIds.InnerRelease);
        Assert.NotEqual(0u, WARActions.StatusIds.NascentChaos);
        Assert.NotEqual(0u, WARActions.StatusIds.Holmgang);
        Assert.NotEqual(0u, WARActions.StatusIds.Vengeance);
        Assert.NotEqual(0u, WARActions.StatusIds.Bloodwhetting);
        Assert.NotEqual(0u, WARActions.StatusIds.ThrillOfBattle);
        Assert.NotEqual(0u, WARActions.StatusIds.Rampart);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_Defiance_MatchesGameData()
    {
        Assert.Equal(91u, WARActions.StatusIds.Defiance);
    }

    [Fact]
    public void StatusId_SurgingTempest_MatchesGameData()
    {
        Assert.Equal(2677u, WARActions.StatusIds.SurgingTempest);
    }

    [Fact]
    public void StatusId_InnerRelease_MatchesGameData()
    {
        Assert.Equal(1177u, WARActions.StatusIds.InnerRelease);
    }

    [Fact]
    public void StatusId_NascentChaos_MatchesGameData()
    {
        Assert.Equal(1897u, WARActions.StatusIds.NascentChaos);
    }

    [Fact]
    public void StatusId_Holmgang_MatchesGameData()
    {
        Assert.Equal(409u, WARActions.StatusIds.Holmgang);
    }

    [Fact]
    public void StatusId_Vengeance_MatchesGameData()
    {
        Assert.Equal(89u, WARActions.StatusIds.Vengeance);
    }

    [Fact]
    public void StatusId_Bloodwhetting_MatchesGameData()
    {
        Assert.Equal(2678u, WARActions.StatusIds.Bloodwhetting);
    }

    [Fact]
    public void StatusId_ThrillOfBattle_MatchesGameData()
    {
        Assert.Equal(87u, WARActions.StatusIds.ThrillOfBattle);
    }

    [Fact]
    public void StatusId_Rampart_MatchesGameData()
    {
        Assert.Equal(1191u, WARActions.StatusIds.Rampart);
    }

    #endregion

    #region Lookup Helpers — Level Progression

    [Theory]
    [InlineData(70, 7389u)]  // Level 70 = Inner Release
    [InlineData(6, 38u)]     // Level 6 = Berserk
    public void GetDamageBuffAction_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = WARActions.GetDamageBuffAction(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(92, 36923u)] // Damnation
    [InlineData(38, 44u)]    // Vengeance
    public void GetVengeanceAction_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = WARActions.GetVengeanceAction(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(82, 25751u)] // Bloodwhetting
    [InlineData(56, 3551u)]  // Raw Intuition
    public void GetBloodwhettingAction_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = WARActions.GetBloodwhettingAction(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(54, 3549u)]  // Fell Cleave
    [InlineData(35, 49u)]    // Inner Beast
    public void GetFellCleaveAction_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = WARActions.GetFellCleaveAction(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(60, 3550u)]  // Decimate
    [InlineData(45, 51u)]    // Steel Cyclone
    public void GetDecimateAction_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = WARActions.GetDecimateAction(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    [Fact]
    public void HasDefiance_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasDefiance(mock.Object));
    }

    [Fact]
    public void HasSurgingTempest_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasSurgingTempest(mock.Object));
    }

    [Fact]
    public void HasInnerRelease_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasInnerRelease(mock.Object));
    }

    [Fact]
    public void HasNascentChaos_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasNascentChaos(mock.Object));
    }

    [Fact]
    public void HasPrimalRendReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasPrimalRendReady(mock.Object));
    }

    [Fact]
    public void HasHolmgang_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasHolmgang(mock.Object));
    }

    [Fact]
    public void HasVengeance_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasVengeance(mock.Object));
    }

    [Fact]
    public void HasBloodwhetting_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasBloodwhetting(mock.Object));
    }

    [Fact]
    public void HasThrillOfBattle_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasThrillOfBattle(mock.Object));
    }

    [Fact]
    public void HasRampart_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasRampart(mock.Object));
    }

    [Fact]
    public void HasActiveMitigation_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasActiveMitigation(mock.Object));
    }

    [Fact]
    public void GetSurgingTempestRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.Equal(0f, _helper.GetSurgingTempestRemaining(mock.Object));
    }

    [Fact]
    public void GetInnerReleaseStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.Equal(0, _helper.GetInnerReleaseStacks(mock.Object));
    }

    [Fact]
    public void HasBerserk_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasBerserk(mock.Object));
    }

    [Fact]
    public void HasPrimalRuinationReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        Assert.False(_helper.HasPrimalRuinationReady(mock.Object));
    }

    #endregion
}
