using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Eukrasian shield healing for Sage: E.Diagnosis and E.Prognosis.
/// Eukrasia activation bypasses the scheduler (direct dispatch) because the original
/// pattern fires Eukrasia (oGCD) during the GCD pass, which the scheduler's
/// CanExecuteOgcd gate would block. See CLAUDE.md "SGE Eukrasia timing".
/// </summary>
public sealed class ShieldHealingHandler : IHealingHandler
{
    public int Priority => 20;
    public string Name => "ShieldHealing";

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (isMoving) return;

        var config = context.Configuration.Sage;
        var player = context.Player;

        if (player.Level < SGEActions.Eukrasia.MinLevel) return;

        if (context.HasEukrasia)
        {
            TryPushEukrasianHealSpell(context, scheduler);
            return;
        }

        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        var shouldActivateForAoE = config.EnableEukrasianPrognosis &&
                                   injuredCount >= config.AoEHealMinTargets &&
                                   avgHp < config.AoEHealThreshold;
        var shouldActivateForSt = config.EnableEukrasianDiagnosis &&
                                  lowestHp < config.EukrasianDiagnosisThreshold;

        if (!shouldActivateForAoE && !shouldActivateForSt) return;

        // Direct-dispatch Eukrasia. The scheduler can't dispatch oGCDs during the GCD pass
        // (CanExecuteOgcd is false), but the game accepts ExecuteOgcd called directly because
        // Eukrasia has its own animation timing. See CLAUDE.md "SGE Eukrasia timing" note.
        if (context.ActionService.ExecuteOgcd(SGEActions.Eukrasia, player.GameObjectId))
        {
            context.Debug.PlannedAction = SGEActions.Eukrasia.Name;
            context.Debug.PlanningState = "Eukrasia";
            context.Debug.EukrasiaState = "Activating";
        }
    }

    private void TryPushEukrasianHealSpell(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Prefer AoE if multiple injured
        if (config.EnableEukrasianPrognosis && injuredCount >= config.AoEHealMinTargets)
        {
            var aoeAction = player.Level >= SGEActions.EukrasianPrognosisII.MinLevel
                ? SGEActions.EukrasianPrognosisII
                : SGEActions.EukrasianPrognosis;
            var aoeBehavior = player.Level >= SGEActions.EukrasianPrognosisII.MinLevel
                ? AsclepiusAbilities.EukrasianPrognosisII
                : AsclepiusAbilities.EukrasianPrognosis;

            if (!context.HealingCoordination.TryReserveAoEHeal(
                context.PartyCoordinationService, aoeAction.ActionId, aoeAction.HealPotency, 0))
            {
                context.Debug.EukrasianPrognosisState = "Skipped (remote AOE reserved)";
                return;
            }

            var capturedAvgHp = avgHp;
            var capturedInjuredCount = injuredCount;
            var capturedAction = aoeAction;

            scheduler.PushGcd(aoeBehavior, player.GameObjectId, priority: Priority,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = capturedAction.Name;
                    context.Debug.PlanningState = "E.Prognosis";
                    context.Debug.EukrasianPrognosisState = "Executing";

                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.UtcNow,
                            ActionId = capturedAction.ActionId,
                            ActionName = capturedAction.Name,
                            Category = "Healing",
                            TargetName = "Party",
                            ShortReason = $"E.Prognosis - {capturedInjuredCount} need shields at {capturedAvgHp:P0}",
                            DetailedReason = $"Eukrasian Prognosis placed shields on party. {capturedInjuredCount} members injured at {capturedAvgHp:P0} average HP. Provides instant shield that protects against incoming damage. The Eukrasia → E.Prognosis combo is instant cast!",
                            Factors = new[]
                            {
                                $"Party avg HP: {capturedAvgHp:P0}",
                                $"Injured count: {capturedInjuredCount}",
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
                });
            return;
        }

        // Single-target shield
        if (config.EnableEukrasianDiagnosis)
        {
            var target = context.PartyHelper.FindLowestHpPartyMember(player);
            if (target == null) return;
            if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            {
                context.Debug.EukrasianDiagnosisState = "Skipped (reserved)";
                return;
            }
            if (AsclepiusStatusHelper.HasEukrasianDiagnosisShield(target))
            {
                context.Debug.EukrasianDiagnosisState = "Already shielded";
                return;
            }

            var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
            var action = SGEActions.EukrasianDiagnosis;

            var capturedTarget = target;
            var capturedHpPercent = hpPercent;

            scheduler.PushGcd(AsclepiusAbilities.EukrasianDiagnosis, target.GameObjectId, priority: Priority,
                onDispatched: _ =>
                {
                    var healAmount = action.HealPotency * 10;
                    context.HealingCoordination.TryReserveTarget(
                        capturedTarget.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                    context.Debug.PlannedAction = action.Name;
                    context.Debug.PlanningState = "E.Diagnosis";
                    context.Debug.EukrasianDiagnosisState = "Executing";

                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        var targetName = capturedTarget.Name?.TextValue ?? "Unknown";

                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.UtcNow,
                            ActionId = action.ActionId,
                            ActionName = "Eukrasian Diagnosis",
                            Category = "Healing",
                            TargetName = targetName,
                            ShortReason = $"E.Diagnosis on {targetName} at {capturedHpPercent:P0}",
                            DetailedReason = $"Eukrasian Diagnosis placed on {targetName} at {capturedHpPercent:P0} HP. Provides 300 potency heal + 540 potency shield. The shield absorbs incoming damage, making this very efficient for tank healing before busters!",
                            Factors = new[]
                            {
                                $"Target HP: {capturedHpPercent:P0}",
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
                });
        }
    }
}
