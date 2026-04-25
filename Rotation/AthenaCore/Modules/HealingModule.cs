using System.Collections.Generic;
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
        if (!context.InCombat) return;
        if (!context.Configuration.EnableHealing) return;

        foreach (var handler in _handlers)
            handler.CollectCandidates(context, scheduler, isMoving);
    }

    public void UpdateDebugState(IAthenaContext context)
    {
        context.Debug.AetherflowStacks = context.AetherflowService.CurrentStacks;
    }
}
