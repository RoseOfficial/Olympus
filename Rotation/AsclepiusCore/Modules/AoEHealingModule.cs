using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Handles AoE healing for Sage: Ixochole, Kerachole, PhysisII, Pneuma, Prognosis.
/// </summary>
public sealed class AoEHealingModule
{
    private static readonly string[] _ixocholeAlternatives =
    {
        "Kerachole (AoE regen + mit)",
        "Physis II (AoE HoT + healing buff)",
        "Prognosis (GCD AoE heal)",
    };

    private static readonly string[] _keracholeAlternatives =
    {
        "Ixochole (instant AoE heal)",
        "Taurochole (single-target version)",
        "Physis II (AoE HoT only)",
    };

    private static readonly string[] _physisIIAlternatives =
    {
        "Kerachole (regen + mit, costs Addersgall)",
        "Ixochole (instant heal, costs Addersgall)",
        "Holos (emergency heal + shield + mit)",
    };

    private static readonly string[] _pneumaAlternatives =
    {
        "Save for better timing",
        "Use for pure DPS (skip if party healthy)",
        "Ixochole + Dosis (separate heal and damage)",
    };

    /// <summary>Tries Ixochole, Kerachole, PhysisII. Does not check CanExecuteOgcd.</summary>
    public bool TryOgcd(IAsclepiusContext context)
    {
        if (TryIxochole(context))
            return true;
        if (TryKerachole(context))
            return true;
        if (TryPhysisII(context))
            return true;
        return false;
    }

    /// <summary>Tries Pneuma. Does not check CanExecuteGcd or isMoving.</summary>
    public bool TryGcdPneuma(IAsclepiusContext context, bool isMoving)
    {
        return TryPneuma(context);
    }

    /// <summary>Tries Prognosis. Does not check CanExecuteGcd or isMoving.</summary>
    public bool TryGcdPrognosis(IAsclepiusContext context)
    {
        return TryPrognosis(context);
    }

    private bool TryIxochole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableIxochole)
            return false;

        if (player.Level < SGEActions.Ixochole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.IxocholeState = "No Addersgall";
            return false;
        }

        // Check cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Ixochole.ActionId))
        {
            context.Debug.IxocholeState = "On CD";
            return false;
        }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.IxocholeState = $"{injuredCount} < {config.AoEHealMinTargets} injured";
            return false;
        }

        if (avgHp > config.AoEHealThreshold)
        {
            context.Debug.IxocholeState = $"Avg HP {avgHp:P0} > {config.AoEHealThreshold:P0}";
            return false;
        }

        var action = SGEActions.Ixochole;

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
        {
            context.Debug.IxocholeState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Ixochole";
            context.Debug.IxocholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"{injuredCount} injured");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var stacks = context.AddersgallStacks;

                var shortReason = $"Ixochole - {injuredCount} injured at {avgHp:P0} ({stacks} stacks)";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    $"Addersgall stacks: {stacks}",
                    "400 potency AoE heal",
                    "30s cooldown, instant",
                };

                var alternatives = _ixocholeAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Ixochole",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Ixochole used for {injuredCount} injured party members at {avgHp:P0} average HP with {stacks} Addersgall stacks. 400 potency AoE heal on a 30s cooldown. Great for burst AoE healing when the party takes sudden damage.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Ixochole is your instant AoE heal! Use it for immediate party healing after raidwides. Kerachole provides ongoing healing via regen + mitigation, so use Ixochole for burst healing and Kerachole for sustained healing.",
                    ConceptId = SgeConcepts.IxocholeUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryKerachole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableKerachole)
            return false;

        if (player.Level < SGEActions.Kerachole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.KeracholeState = "No Addersgall";
            return false;
        }

        // Check cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Kerachole.ActionId))
        {
            context.Debug.KeracholeState = "On CD";
            return false;
        }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Check if raidwide is imminent - use proactively for mit + regen
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out var raidwideSource);

        // Burst awareness: Deploy Kerachole proactively before burst windows
        // Regen + mit provides sustained healing during high-damage DPS phases
        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.PreferShieldsBeforeBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            // Deploy Kerachole 3-8 seconds before burst (similar to raidwide logic)
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
            {
                burstImminent = true;
            }
        }

        // Kerachole is best value - use it liberally for regen + mit
        // If raidwide or burst is coming, use even if party is at high HP
        if (!raidwideImminent && !burstImminent && injuredCount < 2)
        {
            context.Debug.KeracholeState = $"{injuredCount} injured";
            return false;
        }

        if (!raidwideImminent && !burstImminent && avgHp > config.KeracholeThreshold)
        {
            context.Debug.KeracholeState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Kerachole;

        // Check if another Olympus healer already has a ground effect in this area
        // Kerachole creates a healing zone - avoid stacking with other ground effects
        if (coordConfig.EnableGroundEffectCoordination &&
            partyCoord?.WouldOverlapWithRemoteGroundEffect(
                player.Position,
                action.ActionId,
                coordConfig.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.KeracholeState = "Skipped (area covered)";
            return false;
        }

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
        {
            context.Debug.KeracholeState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            // Broadcast ground effect placement to other Olympus instances
            partyCoord?.OnGroundEffectPlaced(action.ActionId, player.Position);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Kerachole";
            context.Debug.KeracholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"Party regen + mit");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var stacks = context.AddersgallStacks;
                var trigger = raidwideImminent ? "Raidwide imminent" : burstImminent ? "Burst phase imminent" : "Party needs healing";

                var shortReason = $"Kerachole - {trigger} ({stacks} stacks)";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    trigger,
                    $"Addersgall stacks: {stacks}",
                    "100 potency regen + 10% mit (15s)",
                };

                var alternatives = _keracholeAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Kerachole",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Kerachole placed with {stacks} Addersgall stacks. {trigger}. Creates a 15s healing zone with 100 potency regen/tick AND 10% damage reduction. This is SGE's best sustained party healing tool - use it proactively before raidwides!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Kerachole is AMAZING value - regen + mitigation in one! Place it BEFORE damage hits so the party has mitigation when the raidwide lands, then benefits from regen for recovery. Shares CD with Taurochole.",
                    ConceptId = SgeConcepts.KeracholeUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryPhysisII(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhysisII)
            return false;

        if (player.Level < SGEActions.PhysisII.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.PhysisII.ActionId))
        {
            context.Debug.PhysisIIState = "On CD";
            return false;
        }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.PhysisIIState = $"{injuredCount} injured";
            return false;
        }

        if (avgHp > config.PhysisIIThreshold)
        {
            context.Debug.PhysisIIState = $"Avg HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.PhysisII;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Physis II";
            context.Debug.PhysisIIState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Physis II - {injuredCount} injured at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    "100 potency regen/tick (15s)",
                    "10% healing received buff",
                    "60s cooldown, free (no cost)",
                };

                var alternatives = _physisIIAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Physis II",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Physis II used on {injuredCount} injured party members at {avgHp:P0} average HP. Provides 100 potency regen/tick for 15 seconds PLUS 10% healing received buff. This is FREE (no Addersgall cost) - use it liberally for sustained party healing!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Physis II is FREE healing! The 10% healing received buff also boosts your other heals. Use it early in damage phases - the regen ticks will heal over time while you DPS.",
                    ConceptId = SgeConcepts.PhysisUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryPneuma(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePneuma)
            return false;

        if (player.Level < SGEActions.Pneuma.MinLevel)
        {
            context.Debug.PneumaState = "Level too low";
            return false;
        }

        // Pneuma has a 2-minute cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Pneuma.ActionId))
        {
            context.Debug.PneumaState = "On CD";
            return false;
        }

        // Check if we have a target
        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SGEActions.Pneuma.Range,
            player);

        if (enemy == null)
        {
            context.Debug.PneumaState = "No enemy";
            return false;
        }

        // Use Pneuma when party needs healing AND we can hit an enemy
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.PneumaThreshold && injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.PneumaState = $"Party HP {avgHp:P0}";
            return false;
        }

        var action = SGEActions.Pneuma;
        if (context.ActionService.ExecuteGcd(action, enemy.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Pneuma";
            context.Debug.PneumaState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Pneuma - {injuredCount} injured, enemy in range";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    "330 potency damage line AoE",
                    "600 potency party heal",
                    "120s cooldown",
                };

                var alternatives = _pneumaAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Pneuma",
                    Category = "Healing",
                    TargetName = "Enemy/Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Pneuma used with {injuredCount} injured party members and enemy in range. Deals 330 potency damage in a line AND heals party for 600 potency. This is SGE's signature ability - massive healing that also does damage! Perfect timing when party needs healing and you can hit enemies.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Pneuma is INSANE value when you need healing! It's a 600 potency party heal that ALSO deals damage. Time it so you can hit enemies while the party needs healing. Don't hold it too long - 2 minute cooldown is still short!",
                    ConceptId = SgeConcepts.PneumaUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryPrognosis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
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
                    Timestamp = DateTime.Now,
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
