using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

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

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (isMoving) return;

        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableSuccor) return;

        var (count, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, 0);
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);

        var shouldUse = (avgHp <= config.AoEHealThreshold && count >= config.AoEHealMinTargets) || raidwideImminent;
        if (!shouldUse) return;

        ActionDefinition action;
        AbilityBehavior behavior;
        if (context.FairyStateManager.IsSeraphOrSeraphismActive && player.Level >= SCHActions.Concitation.MinLevel)
        {
            action = SCHActions.Concitation;
            behavior = AthenaAbilities.Concitation;
        }
        else if (player.Level >= SCHActions.Succor.MinLevel)
        {
            action = SCHActions.Succor;
            behavior = AthenaAbilities.Succor;
        }
        else
        {
            return;
        }

        var castTimeMs = (int)(action.CastTime * 1000);
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, castTimeMs))
        {
            context.Debug.AoEHealState = "Skipped (remote AOE reserved)";
            return;
        }

        var capturedAction = action;
        var capturedAvgHp = avgHp;
        var capturedInjuredCount = injuredCount;
        var capturedRaidwideImminent = raidwideImminent;

        scheduler.PushGcd(behavior, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = capturedAction.Name;
                context.Debug.PlanningState = "AoE Heal";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var isSeraphism = capturedAction.ActionId == SCHActions.Concitation.ActionId;
                    var shortReason = capturedRaidwideImminent
                        ? $"{capturedAction.Name} - pre-shield for raidwide!"
                        : $"{capturedAction.Name} - {capturedInjuredCount} injured at {capturedAvgHp:P0}";

                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Injured count: {capturedInjuredCount}",
                        capturedRaidwideImminent ? "Raidwide damage incoming!" : "No raidwide predicted",
                        isSeraphism ? "Seraphism active - using Concitation" : "Using Succor",
                        "Provides heal + Galvanize shield",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = capturedAction.ActionId,
                        ActionName = capturedAction.Name,
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"{capturedAction.Name} cast for {capturedInjuredCount} injured party members at {capturedAvgHp:P0} average HP. {(capturedRaidwideImminent ? "Pre-shielding before incoming raidwide damage. " : "")}Succor/Concitation provides both healing (200 potency) and a Galvanize shield (320 potency). The shield absorbs damage, making it valuable before damage hits.",
                        Factors = factors,
                        Alternatives = _succorAlternatives,
                        Tip = "Succor is best used BEFORE damage (pre-shield) rather than after. After raidwides, prefer oGCD heals like Indomitability to save your GCD for damage.",
                        ConceptId = SchConcepts.SuccorUsage,
                        Priority = capturedRaidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });

                    context.TrainingService.RecordConceptApplication(SchConcepts.SuccorUsage, wasSuccessful: true);
                }
            });
    }
}
