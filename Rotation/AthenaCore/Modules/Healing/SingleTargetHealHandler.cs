using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Handles single-target GCD heals (Adloquium / Manifestation / Physick) for Scholar.
/// Priority 20 in the GCD list.
/// </summary>
public sealed class SingleTargetHealHandler : IHealingHandler
{
    public int Priority => 20;
    public string Name => "SingleTargetHeal";

    public bool TryExecute(IAthenaContext context, bool isMoving)
    {
        if (isMoving) return false;
        return TrySingleTargetHeal(context);
    }

    private bool TrySingleTargetHeal(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        // Check if at least one GCD heal is enabled
        if (!config.EnableAdloquium && !config.EnablePhysick)
            return false;

        var target = context.Configuration.Healing.UseDamageIntakeTriage
            ? context.PartyHelper.FindMostEndangeredPartyMember(
                player, context.DamageIntakeService, 0, context.DamageTrendService, context.ShieldTrackingService)
            : context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        // Choose between Adloquium and Physick
        ActionDefinition? action = null;

        // Check for Manifestation (Seraphism upgrade of Adlo) - controlled by EnableAdloquium
        if (config.EnableAdloquium && context.FairyStateManager.IsSeraphOrSeraphismActive && player.Level >= SCHActions.Manifestation.MinLevel)
        {
            if (hpPercent <= config.AdloquiumThreshold)
            {
                // Avoid overwriting Sage shields
                if (!config.AvoidOverwritingSageShields || !HasSageShield(context, target))
                {
                    action = SCHActions.Manifestation;
                }
            }
        }
        // Adloquium for shields
        else if (config.EnableAdloquium && player.Level >= SCHActions.Adloquium.MinLevel && hpPercent <= config.AdloquiumThreshold)
        {
            // Avoid overwriting existing Galvanize or Sage shields
            if (!context.StatusHelper.HasGalvanize(target))
            {
                if (!config.AvoidOverwritingSageShields || !HasSageShield(context, target))
                {
                    action = SCHActions.Adloquium;
                }
            }
        }

        // Fall back to Physick for raw healing
        if (action == null && config.EnablePhysick && hpPercent <= config.PhysickThreshold)
        {
            action = SCHActions.Physick;
        }

        if (action == null)
            return false;

        // Co-healer awareness: skip raw Physick when another healer's cast will already top up target.
        // Adloquium is left alone — its shield has preemptive value even if HP gets topped up.
        if (action.ActionId == SCHActions.Physick.ActionId &&
            CoHealerAwarenessHelper.CoHealerWillCover(
                context.Configuration.Healing.EnableCoHealerAwareness,
                context.CoHealerDetectionService,
                target,
                context.Configuration.Healing.CoHealerPendingHealThreshold))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            var castTimeMs = (int)(action.CastTime * 1000);
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, castTimeMs);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Single Heal";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var isAdlo = action.ActionId == SCHActions.Adloquium.ActionId || action.ActionId == SCHActions.Manifestation.ActionId;
                var isPhysick = action.ActionId == SCHActions.Physick.ActionId;

                string shortReason;
                string[] factors;
                string tip;
                string conceptId;

                if (isAdlo)
                {
                    shortReason = $"{action.Name} on {targetName} at {hpPercent:P0}";
                    factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
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
                    shortReason = $"Physick on {targetName} at {hpPercent:P0}";
                    factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
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
                    ActionId = action.ActionId,
                    ActionName = action.Name,
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"{action.Name} on {targetName} at {hpPercent:P0} HP. {(isAdlo ? "Adloquium provides 300 potency heal plus a 540 potency Galvanize shield (or 810 with crit Catalyze). " : "Physick provides 450 potency heal but no shield. It's SCH's weakest GCD heal option. ")}GCD heals should be used sparingly - prefer oGCD heals when available.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = conceptId,
                    Priority = hpPercent < 0.3f ? ExplanationPriority.High : ExplanationPriority.Normal,
                });

                context.TrainingService.RecordConceptApplication(conceptId, wasSuccessful: true);
            }

            return true;
        }

        return false;
    }

    private static bool HasSageShield(IAthenaContext context, IBattleChara target)
    {
        // Eukrasian Diagnosis status ID
        const ushort EukrasianDiagnosisStatusId = 2607;
        // Eukrasian Prognosis status ID
        const ushort EukrasianPrognosisStatusId = 2609;

        if (target.StatusList == null)
            return false;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == EukrasianDiagnosisStatusId ||
                status.StatusId == EukrasianPrognosisStatusId)
                return true;
        }
        return false;
    }
}
