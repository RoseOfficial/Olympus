using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Services.Movement.Humanization;
using Olympus.Services.Movement.Probes;

namespace Olympus.Services.Movement;

public class InteractDispatchService : IInteractDispatchService
{
    private readonly IObjectTable? objectTable;
    private readonly IObjectInteractor interactor;
    private readonly IMovementClock clock;
    private readonly Func<MovementConfig> configAccessor;
    private readonly IPluginLog? log;
    private readonly Dictionary<ulong, DateTime> cooldowns = new();

    public InteractDispatchService(IObjectTable objectTable, IClientState clientState,
        IObjectInteractor interactor, IMovementClock clock, Func<MovementConfig> configAccessor, IPluginLog log)
    {
        this.objectTable = objectTable;
        this.interactor = interactor;
        this.clock = clock;
        this.configAccessor = configAccessor;
        this.log = log;
    }

    public void Update()
    {
        var cfg = configAccessor();
        if (!cfg.EnableAutoInteract) return;
        if (IsPlayerDead()) return;
        if (IsPlayerCasting()) return;
        if (IsMenuFocused()) return;
        if (!cfg.InteractInCombat && IsPlayerInCombat()) return;

        var now = clock.UtcNow;
        (ulong id, float dist)? best = null;
        foreach (var (id, kind, distance, targetable) in EnumerateNearbyObjects())
        {
            if (!cfg.InteractAllowedKinds.Contains(kind)) continue;
            if (distance > cfg.InteractRangeYalms) continue;
            if (!targetable) continue;
            if (cooldowns.TryGetValue(id, out var until) && until > now) continue;
            if (best == null || distance < best.Value.dist) best = (id, distance);
        }

        if (best == null) return;

        interactor.Interact(best.Value.id);
        cooldowns[best.Value.id] = now.AddSeconds(cfg.InteractCooldownSeconds);
    }

    /// <summary>Test seam.</summary>
    protected virtual IEnumerable<(ulong gameObjectId, ObjectKind kind, float distance, bool targetable)> EnumerateNearbyObjects()
    {
        if (objectTable == null) yield break;
        var player = objectTable.LocalPlayer;
        if (player == null) yield break;
        var playerPos = player.Position;
        foreach (var obj in objectTable)
        {
            var dist = Vector3.Distance(playerPos, obj.Position);
            yield return (obj.GameObjectId, obj.ObjectKind, dist, obj.IsTargetable);
        }
    }

    /// <summary>Test seam.</summary>
    protected virtual bool IsPlayerInCombat()
    {
        var player = objectTable?.LocalPlayer;
        if (player == null) return false;
        return (player.StatusFlags & StatusFlags.InCombat) != 0;
    }

    /// <summary>Test seam.</summary>
    protected virtual bool IsPlayerCasting()
    {
        var player = objectTable?.LocalPlayer;
        return player != null && player.IsCasting;
    }

    /// <summary>Test seam.</summary>
    protected virtual bool IsMenuFocused() => false;

    /// <summary>Test seam.</summary>
    protected virtual bool IsPlayerDead()
    {
        var player = objectTable?.LocalPlayer;
        return player != null && player.CurrentHp == 0;
    }
}
