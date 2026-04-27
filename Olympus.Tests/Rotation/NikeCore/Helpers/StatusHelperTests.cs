using Olympus.Data;
using Olympus.Rotation.NikeCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.NikeCore.Helpers;

/// <summary>
/// Tests for NikeStatusHelper utility methods.
/// NikeStatusHelper is sealed — status behavior is controlled via StatusList on mock characters.
/// Null-guard tests exercise the BaseStatusHelper null-safe path.
/// </summary>
public class StatusHelperTests
{
    private readonly NikeStatusHelper _helper = new();

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_DamageBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            SAMActions.StatusIds.Fugetsu,
            SAMActions.StatusIds.Fuka,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_SpecialStateBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            SAMActions.StatusIds.MeikyoShisui,
            SAMActions.StatusIds.OgiNamikiriReady,
            SAMActions.StatusIds.KaeshiNamikiriReady,
            SAMActions.StatusIds.ZanshinReady,
            SAMActions.StatusIds.TsubameGaeshiReady,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, SAMActions.StatusIds.Fugetsu);
        Assert.NotEqual(0u, SAMActions.StatusIds.Fuka);
        Assert.NotEqual(0u, SAMActions.StatusIds.MeikyoShisui);
        Assert.NotEqual(0u, SAMActions.StatusIds.OgiNamikiriReady);
        Assert.NotEqual(0u, SAMActions.StatusIds.KaeshiNamikiriReady);
        Assert.NotEqual(0u, SAMActions.StatusIds.ZanshinReady);
        Assert.NotEqual(0u, SAMActions.StatusIds.TsubameGaeshiReady);
        Assert.NotEqual(0u, SAMActions.StatusIds.Higanbana);
        Assert.NotEqual(0u, SAMActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_Fugetsu_MatchesGameData()
    {
        Assert.Equal(1298u, SAMActions.StatusIds.Fugetsu);
    }

    [Fact]
    public void StatusId_Fuka_MatchesGameData()
    {
        Assert.Equal(1299u, SAMActions.StatusIds.Fuka);
    }

    [Fact]
    public void StatusId_MeikyoShisui_MatchesGameData()
    {
        Assert.Equal(1233u, SAMActions.StatusIds.MeikyoShisui);
    }

    [Fact]
    public void StatusId_Higanbana_MatchesGameData()
    {
        Assert.Equal(1228u, SAMActions.StatusIds.Higanbana);
    }

    [Fact]
    public void StatusId_TrueNorth_MatchesGameData()
    {
        Assert.Equal(1250u, SAMActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    [Fact]
    public void HasFugetsu_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFugetsu(mock.Object));
    }

    [Fact]
    public void HasFuka_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFuka(mock.Object));
    }

    [Fact]
    public void HasMeikyoShisui_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasMeikyoShisui(mock.Object));
    }

    [Fact]
    public void HasOgiNamikiriReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasOgiNamikiriReady(mock.Object));
    }

    [Fact]
    public void HasKaeshiNamikiriReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasKaeshiNamikiriReady(mock.Object));
    }

    [Fact]
    public void HasTsubameGaeshiReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTsubameGaeshiReady(mock.Object));
    }

    [Fact]
    public void HasZanshinReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasZanshinReady(mock.Object));
    }

    [Fact]
    public void HasTrueNorth_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTrueNorth(mock.Object));
    }

    [Fact]
    public void HasEnhancedEnpi_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasEnhancedEnpi(mock.Object));
    }

    [Fact]
    public void HasThirdEye_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasThirdEye(mock.Object));
    }

    [Fact]
    public void HasTengentsu_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTengentsu(mock.Object));
    }

    [Fact]
    public void GetFugetsuRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetFugetsuRemaining(mock.Object));
    }

    [Fact]
    public void GetFukaRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetFukaRemaining(mock.Object));
    }

    [Fact]
    public void GetMeikyoStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetMeikyoStacks(mock.Object));
    }

    #endregion

    #region SAMActions Sen Utility

    [Theory]
    [InlineData(SAMActions.SenType.None, 0)]
    [InlineData(SAMActions.SenType.Setsu, 1)]
    [InlineData(SAMActions.SenType.Getsu, 1)]
    [InlineData(SAMActions.SenType.Ka, 1)]
    [InlineData(SAMActions.SenType.Setsu | SAMActions.SenType.Getsu, 2)]
    [InlineData(SAMActions.SenType.Setsu | SAMActions.SenType.Ka, 2)]
    [InlineData(SAMActions.SenType.Getsu | SAMActions.SenType.Ka, 2)]
    [InlineData(SAMActions.SenType.Setsu | SAMActions.SenType.Getsu | SAMActions.SenType.Ka, 3)]
    public void CountSen_ReturnsCorrectCount(SAMActions.SenType sen, int expectedCount)
    {
        Assert.Equal(expectedCount, SAMActions.CountSen(sen));
    }

    [Theory]
    [InlineData(1, 7489u)] // Higanbana
    [InlineData(2, 7488u)] // Tenka Goken
    [InlineData(3, 7487u)] // Midare Setsugekka
    public void GetIaijutsuAction_ReturnsCorrectAction(int senCount, uint expectedActionId)
    {
        var type = SAMActions.GetIaijutsuType(senCount);
        var action = SAMActions.GetIaijutsuAction(type);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(SAMActions.IaijutsuType.Higanbana, 16484u)]      // Kaeshi: Higanbana
    [InlineData(SAMActions.IaijutsuType.TenkaGoken, 16485u)]     // Kaeshi: Goken
    [InlineData(SAMActions.IaijutsuType.MidareSetsugekka, 16486u)] // Kaeshi: Setsugekka
    public void GetKaeshiAction_ReturnsCorrectFollowUp(SAMActions.IaijutsuType lastIaijutsu, uint expectedActionId)
    {
        var action = SAMActions.GetKaeshiAction(lastIaijutsu);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(92, 36963u)] // Gyofu at level 92+
    [InlineData(91, 7477u)]  // Hakaze before level 92
    public void GetComboStarter_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = SAMActions.GetComboStarter(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(86, 25780u)] // Fuko at level 86+
    [InlineData(85, 7483u)]  // Fuga before level 86
    public void GetAoeComboStarter_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = SAMActions.GetAoeComboStarter(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    #endregion
}
