using System;
using Olympus.Ipc;

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

    #region Healer Burst Awareness

    /// <summary>
    /// Enable healer burst awareness.
    /// When enabled, healers will be aware of DPS burst windows and can optimize
    /// HoT deployment, shield timing, and mitigation cooldowns around burst phases.
    /// </summary>
    public bool EnableHealerBurstAwareness { get; set; } = true;

    /// <summary>
    /// Time window (in seconds) to consider a burst as "imminent".
    /// If a DPS burst is expected within this window, healers may pre-deploy HoTs or shields.
    /// Valid range: 2.0 to 10.0 seconds.
    /// </summary>
    private float _burstImminentWindowSeconds = 5.0f;
    public float BurstImminentWindowSeconds
    {
        get => _burstImminentWindowSeconds;
        set => _burstImminentWindowSeconds = Math.Clamp(value, 2.0f, 10.0f);
    }

    /// <summary>
    /// Deploy HoTs and shields proactively when a burst is imminent.
    /// When enabled, healers will deploy Asylum, Kerachole, etc. before burst windows
    /// so the party has sustained healing during high-damage DPS phases.
    /// </summary>
    public bool PreferShieldsBeforeBurst { get; set; } = false;

    /// <summary>
    /// Delay major party mitigations during active burst windows.
    /// When enabled, abilities like Temperance, Expedient, etc. will be delayed
    /// during burst windows unless party HP drops to emergency levels.
    /// This prevents mitigation timing from conflicting with DPS burst alignment.
    /// </summary>
    public bool DelayMitigationsDuringBurst { get; set; } = false;

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

    #region Resurrection Coordination

    /// <summary>
    /// Enable resurrection coordination with other Olympus instances.
    /// When enabled, healers will coordinate raises to prevent multiple
    /// instances from raising the same dead party member.
    /// </summary>
    public bool EnableRaiseCoordination { get; set; } = true;

    /// <summary>
    /// How long raise reservations remain valid (milliseconds).
    /// After this time plus expected cast time, a reservation expires.
    /// Should be long enough for cast time + network delay buffer.
    /// Valid range: 5000 to 15000.
    /// </summary>
    private int _raiseReservationExpiryMs = 10000;
    public int RaiseReservationExpiryMs
    {
        get => _raiseReservationExpiryMs;
        set => _raiseReservationExpiryMs = Math.Clamp(value, 5000, 15000);
    }

    #endregion

    #region Cleanse Coordination

    /// <summary>
    /// Enable cleanse coordination with other Olympus instances.
    /// When enabled, healers will coordinate Esuna usage to prevent multiple
    /// instances from cleansing the same debuff on the same target.
    /// </summary>
    public bool EnableCleanseCoordination { get; set; } = true;

    /// <summary>
    /// How long cleanse reservations remain valid (milliseconds).
    /// After this time, a reservation expires.
    /// Should be short since Esuna is instant cast.
    /// Valid range: 1000 to 5000.
    /// </summary>
    private int _cleanseReservationExpiryMs = 2000;
    public int CleanseReservationExpiryMs
    {
        get => _cleanseReservationExpiryMs;
        set => _cleanseReservationExpiryMs = Math.Clamp(value, 1000, 5000);
    }

    #endregion

    #region Interrupt Coordination

    /// <summary>
    /// Enable interrupt coordination with other Olympus instances.
    /// When enabled, tanks and ranged physical DPS will coordinate interrupt abilities
    /// (Interject, Head Graze, Low Blow) to prevent multiple instances from
    /// interrupting the same enemy cast.
    /// </summary>
    public bool EnableInterruptCoordination { get; set; } = true;

    /// <summary>
    /// How long interrupt reservations remain valid (milliseconds).
    /// After this time, a reservation expires.
    /// Should be short since interrupts are used on fast enemy casts.
    /// Valid range: 1000 to 5000.
    /// </summary>
    private int _interruptReservationExpiryMs = 3000;
    public int InterruptReservationExpiryMs
    {
        get => _interruptReservationExpiryMs;
        set => _interruptReservationExpiryMs = Math.Clamp(value, 1000, 5000);
    }

    #endregion

    #region Multi-Healer Optimization

    /// <summary>
    /// Enable healer gauge state sharing.
    /// When enabled, healers will broadcast their resource counts (Lily, Aetherflow, etc.)
    /// to other Olympus instances for resource-aware healing decisions.
    /// </summary>
    public bool EnableHealerGaugeSharing { get; set; } = true;

    /// <summary>
    /// Enable healer role coordination.
    /// When enabled, healers will declare primary/secondary roles
    /// and adjust healing thresholds accordingly.
    /// </summary>
    public bool EnableHealerRoleCoordination { get; set; } = true;

    /// <summary>
    /// Preferred healer role.
    /// Auto: Auto-detect based on job priority (WHM > AST > SCH > SGE).
    /// Primary: Take lead on healing, higher thresholds.
    /// Secondary: Assist, lower thresholds, more DPS focus.
    /// </summary>
    public HealerRole PreferredHealerRole { get; set; } = HealerRole.Auto;

    /// <summary>
    /// HP threshold for secondary healer to assist with healing.
    /// Secondary healers will only heal targets below this threshold.
    /// Valid range: 0.30 to 0.80.
    /// </summary>
    private float _secondaryHealAssistThreshold = 0.50f;
    public float SecondaryHealAssistThreshold
    {
        get => _secondaryHealAssistThreshold;
        set => _secondaryHealAssistThreshold = Math.Clamp(value, 0.30f, 0.80f);
    }

    /// <summary>
    /// Enable ground effect coordination.
    /// When enabled, healers will coordinate ground-targeted abilities
    /// (Asylum, Sacred Soil, Earthly Star, Kerachole) to prevent overlapping.
    /// </summary>
    public bool EnableGroundEffectCoordination { get; set; } = true;

    /// <summary>
    /// Overlap threshold for ground effects (0-1).
    /// 0.5 = skip if 50% overlap, 1.0 = skip only if identical position.
    /// Lower values are more conservative.
    /// Valid range: 0.3 to 1.0.
    /// </summary>
    private float _groundEffectOverlapThreshold = 0.5f;
    public float GroundEffectOverlapThreshold
    {
        get => _groundEffectOverlapThreshold;
        set => _groundEffectOverlapThreshold = Math.Clamp(value, 0.3f, 1.0f);
    }

    #endregion
}
