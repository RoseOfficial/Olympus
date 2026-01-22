using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Olympus.Ipc;

/// <summary>
/// Message types for party coordination IPC protocol.
/// </summary>
public enum PartyMessageType
{
    /// <summary>Periodic alive signal with instance info.</summary>
    Heartbeat = 0,

    /// <summary>Announce intent to heal a target.</summary>
    HealIntent = 1,

    /// <summary>Announce that a heal has landed.</summary>
    HealLanded = 2,

    /// <summary>Announce major cooldown usage.</summary>
    CooldownUsed = 3,

    /// <summary>Announce intent to cast an AoE heal.</summary>
    AoEHealIntent = 4,

    /// <summary>Announce intent to use a raid buff (for DPS coordination).</summary>
    RaidBuffIntent = 5,

    /// <summary>Announce start of a burst window (when raid buff is activated).</summary>
    BurstWindowStart = 6,
}

/// <summary>
/// Base class for all party coordination messages.
/// </summary>
public abstract class PartyMessage
{
    /// <summary>Unique identifier for this Olympus instance (stable across frames).</summary>
    [JsonPropertyName("id")]
    public Guid InstanceId { get; set; }

    /// <summary>UTC timestamp when message was created.</summary>
    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }

    /// <summary>Type discriminator for deserialization.</summary>
    [JsonPropertyName("type")]
    public PartyMessageType MessageType { get; set; }

    protected PartyMessage(PartyMessageType type)
    {
        InstanceId = Guid.Empty;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        MessageType = type;
    }

    /// <summary>
    /// Serializes the message to JSON.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, GetType(), PartyMessageJsonContext.Options);
    }

    /// <summary>
    /// Deserializes a message from JSON.
    /// Returns null if deserialization fails.
    /// </summary>
    public static PartyMessage? FromJson(string json)
    {
        try
        {
            // First parse to get the type
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("type", out var typeElement))
                return null;

            var type = (PartyMessageType)typeElement.GetInt32();
            return type switch
            {
                PartyMessageType.Heartbeat => JsonSerializer.Deserialize<HeartbeatMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.HealIntent => JsonSerializer.Deserialize<HealIntentMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.HealLanded => JsonSerializer.Deserialize<HealLandedMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.CooldownUsed => JsonSerializer.Deserialize<CooldownUsedMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.AoEHealIntent => JsonSerializer.Deserialize<AoEHealIntentMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.RaidBuffIntent => JsonSerializer.Deserialize<RaidBuffIntentMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.BurstWindowStart => JsonSerializer.Deserialize<BurstWindowStartMessage>(json, PartyMessageJsonContext.Options),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Periodic heartbeat message announcing instance presence.
/// </summary>
public sealed class HeartbeatMessage : PartyMessage
{
    /// <summary>Job ID of the player running this instance.</summary>
    [JsonPropertyName("job")]
    public uint JobId { get; set; }

    /// <summary>Entity ID of the local player.</summary>
    [JsonPropertyName("eid")]
    public uint PlayerEntityId { get; set; }

    /// <summary>Whether Olympus is currently enabled and active.</summary>
    [JsonPropertyName("on")]
    public bool IsEnabled { get; set; }

    public HeartbeatMessage() : base(PartyMessageType.Heartbeat) { }

    public HeartbeatMessage(Guid instanceId, uint jobId, uint playerEntityId, bool isEnabled)
        : base(PartyMessageType.Heartbeat)
    {
        InstanceId = instanceId;
        JobId = jobId;
        PlayerEntityId = playerEntityId;
        IsEnabled = isEnabled;
    }
}

/// <summary>
/// Message announcing intent to heal a target.
/// Used to reserve targets and prevent double-healing.
/// </summary>
public sealed class HealIntentMessage : PartyMessage
{
    /// <summary>Entity ID of the heal target.</summary>
    [JsonPropertyName("tid")]
    public uint TargetEntityId { get; set; }

    /// <summary>Estimated heal amount (before crit/DH variance).</summary>
    [JsonPropertyName("amt")]
    public int EstimatedHealAmount { get; set; }

    /// <summary>Action ID being cast.</summary>
    [JsonPropertyName("act")]
    public uint ActionId { get; set; }

    /// <summary>Cast time in milliseconds (0 for instant).</summary>
    [JsonPropertyName("cast")]
    public int CastTimeMs { get; set; }

    public HealIntentMessage() : base(PartyMessageType.HealIntent) { }

    public HealIntentMessage(Guid instanceId, uint targetEntityId, int estimatedHealAmount, uint actionId, int castTimeMs)
        : base(PartyMessageType.HealIntent)
    {
        InstanceId = instanceId;
        TargetEntityId = targetEntityId;
        EstimatedHealAmount = estimatedHealAmount;
        ActionId = actionId;
        CastTimeMs = castTimeMs;
    }
}

/// <summary>
/// Message announcing that a heal has landed on a target.
/// Used to clear reservations and track actual healing.
/// </summary>
public sealed class HealLandedMessage : PartyMessage
{
    /// <summary>Entity ID of the healed target.</summary>
    [JsonPropertyName("tid")]
    public uint TargetEntityId { get; set; }

    /// <summary>Actual heal amount that landed.</summary>
    [JsonPropertyName("amt")]
    public int ActualHealAmount { get; set; }

    /// <summary>Action ID that was used.</summary>
    [JsonPropertyName("act")]
    public uint ActionId { get; set; }

    public HealLandedMessage() : base(PartyMessageType.HealLanded) { }

    public HealLandedMessage(Guid instanceId, uint targetEntityId, int actualHealAmount, uint actionId)
        : base(PartyMessageType.HealLanded)
    {
        InstanceId = instanceId;
        TargetEntityId = targetEntityId;
        ActualHealAmount = actualHealAmount;
        ActionId = actionId;
    }
}

/// <summary>
/// Message announcing major cooldown usage.
/// Used to coordinate abilities like Temperance, Liturgy of the Bell, etc.
/// </summary>
public sealed class CooldownUsedMessage : PartyMessage
{
    /// <summary>Action ID of the cooldown used.</summary>
    [JsonPropertyName("act")]
    public uint ActionId { get; set; }

    /// <summary>Recast time in milliseconds until the cooldown is available again.</summary>
    [JsonPropertyName("cd")]
    public int RecastTimeMs { get; set; }

    public CooldownUsedMessage() : base(PartyMessageType.CooldownUsed) { }

    public CooldownUsedMessage(Guid instanceId, uint actionId, int recastTimeMs)
        : base(PartyMessageType.CooldownUsed)
    {
        InstanceId = instanceId;
        ActionId = actionId;
        RecastTimeMs = recastTimeMs;
    }
}

/// <summary>
/// Message announcing intent to cast an AoE heal.
/// Used to reserve the entire party and prevent multiple healers from casting AoE heals simultaneously.
/// </summary>
public sealed class AoEHealIntentMessage : PartyMessage
{
    /// <summary>Action ID of the AoE heal being cast.</summary>
    [JsonPropertyName("act")]
    public uint ActionId { get; set; }

    /// <summary>Heal potency of the AoE heal.</summary>
    [JsonPropertyName("pot")]
    public int HealPotency { get; set; }

    /// <summary>Cast time in milliseconds (0 for instant).</summary>
    [JsonPropertyName("cast")]
    public int CastTimeMs { get; set; }

    public AoEHealIntentMessage() : base(PartyMessageType.AoEHealIntent) { }

    public AoEHealIntentMessage(Guid instanceId, uint actionId, int healPotency, int castTimeMs)
        : base(PartyMessageType.AoEHealIntent)
    {
        InstanceId = instanceId;
        ActionId = actionId;
        HealPotency = healPotency;
        CastTimeMs = castTimeMs;
    }
}

/// <summary>
/// JSON serialization context for party messages.
/// </summary>
internal static class PartyMessageJsonContext
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };
}

/// <summary>
/// Represents a heal reservation from a remote instance.
/// </summary>
public sealed class HealReservation
{
    /// <summary>Instance that made the reservation.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Target entity ID.</summary>
    public uint TargetEntityId { get; init; }

    /// <summary>Estimated heal amount.</summary>
    public int EstimatedHealAmount { get; init; }

    /// <summary>Action being cast.</summary>
    public uint ActionId { get; init; }

    /// <summary>When the reservation was made.</summary>
    public DateTime ReservedAt { get; init; }

    /// <summary>Expected cast completion time.</summary>
    public DateTime ExpectedLandingTime { get; init; }
}

/// <summary>
/// Represents a known remote Olympus instance.
/// </summary>
public sealed class RemoteOlympusInstance
{
    /// <summary>Unique instance identifier.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Job ID of the remote player.</summary>
    public uint JobId { get; set; }

    /// <summary>Entity ID of the remote player.</summary>
    public uint PlayerEntityId { get; set; }

    /// <summary>Whether the remote instance is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Last heartbeat received.</summary>
    public DateTime LastHeartbeat { get; set; }
}

/// <summary>
/// Tracks a defensive cooldown used by a remote Olympus instance.
/// Used to coordinate party mitigation and prevent stacking.
/// </summary>
public sealed class RemoteCooldownInfo
{
    /// <summary>Instance that used this cooldown.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Action ID of the cooldown.</summary>
    public uint ActionId { get; init; }

    /// <summary>When the cooldown was used (UTC).</summary>
    public DateTime UsedAt { get; init; }

    /// <summary>Recast time in milliseconds.</summary>
    public int RecastTimeMs { get; init; }

    /// <summary>
    /// Remaining seconds until this cooldown is available again.
    /// Returns 0 if the cooldown has expired.
    /// </summary>
    public float RemainingSeconds
    {
        get
        {
            var elapsed = (float)(DateTime.UtcNow - UsedAt).TotalSeconds;
            var total = RecastTimeMs / 1000f;
            return Math.Max(0, total - elapsed);
        }
    }

    /// <summary>
    /// Whether this cooldown is still on recast (not available yet).
    /// </summary>
    public bool IsOnCooldown => RemainingSeconds > 0;

    /// <summary>
    /// Seconds since this cooldown was used.
    /// Useful for checking if a mitigation was used "recently".
    /// </summary>
    public float SecondsSinceUsed => (float)(DateTime.UtcNow - UsedAt).TotalSeconds;
}

/// <summary>
/// Represents an AoE heal reservation from a remote instance.
/// Used to prevent multiple healers from casting party-wide heals simultaneously.
/// </summary>
public sealed class AoEHealReservation
{
    /// <summary>Instance that made the reservation.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Action ID of the AoE heal.</summary>
    public uint ActionId { get; init; }

    /// <summary>Heal potency of the AoE heal.</summary>
    public int HealPotency { get; init; }

    /// <summary>When the reservation was made.</summary>
    public DateTime ReservedAt { get; init; }

    /// <summary>When the reservation expires.</summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Whether this reservation has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// Message announcing intent to use a raid buff.
/// Used to coordinate DPS burst windows between multiple Olympus instances.
/// Unlike defensive cooldowns (which should be staggered), raid buffs benefit from synchronization.
/// </summary>
public sealed class RaidBuffIntentMessage : PartyMessage
{
    /// <summary>Action ID of the raid buff being activated.</summary>
    [JsonPropertyName("act")]
    public uint ActionId { get; set; }

    /// <summary>
    /// Seconds until the buff will be activated.
    /// 0 means immediate activation, positive values indicate delayed activation.
    /// </summary>
    [JsonPropertyName("delay")]
    public float SecondsUntilActivation { get; set; }

    /// <summary>Duration of the buff in seconds.</summary>
    [JsonPropertyName("dur")]
    public float BuffDuration { get; set; }

    public RaidBuffIntentMessage() : base(PartyMessageType.RaidBuffIntent) { }

    public RaidBuffIntentMessage(Guid instanceId, uint actionId, float secondsUntilActivation, float buffDuration)
        : base(PartyMessageType.RaidBuffIntent)
    {
        InstanceId = instanceId;
        ActionId = actionId;
        SecondsUntilActivation = secondsUntilActivation;
        BuffDuration = buffDuration;
    }
}

/// <summary>
/// Message announcing the start of a burst window.
/// Sent when a raid buff is actually activated to signal other instances to align their buffs.
/// </summary>
public sealed class BurstWindowStartMessage : PartyMessage
{
    /// <summary>Action ID of the buff that triggered the burst window.</summary>
    [JsonPropertyName("act")]
    public uint TriggerActionId { get; set; }

    /// <summary>Duration of the burst window in seconds.</summary>
    [JsonPropertyName("dur")]
    public float WindowDuration { get; set; }

    /// <summary>
    /// Whether this is a major burst window (2-minute cooldowns).
    /// Minor bursts are 60s cooldowns like Lance Charge.
    /// </summary>
    [JsonPropertyName("major")]
    public bool IsMajorBurst { get; set; }

    public BurstWindowStartMessage() : base(PartyMessageType.BurstWindowStart) { }

    public BurstWindowStartMessage(Guid instanceId, uint triggerActionId, float windowDuration, bool isMajorBurst)
        : base(PartyMessageType.BurstWindowStart)
    {
        InstanceId = instanceId;
        TriggerActionId = triggerActionId;
        WindowDuration = windowDuration;
        IsMajorBurst = isMajorBurst;
    }
}

/// <summary>
/// Represents the current burst window state for healer consumption.
/// Healers can query this to optimize shield timing, oGCD holds, and defensive cooldown decisions.
/// </summary>
public readonly struct BurstWindowState
{
    /// <summary>Whether a burst window is currently active (raid buffs are up).</summary>
    public bool IsActive { get; init; }

    /// <summary>Whether a burst is imminent (intent announced but not yet active).</summary>
    public bool IsImminent { get; init; }

    /// <summary>Seconds until the next burst window starts. 0 if active, -1 if unknown.</summary>
    public float SecondsUntilBurst { get; init; }

    /// <summary>Seconds remaining in the current burst window. 0 if not in burst.</summary>
    public float SecondsRemaining { get; init; }

    /// <summary>Number of DPS players with pending burst intents.</summary>
    public int PendingBurstCount { get; init; }

    /// <summary>Whether we have burst info from any remote DPS instances.</summary>
    public bool HasBurstInfo { get; init; }

    /// <summary>
    /// Creates a default "no burst info" state.
    /// </summary>
    public static BurstWindowState NoInfo => new()
    {
        IsActive = false,
        IsImminent = false,
        SecondsUntilBurst = -1f,
        SecondsRemaining = 0f,
        PendingBurstCount = 0,
        HasBurstInfo = false
    };
}

/// <summary>
/// Tracks the state of a remote DPS player's raid buffs.
/// Used to coordinate burst windows between multiple Olympus instances.
/// </summary>
public sealed class RemoteRaidBuffState
{
    /// <summary>Instance that owns this state.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Action ID of the raid buff.</summary>
    public uint ActionId { get; init; }

    /// <summary>When the intent was announced (UTC).</summary>
    public DateTime IntentAnnouncedAt { get; init; }

    /// <summary>Seconds until activation was planned.</summary>
    public float PlannedDelaySeconds { get; init; }

    /// <summary>When the buff was actually activated (UTC). Null if only intent was announced.</summary>
    public DateTime? ActivatedAt { get; set; }

    /// <summary>Duration of the buff in seconds.</summary>
    public float BuffDuration { get; init; }

    /// <summary>Recast time in milliseconds.</summary>
    public int RecastTimeMs { get; init; }

    /// <summary>
    /// Whether this is just an intent (not yet activated).
    /// </summary>
    public bool IsIntentOnly => ActivatedAt == null;

    /// <summary>
    /// Whether the buff is currently active.
    /// </summary>
    public bool IsBuffActive
    {
        get
        {
            if (ActivatedAt == null)
                return false;

            var elapsed = (DateTime.UtcNow - ActivatedAt.Value).TotalSeconds;
            return elapsed < BuffDuration;
        }
    }

    /// <summary>
    /// Remaining seconds of buff duration.
    /// Returns 0 if buff is not active.
    /// </summary>
    public float BuffRemainingSeconds
    {
        get
        {
            if (ActivatedAt == null)
                return 0;

            var elapsed = (float)(DateTime.UtcNow - ActivatedAt.Value).TotalSeconds;
            return Math.Max(0, BuffDuration - elapsed);
        }
    }

    /// <summary>
    /// Remaining seconds until the cooldown is available again.
    /// Based on activation time if activated, or intent time if still pending.
    /// </summary>
    public float CooldownRemainingSeconds
    {
        get
        {
            var baseTime = ActivatedAt ?? IntentAnnouncedAt.AddSeconds(PlannedDelaySeconds);
            var elapsed = (float)(DateTime.UtcNow - baseTime).TotalSeconds;
            var total = RecastTimeMs / 1000f;
            return Math.Max(0, total - elapsed);
        }
    }

    /// <summary>
    /// Whether this intent has expired (activation window passed without activation).
    /// Intent expires 5 seconds after planned activation time.
    /// </summary>
    public bool IsIntentExpired
    {
        get
        {
            if (!IsIntentOnly)
                return false;

            var expectedActivation = IntentAnnouncedAt.AddSeconds(PlannedDelaySeconds);
            return DateTime.UtcNow > expectedActivation.AddSeconds(5);
        }
    }
}
