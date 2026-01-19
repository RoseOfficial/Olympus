using System;

namespace Olympus.Config;

/// <summary>
/// Configuration options for multi-Olympus party coordination via IPC.
/// Enables heal overlap prevention and cooldown coordination between party members.
/// </summary>
public sealed class PartyCoordinationConfig
{
    /// <summary>
    /// Enable party coordination via IPC.
    /// When enabled, Olympus instances in the same party will communicate
    /// to prevent heal overlap and coordinate cooldown usage.
    /// Default false (opt-in feature).
    /// </summary>
    public bool EnablePartyCoordination { get; set; } = false;

    /// <summary>
    /// Interval between heartbeat broadcasts (milliseconds).
    /// Heartbeats announce presence to other Olympus instances.
    /// Lower values = faster detection, higher overhead.
    /// Valid range: 500 to 5000.
    /// </summary>
    private int _heartbeatIntervalMs = 1000;
    public int HeartbeatIntervalMs
    {
        get => _heartbeatIntervalMs;
        set => _heartbeatIntervalMs = Math.Clamp(value, 500, 5000);
    }

    /// <summary>
    /// Timeout before considering an instance disconnected (milliseconds).
    /// If no heartbeat received within this window, instance is removed.
    /// Should be at least 2x HeartbeatIntervalMs for reliability.
    /// Valid range: 2000 to 15000.
    /// </summary>
    private int _instanceTimeoutMs = 5000;
    public int InstanceTimeoutMs
    {
        get => _instanceTimeoutMs;
        set => _instanceTimeoutMs = Math.Clamp(value, 2000, 15000);
    }

    /// <summary>
    /// How long heal reservations remain valid (milliseconds).
    /// After this time, a reservation expires if not fulfilled.
    /// Should be long enough for cast + travel time.
    /// Valid range: 1000 to 5000.
    /// </summary>
    private int _healReservationExpiryMs = 3000;
    public int HealReservationExpiryMs
    {
        get => _healReservationExpiryMs;
        set => _healReservationExpiryMs = Math.Clamp(value, 1000, 5000);
    }

    /// <summary>
    /// Broadcast major cooldown usage to other instances.
    /// Allows coordination of abilities like Temperance, Liturgy of the Bell, etc.
    /// </summary>
    public bool BroadcastMajorCooldowns { get; set; } = true;

    /// <summary>
    /// Log coordination events for debugging.
    /// Only enable when troubleshooting coordination issues.
    /// </summary>
    public bool LogCoordinationEvents { get; set; } = false;

    /// <summary>
    /// Minimum estimated heal amount to broadcast an intent.
    /// Prevents broadcasting for trivial heals that don't matter.
    /// Set to 0 to broadcast all heals.
    /// Valid range: 0 to 10000.
    /// </summary>
    private int _minHealAmountToBroadcast = 1000;
    public int MinHealAmountToBroadcast
    {
        get => _minHealAmountToBroadcast;
        set => _minHealAmountToBroadcast = Math.Clamp(value, 0, 10000);
    }
}
