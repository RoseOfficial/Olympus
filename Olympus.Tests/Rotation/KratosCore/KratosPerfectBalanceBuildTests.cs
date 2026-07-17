using Olympus.Rotation;
using Olympus.Rotation.KratosCore.Context;
using Xunit;

namespace Olympus.Tests.Rotation.KratosCore;

/// <summary>
/// Pure-function tests for Kratos.ComputePerfectBalanceBuild.
/// Verifies correct nadi-aware chakra selection during Perfect Balance.
/// Game rules:
///   3 SAME Beast Chakra  = Elixir Burst  = grants LUNAR nadi
///   3 DIFFERENT chakra   = Rising Phoenix = grants SOLAR nadi
///   Both nadi + any 3   = Phantom Rush
/// Branch rules:
///   Both nadi  -> Opo x3 (highest potency Phantom Rush fill)
///   Lunar only -> Opo, Raptor, Coeurl (3 different for Rising Phoenix / Solar)
///   Solar only -> Opo x3 (3 same for Elixir Burst / Lunar)
///   No nadi    -> Opo x3 (build Lunar first per community standard)
/// </summary>
public class KratosPerfectBalanceBuildTests
{
    // ---------------------------------------------------------------
    // Per-decision [Theory] suite
    // ---------------------------------------------------------------

    [Theory]
    // No nadi: always build Lunar first (3 same = Opo x3)
    [InlineData(false, false, false, false, false, MonkForm.OpoOpo)] // empty: push first Opo
    [InlineData(false, false, true, false, false, MonkForm.OpoOpo)]  // 1 Opo: push second Opo
    [InlineData(false, false, true, true, false, MonkForm.OpoOpo)]   // mixed: still push Opo
    [InlineData(false, false, true, false, true, MonkForm.OpoOpo)]   // mixed: still push Opo
    // Solar only: need Lunar, build 3 same (Opo x3)
    [InlineData(false, true, false, false, false, MonkForm.OpoOpo)]  // no chakra: push Opo
    [InlineData(false, true, true, false, false, MonkForm.OpoOpo)]   // 1 Opo: push second Opo
    // Lunar only: need Solar, build 3 different (Opo -> Raptor -> Coeurl)
    [InlineData(true, false, false, false, false, MonkForm.OpoOpo)]  // no chakra: first Opo
    [InlineData(true, false, true, false, false, MonkForm.Raptor)]   // have Opo: push Raptor
    [InlineData(true, false, true, true, false, MonkForm.Coeurl)]    // have Opo+Raptor: push Coeurl
    [InlineData(true, false, true, true, true, MonkForm.Coeurl)]     // all 3 filled (edge): Coeurl fallback
    // Both nadi: any 3 fires Phantom Rush, Opo highest potency
    [InlineData(true, true, false, false, false, MonkForm.OpoOpo)]   // empty: push Opo
    [InlineData(true, true, true, false, false, MonkForm.OpoOpo)]    // have Opo: push Opo
    [InlineData(true, true, true, true, true, MonkForm.OpoOpo)]      // all filled (edge): Opo
    public void ComputePerfectBalanceBuild_ReturnsExpectedForm(
        bool hasLunar, bool hasSolar,
        bool hasOpo, bool hasRaptor, bool hasCoeurl,
        MonkForm expected)
    {
        var result = Kratos.ComputePerfectBalanceBuild(hasLunar, hasSolar, hasOpo, hasRaptor, hasCoeurl);
        Assert.Equal(expected, result);
    }

    // ---------------------------------------------------------------
    // Full 3-GCD sequence tests
    // ---------------------------------------------------------------

    [Fact]
    public void FullSequence_NoNadi_ProducesOpoOpoOpo()
    {
        // GCD 1: empty
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(false, false, hasOpo: false, hasRaptor: false, hasCoeurl: false));
        // GCD 2: 1 Opo slot filled
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(false, false, hasOpo: true, hasRaptor: false, hasCoeurl: false));
        // GCD 3: 2 Opo slots filled (hasOpo still true, hasRaptor/hasCoeurl false)
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(false, false, hasOpo: true, hasRaptor: false, hasCoeurl: false));
    }

    [Fact]
    public void FullSequence_SolarOnly_ProducesOpoOpoOpo()
    {
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(false, true, hasOpo: false, hasRaptor: false, hasCoeurl: false));
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(false, true, hasOpo: true, hasRaptor: false, hasCoeurl: false));
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(false, true, hasOpo: true, hasRaptor: false, hasCoeurl: false));
    }

    [Fact]
    public void FullSequence_LunarOnly_ProducesOpoRaptorCoeurl()
    {
        // GCD 1: empty -> Opo
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(true, false, hasOpo: false, hasRaptor: false, hasCoeurl: false));
        // GCD 2: Opo filled -> Raptor
        Assert.Equal(MonkForm.Raptor,
            Kratos.ComputePerfectBalanceBuild(true, false, hasOpo: true, hasRaptor: false, hasCoeurl: false));
        // GCD 3: Opo+Raptor filled -> Coeurl
        Assert.Equal(MonkForm.Coeurl,
            Kratos.ComputePerfectBalanceBuild(true, false, hasOpo: true, hasRaptor: true, hasCoeurl: false));
    }

    [Fact]
    public void FullSequence_BothNadi_ProducesOpoFill()
    {
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(true, true, hasOpo: false, hasRaptor: false, hasCoeurl: false));
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(true, true, hasOpo: true, hasRaptor: false, hasCoeurl: false));
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(true, true, hasOpo: true, hasRaptor: false, hasCoeurl: false));
    }

    // ---------------------------------------------------------------
    // isOpener = true: Lunar-only stays all-same (Opo x3) for double-lunar
    // ---------------------------------------------------------------

    [Theory]
    // isOpener + Lunar only: all Opo (keep building Lunar, NOT switching to Solar)
    [InlineData(true, false, false, false, false, MonkForm.OpoOpo)] // empty: Opo
    [InlineData(true, false, true,  false, false, MonkForm.OpoOpo)] // have Opo: still Opo (not Raptor!)
    [InlineData(true, false, true,  true,  false, MonkForm.OpoOpo)] // have Opo+Raptor: still Opo
    [InlineData(true, false, true,  true,  true,  MonkForm.OpoOpo)] // all filled: still Opo
    // isOpener + Both nadi: Phantom Rush unchanged
    [InlineData(true, true, false, false, false, MonkForm.OpoOpo)]
    // isOpener + Solar only or no nadi: identical to non-opener (Opo x3)
    [InlineData(false, true,  false, false, false, MonkForm.OpoOpo)]
    [InlineData(false, false, false, false, false, MonkForm.OpoOpo)]
    public void ComputePerfectBalanceBuild_IsOpener_ReturnsExpectedForm(
        bool hasLunar, bool hasSolar,
        bool hasOpo, bool hasRaptor, bool hasCoeurl,
        MonkForm expected)
    {
        var result = Kratos.ComputePerfectBalanceBuild(
            hasLunar, hasSolar, hasOpo, hasRaptor, hasCoeurl, isOpener: true);
        Assert.Equal(expected, result);
    }

    /// <summary>Discrimination: non-opener Lunar-only DOES switch to Solar (Raptor/Coeurl).</summary>
    [Theory]
    [InlineData(true, false, true, false, false, MonkForm.Raptor)] // have Opo, isOpener false -> Raptor
    [InlineData(true, false, true, true,  false, MonkForm.Coeurl)] // have Opo+Raptor, isOpener false -> Coeurl
    public void ComputePerfectBalanceBuild_IsOpenerFalse_LunarOnly_SwitchesToSolar(
        bool hasLunar, bool hasSolar,
        bool hasOpo, bool hasRaptor, bool hasCoeurl,
        MonkForm expected)
    {
        var result = Kratos.ComputePerfectBalanceBuild(
            hasLunar, hasSolar, hasOpo, hasRaptor, hasCoeurl, isOpener: false);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FullSequence_IsOpener_LunarOnly_ProducesOpoOpoOpo()
    {
        // During the opener with Lunar nadi, the second PB keeps building Lunar (Opo x3)
        // rather than switching to Solar (Opo -> Raptor -> Coeurl).
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(true, false, hasOpo: false, hasRaptor: false, hasCoeurl: false, isOpener: true));
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(true, false, hasOpo: true, hasRaptor: false, hasCoeurl: false, isOpener: true));
        Assert.Equal(MonkForm.OpoOpo,
            Kratos.ComputePerfectBalanceBuild(true, false, hasOpo: true, hasRaptor: true, hasCoeurl: false, isOpener: true));
    }
}
