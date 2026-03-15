using System.Collections.Generic;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Coordinates healing for Sage using two priority-sorted handler lists.
/// oGCDs run first (CanExecuteOgcd), GCDs run second (CanExecuteGcd).
/// </summary>
public sealed class HealingModule : IAsclepiusModule
{
    private readonly List<IHealingHandler> _ogcdHandlers;
    private readonly List<IHealingHandler> _gcdHandlers;

    public int Priority => 10;
    public string Name => "Healing";

    public HealingModule()
    {
        _ogcdHandlers = new List<IHealingHandler>
        {
            new SingleTargetOgcdHandler(), // 10
            new IxocholeHandler(),         // 15
            new KeracholeHandler(),        // 20
            new PhysisIIHandler(),         // 25
            new HolosHandler(),            // 30
            new HaimaHandler(),            // 35
            new PanhaimaHandler(),         // 40
            new PepsisHandler(),           // 45
            new RhizomataHandler(),        // 50
            new KrasisHandler(),           // 55
            new ZoeHandler(),              // 60
            new LucidDreamingHandler(),    // 70
        };
        _ogcdHandlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        _gcdHandlers = new List<IHealingHandler>
        {
            new PneumaHandler(),          // 10
            new ShieldHealingHandler(),   // 20
            new PrognosisHandler(),       // 30
            new DiagnosisHandler(),       // 40
        };
        _gcdHandlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        context.HealingCoordination.Clear();

        if (!context.Configuration.EnableHealing) return false;

        if (context.CanExecuteOgcd)
            foreach (var h in _ogcdHandlers)
                if (h.TryExecute(context, isMoving)) return true;

        if (context.CanExecuteGcd)
            foreach (var h in _gcdHandlers)
                if (h.TryExecute(context, isMoving)) return true;

        return false;
    }

    public void UpdateDebugState(IAsclepiusContext context)
    {
        context.Debug.AddersgallStacks = context.AddersgallStacks;
        context.Debug.AddersgallTimer = context.AddersgallTimer;
        context.Debug.AdderstingStacks = context.AdderstingStacks;

        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
        context.Debug.AoEInjuredCount = injuredCount;
        context.Debug.PlayerHpPercent = context.Player.MaxHp > 0
            ? (float)context.Player.CurrentHp / context.Player.MaxHp
            : 1f;
    }
}
