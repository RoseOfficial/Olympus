using Olympus.Services.Movement.Humanization;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class ReactionDelayTests
{
    [Theory]
    [InlineData(123ul, 250, 700)]
    [InlineData(0ul, 250, 700)]
    [InlineData(ulong.MaxValue, 100, 1000)]
    public void Compute_StaysWithinRange(ulong seed, int minMs, int maxMs)
    {
        var result = ReactionDelay.Compute(seed, minMs, maxMs);
        Assert.InRange(result, minMs, maxMs);
    }

    [Fact]
    public void Compute_DeterministicForSameSeed()
    {
        var a = ReactionDelay.Compute(42, 250, 700);
        var b = ReactionDelay.Compute(42, 250, 700);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Compute_DifferentSeedsDifferentValues()
    {
        var a = ReactionDelay.Compute(1, 250, 700);
        var b = ReactionDelay.Compute(2, 250, 700);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Compute_MinEqualsMax_ReturnsMin()
    {
        Assert.Equal(500, ReactionDelay.Compute(123, 500, 500));
    }
}
