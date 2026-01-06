namespace Olympus.Config;

/// <summary>
/// Configuration for buff and utility oGCDs.
/// </summary>
public sealed class BuffConfig
{
    public bool EnablePresenceOfMind { get; set; } = true;
    public bool EnableThinAir { get; set; } = true;
    public bool EnableAetherialShift { get; set; } = true;

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
}
