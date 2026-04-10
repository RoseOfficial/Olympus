using Olympus.Rotation.Common.Helpers;

namespace Olympus.Tests.Rotation.Common.Helpers;

/// <summary>
/// Tests for <see cref="HealerPartyHelper"/>'s NoHeal status checks.
/// Dalamud's <c>StatusList</c> cannot be easily mocked, so the full
/// <c>HasNoHealStatus(IBattleChara)</c> path is exercised in-game. These
/// tests target the pure <see cref="HealerPartyHelper.IsNoHealStatusId"/>
/// predicate that owns the list itself.
/// </summary>
public class HealerPartyHelperTests
{
    [Theory]
    [InlineData(82u, "Hallowed Ground (PLD invuln)")]
    [InlineData(409u, "Holmgang (WAR invuln)")]
    [InlineData(810u, "Living Dead (DRK invuln)")]
    [InlineData(1836u, "Superbolide (GNB invuln)")]
    [InlineData(1220u, "Excogitation (SCH delayed heal)")]
    [InlineData(2685u, "Catharsis of Corundum (GNB delayed heal)")]
    public void IsNoHealStatusId_RecognizedStatuses_ReturnsTrue(uint statusId, string description)
    {
        Assert.True(HealerPartyHelper.IsNoHealStatusId(statusId),
            $"Expected {description} (ID {statusId}) to be flagged as a NoHeal status");
    }

    [Theory]
    [InlineData(0u)]                  // no status
    [InlineData(148u)]                 // Raise — raise target, not invuln
    [InlineData(2656u)]                // Transcendent — handled separately (HasTranscendent)
    [InlineData(1174u)]                // Divine Veil — PLD shield, still allow heals
    [InlineData(1191u)]                // Stoneskin — old shield, still allow heals
    [InlineData(86u)]                  // Dia (WHM DoT — wrong direction entirely)
    public void IsNoHealStatusId_UnrelatedStatuses_ReturnsFalse(uint statusId)
    {
        Assert.False(HealerPartyHelper.IsNoHealStatusId(statusId));
    }

    [Fact]
    public void IsNoHealStatusId_BoundariesAroundHallowedGround()
    {
        // Sanity: status IDs adjacent to Hallowed Ground (82) must not leak in.
        Assert.False(HealerPartyHelper.IsNoHealStatusId(81u));
        Assert.True(HealerPartyHelper.IsNoHealStatusId(82u));
        Assert.False(HealerPartyHelper.IsNoHealStatusId(83u));
    }

    [Fact]
    public void IsNoHealStatusId_WalkingDead_NotInList()
    {
        // DRK Living Dead (810) transitions to Walking Dead (811) when HP
        // hits 1 or the 10s expires. During Walking Dead the DRK MUST be
        // healed back to full or dies — direct heals are the correct
        // response, not wasted. 811 must NOT be in the skip list.
        Assert.True(HealerPartyHelper.IsNoHealStatusId(810u));
        Assert.False(HealerPartyHelper.IsNoHealStatusId(811u));
    }
}
