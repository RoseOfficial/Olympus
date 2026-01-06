using System;

namespace Olympus.Config;

/// <summary>
/// Configuration for buff and utility oGCDs.
/// </summary>
public sealed class BuffConfig
{
    public bool EnablePresenceOfMind { get; set; } = true;
    public bool EnableThinAir { get; set; } = true;
    public bool EnableAetherialShift { get; set; } = true;

    // Predictive Lucid Dreaming Settings

    /// <summary>
    /// Enable predictive Lucid Dreaming usage based on MP exhaustion forecast.
    /// When enabled, uses Lucid when MP would drop below threshold within lookahead window.
    /// Default true enables proactive MP management.
    /// </summary>
    public bool EnablePredictiveLucid { get; set; } = true;

    /// <summary>
    /// MP threshold for predictive Lucid Dreaming trigger.
    /// Lucid will be used when MP is projected to drop below this in the lookahead window.
    /// Default 3000 ensures enough MP for emergency heals.
    /// Valid range: 1000 to 5000.
    /// </summary>
    private int _lucidPredictionThreshold = 3000;
    public int LucidPredictionThreshold
    {
        get => _lucidPredictionThreshold;
        set => _lucidPredictionThreshold = Math.Clamp(value, 1000, 5000);
    }

    /// <summary>
    /// How far ahead to look for MP exhaustion (seconds).
    /// Default 10 means trigger Lucid if MP would drop below threshold in 10 seconds.
    /// Valid range: 5 to 30.
    /// </summary>
    private float _lucidPredictionLookahead = 10f;
    public float LucidPredictionLookahead
    {
        get => _lucidPredictionLookahead;
        set => _lucidPredictionLookahead = Math.Clamp(value, 5f, 30f);
    }

    // PoM Coordination
    /// <summary>
    /// Delay Presence of Mind when a Raise is imminent.
    /// If a party member is dead and Swiftcast is coming off cooldown soon,
    /// save the spell speed buff for after the raise.
    /// </summary>
    public bool DelayPoMForRaise { get; set; } = true;

    /// <summary>
    /// Maximum Swiftcast cooldown (seconds) to delay PoM.
    /// If Swiftcast will be ready within this time, delay PoM for the raise.
    /// Default 10 seconds.
    /// </summary>
    public float PoMRaiseDelayCooldown { get; set; } = 10f;

    /// <summary>
    /// Try to stack Presence of Mind with Assize for DPS synergy.
    /// When enabled, prefers to use PoM when Assize is also ready.
    /// </summary>
    public bool StackPoMWithAssize { get; set; } = true;

    // MP Conservation

    /// <summary>
    /// Enable MP conservation mode for Thin Air.
    /// When enabled, Thin Air will be used more aggressively when MP is running low
    /// to preserve MP for emergency heals and raises.
    /// </summary>
    public bool EnableMpConservation { get; set; } = true;

    /// <summary>
    /// Enable raise preparation mode.
    /// When enabled, reserves Thin Air and prioritizes MP regeneration
    /// when a party member dies and MP is low.
    /// </summary>
    public bool EnableRaisePrepMode { get; set; } = true;

    /// <summary>
    /// MP percentage threshold to enter raise preparation mode.
    /// When MP drops below this percentage and a raise is needed,
    /// the plugin will conserve resources for the raise.
    /// Default 0.40 (40%) - enough for a raise with buffer.
    /// </summary>
    public float RaisePrepMpThreshold { get; set; } = 0.40f;
}
