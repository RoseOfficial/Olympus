using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Sage-specific defensive module (scheduler-driven).
/// Handles Taurochole (tank single-target mit/heal) and Panhaima (party multi-hit shields).
/// Kerachole and Holos are handled by HealingModule.
/// </summary>
public sealed class DefensiveModule : BaseDefensiveModule<IAsclepiusContext>, IAsclepiusModule
{
    protected override void SetDefensiveState(IAsclepiusContext context, string state) =>
        context.Debug.PlanningState = state;

    protected override void SetPlannedAction(IAsclepiusContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(IAsclepiusContext context) =>
        context.PartyHelper.CalculatePartyHealthMetrics(context.Player);

    protected override bool TryJobSpecificDefensives(IAsclepiusContext context, bool isMoving) => false;

    public override bool TryExecute(IAsclepiusContext context, bool isMoving) => false;

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) return;

        TryPushTaurochole(context, scheduler);
        TryPushPanhaima(context, scheduler);
    }

    public override void UpdateDebugState(IAsclepiusContext context)
    {
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
        SetDefensiveState(context, $"Avg HP {avgHp:P0}, {injuredCount} injured");
    }

    private void TryPushTaurochole(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableTaurochole) return;
        if (player.Level < SGEActions.Taurochole.MinLevel) return;
        if (context.AddersgallStacks < 1) { context.Debug.TaurocholeState = "No Addersgall"; return; }
        if (!context.ActionService.IsActionReady(SGEActions.Taurochole.ActionId)) { context.Debug.TaurocholeState = "On CD"; return; }

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null) { context.Debug.TaurocholeState = "No tank"; return; }
        if (AsclepiusStatusHelper.HasTaurochole(tank)) { context.Debug.TaurocholeState = "Already has mit"; return; }

        var hpPercent = tank.MaxHp > 0 ? (float)tank.CurrentHp / tank.MaxHp : 1f;
        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);
        if (hpPercent > config.TaurocholeThreshold && !tankBusterImminent)
        {
            context.Debug.TaurocholeState = $"Tank at {hpPercent:P0}";
            return;
        }

        var action = SGEActions.Taurochole;
        var capturedHpPercent = hpPercent;
        var capturedStacks = context.AddersgallStacks;

        // Defensive Taurochole at priority 75 — loses to HealingModule's reactive Taurochole (10)
        // when both conditions match, but fires when only the defensive (proactive tank buster) condition matches.
        scheduler.PushOgcd(AsclepiusAbilities.TaurocholeDefensive, tank.GameObjectId, priority: 75,
            onDispatched: _ =>
            {
                SetDefensiveState(context, "Taurochole");
                SetPlannedAction(context, action.Name);
                context.Debug.TaurocholeState = "Executing";
                context.LogAddersgallDecision(action.Name, capturedStacks, $"Tank at {capturedHpPercent:P0} - heal + 10% mit");
            });
    }

    private void TryPushPanhaima(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePanhaima) return;
        if (player.Level < SGEActions.Panhaima.MinLevel) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.PanhaimaState = "Skipped (remote mit)";
            return;
        }

        if (coordConfig.EnableHealerBurstAwareness && coordConfig.DelayMitigationsDuringBurst && partyCoord != null)
        {
            var (avgHpCheck, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpCheck > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                context.Debug.PanhaimaState = "Delayed (burst active)";
                return;
            }
        }

        if (!context.ActionService.IsActionReady(SGEActions.Panhaima.ActionId)) { context.Debug.PanhaimaState = "On CD"; return; }
        if (AsclepiusStatusHelper.HasPanhaima(player)) { context.Debug.PanhaimaState = "Already active"; return; }

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);
        if (avgHp > config.PanhaimaThreshold && !raidwideImminent) { context.Debug.PanhaimaState = $"Avg HP {avgHp:P0}"; return; }

        var action = SGEActions.Panhaima;

        scheduler.PushOgcd(AsclepiusAbilities.PanhaimaDefensive, player.GameObjectId, priority: 80,
            onDispatched: _ =>
            {
                SetDefensiveState(context, "Panhaima");
                SetPlannedAction(context, action.Name);
                context.Debug.PanhaimaState = "Executing";
                partyCoord?.OnCooldownUsed(action.ActionId, 120_000);
            });
    }
}
