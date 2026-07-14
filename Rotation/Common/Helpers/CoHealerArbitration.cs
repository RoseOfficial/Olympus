using Olympus.Services.Party;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Shared arbitration predicate for single-target emergency oGCD heal deferral.
///
/// When two Olympus healers are in the same party, both evaluate their own
/// emergency-heal candidates independently each frame. This predicate implements
/// "let the richer healer take it" — if the co-healer has more of their primary
/// emergency resource than we do, defer for one evaluation cycle (~200ms) and let
/// them act first. We still fire at the hard floor regardless (deadman gate).
///
/// Use cases satisfied:
///   1. Emergency-resource arbitration: the healer with MORE spare resource answers.
///   3. Overcap bias: when my resource is near cap, spend mine rather than deferring.
/// </summary>
public static class CoHealerArbitration
{
    /// <summary>
    /// Returns true when the local healer should skip pushing a single-target
    /// emergency oGCD this frame, deferring to the co-healer who has more resource.
    ///
    /// All five gates must hold simultaneously for deferral:
    ///   1. The feature toggle is enabled.
    ///   2. A fresh co-healer gauge snapshot exists (age ≤ staleAgeSeconds).
    ///   3. The target's HP is above the hard floor (not a true emergency).
    ///   4. The local resource count is below the overcap bias threshold.
    ///   5. The co-healer's primary resource fraction (PrimaryResource / remoteResourceCap)
    ///      is strictly greater than the local resource fraction (myResourceCount / overcapBiasThreshold).
    /// </summary>
    /// <param name="toggleEnabled">
    ///   Whether healer resource arbitration is enabled
    ///   (<c>Configuration.PartyCoordination.EnableHealerResourceArbitration</c>).
    /// </param>
    /// <param name="coordination">
    ///   The party coordination service (may be null when coordination is disabled).
    /// </param>
    /// <param name="myResourceCount">
    ///   Local emergency resource count: charge count for charge-based abilities
    ///   (Tetragrammaton, Essential Dignity) or stack count for resource abilities
    ///   (Aetherflow, Addersgall).
    /// </param>
    /// <param name="overcapBiasThreshold">
    ///   When <paramref name="myResourceCount"/> is at or above this value (typically
    ///   the resource cap), the local healer spends rather than deferring — prevents
    ///   overcap waste. Use the action's max charges or the gauge's max stacks.
    /// </param>
    /// <param name="targetHpPercent">
    ///   Current HP fraction of the heal target (0–1). Values at or below
    ///   <paramref name="hardFloor"/> bypass deferral unconditionally.
    /// </param>
    /// <param name="hardFloor">
    ///   HP fraction below which deferral is always suppressed. Set to the
    ///   handler's emergency threshold minus a safety margin (typically 0.25).
    /// </param>
    /// <param name="staleAgeSeconds">
    ///   Maximum age for the remote gauge snapshot to be considered valid.
    ///   Defaults to 3 seconds to match the design-doc recommendation.
    /// </param>
    /// <param name="remoteResourceCap">
    ///   The maximum value of the remote healer's primary resource (denominator for
    ///   the remote fraction). Defaults to 3 because all healer gauge primaries use
    ///   a 0-3 scale (lily count, aetherflow stacks, addersgall stacks, seal count).
    ///   Pass a different value only when the remote broadcast uses a different scale.
    /// </param>
    public static bool ShouldDefer(
        bool toggleEnabled,
        IPartyCoordinationService? coordination,
        int myResourceCount,
        int overcapBiasThreshold,
        float targetHpPercent,
        float hardFloor,
        float staleAgeSeconds = 3f,
        int remoteResourceCap = 3)
    {
        if (!toggleEnabled)
            return false;

        if (coordination == null)
            return false;

        var remote = coordination.GetFreshestRemoteHealerGauge(staleAgeSeconds);
        if (remote == null)
            return false;

        if (targetHpPercent <= hardFloor)
            return false;

        if (myResourceCount >= overcapBiasThreshold)
            return false;

        // Normalize both sides to a 0..1 fraction of their respective caps so
        // the comparison is like-for-like. Without normalization, charge-based
        // abilities (Tetragrammaton 0-2, Essential Dignity 0-2) would be compared
        // directly against gauge primaries (lily count 0-3, seal count 0-3),
        // making "2 lilies vs 1 Tetra charge" look like "richer remote" even when
        // proportional fullness would not confirm that.
        var localFraction = overcapBiasThreshold > 0 ? (float)myResourceCount / overcapBiasThreshold : 0f;
        var remoteFraction = remoteResourceCap > 0 ? (float)remote.PrimaryResource / remoteResourceCap : 0f;
        return remoteFraction > localFraction;
    }
}
