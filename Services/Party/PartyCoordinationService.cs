using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

    // Healer gauge state tracking
    private readonly Dictionary<Guid, RemoteHealerGaugeState> _remoteHealerGauges = new();

    // Healer role tracking
    private readonly Dictionary<Guid, RemoteHealerRole> _remoteHealerRoles = new();
    private uint _localJobId;
    private HealerRole _localDeclaredRole = HealerRole.Auto;

    // Ground effect tracking
    private readonly List<RemoteGroundEffect> _remoteGroundEffects = new();

    // Raise reservation tracking
    private readonly Dictionary<uint, RaiseReservation> _localRaiseReservations = new();
    private readonly Dictionary<uint, RaiseReservation> _remoteRaiseReservations = new();

    // Cleanse reservation tracking
    private readonly Dictionary<uint, CleanseReservation> _localCleanseReservations = new();
    private readonly Dictionary<uint, CleanseReservation> _remoteCleanseReservations = new();

    // Interrupt reservation tracking
    private readonly Dictionary<uint, InterruptReservation> _localInterruptReservations = new();
    private readonly Dictionary<uint, InterruptReservation> _remoteInterruptReservations = new();

    // Heartbeat timing
    private DateTime _lastHeartbeatSent = DateTime.MinValue;
    private DateTime _lastGaugeBroadcast = DateTime.MinValue;

    // Event callbacks for IPC layer
    public event Action<HeartbeatMessage>? OnHeartbeatReady;
    public event Action<HealIntentMessage>? OnHealIntentReady;
    public event Action<HealLandedMessage>? OnHealLandedReady;
    public event Action<CooldownUsedMessage>? OnCooldownUsedReady;
    public event Action<AoEHealIntentMessage>? OnAoEHealIntentReady;
    public event Action<RaidBuffIntentMessage>? OnRaidBuffIntentReady;
    public event Action<BurstWindowStartMessage>? OnBurstWindowStartReady;
    public event Action<GaugeStateMessage>? OnGaugeStateReady;
    public event Action<RoleDeclarationMessage>? OnRoleDeclarationReady;
    public event Action<GroundEffectPlacedMessage>? OnGroundEffectPlacedReady;
    public event Action<RaiseIntentMessage>? OnRaiseIntentReady;
    public event Action<CleanseIntentMessage>? OnCleanseIntentReady;
    public event Action<InterruptIntentMessage>? OnInterruptIntentReady;

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

    public bool WasInvulnerabilityUsedRecently(float withinSeconds = 5f)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableCooldownCoordination)
            return false;

        foreach (var kvp in _remoteCooldowns)
        {
            // Only check invulnerabilities
            if (!CoordinatedCooldowns.IsInvulnerability(kvp.Key))
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

    #region Healer Gauge Coordination

    public void BroadcastGaugeState(uint jobId, int primary, int secondary, int tertiary)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableHealerGaugeSharing)
            return;

        var message = new GaugeStateMessage(_instanceId, jobId, primary, secondary, tertiary);
        OnGaugeStateReady?.Invoke(message);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Broadcasting gauge state: {0}/{1}/{2}", primary, secondary, tertiary);
    }

    public RemoteHealerGaugeState? GetRemoteHealerGaugeState(Guid instanceId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableHealerGaugeSharing)
            return null;

        return _remoteHealerGauges.TryGetValue(instanceId, out var state) ? state : null;
    }

    public IReadOnlyList<RemoteHealerGaugeState> GetAllRemoteHealerGaugeStates()
    {
        if (!_config.EnablePartyCoordination || !_config.EnableHealerGaugeSharing)
            return Array.Empty<RemoteHealerGaugeState>();

        return _remoteHealerGauges.Values.ToList();
    }

    public bool IsAnyRemoteHealerResourceRich(int minimumPrimaryResource = 2)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableHealerGaugeSharing)
            return false;

        foreach (var state in _remoteHealerGauges.Values)
        {
            if (state.PrimaryResource >= minimumPrimaryResource)
                return true;
        }

        return false;
    }

    #endregion

    #region Healer Role Coordination

    public void DeclareHealerRole(uint jobId, HealerRole role)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableHealerRoleCoordination)
            return;

        _localJobId = jobId;
        _localDeclaredRole = role;

        var priority = GetHealerJobPriority(jobId);
        var message = new RoleDeclarationMessage(_instanceId, jobId, role, priority);
        OnRoleDeclarationReady?.Invoke(message);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Declared healer role: {0} (priority {1})", role, priority);
    }

    public bool IsPrimaryHealer
    {
        get
        {
            if (!_config.EnablePartyCoordination || !_config.EnableHealerRoleCoordination)
                return true; // Default to primary if coordination disabled

            // If explicitly set, use that
            if (_localDeclaredRole == HealerRole.Primary)
                return true;
            if (_localDeclaredRole == HealerRole.Secondary)
                return false;

            // Auto-determine based on job priority
            var localPriority = GetHealerJobPriority(_localJobId);

            foreach (var remoteRole in _remoteHealerRoles.Values)
            {
                // If any remote healer has higher priority (lower number), we're secondary
                if (remoteRole.JobPriority < localPriority)
                    return false;

                // If same priority, use instance ID as tiebreaker (lower ID = primary)
                if (remoteRole.JobPriority == localPriority &&
                    remoteRole.InstanceId.CompareTo(_instanceId) < 0)
                    return false;
            }

            return true;
        }
    }

    public RemoteHealerRole? GetRemoteHealerRole(Guid instanceId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableHealerRoleCoordination)
            return null;

        return _remoteHealerRoles.TryGetValue(instanceId, out var role) ? role : null;
    }

    public IReadOnlyList<RemoteHealerRole> GetAllRemoteHealerRoles()
    {
        if (!_config.EnablePartyCoordination || !_config.EnableHealerRoleCoordination)
            return Array.Empty<RemoteHealerRole>();

        return _remoteHealerRoles.Values.ToList();
    }

    public int GetHealerJobPriority(uint jobId)
    {
        // WHM has highest priority, SGE lowest
        // This matches typical healer pairing conventions
        return jobId switch
        {
            JobRegistry.WhiteMage or JobRegistry.Conjurer => 1,
            JobRegistry.Astrologian => 2,
            JobRegistry.Scholar or JobRegistry.Arcanist => 3,
            JobRegistry.Sage => 4,
            _ => 99, // Non-healers get lowest priority
        };
    }

    #endregion

    #region Ground Effect Coordination

    public bool WouldOverlapWithRemoteGroundEffect(Vector3 position, uint actionId, float overlapThreshold = 0.5f)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableGroundEffectCoordination)
            return false;

        var info = CoordinatedGroundEffects.GetEffectInfo(actionId);
        if (info == null)
            return false;

        var localRadius = info.Value.Radius;

        // Check all active remote ground effects
        foreach (var remote in _remoteGroundEffects)
        {
            if (remote.IsExpired)
                continue;

            // Calculate distance between centers
            var distance = Vector3.Distance(position, remote.Position);

            // Calculate overlap ratio
            // If distance < sum of radii, they overlap
            var sumOfRadii = localRadius + remote.Radius;
            if (distance < sumOfRadii)
            {
                // Calculate how much they overlap (0 = just touching, 1 = same position)
                var overlap = 1f - (distance / sumOfRadii);
                if (overlap >= overlapThreshold)
                {
                    if (_config.LogCoordinationEvents)
                        _log.Debug("[PartyCoord] Ground effect would overlap: {0} at ({1:F1},{2:F1},{3:F1}) overlaps {4:P0} with remote {5}",
                            actionId, position.X, position.Y, position.Z, overlap, remote.ActionId);
                    return true;
                }
            }
        }

        return false;
    }

    public void OnGroundEffectPlaced(uint actionId, Vector3 position)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableGroundEffectCoordination)
            return;

        var info = CoordinatedGroundEffects.GetEffectInfo(actionId);
        if (info == null)
            return;

        var message = new GroundEffectPlacedMessage(
            _instanceId,
            actionId,
            position.X,
            position.Y,
            position.Z,
            info.Value.Radius,
            info.Value.Duration);

        OnGroundEffectPlacedReady?.Invoke(message);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Ground effect placed: {0} at ({1:F1},{2:F1},{3:F1})",
                actionId, position.X, position.Y, position.Z);
    }

    public IReadOnlyList<RemoteGroundEffect> GetActiveRemoteGroundEffects()
    {
        if (!_config.EnablePartyCoordination || !_config.EnableGroundEffectCoordination)
            return Array.Empty<RemoteGroundEffect>();

        // Return only non-expired effects
        return _remoteGroundEffects.Where(e => !e.IsExpired).ToList();
    }

    public bool IsRemoteGroundEffectActiveNear(Vector3 position, float radius = 8f)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableGroundEffectCoordination)
            return false;

        foreach (var effect in _remoteGroundEffects)
        {
            if (effect.IsExpired)
                continue;

            var distance = Vector3.Distance(position, effect.Position);
            if (distance < radius + effect.Radius)
                return true;
        }

        return false;
    }

    #endregion

    #region Resurrection Coordination

    public bool IsRaiseTargetReservedByOther(uint entityId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaiseCoordination)
            return false;

        // Check remote reservations
        if (_remoteRaiseReservations.TryGetValue(entityId, out var reservation))
        {
            // Check if reservation is still valid
            if (!reservation.IsExpired)
                return true;

            // Expired, clean up
            _remoteRaiseReservations.Remove(entityId);
        }

        return false;
    }

    public bool ReserveRaiseTarget(uint entityId, uint actionId, int castTimeMs, bool usingSwiftcast)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaiseCoordination)
            return true;

        // Check if already reserved by remote with higher priority
        if (_remoteRaiseReservations.TryGetValue(entityId, out var remoteReservation))
        {
            if (!remoteReservation.IsExpired)
            {
                // Swiftcast raises take priority over hardcast raises
                if (remoteReservation.UsingSwiftcast && !usingSwiftcast)
                {
                    if (_config.LogCoordinationEvents)
                        _log.Debug("[PartyCoord] Raise target {0} reserved by remote with Swiftcast, skipping hardcast", entityId);
                    return false;
                }

                // If both are same type, first reservation wins
                if (remoteReservation.UsingSwiftcast == usingSwiftcast)
                {
                    if (_config.LogCoordinationEvents)
                        _log.Debug("[PartyCoord] Raise target {0} already reserved by remote, skipping", entityId);
                    return false;
                }

                // Our Swiftcast takes priority over their hardcast - proceed and take over
            }
        }

        var now = DateTime.UtcNow;

        // Create local reservation
        var reservation = new RaiseReservation
        {
            InstanceId = _instanceId,
            TargetEntityId = entityId,
            ActionId = actionId,
            ReservedAt = now,
            ExpectedCompletionTime = now.AddMilliseconds(castTimeMs),
            UsingSwiftcast = usingSwiftcast
        };

        _localRaiseReservations[entityId] = reservation;

        // Broadcast the intent
        var message = new RaiseIntentMessage(_instanceId, entityId, actionId, castTimeMs, usingSwiftcast);
        OnRaiseIntentReady?.Invoke(message);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Reserved raise target {0} (Swiftcast: {1}, cast time: {2}ms)",
                entityId, usingSwiftcast, castTimeMs);

        return true;
    }

    public void ClearRaiseReservation(uint entityId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableRaiseCoordination)
            return;

        _localRaiseReservations.Remove(entityId);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Cleared raise reservation for {0}", entityId);
    }

    public IReadOnlyDictionary<uint, RaiseReservation> GetRemoteRaiseReservations()
    {
        return _remoteRaiseReservations;
    }

    #endregion

    #region Cleanse Coordination

    public bool IsCleanseTargetReservedByOther(uint entityId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableCleanseCoordination)
            return false;

        // Check remote reservations
        if (_remoteCleanseReservations.TryGetValue(entityId, out var reservation))
        {
            // Check if reservation is still valid
            if (!reservation.IsExpired)
                return true;

            // Expired, clean up
            _remoteCleanseReservations.Remove(entityId);
        }

        return false;
    }

    public bool ReserveCleanseTarget(uint entityId, uint statusId, uint actionId, int debuffPriority)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableCleanseCoordination)
            return true;

        // Check if already reserved by remote
        if (IsCleanseTargetReservedByOther(entityId))
            return false;

        var now = DateTime.UtcNow;

        // Create local reservation
        var reservation = new CleanseReservation
        {
            InstanceId = _instanceId,
            TargetEntityId = entityId,
            StatusId = statusId,
            ReservedAt = now,
            ExpiresAt = now.AddMilliseconds(_config.CleanseReservationExpiryMs)
        };

        _localCleanseReservations[entityId] = reservation;

        // Broadcast the intent
        var message = new CleanseIntentMessage(_instanceId, entityId, statusId, actionId, debuffPriority);
        OnCleanseIntentReady?.Invoke(message);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Reserved cleanse target {0} (status {1}, priority {2})",
                entityId, statusId, debuffPriority);

        return true;
    }

    public void ClearCleanseReservation(uint entityId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableCleanseCoordination)
            return;

        _localCleanseReservations.Remove(entityId);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Cleared cleanse reservation for {0}", entityId);
    }

    public IReadOnlyDictionary<uint, CleanseReservation> GetRemoteCleanseReservations()
    {
        return _remoteCleanseReservations;
    }

    #endregion

    #region Interrupt Coordination

    public bool IsInterruptTargetReservedByOther(uint entityId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableInterruptCoordination)
            return false;

        // Check remote reservations
        if (_remoteInterruptReservations.TryGetValue(entityId, out var reservation))
        {
            // Check if reservation is still valid
            if (!reservation.IsExpired)
                return true;

            // Expired, clean up
            _remoteInterruptReservations.Remove(entityId);
        }

        return false;
    }

    public bool ReserveInterruptTarget(uint entityId, uint actionId, int castTimeMs)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableInterruptCoordination)
            return true;

        // Check if already reserved by remote
        if (IsInterruptTargetReservedByOther(entityId))
            return false;

        var now = DateTime.UtcNow;

        // Create local reservation - expires after enemy's remaining cast time + buffer
        var reservation = new InterruptReservation
        {
            InstanceId = _instanceId,
            TargetEntityId = entityId,
            ActionId = actionId,
            ReservedAt = now,
            ExpiresAt = now.AddMilliseconds(_config.InterruptReservationExpiryMs)
        };

        _localInterruptReservations[entityId] = reservation;

        // Broadcast the intent
        var message = new InterruptIntentMessage(_instanceId, entityId, actionId, castTimeMs);
        OnInterruptIntentReady?.Invoke(message);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Reserved interrupt target {0} (action {1}, cast time: {2}ms)",
                entityId, actionId, castTimeMs);

        return true;
    }

    public void ClearInterruptReservation(uint entityId)
    {
        if (!_config.EnablePartyCoordination || !_config.EnableInterruptCoordination)
            return;

        _localInterruptReservations.Remove(entityId);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Cleared interrupt reservation for {0}", entityId);
    }

    public IReadOnlyDictionary<uint, InterruptReservation> GetRemoteInterruptReservations()
    {
        return _remoteInterruptReservations;
    }

    #endregion

    public void Update(uint playerEntityId, uint jobId, bool isEnabled)
    {
        if (!_config.EnablePartyCoordination)
            return;

        _localJobId = jobId;
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

        // Clean up expired ground effects
        CleanupExpiredGroundEffects();

        // Clean up expired raise reservations
        CleanupExpiredRaiseReservations();

        // Clean up expired cleanse reservations
        CleanupExpiredCleanseReservations();

        // Clean up expired interrupt reservations
        CleanupExpiredInterruptReservations();
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
        _remoteHealerGauges.Clear();
        _remoteHealerRoles.Clear();
        _remoteGroundEffects.Clear();
        _localRaiseReservations.Clear();
        _remoteRaiseReservations.Clear();
        _localCleanseReservations.Clear();
        _remoteCleanseReservations.Clear();
        _localInterruptReservations.Clear();
        _remoteInterruptReservations.Clear();

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

        // Track party mitigations, personal defensives, and invulnerabilities for coordination
        var isPartyMitigation = CoordinatedCooldowns.IsCoordinatedCooldown(message.ActionId);
        var isPersonalDefensive = CoordinatedCooldowns.IsPersonalDefensive(message.ActionId);
        var isInvulnerability = CoordinatedCooldowns.IsInvulnerability(message.ActionId);

        if (!isPartyMitigation && !isPersonalDefensive && !isInvulnerability)
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

        var cdType = isInvulnerability ? "invulnerability" : (isPersonalDefensive ? "personal defensive" : "party mitigation");
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

    /// <summary>
    /// Handles incoming gauge state from a remote healer instance.
    /// </summary>
    public void HandleRemoteGaugeState(GaugeStateMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        if (!_config.EnableHealerGaugeSharing)
            return;

        if (_remoteHealerGauges.TryGetValue(message.InstanceId, out var existing))
        {
            existing.PrimaryResource = message.PrimaryResource;
            existing.SecondaryResource = message.SecondaryResource;
            existing.TertiaryResource = message.TertiaryResource;
            existing.LastUpdate = DateTime.UtcNow;
        }
        else
        {
            _remoteHealerGauges[message.InstanceId] = new RemoteHealerGaugeState
            {
                InstanceId = message.InstanceId,
                JobId = message.JobId,
                PrimaryResource = message.PrimaryResource,
                SecondaryResource = message.SecondaryResource,
                TertiaryResource = message.TertiaryResource,
                LastUpdate = DateTime.UtcNow
            };
        }

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Remote gauge state: {0}/{1}/{2} from instance {3}",
                message.PrimaryResource, message.SecondaryResource, message.TertiaryResource, message.InstanceId);
    }

    /// <summary>
    /// Handles incoming role declaration from a remote healer instance.
    /// </summary>
    public void HandleRemoteRoleDeclaration(RoleDeclarationMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        if (!_config.EnableHealerRoleCoordination)
            return;

        if (_remoteHealerRoles.TryGetValue(message.InstanceId, out var existing))
        {
            existing.Role = message.Role;
            existing.JobPriority = message.JobPriority;
            existing.LastUpdate = DateTime.UtcNow;
        }
        else
        {
            _remoteHealerRoles[message.InstanceId] = new RemoteHealerRole
            {
                InstanceId = message.InstanceId,
                JobId = message.JobId,
                Role = message.Role,
                JobPriority = message.JobPriority,
                LastUpdate = DateTime.UtcNow
            };
        }

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Remote role declaration: {0} (priority {1}) from instance {2}",
                message.Role, message.JobPriority, message.InstanceId);
    }

    /// <summary>
    /// Handles incoming ground effect placement from a remote healer instance.
    /// </summary>
    public void HandleRemoteGroundEffectPlaced(GroundEffectPlacedMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        if (!_config.EnableGroundEffectCoordination)
            return;

        var effect = new RemoteGroundEffect
        {
            InstanceId = message.InstanceId,
            ActionId = message.ActionId,
            Position = new Vector3(message.PositionX, message.PositionY, message.PositionZ),
            Radius = message.Radius,
            PlacedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(message.Duration)
        };

        // Remove any existing effect from the same instance with the same action
        _remoteGroundEffects.RemoveAll(e =>
            e.InstanceId == message.InstanceId && e.ActionId == message.ActionId);

        _remoteGroundEffects.Add(effect);

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Remote ground effect: {0} at ({1:F1},{2:F1},{3:F1}) from instance {4}",
                message.ActionId, message.PositionX, message.PositionY, message.PositionZ, message.InstanceId);
    }

    /// <summary>
    /// Handles incoming raise intent from a remote instance.
    /// </summary>
    public void HandleRemoteRaiseIntent(RaiseIntentMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        if (!_config.EnableRaiseCoordination)
            return;

        // Check if we already have a local reservation for this target
        if (_localRaiseReservations.TryGetValue(message.TargetEntityId, out var localReservation))
        {
            // If we're using Swiftcast and they're not, keep our reservation
            if (localReservation.UsingSwiftcast && !message.UsingSwiftcast)
            {
                if (_config.LogCoordinationEvents)
                    _log.Debug("[PartyCoord] Ignoring remote raise intent for {0} - we have Swiftcast priority",
                        message.TargetEntityId);
                return;
            }
        }

        var reservation = new RaiseReservation
        {
            InstanceId = message.InstanceId,
            TargetEntityId = message.TargetEntityId,
            ActionId = message.ActionId,
            ReservedAt = DateTime.UtcNow,
            ExpectedCompletionTime = DateTime.UtcNow.AddMilliseconds(message.CastTimeMs),
            UsingSwiftcast = message.UsingSwiftcast
        };

        _remoteRaiseReservations[message.TargetEntityId] = reservation;

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Remote raise reservation: target {0}, Swiftcast: {1}, cast time: {2}ms from instance {3}",
                message.TargetEntityId, message.UsingSwiftcast, message.CastTimeMs, message.InstanceId);
    }

    /// <summary>
    /// Handles incoming cleanse intent from a remote instance.
    /// </summary>
    public void HandleRemoteCleanseIntent(CleanseIntentMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        if (!_config.EnableCleanseCoordination)
            return;

        var reservation = new CleanseReservation
        {
            InstanceId = message.InstanceId,
            TargetEntityId = message.TargetEntityId,
            StatusId = message.StatusId,
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(_config.CleanseReservationExpiryMs)
        };

        _remoteCleanseReservations[message.TargetEntityId] = reservation;

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Remote cleanse reservation: target {0}, status {1}, priority {2} from instance {3}",
                message.TargetEntityId, message.StatusId, message.DebuffPriority, message.InstanceId);
    }

    /// <summary>
    /// Handles incoming interrupt intent from a remote instance.
    /// </summary>
    public void HandleRemoteInterruptIntent(InterruptIntentMessage message)
    {
        if (message.InstanceId == _instanceId)
            return;

        if (!_config.EnableInterruptCoordination)
            return;

        var reservation = new InterruptReservation
        {
            InstanceId = message.InstanceId,
            TargetEntityId = message.TargetEntityId,
            ActionId = message.ActionId,
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(_config.InterruptReservationExpiryMs)
        };

        _remoteInterruptReservations[message.TargetEntityId] = reservation;

        if (_config.LogCoordinationEvents)
            _log.Debug("[PartyCoord] Remote interrupt reservation: target {0}, action {1}, cast time: {2}ms from instance {3}",
                message.TargetEntityId, message.ActionId, message.CastTimeMs, message.InstanceId);
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

            // Remove gauge state from disconnected instance
            _remoteHealerGauges.Remove(id);

            // Remove role declaration from disconnected instance
            _remoteHealerRoles.Remove(id);

            // Remove ground effects from disconnected instance
            _remoteGroundEffects.RemoveAll(e => e.InstanceId == id);

            // Remove raise reservations from disconnected instance
            var raiseReservationsToRemove = _remoteRaiseReservations
                .Where(r => r.Value.InstanceId == id)
                .Select(r => r.Key)
                .ToList();

            foreach (var targetId in raiseReservationsToRemove)
            {
                _remoteRaiseReservations.Remove(targetId);
            }

            // Remove cleanse reservations from disconnected instance
            var cleanseReservationsToRemove = _remoteCleanseReservations
                .Where(r => r.Value.InstanceId == id)
                .Select(r => r.Key)
                .ToList();

            foreach (var targetId in cleanseReservationsToRemove)
            {
                _remoteCleanseReservations.Remove(targetId);
            }

            // Remove interrupt reservations from disconnected instance
            var interruptReservationsToRemove = _remoteInterruptReservations
                .Where(r => r.Value.InstanceId == id)
                .Select(r => r.Key)
                .ToList();

            foreach (var targetId in interruptReservationsToRemove)
            {
                _remoteInterruptReservations.Remove(targetId);
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

    private void CleanupExpiredGroundEffects()
    {
        _remoteGroundEffects.RemoveAll(e => e.IsExpired);
    }

    private void CleanupExpiredRaiseReservations()
    {
        // Clean up local raise reservations
        var expiredLocal = _localRaiseReservations
            .Where(r => r.Value.IsExpired)
            .Select(r => r.Key)
            .ToList();

        foreach (var key in expiredLocal)
        {
            _localRaiseReservations.Remove(key);
        }

        // Clean up remote raise reservations
        var expiredRemote = _remoteRaiseReservations
            .Where(r => r.Value.IsExpired)
            .Select(r => r.Key)
            .ToList();

        foreach (var key in expiredRemote)
        {
            _remoteRaiseReservations.Remove(key);
        }
    }

    private void CleanupExpiredCleanseReservations()
    {
        // Clean up local cleanse reservations
        var expiredLocal = _localCleanseReservations
            .Where(r => r.Value.IsExpired)
            .Select(r => r.Key)
            .ToList();

        foreach (var key in expiredLocal)
        {
            _localCleanseReservations.Remove(key);
        }

        // Clean up remote cleanse reservations
        var expiredRemote = _remoteCleanseReservations
            .Where(r => r.Value.IsExpired)
            .Select(r => r.Key)
            .ToList();

        foreach (var key in expiredRemote)
        {
            _remoteCleanseReservations.Remove(key);
        }
    }

    private void CleanupExpiredInterruptReservations()
    {
        // Clean up local interrupt reservations
        var expiredLocal = _localInterruptReservations
            .Where(r => r.Value.IsExpired)
            .Select(r => r.Key)
            .ToList();

        foreach (var key in expiredLocal)
        {
            _localInterruptReservations.Remove(key);
        }

        // Clean up remote interrupt reservations
        var expiredRemote = _remoteInterruptReservations
            .Where(r => r.Value.IsExpired)
            .Select(r => r.Key)
            .ToList();

        foreach (var key in expiredRemote)
        {
            _remoteInterruptReservations.Remove(key);
        }
    }

    #endregion
}
