using Olympus.Data;
using Olympus.Rotation.HermesCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.HermesCore.Helpers;

/// <summary>
/// Tests for HermesStatusHelper utility methods.
/// HermesStatusHelper is sealed — status behavior is controlled via StatusList on mock characters.
/// Null-guard tests exercise the BaseStatusHelper null-safe path.
/// </summary>
public class StatusHelperTests
{
    private readonly HermesStatusHelper _helper = new();

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_NinjutsuBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            NINActions.StatusIds.Suiton,
            NINActions.StatusIds.Huton,
            NINActions.StatusIds.Doton,
            NINActions.StatusIds.Kassatsu,
            NINActions.StatusIds.TenChiJin,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_CombatBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            NINActions.StatusIds.Bunshin,
            NINActions.StatusIds.PhantomKamaitachiReady,
            NINActions.StatusIds.RaijuReady,
            NINActions.StatusIds.Meisui,
            NINActions.StatusIds.TenriJindoReady,
            NINActions.StatusIds.Kazematoi,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_Debuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            NINActions.StatusIds.VulnerabilityUp,
            NINActions.StatusIds.KunaisBane,
            NINActions.StatusIds.Dokumori,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, NINActions.StatusIds.Suiton);
        Assert.NotEqual(0u, NINActions.StatusIds.Kassatsu);
        Assert.NotEqual(0u, NINActions.StatusIds.TenChiJin);
        Assert.NotEqual(0u, NINActions.StatusIds.Bunshin);
        Assert.NotEqual(0u, NINActions.StatusIds.PhantomKamaitachiReady);
        Assert.NotEqual(0u, NINActions.StatusIds.RaijuReady);
        Assert.NotEqual(0u, NINActions.StatusIds.Meisui);
        Assert.NotEqual(0u, NINActions.StatusIds.TenriJindoReady);
        Assert.NotEqual(0u, NINActions.StatusIds.KunaisBane);
        Assert.NotEqual(0u, NINActions.StatusIds.Dokumori);
        Assert.NotEqual(0u, NINActions.StatusIds.Kazematoi);
        Assert.NotEqual(0u, NINActions.StatusIds.Mudra);
        Assert.NotEqual(0u, NINActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_Suiton_MatchesGameData()
    {
        Assert.Equal(507u, NINActions.StatusIds.Suiton);
    }

    [Fact]
    public void StatusId_Kassatsu_MatchesGameData()
    {
        Assert.Equal(497u, NINActions.StatusIds.Kassatsu);
    }

    [Fact]
    public void StatusId_TenChiJin_MatchesGameData()
    {
        Assert.Equal(1186u, NINActions.StatusIds.TenChiJin);
    }

    [Fact]
    public void StatusId_Bunshin_MatchesGameData()
    {
        Assert.Equal(1954u, NINActions.StatusIds.Bunshin);
    }

    [Fact]
    public void StatusId_RaijuReady_MatchesGameData()
    {
        Assert.Equal(2690u, NINActions.StatusIds.RaijuReady);
    }

    [Fact]
    public void StatusId_KunaisBane_MatchesGameData()
    {
        Assert.Equal(3906u, NINActions.StatusIds.KunaisBane);
    }

    [Fact]
    public void StatusId_TrueNorth_MatchesGameData()
    {
        Assert.Equal(1250u, NINActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    [Fact]
    public void HasSuiton_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasSuiton(mock.Object));
    }

    [Fact]
    public void HasKassatsu_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasKassatsu(mock.Object));
    }

    [Fact]
    public void HasTenChiJin_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTenChiJin(mock.Object));
    }

    [Fact]
    public void IsMudraActive_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.IsMudraActive(mock.Object));
    }

    [Fact]
    public void HasBunshin_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasBunshin(mock.Object));
    }

    [Fact]
    public void HasPhantomKamaitachiReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasPhantomKamaitachiReady(mock.Object));
    }

    [Fact]
    public void HasRaijuReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRaijuReady(mock.Object));
    }

    [Fact]
    public void HasMeisui_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasMeisui(mock.Object));
    }

    [Fact]
    public void HasTenriJindoReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTenriJindoReady(mock.Object));
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
    public void HasShadeShift_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasShadeShift(mock.Object));
    }

    [Fact]
    public void HasKazematoi_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasKazematoi(mock.Object));
    }

    [Fact]
    public void HasKunaisBane_TargetNullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasKunaisBane(mock.Object, playerId: 1u));
    }

    [Fact]
    public void HasDokumori_TargetNullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasDokumori(mock.Object, playerId: 1u));
    }

    [Fact]
    public void HasVulnerabilityUp_TargetNullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasVulnerabilityUp(mock.Object, playerId: 1u));
    }

    #endregion

    #region Get* Methods — Null StatusList Guard Tests

    [Fact]
    public void GetSuitonRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetSuitonRemaining(mock.Object));
    }

    [Fact]
    public void GetTenChiJinStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetTenChiJinStacks(mock.Object));
    }

    [Fact]
    public void GetBunshinStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetBunshinStacks(mock.Object));
    }

    [Fact]
    public void GetRaijuStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetRaijuStacks(mock.Object));
    }

    [Fact]
    public void GetKazematoiStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetKazematoiStacks(mock.Object));
    }

    [Fact]
    public void GetKunaisBaneRemaining_TargetNullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetKunaisBaneRemaining(mock.Object, playerId: 1u));
    }

    [Fact]
    public void GetDokumoriRemaining_TargetNullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetDokumoriRemaining(mock.Object, playerId: 1u));
    }

    #endregion

    #region MudraHelper — State Machine Tests

    [Fact]
    public void MudraHelper_InitialState_IsIdle()
    {
        var helper = new MudraHelper();
        Assert.Equal(MudraState.Idle, helper.State);
        Assert.Equal(NINActions.NinjutsuType.None, helper.TargetNinjutsu);
        Assert.False(helper.IsSequenceActive);
        Assert.False(helper.IsReadyToExecute);
    }

    [Fact]
    public void MudraHelper_StartSequence_Raiton_SetsTwoMudras()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Raiton);

        Assert.True(helper.IsSequenceActive);
        Assert.Equal(MudraState.FirstMudra, helper.State);
        Assert.Equal(NINActions.MudraType.Ten, helper.Mudra1);
        Assert.Equal(NINActions.MudraType.Chi, helper.Mudra2);
        Assert.Equal(NINActions.MudraType.None, helper.Mudra3);
    }

    [Fact]
    public void MudraHelper_StartSequence_Suiton_SetsThreeMudras()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Suiton);

        Assert.True(helper.IsSequenceActive);
        Assert.Equal(MudraState.FirstMudra, helper.State);
        Assert.Equal(NINActions.MudraType.Ten, helper.Mudra1);
        Assert.Equal(NINActions.MudraType.Chi, helper.Mudra2);
        Assert.Equal(NINActions.MudraType.Jin, helper.Mudra3);
    }

    [Fact]
    public void MudraHelper_StartSequence_FumaShuriken_SetsOneMudra()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.FumaShuriken);

        Assert.True(helper.IsSequenceActive);
        Assert.Equal(MudraState.FirstMudra, helper.State);
        Assert.Equal(NINActions.MudraType.Ten, helper.Mudra1);
        Assert.Equal(NINActions.MudraType.None, helper.Mudra2);
        Assert.Equal(NINActions.MudraType.None, helper.Mudra3);
    }

    [Fact]
    public void MudraHelper_AdvanceSequence_OneMudra_BecomesReadyToExecute()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.FumaShuriken);
        helper.AdvanceSequence(); // Input Ten

        Assert.Equal(MudraState.ReadyToExecute, helper.State);
        Assert.True(helper.IsReadyToExecute);
        Assert.Equal(1, helper.MudraCount);
    }

    [Fact]
    public void MudraHelper_AdvanceSequence_TwoMudra_ProgressesToSecondAndThenReady()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Raiton); // Ten-Chi

        helper.AdvanceSequence(); // Input Ten
        Assert.Equal(MudraState.SecondMudra, helper.State);
        Assert.Equal(1, helper.MudraCount);

        helper.AdvanceSequence(); // Input Chi
        Assert.Equal(MudraState.ReadyToExecute, helper.State);
        Assert.True(helper.IsReadyToExecute);
        Assert.Equal(2, helper.MudraCount);
    }

    [Fact]
    public void MudraHelper_AdvanceSequence_ThreeMudra_ProgressesThrough3Steps()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Suiton); // Ten-Chi-Jin

        helper.AdvanceSequence(); // Ten
        Assert.Equal(MudraState.SecondMudra, helper.State);

        helper.AdvanceSequence(); // Chi
        Assert.Equal(MudraState.ThirdMudra, helper.State);

        helper.AdvanceSequence(); // Jin
        Assert.Equal(MudraState.ReadyToExecute, helper.State);
        Assert.Equal(3, helper.MudraCount);
    }

    [Fact]
    public void MudraHelper_CompleteSequence_ResetsToIdle()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Raiton);
        helper.AdvanceSequence();
        helper.AdvanceSequence();
        helper.CompleteSequence();

        Assert.Equal(MudraState.Idle, helper.State);
        Assert.Equal(NINActions.NinjutsuType.None, helper.TargetNinjutsu);
        Assert.False(helper.IsSequenceActive);
    }

    [Fact]
    public void MudraHelper_Reset_FromMidSequence_ResetsToIdle()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Suiton);
        helper.AdvanceSequence();
        helper.Reset();

        Assert.Equal(MudraState.Idle, helper.State);
        Assert.Equal(0, helper.MudraCount);
        Assert.Equal(NINActions.MudraType.None, helper.Mudra1);
    }

    [Fact]
    public void MudraHelper_GetNextMudra_ReturnsCorrectMudraAtEachStep()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Suiton); // Ten-Chi-Jin

        Assert.Equal(NINActions.MudraType.Ten, helper.GetNextMudra());
        helper.AdvanceSequence();

        Assert.Equal(NINActions.MudraType.Chi, helper.GetNextMudra());
        helper.AdvanceSequence();

        Assert.Equal(NINActions.MudraType.Jin, helper.GetNextMudra());
    }

    [Fact]
    public void MudraHelper_GetRequiredMudraCount_FumaShuriken_Returns1()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.FumaShuriken);
        Assert.Equal(1, helper.GetRequiredMudraCount());
    }

    [Fact]
    public void MudraHelper_GetRequiredMudraCount_Raiton_Returns2()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Raiton);
        Assert.Equal(2, helper.GetRequiredMudraCount());
    }

    [Fact]
    public void MudraHelper_GetRequiredMudraCount_Suiton_Returns3()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Suiton);
        Assert.Equal(3, helper.GetRequiredMudraCount());
    }

    #endregion

    #region MudraHelper.GetRecommendedNinjutsu — Logic Tests

    [Fact]
    public void GetRecommendedNinjutsu_NeedsSuiton_ReturnsSuiton()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: 100,
            hasKassatsu: false,
            needsSuiton: true,
            enemyCount: 1);

        Assert.Equal(NINActions.NinjutsuType.Suiton, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_SingleTarget_NoKassatsu_ReturnsRaiton()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: 100,
            hasKassatsu: false,
            needsSuiton: false,
            enemyCount: 1);

        Assert.Equal(NINActions.NinjutsuType.Raiton, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_AoE_NoKassatsu_ReturnsDoton()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: 100,
            hasKassatsu: false,
            needsSuiton: false,
            enemyCount: 3);

        Assert.Equal(NINActions.NinjutsuType.Doton, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_SingleTarget_Kassatsu_HighLevel_ReturnsHyoshoRanryu()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: 100,
            hasKassatsu: true,
            needsSuiton: false,
            enemyCount: 1);

        Assert.Equal(NINActions.NinjutsuType.HyoshoRanryu, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_AoE_Kassatsu_HighLevel_ReturnsGokaMekkyaku()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: 100,
            hasKassatsu: true,
            needsSuiton: false,
            enemyCount: 3);

        Assert.Equal(NINActions.NinjutsuType.GokaMekkyaku, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_LowLevel_NoMudraOptions_ReturnsFumaShuriken()
    {
        // Level 30 only has Ten, no Raiton (level 35)
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: 30,
            hasKassatsu: false,
            needsSuiton: false,
            enemyCount: 1);

        Assert.Equal(NINActions.NinjutsuType.FumaShuriken, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_NeedsSuiton_BelowLevel45_NotSuiton()
    {
        // Suiton requires level 45
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: 35,
            hasKassatsu: false,
            needsSuiton: true, // Wants Suiton but can't yet
            enemyCount: 1);

        // Falls through to Raiton (level 35 has Raiton)
        Assert.Equal(NINActions.NinjutsuType.Raiton, result);
    }

    #endregion

    #region NINActions.GetMudraSequence — Correctness Tests

    [Theory]
    [InlineData(NINActions.NinjutsuType.FumaShuriken, NINActions.MudraType.Ten, NINActions.MudraType.None, NINActions.MudraType.None)]
    [InlineData(NINActions.NinjutsuType.Raiton, NINActions.MudraType.Ten, NINActions.MudraType.Chi, NINActions.MudraType.None)]
    [InlineData(NINActions.NinjutsuType.Katon, NINActions.MudraType.Chi, NINActions.MudraType.Ten, NINActions.MudraType.None)]
    [InlineData(NINActions.NinjutsuType.Suiton, NINActions.MudraType.Ten, NINActions.MudraType.Chi, NINActions.MudraType.Jin)]
    [InlineData(NINActions.NinjutsuType.Huton, NINActions.MudraType.Jin, NINActions.MudraType.Chi, NINActions.MudraType.Ten)]
    [InlineData(NINActions.NinjutsuType.Doton, NINActions.MudraType.Ten, NINActions.MudraType.Jin, NINActions.MudraType.Chi)]
    public void GetMudraSequence_ReturnsCorrectSequence(
        NINActions.NinjutsuType ninjutsu,
        NINActions.MudraType expectedM1,
        NINActions.MudraType expectedM2,
        NINActions.MudraType expectedM3)
    {
        var (m1, m2, m3) = NINActions.GetMudraSequence(ninjutsu);
        Assert.Equal(expectedM1, m1);
        Assert.Equal(expectedM2, m2);
        Assert.Equal(expectedM3, m3);
    }

    #endregion
}
