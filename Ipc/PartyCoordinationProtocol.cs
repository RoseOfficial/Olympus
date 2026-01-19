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
