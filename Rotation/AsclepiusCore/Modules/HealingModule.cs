using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Handles healing for Sage.
/// Priority order:
/// 1. Emergency healing (oGCD Addersgall spenders)
/// 2. Lucid Dreaming (MP management)
/// 3. Free oGCD heals (Physis II, Kerachole, etc.)
/// 4. AoE healing (Ixochole, Prognosis)
/// 5. Single-target healing (Druochole, Diagnosis)
/// 6. Shields (Haima, Panhaima, Eukrasian heals)
/// </summary>
public sealed class HealingModule : IAsclepiusModule
{
    public int Priority => 10; // High priority - healing is essential
    public string Name => "Healing";

    // Training explanation arrays
    private static readonly string[] _druocholeAlternatives =
    {
        "Taurochole (if tank, adds 10% mit)",
        "Diagnosis (GCD, save Addersgall)",
        "Kardia healing (passive)",
    };

    private static readonly string[] _taurocholeAlternatives =
    {
        "Druochole (no mit, no shared CD)",
        "Kerachole (AoE version)",
        "Haima (multi-hit shield)",
    };

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

    private static readonly string[] _pneumaAlternatives =
    {
        "Save for better timing",
        "Use for pure DPS (skip if party healthy)",
        "Ixochole + Dosis (separate heal and damage)",
    };

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        // Clear frame-scoped coordination state to allow new reservations
        context.HealingCoordination.Clear();

        var config = context.Configuration.Sage;
        var player = context.Player;

        // oGCD: Emergency Addersgall heals
        if (context.CanExecuteOgcd)
        {
            // Priority 1: Druochole for emergency single-target
            if (TryDruochole(context))
                return true;

            // Priority 2: Taurochole for tank healing + mit
            if (TryTaurochole(context))
                return true;

            // Priority 3: Ixochole for AoE emergency
            if (TryIxochole(context))
                return true;

            // Priority 4: Kerachole for AoE regen + mit
            if (TryKerachole(context))
                return true;

            // Priority 5: Physis II for AoE HoT
            if (TryPhysisII(context))
                return true;

            // Priority 6: Holos for emergency AoE heal + shield + mit
            if (TryHolos(context))
                return true;

            // Priority 7: Haima for single-target multi-hit shield
            if (TryHaima(context))
                return true;

            // Priority 8: Panhaima for AoE multi-hit shield
            if (TryPanhaima(context))
                return true;

            // Priority 9: Pepsis to consume shields for healing
            if (TryPepsis(context))
                return true;

            // Priority 10: Rhizomata for Addersgall management
            if (TryRhizomata(context))
                return true;

            // Priority 11: Krasis for healing boost
            if (TryKrasis(context))
                return true;

            // Priority 12: Zoe for next GCD heal boost
            if (TryZoe(context))
                return true;

            // Priority 13: Lucid Dreaming for MP
            if (TryLucidDreaming(context))
                return true;
        }

        // GCD: Healing spells
        if (context.CanExecuteGcd)
        {
            // Priority 1: Pneuma for AoE damage + heal
            if (!isMoving && TryPneuma(context))
                return true;

            // Priority 2: Eukrasian shields
            if (TryEukrasianHealing(context, isMoving))
                return true;

            // Priority 3: Prognosis for AoE
            if (!isMoving && TryPrognosis(context))
                return true;

            // Priority 4: Diagnosis for single-target
            if (!isMoving && TryDiagnosis(context))
                return true;
        }

        return false;
    }

    public void UpdateDebugState(IAsclepiusContext context)
    {
        context.Debug.AddersgallStacks = context.AddersgallStacks;
        context.Debug.AddersgallTimer = context.AddersgallTimer;
        context.Debug.AdderstingStacks = context.AdderstingStacks;

        // Update healing state based on party health
        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
        context.Debug.AoEInjuredCount = injuredCount;
        context.Debug.PlayerHpPercent = context.Player.MaxHp > 0
            ? (float)context.Player.CurrentHp / context.Player.MaxHp
            : 1f;
    }

    #region Addersgall Heals

    private bool TryDruochole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (player.Level < SGEActions.Druochole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.DruocholeState = "No Addersgall";
            return false;
        }

        // Reserve stacks if configured
        if (context.AddersgallStacks <= config.AddersgallReserve)
        {
            context.Debug.DruocholeState = $"Reserved ({config.AddersgallReserve})";
            return false;
        }

        // Find target needing healing
        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
        {
            context.Debug.DruocholeState = "No target";
            return false;
        }

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
        {
            context.Debug.DruocholeState = "Skipped (reserved)";
            return false;
        }

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.DruocholeThreshold)
        {
            context.Debug.DruocholeState = $"{hpPercent:P0} > {config.DruocholeThreshold:P0}";
            return false;
        }

        var action = SGEActions.Druochole;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Druochole";
            context.Debug.DruocholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"Target at {hpPercent:P0}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var stacks = context.AddersgallStacks;

                var shortReason = $"Druochole on {targetName} at {hpPercent:P0} ({stacks} stacks)";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.DruocholeThreshold:P0}",
                    $"Addersgall stacks: {stacks}",
                    "600 potency oGCD heal",
                    "Restores 7% MP (700 MP)",
                };

                var alternatives = _druocholeAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Druochole",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Druochole used on {targetName} at {hpPercent:P0} HP with {stacks} Addersgall stacks. 600 potency oGCD heal plus 7% MP restoration. This is SGE's primary Addersgall single-target heal - efficient and free (restores MP!). Use freely when Addersgall is available.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Druochole is your bread-and-butter heal! It costs Addersgall but RESTORES MP, making it very efficient. Don't hoard Addersgall - use it! Stacks regenerate automatically.",
                    ConceptId = SgeConcepts.DruocholeUsage,
                    Priority = hpPercent < 0.3f ? ExplanationPriority.Critical : ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryTaurochole(IAsclepiusContext context)
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

        // Check cooldown (shares with Kerachole)
        if (!context.ActionService.IsActionReady(SGEActions.Taurochole.ActionId))
        {
            context.Debug.TaurocholeState = "On CD";
            return false;
        }

        // Taurochole is best for tanks needing healing + mitigation
        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null)
        {
            context.Debug.TaurocholeState = "No tank";
            return false;
        }

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(tank.EntityId, context.PartyCoordinationService))
        {
            context.Debug.TaurocholeState = "Skipped (reserved)";
            return false;
        }

        var hpPercent = tank.MaxHp > 0 ? (float)tank.CurrentHp / tank.MaxHp : 1f;
        if (hpPercent > config.TaurocholeThreshold)
        {
            context.Debug.TaurocholeState = $"Tank at {hpPercent:P0}";
            return false;
        }

        // Don't use if tank already has Kerachole/Taurochole mit
        if (AsclepiusStatusHelper.HasKerachole(tank))
        {
            context.Debug.TaurocholeState = "Already has mit";
            return false;
        }

        var action = SGEActions.Taurochole;
        if (context.ActionService.ExecuteOgcd(action, tank.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                tank.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Taurochole";
            context.Debug.TaurocholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"Tank at {hpPercent:P0}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var tankName = tank.Name?.TextValue ?? "Unknown";
                var stacks = context.AddersgallStacks;

                var shortReason = $"Taurochole on {tankName} at {hpPercent:P0} - heal + 10% mit";

                var factors = new[]
                {
                    $"Tank HP: {hpPercent:P0}",
                    $"Threshold: {config.TaurocholeThreshold:P0}",
                    $"Addersgall stacks: {stacks}",
                    "700 potency heal + 10% mit (15s)",
                    "Shares 45s CD with Kerachole",
                };

                var alternatives = _taurocholeAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Taurochole",
                    Category = "Healing",
                    TargetName = tankName,
                    ShortReason = shortReason,
                    DetailedReason = $"Taurochole used on tank {tankName} at {hpPercent:P0} HP with {stacks} Addersgall stacks. 700 potency heal PLUS 10% damage reduction for 15 seconds. Perfect for tank healing + tankbuster mitigation. Shares a 45s CD with Kerachole - plan which you need more!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Taurochole is your best tank heal! The 10% mitigation is fantastic for tankbusters. Remember it shares a 45s cooldown with Kerachole - if you need party mitigation, save for Kerachole instead.",
                    ConceptId = SgeConcepts.TaurocholeUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
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

    #endregion

    #region Free oGCD Heals

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

    #endregion

    #region GCD Heals

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

    private bool TryDiagnosis(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
        if (hpPercent > config.DruocholeThreshold)
            return false;

        var action = SGEActions.Diagnosis;
        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            var castTimeMs = (int)(action.CastTime * 1000);
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, castTimeMs);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Diagnosis";
            context.LogHealDecision(target.Name?.TextValue ?? "Unknown", hpPercent, action.Name, action.HealPotency, "Low HP");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Diagnosis",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = $"Diagnosis on {targetName} at {hpPercent:P0} (GCD heal)",
                    DetailedReason = $"Diagnosis cast on {targetName} at {hpPercent:P0} HP. This is SGE's basic GCD single-target heal - a fallback when Addersgall heals aren't available. Has a cast time, so prefer oGCDs when possible.",
                    Factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
                        "450 potency heal",
                        "1.5s cast time",
                        "700 MP cost",
                    },
                    Alternatives = new[]
                    {
                        "Druochole (oGCD, instant, restores MP)",
                        "Taurochole (oGCD for tanks, adds mit)",
                        "E.Diagnosis (instant shield)",
                        "Kardia passive healing",
                    },
                    Tip = "Diagnosis is your fallback single-target heal. You should rarely need it because Druochole (oGCD, restores MP!) is almost always better. Only use Diagnosis when Addersgall is empty and Rhizomata is on cooldown.",
                    ConceptId = SgeConcepts.EmergencyHealing,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    #endregion
}
