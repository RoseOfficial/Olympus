using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Services.Content;
using Olympus.Services.Movement.Geometry;
using Olympus.Services.Movement.Humanization;
using Olympus.Services.Movement.Probes;
using Dalamud.Game.ClientState.Objects.Types;

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
    private readonly IObjectTable? objectTable;
    private readonly IHighEndContentService? highEndContent;
    private readonly ICondition? condition;

    private readonly Dictionary<ulong, DateTime> firstSeenPerCast = new();
    private readonly Dictionary<ulong, int> reactionDelayPerCast = new();
    private readonly Dictionary<ulong, float> arrivalTolerancePerCast = new();
    private DateTime lastDodgeCompleted = DateTime.MinValue;

    public TrashAvoidanceService(IRMIWalkHookService hook, IEnemyAOECastTracker tracker,
        IBossCombatDetector boss, IBGCollisionProbe collision, IMovementClock clock,
        Func<MovementConfig> configAccessor, IPluginLog log,
        IClientState? clientState = null,
        IHighEndContentService? highEndContent = null,
        IObjectTable? objectTable = null,
        ICondition? condition = null)
    {
        this.hook = hook;
        this.tracker = tracker;
        this.boss = boss;
        this.collision = collision;
        this.clock = clock;
        this.configAccessor = configAccessor;
        this.log = log;
        this.clientState = clientState;
        this.objectTable = objectTable;
        this.highEndContent = highEndContent;
        this.condition = condition;

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

        // All non-expired, in-range threats. Passed to the scorer so candidates that would
        // move the player into a second active AOE are penalised even if the player is not
        // currently inside that second AOE (finding #23).
        var allThreats = tracker.ActiveAOEs;
        var allValidThreats = new List<TrackedAOE>();
        foreach (var t in allThreats)
        {
            if (t.ResolveAt <= now) continue;
            if (Vector2.Distance(t.Origin, playerPos2D) > cfg.MaxThreatRangeYalms) continue;
            allValidThreats.Add(t);
        }

        // Threats that currently endanger the player: those where the player is inside the
        // shape OR within the per-cast arrival tolerance of the boundary. Using the expanded
        // containment check provides hysteresis -- the movement vector keeps firing until the
        // player is clearly outside the tolerance margin, preventing oscillation at the
        // geometric edge when position fluctuates by sub-yalm amounts (finding #41).
        // On the first frame a threat appears its tolerance is not yet in the dictionary and
        // defaults to 0, making ContainsExpanded equivalent to Contains for the initial trigger.
        var activeThreats = new List<TrackedAOE>();
        foreach (var t in allValidThreats)
        {
            var tol = arrivalTolerancePerCast.GetValueOrDefault(t.CasterId, 0f);
            if (!t.Shape.ContainsExpanded(t.Origin, t.RotationRadians, playerPos2D, tol)) continue;
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
            var score = SafeEdgeSolver.Score(c, allValidThreats, _ => collision.IsPathBlocked(playerPos3D, c3));
            if (score > bestScore && score > 0)
            {
                bestScore = score;
                bestCandidate = c;
            }
        }

        if (bestCandidate == null) { ClearVector(); return; }

        // Already-safe check: consistent with the activeThreats filter above. All entries in
        // activeThreats were selected by ContainsExpanded, so this check re-confirms each one
        // and will always be true. It exists as a defensive guard for future maintainers who
        // might decouple the selection criterion from the stop criterion.
        var anyContains = false;
        foreach (var t in activeThreats)
        {
            var tol = arrivalTolerancePerCast.GetValueOrDefault(t.CasterId, 0f);
            if (t.Shape.ContainsExpanded(t.Origin, t.RotationRadians, playerPos2D, tol))
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

    /// <summary>
    /// Returns the player's 2D position in the XZ plane (X, Z) used throughout the movement
    /// subsystem. Reads from <see cref="clientState"/> in production; overridden by test doubles.
    /// </summary>
    protected virtual Vector2 GetPlayerPos2D()
    {
        var p = objectTable?.LocalPlayer;
        return p != null ? new Vector2(p.Position.X, p.Position.Z) : Vector2.Zero;
    }

    /// <summary>
    /// Returns the player's 3D world position used for raycast origin. Reads from
    /// <see cref="objectTable"/> in production; overridden by test doubles.
    /// </summary>
    protected virtual Vector3 GetPlayerPos3D()
    {
        return objectTable?.LocalPlayer?.Position ?? Vector3.Zero;
    }

    /// <summary>
    /// Returns true when avoidance should be suppressed because the zone is high-end content
    /// (savage, extreme, ultimate, criterion). Reads from <see cref="highEndContent"/> when
    /// wired; falls back to false so the feature remains opt-in until Plugin.cs passes the
    /// service. Overridden by test doubles.
    /// </summary>
    protected virtual bool IsHighEndZone() => highEndContent?.IsHighEndZone ?? false;

    /// <summary>
    /// Returns true when the player cannot or should not move (dead). Overridden by test doubles.
    /// </summary>
    protected virtual bool IsPlayerUnavailable()
    {
        var p = objectTable?.LocalPlayer;
        if (p == null) return true;
        // Dead (CurrentHp == 0 also covers the post-raise ghost state) or casting:
        // movement input cancels an in-flight cast, so avoidance must never fire mid-cast.
        if (p.CurrentHp == 0 || p.IsCasting) return true;
        // Mounted or watching a cutscene: injected movement is at best noise, at worst
        // breaks scripted camera or movement.
        if (condition is not null
            && (condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Mounted]
                || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInCutSceneEvent]
                || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.WatchingCutscene]))
            return true;
        return false;
    }
}
