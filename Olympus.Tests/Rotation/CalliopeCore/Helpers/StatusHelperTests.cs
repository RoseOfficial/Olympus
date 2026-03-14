using Olympus.Data;
using Olympus.Rotation.CalliopeCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.CalliopeCore.Helpers;

/// <summary>
/// Tests for CalliopeStatusHelper utility methods.
/// Null-guard tests exercise the BaseStatusHelper null-safe path.
/// </summary>
public class StatusHelperTests
{
    private readonly CalliopeStatusHelper _helper = new();

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_SelfBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            BRDActions.StatusIds.StraightShotReady,
            BRDActions.StatusIds.RagingStrikes,
            BRDActions.StatusIds.BattleVoice,
            BRDActions.StatusIds.Barrage,
            BRDActions.StatusIds.RadiantFinale,
            BRDActions.StatusIds.BlastArrowReady,
            BRDActions.StatusIds.ResonantArrowReady,
            BRDActions.StatusIds.RadiantEncoreReady,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_SongBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            BRDActions.StatusIds.MagesBallad,
            BRDActions.StatusIds.ArmysPaeon,
            BRDActions.StatusIds.WanderersMinuet,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_TargetDots_AreDistinct()
    {
        var ids = new uint[]
        {
            BRDActions.StatusIds.CausticBite,
            BRDActions.StatusIds.VenomousBite,
            BRDActions.StatusIds.Stormbite,
            BRDActions.StatusIds.Windbite,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, BRDActions.StatusIds.StraightShotReady);
        Assert.NotEqual(0u, BRDActions.StatusIds.RagingStrikes);
        Assert.NotEqual(0u, BRDActions.StatusIds.BattleVoice);
        Assert.NotEqual(0u, BRDActions.StatusIds.Barrage);
        Assert.NotEqual(0u, BRDActions.StatusIds.RadiantFinale);
        Assert.NotEqual(0u, BRDActions.StatusIds.BlastArrowReady);
        Assert.NotEqual(0u, BRDActions.StatusIds.ResonantArrowReady);
        Assert.NotEqual(0u, BRDActions.StatusIds.RadiantEncoreReady);
        Assert.NotEqual(0u, BRDActions.StatusIds.CausticBite);
        Assert.NotEqual(0u, BRDActions.StatusIds.Stormbite);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_StraightShotReady_MatchesGameData()
    {
        Assert.Equal(122u, BRDActions.StatusIds.StraightShotReady);
    }

    [Fact]
    public void StatusId_RagingStrikes_MatchesGameData()
    {
        Assert.Equal(125u, BRDActions.StatusIds.RagingStrikes);
    }

    [Fact]
    public void StatusId_BattleVoice_MatchesGameData()
    {
        Assert.Equal(141u, BRDActions.StatusIds.BattleVoice);
    }

    [Fact]
    public void StatusId_CausticBite_MatchesGameData()
    {
        Assert.Equal(1200u, BRDActions.StatusIds.CausticBite);
    }

    [Fact]
    public void StatusId_Stormbite_MatchesGameData()
    {
        Assert.Equal(1201u, BRDActions.StatusIds.Stormbite);
    }

    #endregion

    #region Has* Self Buff Methods — Null StatusList Guard Tests

    [Fact]
    public void HasHawksEye_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasHawksEye(mock.Object));
    }

    [Fact]
    public void HasRagingStrikes_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRagingStrikes(mock.Object));
    }

    [Fact]
    public void HasBattleVoice_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasBattleVoice(mock.Object));
    }

    [Fact]
    public void HasBarrage_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasBarrage(mock.Object));
    }

    [Fact]
    public void HasRadiantFinale_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRadiantFinale(mock.Object));
    }

    [Fact]
    public void HasBlastArrowReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasBlastArrowReady(mock.Object));
    }

    [Fact]
    public void HasResonantArrowReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasResonantArrowReady(mock.Object));
    }

    [Fact]
    public void HasRadiantEncoreReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRadiantEncoreReady(mock.Object));
    }

    [Fact]
    public void HasWanderersMinuet_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasWanderersMinuet(mock.Object));
    }

    [Fact]
    public void HasMagesBallad_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasMagesBallad(mock.Object));
    }

    [Fact]
    public void HasArmysPaeon_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasArmysPaeon(mock.Object));
    }

    [Fact]
    public void HasTroubadour_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTroubadour(mock.Object));
    }

    [Fact]
    public void HasNaturesMinne_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasNaturesMinne(mock.Object));
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
    public void GetRagingStrikesRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetRagingStrikesRemaining(mock.Object));
    }

    #endregion

    #region BRDActions Lookup Helpers

    [Theory]
    [InlineData(100, 16495u)] // BurstShot at level 100
    [InlineData(76, 16495u)]  // BurstShot at level 76
    [InlineData(75, 97u)]     // HeavyShot before 76
    public void GetFiller_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = BRDActions.GetFiller(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 7409u)] // RefulgentArrow at level 100
    [InlineData(70, 7409u)]  // RefulgentArrow at level 70
    [InlineData(69, 98u)]    // StraightShot before 70
    public void GetProcAction_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = BRDActions.GetProcAction(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 7406u)] // CausticBite at level 100
    [InlineData(64, 7406u)]  // CausticBite at level 64
    [InlineData(63, 100u)]   // VenomousBite before 64
    public void GetCausticBite_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = BRDActions.GetCausticBite(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 7407u)] // Stormbite at level 100
    [InlineData(64, 7407u)]  // Stormbite at level 64
    [InlineData(63, 113u)]   // Windbite before 64
    public void GetStormbite_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = BRDActions.GetStormbite(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 36975u)]  // HeartbreakShot at level 100
    [InlineData(92, 36975u)]   // HeartbreakShot at level 92
    [InlineData(91, 110u)]     // Bloodletter before 92
    public void GetBloodletter_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = BRDActions.GetBloodletter(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    #endregion
}
