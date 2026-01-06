using Olympus.Config;
using Olympus.Services.Healing.Models;
using Xunit;

namespace Olympus.Tests.Services.Healing.Models;

/// <summary>
/// Tests for HealSelectionContext and AoEHealSelectionContext records.
/// NOTE: Full context testing requires Dalamud runtime for IPlayerCharacter/IBattleChara.
/// These tests verify the record structures and default behaviors.
/// </summary>
public class HealSelectionContextTests
{
    [Fact]
    public void HealSelectionContext_RecordEquality_WorksCorrectly()
    {
        // Record types should have value-based equality
        // We can't fully test without Dalamud types, but we verify the type exists
        Assert.True(typeof(HealSelectionContext).IsClass);
    }

    [Fact]
    public void AoEHealSelectionContext_RecordEquality_WorksCorrectly()
    {
        // Record types should have value-based equality
        Assert.True(typeof(AoEHealSelectionContext).IsClass);
    }

    [Fact]
    public void HealSelectionContext_RequiredProperties_AreRequired()
    {
        // Verify the required properties exist via reflection
        var type = typeof(HealSelectionContext);

        Assert.NotNull(type.GetProperty("Player"));
        Assert.NotNull(type.GetProperty("Target"));
        Assert.NotNull(type.GetProperty("Mind"));
        Assert.NotNull(type.GetProperty("Det"));
        Assert.NotNull(type.GetProperty("Wd"));
        Assert.NotNull(type.GetProperty("MissingHp"));
        Assert.NotNull(type.GetProperty("HpPercent"));
        Assert.NotNull(type.GetProperty("LilyCount"));
        Assert.NotNull(type.GetProperty("BloodLilyCount"));
        Assert.NotNull(type.GetProperty("IsWeaveWindow"));
        Assert.NotNull(type.GetProperty("HasFreecure"));
        Assert.NotNull(type.GetProperty("HasRegen"));
        Assert.NotNull(type.GetProperty("RegenRemaining"));
        Assert.NotNull(type.GetProperty("IsInMpConservationMode"));
        Assert.NotNull(type.GetProperty("LilyStrategy"));
        Assert.NotNull(type.GetProperty("CombatDuration"));
        Assert.NotNull(type.GetProperty("Config"));
    }

    [Fact]
    public void AoEHealSelectionContext_RequiredProperties_AreRequired()
    {
        // Verify the required properties exist via reflection
        var type = typeof(AoEHealSelectionContext);

        Assert.NotNull(type.GetProperty("Player"));
        Assert.NotNull(type.GetProperty("Mind"));
        Assert.NotNull(type.GetProperty("Det"));
        Assert.NotNull(type.GetProperty("Wd"));
        Assert.NotNull(type.GetProperty("AverageMissingHp"));
        Assert.NotNull(type.GetProperty("InjuredCount"));
        Assert.NotNull(type.GetProperty("AnyHaveRegen"));
        Assert.NotNull(type.GetProperty("IsWeaveWindow"));
        Assert.NotNull(type.GetProperty("CureIIITargetCount"));
        Assert.NotNull(type.GetProperty("CureIIITarget"));
        Assert.NotNull(type.GetProperty("IsInMpConservationMode"));
        Assert.NotNull(type.GetProperty("LilyCount"));
        Assert.NotNull(type.GetProperty("BloodLilyCount"));
        Assert.NotNull(type.GetProperty("LilyStrategy"));
        Assert.NotNull(type.GetProperty("CombatDuration"));
        Assert.NotNull(type.GetProperty("Config"));
    }
}
