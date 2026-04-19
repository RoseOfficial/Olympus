using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.AstraeaCore.Helpers;

/// <summary>
/// Tests for AstraeaStatusHelper utility methods.
/// AstraeaStatusHelper is sealed — status behavior is controlled by setting up
/// StatusList on mock characters (or passing null to exercise null-guard paths).
/// </summary>
public class StatusHelperTests
{
    private readonly AstraeaStatusHelper _helper;

    public StatusHelperTests()
    {
        _helper = new AstraeaStatusHelper();
    }

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_CoreBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            ASTActions.LightspeedStatusId,
            ASTActions.NeutralSectStatusId,
            ASTActions.DiviningStatusId,
            ASTActions.DivinationStatusId,
            ASTActions.SynastryStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_HoroscopeSystem_AreDistinct()
    {
        var ids = new uint[]
        {
            ASTActions.HoroscopeStatusId,
            ASTActions.HoroscopeHeliosStatusId,
            ASTActions.MacrocosmosStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_DoTSystem_AreDistinct()
    {
        var ids = new uint[]
        {
            ASTActions.CombustStatusId,
            ASTActions.CombustIIStatusId,
            ASTActions.CombustIIIStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_CardBuffs_AreDistinct()
    {
        // LordOfCrownsStatusId is intentionally omitted: in Olympus's current data model
        // it shares the same status ID as TheSpear (3889). Lord of Crowns is primarily a
        // damage AoE in live game data; the Olympus constant is retained for documentation.
        var ids = new uint[]
        {
            ASTActions.TheBalanceStatusId,
            ASTActions.TheSpearStatusId,
            ASTActions.LadyOfCrownsStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, (uint)ASTActions.LightspeedStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.NeutralSectStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.DiviningStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.DivinationStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.SynastryStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.HoroscopeStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.HoroscopeHeliosStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.MacrocosmosStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.CombustStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.CombustIIStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.CombustIIIStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.ExaltationStatusId);
        Assert.NotEqual(0u, (uint)ASTActions.AspectedBeneficStatusId);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_Lightspeed_MatchesGameData()
    {
        Assert.Equal(841u, (uint)ASTActions.LightspeedStatusId);
    }

    [Fact]
    public void StatusId_NeutralSect_MatchesGameData()
    {
        Assert.Equal(1892u, (uint)ASTActions.NeutralSectStatusId);
    }

    [Fact]
    public void StatusId_Divination_MatchesGameData()
    {
        Assert.Equal(1878u, (uint)ASTActions.DivinationStatusId);
    }

    [Fact]
    public void StatusId_Divining_MatchesGameData()
    {
        Assert.Equal(3893u, (uint)ASTActions.DiviningStatusId);
    }

    [Fact]
    public void StatusId_Horoscope_MatchesGameData()
    {
        Assert.Equal(1890u, (uint)ASTActions.HoroscopeStatusId);
    }

    [Fact]
    public void StatusId_Macrocosmos_MatchesGameData()
    {
        Assert.Equal(2718u, (uint)ASTActions.MacrocosmosStatusId);
    }

    [Fact]
    public void StatusId_AspectedBenefic_MatchesGameData()
    {
        Assert.Equal(835u, (uint)ASTActions.AspectedBeneficStatusId);
    }

    [Fact]
    public void StatusId_Exaltation_MatchesGameData()
    {
        Assert.Equal(2717u, (uint)ASTActions.ExaltationStatusId);
    }

    [Fact]
    public void StatusId_CombustIII_MatchesGameData()
    {
        Assert.Equal(1881u, (uint)ASTActions.CombustIIIStatusId);
    }

    [Fact]
    public void StatusId_TheBalance_MatchesGameData()
    {
        Assert.Equal(3887u, (uint)ASTActions.TheBalanceStatusId);
    }

    [Fact]
    public void StatusId_TheSpear_MatchesGameData()
    {
        Assert.Equal(3889u, (uint)ASTActions.TheSpearStatusId);
    }

    #endregion

    #region GetDotStatusId Level Progression Tests

    [Theory]
    [InlineData(4, 838u)]    // Level 4 = Combust
    [InlineData(45, 838u)]   // Level 45 = Combust (below II)
    [InlineData(46, 843u)]   // Level 46 = Combust II
    [InlineData(71, 843u)]   // Level 71 = Combust II (below III)
    [InlineData(72, 1881u)]  // Level 72 = Combust III
    [InlineData(90, 1881u)]  // Level 90 = Combust III
    [InlineData(100, 1881u)] // Level 100 = Combust III
    public void GetDotStatusId_ReturnsCorrectStatusForLevel(byte level, uint expectedStatusId)
    {
        var result = ASTActions.GetDotStatusId(level);
        Assert.Equal(expectedStatusId, result);
    }

    [Fact]
    public void GetDotStatusId_LevelProgression_UpgradesCorrectly()
    {
        var combust = ASTActions.GetDotStatusId(4);
        var combustII = ASTActions.GetDotStatusId(46);
        var combustIII = ASTActions.GetDotStatusId(72);

        Assert.NotEqual(combust, combustII);
        Assert.NotEqual(combustII, combustIII);
        Assert.NotEqual(combust, combustIII);
    }

    #endregion

    #region GetDamageGcdForLevel Level Progression Tests

    [Theory]
    [InlineData(1, 3596u)]   // Malefic
    [InlineData(53, 3596u)]  // Malefic (below II)
    [InlineData(54, 3598u)]  // Malefic II
    [InlineData(63, 3598u)]  // Malefic II (below III)
    [InlineData(64, 7442u)]  // Malefic III
    [InlineData(71, 7442u)]  // Malefic III (below IV)
    [InlineData(72, 16555u)] // Malefic IV
    [InlineData(81, 16555u)] // Malefic IV (below Fall)
    [InlineData(82, 25871u)] // Fall Malefic
    [InlineData(100, 25871u)] // Fall Malefic at max level
    public void GetDamageGcdForLevel_ReturnsCorrectAction(byte level, uint expectedActionId)
    {
        var action = ASTActions.GetDamageGcdForLevel(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    // These tests verify that each Has* method safely returns false when StatusList is null,
    // exercising the defensive null-check code path in BaseStatusHelper.

    [Fact]
    public void HasLightspeed_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasLightspeed(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasNeutralSect_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasNeutralSect(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasDivining_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasDivining(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasHoroscope_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasHoroscope(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasHoroscopeHelios_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasHoroscopeHelios(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasMacrocosmos_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasMacrocosmos(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasSynastry_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasSynastry(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasAspectedBenefic_NullTarget_ReturnsFalse()
    {
        // Non-IBattleChara target returns false
        var mock = new Mock<IGameObject>();

        var result = _helper.HasAspectedBenefic(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasAspectedBenefic_BattleCharaWithNullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasAspectedBenefic(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void GetAspectedBeneficDuration_NullTarget_ReturnsZero()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.GetAspectedBeneficDuration(mock.Object);

        Assert.Equal(0f, result);
    }

    [Fact]
    public void HasExaltation_NullTarget_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasExaltation(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasExaltation_BattleCharaWithNullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasExaltation(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasSynastryLink_NullTarget_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasSynastryLink(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasBalanceBuff_NullTarget_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasBalanceBuff(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasSpearBuff_NullTarget_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasSpearBuff(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasAnyCardBuff_NullTarget_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasAnyCardBuff(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasAnyCardBuff_BattleCharaWithNullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasAnyCardBuff(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasTankStance_NullTarget_ReturnsFalse()
    {
        var mock = new Mock<IGameObject>();

        var result = _helper.HasTankStance(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasTankStance_BattleCharaWithNullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = _helper.HasTankStance(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasOurDot_NullTarget_ReturnsFalse()
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

    #endregion
}
