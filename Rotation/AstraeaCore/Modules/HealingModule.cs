using System.Collections.Generic;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Modules.Healing;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Coordinates healing for Astrologian. Most handlers are migrated to scheduler push.
/// EarthlyStarPlacement (ground-targeted), LadyOfCrowns (priority 60), and
/// HoroscopePreparation (priority 65) remain on legacy TryExecute. Both migrated
/// CollectCandidates and legacy TryExecute paths are wired up; the rotation's
/// ExecuteModules handles which fires when.
/// </summary>
public sealed class HealingModule : IAstraeaModule
{
    private readonly List<IHealingHandler> _allHandlers;
    private readonly List<IHealingHandler> _ogcdLegacyHandlers;

    public int Priority => 10;
    public string Name => "Healing";

    public HealingModule()
    {
        var preemptive = new PreemptiveHealingHandler();

        _allHandlers = new List<IHealingHandler>
        {
            preemptive,
            new EsunaHandler(),
            new EssentialDignityHandler(),
            new CelestialIntersectionHandler(),
            new CelestialOppositionHandler(),
            new ExaltationHandler(),
            new HoroscopeDetonationHandler(),
            new MicrocosmosHandler(),
            new EarthlyStarDetonationHandler(),
            new SynastryHandler(),
            new MacrocosmosHandler(),
            new AoEHealingHandler(),
            new AspectedBeneficHandler(),
            new SingleTargetHandler(),
        };

        // Legacy oGCD handlers (priorities 50, 60, 65) — fire only when scheduler doesn't dispatch
        _ogcdLegacyHandlers = new List<IHealingHandler>
        {
            new EarthlyStarPlacementHandler(),  // 50 — ground-targeted
            new LadyOfCrownsHandler(),          // 60 — preserve order
            new HoroscopePreparationHandler(),  // 65 — preserve order
        };
        _ogcdLegacyHandlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public bool TryExecute(IAstraeaContext context, bool isMoving)
    {
        if (!context.InCombat) return false;
        if (!context.Configuration.EnableHealing) return false;

        if (context.CanExecuteOgcd)
        {
            foreach (var h in _ogcdLegacyHandlers)
                if (h.TryExecute(context, isMoving)) return true;
        }

        return false;
    }

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
    {
        context.HealingCoordination.Clear();
        if (!context.InCombat) return;
        if (!context.Configuration.EnableHealing) return;

        foreach (var handler in _allHandlers)
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
