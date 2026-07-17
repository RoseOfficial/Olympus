using Xunit;

namespace Olympus.Tests.Rotation.AresCore;

/// <summary>
/// Proves IRotationContext.CountdownRemaining surfaces the value threaded in at construction.
/// Spec A2 requires "one test proving a context surfaces a countdown value."
/// AresContext (concrete-returning factory) is used as the representative.
/// </summary>
public class CountdownRemainingContextTests
{
    [Fact]
    public void AresContext_surfaces_non_null_countdown_value()
    {
        var ctx = AresTestContext.Create(countdownRemaining: 5f);
        Assert.Equal(5f, ctx.CountdownRemaining);
    }

    [Fact]
    public void AresContext_surfaces_null_when_countdown_not_active()
    {
        var ctx = AresTestContext.Create(countdownRemaining: null);
        Assert.Null(ctx.CountdownRemaining);
    }
}
