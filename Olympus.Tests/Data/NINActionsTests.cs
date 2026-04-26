using Olympus.Data;
using Xunit;

namespace Olympus.Tests.Data;

/// <summary>
/// Unit tests for NINActions.GetNinjutsuResult covering all legal mudra orderings
/// (including the "also works" alternates the game accepts) and the RabbitMedium
/// failure case for invalid sequences. Frozen by these tests so a future patch
/// cannot silently drop a valid ordering.
/// GetMudraSequence is exercised indirectly by MudraHelperTests; no separate
/// coverage needed here.
/// </summary>
public class NINActionsTests
{
    // ----- Single mudra -----

    [Theory]
    [InlineData(NINActions.MudraType.Ten)]
    [InlineData(NINActions.MudraType.Chi)]
    [InlineData(NINActions.MudraType.Jin)]
    public void GetNinjutsuResult_SingleMudra_Returns_FumaShuriken(NINActions.MudraType single)
    {
        var result = NINActions.GetNinjutsuResult(single);

        Assert.Equal(NINActions.NinjutsuType.FumaShuriken, result);
    }

    // ----- Two mudra: 2 valid orderings per ninjutsu -----

    [Theory]
    [InlineData(NINActions.MudraType.Ten, NINActions.MudraType.Chi)] // Primary
    [InlineData(NINActions.MudraType.Chi, NINActions.MudraType.Jin)] // Also works
    public void GetNinjutsuResult_TwoMudra_Raiton_Orderings(NINActions.MudraType first, NINActions.MudraType second)
    {
        var result = NINActions.GetNinjutsuResult(first, second);

        Assert.Equal(NINActions.NinjutsuType.Raiton, result);
    }

    [Theory]
    [InlineData(NINActions.MudraType.Chi, NINActions.MudraType.Ten)] // Primary
    [InlineData(NINActions.MudraType.Jin, NINActions.MudraType.Chi)] // Also works
    public void GetNinjutsuResult_TwoMudra_Katon_Orderings(NINActions.MudraType first, NINActions.MudraType second)
    {
        var result = NINActions.GetNinjutsuResult(first, second);

        Assert.Equal(NINActions.NinjutsuType.Katon, result);
    }

    [Theory]
    [InlineData(NINActions.MudraType.Ten, NINActions.MudraType.Jin)] // Primary
    [InlineData(NINActions.MudraType.Jin, NINActions.MudraType.Ten)] // Also works
    public void GetNinjutsuResult_TwoMudra_Hyoton_Orderings(NINActions.MudraType first, NINActions.MudraType second)
    {
        var result = NINActions.GetNinjutsuResult(first, second);

        Assert.Equal(NINActions.NinjutsuType.Hyoton, result);
    }

    [Fact]
    public void GetNinjutsuResult_TwoMudra_Invalid_RepeatedSameMudra_Returns_RabbitMedium()
    {
        var result = NINActions.GetNinjutsuResult(NINActions.MudraType.Ten, NINActions.MudraType.Ten);

        Assert.Equal(NINActions.NinjutsuType.RabbitMedium, result);
    }

    // ----- Three mudra: 2 valid orderings per ninjutsu -----

    [Theory]
    [InlineData(NINActions.MudraType.Jin, NINActions.MudraType.Chi, NINActions.MudraType.Ten)] // Primary
    [InlineData(NINActions.MudraType.Chi, NINActions.MudraType.Jin, NINActions.MudraType.Ten)] // Also works
    public void GetNinjutsuResult_ThreeMudra_Huton_Orderings(NINActions.MudraType a, NINActions.MudraType b, NINActions.MudraType c)
    {
        var result = NINActions.GetNinjutsuResult(a, b, c);

        Assert.Equal(NINActions.NinjutsuType.Huton, result);
    }

    [Theory]
    [InlineData(NINActions.MudraType.Ten, NINActions.MudraType.Jin, NINActions.MudraType.Chi)] // Primary
    [InlineData(NINActions.MudraType.Jin, NINActions.MudraType.Ten, NINActions.MudraType.Chi)] // Also works
    public void GetNinjutsuResult_ThreeMudra_Doton_Orderings(NINActions.MudraType a, NINActions.MudraType b, NINActions.MudraType c)
    {
        var result = NINActions.GetNinjutsuResult(a, b, c);

        Assert.Equal(NINActions.NinjutsuType.Doton, result);
    }

    [Theory]
    [InlineData(NINActions.MudraType.Ten, NINActions.MudraType.Chi, NINActions.MudraType.Jin)] // Primary
    [InlineData(NINActions.MudraType.Chi, NINActions.MudraType.Ten, NINActions.MudraType.Jin)] // Also works
    public void GetNinjutsuResult_ThreeMudra_Suiton_Orderings(NINActions.MudraType a, NINActions.MudraType b, NINActions.MudraType c)
    {
        var result = NINActions.GetNinjutsuResult(a, b, c);

        Assert.Equal(NINActions.NinjutsuType.Suiton, result);
    }

    [Fact]
    public void GetNinjutsuResult_ThreeMudra_Invalid_RepeatedSameMudra_Returns_RabbitMedium()
    {
        var result = NINActions.GetNinjutsuResult(NINActions.MudraType.Ten, NINActions.MudraType.Ten, NINActions.MudraType.Ten);

        Assert.Equal(NINActions.NinjutsuType.RabbitMedium, result);
    }
}
