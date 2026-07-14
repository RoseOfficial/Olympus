using Olympus.Data;
using Xunit;

namespace Olympus.Tests.Data;

/// <summary>
/// Verifies that CoordinatedCooldowns contains the correct action IDs for each category.
/// Wrong IDs cause WasActionUsedByOther to always return false, making coordination silently no-op.
/// </summary>
public class CoordinatedCooldownsTests
{
    // ── PartyMitigations / TankPartyMitigations ────────────────────────────

    [Fact]
    public void PartyMitigations_ContainsDarkMissionary()
    {
        // ActionIds.DarkMissionary = 16471 (DRKActions.DarkMissionary.ActionId)
        // Previously wrong: literal 36927 (DRK ShadowedVigil personal defensive)
        Assert.Contains(ActionIds.DarkMissionary, CoordinatedCooldowns.PartyMitigations);
        Assert.Equal(16471u, ActionIds.DarkMissionary);
    }

    [Fact]
    public void PartyMitigations_ContainsHeartOfLight()
    {
        // ActionIds.HeartOfLight = 16160 (GNBActions.HeartOfLight.ActionId)
        // Previously wrong: literal 36934 (GNB Trajectory gap-closer)
        Assert.Contains(ActionIds.HeartOfLight, CoordinatedCooldowns.PartyMitigations);
        Assert.Equal(16160u, ActionIds.HeartOfLight);
    }

    [Fact]
    public void TankPartyMitigations_ContainsDarkMissionary()
    {
        Assert.Contains(ActionIds.DarkMissionary, CoordinatedCooldowns.TankPartyMitigations);
    }

    [Fact]
    public void TankPartyMitigations_ContainsHeartOfLight()
    {
        Assert.Contains(ActionIds.HeartOfLight, CoordinatedCooldowns.TankPartyMitigations);
    }

    [Fact]
    public void PartyMitigations_DoesNotContainShadowedVigil()
    {
        // ShadowedVigil (36927) is a personal defensive, not a party mitigation
        Assert.DoesNotContain(ActionIds.ShadowedVigil, CoordinatedCooldowns.PartyMitigations);
    }

    [Fact]
    public void PartyMitigations_DoesNotContainTrajectory()
    {
        // Trajectory (36934) is a GNB gap-closer, not a party mitigation
        Assert.DoesNotContain(36934u, CoordinatedCooldowns.PartyMitigations);
    }

    // ── PersonalDefensives ─────────────────────────────────────────────────

    [Fact]
    public void PersonalDefensives_ShadowedVigilHasCorrectId()
    {
        // ActionIds.ShadowedVigil must be 36927 (DRKActions.ShadowedVigil.ActionId)
        // Previously wrong: 36924 (WAR Primal Wrath)
        Assert.Equal(36927u, ActionIds.ShadowedVigil);
        Assert.True(CoordinatedCooldowns.IsPersonalDefensive(36927u),
            "DRK ShadowedVigil (36927) must be tracked as a personal defensive");
    }

    [Fact]
    public void PersonalDefensives_DoesNotContainPrimalWrath()
    {
        // 36924 is WAR Primal Wrath, not DRK ShadowedVigil
        Assert.False(CoordinatedCooldowns.IsPersonalDefensive(36924u),
            "WAR Primal Wrath (36924) must not be in PersonalDefensives");
    }

    // ── PartyDebuffs (finding #7) ──────────────────────────────────────────

    [Fact]
    public void PartyDebuffs_ContainsFeint()
    {
        // Feint = 7549 (RoleActions.Feint.ActionId)
        Assert.True(CoordinatedCooldowns.IsPartyDebuff(RoleActions.Feint.ActionId));
        Assert.True(CoordinatedCooldowns.IsPartyDebuff(7549u));
    }

    [Fact]
    public void PartyDebuffs_ContainsAddle()
    {
        // Addle = 7560 (RoleActions.Addle.ActionId)
        Assert.True(CoordinatedCooldowns.IsPartyDebuff(RoleActions.Addle.ActionId));
        Assert.True(CoordinatedCooldowns.IsPartyDebuff(7560u));
    }

    [Fact]
    public void IsPartyDebuff_ReturnsFalseForUnrelatedAction()
    {
        Assert.False(CoordinatedCooldowns.IsPartyDebuff(ActionIds.Reprisal));
    }

    // ── DefaultRecastTimes correctness ─────────────────────────────────────

    [Fact]
    public void DefaultRecastTimes_DarkMissionaryKeyedCorrectly()
    {
        Assert.Equal(90_000, CoordinatedCooldowns.GetDefaultRecastTime(ActionIds.DarkMissionary));
    }

    [Fact]
    public void DefaultRecastTimes_HeartOfLightKeyedCorrectly()
    {
        Assert.Equal(90_000, CoordinatedCooldowns.GetDefaultRecastTime(ActionIds.HeartOfLight));
    }

    [Fact]
    public void DefaultRecastTimes_FeintKeyedCorrectly()
    {
        Assert.Equal(90_000, CoordinatedCooldowns.GetDefaultRecastTime(RoleActions.Feint.ActionId));
    }

    [Fact]
    public void DefaultRecastTimes_AddleKeyedCorrectly()
    {
        Assert.Equal(90_000, CoordinatedCooldowns.GetDefaultRecastTime(RoleActions.Addle.ActionId));
    }

    [Fact]
    public void DefaultRecastTimes_ShadowedVigilKeyedCorrectly()
    {
        // After fixing ActionIds.ShadowedVigil = 36927, this should return 120s
        Assert.Equal(120_000, CoordinatedCooldowns.GetDefaultRecastTime(ActionIds.ShadowedVigil));
        Assert.Equal(120_000, CoordinatedCooldowns.GetDefaultRecastTime(36927u));
    }
}
