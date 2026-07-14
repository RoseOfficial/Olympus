using Olympus.Services.FFLogs;
using Xunit;

namespace Olympus.Tests.Services.FFLogs;

/// <summary>
/// Verifies the tier-selection logic in <see cref="FFlogsRaidTiers.GetTierForTerritory"/>.
/// These are pure data tests with no Dalamud dependencies.
/// </summary>
public class FFlogsRaidTiersTests
{
    // -------------------------------------------------------------------------
    // Territory → tier mapping
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(1321u)] // r9s  Vamp Fatale
    [InlineData(1323u)] // r10s Red Hot
    [InlineData(1325u)] // r11s The Tyrant
    [InlineData(1327u)] // r12s Lindwurm
    public void GetTierForTerritory_HeavyweightTerritory_ReturnsZone73(uint territoryId)
    {
        var tier = FFlogsRaidTiers.GetTierForTerritory(territoryId);
        Assert.Equal(73, tier.ZoneId);
        Assert.Equal("AAC Heavyweight Savage", tier.DisplayName);
    }

    [Theory]
    [InlineData(1257u)] // r5s  Dancing Green
    [InlineData(1259u)] // r6s  Sugar Riot
    [InlineData(1261u)] // r7s  Brute Abombinator
    [InlineData(1263u)] // r8s  Howling Blade
    public void GetTierForTerritory_CruiserweightTerritory_ReturnsZone68(uint territoryId)
    {
        var tier = FFlogsRaidTiers.GetTierForTerritory(territoryId);
        Assert.Equal(68, tier.ZoneId);
        Assert.Equal("AAC Cruiserweight Savage", tier.DisplayName);
    }

    [Theory]
    [InlineData(1226u)] // r1s  Black Cat
    [InlineData(1228u)] // r2s  Honey B. Lovely
    [InlineData(1230u)] // r3s  Brute Bomber
    [InlineData(1232u)] // r4s  Wicked Thunder
    public void GetTierForTerritory_LightHeavyweightTerritory_ReturnsZone62(uint territoryId)
    {
        var tier = FFlogsRaidTiers.GetTierForTerritory(territoryId);
        Assert.Equal(62, tier.ZoneId);
        Assert.Equal("AAC Light-heavyweight Savage", tier.DisplayName);
    }

    // -------------------------------------------------------------------------
    // Fallback behaviour (territory 0 or unknown)
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTierForTerritory_ZeroTerritory_FallsBackToNewestTier()
    {
        // 0 = not in a raid zone; should default to Heavyweight (newest)
        var tier = FFlogsRaidTiers.GetTierForTerritory(0u);
        Assert.Equal(73, tier.ZoneId);
    }

    [Fact]
    public void GetTierForTerritory_UnknownTerritory_FallsBackToNewestTier()
    {
        // An arbitrary non-raid territory should also produce the newest tier
        var tier = FFlogsRaidTiers.GetTierForTerritory(12345u);
        Assert.Equal(73, tier.ZoneId);
    }

    // -------------------------------------------------------------------------
    // Tier data integrity
    // -------------------------------------------------------------------------

    [Fact]
    public void All_IsOrderedNewestFirst()
    {
        // Heavyweight (73) must come before Cruiserweight (68) must come before
        // Light-heavyweight (62) so the fallback always lands on the newest tier.
        var zoneIds = Array.ConvertAll(FFlogsRaidTiers.All, t => t.ZoneId);
        for (var i = 1; i < zoneIds.Length; i++)
        {
            Assert.True(zoneIds[i - 1] > zoneIds[i],
                $"Tier at index {i - 1} (zone {zoneIds[i - 1]}) must be newer than " +
                $"tier at index {i} (zone {zoneIds[i]})");
        }
    }

    [Fact]
    public void HeavyweightTier_HasFiveEncounters()
    {
        // Lindwurm logs phase 1 (104) and phase 2 (105) separately on FFLogs
        var tier = FFlogsRaidTiers.All[0];
        Assert.Equal(5, tier.EncounterIds.Length);
        Assert.Contains(104, tier.EncounterIds);
        Assert.Contains(105, tier.EncounterIds);
    }

    [Fact]
    public void CruiserweightTier_HasFourEncounters()
    {
        var tier = FFlogsRaidTiers.All[1];
        Assert.Equal(4, tier.EncounterIds.Length);
    }
}
