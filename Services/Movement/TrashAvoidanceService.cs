using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Services.Movement.Geometry;
using Olympus.Services.Movement.Humanization;
using Olympus.Services.Movement.Probes;

namespace Olympus.Services.Movement;

public class TrashAvoidanceService : ITrashAvoidanceService
{
    private const int SampleDirections = 16;
    private const float SampleRadius = 5f;

    private readonly IRMIWalkHookService hook;
    private readonly IEnemyAOECastTracker tracker;
    private readonly IBossCombatDetector boss;
    private readonly IBGCollisionProbe collision;
    private readonly IMovementClock clock;
    private readonly Func<MovementConfig> configAccessor;
    private readonly IPluginLog? log;
    private readonly IClientState? clientState;

    private readonly Dictionary<ulong, DateTime> firstSeenPerCast = new();
    private readonly Dictionary<ulong, int> reactionDelayPerCast = new();
    private readonly Dictionary<ulong, float> arrivalTolerancePerCast = new();
    private DateTime lastDodgeCompleted = DateTime.MinValue;

    public TrashAvoidanceService(IRMIWalkHookService hook, IEnemyAOECastTracker tracker,
        IBossCombatDetector boss, IBGCollisionProbe collision, IMovementClock clock,
        Func<MovementConfig> configAccessor, IPluginLog log,
        IClientState? clientState = null)
    {
        this.hook = hook;
        this.tracker = tracker;
        this.boss = boss;
        this.collision = collision;
        this.clock = clock;
        this.configAccessor = configAccessor;
        this.log = log;
        this.clientState = clientState;

        if (clientState != null)
            clientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose()
    {
        if (clientState != null)
            clientState.TerritoryChanged -= OnTerritoryChanged;
    }

    internal bool HasFirstSeenEntry(ulong casterId) => firstSeenPerCast.ContainsKey(casterId);

    internal void OnTerritoryChanged(ushort _)
    {
        firstSeenPerCast.Clear();
        reactionDelayPerCast.Clear();
        arrivalTolerancePerCast.Clear();
        lastDodgeCompleted = DateTime.MinValue;
    }

    // Two overloads -- same dual-signature pattern as Plugin.OnTerritoryChanged / EnemyAOECastTracker --
    // so either Action<ushort> or Action<uint> delegate form compiles against the Dalamud SDK in use.
    private void OnTerritoryChanged(uint id) => OnTerritoryChanged((ushort)id);

    public void Update()
    {
        PrunePerCastStateAgainstTracker();

        var cfg = configAccessor();

        if (!cfg.EnableTrashAoEAvoidance) { ClearVector(); return; }
        if (!hook.HookInstalled) { ClearVector(); return; }
        if (IsHighEndZone()) { ClearVector(); return; }
        if (boss.IsBossEngaged) { ClearVector(); return; }
        if (IsPlayerUnavailable()) { ClearVector(); return; }

        var now = clock.UtcNow;
        var playerPos2D = GetPlayerPos2D();
        var playerPos3D = GetPlayerPos3D();

        // Threat assessment: filter to threats containing the player
        var allThreats = tracker.ActiveAOEs;
        var activeThreats = new List<TrackedAOE>();
        foreach (var t in allThreats)
        {
            if (t.ResolveAt <= now) continue;
            var distToCaster = Vector2.Distance(t.Origin, playerPos2D);
            if (distToCaster > cfg.MaxThreatRangeYalms) continue;
            if (!t.Shape.Contains(t.Origin, t.RotationRadians, playerPos2D)) continue;
            activeThreats.Add(t);
        }

        if (activeThreats.Count == 0) { ClearVector(); return; }

        // Reaction gate: track first-seen, decide if past delay
        var earliestFirstSeen = DateTime.MaxValue;
        var earliestCasterId = activeThreats[0].CasterId;
        foreach (var t in activeThreats)
        {
            if (!firstSeenPerCast.TryGetValue(t.CasterId, out var seen))
            {
                seen = now;
                firstSeenPerCast[t.CasterId] = seen;
                reactionDelayPerCast[t.CasterId] = ReactionDelay.Compute(t.CasterId, cfg.ReactionDelayMinMs, cfg.ReactionDelayMaxMs);
                arrivalTolerancePerCast[t.CasterId] = ArrivalTolerance.Compute(t.CasterId, cfg.ArrivalToleranceMinYalms, cfg.ArrivalToleranceMaxYalms);
            }
            if (seen < earliestFirstSeen)
            {
                earliestFirstSeen = seen;
                earliestCasterId = t.CasterId;
            }
        }

        var reactionDelayMs = reactionDelayPerCast[earliestCasterId];
        if ((now - earliestFirstSeen).TotalMilliseconds < reactionDelayMs)
        {
            ClearVector(); return;
        }

        // Inter-cast pause (skip unless any threat resolves within 0.5s)
        var interCastPauseMs = (cfg.InterCastPauseMinMs + cfg.InterCastPauseMaxMs) / 2;
        var anyImminent = false;
        foreach (var t in activeThreats)
        {
            if ((t.ResolveAt - now).TotalSeconds < 0.5)
            {
                anyImminent = true;
                break;
            }
        }
        if (!anyImminent && (now - lastDodgeCompleted).TotalMilliseconds < interCastPauseMs)
        {
            ClearVector(); return;
        }

        // Sample candidates and score
        Vector2? bestCandidate = null;
        var bestScore = float.NegativeInfinity;
        var raycastsRemaining = cfg.RaycastBudgetPerFrame;
        foreach (var c in SafeEdgeSolver.SampleCandidates(playerPos2D, SampleDirections, SampleRadius))
        {
            if (raycastsRemaining <= 0) break;
            raycastsRemaining--;
            var c3 = new Vector3(c.X, playerPos3D.Y, c.Y);
            var score = SafeEdgeSolver.Score(c, activeThreats, _ => collision.IsPathBlocked(playerPos3D, c3));
            if (score > bestScore && score > 0)
            {
                bestScore = score;
                bestCandidate = c;
            }
        }

        if (bestCandidate == null) { ClearVector(); return; }

        // Already-safe check: if player is no longer inside any threat, clear and record completion
        var anyContains = false;
        foreach (var t in activeThreats)
        {
            if (t.Shape.Contains(t.Origin, t.RotationRadians, playerPos2D))
            {
                anyContains = true;
                break;
            }
        }
        if (!anyContains)
        {
            ClearVector();
            lastDodgeCompleted = now;
            return;
        }

        // Compute input vector
        var direction = bestCandidate.Value - playerPos2D;
        var distance = direction.Length();
        if (distance < 0.001f) { ClearVector(); return; }
        direction /= distance;

        direction = DirectionalNoise.Apply(direction, now, earliestCasterId, cfg.DirectionalNoiseDegrees);

        var minResolveSeconds = double.MaxValue;
        foreach (var t in activeThreats)
        {
            var s = (t.ResolveAt - now).TotalSeconds;
            if (s < minResolveSeconds) minResolveSeconds = s;
        }
        var magnitude = minResolveSeconds < cfg.WalkVsSprintThresholdSeconds ? 1.0f : 0.5f;

        // World-space to input-vector mapping. v1 writes XZ directly into RMIWalk's (Forward, Left).
        // T20 smoke test will refine the camera-relative conversion if needed.
        hook.DesiredInputVector = new Vector3(direction.X * magnitude, direction.Y * magnitude, 0f);
    }

    private void ClearVector() => hook.DesiredInputVector = null;

    private void PrunePerCastStateAgainstTracker()
    {
        if (firstSeenPerCast.Count == 0) return;

        var activeIds = new HashSet<ulong>();
        foreach (var t in tracker.ActiveAOEs) activeIds.Add(t.CasterId);

        var stale = new List<ulong>();
        foreach (var id in firstSeenPerCast.Keys)
            if (!activeIds.Contains(id)) stale.Add(id);

        foreach (var id in stale)
        {
            firstSeenPerCast.Remove(id);
            reactionDelayPerCast.Remove(id);
            arrivalTolerancePerCast.Remove(id);
        }
    }

    /// <summary>Test seam.</summary>
    protected virtual Vector2 GetPlayerPos2D() => Vector2.Zero;

    /// <summary>Test seam.</summary>
    protected virtual Vector3 GetPlayerPos3D() => Vector3.Zero;

    /// <summary>Test seam -- high-end content check.</summary>
    protected virtual bool IsHighEndZone() => false;

    /// <summary>Test seam -- combined check for dead, mounted, in cutscene, casting LB.</summary>
    protected virtual bool IsPlayerUnavailable() => false;
}
