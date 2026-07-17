using Moq;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Input;
using Olympus.Timeline;
using Xunit;

namespace Olympus.Tests.Rotation.Common.Helpers;

/// <summary>
/// Targeted tests proving that ShouldDumpForDowntime IGNORES ModifierKeys (dump decisions
/// are loss prevention, not aggression) and covers the 18s window used by SMN demi guards.
/// Modifier-key behavioral coverage also lives in BurstHoldHelperTests
/// (ShouldDumpForDowntime_BurstOverrideActive_DoesNotAffectResult and
/// ShouldDumpForDowntime_ConservativeOverrideActive_DoesNotAffectResult), but the
/// spec mandates a dedicated file so the intent is unmistakable.
/// Each test resets BurstHoldHelper.ModifierKeys in both ctor and Dispose to prevent
/// cross-test contamination (the documented CLAUDE.md invariant).
/// </summary>
public sealed class BurstHoldHelperDowntimeDumpTests : IDisposable
{
    public BurstHoldHelperDowntimeDumpTests()
    {
        BurstHoldHelper.ModifierKeys = null;
    }

    public void Dispose()
    {
        BurstHoldHelper.ModifierKeys = null;
    }

    // -----------------------------------------------------------------------
    // Shared helpers
    // -----------------------------------------------------------------------

    private static Mock<ITimelineService> TimelineSvc(float confidence, float? secondsUntil)
    {
        var m = new Mock<ITimelineService>();
        m.Setup(s => s.Confidence).Returns(confidence);
        m.Setup(s => s.SecondsUntilNextUntargetablePhase()).Returns(secondsUntil);
        return m;
    }

    private static Mock<IModifierKeyService> Modifier(bool burst = false, bool conservative = false)
    {
        var m = new Mock<IModifierKeyService>();
        m.Setup(x => x.IsBurstOverride).Returns(burst);
        m.Setup(x => x.IsConservativeOverride).Returns(conservative);
        return m;
    }

    // -----------------------------------------------------------------------
    // Modifier-key invariant: dump result must not change when overrides are held
    // -----------------------------------------------------------------------

    [Fact]
    public void ShouldDumpForDowntime_ReturnsTrue_EvenWhenConservativeKeyHeld()
    {
        // Conservative override forces burst holds to engage, but dump decisions
        // are loss prevention and must never be suppressed by modifier keys.
        var svc = TimelineSvc(confidence: 1.0f, secondsUntil: 8f);
        BurstHoldHelper.ModifierKeys = Modifier(conservative: true).Object;

        Assert.True(BurstHoldHelper.ShouldDumpForDowntime(svc.Object, 10f));
    }

    [Fact]
    public void ShouldDumpForDowntime_ReturnsTrue_EvenWhenBurstKeyHeld()
    {
        // Burst override forces burst windows to be treated as active, but dump
        // decisions (spending gauge before downtime) are independent of that.
        var svc = TimelineSvc(confidence: 1.0f, secondsUntil: 8f);
        BurstHoldHelper.ModifierKeys = Modifier(burst: true).Object;

        Assert.True(BurstHoldHelper.ShouldDumpForDowntime(svc.Object, 10f));
    }

    // -----------------------------------------------------------------------
    // 18s window coverage: substitute test for SMN demi guard
    // (FireSummonDemiRaw uses windowSeconds: 18f; native dispatch is untestable
    // in unit tests, so this helper-level test is the spec-designated substitute.)
    // -----------------------------------------------------------------------

    [Fact]
    public void ShouldDumpForDowntime_ReturnsTrue_WhenWindowIsLarger_18s()
    {
        // secondsUntil=15f is inside the 18s window but outside the default 10s window.
        // Proves the windowSeconds parameter is respected for larger values (SMN's 18s guard).
        var svc = TimelineSvc(confidence: 1.0f, secondsUntil: 15f);

        Assert.True(BurstHoldHelper.ShouldDumpForDowntime(svc.Object, 18f));
    }

    [Fact]
    public void ShouldDumpForDowntime_ReturnsFalse_WhenWindowIsLarger_18s_ButOutsideWindow()
    {
        // Discrimination: 20s until phase is outside even the 18s window.
        var svc = TimelineSvc(confidence: 1.0f, secondsUntil: 20f);

        Assert.False(BurstHoldHelper.ShouldDumpForDowntime(svc.Object, 18f));
    }

    [Fact]
    public void ShouldDumpForDowntime_ReturnsFalse_WhenWindowIs18s_LowConfidence()
    {
        // Fail-closed: low confidence must block even with a large window and near phase.
        var svc = TimelineSvc(confidence: 0.5f, secondsUntil: 15f);

        Assert.False(BurstHoldHelper.ShouldDumpForDowntime(svc.Object, 18f));
    }
}
