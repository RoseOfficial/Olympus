using System;
using Moq;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Services.Input;
using Xunit;

namespace Olympus.Tests.Rotation.Common.Helpers;

/// <summary>
/// Verifies the modifier-override layer added on top of BurstHoldHelper.
/// Each test resets the static <see cref="BurstHoldHelper.ModifierKeys"/> in finally
/// to avoid cross-test contamination. Tests run in parallel by default in xUnit;
/// using a per-test override avoids needing a Collection fixture for serialization.
/// </summary>
public class BurstHoldHelperTests : IDisposable
{
    private readonly IModifierKeyService? originalModifierKeys;

    public BurstHoldHelperTests()
    {
        originalModifierKeys = BurstHoldHelper.ModifierKeys;
        BurstHoldHelper.ModifierKeys = null;
    }

    public void Dispose()
    {
        BurstHoldHelper.ModifierKeys = originalModifierKeys;
    }

    private static Mock<IBurstWindowService> BurstService(bool inBurst = false, bool imminent = false)
    {
        var m = new Mock<IBurstWindowService>();
        m.Setup(s => s.IsInBurstWindow).Returns(inBurst);
        m.Setup(s => s.IsBurstImminent(It.IsAny<float>())).Returns(imminent);
        return m;
    }

    private static Mock<IModifierKeyService> Modifier(bool burst = false, bool conservative = false)
    {
        var m = new Mock<IModifierKeyService>();
        m.Setup(x => x.IsBurstOverride).Returns(burst);
        m.Setup(x => x.IsConservativeOverride).Returns(conservative);
        return m;
    }

    // ---------------- IsInBurst ----------------

    [Fact]
    public void IsInBurst_NoModifier_DelegatesToService()
    {
        var svc = BurstService(inBurst: true);

        Assert.True(BurstHoldHelper.IsInBurst(svc.Object));
    }

    [Fact]
    public void IsInBurst_BurstOverride_ReturnsTrueEvenWhenServiceReportsFalse()
    {
        var svc = BurstService(inBurst: false);
        BurstHoldHelper.ModifierKeys = Modifier(burst: true).Object;

        Assert.True(BurstHoldHelper.IsInBurst(svc.Object));
    }

    [Fact]
    public void IsInBurst_ConservativeOverride_ReturnsFalseEvenWhenServiceReportsTrue()
    {
        var svc = BurstService(inBurst: true);
        BurstHoldHelper.ModifierKeys = Modifier(conservative: true).Object;

        Assert.False(BurstHoldHelper.IsInBurst(svc.Object));
    }

    // ---------------- ShouldHoldForBurst ----------------

    [Fact]
    public void ShouldHoldForBurst_BurstImminentNotInBurst_ReturnsTrue()
    {
        var svc = BurstService(inBurst: false, imminent: true);

        Assert.True(BurstHoldHelper.ShouldHoldForBurst(svc.Object));
    }

    [Fact]
    public void ShouldHoldForBurst_BurstOverride_ForcesFalse()
    {
        var svc = BurstService(inBurst: false, imminent: true);
        BurstHoldHelper.ModifierKeys = Modifier(burst: true).Object;

        Assert.False(BurstHoldHelper.ShouldHoldForBurst(svc.Object));
    }

    [Fact]
    public void ShouldHoldForBurst_ConservativeOverride_ForcesTrue()
    {
        var svc = BurstService(inBurst: false, imminent: false);
        BurstHoldHelper.ModifierKeys = Modifier(conservative: true).Object;

        Assert.True(BurstHoldHelper.ShouldHoldForBurst(svc.Object));
    }

    [Fact]
    public void ShouldHoldForBurst_AlreadyInBurst_ReturnsFalse()
    {
        var svc = BurstService(inBurst: true, imminent: true);

        Assert.False(BurstHoldHelper.ShouldHoldForBurst(svc.Object));
    }

    [Fact]
    public void ShouldHoldForBurst_NullService_ReturnsFalse()
    {
        Assert.False(BurstHoldHelper.ShouldHoldForBurst(null));
    }

    [Fact]
    public void ShouldHoldForBurst_NullServiceWithBurstOverride_ReturnsFalse()
    {
        BurstHoldHelper.ModifierKeys = Modifier(burst: true).Object;

        Assert.False(BurstHoldHelper.ShouldHoldForBurst(null));
    }

    [Fact]
    public void ShouldHoldForBurst_NullServiceWithConservativeOverride_ReturnsTrue()
    {
        BurstHoldHelper.ModifierKeys = Modifier(conservative: true).Object;

        Assert.True(BurstHoldHelper.ShouldHoldForBurst(null));
    }

    // ---------------- ShouldHoldForPhaseTransition ----------------

    [Fact]
    public void ShouldHoldForPhaseTransition_BurstOverride_ForcesFalse()
    {
        BurstHoldHelper.ModifierKeys = Modifier(burst: true).Object;

        Assert.False(BurstHoldHelper.ShouldHoldForPhaseTransition(null));
    }

    [Fact]
    public void ShouldHoldForPhaseTransition_ConservativeOverride_ForcesTrue()
    {
        BurstHoldHelper.ModifierKeys = Modifier(conservative: true).Object;

        Assert.True(BurstHoldHelper.ShouldHoldForPhaseTransition(null));
    }

    [Fact]
    public void ShouldHoldForPhaseTransition_NoModifierAndNoTimeline_ReturnsFalse()
    {
        Assert.False(BurstHoldHelper.ShouldHoldForPhaseTransition(null));
    }
}
