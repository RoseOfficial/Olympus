using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Handles AoE GCD heals (Succor / Concitation) for Scholar. Priority 10 in the GCD list.
/// </summary>
public sealed class AoEHealHandler : IHealingHandler
{
    public int Priority => 10;
    public string Name => "AoEHeal";

    private static readonly string[] _succorAlternatives =
    {
        "Indomitability (oGCD, no shield)",
        "Whispering Dawn (fairy HoT)",
        "Sacred Soil (mitigation + HoT)",
    };

    public bool TryExecute(IAthenaContext context, bool isMoving)
    {
        if (isMoving) return false;
        return TryAoEHeal(context);
    }

    private bool TryAoEHeal(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableSuccor)
            return false;

        if (!ShouldUseAoEHeal(context))
            return false;

        // Choose between Succor and Concitation (Seraphism upgrade)
        ActionDefinition action;
        if (context.FairyStateManager.IsSeraphOrSeraphismActive && player.Level >= SCHActions.Concitation.MinLevel)
        {
            action = SCHActions.Concitation;
        }
        else if (player.Level >= SCHActions.Succor.MinLevel)
        {
            action = SCHActions.Succor;
        }
        else
        {
            return false;
        }

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        var castTimeMs = (int)(action.CastTime * 1000);
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, castTimeMs))
        {
            context.Debug.AoEHealState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "AoE Heal";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
                var raidwideImminent = TimelineHelper.IsRaidwideImminent(
                    context.TimelineService,
                    context.BossMechanicDetector,
                    context.Configuration.Healing,
                    out _);

                var isSeraphism = action.ActionId == SCHActions.Concitation.ActionId;
                var shortReason = raidwideImminent
                    ? $"{action.Name} - pre-shield for raidwide!"
                    : $"{action.Name} - {injuredCount} injured at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    raidwideImminent ? "Raidwide damage incoming!" : "No raidwide predicted",
                    isSeraphism ? "Seraphism active - using Concitation" : "Using Succor",
                    "Provides heal + Galvanize shield",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = action.Name,
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"{action.Name} cast for {injuredCount} injured party members at {avgHp:P0} average HP. {(raidwideImminent ? "Pre-shielding before incoming raidwide damage. " : "")}Succor/Concitation provides both healing (200 potency) and a Galvanize shield (320 potency). The shield absorbs damage, making it valuable before damage hits.",
                    Factors = factors,
                    Alternatives = _succorAlternatives,
                    Tip = "Succor is best used BEFORE damage (pre-shield) rather than after. After raidwides, prefer oGCD heals like Indomitability to save your GCD for damage.",
                    ConceptId = SchConcepts.SuccorUsage,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool ShouldUseAoEHeal(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var (count, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, 0);
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Timeline-aware: pre-shield before raidwides
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Use if party needs healing OR raidwide is imminent (for pre-shielding)
        return (avgHp <= config.AoEHealThreshold && count >= config.AoEHealMinTargets) || raidwideImminent;
    }
}
