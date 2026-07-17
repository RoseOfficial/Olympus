using System.Collections.Generic;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Orchestrates healing sub-handlers for the WHM rotation.
/// Each handler pushes its own scheduler candidates; dispatch happens centrally.
/// </summary>
public sealed class HealingModule : IApolloModule
{
    private readonly List<IHealingHandler> _handlers;

    public int Priority => 10;
    public string Name => "Healing";

    public HealingModule()
    {
        _handlers = new List<IHealingHandler>
        {
            new BenedictionHandler(),
            new AssizeHealingHandler(),
            new TetragrammatonHandler(),
            new EsunaHandler(),
            new PreemptiveHealingHandler(),
            new RegenHandler(),
            new AoEHealingHandler(),
            new SingleTargetHealingHandler(),
            new BloodLilyBuildingHandler(),
            new LilyCapPreventionHandler(),
        };
    }

    public bool TryExecute(IApolloContext context, bool isMoving) => false;

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        context.HealingCoordination.Clear();
        TryPrePullRegen(context, scheduler);    // NEW — runs before the InCombat gate
        if (!context.InCombat) return;

        foreach (var handler in _handlers)
            handler.CollectCandidates(context, scheduler, isMoving);
    }

    public void UpdateDebugState(IApolloContext context)
    {
        // Debug state is updated during handler execution.
    }

    private static void TryPrePullRegen(IApolloContext context, RotationScheduler scheduler)
    {
        var countdown = context.CountdownRemaining;
        if (countdown == null || countdown > 4f) return;
        if (!context.Configuration.PrePull.EnablePrePullActions) return;
        if (!context.Configuration.EnableHealing || !context.Configuration.Healing.EnableRegen) return;

        var player = context.Player;
        if (player.Level < WHMActions.Regen.MinLevel) return;

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null) return;
        // StatusHelper.HasStatus guards on null StatusList; returns false in tests (benign).
        if (StatusHelper.HasRegenActive(tank, out _)) return;

        var capturedTank = tank;
        scheduler.PushGcd(ApolloAbilities.Regen, tank.GameObjectId,
            priority: (int)HealingPriority.Regen,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction =
                    $"Pre-pull Regen on {capturedTank.Name?.TextValue ?? "tank"}";
            });
    }
}
