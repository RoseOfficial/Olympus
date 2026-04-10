using System.Collections.Generic;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Modules.Healing;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Coordinates healing for Astrologian using two priority-sorted handler lists.
/// </summary>
public sealed class HealingModule : IAstraeaModule
{
    private readonly List<IHealingHandler> _ogcdHandlers;
    private readonly List<IHealingHandler> _gcdHandlers;

    public int Priority => 10;
    public string Name => "Healing";

    public HealingModule()
    {
        _ogcdHandlers = new List<IHealingHandler>
        {
            new EssentialDignityHandler(),       // 10
            new CelestialIntersectionHandler(),  // 15
            new CelestialOppositionHandler(),    // 20
            new ExaltationHandler(),             // 25
            new HoroscopeDetonationHandler(),    // 30
            new MicrocosmosHandler(),            // 35
            new EarthlyStarDetonationHandler(),  // 40
            new SynastryHandler(),               // 45
            new EarthlyStarPlacementHandler(),   // 50
            new LadyOfCrownsHandler(),           // 60
            new HoroscopePreparationHandler(),   // 65 — oGCD despite spec's GCD table; calls ExecuteOgcd
        };
        _ogcdHandlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        _gcdHandlers = new List<IHealingHandler>
        {
            new EsunaHandler(),                // 5
            new MacrocosmosHandler(),          // 20
            new AoEHealingHandler(),           // 30
            new AspectedBeneficHandler(),      // 40
            new SingleTargetHandler(),         // 50
        };
        _gcdHandlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public bool TryExecute(IAstraeaContext context, bool isMoving)
    {
        context.HealingCoordination.Clear();

        if (!context.InCombat) return false;
        if (!context.Configuration.EnableHealing) return false;

        if (context.CanExecuteOgcd)
            foreach (var h in _ogcdHandlers)
                if (h.TryExecute(context, isMoving)) return true;

        if (context.CanExecuteGcd)
            foreach (var h in _gcdHandlers)
                if (h.TryExecute(context, isMoving)) return true;

        return false;
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
