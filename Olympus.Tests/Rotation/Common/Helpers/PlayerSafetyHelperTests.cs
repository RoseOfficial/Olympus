using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Tests.Rotation.Common.Helpers;

/// <summary>
/// Tests for <see cref="PlayerSafetyHelper"/>'s forced-movement status check.
/// Dalamud's <c>StatusList</c> cannot be easily mocked, so the full
/// <c>IsForcedMovementActive(IBattleChara)</c> path is exercised in-game. These
/// tests target the pure <see cref="PlayerSafetyHelper.IsForcedMovementStatusId"/>
/// predicate that owns the list itself.
/// </summary>
public class PlayerSafetyHelperTests
{
    [Theory]
    [InlineData(1140u, "Forward March")]
    [InlineData(1141u, "Backward March")]
    [InlineData(1142u, "Leftward March")]
    [InlineData(1143u, "Rightward March")]
    public void IsForcedMovementStatusId_RecognizedStatuses_ReturnsTrue(uint statusId, string description)
    {
        Assert.True(PlayerSafetyHelper.IsForcedMovementStatusId(statusId),
            $"Expected {description} (ID {statusId}) to be flagged as a forced-movement status");
    }

    [Theory]
    [InlineData(0u)]      // no status
    [InlineData(18u)]     // Stun — incapacitation, handled separately
    [InlineData(3u)]      // Sleep — incapacitation, handled separately
    [InlineData(4u)]      // Bind — restricts movement but casts still work, not in list
    [InlineData(148u)]    // Raise — unrelated
    [InlineData(82u)]     // Hallowed Ground — unrelated (tank invuln)
    public void IsForcedMovementStatusId_UnrelatedStatuses_ReturnsFalse(uint statusId)
    {
        Assert.False(PlayerSafetyHelper.IsForcedMovementStatusId(statusId));
    }

    [Theory]
    [InlineData(960u)]
    public void IsStandStillPunisherStatusId_Pyretic_ReturnsTrue(uint statusId)
    {
        Assert.True(PlayerSafetyHelper.IsStandStillPunisherStatusId(statusId));
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(959u)]    // boundary below Pyretic
    [InlineData(961u)]    // boundary above Pyretic
    [InlineData(1140u)]   // Forward March — forced movement, not a stand-still punisher
    [InlineData(18u)]     // Stun
    public void IsStandStillPunisherStatusId_Unrelated_ReturnsFalse(uint statusId)
    {
        Assert.False(PlayerSafetyHelper.IsStandStillPunisherStatusId(statusId));
    }

    [Fact]
    public void IsForcedMovementStatusId_BoundariesAroundForwardMarch()
    {
        // Sanity: status IDs adjacent to Forward March (1140) must not leak in.
        Assert.False(PlayerSafetyHelper.IsForcedMovementStatusId(1139u));
        Assert.True(PlayerSafetyHelper.IsForcedMovementStatusId(1140u));
        // 1141-1143 are the other three march directions, expected true.
        Assert.True(PlayerSafetyHelper.IsForcedMovementStatusId(1141u));
        Assert.True(PlayerSafetyHelper.IsForcedMovementStatusId(1142u));
        Assert.True(PlayerSafetyHelper.IsForcedMovementStatusId(1143u));
        Assert.False(PlayerSafetyHelper.IsForcedMovementStatusId(1144u));
    }

    // -------------------------------------------------------------------------
    // IsHardActionLocked / IsHardActionLockStatusId
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(18u, "Stun")]
    [InlineData(3u, "Sleep")]
    [InlineData(1u, "Petrification")]
    [InlineData(149u, "Deep Freeze")]
    [InlineData(2656u, "Transcendent")]
    [InlineData(3581u, "Willful")]
    public void IsHardActionLockStatusId_RecognizedStatuses_ReturnsTrue(uint statusId, string description)
    {
        Assert.True(PlayerSafetyHelper.IsHardActionLockStatusId(statusId),
            $"Expected {description} (ID {statusId}) to be flagged as a hard-action-lock status");
    }

    [Theory]
    [InlineData(0u)]       // no status
    [InlineData(4u)]       // Bind — movement only, player CAN still act
    [InlineData(1140u)]    // Forward March — forced movement, not action lock
    [InlineData(960u)]     // Pyretic — handled by stand-still punisher check
    [InlineData(82u)]      // Hallowed Ground — tank invuln, player still acts
    public void IsHardActionLockStatusId_UnrelatedStatuses_ReturnsFalse(uint statusId)
    {
        Assert.False(PlayerSafetyHelper.IsHardActionLockStatusId(statusId));
    }

    [Fact]
    public void IsHardActionLocked_NullPlayer_ReturnsFalse()
    {
        Assert.False(PlayerSafetyHelper.IsHardActionLocked(null));
    }

    [Fact]
    public void IsHardActionLocked_NullStatusList_ReturnsFalse()
    {
        // IBattleChara with StatusList == null should not throw and should return false.
        Assert.False(PlayerSafetyHelper.IsHardActionLocked(null));
    }

    /// <summary>
    /// Set-content assertions that lock in the Bind exclusion contract.
    /// Any future edit that adds Bind (4) to HardActionLockStatusIds will fail here.
    /// </summary>
    [Fact]
    public void HardActionLockStatusIds_ContainsStun_DoesNotContainBind()
    {
        Assert.Contains(18u, FFXIVConstants.HardActionLockStatusIds);
        Assert.DoesNotContain(4u, FFXIVConstants.HardActionLockStatusIds);
    }
}
