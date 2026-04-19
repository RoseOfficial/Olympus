using Olympus.Data;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Rotation.KratosCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.KratosCore.Helpers;

/// <summary>
/// Tests for KratosStatusHelper utility methods.
/// KratosStatusHelper is sealed — status behavior is controlled via StatusList on mock characters.
/// Null-guard tests exercise the BaseStatusHelper null-safe path.
/// </summary>
public class StatusHelperTests
{
    private readonly KratosStatusHelper _helper = new();

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_FormStatuses_AreDistinct()
    {
        var ids = new uint[]
        {
            MNKActions.StatusIds.OpoOpoForm,
            MNKActions.StatusIds.RaptorForm,
            MNKActions.StatusIds.CoeurlForm,
            MNKActions.StatusIds.FormlessFist,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_DamageBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            MNKActions.StatusIds.LeadenFist,
            MNKActions.StatusIds.DisciplinedFist,
            MNKActions.StatusIds.RiddleOfFire,
            MNKActions.StatusIds.Brotherhood,
            MNKActions.StatusIds.PerfectBalance,
            MNKActions.StatusIds.RiddleOfWind,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_ProcStatuses_AreDistinct()
    {
        var ids = new uint[]
        {
            MNKActions.StatusIds.RaptorsFury,
            MNKActions.StatusIds.CoeurlsFury,
            MNKActions.StatusIds.OpooposFury,
            MNKActions.StatusIds.FiresRumination,
            MNKActions.StatusIds.WindsRumination,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, MNKActions.StatusIds.OpoOpoForm);
        Assert.NotEqual(0u, MNKActions.StatusIds.RaptorForm);
        Assert.NotEqual(0u, MNKActions.StatusIds.CoeurlForm);
        Assert.NotEqual(0u, MNKActions.StatusIds.FormlessFist);
        Assert.NotEqual(0u, MNKActions.StatusIds.LeadenFist);
        Assert.NotEqual(0u, MNKActions.StatusIds.DisciplinedFist);
        Assert.NotEqual(0u, MNKActions.StatusIds.RiddleOfFire);
        Assert.NotEqual(0u, MNKActions.StatusIds.Brotherhood);
        Assert.NotEqual(0u, MNKActions.StatusIds.PerfectBalance);
        Assert.NotEqual(0u, MNKActions.StatusIds.RiddleOfWind);
        Assert.NotEqual(0u, MNKActions.StatusIds.RaptorsFury);
        Assert.NotEqual(0u, MNKActions.StatusIds.CoeurlsFury);
        Assert.NotEqual(0u, MNKActions.StatusIds.OpooposFury);
        Assert.NotEqual(0u, MNKActions.StatusIds.FiresRumination);
        Assert.NotEqual(0u, MNKActions.StatusIds.WindsRumination);
        Assert.NotEqual(0u, MNKActions.StatusIds.Demolish);
        Assert.NotEqual(0u, MNKActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_OpoOpoForm_MatchesGameData()
    {
        Assert.Equal(107u, MNKActions.StatusIds.OpoOpoForm);
    }

    [Fact]
    public void StatusId_RaptorForm_MatchesGameData()
    {
        Assert.Equal(108u, MNKActions.StatusIds.RaptorForm);
    }

    [Fact]
    public void StatusId_CoeurlForm_MatchesGameData()
    {
        Assert.Equal(109u, MNKActions.StatusIds.CoeurlForm);
    }

    [Fact]
    public void StatusId_FormlessFist_MatchesGameData()
    {
        Assert.Equal(2513u, MNKActions.StatusIds.FormlessFist);
    }

    [Fact]
    public void StatusId_DisciplinedFist_MatchesGameData()
    {
        Assert.Equal(3001u, MNKActions.StatusIds.DisciplinedFist);
    }

    [Fact]
    public void StatusId_RiddleOfFire_MatchesGameData()
    {
        Assert.Equal(1181u, MNKActions.StatusIds.RiddleOfFire);
    }

    [Fact]
    public void StatusId_Brotherhood_MatchesGameData()
    {
        Assert.Equal(1185u, MNKActions.StatusIds.Brotherhood);
    }

    [Fact]
    public void StatusId_TrueNorth_MatchesGameData()
    {
        Assert.Equal(1250u, MNKActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    [Fact]
    public void HasDisciplinedFist_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasDisciplinedFist(mock.Object));
    }

    [Fact]
    public void HasLeadenFist_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasLeadenFist(mock.Object));
    }

    [Fact]
    public void HasRiddleOfFire_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRiddleOfFire(mock.Object));
    }

    [Fact]
    public void HasBrotherhood_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasBrotherhood(mock.Object));
    }

    [Fact]
    public void HasPerfectBalance_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasPerfectBalance(mock.Object));
    }

    [Fact]
    public void HasRiddleOfWind_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRiddleOfWind(mock.Object));
    }

    [Fact]
    public void HasRaptorsFury_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRaptorsFury(mock.Object));
    }

    [Fact]
    public void HasCoeurlsFury_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasCoeurlsFury(mock.Object));
    }

    [Fact]
    public void HasOpooposFury_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasOpooposFury(mock.Object));
    }

    [Fact]
    public void HasFiresRumination_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFiresRumination(mock.Object));
    }

    [Fact]
    public void HasWindsRumination_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasWindsRumination(mock.Object));
    }

    [Fact]
    public void HasTrueNorth_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTrueNorth(mock.Object));
    }

    [Fact]
    public void HasBloodbath_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasBloodbath(mock.Object));
    }

    [Fact]
    public void GetDisciplinedFistRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetDisciplinedFistRemaining(mock.Object));
    }

    [Fact]
    public void GetRiddleOfFireRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetRiddleOfFireRemaining(mock.Object));
    }

    [Fact]
    public void GetPerfectBalanceStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetPerfectBalanceStacks(mock.Object));
    }

    [Fact]
    public void HasFormlessFist_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFormlessFist(mock.Object));
    }

    [Fact]
    public void GetCurrentForm_NullStatusList_ReturnsNone()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(MonkForm.None, _helper.GetCurrentForm(mock.Object));
    }

    [Fact]
    public void HasRiddleOfEarth_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRiddleOfEarth(mock.Object));
    }

    #endregion

    #region MonkForm Enum — Values Match Context Interface

    [Fact]
    public void MonkForm_None_IsZero()
    {
        Assert.Equal(0, (int)MonkForm.None);
    }

    [Fact]
    public void MonkForm_OpoOpo_IsOne()
    {
        Assert.Equal(1, (int)MonkForm.OpoOpo);
    }

    [Fact]
    public void MonkForm_Raptor_IsTwo()
    {
        Assert.Equal(2, (int)MonkForm.Raptor);
    }

    [Fact]
    public void MonkForm_Coeurl_IsThree()
    {
        Assert.Equal(3, (int)MonkForm.Coeurl);
    }

    [Fact]
    public void MonkForm_Formless_IsFour()
    {
        Assert.Equal(4, (int)MonkForm.Formless);
    }

    #endregion

    #region MNKActions Lookup Helpers

    [Theory]
    [InlineData(100, 3547u)] // TheForbiddenChakra at level 100
    [InlineData(54, 3547u)]  // TheForbiddenChakra at level 54
    [InlineData(53, 25761u)] // SteelPeak before level 54
    public void GetChakraSpender_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = MNKActions.GetChakraSpender(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 16474u)] // Enlightenment at level 100
    [InlineData(74, 16474u)]  // Enlightenment at level 74
    [InlineData(73, 25763u)]  // HowlingFist before level 74
    [InlineData(40, 25763u)]  // HowlingFist at level 40
    public void GetAoeChakraSpender_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = MNKActions.GetAoeChakraSpender(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, true, true, 1, 1, 1, 25769u)] // PhantomRush with both Nadi
    [InlineData(100, false, false, 1, 1, 1, 36948u)] // ElixirBurst (3 same chakra, lv92+)
    [InlineData(88, false, false, 1, 1, 1, 3545u)]   // ElixirField (3 same chakra, lv60-91)
    [InlineData(100, false, false, 1, 2, 3, 25768u)] // RisingPhoenix (3 different, lv86+)
    public void GetBlitzAction_ReturnsCorrectBlitz(
        byte level, bool lunarNadi, bool solarNadi,
        byte c1, byte c2, byte c3, uint expectedActionId)
    {
        var action = MNKActions.GetBlitzAction(
            level, lunarNadi, solarNadi,
            (MNKActions.BeastChakraType)c1,
            (MNKActions.BeastChakraType)c2,
            (MNKActions.BeastChakraType)c3);
        Assert.NotNull(action);
        Assert.Equal(expectedActionId, action!.ActionId);
    }

    [Fact]
    public void GetBlitzAction_WithMissingChakra_ReturnsNull()
    {
        var action = MNKActions.GetBlitzAction(
            100, false, false,
            MNKActions.BeastChakraType.OpoOpo,
            MNKActions.BeastChakraType.None, // Missing
            MNKActions.BeastChakraType.Coeurl);
        Assert.Null(action);
    }

    #endregion
}
