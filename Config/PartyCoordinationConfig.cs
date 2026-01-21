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
    /// Enable cooldown coordination with other Olympus instances.
    /// When enabled, defensive cooldowns will be checked against remote instances
    /// to prevent stacking party mitigations.
    /// </summary>
    public bool EnableCooldownCoordination { get; set; } = true;

    /// <summary>
    /// Enable AoE heal coordination with other Olympus instances.
    /// When enabled, party-wide heals will be coordinated to prevent
    /// multiple healers from casting AoE heals simultaneously.
    /// </summary>
    public bool EnableAoEHealCoordination { get; set; } = true;

    /// <summary>
    /// How long AoE heal reservations remain valid (milliseconds).
    /// After this time, a reservation expires if not fulfilled.
    /// Should be long enough for cast + application time.
    /// Valid range: 1500 to 5000.
    /// </summary>
    private int _aoEHealReservationExpiryMs = 2500;
    public int AoEHealReservationExpiryMs
    {
        get => _aoEHealReservationExpiryMs;
        set => _aoEHealReservationExpiryMs = Math.Clamp(value, 1500, 5000);
    }

    /// <summary>
    /// Time window (in seconds) to skip using party mitigation if another instance
    /// recently used one. Prevents wasteful cooldown stacking.
    /// Valid range: 1.0 to 10.0 seconds.
    /// </summary>
    private float _cooldownOverlapWindowSeconds = 3.0f;
    public float CooldownOverlapWindowSeconds
    {
        get => _cooldownOverlapWindowSeconds;
        set => _cooldownOverlapWindowSeconds = Math.Clamp(value, 1.0f, 10.0f);
    }

    /// <summary>
    /// Log cooldown coordination decisions for debugging.
    /// Shows when actions are skipped due to remote cooldown usage.
    /// </summary>
    public bool LogCooldownCoordination { get; set; } = false;

    /// <summary>
    /// Log coordination events for debugging.
    /// Only enable when troubleshooting coordination issues.
    /// </summary>
    public bool LogCoordinationEvents { get; set; } = false;

    #region Raid Buff Coordination

    /// <summary>
    /// Enable raid buff coordination with other Olympus instances.
    /// When enabled, DPS raid buffs (Battle Litany, Battle Voice, Radiant Finale)
    /// will be synchronized across party members for maximum burst damage.
    /// Unlike defensive mitigations (which are staggered), raid buffs benefit from synchronization.
    /// </summary>
    public bool EnableRaidBuffCoordination { get; set; } = true;

    /// <summary>
    /// Time window (in seconds) to consider buffs as aligned for burst coordination.
    /// If another instance is about to use a raid buff within this window, align with them.
    /// Valid range: 1.0 to 10.0 seconds.
    /// </summary>
    private float _raidBuffAlignmentWindowSeconds = 3.0f;
    public float RaidBuffAlignmentWindowSeconds
    {
        get => _raidBuffAlignmentWindowSeconds;
        set => _raidBuffAlignmentWindowSeconds = Math.Clamp(value, 1.0f, 10.0f);
    }

    /// <summary>
    /// Maximum desync time (in seconds) before using buffs independently.
    /// If buffs are desynchronized by more than this amount (e.g., due to death),
    /// stop trying to align and use buffs independently until they naturally realign.
    /// Valid range: 10.0 to 60.0 seconds.
    /// </summary>
    private float _maxBuffDesyncSeconds = 30.0f;
    public float MaxBuffDesyncSeconds
    {
        get => _maxBuffDesyncSeconds;
        set => _maxBuffDesyncSeconds = Math.Clamp(value, 10.0f, 60.0f);
    }

    /// <summary>
    /// Log raid buff coordination decisions for debugging.
    /// Shows when buffs are aligned, delayed, or used independently.
    /// </summary>
    public bool LogRaidBuffCoordination { get; set; } = false;

    #endregion

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
