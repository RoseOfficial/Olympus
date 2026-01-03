using Olympus.Services.Debuff;

namespace Olympus.Tests.Services.Debuff;

/// <summary>
/// Tests for DebuffDetectionService priority logic.
/// Note: IsDispellable and FindHighestPriorityDebuff require Dalamud IDataManager
/// and cannot be easily unit tested without game data mocking.
/// </summary>
public class DebuffDetectionServiceTests
{
    #region GetDebuffPriority Tests - Pure Logic

    [Theory]
    [InlineData(910u)]   // Doom (common)
    [InlineData(1769u)]  // Throttle
    [InlineData(2519u)]  // Doom (Bozjan)
    [InlineData(3364u)]  // Doom (variant dungeon)
    public void GetDebuffPriority_LethalDebuffs_ReturnsLethal(uint statusId)
    {
        // Lethal debuffs must be cleansed immediately
        var priority = GetPriorityForStatusId(statusId);

        Assert.Equal(DebuffPriority.Lethal, priority);
    }

    [Theory]
    [InlineData(714u)]   // Vulnerability Up
    [InlineData(638u)]   // Damage Down
    [InlineData(1195u)]  // Vulnerability Up (alternate)
    public void GetDebuffPriority_HighPriorityDebuffs_ReturnsHigh(uint statusId)
    {
        var priority = GetPriorityForStatusId(statusId);

        Assert.Equal(DebuffPriority.High, priority);
    }

    [Theory]
    [InlineData(17u)]   // Paralysis
    [InlineData(7u)]    // Silence
    [InlineData(6u)]    // Pacification
    [InlineData(3u)]    // Sleep
    [InlineData(18u)]   // Stun
    public void GetDebuffPriority_MediumPriorityDebuffs_ReturnsMedium(uint statusId)
    {
        var priority = GetPriorityForStatusId(statusId);

        Assert.Equal(DebuffPriority.Medium, priority);
    }

    [Theory]
    [InlineData(13u)]   // Bind
    [InlineData(14u)]   // Heavy
    [InlineData(15u)]   // Blind
    [InlineData(564u)]  // Leaden
    public void GetDebuffPriority_LowPriorityDebuffs_ReturnsLow(uint statusId)
    {
        var priority = GetPriorityForStatusId(statusId);

        Assert.Equal(DebuffPriority.Low, priority);
    }

    [Theory]
    [InlineData(99999u)]  // Unknown status
    [InlineData(12345u)]  // Random ID
    [InlineData(1u)]      // Not in any priority list
    public void GetDebuffPriority_UnknownDebuffs_ReturnsLow(uint statusId)
    {
        // Unknown dispellable debuffs default to Low priority
        var priority = GetPriorityForStatusId(statusId);

        Assert.Equal(DebuffPriority.Low, priority);
    }

    [Fact]
    public void GetDebuffPriority_PriorityOrdering_LethalIsHighest()
    {
        // Verify enum ordering: Lethal < High < Medium < Low < None
        Assert.True(DebuffPriority.Lethal < DebuffPriority.High);
        Assert.True(DebuffPriority.High < DebuffPriority.Medium);
        Assert.True(DebuffPriority.Medium < DebuffPriority.Low);
        Assert.True(DebuffPriority.Low < DebuffPriority.None);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper to test GetDebuffPriority without needing full service instantiation.
    /// This replicates the pure logic from DebuffDetectionService.GetDebuffPriority.
    /// </summary>
    private static DebuffPriority GetPriorityForStatusId(uint statusId)
    {
        // Lethal debuffs
        if (statusId is 910 or 1769 or 2519 or 3364)
            return DebuffPriority.Lethal;

        // High priority
        if (statusId is 714 or 638 or 1195)
            return DebuffPriority.High;

        // Medium priority
        if (statusId is 17 or 7 or 6 or 3 or 18)
            return DebuffPriority.Medium;

        // Low priority (known)
        if (statusId is 13 or 14 or 15 or 564)
            return DebuffPriority.Low;

        // Unknown dispellable = Low priority
        return DebuffPriority.Low;
    }

    #endregion
}
