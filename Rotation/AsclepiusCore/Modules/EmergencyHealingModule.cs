using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Handles emergency oGCD cooldowns for Sage:
/// Holos, Haima, Panhaima, Pepsis, Rhizomata, Krasis, Zoe, LucidDreaming.
/// </summary>
public sealed class EmergencyHealingModule
{
    private static readonly string[] _holosAlternatives =
    {
        "Ixochole (AoE heal, 30s CD)",
        "Kerachole (AoE regen + mit, 45s CD)",
        "Panhaima (AoE multi-hit shields, 120s CD)",
    };

    private static readonly string[] _haimaAlternatives =
    {
        "Taurochole (heal + 10% mit)",
        "E.Diagnosis (GCD shield)",
        "Panhaima (AoE version)",
    };

    private static readonly string[] _panhaimaAlternatives =
    {
        "Holos (heal + shield + mit)",
        "Kerachole (regen + mit)",
        "E.Prognosis (GCD party shield)",
    };

    private static readonly string[] _pepsisAlternatives =
    {
        "Let shields absorb damage naturally",
        "Use other heals instead",
        "Re-shield for future damage",
    };

    /// <summary>
    /// Tries Holos, Haima, Panhaima, Pepsis, Rhizomata, Krasis, Zoe, LucidDreaming.
    /// Does not check CanExecuteOgcd.
    /// </summary>
    public bool TryOgcd(IAsclepiusContext context)
    {
        if (TryHolos(context))
            return true;
        if (TryHaima(context))
            return true;
        if (TryPanhaima(context))
            return true;
        if (TryPepsis(context))
            return true;
        if (TryRhizomata(context))
            return true;
        if (TryKrasis(context))
            return true;
        if (TryZoe(context))
            return true;
        if (TryLucidDreaming(context))
            return true;
        return false;
    }

    private bool TryHolos(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableHolos)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.HolosState = "Skipped (remote mit)";
            return false;
        }

        // Burst awareness: Delay mitigations during burst windows unless emergency
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var (avgHpCheck, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpCheck > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                context.Debug.HolosState = $"Delayed (burst active)";
                return false;
            }
        }

        if (player.Level < SGEActions.Holos.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Holos.ActionId))
        {
            context.Debug.HolosState = "On CD";
            return false;
        }

        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Holos is a 2-minute CD - save for emergencies
        if (lowestHp > config.HolosThreshold)
        {
            context.Debug.HolosState = $"Lowest HP {lowestHp:P0}";
            return false;
        }

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.HolosState = $"{injuredCount} injured";
            return false;
        }

        var action = SGEActions.Holos;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Holos";
            context.Debug.HolosState = "Executing";
            partyCoord?.OnCooldownUsed(action.ActionId, 120_000);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Holos - emergency heal ({lowestHp:P0} lowest, {injuredCount} injured)";

                var factors = new[]
                {
                    $"Lowest HP: {lowestHp:P0}",
                    $"Threshold: {config.HolosThreshold:P0}",
                    $"Injured count: {injuredCount}",
                    "300 potency heal + shield + 10% mit (20s)",
                    "120s cooldown - big emergency button",
                };

                var alternatives = _holosAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Holos",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Holos used as emergency response. Party at {avgHp:P0} avg HP with lowest at {lowestHp:P0}. Provides 300 potency heal + 300 potency shield + 10% damage reduction for 20 seconds. This is SGE's panic button - save it for real emergencies!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Holos is your 2-minute panic button! It does everything: heals, shields, AND mitigates. Save it for when things go wrong, or use proactively for massive incoming damage you know about.",
                    ConceptId = SgeConcepts.HolosUsage,
                    Priority = ExplanationPriority.Critical,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryHaima(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableHaima)
            return false;

        if (player.Level < SGEActions.Haima.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Haima.ActionId))
        {
            context.Debug.HaimaState = "On CD";
            return false;
        }

        // Haima is best for tanks taking consistent damage
        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null)
        {
            context.Debug.HaimaState = "No tank";
            return false;
        }

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(tank.EntityId, context.PartyCoordinationService))
        {
            context.Debug.HaimaState = "Skipped (reserved)";
            return false;
        }

        var hpPercent = tank.MaxHp > 0 ? (float)tank.CurrentHp / tank.MaxHp : 1f;

        // Don't use if tank already has Haima
        if (AsclepiusStatusHelper.HasHaima(tank))
        {
            context.Debug.HaimaState = "Already has Haima";
            return false;
        }

        // Check if tank buster is imminent - use proactively
        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out var busterSource);

        // Use if tank buster is coming or tank HP is low
        if (hpPercent > config.HaimaThreshold && !tankBusterImminent)
        {
            context.Debug.HaimaState = $"Tank at {hpPercent:P0}";
            return false;
        }

        var action = SGEActions.Haima;
        if (context.ActionService.ExecuteOgcd(action, tank.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate for shield value
            context.HealingCoordination.TryReserveTarget(
                tank.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Haima";
            context.Debug.HaimaState = "Executing";
            context.Debug.HaimaTarget = tank.Name?.TextValue ?? "Unknown";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var tankName = tank.Name?.TextValue ?? "Unknown";

                var shortReason = tankBusterImminent
                    ? $"Haima on {tankName} - tankbuster incoming!"
                    : $"Haima on {tankName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Tank HP: {hpPercent:P0}",
                    tankBusterImminent ? "Tankbuster imminent!" : $"Threshold: {config.HaimaThreshold:P0}",
                    "300 potency shield x5 stacks",
                    "Shield refreshes when broken",
                    "120s cooldown",
                };

                var alternatives = _haimaAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Haima",
                    Category = "Healing",
                    TargetName = tankName,
                    ShortReason = shortReason,
                    DetailedReason = $"Haima placed on tank {tankName} at {hpPercent:P0} HP. {(tankBusterImminent ? "Tankbuster detected - Haima will absorb multiple hits!" : "Proactive shield for tank damage.")} Provides 5 stacks of 300 potency shields that refresh when consumed. Perfect for sustained tank damage or multi-hit tankbusters!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Haima is AMAZING for multi-hit tankbusters! Each time the shield breaks, a new one appears (up to 5 times). It heals for any remaining shield value when it expires. Pre-place before tankbusters!",
                    ConceptId = SgeConcepts.HaimaUsage,
                    Priority = tankBusterImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryPanhaima(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePanhaima)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.PanhaimaState = "Skipped (remote mit)";
            return false;
        }

        // Burst awareness: Delay mitigations during burst windows unless emergency
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var (avgHpCheck, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpCheck > context.Configuration.Healing.GcdEmergencyThreshold)
            {
                context.Debug.PanhaimaState = $"Delayed (burst active)";
                return false;
            }
        }

        if (player.Level < SGEActions.Panhaima.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Panhaima.ActionId))
        {
            context.Debug.PanhaimaState = "On CD";
            return false;
        }

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Check if raidwide is imminent - use proactively for AoE shields
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out var raidwideSource);

        // Panhaima is a 2-minute CD - save for raidwides
        // Use if raidwide is coming or party HP is low
        if (avgHp > config.PanhaimaThreshold && !raidwideImminent)
        {
            context.Debug.PanhaimaState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Panhaima;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Panhaima";
            context.Debug.PanhaimaState = "Executing";
            partyCoord?.OnCooldownUsed(action.ActionId, 120_000);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = raidwideImminent
                    ? "Panhaima - raidwide incoming!"
                    : $"Panhaima - party at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    raidwideImminent ? "Raidwide imminent!" : $"Threshold: {config.PanhaimaThreshold:P0}",
                    "200 potency shield x5 stacks (party-wide)",
                    "Shields refresh when broken",
                    "120s cooldown",
                };

                var alternatives = _panhaimaAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Panhaima",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Panhaima placed on party at {avgHp:P0} avg HP. {(raidwideImminent ? "Raidwide detected - shields will absorb incoming damage!" : "Proactive party shielding.")} Provides 5 stacks of 200 potency shields to ALL party members that refresh when consumed. Amazing for multi-hit raidwides!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Panhaima is the AoE version of Haima! Use it before multi-hit raidwides where the party will take repeated damage. Any remaining shield value heals when it expires. Excellent for prog where damage patterns are unknown.",
                    ConceptId = SgeConcepts.PanhaimaUsage,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryPepsis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePepsis)
            return false;

        if (player.Level < SGEActions.Pepsis.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Pepsis.ActionId))
        {
            context.Debug.PepsisState = "On CD";
            return false;
        }

        // Count party members with Eukrasian shields
        var shieldedCount = 0;
        foreach (var member in context.PartyHelper.GetAllPartyMembers(player))
        {
            if (AsclepiusStatusHelper.HasEukrasianDiagnosisShield(member) ||
                AsclepiusStatusHelper.HasEukrasianPrognosisShield(member))
            {
                shieldedCount++;
            }
        }

        if (shieldedCount < config.AoEHealMinTargets)
        {
            context.Debug.PepsisState = $"{shieldedCount} shielded";
            return false;
        }

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.PepsisThreshold)
        {
            context.Debug.PepsisState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Pepsis;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Pepsis";
            context.Debug.PepsisState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Pepsis - converting {shieldedCount} shields to heals";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Shielded members: {shieldedCount}",
                    "450 potency heal per E.Diagnosis shield",
                    "540 potency heal per E.Prognosis shield",
                    "Consumes shields instantly",
                };

                var alternatives = _pepsisAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Pepsis",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Pepsis converted {shieldedCount} Eukrasian shields into healing. Party at {avgHp:P0} avg HP. E.Diagnosis shields become 450 potency heals, E.Prognosis shields become 540 potency heals. Great when shields won't be consumed by incoming damage but healing is needed!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Pepsis is situational but powerful! If you've applied shields but damage has already passed, use Pepsis to convert those shields into healing. Also useful in emergencies - shield then immediately Pepsis for GCD heal + instant heal combo.",
                    ConceptId = SgeConcepts.PepsisUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryRhizomata(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableRhizomata)
            return false;

        if (player.Level < SGEActions.Rhizomata.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Rhizomata.ActionId))
        {
            context.Debug.RhizomataState = "On CD";
            return false;
        }

        // Don't overcap Addersgall
        if (context.AddersgallStacks >= 3)
        {
            context.Debug.RhizomataState = "At max stacks";
            return false;
        }

        // Use proactively to prevent overcapping
        if (config.PreventAddersgallCap && context.AddersgallStacks >= 2 && context.AddersgallTimer < 5f)
        {
            // About to cap, use Rhizomata to bank a stack
            var action = SGEActions.Rhizomata;
            if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Rhizomata";
                context.Debug.RhizomataState = "Preventing cap";

                // Training mode: capture explanation
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var stacks = context.AddersgallStacks;
                    var timer = context.AddersgallTimer;

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.Now,
                        ActionId = action.ActionId,
                        ActionName = "Rhizomata",
                        Category = "Resource",
                        TargetName = "Self",
                        ShortReason = $"Rhizomata - preventing Addersgall cap ({stacks}/3, {timer:F1}s)",
                        DetailedReason = $"Rhizomata used to prevent Addersgall overcap. Currently at {stacks}/3 stacks with {timer:F1}s until next natural regen. Using Rhizomata now banks an extra stack that won't be lost.",
                        Factors = new[]
                        {
                            $"Current stacks: {stacks}/3",
                            $"Timer to next regen: {timer:F1}s",
                            "Would overcap if not used",
                            "90s cooldown",
                        },
                        Alternatives = new[]
                        {
                            "Spend Addersgall first (Druochole, Kerachole, etc.)",
                            "Accept losing the stack",
                        },
                        Tip = "Rhizomata grants a free Addersgall stack on a 90s CD. Use it when you're at 2 stacks and about to regen naturally, or when you're empty and need healing resources!",
                        ConceptId = SgeConcepts.RhizomataUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }

                return true;
            }
        }

        // Use when low on Addersgall
        if (context.AddersgallStacks == 0)
        {
            var action = SGEActions.Rhizomata;
            if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Rhizomata";
                context.Debug.RhizomataState = "Out of Addersgall";

                // Training mode: capture explanation
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.Now,
                        ActionId = action.ActionId,
                        ActionName = "Rhizomata",
                        Category = "Resource",
                        TargetName = "Self",
                        ShortReason = "Rhizomata - out of Addersgall!",
                        DetailedReason = "Rhizomata used because Addersgall is empty. This provides an immediate stack for emergency healing options like Druochole, Taurochole, Ixochole, or Kerachole.",
                        Factors = new[]
                        {
                            "Addersgall: 0/3",
                            "Emergency resource generation",
                            "90s cooldown",
                        },
                        Alternatives = new[]
                        {
                            "Wait for natural regen (20s)",
                            "Use non-Addersgall heals (Physis, Holos)",
                        },
                        Tip = "Don't be afraid to use Rhizomata when empty! It's a 90s CD that gives you instant access to your best heals. Better to have it available when you need healing!",
                        ConceptId = SgeConcepts.RhizomataUsage,
                        Priority = ExplanationPriority.High,
                    });
                }

                return true;
            }
        }

        context.Debug.RhizomataState = "Saving";
        return false;
    }

    private bool TryKrasis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableKrasis)
            return false;

        if (player.Level < SGEActions.Krasis.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Krasis.ActionId))
        {
            context.Debug.KrasisState = "On CD";
            return false;
        }

        // Find a target that needs healing boost
        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
        {
            context.Debug.KrasisState = "No target";
            return false;
        }

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
        {
            context.Debug.KrasisState = "Skipped (reserved)";
            return false;
        }

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.KrasisThreshold)
        {
            context.Debug.KrasisState = $"Target at {hpPercent:P0}";
            return false;
        }

        // Don't stack with existing Krasis
        if (AsclepiusStatusHelper.HasKrasis(target))
        {
            context.Debug.KrasisState = "Already has Krasis";
            return false;
        }

        var action = SGEActions.Krasis;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target - Krasis increases healing received on this target
            var healAmount = 1000; // Krasis boosts heals, rough estimate for coordination
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Krasis";
            context.Debug.KrasisState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Krasis",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = $"Krasis on {targetName} at {hpPercent:P0} - boosting heals",
                    DetailedReason = $"Krasis placed on {targetName} at {hpPercent:P0} HP. Provides a 20% healing received buff for 10 seconds. Use before your biggest heals to maximize their effectiveness!",
                    Factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
                        $"Threshold: {config.KrasisThreshold:P0}",
                        "20% healing received buff (10s)",
                        "60s cooldown",
                    },
                    Alternatives = new[]
                    {
                        "Direct heals without buff",
                        "Zoe (50% buff for next GCD heal)",
                        "Wait for natural healing",
                    },
                    Tip = "Krasis increases ALL healing the target receives by 20% for 10 seconds. This includes your co-healer's heals and even the target's self-heals! Great for tanks taking heavy damage.",
                    ConceptId = SgeConcepts.KrasisUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryZoe(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableZoe)
            return false;

        if (player.Level < SGEActions.Zoe.MinLevel)
            return false;

        // Already have Zoe active
        if (context.HasZoe)
        {
            context.Debug.ZoeState = "Active";
            return false;
        }

        if (!context.ActionService.IsActionReady(SGEActions.Zoe.ActionId))
        {
            context.Debug.ZoeState = "On CD";
            return false;
        }

        // Use Zoe before a big heal
        var (avgHp, lowestHp, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Use when someone is critically low and we'll need a big heal
        if (lowestHp > config.DiagnosisThreshold)
        {
            context.Debug.ZoeState = $"Lowest HP {lowestHp:P0}";
            return false;
        }

        var action = SGEActions.Zoe;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Zoe";
            context.Debug.ZoeState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Zoe",
                    Category = "Healing",
                    TargetName = "Self (buff)",
                    ShortReason = $"Zoe - preparing 50% boosted GCD heal (lowest: {lowestHp:P0})",
                    DetailedReason = $"Zoe activated to boost the next GCD heal by 50%. Party member at {lowestHp:P0} HP - the boosted heal will provide much more recovery. Zoe works on Diagnosis, Prognosis, Pneuma, and Eukrasian heals!",
                    Factors = new[]
                    {
                        $"Lowest HP: {lowestHp:P0}",
                        "50% potency boost on next GCD heal",
                        "90s cooldown",
                        "Works on: Diagnosis, Prognosis, Pneuma, E.Diagnosis, E.Prognosis",
                    },
                    Alternatives = new[]
                    {
                        "Krasis (20% healing received buff)",
                        "Direct heal without buff",
                        "oGCD heals instead",
                    },
                    Tip = "Zoe is a 50% boost to your next GCD heal! Best paired with Pneuma (600 potency → 900 potency party heal!) or E.Prognosis for massive party shields. Don't waste it on small heals!",
                    ConceptId = SgeConcepts.ZoeUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryLucidDreaming(IAsclepiusContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Sage.EnableLucidDreaming)
        {
            context.Debug.LucidState = "Disabled";
            return false;
        }

        if (player.Level < SGEActions.LucidDreaming.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.LucidDreaming.ActionId))
        {
            context.Debug.LucidState = "On CD";
            return false;
        }

        var mpPercent = (float)player.CurrentMp / player.MaxMp;
        if (mpPercent > config.Sage.LucidDreamingThreshold)
        {
            context.Debug.LucidState = $"MP {mpPercent:P0}";
            return false;
        }

        var action = SGEActions.LucidDreaming;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Lucid Dreaming";
            context.Debug.LucidState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Lucid Dreaming",
                    Category = "Resource",
                    TargetName = "Self",
                    ShortReason = $"Lucid Dreaming at {mpPercent:P0} MP",
                    DetailedReason = $"Lucid Dreaming activated at {mpPercent:P0} MP (threshold: {config.Sage.LucidDreamingThreshold:P0}). Restores 3850 MP over 21 seconds. SGE is less MP-dependent than other healers (Addersgall heals restore MP!), but Lucid is still important for GCD heals and raises.",
                    Factors = new[]
                    {
                        $"Current MP: {mpPercent:P0}",
                        $"Threshold: {config.Sage.LucidDreamingThreshold:P0}",
                        "3850 MP over 21s",
                        "60s cooldown",
                    },
                    Alternatives = new[]
                    {
                        "Use Addersgall heals (restore 700 MP each)",
                        "Wait for natural MP regen",
                        "Accept MP constraints",
                    },
                    Tip = "SGE has the best MP economy of all healers! Addersgall heals (Druochole, Kerachole, etc.) actually RESTORE 700 MP. Use Lucid mainly for GCD heals and raises. Don't panic about MP as SGE!",
                    ConceptId = SgeConcepts.AddersgallManagement,
                    Priority = ExplanationPriority.Low,
                });
            }

            return true;
        }

        return false;
    }
}
