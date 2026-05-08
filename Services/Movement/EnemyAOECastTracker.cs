using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Services.Movement.Geometry;

namespace Olympus.Services.Movement;

/// <summary>
/// Tracks active enemy AoE casts by polling <see cref="IObjectTable"/> each frame.
/// Decodes geometry via <see cref="GuessShape"/> and exposes <see cref="ActiveAOEs"/>
/// for movement modules to query.
/// </summary>
/// <remarks>
/// Path B implementation: self-contained per-frame polling. Extending
/// <see cref="ICombatEventService"/> for enemy cast events was not done because
/// CombatEventService owns a native hook (ActionEffectHandler.Receive) which fires on
/// action resolution — not on cast-bar start. Adding a second polling loop to that
/// service would mix two unrelated data-gathering patterns. The tracker owns its own
/// polling cleanly.
/// </remarks>
public class EnemyAOECastTracker : IEnemyAOECastTracker, IDisposable
{
    /// <summary>
    /// Circles/shapes with EffectRange at or above this value are treated as
    /// raidwides and are not tracked as avoidable AoEs.
    /// </summary>
    private const float RaidwideRangeYalms = 30f;

    /// <summary>
    /// How long after ResolveAt to keep an entry before pruning it.
    /// Handles brief latency between cast completing and HandleCastFinished call.
    /// </summary>
    private const float StaleGracePeriodSeconds = 0.5f;

    private readonly Dictionary<ulong, TrackedAOE> entries = new();

    // Per-frame polling state: entity ID -> last-seen cast action ID (0 = not casting)
    private readonly Dictionary<ulong, uint> previousCastActionId = new();

    private readonly IPluginLog? log;
    private readonly IObjectTable? objectTable;
    private readonly IClientState? clientState;
    private readonly IDataManager? dataManager;

    public IReadOnlyList<TrackedAOE> ActiveAOEs
    {
        get
        {
            var list = new List<TrackedAOE>(entries.Count);
            foreach (var kvp in entries)
                list.Add(kvp.Value);
            return list;
        }
    }

    public EnemyAOECastTracker(IPluginLog log, IObjectTable objectTable, IClientState clientState, IDataManager? dataManager = null)
    {
        this.log = log;
        this.objectTable = objectTable;
        this.clientState = clientState;
        this.dataManager = dataManager;

        if (clientState != null)
            clientState.TerritoryChanged += OnTerritoryChanged;
    }

    /// <summary>
    /// Call once per frame (from Plugin.OnFrameworkUpdate) to detect new and finished enemy casts.
    /// </summary>
    public void Update(DateTime now)
    {
        PruneStaleEntries(now);
        PollObjectTable();
    }

    public void Dispose()
    {
        if (clientState != null)
            clientState.TerritoryChanged -= OnTerritoryChanged;
        ClearAll();
    }

    private void OnTerritoryChanged(ushort _) => ClearAll();

    // Two overloads — same dual-signature pattern as Plugin.OnTerritoryChanged — so either
    // Action<ushort> or Action<uint> delegate form compiles against the Dalamud SDK in use.
    private void OnTerritoryChanged(uint _) => ClearAll();

    private void PollObjectTable()
    {
        if (objectTable == null) return;

        var seen = new HashSet<ulong>();

        foreach (var obj in objectTable)
        {
            if (obj is not IBattleNpc npc) continue;
            if (npc.GameObjectId == 0) continue;

            var id = npc.GameObjectId;
            seen.Add(id);

            var currentCastId = npc.IsCasting ? npc.CastActionId : 0u;
            previousCastActionId.TryGetValue(id, out var prevCastId);

            if (currentCastId != 0 && prevCastId == 0)
            {
                // Cast just started
                var castTimeRemaining = npc.TotalCastTime - npc.CurrentCastTime;
                if (castTimeRemaining <= 0f)
                    castTimeRemaining = npc.TotalCastTime;

                var pos2D = new Vector2(npc.Position.X, npc.Position.Z);
                var hitboxRadius = npc.HitboxRadius;

                DecodeAndTrack(id, currentCastId, pos2D, npc.Rotation, castTimeRemaining, hitboxRadius);
            }
            else if (currentCastId == 0 && prevCastId != 0)
            {
                // Cast ended (interrupted or resolved)
                HandleCastFinished(id);
            }

            previousCastActionId[id] = currentCastId;
        }

        // Remove tracking state for entities that left the object table
        var toRemove = new List<ulong>();
        foreach (var id in previousCastActionId.Keys)
        {
            if (!seen.Contains(id))
                toRemove.Add(id);
        }
        foreach (var id in toRemove)
        {
            previousCastActionId.Remove(id);
            entries.Remove(id);
        }
    }

    private void DecodeAndTrack(ulong casterId, uint actionId, Vector2 origin, float rotation,
        float castTimeRemainingSeconds, float casterHitboxRadius)
    {
        if (dataManager == null) return;

        var actionRow = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>()?.GetRowOrDefault(actionId);
        if (!actionRow.HasValue) return;

        var action = actionRow.Value;
        var castType = action.CastType;
        var effectRange = (float)action.EffectRange;
        var xAxisModifier = (float)action.XAxisModifier;
        var omenPath = action.Omen.ValueNullable?.Path.ToString() ?? "";

        HandleCastStarted(casterId, castType, effectRange, xAxisModifier, omenPath,
            casterHitboxRadius, origin, rotation, castTimeRemainingSeconds);
    }

    /// <summary>
    /// Test seam: add a tracked AoE directly, bypassing IObjectTable polling and IDataManager.
    /// </summary>
    protected internal void HandleCastStarted(
        ulong casterId,
        byte castType,
        float effectRange,
        float xAxisModifier,
        string omenPath,
        float casterHitboxRadius,
        Vector2 origin,
        float rotation,
        float castTimeRemainingSeconds)
    {
        if (effectRange >= RaidwideRangeYalms)
            return;

        var shape = GuessShape.From(castType, effectRange, xAxisModifier, omenPath, casterHitboxRadius);
        if (shape == null)
            return;

        entries[casterId] = new TrackedAOE(
            casterId,
            origin,
            rotation,
            shape,
            DateTime.UtcNow.AddSeconds(castTimeRemainingSeconds));
    }

    /// <summary>
    /// Test seam: remove a tracked AoE by caster ID.
    /// </summary>
    protected internal void HandleCastFinished(ulong casterId) => entries.Remove(casterId);

    /// <summary>
    /// Test seam: remove all entries whose ResolveAt + grace period has passed.
    /// </summary>
    protected internal void PruneStaleEntries(DateTime now)
    {
        var stale = new List<ulong>();
        foreach (var kvp in entries)
        {
            if (kvp.Value.ResolveAt.AddSeconds(StaleGracePeriodSeconds) < now)
                stale.Add(kvp.Key);
        }
        foreach (var id in stale)
            entries.Remove(id);
    }

    /// <summary>
    /// Test seam: remove all active AoEs (called on territory change).
    /// </summary>
    protected internal void ClearAll()
    {
        entries.Clear();
        previousCastActionId.Clear();
    }
}
