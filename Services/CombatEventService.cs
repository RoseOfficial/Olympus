using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Olympus.Services.Calculation;

namespace Olympus.Services;

/// <summary>
/// Record of a healing event from the local player.
/// </summary>
public record HealEvent(DateTime Timestamp, uint TargetId, string TargetName, uint ActionId, int Amount, int OverhealAmount);

/// <summary>
/// Hooks into ActionEffectHandler.Receive to track HP changes in real-time,
/// before the game's visible HP bars update.
/// </summary>
public sealed unsafe class CombatEventService : ICombatEventService, IDisposable
{
    private readonly IPluginLog log;
    private readonly IObjectTable objectTable;
    private readonly Hook<ActionEffectHandler.Delegates.Receive>? receiveHook;

    /// <summary>
    /// Event raised when a healing effect from the local player lands.
    /// The uint parameter is the target entity ID that received the heal.
    /// Subscribers can use this to clear pending heals for that target.
    /// </summary>
    public event System.Action<uint>? OnLocalPlayerHealLanded;

    /// <summary>
    /// Event raised when damage is received by any party member.
    /// Used by DamageIntakeService to track damage patterns for healing triage.
    /// Parameters: (entityId, damageAmount)
    /// </summary>
    public event System.Action<uint, int>? OnDamageReceived;

    /// <summary>
    /// Event raised when any heal effect lands (from any source, not just local player).
    /// Used by CoHealerDetectionService to track other healers' healing output.
    /// Parameters: (healerEntityId, targetEntityId, healAmount)
    /// </summary>
    public event System.Action<uint, uint, int>? OnAnyHealReceived;

    /// <summary>
    /// Event raised when any ability is used (action effect resolves).
    /// Used by TimelineService for timeline sync.
    /// Parameters: (sourceEntityId, actionId)
    /// </summary>
    public event System.Action<uint, uint>? OnAbilityUsed;

    // Shadow HP tracking: EntityId -> (CurrentHp, LastActionUpdate)
    // LastActionUpdate is set when HP changes from action effects (heal/damage)
    // to prevent InitializeHp from overwriting before game HP catches up
    private readonly ConcurrentDictionary<uint, (uint Hp, DateTime LastActionUpdate)> shadowHp = new();

    // How long to protect shadow HP from being overwritten by InitializeHp after an action effect
    // This prevents double-casting heals due to race condition with game HP updates
    private const int ActionUpdateProtectionMs = 3000;

    // Recent heals from local player
    private readonly List<HealEvent> recentHeals = new();
    private const int MaxHealHistory = 20;
    private readonly object healLock = new();

    // Overheal statistics tracking (session only)
    private readonly Dictionary<uint, SpellOverhealStats> spellOverhealStats = new();
    private readonly Dictionary<uint, TargetOverhealStats> targetOverhealStats = new();
    private readonly List<OverhealEvent> recentOverhealEvents = new();
    private const int MaxOverhealHistory = 50;
    private readonly object overhealLock = new();
    private DateTime sessionStartTime = DateTime.Now;

    // Internal tracking classes for overheal statistics
    private sealed class SpellOverhealStats
    {
        public string SpellName { get; set; } = "";
        public int TotalHealing { get; set; }
        public int TotalOverheal { get; set; }
        public int CastCount { get; set; }
    }

    private sealed class TargetOverhealStats
    {
        public string TargetName { get; set; } = "";
        public int TotalHealing { get; set; }
        public int TotalOverheal { get; set; }
        public int HealCount { get; set; }
    }

    /// <summary>
    /// An overheal event for the timeline.
    /// </summary>
    public record OverhealEvent(DateTime Timestamp, string SpellName, string TargetName, int HealAmount, int OverhealAmount);

    // For calibration: store the last predicted heal (raw, without correction factor)
    private int _lastPredictedHealRaw;
    private DateTime _lastPredictionTime;

    // Combat duration tracking
    private DateTime? _combatStartTime;
    private bool _isInCombat;

    // ActionEffectType values from FFXIVClientStructs
    private const byte EffectTypeDamage = 3;
    private const byte EffectTypeHeal = 4;

    public CombatEventService(IGameInteropProvider gameInterop, IPluginLog log, IObjectTable objectTable)
    {
        this.log = log;
        this.objectTable = objectTable;

        try
        {
            receiveHook = gameInterop.HookFromAddress<ActionEffectHandler.Delegates.Receive>(
                (nint)ActionEffectHandler.MemberFunctionPointers.Receive,
                ReceiveDetour);
            receiveHook.Enable();
            log.Info("CombatEventService: ActionEffect hook enabled");
        }
        catch (Exception ex)
        {
            log.Error(ex, "CombatEventService: Failed to create ActionEffect hook");
        }
    }


    /// <summary>
    /// Register a predicted heal (raw value without correction factor) for calibration.
    /// When the actual heal arrives, it will be used to calibrate the formula.
    /// </summary>
    public void RegisterPredictionForCalibration(int rawPredictedHeal)
    {
        Interlocked.Exchange(ref _lastPredictedHealRaw, rawPredictedHeal);
        _lastPredictionTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the shadow HP for an entity, or the fallback value if not tracked.
    /// </summary>
    public uint GetShadowHp(uint entityId, uint fallbackHp)
        => shadowHp.TryGetValue(entityId, out var entry) ? entry.Hp : fallbackHp;

    /// <summary>
    /// Initializes or updates the shadow HP for an entity.
    /// Call this each frame to ensure new party members are tracked.
    /// Respects timestamp protection: won't overwrite HP that was recently updated by action effects.
    /// </summary>
    public void InitializeHp(uint entityId, uint currentHp)
    {
        if (shadowHp.TryGetValue(entityId, out var existing))
        {
            // Skip if recently updated by action effect (heal/damage)
            // This prevents race condition where game HP hasn't caught up yet
            var timeSinceAction = (DateTime.UtcNow - existing.LastActionUpdate).TotalMilliseconds;
            if (timeSinceAction < ActionUpdateProtectionMs)
                return;

            // Only update if HP changed (avoids dictionary writes when values are stable)
            if (existing.Hp == currentHp)
                return;
        }

        // Initialize with MinValue timestamp (not from action effect)
        shadowHp[entityId] = (currentHp, DateTime.MinValue);
    }

    /// <summary>
    /// Gets all currently tracked shadow HP values.
    /// </summary>
    public IEnumerable<(uint EntityId, uint Hp)> GetAllShadowHp()
        => shadowHp.Select(kvp => (kvp.Key, kvp.Value.Hp));

    /// <summary>
    /// Clears all tracked shadow HP. Call on zone transitions.
    /// </summary>
    public void Clear()
        => shadowHp.Clear();

    /// <summary>
    /// Gets recent healing events from the local player.
    /// </summary>
    public IReadOnlyList<HealEvent> GetRecentHeals()
    {
        lock (healLock)
        {
            return recentHeals.ToList();
        }
    }

    /// <summary>
    /// Clears the heal history.
    /// </summary>
    public void ClearHeals()
    {
        lock (healLock)
        {
            recentHeals.Clear();
        }
    }

    /// <summary>
    /// Gets aggregated overheal statistics for the current session.
    /// </summary>
    public OverhealStatistics GetOverhealStatistics()
    {
        lock (overhealLock)
        {
            var totalHealing = 0;
            var totalOverheal = 0;

            var bySpell = new List<(uint ActionId, string SpellName, int TotalHealing, int TotalOverheal, int CastCount)>();
            foreach (var kvp in spellOverhealStats)
            {
                totalHealing += kvp.Value.TotalHealing;
                totalOverheal += kvp.Value.TotalOverheal;
                bySpell.Add((kvp.Key, kvp.Value.SpellName, kvp.Value.TotalHealing, kvp.Value.TotalOverheal, kvp.Value.CastCount));
            }

            var byTarget = new List<(uint TargetId, string TargetName, int TotalHealing, int TotalOverheal, int HealCount)>();
            foreach (var kvp in targetOverhealStats)
            {
                byTarget.Add((kvp.Key, kvp.Value.TargetName, kvp.Value.TotalHealing, kvp.Value.TotalOverheal, kvp.Value.HealCount));
            }

            var recentEvents = recentOverhealEvents.ToList();

            return new OverhealStatistics(
                sessionStartTime,
                totalHealing,
                totalOverheal,
                bySpell,
                byTarget,
                recentEvents);
        }
    }

    /// <summary>
    /// Resets all overheal statistics for a new session.
    /// </summary>
    public void ResetOverhealStatistics()
    {
        lock (overhealLock)
        {
            spellOverhealStats.Clear();
            targetOverhealStats.Clear();
            recentOverhealEvents.Clear();
            sessionStartTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Aggregated overheal statistics for display.
    /// </summary>
    public record OverhealStatistics(
        DateTime SessionStartTime,
        int TotalHealing,
        int TotalOverheal,
        List<(uint ActionId, string SpellName, int TotalHealing, int TotalOverheal, int CastCount)> BySpell,
        List<(uint TargetId, string TargetName, int TotalHealing, int TotalOverheal, int HealCount)> ByTarget,
        List<OverhealEvent> RecentOverhealEvents)
    {
        public float OverhealPercent => TotalHealing > 0 ? (float)TotalOverheal / TotalHealing * 100f : 0f;
        public TimeSpan SessionDuration => DateTime.Now - SessionStartTime;
    }

    private void ReceiveDetour(
        uint casterEntityId,
        Character* casterPtr,
        Vector3* targetPos,
        ActionEffectHandler.Header* header,
        ActionEffectHandler.TargetEffects* effects,
        GameObjectId* targetEntityIds)
    {
        try
        {
            ProcessEffects(casterEntityId, header, effects, targetEntityIds);
        }
        catch (Exception ex)
        {
            log.Error(ex, "CombatEventService: Error processing action effects");
        }

        // Always call original
        receiveHook!.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);
    }

    private void ProcessEffects(
        uint casterEntityId,
        ActionEffectHandler.Header* header,
        ActionEffectHandler.TargetEffects* effects,
        GameObjectId* targetEntityIds)
    {
        var localPlayer = objectTable.LocalPlayer;
        var isFromLocalPlayer = localPlayer != null && casterEntityId == localPlayer.EntityId;

        for (var i = 0; i < header->NumTargets; i++)
        {
            var targetId = (uint)targetEntityIds[i].ObjectId;
            var targetEffects = effects[i];

            var totalDelta = 0;
            var totalHeal = 0;
            for (var j = 0; j < 8; j++)
            {
                var effect = targetEffects.Effects[j];

                switch (effect.Type)
                {
                    case EffectTypeDamage:
                        totalDelta -= effect.Value;
                        break;
                    case EffectTypeHeal:
                        totalDelta += effect.Value;
                        totalHeal += effect.Value;
                        break;
                }
            }

            if (totalDelta != 0 && shadowHp.TryGetValue(targetId, out var current))
            {
                var newHp = (uint)Math.Max(0, (int)current.Hp + totalDelta);
                // Set timestamp to protect this value from being overwritten by InitializeHp
                shadowHp[targetId] = (newHp, DateTime.UtcNow);
            }

            // Raise damage received event for damage intake tracking
            if (totalDelta < 0)
            {
                OnDamageReceived?.Invoke(targetId, -totalDelta);
            }

            // Raise heal received event for ALL heals (co-healer tracking)
            if (totalHeal > 0)
            {
                OnAnyHealReceived?.Invoke(casterEntityId, targetId, totalHeal);
            }

            // Track heals from local player
            if (isFromLocalPlayer && totalHeal > 0)
            {
                var targetName = "Unknown";
                uint targetMaxHp = 0;
                uint targetCurrentHp = 0;
                var targetObj = objectTable.SearchById(targetId);
                if (targetObj != null)
                {
                    targetName = targetObj.Name.TextValue;
                    if (targetObj is Dalamud.Game.ClientState.Objects.Types.ICharacter character)
                    {
                        targetMaxHp = character.MaxHp;
                        // HP before heal = current HP (game hasn't updated yet)
                        targetCurrentHp = character.CurrentHp;
                    }
                }

                // Calculate overheal: how much exceeded target's missing HP
                var overhealAmount = 0;
                if (targetMaxHp > 0)
                {
                    var missingHp = (int)(targetMaxHp - targetCurrentHp);
                    overhealAmount = Math.Max(0, totalHeal - missingHp);
                }

                var healEvent = new HealEvent(
                    DateTime.Now,
                    targetId,
                    targetName,
                    header->ActionId,
                    totalHeal,
                    overhealAmount);

                lock (healLock)
                {
                    recentHeals.Insert(0, healEvent);
                    if (recentHeals.Count > MaxHealHistory)
                        recentHeals.RemoveAt(recentHeals.Count - 1);
                }

                // Track overheal statistics
                lock (overhealLock)
                {
                    // Per-spell tracking
                    if (!spellOverhealStats.TryGetValue(header->ActionId, out var spellStats))
                    {
                        spellStats = new SpellOverhealStats { SpellName = $"Action{header->ActionId}" };
                        spellOverhealStats[header->ActionId] = spellStats;
                    }
                    spellStats.TotalHealing += totalHeal;
                    spellStats.TotalOverheal += overhealAmount;
                    spellStats.CastCount++;

                    // Per-target tracking
                    if (!targetOverhealStats.TryGetValue(targetId, out var targetStats))
                    {
                        targetStats = new TargetOverhealStats { TargetName = targetName };
                        targetOverhealStats[targetId] = targetStats;
                    }
                    targetStats.TargetName = targetName; // Update name in case it changed
                    targetStats.TotalHealing += totalHeal;
                    targetStats.TotalOverheal += overhealAmount;
                    targetStats.HealCount++;

                    // Add to overheal events timeline (only if there was overheal)
                    if (overhealAmount > 0)
                    {
                        recentOverhealEvents.Insert(0, new OverhealEvent(
                            DateTime.Now,
                            $"Action{header->ActionId}",
                            targetName,
                            totalHeal,
                            overhealAmount));

                        if (recentOverhealEvents.Count > MaxOverhealHistory)
                            recentOverhealEvents.RemoveAt(recentOverhealEvents.Count - 1);
                    }
                }
            }

            // When our heal effect lands, notify subscribers and calibrate
            if (isFromLocalPlayer && totalHeal > 0)
            {
                // Raise event so HpPredictionService can clear pending heals for this target
                OnLocalPlayerHealLanded?.Invoke(targetId);

                // Calibrate if we have a recent prediction (within 3 seconds)
                var timeSincePrediction = (DateTime.UtcNow - _lastPredictionTime).TotalSeconds;
                var predictedHeal = Interlocked.Exchange(ref _lastPredictedHealRaw, 0);
                if (predictedHeal > 0 && timeSincePrediction < 3.0)
                {
                    HealingCalculator.CalibrateFromActual(predictedHeal, totalHeal);
                }
            }
        }

        // Raise ability used event for timeline sync (once per action, not per target)
        OnAbilityUsed?.Invoke(casterEntityId, header->ActionId);
    }

    /// <summary>
    /// Updates the combat state. Call this when entering or leaving combat.
    /// </summary>
    public void UpdateCombatState(bool inCombat)
    {
        if (inCombat && !_isInCombat)
        {
            // Entering combat
            _combatStartTime = DateTime.UtcNow;
            _isInCombat = true;
        }
        else if (!inCombat && _isInCombat)
        {
            // Leaving combat
            _combatStartTime = null;
            _isInCombat = false;
        }
    }

    /// <summary>
    /// Gets the duration of the current combat in seconds.
    /// Returns 0 if not in combat.
    /// </summary>
    public float GetCombatDurationSeconds()
    {
        if (!_isInCombat || !_combatStartTime.HasValue)
            return 0f;

        return (float)(DateTime.UtcNow - _combatStartTime.Value).TotalSeconds;
    }

    /// <summary>
    /// Whether the player is currently in combat.
    /// </summary>
    public bool IsInCombat => _isInCombat;

    public void Dispose()
    {
        receiveHook?.Dispose();
        shadowHp.Clear();
        log.Info("CombatEventService: Disposed");
    }
}
