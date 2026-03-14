using Olympus.Data;
using Olympus.Rotation.EchidnaCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.EchidnaCore.Helpers;

/// <summary>
/// Tests for EchidnaStatusHelper utility methods.
/// EchidnaStatusHelper is sealed — status behavior is controlled via StatusList on mock characters.
/// Null-guard tests exercise the BaseStatusHelper null-safe path.
/// </summary>
public class StatusHelperTests
{
    private readonly EchidnaStatusHelper _helper = new();

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_CoreBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            VPRActions.StatusIds.HuntersInstinct,
            VPRActions.StatusIds.Swiftscaled,
            VPRActions.StatusIds.Reawakened,
            VPRActions.StatusIds.HonedSteel,
            VPRActions.StatusIds.HonedReavers,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_VenomBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            VPRActions.StatusIds.FlankstungVenom,
            VPRActions.StatusIds.HindstungVenom,
            VPRActions.StatusIds.FlanksbaneVenom,
            VPRActions.StatusIds.HindsbaneVenom,
            VPRActions.StatusIds.GrimskinsVenom,
            VPRActions.StatusIds.GrimhuntersVenom,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_ProcStatuses_AreDistinct()
    {
        var ids = new uint[]
        {
            VPRActions.StatusIds.PoisedForTwinfang,
            VPRActions.StatusIds.PoisedForTwinblood,
            VPRActions.StatusIds.ReadyToReawaken,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, VPRActions.StatusIds.NoxiousGnash);
        Assert.NotEqual(0u, VPRActions.StatusIds.HuntersInstinct);
        Assert.NotEqual(0u, VPRActions.StatusIds.Swiftscaled);
        Assert.NotEqual(0u, VPRActions.StatusIds.Reawakened);
        Assert.NotEqual(0u, VPRActions.StatusIds.HonedSteel);
        Assert.NotEqual(0u, VPRActions.StatusIds.HonedReavers);
        Assert.NotEqual(0u, VPRActions.StatusIds.FlankstungVenom);
        Assert.NotEqual(0u, VPRActions.StatusIds.HindstungVenom);
        Assert.NotEqual(0u, VPRActions.StatusIds.FlanksbaneVenom);
        Assert.NotEqual(0u, VPRActions.StatusIds.HindsbaneVenom);
        Assert.NotEqual(0u, VPRActions.StatusIds.GrimskinsVenom);
        Assert.NotEqual(0u, VPRActions.StatusIds.GrimhuntersVenom);
        Assert.NotEqual(0u, VPRActions.StatusIds.PoisedForTwinfang);
        Assert.NotEqual(0u, VPRActions.StatusIds.PoisedForTwinblood);
        Assert.NotEqual(0u, VPRActions.StatusIds.ReadyToReawaken);
        Assert.NotEqual(0u, VPRActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_NoxiousGnash_MatchesGameData()
    {
        Assert.Equal(3667u, VPRActions.StatusIds.NoxiousGnash);
    }

    [Fact]
    public void StatusId_HuntersInstinct_MatchesGameData()
    {
        Assert.Equal(3668u, VPRActions.StatusIds.HuntersInstinct);
    }

    [Fact]
    public void StatusId_Swiftscaled_MatchesGameData()
    {
        Assert.Equal(3669u, VPRActions.StatusIds.Swiftscaled);
    }

    [Fact]
    public void StatusId_Reawakened_MatchesGameData()
    {
        Assert.Equal(3670u, VPRActions.StatusIds.Reawakened);
    }

    [Fact]
    public void StatusId_FlankstungVenom_MatchesGameData()
    {
        Assert.Equal(3645u, VPRActions.StatusIds.FlankstungVenom);
    }

    [Fact]
    public void StatusId_HindstungVenom_MatchesGameData()
    {
        Assert.Equal(3646u, VPRActions.StatusIds.HindstungVenom);
    }

    [Fact]
    public void StatusId_PoisedForTwinfang_MatchesGameData()
    {
        Assert.Equal(3665u, VPRActions.StatusIds.PoisedForTwinfang);
    }

    [Fact]
    public void StatusId_PoisedForTwinblood_MatchesGameData()
    {
        Assert.Equal(3666u, VPRActions.StatusIds.PoisedForTwinblood);
    }

    [Fact]
    public void StatusId_ReadyToReawaken_MatchesGameData()
    {
        Assert.Equal(4094u, VPRActions.StatusIds.ReadyToReawaken);
    }

    [Fact]
    public void StatusId_TrueNorth_MatchesGameData()
    {
        Assert.Equal(1250u, VPRActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    [Fact]
    public void HasHuntersInstinct_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasHuntersInstinct(mock.Object));
    }

    [Fact]
    public void HasSwiftscaled_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasSwiftscaled(mock.Object));
    }

    [Fact]
    public void HasReawakened_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasReawakened(mock.Object));
    }

    [Fact]
    public void HasHonedSteel_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasHonedSteel(mock.Object));
    }

    [Fact]
    public void HasHonedReavers_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasHonedReavers(mock.Object));
    }

    [Fact]
    public void HasReadyToReawaken_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasReadyToReawaken(mock.Object));
    }

    [Fact]
    public void HasFlankstungVenom_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFlankstungVenom(mock.Object));
    }

    [Fact]
    public void HasHindstungVenom_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasHindstungVenom(mock.Object));
    }

    [Fact]
    public void HasFlanksbaneVenom_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFlanksbaneVenom(mock.Object));
    }

    [Fact]
    public void HasHindsbaneVenom_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasHindsbaneVenom(mock.Object));
    }

    [Fact]
    public void HasGrimskinsVenom_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasGrimskinsVenom(mock.Object));
    }

    [Fact]
    public void HasGrimhuntersVenom_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasGrimhuntersVenom(mock.Object));
    }

    [Fact]
    public void HasPoisedForTwinfang_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasPoisedForTwinfang(mock.Object));
    }

    [Fact]
    public void HasPoisedForTwinblood_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasPoisedForTwinblood(mock.Object));
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
    public void HasAnyVenom_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasAnyVenom(mock.Object));
    }

    #endregion

    #region VPRActions Lookup Helpers

    [Fact]
    public void GetPositionalFinisher_HindstungVenom_FromHunters_ReturnsHindstingStrike()
    {
        var action = VPRActions.GetPositionalFinisher(
            hasFlankstungVenom: false,
            hasHindstungVenom: true,
            hasFlanksbaneVenom: false,
            hasHindsbaneVenom: false,
            isFromHunters: true);
        Assert.Equal(VPRActions.HindstingStrike.ActionId, action.ActionId);
    }

    [Fact]
    public void GetPositionalFinisher_FlankstungVenom_FromHunters_ReturnsFlankstingStrike()
    {
        var action = VPRActions.GetPositionalFinisher(
            hasFlankstungVenom: true,
            hasHindstungVenom: false,
            hasFlanksbaneVenom: false,
            hasHindsbaneVenom: false,
            isFromHunters: true);
        Assert.Equal(VPRActions.FlankstingStrike.ActionId, action.ActionId);
    }

    [Fact]
    public void GetPositionalFinisher_NoVenom_FromHunters_ReturnsFlankstingStrike()
    {
        var action = VPRActions.GetPositionalFinisher(
            hasFlankstungVenom: false,
            hasHindstungVenom: false,
            hasFlanksbaneVenom: false,
            hasHindsbaneVenom: false,
            isFromHunters: true);
        Assert.Equal(VPRActions.FlankstingStrike.ActionId, action.ActionId);
    }

    [Fact]
    public void GetPositionalFinisher_HindsbaneVenom_FromSwiftskins_ReturnsHindsbaneFang()
    {
        var action = VPRActions.GetPositionalFinisher(
            hasFlankstungVenom: false,
            hasHindstungVenom: false,
            hasFlanksbaneVenom: false,
            hasHindsbaneVenom: true,
            isFromHunters: false);
        Assert.Equal(VPRActions.HindsbaneFang.ActionId, action.ActionId);
    }

    [Fact]
    public void GetPositionalFinisher_NoVenom_FromSwiftskins_ReturnsFlanksbaneFang()
    {
        var action = VPRActions.GetPositionalFinisher(
            hasFlankstungVenom: false,
            hasHindstungVenom: false,
            hasFlanksbaneVenom: false,
            hasHindsbaneVenom: false,
            isFromHunters: false);
        Assert.Equal(VPRActions.FlanksbaneFang.ActionId, action.ActionId);
    }

    [Fact]
    public void GetDreadComboOgcd_HunterCoilReady_ReturnsTwinfang()
    {
        var action = VPRActions.GetDreadComboOgcd(VPRActions.DreadCombo.HunterCoilReady, isAoe: false);
        Assert.NotNull(action);
        Assert.Equal(VPRActions.Twinfang.ActionId, action!.ActionId);
    }

    [Fact]
    public void GetDreadComboOgcd_SwiftskinCoilReady_ReturnsTwinblood()
    {
        var action = VPRActions.GetDreadComboOgcd(VPRActions.DreadCombo.SwiftskinCoilReady, isAoe: false);
        Assert.NotNull(action);
        Assert.Equal(VPRActions.Twinblood.ActionId, action!.ActionId);
    }

    [Fact]
    public void GetDreadComboOgcd_HunterDenReady_AoE_ReturnsTwinfangBite()
    {
        var action = VPRActions.GetDreadComboOgcd(VPRActions.DreadCombo.HunterDenReady, isAoe: true);
        Assert.NotNull(action);
        Assert.Equal(VPRActions.TwinfangBite.ActionId, action!.ActionId);
    }

    [Fact]
    public void GetDreadComboOgcd_None_ReturnsNull()
    {
        var action = VPRActions.GetDreadComboOgcd(VPRActions.DreadCombo.None, isAoe: false);
        Assert.Null(action);
    }

    [Fact]
    public void GetLegacyOgcd_FirstLegacy_ReturnsFirstLegacy()
    {
        var action = VPRActions.GetLegacyOgcd(VPRActions.SerpentCombo.FirstLegacy);
        Assert.NotNull(action);
        Assert.Equal(VPRActions.FirstLegacy.ActionId, action!.ActionId);
    }

    [Fact]
    public void GetLegacyOgcd_FourthLegacy_ReturnsFourthLegacy()
    {
        var action = VPRActions.GetLegacyOgcd(VPRActions.SerpentCombo.FourthLegacy);
        Assert.NotNull(action);
        Assert.Equal(VPRActions.FourthLegacy.ActionId, action!.ActionId);
    }

    [Fact]
    public void GetLegacyOgcd_DeathRattle_ReturnsDeathRattle()
    {
        var action = VPRActions.GetLegacyOgcd(VPRActions.SerpentCombo.DeathRattle);
        Assert.NotNull(action);
        Assert.Equal(VPRActions.DeathRattle.ActionId, action!.ActionId);
    }

    [Fact]
    public void GetLegacyOgcd_None_ReturnsNull()
    {
        var action = VPRActions.GetLegacyOgcd(VPRActions.SerpentCombo.None);
        Assert.Null(action);
    }

    [Fact]
    public void GetGenerationGcd_FiveTribute_ReturnsFirstGeneration()
    {
        var action = VPRActions.GetGenerationGcd(5);
        Assert.Equal(VPRActions.FirstGeneration.ActionId, action.ActionId);
    }

    [Fact]
    public void GetGenerationGcd_FourTribute_ReturnsSecondGeneration()
    {
        var action = VPRActions.GetGenerationGcd(4);
        Assert.Equal(VPRActions.SecondGeneration.ActionId, action.ActionId);
    }

    [Fact]
    public void GetGenerationGcd_ThreeTribute_ReturnsThirdGeneration()
    {
        var action = VPRActions.GetGenerationGcd(3);
        Assert.Equal(VPRActions.ThirdGeneration.ActionId, action.ActionId);
    }

    [Fact]
    public void GetGenerationGcd_TwoTribute_ReturnsFourthGeneration()
    {
        var action = VPRActions.GetGenerationGcd(2);
        Assert.Equal(VPRActions.FourthGeneration.ActionId, action.ActionId);
    }

    [Fact]
    public void GetGenerationGcd_OneTribute_ReturnsOuroboros()
    {
        var action = VPRActions.GetGenerationGcd(1);
        Assert.Equal(VPRActions.Ouroboros.ActionId, action.ActionId);
    }

    #endregion
}
