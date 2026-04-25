using System.Collections.Generic;
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
}
