using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.AthenaCore.Helpers;

/// <summary>
/// Tests for AthenaStatusHelper utility methods.
/// AthenaStatusHelper is non-static — status behavior is controlled by setting up
/// StatusList on mock characters (or passing null to exercise null-guard paths).
/// </summary>
public class StatusHelperTests
{
    private readonly AthenaStatusHelper _helper;

    public StatusHelperTests()
    {
        _helper = new AthenaStatusHelper();
    }

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_ScholarBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            SCHActions.RecitationStatusId,
            SCHActions.EmergencyTacticsStatusId,
            SCHActions.DissipationStatusId,
            SCHActions.SeraphismStatusId,
            SCHActions.ImpactImminentStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_ShieldSystem_AreDistinct()
    {
        var ids = new uint[]
        {
            SCHActions.GalvanizeStatusId,
            SCHActions.CatalyzeStatusId,
            SCHActions.ExcogitationStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_FairySystem_AreDistinct()
    {
        var ids = new uint[]
        {
            SCHActions.FeyUnionStatusId,
            SCHActions.ProtractionStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, (uint)SCHActions.GalvanizeStatusId);
        Assert.NotEqual(0u, (uint)SCHActions.CatalyzeStatusId);
        Assert.NotEqual(0u, (uint)SCHActions.ExcogitationStatusId);
        Assert.NotEqual(0u, (uint)SCHActions.RecitationStatusId);
        Assert.NotEqual(0u, (uint)SCHActions.EmergencyTacticsStatusId);
        Assert.NotEqual(0u, (uint)SCHActions.DissipationStatusId);
        Assert.NotEqual(0u, (uint)SCHActions.ChainStratagemStatusId);
        Assert.NotEqual(0u, (uint)SCHActions.FeyUnionStatusId);
        Assert.NotEqual(0u, (uint)SCHActions.SeraphismStatusId);
        Assert.NotEqual(0u, (uint)SCHActions.ProtractionStatusId);
        Assert.NotEqual(0u, (uint)SCHActions.ImpactImminentStatusId);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_Galvanize_MatchesGameData()
    {
        Assert.Equal(297u, (uint)SCHActions.GalvanizeStatusId);
    }

    [Fact]
    public void StatusId_Excogitation_MatchesGameData()
    {
        Assert.Equal(1220u, (uint)SCHActions.ExcogitationStatusId);
    }

    [Fact]
    public void StatusId_Recitation_MatchesGameData()
    {
        Assert.Equal(1896u, (uint)SCHActions.RecitationStatusId);
    }

    [Fact]
    public void StatusId_Dissipation_MatchesGameData()
    {
        Assert.Equal(791u, (uint)SCHActions.DissipationStatusId);
    }

    [Fact]
    public void StatusId_ChainStratagem_MatchesGameData()
    {
        Assert.Equal(1221u, (uint)SCHActions.ChainStratagemStatusId);
    }

    [Fact]
    public void StatusId_Seraphism_MatchesGameData()
    {
        Assert.Equal(3963u, (uint)SCHActions.SeraphismStatusId);
    }

    [Fact]
    public void StatusId_ImpactImminent_MatchesGameData()
    {
        Assert.Equal(3961u, (uint)SCHActions.ImpactImminentStatusId);
    }

    #endregion

    #region GetDotStatusId Level Progression Tests

    [Theory]
    [InlineData(2, 179u)]    // Level 2 = Bio
    [InlineData(25, 179u)]   // Level 25 = Bio (below Bio II)
    [InlineData(26, 189u)]   // Level 26 = Bio II
    [InlineData(71, 189u)]   // Level 71 = Bio II (below Biolysis)
    [InlineData(72, 1895u)]  // Level 72 = Biolysis
    [InlineData(90, 1895u)]  // Level 90 = Biolysis
    [InlineData(100, 1895u)] // Level 100 = Biolysis
    public void GetDotStatusId_ReturnsCorrectStatusForLevel(byte level, uint expectedStatusId)
    {
        var result = SCHActions.GetDotStatusId(level);
        Assert.Equal(expectedStatusId, result);
    }

    [Fact]
    public void GetDotStatusId_LevelProgression_UpgradesCorrectly()
    {
        var bio = SCHActions.GetDotStatusId(2);
        var bioII = SCHActions.GetDotStatusId(26);
        var biolysis = SCHActions.GetDotStatusId(72);

        Assert.NotEqual(bio, bioII);
        Assert.NotEqual(bioII, biolysis);
        Assert.NotEqual(bio, biolysis);
    }

    #endregion

    #region GetDamageGcdForLevel Level Progression Tests

    [Theory]
    [InlineData(1, 17869u)]   // Ruin (level 1)
    [InlineData(53, 17869u)]  // Ruin (below Broil lvl 54)
    [InlineData(54, 3584u)]   // Broil
    [InlineData(63, 3584u)]   // Broil (below Broil II)
    [InlineData(64, 7435u)]   // Broil II
    [InlineData(71, 7435u)]   // Broil II (below Broil III)
    [InlineData(72, 16541u)]  // Broil III
    [InlineData(81, 16541u)]  // Broil III (below Broil IV)
    [InlineData(82, 25865u)]  // Broil IV
    [InlineData(100, 25865u)] // Broil IV at max level
    public void GetDamageGcdForLevel_NotMoving_ReturnsCorrectAction(byte level, uint expectedActionId)
    {
        var action = SCHActions.GetDamageGcdForLevel(level, isMoving: false);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Fact]
    public void GetDamageGcdForLevel_Moving_Level38Plus_ReturnsRuinII()
    {
        var action = SCHActions.GetDamageGcdForLevel(100, isMoving: true);
        Assert.Equal(SCHActions.RuinII.ActionId, action.ActionId);
    }

    [Fact]
    public void GetDamageGcdForLevel_Moving_BelowLevel38_ReturnsCastableAction()
    {
        // Below RuinII (level 38), moving returns Ruin (the only option)
        var action = SCHActions.GetDamageGcdForLevel(10, isMoving: true);
        Assert.Equal(SCHActions.Ruin.ActionId, action.ActionId);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    // These tests verify that each Has* method safely returns false when StatusList is null,
    // exercising the defensive null-check code path in BaseStatusHelper.

    [Fact]
    public void HasRecitation_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasRecitation(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasEmergencyTactics_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasEmergencyTactics(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasDissipation_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasDissipation(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasSeraphism_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasSeraphism(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasImpactImminent_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasImpactImminent(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasProtraction_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasProtraction(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasGalvanize_NonBattleChara_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasGalvanize(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasGalvanize_BattleCharaWithNullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasGalvanize(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasCatalyze_NonBattleChara_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasCatalyze(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasExcogitation_NonBattleChara_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasExcogitation(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasExcogitation_BattleCharaWithNullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasExcogitation(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasFeyUnion_NonBattleChara_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasFeyUnion(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasFeyUnionActive_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasFeyUnionActive(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasChainStratagem_NonBattleChara_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasChainStratagem(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasChainStratagem_BattleCharaWithNullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasChainStratagem(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasOurDot_NonBattleCharaTarget_ReturnsFalse()
    {
        var player = MockBuilders.CreateMockPlayerCharacter();
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        var target = new Mock<IGameObject>();

        var result = _helper.HasOurDot(player.Object, target.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasOurDot_BattleCharaWithNullStatusList_ReturnsFalse()
    {
        var player = MockBuilders.CreateMockPlayerCharacter();
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        var target = MockBuilders.CreateMockBattleChara();
        target.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasOurDot(player.Object, target.Object);

        Assert.False(result);
    }

    [Fact]
    public void GetDotDuration_BattleCharaWithNullStatusList_ReturnsZero()
    {
        var player = MockBuilders.CreateMockPlayerCharacter();
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        var target = MockBuilders.CreateMockBattleChara();
        target.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.GetDotDuration(player.Object, target.Object);

        Assert.Equal(0f, result);
    }

    [Fact]
    public void GetGalvanizeDuration_NonBattleChara_ReturnsZero()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.GetGalvanizeDuration(mock.Object);

        Assert.Equal(0f, result);
    }

    [Fact]
    public void GetExcogitationDuration_NonBattleChara_ReturnsZero()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.GetExcogitationDuration(mock.Object);

        Assert.Equal(0f, result);
    }

    #endregion
}
