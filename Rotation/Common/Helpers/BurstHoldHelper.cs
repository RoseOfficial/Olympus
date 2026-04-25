using Olympus.Services;
using Olympus.Services.Input;
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
    /// Player-intent override for all burst-window decisions. Set once at plugin
    /// startup. Static accessor used here because the override is a global player
    /// signal; threading <see cref="IModifierKeyService"/> through ~30 call sites
    /// would be churn for no architectural gain.
    /// </summary>
    public static IModifierKeyService? ModifierKeys { get; set; }

    /// <summary>
    /// True when raid buff burst window is currently active.
    /// When the burst-override key is held, returns true regardless of real state
    /// (rotation should act as if in burst). When the conservative key is held,
    /// returns false (rotation should act as if not in burst).
    /// </summary>
    public static bool IsInBurst(IBurstWindowService? burstWindowService)
    {
        if (ModifierKeys?.IsBurstOverride == true) return true;
        if (ModifierKeys?.IsConservativeOverride == true) return false;
        return burstWindowService?.IsInBurstWindow == true;
    }

    /// <summary>
    /// True when burst is imminent within <paramref name="thresholdSeconds"/> and not yet active.
    /// Use to hold cooldowns/gauge spenders until the burst window opens.
    /// When the burst-override key is held, returns false (don't hold, fire now).
    /// When the conservative key is held, returns true (hold regardless of detected burst).
    /// </summary>
    public static bool ShouldHoldForBurst(IBurstWindowService? burstWindowService, float thresholdSeconds = 8f)
    {
        if (ModifierKeys?.IsBurstOverride == true) return false;
        if (ModifierKeys?.IsConservativeOverride == true) return true;
        return burstWindowService?.IsBurstImminent(thresholdSeconds) == true &&
               burstWindowService?.IsInBurstWindow != true;
    }

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a high-confidence phase transition is expected within the window.
    /// Modifier overrides apply: burst-override forces false, conservative forces true.
    /// </summary>
    public static bool ShouldHoldForPhaseTransition(ITimelineService? timelineService, float windowSeconds = 8f)
    {
        if (ModifierKeys?.IsBurstOverride == true) return false;
        if (ModifierKeys?.IsConservativeOverride == true) return true;
        var nextPhase = timelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;
        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }
}
