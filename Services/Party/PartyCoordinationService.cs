using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Data;
using Olympus.Ipc;

namespace Olympus.Services.Party;

/// <summary>
/// Core coordination service for multi-Olympus party coordination.
/// Manages local and remote state for heal overlap prevention.
/// </summary>
public sealed class PartyCoordinationService : IPartyCoordinationService
{
    private readonly PartyCoordinationConfig _config;
    private readonly IPluginLog _log;

    // Stable instance identifier
    private readonly Guid _instanceId = Guid.NewGuid();

    // Remote instance tracking
    private readonly Dictionary<Guid, RemoteOlympusInstance> _remoteInstances = new();

    // Heal reservations (local + remote)
    private readonly Dictionary<uint, HealReservation> _localReservations = new();
    private readonly Dictionary<uint, HealReservation> _remoteReservations = new();

    // Remote cooldown tracking (key = actionId, value = list of active cooldowns from remote instances)
    private readonly Dictionary<uint, List<RemoteCooldownInfo>> _remoteCooldowns = new();

    // Remote AoE heal reservation
    private AoEHealReservation? _remoteAoEReservation;

    // Remote raid buff state tracking (key = actionId, value = list of states from remote instances)
    private readonly Dictionary<uint, List<RemoteRaidBuffState>> _remoteRaidBuffStates = new();

    // Active burst window tracking
    private DateTime? _burstWindowStart;
    private float _burstWindowDuration;
    private uint _burstWindowTriggerAction;

    // Heartbeat timing
    private DateTime _lastHeartbeatSent = DateTime.MinValue;

    // Event callbacks for IPC layer
    public event Action<HeartbeatMessage>? OnHeartbeatReady;
    public event Action<HealIntentMessage>? OnHealIntentReady;
    public event Action<HealLandedMessage>? OnHealLandedReady;
    public event Action<CooldownUsedMessage>? OnCooldownUsedReady;
    public event Action<AoEHealIntentMessage>? OnAoEHealIntentReady;
    public event Action<RaidBuffIntentMessage>? OnRaidBuffIntentReady;
    public event Action<BurstWindowStartMessage>? OnBurstWindowStartReady;

    public PartyCoordinationService(PartyCoordinationConfig config, IPluginLog log)
    {
        _config = config;
        _log = log;
    }

    #region IPartyCoordinationService Implementation

    public bool IsPartyCoordinationEnabled => _config.EnablePartyCoordination;

    public int RemoteInstanceCount => _remoteInstances.Count;

    public bool HasRemoteHealers => _remoteInstances.Values.Any(i => i.IsEnabled && JobRegistry.IsHealer(i.JobId));

    public Guid InstanceId => _instanceId;

    public bool IsTargetReservedByOther(uint entityId)
    {
        if (!_config.EnablePartyCoordination)
            return false;

        // Check remote reservations
        if (_remoteReservations.TryGetValue(entityId, out var reservation))
        {
            // Check if reservation is still valid
            var elapsed = (DateTime.UtcNow - reservation.ReservedAt).TotalMilliseconds;
            if (elapsed < _config.HealReservationExpiryMs)
                return true;

            // Expired, clean up
            _remoteReservations.Remove(entityId);
        }

        return false;
    }

    public bool ReserveTarget(uint entityId, int healAmount, uint actionId, int castTimeMs = 0)
    {
        if (!_config.EnablePartyCoordination)
            return true;

        // Check if already reserved by remote
        if (IsTargetReservedByOther(entityId))
            return false;

        var now = DateTime.UtcNow;

        // Create local reservation
        var reservation = new HealReservation
        {
            InstanceId = _instanceId,
            TargetEntityId = entityId,
            EstimatedHealAmount = healAmount,
            ActionId = actionId,
            ReservedAt = now,
            ExpectedLandingTime = now.AddMilliseconds(castTimeMs)
        };

        _localReservations[entityId] = reservation;

        // Only broadcast if heal amount meets threshold
        if (healAmount >= _config.MinHealAmountToBroadcast)
        {
            var message = new HealIntentMessage(_instanceId, entityId, healAmount, actionId, castTimeMs);
            OnHealIntentReady?.Invoke(message);

            if (_config.LogCoordinationEvents)
                _log.Debug("[PartyCoord] Reserved target {0} for heal ({1} HP, action {2})", entityId, healAmount, actionId);
        }

        return true;
    }

    public void OnHealLanded(uint entityId, int amount, uint actionId)
    {
        if (!_config.EnablePartyCoordination)
            return;

        // Clear local reservation
        _localReservations.Remove(entityId);

        // Broadcast heal landed
        if (amount >= _config.MinHealAmountToBroadcast)
        {
            var message = new HealLandedMessage(_instanceId, entityId, amount, actionId);
            OnHealLandedReady?.Invoke(message);

            if (_config.LogCoordinationEvents)
                _log.Debug("[PartyCoord] Heal landed on {0} ({1} HP, action {2})", entityId, amount, actionId);
        }
    }

    public void OnCooldownUsed(uint actionId, int recastTimeMs)
    {
        if (!_config.EnablePartyCoordination || !_config.BroadcastMajorCooldowns)
            return;

        var message = new CooldownUsedMessage(_instanceId, actionId, recastTimeMs);
        OnCooldownUsedReady?.Invoke(message);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Cooldown used: action {0}, recast {1}ms", actionId, recastTimeMs);
    }

    public IReadOnlyDictionary<uint, HealReservation> GetRemoteReservations()
    {
        return _remoteReservations;
    }

    public IReadOnlyList<RemoteOlympusInstance> GetRemoteInstances()
    {
        return _remoteInstances.Values.ToList();
    }

    public int GetRemotePendingHealAmount(uint entityId)
    {
        if (!_config.EnablePartyCoordination)
            return 0;

        if (_remoteReservations.TryGetValue(entityId, out var reservation))
        {
            var elapsed = (DateTime.UtcNow - reservation.ReservedAt).TotalMilliseconds;
            if (elapsed < _config.HealReservationExpiryMs)
                return reservation.EstimatedHealAmount;
        }

        return 0;
    }

    #region AoE Heal Coordination

    public bool IsAoEHealReservedByOther()
    {
        if (!_config.EnablePartyCoordination || !_config.EnableAoEHealCoordination)
            return false;

        // Check if remote reservation exists and is not expired
        if (_remoteAoEReservation != null && !_remoteAoEReservation.IsExpired)
            return true;

        // Expired, clean up
        _remoteAoEReservation = null;
        return false;
    }

    public void ReserveAoEHeal(uint actionId, int healPotency, int castTimeMs)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableAoEHealCoordination)
            return;

        var message = new AoEHealIntentMessage(_instanceId, actionId, healPotency, castTimeMs);
        OnAoEHealIntentReady?.Invoke(message);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Reserved AoE heal (action {0}, potency {1})", actionId, healPotency);
    }

    #endregion

    #region Cooldown Coordination

    public bool IsCooldownActiveRemotely(uint actionId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableCooldownCoordination)
            return false;

        if (!_remoteCooldowns.TryGetValue(actionId, out var list))
            return false;

        return list.Exists(c => c.IsOnCooldown);
    }

    public int GetRemoteCooldownCount(uint actionId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableCooldownCoordination)
            return 0;

        if (!_remoteCooldowns.TryGetValue(actionId, out var list))
            return 0;

        var count = 0;
        foreach (var cd in list)
        {
            if (cd.IsOnCooldown)
                count++;
        }
        return count;
    }

    public float GetShortestRemoteCooldownRemaining(uint actionId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableCooldownCoordination)
            return 0;

        if (!_remoteCooldowns.TryGetValue(actionId, out var list))
            return 0;

        var shortest = float.MaxValue;
        var found = false;

        foreach (var cd in list)
        {
            if (cd.IsOnCooldown && cd.RemainingSeconds < shortest)
            {
                shortest = cd.RemainingSeconds;
                found = true;
            }
        }

        return found ? shortest : 0;
    }

    public bool WasPartyMitigationUsedRecently(float withinSeconds = 3f)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableCooldownCoordination)
            return false;

        foreach (var kvp in _remoteCooldowns)
        {
            // Only check coordinated cooldowns (should already be filtered, but double-check)
            if (!CoordinatedCooldowns.IsCoordinatedCooldown(kvp.Key))
                continue;

            foreach (var cd in kvp.Value)
            {
                if (cd.SecondsSinceUsed <= withinSeconds)
                    return true;
            }
        }

        return false;
    }

    public bool WasPersonalDefensiveUsedRecently(float withinSeconds = 3f)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableCooldownCoordination)
            return false;

        foreach (var kvp in _remoteCooldowns)
        {
            // Only check personal defensives
            if (!CoordinatedCooldowns.IsPersonalDefensive(kvp.Key))
                continue;

            foreach (var cd in kvp.Value)
            {
                if (cd.SecondsSinceUsed <= withinSeconds)
                    return true;
            }
        }

        return false;
    }

    public IReadOnlyList<RemoteCooldownInfo> GetRemoteCooldowns(uint actionId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableCooldownCoordination)
            return Array.Empty<RemoteCooldownInfo>();

        if (!_remoteCooldowns.TryGetValue(actionId, out var list))
            return Array.Empty<RemoteCooldownInfo>();

        // Return only active cooldowns
        var active = new List<RemoteCooldownInfo>();
        foreach (var cd in list)
        {
            if (cd.IsOnCooldown)
                active.Add(cd);
        }
        return active;
    }

    #endregion

    #region Raid Buff Coordination

    public bool HasRemoteDps => _remoteInstances.Values.Any(i => i.IsEnabled && JobRegistry.IsDps(i.JobId));

    public bool HasPendingRaidBuffIntent(float withinSeconds = 5f)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaidBuffCoordination)
            return false;

        var now = DateTime.UtcNow;

        foreach (var kvp in _remoteRaidBuffStates)
        {
            foreach (var state in kvp.Value)
            {
                if (state.IsIntentOnly && !state.IsIntentExpired)
                {
                    // Check if activation is expected within the window
                    var expectedActivation = state.IntentAnnouncedAt.AddSeconds(state.PlannedDelaySeconds);
                    var timeUntilActivation = (expectedActivation - now).TotalSeconds;
                    if (timeUntilActivation <= withinSeconds && timeUntilActivation >= -1)
                        return true;
                }
            }
        }

        return false;
    }

    public IReadOnlyList<RemoteRaidBuffState> GetPendingRaidBuffIntents()
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaidBuffCoordination)
            return Array.Empty<RemoteRaidBuffState>();

        var result = new List<RemoteRaidBuffState>();

        foreach (var kvp in _remoteRaidBuffStates)
        {
            foreach (var state in kvp.Value)
            {
                if (state.IsIntentOnly && !state.IsIntentExpired)
                    result.Add(state);
            }
        }

        return result;
    }

    public bool IsInBurstWindow()
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaidBuffCoordination)
            return false;

        // Check local burst window tracking
        if (_burstWindowStart.HasValue)
        {
            var elapsed = (DateTime.UtcNow - _burstWindowStart.Value).TotalSeconds;
            if (elapsed < _burstWindowDuration)
                return true;
        }

        // Check if any remote instances have active buffs
        foreach (var kvp in _remoteRaidBuffStates)
        {
            foreach (var state in kvp.Value)
            {
                if (state.IsBuffActive)
                    return true;
            }
        }

        return false;
    }

    public float GetBurstWindowRemaining()
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaidBuffCoordination)
            return 0;

        float maxRemaining = 0;

        // Check local burst window
        if (_burstWindowStart.HasValue)
        {
            var elapsed = (float)(DateTime.UtcNow - _burstWindowStart.Value).TotalSeconds;
            var remaining = _burstWindowDuration - elapsed;
            if (remaining > maxRemaining)
                maxRemaining = remaining;
        }

        // Check remote buff durations
        foreach (var kvp in _remoteRaidBuffStates)
        {
            foreach (var state in kvp.Value)
            {
                if (state.BuffRemainingSeconds > maxRemaining)
                    maxRemaining = state.BuffRemainingSeconds;
            }
        }

        return Math.Max(0, maxRemaining);
    }

    public void AnnounceRaidBuffIntent(uint actionId, float secondsUntilActivation = 0f)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaidBuffCoordination)
            return;

        if (!CoordinatedRaidBuffs.IsCoordinatedRaidBuff(actionId))
            return;

        var duration = CoordinatedRaidBuffs.GetBuffDuration(actionId);
        var message = new RaidBuffIntentMessage(_instanceId, actionId, secondsUntilActivation, duration);
        OnRaidBuffIntentReady?.Invoke(message);

        if (_config.LogRaidBuffCoordination)
            _log.Debug("[PartyCoord] Announced raid buff intent: action {0}, activating in {1:F1}s", actionId, secondsUntilActivation);
    }

    public void OnRaidBuffUsed(uint actionId, int recastTimeMs)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaidBuffCoordination)
            return;

        if (!CoordinatedRaidBuffs.IsCoordinatedRaidBuff(actionId))
            return;

        var duration = CoordinatedRaidBuffs.GetBuffDuration(actionId);
        var isMajorBurst = recastTimeMs >= 100_000; // 100+ second CD = major burst

        // Update local burst window tracking
        _burstWindowStart = DateTime.UtcNow;
        _burstWindowDuration = duration;
        _burstWindowTriggerAction = actionId;

        var message = new BurstWindowStartMessage(_instanceId, actionId, duration, isMajorBurst);
        OnBurstWindowStartReady?.Invoke(message);

        if (_config.LogRaidBuffCoordination)
            _log.Debug("[PartyCoord] Raid buff activated: action {0}, duration {1:F1}s, major={2}", actionId, duration, isMajorBurst);
    }

    public bool IsRaidBuffAligned(uint actionId, float toleranceSeconds = 0f)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaidBuffCoordination)
            return true; // No coordination = always "aligned"

        // Use config value if tolerance not specified
        if (toleranceSeconds <= 0)
            toleranceSeconds = _config.MaxBuffDesyncSeconds;

        // If no remote data for this action, consider aligned
        if (!_remoteRaidBuffStates.TryGetValue(actionId, out var states) || states.Count == 0)
            return true;

        // Check if any remote instance has significantly different cooldown timing
        foreach (var state in states)
        {
            // If they recently used the buff, check how desynced we would be
            var remoteCdRemaining = state.CooldownRemainingSeconds;

            // If remote CD is much higher (they used it recently but we haven't)
            // or much lower (we used it recently but they haven't), we're desynced
            if (remoteCdRemaining > toleranceSeconds)
            {
                if (_config.LogRaidBuffCoordination)
                    _log.Debug("[PartyCoord] Raid buff desynced: action {0}, remote CD remaining {1:F1}s > tolerance {2:F1}s",
                        actionId, remoteCdRemaining, toleranceSeconds);
                return false;
            }
        }

        return true;
    }

    public BurstWindowState GetBurstWindowState()
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaidBuffCoordination)
            return BurstWindowState.NoInfo;

        // Check if we have any remote DPS instances
        if (!HasRemoteDps)
            return BurstWindowState.NoInfo;

        var isActive = IsInBurstWindow();
        var remainingSeconds = GetBurstWindowRemaining();

        // Gather pending intents
        var pendingIntents = GetPendingRaidBuffIntents();
        var pendingCount = pendingIntents.Count;

        // Calculate seconds until next burst from pending intents
        float secondsUntilBurst = -1f;
        if (isActive)
        {
            secondsUntilBurst = 0f;
        }
        else if (pendingCount > 0)
        {
            // Find the soonest pending intent
            var now = DateTime.UtcNow;
            foreach (var intent in pendingIntents)
            {
                var expectedActivation = intent.IntentAnnouncedAt.AddSeconds(intent.PlannedDelaySeconds);
                var timeUntil = (float)(expectedActivation - now).TotalSeconds;
                if (timeUntil > 0 && (secondsUntilBurst < 0 || timeUntil < secondsUntilBurst))
                    secondsUntilBurst = timeUntil;
            }
        }

        return new BurstWindowState
        {
            IsActive = isActive,
            IsImminent = pendingCount > 0 && !isActive,
            SecondsUntilBurst = secondsUntilBurst,
            SecondsRemaining = remainingSeconds,
            PendingBurstCount = pendingCount,
            HasBurstInfo = true
        };
    }

    public bool IsBurstImminentOrActive(float imminentSeconds = 5f)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaidBuffCoordination)
            return false;

        // Check if currently in burst
        if (IsInBurstWindow())
            return true;

        // Check if burst is imminent
        return HasPendingRaidBuffIntent(imminentSeconds);
    }

    public float GetSecondsUntilBurst()
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaidBuffCoordination)
            return -1f;

        // If already in burst, return 0
        if (IsInBurstWindow())
            return 0f;

        // Find the soonest pending intent
        var pendingIntents = GetPendingRaidBuffIntents();
        if (pendingIntents.Count == 0)
            return -1f;

        var now = DateTime.UtcNow;
        float soonest = float.MaxValue;

        foreach (var intent in pendingIntents)
        {
            var expectedActivation = intent.IntentAnnouncedAt.AddSeconds(intent.PlannedDelaySeconds);
            var timeUntil = (float)(expectedActivation - now).TotalSeconds;
            if (timeUntil > 0 && timeUntil < soonest)
                soonest = timeUntil;
        }

        return soonest < float.MaxValue ? soonest : -1f;
    }

    #endregion

    public void Update(uint playerEntityId, uint jobId, bool isEnabled)
    {
        if (!_config.EnablePartyCoordination)
            return;

        var now = DateTime.UtcNow;

        // Send heartbeat if interval elapsed
        var heartbeatElapsed = (now - _lastHeartbeatSent).TotalMilliseconds;
        if (heartbeatElapsed >= _config.HeartbeatIntervalMs)
        {
            var heartbeat = new HeartbeatMessage(_instanceId, jobId, playerEntityId, isEnabled);
            OnHeartbeatReady?.Invoke(heartbeat);
            _lastHeartbeatSent = now;
        }

        // Clean up expired remote instances
        CleanupExpiredInstances(now);

        // Clean up expired local reservations
        CleanupExpiredReservations(now);
    }

    public void Clear()
    {
        _remoteInstances.Clear();
        _localReservations.Clear();
        _remoteReservations.Clear();
        _remoteCooldowns.Clear();
        _remoteAoEReservation = null;
        _remoteRaidBuffStates.Clear();
        _burstWindowStart = null;
        _burstWindowDuration = 0;
        _burstWindowTriggerAction = 0;

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Cleared all coordination state");
    }

    #endregion

    #region Message Handlers (called by IPC layer)

    /// <summary>
    /// Handles incoming heartbeat from a remote instance.
    /// </summary>
    public void HandleRemoteHeartbeat(HeartbeatMessage message)
    {
        if (message.InstanceId == _instanceId)
            return; // Ignore our own messages

        if (_remoteInstances.TryGetValue(message.InstanceId, out var existing))
        {
            // Update existing instance
            existing.JobId = message.JobId;
            existing.PlayerEntityId = message.PlayerEntityId;
            existing.IsEnabled = message.IsEnabled;
            existing.LastHeartbeat = DateTime.UtcNow;
        }
        else
        {
            // New instance discovered
            _remoteInstances[message.InstanceId] = new RemoteOlympusInstance
            {
                InstanceId = message.InstanceId,
                JobId = message.JobId,
                PlayerEntityId = message.PlayerEntityId,
                IsEnabled = message.IsEnabled,
                LastHeartbeat = DateTime.UtcNow
            };

            if (_config.LogCoordinationEvents)
                _log.Info("[PartyCoord] Discovered remote Olympus instance: {0} (Job {1})", message.InstanceId, message.JobId);
        }
    }

    /// <summary>
    /// Handles incoming heal intent from a remote instance.
    /// </summary>
    public void HandleRemoteHealIntent(HealIntentMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        var reservation = new HealReservation
        {
            InstanceId = message.InstanceId,
            TargetEntityId = message.TargetEntityId,
            EstimatedHealAmount = message.EstimatedHealAmount,
            ActionId = message.ActionId,
            ReservedAt = DateTime.UtcNow,
            ExpectedLandingTime = DateTime.UtcNow.AddMilliseconds(message.CastTimeMs)
        };

        _remoteReservations[message.TargetEntityId] = reservation;

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Remote reservation: target {0}, {1} HP from instance {2}",
                message.TargetEntityId, message.EstimatedHealAmount, message.InstanceId);
    }

    /// <summary>
    /// Handles incoming heal landed from a remote instance.
    /// </summary>
    public void HandleRemoteHealLanded(HealLandedMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        // Clear the reservation for this target
        _remoteReservations.Remove(message.TargetEntityId);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Remote heal landed: target {0}, {1} HP from instance {2}",
                message.TargetEntityId, message.ActualHealAmount, message.InstanceId);
    }

    /// <summary>
    /// Handles incoming cooldown used from a remote instance.
    /// Tracks the cooldown for coordination decisions.
    /// </summary>
    public void HandleRemoteCooldownUsed(CooldownUsedMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        // Track both party mitigations and personal defensives for coordination
        var isPartyMitigation = CoordinatedCooldowns.IsCoordinatedCooldown(message.ActionId);
        var isPersonalDefensive = CoordinatedCooldowns.IsPersonalDefensive(message.ActionId);

        if (!isPartyMitigation && !isPersonalDefensive)
        {
            if (_config.LogCoordinationEvents)
                _log.Debug("[PartyCoord] Ignoring non-coordinated cooldown: action {0}", message.ActionId);
            return;
        }

        var info = new RemoteCooldownInfo
        {
            InstanceId = message.InstanceId,
            ActionId = message.ActionId,
            UsedAt = DateTime.UtcNow,
            RecastTimeMs = message.RecastTimeMs > 0 ? message.RecastTimeMs : CoordinatedCooldowns.GetDefaultRecastTime(message.ActionId)
        };

        // Get or create list for this action
        if (!_remoteCooldowns.TryGetValue(message.ActionId, out var list))
        {
            list = new List<RemoteCooldownInfo>();
            _remoteCooldowns[message.ActionId] = list;
        }

        // Replace existing cooldown from same instance (if any)
        list.RemoveAll(c => c.InstanceId == message.InstanceId);
        list.Add(info);

        var cdType = isPersonalDefensive ? "personal defensive" : "party mitigation";
        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Tracked remote {0}: action {1}, recast {2}ms from instance {3}",
                cdType, message.ActionId, info.RecastTimeMs, message.InstanceId);
    }

    /// <summary>
    /// Handles incoming AoE heal intent from a remote instance.
    /// </summary>
    public void HandleRemoteAoEHealIntent(AoEHealIntentMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        if (!_config.EnableAoEHealCoordination)
            return;

        var reservation = new AoEHealReservation
        {
            InstanceId = message.InstanceId,
            ActionId = message.ActionId,
            HealPotency = message.HealPotency,
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(_config.AoEHealReservationExpiryMs)
        };

        _remoteAoEReservation = reservation;

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Remote AoE heal reservation: action {0}, potency {1} from instance {2}",
                message.ActionId, message.HealPotency, message.InstanceId);
    }

    /// <summary>
    /// Handles incoming raid buff intent from a remote instance.
    /// </summary>
    public void HandleRemoteRaidBuffIntent(RaidBuffIntentMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        if (!_config.EnableRaidBuffCoordination)
            return;

        // Only track coordinated raid buffs
        if (!CoordinatedRaidBuffs.IsCoordinatedRaidBuff(message.ActionId))
            return;

        var state = new RemoteRaidBuffState
        {
            InstanceId = message.InstanceId,
            ActionId = message.ActionId,
            IntentAnnouncedAt = DateTime.UtcNow,
            PlannedDelaySeconds = message.SecondsUntilActivation,
            ActivatedAt = null,
            BuffDuration = message.BuffDuration,
            RecastTimeMs = CoordinatedRaidBuffs.GetDefaultRecastTime(message.ActionId)
        };

        // Get or create list for this action
        if (!_remoteRaidBuffStates.TryGetValue(message.ActionId, out var list))
        {
            list = new List<RemoteRaidBuffState>();
            _remoteRaidBuffStates[message.ActionId] = list;
        }

        // Replace existing state from same instance
        list.RemoveAll(s => s.InstanceId == message.InstanceId);
        list.Add(state);

        if (_config.LogRaidBuffCoordination)
            _log.Debug("[PartyCoord] Remote raid buff intent: action {0}, delay {1:F1}s from instance {2}",
                message.ActionId, message.SecondsUntilActivation, message.InstanceId);
    }

    /// <summary>
    /// Handles incoming burst window start from a remote instance.
    /// </summary>
    public void HandleRemoteBurstWindowStart(BurstWindowStartMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        if (!_config.EnableRaidBuffCoordination)
            return;

        // Only track coordinated raid buffs
        if (!CoordinatedRaidBuffs.IsCoordinatedRaidBuff(message.TriggerActionId))
            return;

        // Update existing intent state to mark as activated
        if (_remoteRaidBuffStates.TryGetValue(message.TriggerActionId, out var list))
        {
            var existingState = list.Find(s => s.InstanceId == message.InstanceId);
            if (existingState != null)
            {
                existingState.ActivatedAt = DateTime.UtcNow;
            }
            else
            {
                // No intent was received, create state directly
                var state = new RemoteRaidBuffState
                {
                    InstanceId = message.InstanceId,
                    ActionId = message.TriggerActionId,
                    IntentAnnouncedAt = DateTime.UtcNow,
                    PlannedDelaySeconds = 0,
                    ActivatedAt = DateTime.UtcNow,
                    BuffDuration = message.WindowDuration,
                    RecastTimeMs = CoordinatedRaidBuffs.GetDefaultRecastTime(message.TriggerActionId)
                };
                list.Add(state);
            }
        }
        else
        {
            // No tracking for this action yet, create new
            var state = new RemoteRaidBuffState
            {
                InstanceId = message.InstanceId,
                ActionId = message.TriggerActionId,
                IntentAnnouncedAt = DateTime.UtcNow,
                PlannedDelaySeconds = 0,
                ActivatedAt = DateTime.UtcNow,
                BuffDuration = message.WindowDuration,
                RecastTimeMs = CoordinatedRaidBuffs.GetDefaultRecastTime(message.TriggerActionId)
            };
            _remoteRaidBuffStates[message.TriggerActionId] = new List<RemoteRaidBuffState> { state };
        }

        // Update local burst window tracking for UI display
        if (!_burstWindowStart.HasValue || message.WindowDuration > _burstWindowDuration)
        {
            _burstWindowStart = DateTime.UtcNow;
            _burstWindowDuration = message.WindowDuration;
            _burstWindowTriggerAction = message.TriggerActionId;
        }

        if (_config.LogRaidBuffCoordination)
            _log.Debug("[PartyCoord] Remote burst window started: action {0}, duration {1:F1}s, major={2} from instance {3}",
                message.TriggerActionId, message.WindowDuration, message.IsMajorBurst, message.InstanceId);
    }

    #endregion

    #region Cleanup

    private void CleanupExpiredInstances(DateTime now)
    {
        var expiredInstances = new List<Guid>();

        foreach (var kvp in _remoteInstances)
        {
            var elapsed = (now - kvp.Value.LastHeartbeat).TotalMilliseconds;
            if (elapsed > _config.InstanceTimeoutMs)
            {
                expiredInstances.Add(kvp.Key);
            }
        }

        foreach (var id in expiredInstances)
        {
            _remoteInstances.Remove(id);

            // Also remove any reservations from this instance
            var reservationsToRemove = _remoteReservations
                .Where(r => r.Value.InstanceId == id)
                .Select(r => r.Key)
                .ToList();

            foreach (var targetId in reservationsToRemove)
            {
                _remoteReservations.Remove(targetId);
            }

            // Remove cooldowns from disconnected instance
            foreach (var kvp in _remoteCooldowns)
            {
                kvp.Value.RemoveAll(c => c.InstanceId == id);
            }

            // Remove raid buff states from disconnected instance
            foreach (var kvp in _remoteRaidBuffStates)
            {
                kvp.Value.RemoveAll(s => s.InstanceId == id);
            }

            if (_config.LogCoordinationEvents)
                _log.Info("[PartyCoord] Remote instance timed out: {0}", id);
        }

        // Clean up expired cooldowns (no longer on recast)
        CleanupExpiredCooldowns();

        // Clean up expired raid buff states
        CleanupExpiredRaidBuffStates();

        // Clean up burst window if expired
        if (_burstWindowStart.HasValue)
        {
            var elapsed = (DateTime.UtcNow - _burstWindowStart.Value).TotalSeconds;
            if (elapsed > _burstWindowDuration)
            {
                _burstWindowStart = null;
                _burstWindowDuration = 0;
                _burstWindowTriggerAction = 0;
            }
        }
    }

    private void CleanupExpiredCooldowns()
    {
        // Remove expired cooldowns from all lists
        foreach (var kvp in _remoteCooldowns)
        {
            kvp.Value.RemoveAll(c => !c.IsOnCooldown);
        }

        // Remove empty lists
        var emptyKeys = _remoteCooldowns
            .Where(kvp => kvp.Value.Count == 0)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in emptyKeys)
        {
            _remoteCooldowns.Remove(key);
        }
    }

    private void CleanupExpiredRaidBuffStates()
    {
        // Remove expired intents and inactive buff states
        foreach (var kvp in _remoteRaidBuffStates)
        {
            kvp.Value.RemoveAll(s =>
            {
                // Remove expired intents that were never activated
                if (s.IsIntentOnly && s.IsIntentExpired)
                    return true;

                // Keep states that are either:
                // - Still intent (not expired)
                // - Buff is active
                // - Cooldown is still running (for alignment tracking)
                return !s.IsIntentOnly && !s.IsBuffActive && s.CooldownRemainingSeconds <= 0;
            });
        }

        // Remove empty lists
        var emptyKeys = _remoteRaidBuffStates
            .Where(kvp => kvp.Value.Count == 0)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in emptyKeys)
        {
            _remoteRaidBuffStates.Remove(key);
        }
    }

    private void CleanupExpiredReservations(DateTime now)
    {
        // Clean up local reservations
        var expiredLocal = _localReservations
            .Where(r => (now - r.Value.ReservedAt).TotalMilliseconds > _config.HealReservationExpiryMs)
            .Select(r => r.Key)
            .ToList();

        foreach (var key in expiredLocal)
        {
            _localReservations.Remove(key);
        }

        // Clean up remote reservations
        var expiredRemote = _remoteReservations
            .Where(r => (now - r.Value.ReservedAt).TotalMilliseconds > _config.HealReservationExpiryMs)
            .Select(r => r.Key)
            .ToList();

        foreach (var key in expiredRemote)
        {
            _remoteReservations.Remove(key);
        }
    }

    #endregion
}
