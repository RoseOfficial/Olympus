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

    /// <summary>Broadcast healer gauge state (Lily, Aetherflow, Addersgall counts).</summary>
    GaugeState = 7,

    /// <summary>Declare healer role (primary/secondary).</summary>
    RoleDeclaration = 8,

    /// <summary>Announce ground-targeted effect placement (Asylum, Sacred Soil, etc.).</summary>
    GroundEffectPlaced = 9,

    /// <summary>Announce intent to raise a dead party member.</summary>
    RaiseIntent = 10,

    /// <summary>Announce intent to cleanse a debuff from a party member.</summary>
    CleanseIntent = 11,

    /// <summary>Announce intent to interrupt an enemy cast.</summary>
    InterruptIntent = 12,

    /// <summary>Announce intent to perform a tank swap (Provoke or Shirk).</summary>
    TankSwapIntent = 13,
}

/// <summary>
/// Base class for all party coordination messages.
/// </summary>
public abstract class PartyMessage
{
    /// <summary>Current protocol version. Increment when the message schema changes incompatibly.</summary>
    public const int CurrentProtocolVersion = 1;

    /// <summary>Unique identifier for this Olympus instance (stable across frames).</summary>
    [JsonPropertyName("id")]
    public Guid InstanceId { get; set; }

    /// <summary>UTC timestamp when message was created.</summary>
    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }

    /// <summary>Type discriminator for deserialization.</summary>
    [JsonPropertyName("type")]
    public PartyMessageType MessageType { get; set; }

    /// <summary>Protocol version for schema compatibility checking.</summary>
    [JsonPropertyName("ver")]
    public int ProtocolVersion { get; set; }

    protected PartyMessage(PartyMessageType type)
    {
        InstanceId = Guid.Empty;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        MessageType = type;
        ProtocolVersion = CurrentProtocolVersion;
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
            // First parse to get the type and protocol version
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("type", out var typeElement))
                return null;

            // Reject messages from incompatible schema versions
            if (doc.RootElement.TryGetProperty("ver", out var verElement) &&
                verElement.GetInt32() != CurrentProtocolVersion)
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
                PartyMessageType.GaugeState => JsonSerializer.Deserialize<GaugeStateMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.RoleDeclaration => JsonSerializer.Deserialize<RoleDeclarationMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.GroundEffectPlaced => JsonSerializer.Deserialize<GroundEffectPlacedMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.RaiseIntent => JsonSerializer.Deserialize<RaiseIntentMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.CleanseIntent => JsonSerializer.Deserialize<CleanseIntentMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.InterruptIntent => JsonSerializer.Deserialize<InterruptIntentMessage>(json, PartyMessageJsonContext.Options),
                PartyMessageType.TankSwapIntent => JsonSerializer.Deserialize<TankSwapIntentMessage>(json, PartyMessageJsonContext.Options),
                _ => null,
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

/// <summary>
/// Healer role designation for multi-healer coordination.
/// </summary>
public enum HealerRole
{
    /// <summary>Auto-detect based on job priority.</summary>
    Auto = 0,

    /// <summary>Primary healer - takes lead on healing, higher thresholds.</summary>
    Primary = 1,

    /// <summary>Secondary healer - assists, lower thresholds, more DPS focus.</summary>
    Secondary = 2,
}

/// <summary>
/// Message broadcasting healer gauge state.
/// Used for resource-aware healing decisions.
/// </summary>
public sealed class GaugeStateMessage : PartyMessage
{
    /// <summary>Job ID of the healer.</summary>
    [JsonPropertyName("job")]
    public uint JobId { get; set; }

    /// <summary>Primary resource count (Lily for WHM, Aetherflow for SCH, etc.).</summary>
    [JsonPropertyName("r1")]
    public int PrimaryResource { get; set; }

    /// <summary>Secondary resource count (Blood Lily progress for WHM, Fairy Gauge for SCH, etc.).</summary>
    [JsonPropertyName("r2")]
    public int SecondaryResource { get; set; }

    /// <summary>Tertiary resource count (cards in hand for AST, Addersting for SGE, etc.).</summary>
    [JsonPropertyName("r3")]
    public int TertiaryResource { get; set; }

    public GaugeStateMessage() : base(PartyMessageType.GaugeState) { }

    public GaugeStateMessage(Guid instanceId, uint jobId, int primary, int secondary, int tertiary)
        : base(PartyMessageType.GaugeState)
    {
        InstanceId = instanceId;
        JobId = jobId;
        PrimaryResource = primary;
        SecondaryResource = secondary;
        TertiaryResource = tertiary;
    }
}

/// <summary>
/// Message declaring healer role in the party.
/// </summary>
public sealed class RoleDeclarationMessage : PartyMessage
{
    /// <summary>Job ID of the healer.</summary>
    [JsonPropertyName("job")]
    public uint JobId { get; set; }

    /// <summary>Declared healer role.</summary>
    [JsonPropertyName("role")]
    public HealerRole Role { get; set; }

    /// <summary>Job priority for auto-detection (lower = higher priority).</summary>
    [JsonPropertyName("pri")]
    public int JobPriority { get; set; }

    public RoleDeclarationMessage() : base(PartyMessageType.RoleDeclaration) { }

    public RoleDeclarationMessage(Guid instanceId, uint jobId, HealerRole role, int jobPriority)
        : base(PartyMessageType.RoleDeclaration)
    {
        InstanceId = instanceId;
        JobId = jobId;
        Role = role;
        JobPriority = jobPriority;
    }
}

/// <summary>
/// Message announcing ground-targeted effect placement.
/// Used to prevent overlapping healing zones (Asylum, Sacred Soil, etc.).
/// </summary>
public sealed class GroundEffectPlacedMessage : PartyMessage
{
    /// <summary>Action ID of the ground effect.</summary>
    [JsonPropertyName("act")]
    public uint ActionId { get; set; }

    /// <summary>X position where effect was placed.</summary>
    [JsonPropertyName("x")]
    public float PositionX { get; set; }

    /// <summary>Y position where effect was placed.</summary>
    [JsonPropertyName("y")]
    public float PositionY { get; set; }

    /// <summary>Z position where effect was placed.</summary>
    [JsonPropertyName("z")]
    public float PositionZ { get; set; }

    /// <summary>Radius of the ground effect in yalms.</summary>
    [JsonPropertyName("rad")]
    public float Radius { get; set; }

    /// <summary>Duration of the effect in seconds.</summary>
    [JsonPropertyName("dur")]
    public float Duration { get; set; }

    public GroundEffectPlacedMessage() : base(PartyMessageType.GroundEffectPlaced) { }

    public GroundEffectPlacedMessage(Guid instanceId, uint actionId, float x, float y, float z, float radius, float duration)
        : base(PartyMessageType.GroundEffectPlaced)
    {
        InstanceId = instanceId;
        ActionId = actionId;
        PositionX = x;
        PositionY = y;
        PositionZ = z;
        Radius = radius;
        Duration = duration;
    }
}

/// <summary>
/// Tracks gauge state from a remote healer instance.
/// </summary>
public sealed class RemoteHealerGaugeState
{
    /// <summary>Instance that owns this state.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Job ID of the healer.</summary>
    public uint JobId { get; init; }

    /// <summary>Primary resource count.</summary>
    public int PrimaryResource { get; set; }

    /// <summary>Secondary resource count.</summary>
    public int SecondaryResource { get; set; }

    /// <summary>Tertiary resource count.</summary>
    public int TertiaryResource { get; set; }

    /// <summary>Last update time.</summary>
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// Tracks role declaration from a remote healer instance.
/// </summary>
public sealed class RemoteHealerRole
{
    /// <summary>Instance that owns this state.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Job ID of the healer.</summary>
    public uint JobId { get; init; }

    /// <summary>Declared role.</summary>
    public HealerRole Role { get; set; }

    /// <summary>Job priority for auto-detection.</summary>
    public int JobPriority { get; set; }

    /// <summary>Last update time.</summary>
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// Tracks an active ground effect from a remote healer instance.
/// </summary>
public sealed class RemoteGroundEffect
{
    /// <summary>Instance that placed this effect.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Action ID of the ground effect.</summary>
    public uint ActionId { get; init; }

    /// <summary>Center position of the effect.</summary>
    public System.Numerics.Vector3 Position { get; init; }

    /// <summary>Radius of the effect in yalms.</summary>
    public float Radius { get; init; }

    /// <summary>When the effect was placed.</summary>
    public DateTime PlacedAt { get; init; }

    /// <summary>When the effect expires.</summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>Whether this effect has expired.</summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>Remaining duration in seconds.</summary>
    public float RemainingSeconds => Math.Max(0, (float)(ExpiresAt - DateTime.UtcNow).TotalSeconds);
}

/// <summary>
/// Message announcing intent to raise a dead party member.
/// Used to reserve raise targets and prevent multiple healers from raising the same target.
/// </summary>
public sealed class RaiseIntentMessage : PartyMessage
{
    /// <summary>Entity ID of the raise target (the dead player).</summary>
    [JsonPropertyName("tid")]
    public uint TargetEntityId { get; set; }

    /// <summary>Action ID of the raise spell being cast.</summary>
    [JsonPropertyName("act")]
    public uint ActionId { get; set; }

    /// <summary>Cast time in milliseconds (0 for Swiftcast raise).</summary>
    [JsonPropertyName("cast")]
    public int CastTimeMs { get; set; }

    /// <summary>Whether this raise is using Swiftcast (takes priority over hardcast).</summary>
    [JsonPropertyName("swift")]
    public bool UsingSwiftcast { get; set; }

    public RaiseIntentMessage() : base(PartyMessageType.RaiseIntent) { }

    public RaiseIntentMessage(Guid instanceId, uint targetEntityId, uint actionId, int castTimeMs, bool usingSwiftcast)
        : base(PartyMessageType.RaiseIntent)
    {
        InstanceId = instanceId;
        TargetEntityId = targetEntityId;
        ActionId = actionId;
        CastTimeMs = castTimeMs;
        UsingSwiftcast = usingSwiftcast;
    }
}

/// <summary>
/// Represents a raise reservation from a remote instance.
/// Used to prevent multiple healers from raising the same dead party member.
/// </summary>
public sealed class RaiseReservation
{
    /// <summary>Instance that made the reservation.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Target entity ID (the dead player).</summary>
    public uint TargetEntityId { get; init; }

    /// <summary>Action ID of the raise spell.</summary>
    public uint ActionId { get; init; }

    /// <summary>When the reservation was made.</summary>
    public DateTime ReservedAt { get; init; }

    /// <summary>Expected cast completion time.</summary>
    public DateTime ExpectedCompletionTime { get; init; }

    /// <summary>Whether this raise is using Swiftcast.</summary>
    public bool UsingSwiftcast { get; init; }

    /// <summary>
    /// Whether this reservation has expired.
    /// Reservations expire 500ms after expected completion to account for network delay.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpectedCompletionTime.AddMilliseconds(500);
}

/// <summary>
/// Message announcing intent to cleanse a debuff from a party member.
/// Used to reserve cleanse targets and prevent multiple healers from cleansing the same target.
/// </summary>
public sealed class CleanseIntentMessage : PartyMessage
{
    /// <summary>Entity ID of the cleanse target.</summary>
    [JsonPropertyName("tid")]
    public uint TargetEntityId { get; set; }

    /// <summary>Status ID of the debuff being cleansed.</summary>
    [JsonPropertyName("sid")]
    public uint StatusId { get; set; }

    /// <summary>Action ID of the cleanse spell being cast (Esuna).</summary>
    [JsonPropertyName("act")]
    public uint ActionId { get; set; }

    /// <summary>Priority of the debuff being cleansed.</summary>
    [JsonPropertyName("pri")]
    public int DebuffPriority { get; set; }

    public CleanseIntentMessage() : base(PartyMessageType.CleanseIntent) { }

    public CleanseIntentMessage(Guid instanceId, uint targetEntityId, uint statusId, uint actionId, int debuffPriority)
        : base(PartyMessageType.CleanseIntent)
    {
        InstanceId = instanceId;
        TargetEntityId = targetEntityId;
        StatusId = statusId;
        ActionId = actionId;
        DebuffPriority = debuffPriority;
    }
}

/// <summary>
/// Represents a cleanse reservation from a remote instance.
/// Used to prevent multiple healers from cleansing the same debuff on the same target.
/// </summary>
public sealed class CleanseReservation
{
    /// <summary>Instance that made the reservation.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Target entity ID.</summary>
    public uint TargetEntityId { get; init; }

    /// <summary>Status ID of the debuff being cleansed.</summary>
    public uint StatusId { get; init; }

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
/// Message announcing intent to interrupt an enemy cast.
/// Used to reserve interrupt targets and prevent multiple instances from interrupting the same enemy cast.
/// </summary>
public sealed class InterruptIntentMessage : PartyMessage
{
    /// <summary>Entity ID of the enemy being interrupted.</summary>
    [JsonPropertyName("tid")]
    public uint TargetEntityId { get; set; }

    /// <summary>Action ID of the interrupt ability being used.</summary>
    [JsonPropertyName("act")]
    public uint ActionId { get; set; }

    /// <summary>Remaining cast time of the enemy in milliseconds.</summary>
    [JsonPropertyName("cast")]
    public int CastTimeMs { get; set; }

    public InterruptIntentMessage() : base(PartyMessageType.InterruptIntent) { }

    public InterruptIntentMessage(Guid instanceId, uint targetEntityId, uint actionId, int castTimeMs)
        : base(PartyMessageType.InterruptIntent)
    {
        InstanceId = instanceId;
        TargetEntityId = targetEntityId;
        ActionId = actionId;
        CastTimeMs = castTimeMs;
    }
}

/// <summary>
/// Represents an interrupt reservation from a remote instance.
/// Used to prevent multiple instances from interrupting the same enemy cast.
/// </summary>
public sealed class InterruptReservation
{
    /// <summary>Instance that made the reservation.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Entity ID of the enemy being interrupted.</summary>
    public uint TargetEntityId { get; init; }

    /// <summary>Action ID of the interrupt ability.</summary>
    public uint ActionId { get; init; }

    /// <summary>When the reservation was made.</summary>
    public DateTime ReservedAt { get; init; }

    /// <summary>When the reservation expires (based on enemy cast time + buffer).</summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Whether this reservation has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// Message announcing intent to perform a tank swap (Provoke or Shirk).
/// Used to coordinate tank swaps between two Olympus tank instances.
/// </summary>
public sealed class TankSwapIntentMessage : PartyMessage
{
    /// <summary>Entity ID of the boss being swapped on.</summary>
    [JsonPropertyName("tid")]
    public uint TargetEntityId { get; set; }

    /// <summary>
    /// True if this tank wants to take aggro (Provoke), false if giving aggro (Shirk).
    /// </summary>
    [JsonPropertyName("take")]
    public bool IntendToTakeAggro { get; set; }

    /// <summary>
    /// True if this is a confirmation of a remote swap request, false if initiating.
    /// </summary>
    [JsonPropertyName("conf")]
    public bool IsConfirmation { get; set; }

    /// <summary>
    /// Priority/urgency of the swap request (higher = more urgent).
    /// Used to resolve conflicts when both tanks request simultaneously.
    /// </summary>
    [JsonPropertyName("pri")]
    public int SwapPriority { get; set; }

    public TankSwapIntentMessage() : base(PartyMessageType.TankSwapIntent) { }

    public TankSwapIntentMessage(Guid instanceId, uint targetEntityId, bool intendToTakeAggro, bool isConfirmation, int swapPriority)
        : base(PartyMessageType.TankSwapIntent)
    {
        InstanceId = instanceId;
        TargetEntityId = targetEntityId;
        IntendToTakeAggro = intendToTakeAggro;
        IsConfirmation = isConfirmation;
        SwapPriority = swapPriority;
    }
}

/// <summary>
/// Represents a tank swap reservation from a remote instance.
/// Used to coordinate Provoke and Shirk between two Olympus tanks.
/// </summary>
public sealed class TankSwapReservation
{
    /// <summary>Instance that made the reservation.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Entity ID of the boss being swapped on.</summary>
    public uint TargetEntityId { get; init; }

    /// <summary>True if the remote tank wants to take aggro (Provoke).</summary>
    public bool IntendToTakeAggro { get; init; }

    /// <summary>True if this is a confirmation of our swap request.</summary>
    public bool IsConfirmation { get; init; }

    /// <summary>Priority of the swap request.</summary>
    public int SwapPriority { get; init; }

    /// <summary>When the reservation was made.</summary>
    public DateTime ReservedAt { get; init; }

    /// <summary>When the reservation expires.</summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Whether this reservation has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Remaining seconds until this reservation expires.
    /// </summary>
    public float RemainingSeconds => Math.Max(0, (float)(ExpiresAt - DateTime.UtcNow).TotalSeconds);
}
