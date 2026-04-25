using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

public sealed class SingleTargetHealHandler : IHealingHandler
{
    public int Priority => 20;
    public string Name => "SingleTargetHeal";

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (isMoving) return;

        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableAdloquium && !config.EnablePhysick) return;

        var target = context.Configuration.Healing.UseDamageIntakeTriage
            ? context.PartyHelper.FindMostEndangeredPartyMember(
                player, context.DamageIntakeService, 0, context.DamageTrendService, context.ShieldTrackingService)
            : context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null) return;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        ActionDefinition? action = null;
        AbilityBehavior? behavior = null;

        if (config.EnableAdloquium && context.FairyStateManager.IsSeraphOrSeraphismActive && player.Level >= SCHActions.Manifestation.MinLevel)
        {
            if (hpPercent <= config.AdloquiumThreshold)
            {
                if (!config.AvoidOverwritingSageShields || !HasSageShield(context, target))
                {
                    action = SCHActions.Manifestation;
                    behavior = AthenaAbilities.Manifestation;
                }
            }
        }
        else if (config.EnableAdloquium && player.Level >= SCHActions.Adloquium.MinLevel && hpPercent <= config.AdloquiumThreshold)
        {
            if (!context.StatusHelper.HasGalvanize(target))
            {
                if (!config.AvoidOverwritingSageShields || !HasSageShield(context, target))
                {
                    action = SCHActions.Adloquium;
                    behavior = AthenaAbilities.Adloquium;
                }
            }
        }

        if (action == null && config.EnablePhysick && hpPercent <= config.PhysickThreshold)
        {
            action = SCHActions.Physick;
            behavior = AthenaAbilities.Physick;
        }

        if (action == null || behavior == null) return;

        if (action.ActionId == SCHActions.Physick.ActionId &&
            CoHealerAwarenessHelper.CoHealerWillCover(
                context.Configuration.Healing.EnableCoHealerAwareness,
                context.CoHealerDetectionService,
                target,
                context.Configuration.Healing.CoHealerPendingHealThreshold))
            return;

        var capturedAction = action;
        var capturedTarget = target;
        var capturedHpPercent = hpPercent;

        scheduler.PushGcd(behavior, target.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                var healAmount = capturedAction.HealPotency * 10;
                var castTimeMs = (int)(capturedAction.CastTime * 1000);
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, healAmount, capturedAction.ActionId, castTimeMs);

                context.Debug.PlannedAction = capturedAction.Name;
                context.Debug.PlanningState = "Single Heal";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var isAdlo = capturedAction.ActionId == SCHActions.Adloquium.ActionId || capturedAction.ActionId == SCHActions.Manifestation.ActionId;
                    var isPhysick = capturedAction.ActionId == SCHActions.Physick.ActionId;

                    string shortReason;
                    string[] factors;
                    string tip;
                    string conceptId;

                    if (isAdlo)
                    {
                        shortReason = $"{capturedAction.Name} on {targetName} at {capturedHpPercent:P0}";
                        factors = new[]
                        {
                            $"Target HP: {capturedHpPercent:P0}",
                            $"Threshold: {config.AdloquiumThreshold:P0}",
                            "Provides heal + Galvanize shield",
                            "Shield can crit for Catalyze bonus",
                            $"Target had no existing shield",
                        };
                        tip = "Adloquium is your primary single-target GCD heal. The shield is valuable before damage. Critical Adlos create massive shields with Catalyze!";
                        conceptId = SchConcepts.AdloquiumUsage;
                    }
                    else
                    {
                        shortReason = $"Physick on {targetName} at {capturedHpPercent:P0}";
                        factors = new[]
                        {
                            $"Target HP: {capturedHpPercent:P0}",
                            $"Threshold: {config.PhysickThreshold:P0}",
                            "Pure healing (no shield)",
                            "Low MP cost",
                            "Used when shield not needed/available",
                        };
                        tip = "Physick is generally weak. Use Adloquium for shields or oGCDs like Lustrate when possible. Physick is a last resort.";
                        conceptId = SchConcepts.AdloquiumUsage;
                    }

                    var alternatives = new[]
                    {
                        "Lustrate (oGCD, uses Aetherflow)",
                        "Excogitation (proactive)",
                        isPhysick ? "Adloquium (adds shield)" : "Physick (no shield needed)",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = capturedAction.ActionId,
                        ActionName = capturedAction.Name,
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"{capturedAction.Name} on {targetName} at {capturedHpPercent:P0} HP. {(isAdlo ? "Adloquium provides 300 potency heal plus a 540 potency Galvanize shield (or 810 with crit Catalyze). " : "Physick provides 450 potency heal but no shield. It's SCH's weakest GCD heal option. ")}GCD heals should be used sparingly - prefer oGCD heals when available.",
                        Factors = factors,
                        Alternatives = alternatives,
                        Tip = tip,
                        ConceptId = conceptId,
                        Priority = capturedHpPercent < 0.3f ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });

                    context.TrainingService.RecordConceptApplication(conceptId, wasSuccessful: true);
                }
            });
    }

    private static bool HasSageShield(IAthenaContext context, IBattleChara target)
    {
        const ushort EukrasianDiagnosisStatusId = 2607;
        const ushort EukrasianPrognosisStatusId = 2609;

        if (target.StatusList == null) return false;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == EukrasianDiagnosisStatusId ||
                status.StatusId == EukrasianPrognosisStatusId)
                return true;
        }
        return false;
    }
}
