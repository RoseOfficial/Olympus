using System.Collections.Generic;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
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
        TryPrePullEukrasianShield(context, scheduler);   // NEW
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

    private static void TryPrePullEukrasianShield(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var countdown = context.CountdownRemaining;
        if (countdown == null || countdown > 6f) return;
        if (!context.Configuration.PrePull.EnablePrePullActions) return;
        if (!context.Configuration.EnableHealing) return;
        if (!context.Configuration.Sage.EnableEukrasianPrognosis) return;

        var player = context.Player;
        if (player.Level < SGEActions.Eukrasia.MinLevel) return;

        // Phase 2: Eukrasia already active -- push the AoE shield GCD.
        // HasEukrasia reads Player.StatusList; always false in unit tests
        // (see ShieldHealingHandlerSchedulerTests.cs lines 66-70 for identical caveat).
        // This branch is exercised in-game; its scheduler push is structurally identical to
        // the raidwide branch in ShieldHealingHandler.TryPushEukrasianHealSpell.
        if (context.HasEukrasia)
        {
            var shieldCheckTarget = context.PartyHelper.FindLowestHpPartyMember(player);
            if (shieldCheckTarget != null &&
                AsclepiusStatusHelper.HasEukrasianPrognosisShield(shieldCheckTarget))
                return;

            var aoeAction = player.Level >= SGEActions.EukrasianPrognosisII.MinLevel
                ? SGEActions.EukrasianPrognosisII
                : SGEActions.EukrasianPrognosis;
            var aoeBehavior = player.Level >= SGEActions.EukrasianPrognosisII.MinLevel
                ? AsclepiusAbilities.EukrasianPrognosisII
                : AsclepiusAbilities.EukrasianPrognosis;

            var capturedAction = aoeAction;
            scheduler.PushGcd(aoeBehavior, player.GameObjectId, priority: 20,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = capturedAction.Name;
                    context.Debug.EukrasianPrognosisState = "Executing (pre-pull)";
                });
            return;
        }

        // Phase 1: Activate Eukrasia via direct dispatch.
        // MUST NOT gate on CanExecuteOgcd -- see CLAUDE.md "SGE Eukrasia timing" invariant.
        // This block is byte-identical in structure to ShieldHealingHandler.CollectCandidates
        // line 66: context.ActionService.ExecuteOgcd(SGEActions.Eukrasia, player.GameObjectId).
        if (context.ActionService.ExecuteOgcd(SGEActions.Eukrasia, player.GameObjectId))
        {
            context.Debug.PlannedAction = SGEActions.Eukrasia.Name;
            context.Debug.EukrasiaState = "Activating (pre-pull)";
        }
    }
}
