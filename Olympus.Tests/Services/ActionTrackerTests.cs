using Olympus.Models;
using Olympus.Services;
using Olympus.Tests.Mocks;
using Xunit;

namespace Olympus.Tests.Services;

public class ActionTrackerTests
{
    private static ActionTracker CreateTracker() => MockBuilders.CreateMockActionTracker();

    [Fact]
    public void ClearSpellUsageCounts_PreservesHistory()
    {
        var tracker = CreateTracker();
        tracker.LogAttempt(120, "Target", 10000, ActionResult.Success, 100);
        tracker.LogAttempt(121, "Target", 10000, ActionResult.Success, 100);

        tracker.ClearSpellUsageCounts();

        Assert.Equal(2, tracker.GetHistory().Count);
    }

    [Fact]
    public void ClearSpellUsageCounts_ResetsUsageSoGetSpellUsageCountsReturnsEmpty()
    {
        var tracker = CreateTracker();
        tracker.LogAttempt(120, "Target", 10000, ActionResult.Success, 100);

        tracker.ClearSpellUsageCounts();

        // spellUsageCounts is empty so GetSpellUsageCounts skips all name lookups and returns []
        Assert.Empty(tracker.GetSpellUsageCounts());
    }
}
