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
public record HealEvent(DateTime Timestamp, uint TargetId, string TargetName, uint ActionId, int Amount);

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
    /// Subscribers can use this to clear pending heals or perform other actions.
    /// </summary>
    public event System.Action? OnLocalPlayerHealLanded;

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

    // For calibration: store the last predicted heal (raw, without correction factor)
    private int _lastPredictedHealRaw;
    private DateTime _lastPredictionTime;

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

            // Track heals from local player
            if (isFromLocalPlayer && totalHeal > 0)
            {
                var targetName = "Unknown";
                var targetObj = objectTable.SearchById(targetId);
                if (targetObj != null)
                    targetName = targetObj.Name.TextValue;

                var healEvent = new HealEvent(
                    DateTime.Now,
                    targetId,
                    targetName,
                    header->ActionId,
                    totalHeal);

                lock (healLock)
                {
                    recentHeals.Insert(0, healEvent);
                    if (recentHeals.Count > MaxHealHistory)
                        recentHeals.RemoveAt(recentHeals.Count - 1);
                }
            }

            // When our heal effect lands, notify subscribers and calibrate
            if (isFromLocalPlayer && totalHeal > 0)
            {
                // Raise event so HpPredictionService can clear pending heals
                OnLocalPlayerHealLanded?.Invoke();

                // Calibrate if we have a recent prediction (within 3 seconds)
                var timeSincePrediction = (DateTime.UtcNow - _lastPredictionTime).TotalSeconds;
                var predictedHeal = Interlocked.Exchange(ref _lastPredictedHealRaw, 0);
                if (predictedHeal > 0 && timeSincePrediction < 3.0)
                {
                    HealingCalculator.CalibrateFromActual(predictedHeal, totalHeal);
                }
            }
        }
    }

    public void Dispose()
    {
        receiveHook?.Dispose();
        shadowHp.Clear();
        log.Info("CombatEventService: Disposed");
    }
}
