using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Sage-specific defensive module.
/// Handles single-target tank mitigation (Taurochole) and party AoE shields (Panhaima).
/// Kerachole and Holos are handled exclusively by HealingModule (priority 10).
/// Priority 20 — runs after healing, before buffs.
/// </summary>
public sealed class DefensiveModule : BaseDefensiveModule<IAsclepiusContext>, IAsclepiusModule
{
    #region Base Class Overrides - Debug State

    protected override void SetDefensiveState(IAsclepiusContext context, string state) =>
        context.Debug.PlanningState = state;

    protected override void SetPlannedAction(IAsclepiusContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override (float avgHpPercent, float lowestHpPercent, int injuredCount) GetPartyHealthMetrics(IAsclepiusContext context) =>
        context.PartyHelper.CalculatePartyHealthMetrics(context.Player);

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// SGE-specific defensives in priority order:
    /// Taurochole (tank single-target mit/heal) →
    /// Panhaima (party multi-hit shields)
    /// Note: Kerachole and Holos are handled by HealingModule (priority 10) which is
    /// authoritative for those abilities. DefensiveModule (priority 20) handles only
    /// Taurochole and Panhaima to avoid duplicate logic.
    /// </summary>
    protected override bool TryJobSpecificDefensives(IAsclepiusContext context, bool isMoving)
    {
        if (TryExecuteTaurochole(context))
            return true;

        if (TryExecutePanhaima(context))
            return true;

        return false;
    }

    #endregion

    #region SGE-Specific Defensive Methods

    private bool TryExecuteTaurochole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableTaurochole)
            return false;

        if (player.Level < SGEActions.Taurochole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.TaurocholeState = "No Addersgall";
            return false;
        }

        if (!context.ActionService.IsActionReady(SGEActions.Taurochole.ActionId))
        {
            context.Debug.TaurocholeState = "On CD";
            return false;
        }

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null)
        {
            context.Debug.TaurocholeState = "No tank";
            return false;
        }

        // Don't use if tank already has the Kerachole/Taurochole mitigation buff
        if (AsclepiusStatusHelper.HasTaurochole(tank))
        {
            context.Debug.TaurocholeState = "Already has mit";
            return false;
        }

        var hpPercent = tank.MaxHp > 0 ? (float)tank.CurrentHp / tank.MaxHp : 1f;

        // Check for imminent tank buster — use proactively even at high HP
        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration,
            out _);

        if (hpPercent > config.TaurocholeThreshold && !tankBusterImminent)
        {
            context.Debug.TaurocholeState = $"Tank at {hpPercent:P0}";
            return false;
        }

        var action = SGEActions.Taurochole;
        if (context.ActionService.ExecuteOgcd(action, tank.GameObjectId))
        {
            SetDefensiveState(context, "Taurochole");
            SetPlannedAction(context, action.Name);
            context.Debug.TaurocholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"Tank at {hpPercent:P0} — heal + 10% mit");
            return true;
        }

        return false;
    }

    private bool TryExecutePanhaima(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePanhaima)
            return false;

        if (player.Level < SGEActions.Panhaima.MinLevel)
            return false;

        // Check if another instance recently used a party mitigation
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.PanhaimaState = "Skipped (remote mit)";
            return false;
        }

        // Delay mitigations during burst windows unless emergency
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var (avgHpCheck, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpCheck > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                context.Debug.PanhaimaState = "Delayed (burst active)";
                return false;
            }
        }

        if (!context.ActionService.IsActionReady(SGEActions.Panhaima.ActionId))
        {
            context.Debug.PanhaimaState = "On CD";
            return false;
        }

        // Don't use if party already has Panhaima active
        if (AsclepiusStatusHelper.HasPanhaima(player))
        {
            context.Debug.PanhaimaState = "Already active";
            return false;
        }

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Check for imminent raidwide — use proactively for AoE shields
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration,
            out _);

        if (avgHp > config.PanhaimaThreshold && !raidwideImminent)
        {
            context.Debug.PanhaimaState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Panhaima;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            SetDefensiveState(context, "Panhaima");
            SetPlannedAction(context, action.Name);
            context.Debug.PanhaimaState = "Executing";
            partyCoord?.OnCooldownUsed(action.ActionId, 120_000);
            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(IAsclepiusContext context)
    {
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
        SetDefensiveState(context, $"Avg HP {avgHp:P0}, {injuredCount} injured");
    }
}
