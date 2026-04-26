using Olympus.Data;
using Olympus.Rotation.HermesCore.Helpers;
using Xunit;

namespace Olympus.Tests.Rotation.HermesCore.Helpers;

/// <summary>
/// Unit tests for MudraHelper covering three concerns:
///   1) state-machine transitions (Idle -> FirstMudra -> Second/Third -> ReadyToExecute -> Reset)
///   2) sequence loading from GetMudraSequence per ninjutsu type
///   3) static GetRecommendedNinjutsu decision function
///
/// IsSequenceActive timeout coverage is deferred -- testing it requires
/// injecting a clock into MudraHelper, which crosses the "no new abstractions"
/// boundary set by the spec.
/// </summary>
public class MudraHelperTests
{
    // ----- State machine -----

    [Fact]
    public void Initial_State_Is_Idle_With_Default_Fields()
    {
        var helper = new MudraHelper();

        Assert.Equal(MudraState.Idle, helper.State);
        Assert.Equal(0, helper.MudraCount);
        Assert.False(helper.IsSequenceActive);
        Assert.False(helper.IsReadyToExecute);
        Assert.Equal(NINActions.NinjutsuType.None, helper.TargetNinjutsu);
    }

    [Fact]
    public void StartSequence_Sets_State_To_FirstMudra_And_Loads_Sequence()
    {
        var helper = new MudraHelper();

        helper.StartSequence(NINActions.NinjutsuType.Raiton);

        Assert.Equal(MudraState.FirstMudra, helper.State);
        Assert.Equal(NINActions.NinjutsuType.Raiton, helper.TargetNinjutsu);
        Assert.Equal(NINActions.MudraType.Ten, helper.Mudra1);
        Assert.Equal(NINActions.MudraType.Chi, helper.Mudra2);
        Assert.Equal(NINActions.MudraType.None, helper.Mudra3);
    }

    [Fact]
    public void StartSequence_Resets_Previous_InProgress_Sequence()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Suiton); // Ten-Chi-Jin
        helper.AdvanceSequence(); // now SecondMudra, MudraCount=1

        helper.StartSequence(NINActions.NinjutsuType.FumaShuriken); // single mudra

        Assert.Equal(MudraState.FirstMudra, helper.State);
        Assert.Equal(0, helper.MudraCount);
        Assert.Equal(NINActions.NinjutsuType.FumaShuriken, helper.TargetNinjutsu);
        Assert.Equal(NINActions.MudraType.Ten, helper.Mudra1);
        Assert.Equal(NINActions.MudraType.None, helper.Mudra2);
    }

    [Fact]
    public void AdvanceSequence_FirstMudra_To_SecondMudra_When_Two_Or_More_Mudras_Required()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Raiton); // 2 mudras

        helper.AdvanceSequence();

        Assert.Equal(MudraState.SecondMudra, helper.State);
        Assert.Equal(1, helper.MudraCount);
    }

    [Fact]
    public void AdvanceSequence_FirstMudra_To_ReadyToExecute_For_OneMudra_Ninjutsu()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.FumaShuriken); // 1 mudra

        helper.AdvanceSequence();

        Assert.Equal(MudraState.ReadyToExecute, helper.State);
        Assert.True(helper.IsReadyToExecute);
        Assert.Equal(1, helper.MudraCount);
    }

    [Fact]
    public void AdvanceSequence_SecondMudra_To_ThirdMudra_For_ThreeMudra_Ninjutsu()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Suiton); // Ten-Chi-Jin
        helper.AdvanceSequence(); // SecondMudra

        helper.AdvanceSequence(); // ThirdMudra

        Assert.Equal(MudraState.ThirdMudra, helper.State);
        Assert.Equal(2, helper.MudraCount);
    }

    [Fact]
    public void AdvanceSequence_SecondMudra_To_ReadyToExecute_For_TwoMudra_Ninjutsu()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Raiton); // Ten-Chi
        helper.AdvanceSequence(); // SecondMudra

        helper.AdvanceSequence(); // ReadyToExecute

        Assert.Equal(MudraState.ReadyToExecute, helper.State);
        Assert.True(helper.IsReadyToExecute);
    }

    [Fact]
    public void AdvanceSequence_ThirdMudra_To_ReadyToExecute()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Suiton);
        helper.AdvanceSequence(); // Second
        helper.AdvanceSequence(); // Third

        helper.AdvanceSequence(); // ReadyToExecute

        Assert.Equal(MudraState.ReadyToExecute, helper.State);
        Assert.True(helper.IsReadyToExecute);
    }

    [Fact]
    public void Reset_Returns_All_Fields_To_Defaults()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Suiton);
        helper.AdvanceSequence();

        helper.Reset();

        Assert.Equal(MudraState.Idle, helper.State);
        Assert.Equal(NINActions.NinjutsuType.None, helper.TargetNinjutsu);
        Assert.Equal(NINActions.MudraType.None, helper.Mudra1);
        Assert.Equal(NINActions.MudraType.None, helper.Mudra2);
        Assert.Equal(NINActions.MudraType.None, helper.Mudra3);
        Assert.Equal(0, helper.MudraCount);
    }

    [Fact]
    public void CompleteSequence_Calls_Reset()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Raiton);
        helper.AdvanceSequence();

        helper.CompleteSequence();

        Assert.Equal(MudraState.Idle, helper.State);
        Assert.Equal(0, helper.MudraCount);
    }

    [Fact]
    public void GetNextMudra_Returns_None_When_Idle()
    {
        var helper = new MudraHelper();

        Assert.Equal(NINActions.MudraType.None, helper.GetNextMudra());
    }

    [Fact]
    public void GetNextMudra_Returns_Mudra1_2_3_In_Each_Active_State_And_None_When_Ready()
    {
        var helper = new MudraHelper();
        helper.StartSequence(NINActions.NinjutsuType.Suiton); // Ten-Chi-Jin

        Assert.Equal(NINActions.MudraType.Ten, helper.GetNextMudra());
        helper.AdvanceSequence();
        Assert.Equal(NINActions.MudraType.Chi, helper.GetNextMudra());
        helper.AdvanceSequence();
        Assert.Equal(NINActions.MudraType.Jin, helper.GetNextMudra());
        helper.AdvanceSequence();
        Assert.Equal(NINActions.MudraType.None, helper.GetNextMudra()); // ReadyToExecute
    }

    [Fact]
    public void GetRequiredMudraCount_Reflects_Loaded_Sequence_Length()
    {
        var helper = new MudraHelper();

        helper.StartSequence(NINActions.NinjutsuType.FumaShuriken);
        Assert.Equal(1, helper.GetRequiredMudraCount());

        helper.StartSequence(NINActions.NinjutsuType.Raiton);
        Assert.Equal(2, helper.GetRequiredMudraCount());

        helper.StartSequence(NINActions.NinjutsuType.Suiton);
        Assert.Equal(3, helper.GetRequiredMudraCount());
    }

    // ----- Sequence loading per ninjutsu type -----

    [Theory]
    [InlineData(NINActions.NinjutsuType.FumaShuriken, NINActions.MudraType.Ten, NINActions.MudraType.None, NINActions.MudraType.None)]
    [InlineData(NINActions.NinjutsuType.Raiton,       NINActions.MudraType.Ten, NINActions.MudraType.Chi,  NINActions.MudraType.None)]
    [InlineData(NINActions.NinjutsuType.Katon,        NINActions.MudraType.Chi, NINActions.MudraType.Ten,  NINActions.MudraType.None)]
    [InlineData(NINActions.NinjutsuType.Hyoton,       NINActions.MudraType.Ten, NINActions.MudraType.Jin,  NINActions.MudraType.None)]
    [InlineData(NINActions.NinjutsuType.Huton,        NINActions.MudraType.Jin, NINActions.MudraType.Chi,  NINActions.MudraType.Ten)]
    [InlineData(NINActions.NinjutsuType.Doton,        NINActions.MudraType.Ten, NINActions.MudraType.Jin,  NINActions.MudraType.Chi)]
    [InlineData(NINActions.NinjutsuType.Suiton,       NINActions.MudraType.Ten, NINActions.MudraType.Chi,  NINActions.MudraType.Jin)]
    [InlineData(NINActions.NinjutsuType.GokaMekkyaku, NINActions.MudraType.Chi, NINActions.MudraType.Ten,  NINActions.MudraType.None)] // Kassatsu Katon
    [InlineData(NINActions.NinjutsuType.HyoshoRanryu, NINActions.MudraType.Ten, NINActions.MudraType.Jin,  NINActions.MudraType.None)] // Kassatsu Hyoton
    public void StartSequence_Loads_Correct_Mudras(
        NINActions.NinjutsuType ninjutsu,
        NINActions.MudraType expectedMudra1,
        NINActions.MudraType expectedMudra2,
        NINActions.MudraType expectedMudra3)
    {
        var helper = new MudraHelper();

        helper.StartSequence(ninjutsu);

        Assert.Equal(expectedMudra1, helper.Mudra1);
        Assert.Equal(expectedMudra2, helper.Mudra2);
        Assert.Equal(expectedMudra3, helper.Mudra3);
    }

    // ----- GetRecommendedNinjutsu (static decision function) -----

    [Fact]
    public void GetRecommendedNinjutsu_Kassatsu_AoE_AtGokaLevel_Returns_GokaMekkyaku()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: NINActions.GokaMekkyaku.MinLevel, hasKassatsu: true,
            needsSuiton: false, enemyCount: 3);

        Assert.Equal(NINActions.NinjutsuType.GokaMekkyaku, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_Kassatsu_ST_AtHyoshoLevel_Returns_HyoshoRanryu()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: NINActions.HyoshoRanryu.MinLevel, hasKassatsu: true,
            needsSuiton: false, enemyCount: 1);

        Assert.Equal(NINActions.NinjutsuType.HyoshoRanryu, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_Kassatsu_BelowHyoshoLevel_Falls_Back_To_Raiton()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: (byte)(NINActions.HyoshoRanryu.MinLevel - 1), hasKassatsu: true,
            needsSuiton: false, enemyCount: 1);

        Assert.Equal(NINActions.NinjutsuType.Raiton, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_Kassatsu_BelowRaitonLevel_Falls_Back_To_FumaShuriken()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: (byte)(NINActions.Raiton.MinLevel - 1), hasKassatsu: true,
            needsSuiton: false, enemyCount: 1);

        Assert.Equal(NINActions.NinjutsuType.FumaShuriken, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_NeedsSuiton_AtSuitonLevel_Returns_Suiton()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: NINActions.Suiton.MinLevel, hasKassatsu: false,
            needsSuiton: true, enemyCount: 1);

        Assert.Equal(NINActions.NinjutsuType.Suiton, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_AoE_With_Doton_Enabled_AndEnoughTargets_Returns_Doton()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: NINActions.Doton.MinLevel, hasKassatsu: false,
            needsSuiton: false, enemyCount: 3,
            useDoton: true, dotonMinTargets: 3);

        Assert.Equal(NINActions.NinjutsuType.Doton, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_AoE_With_Doton_Disabled_Returns_Katon()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: NINActions.Katon.MinLevel, hasKassatsu: false,
            needsSuiton: false, enemyCount: 3,
            useDoton: false);

        Assert.Equal(NINActions.NinjutsuType.Katon, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_ST_Default_Returns_Raiton()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: NINActions.Raiton.MinLevel, hasKassatsu: false,
            needsSuiton: false, enemyCount: 1);

        Assert.Equal(NINActions.NinjutsuType.Raiton, result);
    }

    [Fact]
    public void GetRecommendedNinjutsu_BelowRaitonLevel_Returns_FumaShuriken()
    {
        var result = MudraHelper.GetRecommendedNinjutsu(
            level: (byte)(NINActions.Raiton.MinLevel - 1), hasKassatsu: false,
            needsSuiton: false, enemyCount: 1);

        Assert.Equal(NINActions.NinjutsuType.FumaShuriken, result);
    }
}
