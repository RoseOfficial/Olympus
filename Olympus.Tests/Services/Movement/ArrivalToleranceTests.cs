using Olympus.Services.Movement.Humanization;
using Xunit;

namespace Olympus.Tests.Services.Movement;

public class ArrivalToleranceTests
{
    [Theory]
    [InlineData(123ul, 0.3f, 0.8f)]
    [InlineData(0ul, 0.3f, 0.8f)]
    public void Compute_StaysWithinRange(ulong seed, float min, float max)
    {
        var result = ArrivalTolerance.Compute(seed, min, max);
        Assert.InRange(result, min, max);
    }

    [Fact]
    public void Compute_Deterministic()
    {
        Assert.Equal(ArrivalTolerance.Compute(7, 0.3f, 0.8f), ArrivalTolerance.Compute(7, 0.3f, 0.8f));
    }
}
