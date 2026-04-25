using System.Collections.Generic;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Coordinates healing for Sage. All handlers push scheduler candidates;
/// dispatch happens centrally.
/// </summary>
public sealed class HealingModule : IAsclepiusModule
{
    private readonly List<IHealingHandler> _handlers;

    public int Priority => 10;
    public string Name => "Healing";

    public HealingModule()
    {
        _handlers = new List<IHealingHandler>
        {
            new SingleTargetOgcdHandler(),
            new IxocholeHandler(),
            new KeracholeHandler(),
            new PhysisIIHandler(),
            new HolosHandler(),
            new HaimaHandler(),
            new PanhaimaHandler(),
            new PepsisHandler(),
            new RhizomataHandler(),
            new KrasisHandler(),
            new ZoeHandler(),
            new LucidDreamingHandler(),
            new EsunaHandler(),
            new PneumaHandler(),
            new ShieldHealingHandler(),
            new PrognosisHandler(),
            new DiagnosisHandler(),
        };
    }

    public bool TryExecute(IAsclepiusContext context, bool isMoving) => false;

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        context.HealingCoordination.Clear();
        if (!context.InCombat) return;
        if (!context.Configuration.EnableHealing) return;

        foreach (var handler in _handlers)
            handler.CollectCandidates(context, scheduler, isMoving);
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
