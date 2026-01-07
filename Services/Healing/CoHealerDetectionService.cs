using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Data;

namespace Olympus.Services.Healing;

/// <summary>
/// Detects and tracks co-healer presence and healing activity in the party.
/// </summary>
public sealed class CoHealerDetectionService : ICoHealerDetectionService, IDisposable
{
    private readonly ICombatEventService _combatEventService;
    private readonly IPartyList _partyList;
    private readonly IObjectTable _objectTable;
    private readonly HealingConfig _config;

    // Co-healer detection state
    private uint? _coHealerEntityId;
    private uint _coHealerJobId;

    // Healing tracking
    private readonly List<HealRecord> _coHealerHeals = new();
    private readonly Dictionary<uint, int> _pendingHeals = new();
    private const int MaxHealHistory = 50;
    private const float PendingHealDuration = 2.5f; // How long to expect a heal to land

    private DateTime _lastHealTime = DateTime.MinValue;

    private record HealRecord(DateTime Timestamp, uint TargetId, int Amount);

    public CoHealerDetectionService(
        ICombatEventService combatEventService,
        IPartyList partyList,
        IObjectTable objectTable,
        HealingConfig config)
    {
        _combatEventService = combatEventService;
        _partyList = partyList;
        _objectTable = objectTable;
        _config = config;

        // Subscribe to all heal events
        _combatEventService.OnAnyHealReceived += OnHealReceived;
    }

    public bool HasCoHealer => _coHealerEntityId.HasValue;
    public uint? CoHealerEntityId => _coHealerEntityId;
    public uint CoHealerJobId => _coHealerJobId;

    public bool IsCoHealerActive
    {
        get
        {
            if (!HasCoHealer) return false;
            var secondsSinceHeal = (float)(DateTime.UtcNow - _lastHealTime).TotalSeconds;
            return secondsSinceHeal <= _config.CoHealerActiveWindow;
        }
    }

    public float CoHealerHps
    {
        get
        {
            if (!HasCoHealer || _coHealerHeals.Count == 0) return 0f;

            // Calculate HPS over the active window
            var now = DateTime.UtcNow;
            var windowStart = now.AddSeconds(-_config.CoHealerActiveWindow);
            var totalHealing = 0;

            foreach (var heal in _coHealerHeals)
            {
                if (heal.Timestamp >= windowStart)
                    totalHealing += heal.Amount;
            }

            return totalHealing / _config.CoHealerActiveWindow;
        }
    }

    public IReadOnlyDictionary<uint, int> CoHealerPendingHeals => _pendingHeals;

    public float SecondsSinceLastHeal
    {
        get
        {
            if (_lastHealTime == DateTime.MinValue)
                return float.MaxValue;
            return (float)(DateTime.UtcNow - _lastHealTime).TotalSeconds;
        }
    }

    public void Update(uint localPlayerEntityId)
    {
        if (!_config.EnableCoHealerAwareness)
        {
            _coHealerEntityId = null;
            _coHealerJobId = 0;
            return;
        }

        // Scan party for co-healer (cached per frame via simple check)
        ScanPartyForCoHealer(localPlayerEntityId);

        // Clean up old pending heals
        CleanupPendingHeals();

        // Clean up old heal records
        CleanupHealHistory();
    }

    public void Clear()
    {
        _coHealerEntityId = null;
        _coHealerJobId = 0;
        _coHealerHeals.Clear();
        _pendingHeals.Clear();
        _lastHealTime = DateTime.MinValue;
    }

    private void ScanPartyForCoHealer(uint localPlayerEntityId)
    {
        _coHealerEntityId = null;
        _coHealerJobId = 0;

        // Only scan in 8-person content (party size > 4)
        if (_partyList.Length <= 4)
            return;

        foreach (var member in _partyList)
        {
            if (member == null || member.EntityId == localPlayerEntityId)
                continue;

            // Get the party member's character from object table for job info
            var character = _objectTable.SearchById(member.EntityId) as IPlayerCharacter;
            if (character == null)
                continue;

            var jobId = character.ClassJob.RowId;
            if (JobRegistry.IsHealer(jobId))
            {
                _coHealerEntityId = member.EntityId;
                _coHealerJobId = jobId;
                break; // Found co-healer
            }
        }
    }

    private void OnHealReceived(uint healerEntityId, uint targetEntityId, int amount)
    {
        // Only track heals from the co-healer
        if (!HasCoHealer || healerEntityId != _coHealerEntityId)
            return;

        // Record the heal
        var now = DateTime.UtcNow;
        _lastHealTime = now;

        _coHealerHeals.Add(new HealRecord(now, targetEntityId, amount));
        if (_coHealerHeals.Count > MaxHealHistory)
            _coHealerHeals.RemoveAt(0);

        // When a heal lands, we can assume any "pending" heal for this target has resolved
        // But also add a small pending estimate for potential follow-up heals
        // This is a simple heuristic - co-healer just healed, they might heal again soon
        if (_pendingHeals.ContainsKey(targetEntityId))
        {
            _pendingHeals.Remove(targetEntityId);
        }

        // Add a small pending estimate (assume they might cast another heal)
        // This is an approximation - actual pending heal tracking would require
        // intercepting cast bar data, which is more complex
        var estimatedFollowUp = (int)(amount * 0.3f); // 30% of last heal
        if (estimatedFollowUp > 0)
        {
            _pendingHeals[targetEntityId] = estimatedFollowUp;
        }
    }

    private void CleanupPendingHeals()
    {
        // Pending heals expire after PendingHealDuration seconds
        // Since we don't have actual cast tracking, we just decay them over time
        var keysToRemove = new List<uint>();
        var now = DateTime.UtcNow;

        foreach (var kvp in _pendingHeals)
        {
            // Simple decay: if co-healer hasn't healed this target recently, remove pending
            var hasRecentHeal = false;
            foreach (var heal in _coHealerHeals)
            {
                if (heal.TargetId == kvp.Key &&
                    (now - heal.Timestamp).TotalSeconds < PendingHealDuration)
                {
                    hasRecentHeal = true;
                    break;
                }
            }

            if (!hasRecentHeal)
                keysToRemove.Add(kvp.Key);
        }

        foreach (var key in keysToRemove)
            _pendingHeals.Remove(key);
    }

    private void CleanupHealHistory()
    {
        // Remove heals older than the active window
        var cutoff = DateTime.UtcNow.AddSeconds(-_config.CoHealerActiveWindow - 5);
        _coHealerHeals.RemoveAll(h => h.Timestamp < cutoff);
    }

    public void Dispose()
    {
        _combatEventService.OnAnyHealReceived -= OnHealReceived;
    }
}
