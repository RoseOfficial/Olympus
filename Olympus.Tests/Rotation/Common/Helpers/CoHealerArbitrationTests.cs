using Moq;
using Olympus.Ipc;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Party;
using Xunit;

namespace Olympus.Tests.Rotation.Common.Helpers;

/// <summary>
/// Unit tests for CoHealerArbitration.ShouldDefer.
/// Tests every gate in the predicate in isolation.
/// </summary>
public class CoHealerArbitrationTests
{
    // -----------------------------------------------------------------------
    // Helper: builds a mock IPartyCoordinationService that returns the given
    // gauge state from GetFreshestRemoteHealerGauge.
    // -----------------------------------------------------------------------
    private static Mock<IPartyCoordinationService> BuildCoord(RemoteHealerGaugeState? gaugeResult)
    {
        var mock = new Mock<IPartyCoordinationService>();
        mock.Setup(x => x.GetFreshestRemoteHealerGauge(It.IsAny<float>())).Returns(gaugeResult);
        return mock;
    }

    private static RemoteHealerGaugeState MakeGauge(int primaryResource) =>
        new RemoteHealerGaugeState
        {
            InstanceId = System.Guid.NewGuid(),
            JobId = 33,
            PrimaryResource = primaryResource,
            LastUpdate = System.DateTime.UtcNow,
        };

    // -----------------------------------------------------------------------
    // Gate 1: toggle disabled → always false
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_ToggleOff_ReturnsFalse()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 3));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: false,
            coordination: coord.Object,
            myResourceCount: 0,
            overcapBiasThreshold: 2,
            targetHpPercent: 0.60f,
            hardFloor: 0.25f);

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // Gate 2: coordination null → false
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_CoordinationNull_ReturnsFalse()
    {
        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: null,
            myResourceCount: 0,
            overcapBiasThreshold: 2,
            targetHpPercent: 0.60f,
            hardFloor: 0.25f);

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // Gate 2: GetFreshestRemoteHealerGauge returns null → false
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_NoRemoteData_ReturnsFalse()
    {
        var coord = BuildCoord(gaugeResult: null);

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 0,
            overcapBiasThreshold: 2,
            targetHpPercent: 0.60f,
            hardFloor: 0.25f);

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // Gate 3: HP at hard floor → false (deadman gate)
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_HpAtHardFloor_ReturnsFalse()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 3));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 0,
            overcapBiasThreshold: 2,
            targetHpPercent: 0.25f,   // exactly at floor
            hardFloor: 0.25f);

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // Gate 3: HP below hard floor → false
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_HpBelowHardFloor_ReturnsFalse()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 3));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 0,
            overcapBiasThreshold: 2,
            targetHpPercent: 0.10f,   // well below floor
            hardFloor: 0.25f);

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // Gate 4: my resource at overcap bias → false (use case 3, spend mine)
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_MyResourceAtCap_ReturnsFalse()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 5));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 2,
            overcapBiasThreshold: 2,   // at cap
            targetHpPercent: 0.60f,
            hardFloor: 0.25f);

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // Gate 5: remote primary == my resource → false (not strictly greater)
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_RemoteEqualToMine_ReturnsFalse()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 1));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 1,
            overcapBiasThreshold: 3,
            targetHpPercent: 0.60f,
            hardFloor: 0.25f);

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // Gate 5: remote primary < my resource → false
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_RemotePoorerThanMine_ReturnsFalse()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 0));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 2,
            overcapBiasThreshold: 3,
            targetHpPercent: 0.60f,
            hardFloor: 0.25f);

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // All gates pass: remote primary > mine, HP above floor, below cap → true
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_RemoteRicher_HpAboveFloor_BelowCap_ReturnsTrue()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 3));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 1,
            overcapBiasThreshold: 3,
            targetHpPercent: 0.60f,
            hardFloor: 0.25f);

        Assert.True(result);
    }

    // -----------------------------------------------------------------------
    // All gates pass except HP is just above floor: still defers
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_HpJustAboveFloor_ReturnsTrue()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 2));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 0,
            overcapBiasThreshold: 2,
            targetHpPercent: 0.26f,   // just above 0.25 floor
            hardFloor: 0.25f);

        Assert.True(result);
    }

    // -----------------------------------------------------------------------
    // Overcap: my resource one below cap, remote richer → defers (still use case 3 boundary)
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_MyResourceOneBelowCap_RemoteRicher_ReturnsTrue()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 2));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 1,
            overcapBiasThreshold: 2,   // one below cap
            targetHpPercent: 0.60f,
            hardFloor: 0.25f);

        Assert.True(result);
    }

    // -----------------------------------------------------------------------
    // staleAgeSeconds is forwarded: GetFreshestRemoteHealerGauge called with the
    // caller-supplied value.
    // -----------------------------------------------------------------------
    [Fact]
    public void ShouldDefer_PassesStaleAgeToService()
    {
        var coord = BuildCoord(gaugeResult: null);

        CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 0,
            overcapBiasThreshold: 2,
            targetHpPercent: 0.60f,
            hardFloor: 0.25f,
            staleAgeSeconds: 5f);

        coord.Verify(x => x.GetFreshestRemoteHealerGauge(5f), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Normalization: when local and remote resources live on different scales,
    // the comparison uses proportional fullness (fraction of cap), not raw count.
    // WHM scenario: Tetragrammaton charges (cap 2) vs remote lily count (cap 3).
    // -----------------------------------------------------------------------

    /// <summary>
    /// Local = 1 of cap 2 (50% full), remote = 2 of cap 3 (67% full).
    /// Remote is proportionally richer → defers.
    /// </summary>
    [Fact]
    public void ShouldDefer_DifferentScales_RemoteProportionallyRicher_ReturnsTrue()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 2));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 1,
            overcapBiasThreshold: 2,   // Tetragrammaton cap
            targetHpPercent: 0.60f,
            hardFloor: 0.25f,
            remoteResourceCap: 3);     // lily / seal / aetherflow / addersgall cap

        Assert.True(result);
    }

    /// <summary>
    /// Local = 1 of cap 2 (50% full), remote = 1 of cap 3 (33% full).
    /// Local is proportionally richer → does not defer even though counts are equal.
    /// </summary>
    [Fact]
    public void ShouldDefer_DifferentScales_LocalProportionallyRicher_ReturnsFalse()
    {
        var coord = BuildCoord(MakeGauge(primaryResource: 1));

        var result = CoHealerArbitration.ShouldDefer(
            toggleEnabled: true,
            coordination: coord.Object,
            myResourceCount: 1,
            overcapBiasThreshold: 2,   // Tetragrammaton cap
            targetHpPercent: 0.60f,
            hardFloor: 0.25f,
            remoteResourceCap: 3);     // lily cap

        Assert.False(result);
    }
}
