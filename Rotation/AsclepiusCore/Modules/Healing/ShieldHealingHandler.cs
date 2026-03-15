using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Eukrasian shield healing for Sage: E.Diagnosis and E.Prognosis.
/// Priority 20 in the GCD list.
/// </summary>
public sealed class ShieldHealingHandler : IHealingHandler
{
    public int Priority => 20;
    public string Name => "ShieldHealing";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        if (isMoving) return false; // cast time
        return TryEukrasianHealing(context, isMoving);
    }

    private bool TryEukrasianHealing(IAsclepiusContext context, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (player.Level < SGEActions.Eukrasia.MinLevel)
            return false;

        // If we already have Eukrasia active, use it for a heal
        if (context.HasEukrasia)
        {
            return TryEukrasianHealSpell(context);
        }

        // Decide if we should activate Eukrasia for healing
        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // AoE shield if multiple people need shields
        if (config.EnableEukrasianPrognosis && injuredCount >= config.AoEHealMinTargets &&
            avgHp < config.AoEHealThreshold)
        {
            return TryActivateEukrasia(context);
        }

        // Single-target shield for tank or low HP member
        if (config.EnableEukrasianDiagnosis && lowestHp < config.EukrasianDiagnosisThreshold)
        {
            return TryActivateEukrasia(context);
        }

        return false;
    }

    private bool TryActivateEukrasia(IAsclepiusContext context)
    {
        var player = context.Player;

        var action = SGEActions.Eukrasia;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Eukrasia";
            context.Debug.EukrasiaState = "Activating";
            return true;
        }

        return false;
    }

    private bool TryEukrasianHealSpell(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Prefer AoE if multiple injured
        if (config.EnableEukrasianPrognosis && injuredCount >= config.AoEHealMinTargets)
        {
            var action = player.Level >= SGEActions.EukrasianPrognosisII.MinLevel
                ? SGEActions.EukrasianPrognosisII
                : SGEActions.EukrasianPrognosis;

            // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
            if (!context.HealingCoordination.TryReserveAoEHeal(
                context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
            {
                context.Debug.EukrasianPrognosisState = "Skipped (remote AOE reserved)";
                return false;
            }

            if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "E.Prognosis";
                context.Debug.EukrasianPrognosisState = "Executing";

                // Training mode: capture explanation
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.Now,
                        ActionId = action.ActionId,
                        ActionName = action.Name,
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = $"E.Prognosis - {injuredCount} need shields at {avgHp:P0}",
                        DetailedReason = $"Eukrasian Prognosis placed shields on party. {injuredCount} members injured at {avgHp:P0} average HP. Provides instant shield that protects against incoming damage. The Eukrasia → E.Prognosis combo is instant cast!",
                        Factors = new[]
                        {
                            $"Party avg HP: {avgHp:P0}",
                            $"Injured count: {injuredCount}",
                            "100 potency heal + 320 potency shield",
                            "Instant cast (via Eukrasia)",
                            "1000 MP cost",
                        },
                        Alternatives = new[]
                        {
                            "Kerachole (oGCD regen + mit)",
                            "Ixochole (oGCD instant heal)",
                            "Prognosis (GCD heal, no shield)",
                        },
                        Tip = "E.Prognosis is your GCD party shield! Apply BEFORE damage hits for maximum value. The shield absorbs damage, making it more efficient than healing after the fact.",
                        ConceptId = SgeConcepts.EukrasianPrognosisUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }

                return true;
            }
        }

        // Single-target shield
        if (config.EnableEukrasianDiagnosis)
        {
            var target = context.PartyHelper.FindLowestHpPartyMember(player);
            if (target == null)
                return false;

            // Skip if another handler (local or remote Olympus instance) is already healing this target
            if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            {
                context.Debug.EukrasianDiagnosisState = "Skipped (reserved)";
                return false;
            }

            // Don't stack shields
            if (AsclepiusStatusHelper.HasEukrasianDiagnosisShield(target))
            {
                context.Debug.EukrasianDiagnosisState = "Already shielded";
                return false;
            }

            var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;

            var action = SGEActions.EukrasianDiagnosis;
            if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                // Reserve target to prevent other handlers (local or remote) from double-healing
                var healAmount = action.HealPotency * 10; // Rough estimate for heal + shield
                context.HealingCoordination.TryReserveTarget(
                    target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "E.Diagnosis";
                context.Debug.EukrasianDiagnosisState = "Executing";

                // Training mode: capture explanation
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = target.Name?.TextValue ?? "Unknown";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.Now,
                        ActionId = action.ActionId,
                        ActionName = "Eukrasian Diagnosis",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = $"E.Diagnosis on {targetName} at {hpPercent:P0}",
                        DetailedReason = $"Eukrasian Diagnosis placed on {targetName} at {hpPercent:P0} HP. Provides 300 potency heal + 540 potency shield. The shield absorbs incoming damage, making this very efficient for tank healing before busters!",
                        Factors = new[]
                        {
                            $"Target HP: {hpPercent:P0}",
                            "300 potency heal + 540 potency shield",
                            "Instant cast (via Eukrasia)",
                            "900 MP cost",
                        },
                        Alternatives = new[]
                        {
                            "Druochole (oGCD heal, Addersgall cost)",
                            "Taurochole (oGCD heal + mit for tanks)",
                            "Diagnosis (GCD heal, no shield)",
                        },
                        Tip = "E.Diagnosis is amazing for tanks before busters! The shield absorbs the hit, and any leftover becomes healing when it expires. Generates Addersting when the shield breaks!",
                        ConceptId = SgeConcepts.EukrasianDiagnosisUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }

                return true;
            }
        }

        return false;
    }
}
