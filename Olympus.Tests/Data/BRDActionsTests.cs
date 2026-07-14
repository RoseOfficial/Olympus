using Olympus.Data;
using Xunit;

namespace Olympus.Tests.Data;

/// <summary>
/// Unit tests for BRDActions level-aware action selectors.
/// Frozen at the Lv.92 boundary to catch action-ID regressions without a running game.
/// </summary>
public class BRDActionsTests
{
    // GetBloodletter: Bloodletter (ActionId 110) below Lv.92;
    //                 HeartbreakShot (ActionId 36975) at and above Lv.92.
    // ActionManager tracks HeartbreakShot charges under 36975, not 110, so reading
    // charges against the wrong ID silently returns 0 at max level.

    [Theory]
    [InlineData(1,   110u)]    // low level: Bloodletter
    [InlineData(91,  110u)]    // one below replacement threshold: Bloodletter
    [InlineData(92,  36975u)]  // replacement level: HeartbreakShot
    [InlineData(100, 36975u)]  // max level: HeartbreakShot
    public void GetBloodletter_ReturnsLevelAppropriateAction(byte level, uint expectedActionId)
    {
        Assert.Equal(expectedActionId, BRDActions.GetBloodletter(level).ActionId);
    }
}
