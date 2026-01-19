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

    // Heartbeat timing
    private DateTime _lastHeartbeatSent = DateTime.MinValue;

    // Event callbacks for IPC layer
    public event Action<HeartbeatMessage>? OnHeartbeatReady;
    public event Action<HealIntentMessage>? OnHealIntentReady;
    public event Action<HealLandedMessage>? OnHealLandedReady;
    public event Action<CooldownUsedMessage>? OnCooldownUsedReady;

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

        // Only track coordinated cooldowns (party mitigations)
        if (!CoordinatedCooldowns.IsCoordinatedCooldown(message.ActionId))
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

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Tracked remote cooldown: action {0}, recast {1}ms from instance {2}",
                message.ActionId, info.RecastTimeMs, message.InstanceId);
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

            if (_config.LogCoordinationEvents)
                _log.Info("[PartyCoord] Remote instance timed out: {0}", id);
        }

        // Clean up expired cooldowns (no longer on recast)
        CleanupExpiredCooldowns();
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
