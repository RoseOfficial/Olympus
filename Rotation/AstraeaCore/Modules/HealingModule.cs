using System.Collections.Generic;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Modules.Healing;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Coordinates healing for Astrologian. All 17 healing handlers are now scheduler-driven —
/// EarthlyStarPlacement uses PushGroundTargetedOgcd, the rest push regular oGCD/GCD candidates.
/// </summary>
public sealed class HealingModule : IAstraeaModule
{
    private readonly List<IHealingHandler> _handlers;

    public int Priority => 10;
    public string Name => "Healing";

    public HealingModule()
    {
        _handlers = new List<IHealingHandler>
        {
            new PreemptiveHealingHandler(),
            new EsunaHandler(),
            new EssentialDignityHandler(),
            new CelestialIntersectionHandler(),
            new CelestialOppositionHandler(),
            new ExaltationHandler(),
            new HoroscopeDetonationHandler(),
            new MicrocosmosHandler(),
            new EarthlyStarDetonationHandler(),
            new SynastryHandler(),
            new EarthlyStarPlacementHandler(),
            new LadyOfCrownsHandler(),
            new HoroscopePreparationHandler(),
            new MacrocosmosHandler(),
            new AoEHealingHandler(),
            new AspectedBeneficHandler(),
            new SingleTargetHandler(),
        };
    }

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        context.HealingCoordination.Clear();
        TryPrePullEarthlyStar(context, scheduler);   // NEW — bypasses HP threshold
        if (!context.InCombat) return;
        if (!context.Configuration.EnableHealing) return;

        foreach (var handler in _handlers)
            handler.CollectCandidates(context, scheduler, isMoving);
    }

    public void UpdateDebugState(IAstraeaContext context)
    {
        var (avgHp, lowestHp, injured) = context.PartyHealthMetrics;
        context.Debug.AoEInjuredCount = injured;
        context.Debug.PlayerHpPercent = context.Player.MaxHp > 0
            ? (float)context.Player.CurrentHp / context.Player.MaxHp
            : 1f;
    }

    private static void TryPrePullEarthlyStar(IAstraeaContext context, RotationScheduler scheduler)
    {
        var countdown = context.CountdownRemaining;
        if (countdown == null || countdown > 5f) return;
        if (!context.Configuration.PrePull.EnablePrePullActions) return;

        var config = context.Configuration.Astrologian;
        if (!config.EnableEarthlyStar) return;
        if (config.StarPlacement == EarthlyStarPlacementStrategy.Manual) return;

        var player = context.Player;
        if (player.Level < ASTActions.EarthlyStar.MinLevel) return;
        if (context.IsStarPlaced) return;
        if (!context.ActionService.IsActionReady(ASTActions.EarthlyStar.ActionId)) return;

        // Pre-pull placement skips the HP-threshold gate used by EarthlyStarPlacementHandler.
        // At countdown the party is always at full HP; placement is always correct here.
        var targetPosition = player.Position;
        if (config.StarPlacement == EarthlyStarPlacementStrategy.OnMainTank)
        {
            var tank = context.PartyHelper.FindTankInParty(player);
            if (tank != null) targetPosition = tank.Position;
        }

        var capturedPos = targetPosition;
        scheduler.PushGroundTargetedOgcd(AstraeaAbilities.EarthlyStar, capturedPos, priority: 50,
            onDispatched: _ =>
            {
                context.EarthlyStarService.OnStarPlaced(capturedPos);
                context.Debug.PlannedAction = ASTActions.EarthlyStar.Name;
                context.Debug.EarthlyStarState = "Placed (pre-pull)";
            });
    }
}
