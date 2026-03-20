using Olympus.Data;
using Olympus.Services.Action;

namespace Olympus.Tests.Services.Action;

/// <summary>
/// Tests for WeaveOptimizer timing and weave mode calculations.
/// Validates single/double weave decisions, GCD clip prevention, and edge cases.
/// </summary>
public class WeaveOptimizerTests
{
    private readonly WeaveOptimizer _optimizer = new();

    // Constant references for readability
    private static readonly float AnimLock = FFXIVTimings.AnimationLockBase;       // 0.7f
    private static readonly float ClipBuffer = FFXIVTimings.ClipPreventionBuffer;  // 0.1f

    #region RecommendedWeaveMode

    [Fact]
    public void RecommendedWeaveMode_NoGcdActive_ReturnsNone()
    {
        _optimizer.Update(gcdRemaining: 0f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.Equal(WeaveMode.None, _optimizer.RecommendedWeaveMode);
    }

    [Fact]
    public void RecommendedWeaveMode_InAnimationLock_ReturnsNone()
    {
        // GCD active but animation lock blocks weaving
        _optimizer.Update(gcdRemaining: 2.0f, gcdTotal: 2.5f, animationLockRemaining: 0.5f, ogcdsUsedThisCycle: 0);

        Assert.Equal(WeaveMode.None, _optimizer.RecommendedWeaveMode);
    }

    [Fact]
    public void RecommendedWeaveMode_PlentyOfTime_ReturnsDouble()
    {
        // Full GCD window, no animation lock, no oGCDs used yet
        _optimizer.Update(gcdRemaining: 2.4f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.Equal(WeaveMode.Double, _optimizer.RecommendedWeaveMode);
    }

    [Fact]
    public void RecommendedWeaveMode_MediumTime_ReturnsSingle()
    {
        // Enough time for one weave but not two
        _optimizer.Update(gcdRemaining: 1.0f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.Equal(WeaveMode.Single, _optimizer.RecommendedWeaveMode);
    }

    [Fact]
    public void RecommendedWeaveMode_LateWindow_ReturnsLate()
    {
        // Just barely enough for one fast oGCD
        var lateTime = AnimLock + ClipBuffer + 0.1f; // Slightly above minimum but below single threshold
        _optimizer.Update(gcdRemaining: lateTime, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.Equal(WeaveMode.Late, _optimizer.RecommendedWeaveMode);
    }

    [Fact]
    public void RecommendedWeaveMode_TooLittleTime_ReturnsNone()
    {
        // Not enough time for even a fast oGCD
        _optimizer.Update(gcdRemaining: 0.3f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.Equal(WeaveMode.None, _optimizer.RecommendedWeaveMode);
    }

    [Fact]
    public void RecommendedWeaveMode_AlreadyUsedOneOgcd_DoesNotReturnDouble()
    {
        // Full GCD window but already used one oGCD
        _optimizer.Update(gcdRemaining: 2.4f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 1);

        // Should be Single, not Double (already used one slot)
        Assert.NotEqual(WeaveMode.Double, _optimizer.RecommendedWeaveMode);
    }

    #endregion

    #region CanDoubleWeave

    [Fact]
    public void CanDoubleWeave_FullGcdWindow_ReturnsTrue()
    {
        _optimizer.Update(gcdRemaining: 2.4f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.True(_optimizer.CanDoubleWeave);
    }

    [Fact]
    public void CanDoubleWeave_ShortGcdTotal_ReturnsFalse()
    {
        // GCD total below double weave threshold
        _optimizer.Update(gcdRemaining: 1.8f, gcdTotal: 1.8f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.False(_optimizer.CanDoubleWeave);
    }

    [Fact]
    public void CanDoubleWeave_NotEnoughRemainingTime_ReturnsFalse()
    {
        // GCD total meets threshold but remaining time is too short
        var requiredTime = (AnimLock * 2) + ClipBuffer;
        _optimizer.Update(gcdRemaining: requiredTime - 0.1f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.False(_optimizer.CanDoubleWeave);
    }

    [Fact]
    public void CanDoubleWeave_InAnimationLock_ReturnsFalse()
    {
        _optimizer.Update(gcdRemaining: 2.4f, gcdTotal: 2.5f, animationLockRemaining: 0.5f, ogcdsUsedThisCycle: 0);

        Assert.False(_optimizer.CanDoubleWeave);
    }

    #endregion

    #region CanWeaveNow (WouldClipGcd equivalent)

    [Fact]
    public void CanWeaveNow_PlentyOfTime_ReturnsTrue()
    {
        _optimizer.Update(gcdRemaining: 2.0f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.True(_optimizer.CanWeaveNow());
    }

    [Fact]
    public void CanWeaveNow_WouldClipGcd_ReturnsFalse()
    {
        // Only 0.5s left - not enough for default 0.6s animation lock + 0.1s buffer
        _optimizer.Update(gcdRemaining: 0.5f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.False(_optimizer.CanWeaveNow());
    }

    [Fact]
    public void CanWeaveNow_ExactlyEnoughTime_ReturnsTrue()
    {
        var exactTime = 0.6f + ClipBuffer; // Exactly enough for default animation lock
        _optimizer.Update(gcdRemaining: exactTime, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.True(_optimizer.CanWeaveNow());
    }

    [Fact]
    public void CanWeaveNow_InAnimationLock_ReturnsFalse()
    {
        _optimizer.Update(gcdRemaining: 2.0f, gcdTotal: 2.5f, animationLockRemaining: 0.3f, ogcdsUsedThisCycle: 0);

        Assert.False(_optimizer.CanWeaveNow());
    }

    [Fact]
    public void CanWeaveNow_CustomAnimationLock_ChecksThatValue()
    {
        // 1.0s remaining, custom 0.8s animation lock + 0.1s buffer = 0.9s needed
        _optimizer.Update(gcdRemaining: 1.0f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.True(_optimizer.CanWeaveNow(animationLock: 0.8f));

        // But 1.2s animation lock + 0.1s buffer = 1.3s > 1.0s remaining
        Assert.False(_optimizer.CanWeaveNow(animationLock: 1.2f));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ZeroGcdRemaining_AllMethodsReturnSafe()
    {
        _optimizer.Update(gcdRemaining: 0f, gcdTotal: 0f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.Equal(WeaveMode.None, _optimizer.RecommendedWeaveMode);
        Assert.False(_optimizer.CanDoubleWeave);
        Assert.False(_optimizer.CanWeaveNow());
        Assert.Equal(0, _optimizer.RemainingWeaveSlots);
    }

    [Fact]
    public void AnimationLockActive_AllMethodsReturnSafe()
    {
        _optimizer.Update(gcdRemaining: 2.0f, gcdTotal: 2.5f, animationLockRemaining: 0.6f, ogcdsUsedThisCycle: 0);

        Assert.Equal(WeaveMode.None, _optimizer.RecommendedWeaveMode);
        Assert.False(_optimizer.CanDoubleWeave);
        Assert.False(_optimizer.CanWeaveNow());
        Assert.Equal(0, _optimizer.RemainingWeaveSlots);
    }

    [Fact]
    public void OptimalWeaveTime_InAnimLock_ReturnsAnimLockDuration()
    {
        _optimizer.Update(gcdRemaining: 2.0f, gcdTotal: 2.5f, animationLockRemaining: 0.4f, ogcdsUsedThisCycle: 0);

        Assert.Equal(0.4f, _optimizer.OptimalWeaveTime);
    }

    [Fact]
    public void OptimalWeaveTime_NoGcd_ReturnsNegative()
    {
        _optimizer.Update(gcdRemaining: 0f, gcdTotal: 0f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.True(_optimizer.OptimalWeaveTime < 0);
    }

    [Fact]
    public void OptimalWeaveTime_ReadyToWeave_ReturnsZero()
    {
        _optimizer.Update(gcdRemaining: 2.0f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.Equal(0f, _optimizer.OptimalWeaveTime);
    }

    #endregion

    #region RemainingWeaveSlots

    [Fact]
    public void RemainingWeaveSlots_FullWindow_ReturnsTwo()
    {
        _optimizer.Update(gcdRemaining: 2.4f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.Equal(2, _optimizer.RemainingWeaveSlots);
    }

    [Fact]
    public void RemainingWeaveSlots_OneUsed_ReturnsOne()
    {
        _optimizer.Update(gcdRemaining: 2.4f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 1);

        Assert.Equal(1, _optimizer.RemainingWeaveSlots);
    }

    [Fact]
    public void RemainingWeaveSlots_TwoUsed_ReturnsZero()
    {
        _optimizer.Update(gcdRemaining: 2.4f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 2);

        Assert.Equal(0, _optimizer.RemainingWeaveSlots);
    }

    #endregion

    #region Pending oGCD Management

    [Fact]
    public void RegisterAndGetNextOgcd_ReturnsHighestPriority()
    {
        _optimizer.RegisterPendingOgcd(100, OgcdPriority.Low);
        _optimizer.RegisterPendingOgcd(200, OgcdPriority.Emergency);
        _optimizer.RegisterPendingOgcd(300, OgcdPriority.Healing);

        Assert.Equal(200u, _optimizer.GetNextOgcd());
    }

    [Fact]
    public void GetNextOgcd_NoPending_ReturnsZero()
    {
        Assert.Equal(0u, _optimizer.GetNextOgcd());
    }

    [Fact]
    public void RemoveOgcd_RemovesCorrectAction()
    {
        _optimizer.RegisterPendingOgcd(100, OgcdPriority.Emergency);
        _optimizer.RegisterPendingOgcd(200, OgcdPriority.Low);

        _optimizer.RemoveOgcd(100);

        Assert.Equal(200u, _optimizer.GetNextOgcd());
    }

    [Fact]
    public void ClearPendingOgcds_ClearsAll()
    {
        _optimizer.RegisterPendingOgcd(100, OgcdPriority.Emergency);
        _optimizer.RegisterPendingOgcd(200, OgcdPriority.Low);

        _optimizer.ClearPendingOgcds();

        Assert.Equal(0u, _optimizer.GetNextOgcd());
    }

    [Fact]
    public void RegisterPendingOgcd_DuplicateActionId_IgnoresDuplicate()
    {
        _optimizer.RegisterPendingOgcd(100, OgcdPriority.Emergency);
        _optimizer.RegisterPendingOgcd(100, OgcdPriority.Low); // Same ID, different priority

        _optimizer.RemoveOgcd(100);
        // If duplicate was registered, there would be a second entry
        Assert.Equal(0u, _optimizer.GetNextOgcd());
    }

    #endregion

    #region Update Cycle Detection

    [Fact]
    public void Update_NewGcdCycle_ClearsPendingOgcds()
    {
        _optimizer.Update(gcdRemaining: 0.5f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);
        _optimizer.RegisterPendingOgcd(100, OgcdPriority.Emergency);

        // Simulate new GCD starting (remaining jumps up significantly)
        _optimizer.Update(gcdRemaining: 2.5f, gcdTotal: 2.5f, animationLockRemaining: 0f, ogcdsUsedThisCycle: 0);

        Assert.Equal(0u, _optimizer.GetNextOgcd());
    }

    #endregion
}
