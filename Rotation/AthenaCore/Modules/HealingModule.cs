using System.Collections.Generic;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Modules.Healing;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Coordinates healing for Scholar using two priority-sorted handler lists.
/// FairyModule (separate rotation module, priority 3) handles fairy abilities.
/// </summary>
public sealed class HealingModule : IAthenaModule
{
    private readonly List<IHealingHandler> _ogcdHandlers;
    private readonly List<IHealingHandler> _gcdHandlers;

    public int Priority => 10;
    public string Name => "Healing";

    public HealingModule()
    {
        _ogcdHandlers = new List<IHealingHandler>
        {
            new RecitationHandler(),       // 10
            new ExcogitationHandler(),     // 15
            new LustrateHandler(),         // 20
            new IndomitabilityHandler(),   // 25
            new SacredSoilHandler(),       // 30
            new ProtractionHandler(),      // 35
            new EmergencyTacticsHandler(), // 40
        };
        _ogcdHandlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        _gcdHandlers = new List<IHealingHandler>
        {
            new EsunaHandler(),            // 5
            new AoEHealHandler(),          // 10
            new SingleTargetHealHandler(), // 20
        };
        _gcdHandlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public bool TryExecute(IAthenaContext context, bool isMoving)
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

    public void UpdateDebugState(IAthenaContext context)
    {
        context.Debug.AetherflowStacks = context.AetherflowService.CurrentStacks;
    }
}
