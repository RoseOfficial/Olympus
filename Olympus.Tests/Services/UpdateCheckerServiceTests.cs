using Olympus.Services;
using Xunit;

namespace Olympus.Tests.Services;

public class UpdateCheckerServiceTests
{
    [Theory]
    // The phantom-update bug: padded repo version vs 3-part plugin constant is NOT newer.
    [InlineData("4.17.2.0", "4.17.2", false)]
    [InlineData("4.17.2", "4.17.2.0", false)]
    [InlineData("4.17.2", "4.17.2", false)]
    [InlineData("4.17.2.0", "4.17.2.0", false)]
    // Genuine updates still detected across part-count differences.
    [InlineData("4.17.3.0", "4.17.2", true)]
    [InlineData("4.18.0.0", "4.17.2", true)]
    [InlineData("5.0.0", "4.17.2.0", true)]
    // Downgrades are not updates.
    [InlineData("4.17.1.0", "4.17.2", false)]
    // Unparseable input is never an update.
    [InlineData("not-a-version", "4.17.2", false)]
    [InlineData("4.17.3", "not-a-version", false)]
    public void IsNewer_NormalizesPartCounts(string latest, string current, bool expected)
    {
        Assert.Equal(expected, UpdateCheckerService.IsNewer(latest, current));
    }
}
