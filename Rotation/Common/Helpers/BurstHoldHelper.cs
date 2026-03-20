using Olympus.Services;
using Olympus.Timeline;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Shared burst window helpers for BuffModules.
/// Centralizes the ShouldHoldForBurst / IsInBurst logic so that all 21 rotation
/// BuffModules use an identical implementation instead of private copy-pasted methods.
/// </summary>
public static class BurstHoldHelper
{
    /// <summary>
    /// True when raid buff burst window is currently active.
    /// Returns false when the service is null (burst tracking unavailable).
    /// </summary>
    public static bool IsInBurst(IBurstWindowService? burstWindowService) =>
        burstWindowService?.IsInBurstWindow == true;

    /// <summary>
    /// True when burst is imminent within <paramref name="thresholdSeconds"/> and not yet active.
    /// Use to hold cooldowns/gauge spenders until the burst window opens.
    /// Returns false when the service is null (burst tracking unavailable).
    /// </summary>
    public static bool ShouldHoldForBurst(IBurstWindowService? burstWindowService, float thresholdSeconds = 8f) =>
        burstWindowService?.IsBurstImminent(thresholdSeconds) == true &&
        burstWindowService?.IsInBurstWindow != true;

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a high-confidence phase transition is expected within the window.
    /// </summary>
    public static bool ShouldHoldForPhaseTransition(ITimelineService? timelineService, float windowSeconds = 8f)
    {
        var nextPhase = timelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;
        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }
}
