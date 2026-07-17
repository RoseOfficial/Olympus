using System.Collections.Generic;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Coordinates healing for Scholar. All handlers push scheduler candidates;
/// dispatch happens centrally.
/// </summary>
public sealed class HealingModule : IAthenaModule
{
    private readonly List<IHealingHandler> _handlers;

    public int Priority => 10;
    public string Name => "Healing";

    public HealingModule()
    {
        _handlers = new List<IHealingHandler>
        {
            new RecitationHandler(),
            new ExcogitationHandler(),
            new LustrateHandler(),
            new IndomitabilityHandler(),
            new SacredSoilHandler(),
            new ProtractionHandler(),
            new EmergencyTacticsHandler(),
            new EsunaHandler(),
            new AoEHealHandler(),
            new SingleTargetHealHandler(),
        };
    }

    public bool TryExecute(IAthenaContext context, bool isMoving) => false;

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        context.HealingCoordination.Clear();
        TryPrePullRecitation(context, scheduler);    // NEW — fires before InCombat gate
        TryPrePullAdloquium(context, scheduler);     // NEW — fires after Recitation (next frame)
        if (!context.InCombat) return;
        if (!context.Configuration.EnableHealing) return;

        foreach (var handler in _handlers)
            handler.CollectCandidates(context, scheduler, isMoving);
    }

    public void UpdateDebugState(IAthenaContext context)
    {
        context.Debug.AetherflowStacks = context.AetherflowService.CurrentStacks;
    }

    private static void TryPrePullRecitation(IAthenaContext context, RotationScheduler scheduler)
    {
        var countdown = context.CountdownRemaining;
        if (countdown == null || countdown > 10f) return;
        if (!context.Configuration.PrePull.EnablePrePullActions) return;
        if (!context.Configuration.Scholar.EnableRecitation) return;

        var player = context.Player;
        if (player.Level < SCHActions.Recitation.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.Recitation.ActionId)) return;
        // HasRecitation reads Player.StatusList; always false in tests (benign in production too —
        // if already active, wasting the push is harmless).
        if (context.HasRecitation) return;

        scheduler.PushOgcd(AthenaAbilities.Recitation, player.GameObjectId, priority: 10,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SCHActions.Recitation.Name;
            });
    }

    private static void TryPrePullAdloquium(IAthenaContext context, RotationScheduler scheduler)
    {
        var countdown = context.CountdownRemaining;
        if (countdown == null || countdown > 8f) return;
        if (!context.Configuration.PrePull.EnablePrePullActions) return;
        if (!context.Configuration.EnableHealing) return;
        // Only apply with Recitation active — the guaranteed crit is the point.
        // HasRecitation reads Player.StatusList; always false in unit tests (same caveat
        // as TryPrePullRecitation above and ShieldHealingHandlerSchedulerTests.cs lines 66-70).
        if (!context.HasRecitation) return;

        var player = context.Player;
        if (player.Level < SCHActions.Adloquium.MinLevel) return;

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null) return;

        var capturedTank = tank;
        scheduler.PushGcd(AthenaAbilities.Adloquium, tank.GameObjectId, priority: 50,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction =
                    $"Pre-pull Adloquium (Recitation) on {capturedTank.Name?.TextValue ?? "tank"}";
            });
    }
}
