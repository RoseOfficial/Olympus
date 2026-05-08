using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Services.Movement.Humanization;
using Olympus.Services.Movement.Probes;

namespace Olympus.Services.Movement;

public class TrashAvoidanceService : ITrashAvoidanceService
{
    private readonly IRMIWalkHookService hook;
    private readonly IEnemyAOECastTracker tracker;
    private readonly IBossCombatDetector boss;
    private readonly IBGCollisionProbe collision;
    private readonly IMovementClock clock;
    private readonly Func<MovementConfig> configAccessor;
    private readonly IPluginLog? log;

    public TrashAvoidanceService(IRMIWalkHookService hook, IEnemyAOECastTracker tracker,
        IBossCombatDetector boss, IBGCollisionProbe collision, IMovementClock clock,
        Func<MovementConfig> configAccessor, IPluginLog log)
    {
        this.hook = hook;
        this.tracker = tracker;
        this.boss = boss;
        this.collision = collision;
        this.clock = clock;
        this.configAccessor = configAccessor;
        this.log = log;
    }

    public void Update()
    {
        var cfg = configAccessor();

        if (!cfg.EnableTrashAoEAvoidance) { ClearVector(); return; }
        if (!hook.HookInstalled) { ClearVector(); return; }
        if (IsHighEndZone()) { ClearVector(); return; }
        if (boss.IsBossEngaged) { ClearVector(); return; }
        if (IsPlayerUnavailable()) { ClearVector(); return; }

        // Threat assessment + safe target + vector dispatch -- implemented in Task 15.
        ClearVector();
    }

    private void ClearVector() => hook.DesiredInputVector = null;

    /// <summary>Test seam.</summary>
    protected virtual Vector2 GetPlayerPos2D() => Vector2.Zero;

    /// <summary>Test seam.</summary>
    protected virtual Vector3 GetPlayerPos3D() => Vector3.Zero;

    /// <summary>Test seam -- high-end content check.</summary>
    protected virtual bool IsHighEndZone() => false;

    /// <summary>Test seam -- combined check for dead, mounted, in cutscene, casting LB.</summary>
    protected virtual bool IsPlayerUnavailable() => false;
}
