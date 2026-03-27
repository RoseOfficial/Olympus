using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Prognosis for Sage. Priority 30 in the GCD list.
/// </summary>
public sealed class PrognosisHandler : IHealingHandler
{
    public int Priority => 30;
    public string Name => "Prognosis";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        if (isMoving) return false; // cast time
        return TryPrognosis(context);
    }

    private bool TryPrognosis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;

        if (!config.EnablePrognosis)
            return false;

        var player = context.Player;

        if (player.Level < SGEActions.Prognosis.MinLevel)
            return false;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.AoEStatus = $"{injuredCount} < {config.AoEHealMinTargets} injured";
            return false;
        }

        if (avgHp > config.AoEHealThreshold)
        {
            context.Debug.AoEStatus = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Prognosis;

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        var castTimeMs = (int)(action.CastTime * 1000);
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, castTimeMs))
        {
            context.Debug.AoEStatus = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Prognosis";
            context.Debug.AoEStatus = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Prognosis",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = $"Prognosis - {injuredCount} injured at {avgHp:P0}",
                    DetailedReason = $"Prognosis cast for {injuredCount} injured party members at {avgHp:P0} average HP. This is SGE's basic GCD party heal - use when oGCD options are exhausted and you need raw healing throughput.",
                    Factors = new[]
                    {
                        $"Party avg HP: {avgHp:P0}",
                        $"Injured count: {injuredCount}",
                        "300 potency AoE heal",
                        "2s cast time",
                        "800 MP cost",
                    },
                    Alternatives = new[]
                    {
                        "Ixochole (oGCD, instant, Addersgall)",
                        "Kerachole (oGCD regen + mit, Addersgall)",
                        "E.Prognosis (instant shield)",
                    },
                    Tip = "Prognosis is your fallback AoE heal when oGCDs are exhausted. It has a cast time, so prefer instant options like Ixochole or E.Prognosis when available. Only hard-cast when you truly need the raw healing!",
                    ConceptId = SgeConcepts.EmergencyHealing,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }
}
