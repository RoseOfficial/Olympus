using System.Collections.Generic;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Modules.Healing;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Orchestrates healing sub-handlers for the WHM rotation.
/// oGCD handlers run first (when CanExecuteOgcd), GCD handlers run second (when CanExecuteGcd).
/// Priority values are list-local — oGCD and GCD lists are sorted independently.
/// </summary>
public sealed class HealingModule : IApolloModule
{
    private readonly List<IHealingHandler> _ogcdHandlers;
    private readonly List<IHealingHandler> _gcdHandlers;

    public int Priority => 10;
    public string Name => "Healing";

    public HealingModule()
    {
        _ogcdHandlers = new List<IHealingHandler>
        {
            new BenedictionHandler(),     // 10
            new AssizeHealingHandler(),   // 15
            new TetragrammatonHandler(),  // 25
        };
        _ogcdHandlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        _gcdHandlers = new List<IHealingHandler>
        {
            new EsunaHandler(),               // 20
            new PreemptiveHealingHandler(),   // 30 — hybrid: internally checks CanExecuteOgcd for instant heals, falls back to GCD
            new RegenHandler(),               // 35
            new AoEHealingHandler(),          // 40
            new SingleTargetHealingHandler(), // 50
            new BloodLilyBuildingHandler(),   // 60
            new LilyCapPreventionHandler(),   // 80
        };
        _gcdHandlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public bool TryExecute(IApolloContext context, bool isMoving)
    {
        context.HealingCoordination.Clear();

        if (context.CanExecuteOgcd)
            foreach (var h in _ogcdHandlers)
                if (h.TryExecute(context, isMoving)) return true;

        if (context.CanExecuteGcd)
            foreach (var h in _gcdHandlers)
                if (h.TryExecute(context, isMoving)) return true;

        return false;
    }

    public void UpdateDebugState(IApolloContext context)
    {
        // Debug state is updated during handler execution.
    }
}
