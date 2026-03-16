using System;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Handles all defensive cooldowns for the WHM rotation.
/// Includes Temperance, Divine Caress, Plenary Indulgence, Divine Benison, Aquaveil, Liturgy of the Bell.
/// </summary>
public sealed class DefensiveModule : IApolloModule
{
    public int Priority => 20; // Medium-high priority for defensive cooldowns
    public string Name => "Defensive";

    // Training explanation arrays
    private static readonly string[] _liturgyOfTheBellAlternatives =
    {
        "Temperance (mitigation + healing boost)",
        "AoE heals (direct healing)",
        "Save for bigger damage phase",
    };

    public bool TryExecute(IApolloContext context, bool isMoving)
    {
        if (!context.CanExecuteOgcd || !context.InCombat)
            return false;

        var (avgHpPercent, _, injuredCount) = context.PartyHealthMetrics;

        // Divine Caress is auto-triggered when Divine Grace (from Temperance) is active
        if (TryExecuteDivineCaress(context))
            return true;

        // Temperance: Major raid cooldown
        if (TryExecuteTemperance(context, avgHpPercent, injuredCount))
            return true;

        // Plenary Indulgence: 10% damage reduction, synergizes with AoE heals
        if (TryExecutePlenaryIndulgence(context, injuredCount))
            return true;

        // Divine Benison: Shield tank proactively
        if (TryExecuteDivineBenison(context))
            return true;

        // Aquaveil: Damage reduction on tank
        if (TryExecuteAquaveil(context))
            return true;

        // Liturgy of the Bell: Ground-targeted reactive healer
        if (TryExecuteLiturgyOfTheBell(context, injuredCount))
            return true;

        context.Debug.DefensiveState = $"Idle (avg HP {avgHpPercent:P0}, {injuredCount} injured)";
        return false;
    }

    public void UpdateDebugState(IApolloContext context)
    {
        if (!context.InCombat)
            return;

        var player = context.Player;
        var config = context.Configuration;
        var (avgHpPercent, _, injuredCount) = context.PartyHealthMetrics;
        var partyEntityIds = context.PartyHelper.GetAllPartyMembers(context.Player).Select(m => m.EntityId);
        var partyDamageRate = context.DamageIntakeService.GetPartyMemberDamageRate(partyEntityIds, 5f);
        var dmgRateStr = partyDamageRate > 0 ? $", DPS {partyDamageRate:F0}" : "";

        // Update Temperance state
        if (!config.EnableHealing || !config.Defensive.EnableTemperance)
        {
            context.Debug.TemperanceState = "Disabled";
        }
        else if (player.Level < WHMActions.Temperance.MinLevel)
        {
            context.Debug.TemperanceState = $"Level {player.Level} < {WHMActions.Temperance.MinLevel}";
        }
        else if (!context.ActionService.IsActionReady(WHMActions.Temperance.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(WHMActions.Temperance.ActionId);
            context.Debug.TemperanceState = $"CD {cd:F1}s";
        }
        else
        {
            var highDamageIntake = config.Defensive.UseDynamicDefensiveThresholds &&
                                   partyDamageRate >= config.Defensive.DamageSpikeTriggerRate;
            var effectiveThreshold = highDamageIntake
                ? config.Defensive.DefensiveCooldownThreshold + 0.10f
                : config.Defensive.DefensiveCooldownThreshold;

            var shouldUse = injuredCount >= 3 || avgHpPercent < effectiveThreshold || highDamageIntake;
            context.Debug.TemperanceState = shouldUse
                ? $"Ready ({injuredCount} injured, avg HP {avgHpPercent:P0}{dmgRateStr})"
                : $"Waiting ({injuredCount} injured, avg HP {avgHpPercent:P0}{dmgRateStr})";
        }

        context.Debug.DefensiveState = $"Monitoring (avg HP {avgHpPercent:P0}, {injuredCount} injured{dmgRateStr})";
    }

    private bool TryExecuteDivineCaress(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Defensive.EnableDivineCaress)
            return false;

        if (player.Level < WHMActions.DivineCaress.MinLevel)
            return false;

        if (!StatusHelper.HasDivineGrace(player))
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.DivineCaress.ActionId))
            return false;

        if (ActionExecutor.ExecuteOgcd(context, WHMActions.DivineCaress, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp))
        {
            context.Debug.DefensiveState = "Divine Caress (triggered)";
            return true;
        }

        return false;
    }

    private unsafe bool TryExecuteTemperance(IApolloContext context, float avgHpPercent, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Defensive.EnableTemperance)
        {
            context.Debug.TemperanceState = "Disabled";
            return false;
        }

        if (player.Level < WHMActions.Temperance.MinLevel)
        {
            context.Debug.TemperanceState = $"Level {player.Level} < {WHMActions.Temperance.MinLevel}";
            return false;
        }

        if (!context.ActionService.IsActionReady(WHMActions.Temperance.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(WHMActions.Temperance.ActionId);
            context.Debug.TemperanceState = $"CD {cd:F1}s";
            return false;
        }

        // Calculate party damage rate for dynamic thresholds (filtered to party members only)
        var partyEntityIds = context.PartyHelper.GetAllPartyMembers(context.Player).Select(m => m.EntityId);
        var partyDamageRate = context.DamageIntakeService.GetPartyMemberDamageRate(partyEntityIds, 5f);
        var highDamageIntake = config.Defensive.UseDynamicDefensiveThresholds &&
                               partyDamageRate >= config.Defensive.DamageSpikeTriggerRate;

        // Check damage trend for proactive Temperance usage
        var damageSpikeImminent = config.Defensive.UseTemperanceTrendAnalysis &&
                                  context.DamageTrendService.IsDamageSpikeImminent(0.8f);

        // Check timeline/pattern detector for predicted raidwide (unified API)
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            config.Healing,
            out var raidwideSource);

        // Standard threshold or lowered threshold during high damage
        var effectiveThreshold = highDamageIntake || damageSpikeImminent || raidwideImminent
            ? config.Defensive.DefensiveCooldownThreshold + 0.10f  // More proactive during damage spike
            : config.Defensive.DefensiveCooldownThreshold;

        var shouldUse = injuredCount >= 3 ||
                        avgHpPercent < effectiveThreshold ||
                        highDamageIntake ||
                        damageSpikeImminent ||
                        raidwideImminent;

        if (!shouldUse)
        {
            var dmgRateStr = partyDamageRate > 0 ? $", DPS {partyDamageRate:F0}" : "";
            context.Debug.TemperanceState = $"Waiting ({injuredCount} injured, avg HP {avgHpPercent:P0}{dmgRateStr})";
            return false;
        }

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        if (config.PartyCoordination.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(config.PartyCoordination.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.TemperanceState = "Skipped (remote mit active)";
            return false;
        }

        // Burst awareness: Delay mitigations during burst windows unless emergency
        if (config.PartyCoordination.EnableHealerBurstAwareness &&
            config.PartyCoordination.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpPercent > config.Healing.GcdEmergencyThreshold)
            {
                context.Debug.TemperanceState = $"Delayed (burst active, {burstState.SecondsRemaining:F1}s remaining)";
                return false;
            }
        }

        // Check action status before attempting execution
        var actionManager = ActionManager.Instance();
        if (actionManager is not null)
        {
            var status = actionManager->GetActionStatus(ActionType.Action, WHMActions.Temperance.ActionId);
            if (status != 0)
            {
                context.Debug.TemperanceState = $"Blocked (status={status})";
                return false;
            }
        }

        var execDmgRateStr = partyDamageRate > 0 ? $", DPS {partyDamageRate:F0}" : "";
        context.Debug.TemperanceState = $"Executing ({injuredCount} injured, avg HP {avgHpPercent:P0}{execDmgRateStr})";

        if (ActionExecutor.ExecuteOgcd(context, WHMActions.Temperance, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp))
        {
            var reason = raidwideImminent ? $"raidwide predicted ({raidwideSource})" :
                         damageSpikeImminent ? "spike imminent" :
                         highDamageIntake ? "damage spike" : $"{injuredCount} injured";
            context.Debug.DefensiveState = $"Temperance ({reason}, avg HP {avgHpPercent:P0})";
            partyCoord?.OnCooldownUsed(WHMActions.Temperance.ActionId, 120_000);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = raidwideImminent
                    ? $"Pre-raidwide Temperance ({raidwideSource})"
                    : $"Temperance - {injuredCount} injured, {avgHpPercent:P0} avg HP";

                var factors = new[]
                {
                    $"Party average HP: {avgHpPercent:P0}",
                    $"Injured count: {injuredCount}",
                    $"Party damage rate: {partyDamageRate:F0} DPS",
                    $"Effective threshold: {effectiveThreshold:P0}",
                    raidwideImminent ? $"Raidwide predicted via {raidwideSource}" :
                    damageSpikeImminent ? "Damage spike imminent (trend analysis)" :
                    highDamageIntake ? "High party damage intake detected" :
                    "Multiple party members injured",
                };

                var alternatives = raidwideImminent
                    ? new[] { "Liturgy of the Bell (reactive healing)", "Save for later raidwide" }
                    : new[] { "AoE heals instead", "Wait for better timing", "Save for emergency" };

                var tip = raidwideImminent
                    ? "Using Temperance BEFORE raidwides provides both mitigation and healing boost - maximize value!"
                    : "Temperance is your major raid cooldown - don't hold it forever, but use it when the party needs help!";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = WHMActions.Temperance.ActionId,
                    ActionName = "Temperance",
                    Category = "Defensive",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Temperance provides 10% damage reduction and 20% healing boost for 20 seconds. Used because {reason}. Party average HP was {avgHpPercent:P0} with {injuredCount} injured members.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = WhmConcepts.TemperanceUsage,
                    Priority = raidwideImminent || damageSpikeImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        context.Debug.TemperanceState = "UseAction failed";
        return false;
    }

    private bool TryExecutePlenaryIndulgence(IApolloContext context, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.PlenaryIndulgence, config,
            c => c.EnableHealing && c.Defensive.EnablePlenaryIndulgence))
            return false;

        var shouldUse = config.Defensive.UseDefensivesWithAoEHeals && injuredCount >= config.Healing.AoEHealMinTargets;

        if (!shouldUse)
            return false;

        if (ActionExecutor.ExecuteOgcd(context, WHMActions.PlenaryIndulgence, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp))
        {
            context.Debug.DefensiveState = $"Plenary Indulgence ({injuredCount} injured, pre-AoE heal)";
            return true;
        }

        return false;
    }

    private bool TryExecuteDivineBenison(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.DivineBenison, config,
            c => c.EnableHealing && c.Defensive.EnableDivineBenison))
            return false;

        // Get charge information for smarter usage
        var currentCharges = context.ActionService.GetCurrentCharges(WHMActions.DivineBenison.ActionId);
        var maxCharges = context.ActionService.GetMaxCharges(WHMActions.DivineBenison.ActionId, 0);
        var isAtMaxCharges = currentCharges >= maxCharges && maxCharges > 0;

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank is null)
            return false;

        if (StatusHelper.HasStatus(tank, StatusHelper.StatusIds.DivineBenison))
            return false;

        if (Vector3.DistanceSquared(player.Position, tank.Position) >
            WHMActions.DivineBenison.RangeSquared)
            return false;

        var tankHpPct = context.PartyHelper.GetHpPercent(tank);
        var tankDamageRate = context.DamageIntakeService.GetDamageRate(tank.EntityId, 3f);

        // Proactive application: Apply if tank is taking significant sustained damage
        // even if HP is still high (anticipate tank buster)
        var shouldApplyProactively = config.Defensive.EnableProactiveCooldowns &&
                                     tankDamageRate >= config.Defensive.ProactiveBenisonDamageRate;

        // Check timeline/pattern detector for predicted tank buster (unified API)
        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            config.Healing,
            out var tankBusterSource);
        // For pattern-based detection, also check if this is the correct tank
        var shouldApplyForTankBuster = tankBusterImminent &&
            (tankBusterSource == "Timeline" ||
             context.BossMechanicDetector?.PredictedTankBuster?.TargetTankEntityId == tank.EntityId);

        // Standard application thresholds based on charge count
        // At max charges: Apply more freely (98% HP) to avoid wasting charge regen
        // Normal: Apply if tank HP below 95%
        var hpThreshold = isAtMaxCharges ? 0.98f : 0.95f;
        var shouldApplyStandard = tankHpPct < hpThreshold;

        // At max charges, also consider applying if tank is taking any damage at all
        var shouldApplyToAvoidCap = isAtMaxCharges && tankDamageRate > 0;

        if (!shouldApplyProactively && !shouldApplyStandard && !shouldApplyToAvoidCap && !shouldApplyForTankBuster)
            return false;

        var tankName = tank.Name?.TextValue ?? "Unknown";
        if (ActionExecutor.ExecuteOgcd(context, WHMActions.DivineBenison, tank.GameObjectId,
            tankName, tank.CurrentHp))
        {
            var chargeInfo = $"{currentCharges}/{maxCharges}";
            string reason;
            string logReason;

            if (shouldApplyForTankBuster)
            {
                var tbPrediction = TimelineHelper.GetNextTankBuster(
                    context.TimelineService, context.BossMechanicDetector, config.Healing);
                var secondsUntil = tbPrediction?.secondsUntil ?? 0;
                reason = $"tank buster in {secondsUntil:F1}s ({tankBusterSource}) ({chargeInfo})";
                logReason = $"Tank buster predicted ({tankBusterSource}) ({chargeInfo})";
            }
            else if (shouldApplyToAvoidCap && !shouldApplyProactively && !shouldApplyStandard)
            {
                reason = $"avoiding cap ({chargeInfo} charges)";
                logReason = $"At max charges - using to avoid cap ({chargeInfo})";
            }
            else if (shouldApplyProactively)
            {
                reason = $"proactive, DPS {tankDamageRate:F0} ({chargeInfo})";
                logReason = $"Proactive (high damage rate) ({chargeInfo})";
            }
            else
            {
                reason = $"{tankHpPct:P0} HP ({chargeInfo})";
                logReason = $"Standard (HP threshold) ({chargeInfo})";
            }

            context.Debug.DefensiveState = $"Divine Benison on {tankName} ({reason})";

            // Log defensive decision
            context.LogDefensiveDecision(
                tankName,
                tankHpPct,
                "Divine Benison",
                tankDamageRate,
                logReason);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = shouldApplyForTankBuster
                    ? $"Pre-tankbuster shield on {tankName}"
                    : shouldApplyToAvoidCap
                        ? $"Avoiding charge cap on {tankName}"
                        : $"Shield on {tankName} at {tankHpPct:P0}";

                var factors = new[]
                {
                    $"Tank HP: {tankHpPct:P0}",
                    $"Tank damage rate: {tankDamageRate:F0} DPS",
                    $"Charges: {chargeInfo}",
                    shouldApplyForTankBuster ? $"Tank buster predicted via {tankBusterSource}" :
                    shouldApplyToAvoidCap ? "At max charges - avoiding waste" :
                    shouldApplyProactively ? "High sustained damage on tank" :
                    $"HP below {hpThreshold:P0} threshold",
                };

                var alternatives = shouldApplyForTankBuster
                    ? new[] { "Aquaveil (longer mitigation)", "Save for next tank buster" }
                    : new[] { "Hold for tank buster", "Use on different target" };

                var tip = shouldApplyForTankBuster
                    ? "Divine Benison before tank busters provides a solid shield - stack with other mitigations!"
                    : "Divine Benison has charges - don't let them cap! Use proactively on the tank.";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = WHMActions.DivineBenison.ActionId,
                    ActionName = "Divine Benison",
                    Category = "Defensive",
                    TargetName = tankName,
                    ShortReason = shortReason,
                    DetailedReason = $"Divine Benison provides a 500 potency shield on {tankName}. {(shouldApplyForTankBuster ? $"Used proactively before predicted tank buster ({tankBusterSource}). " : shouldApplyToAvoidCap ? "Used to avoid wasting charge regeneration. " : "")}Tank HP: {tankHpPct:P0}, damage rate: {tankDamageRate:F0} DPS. Charges: {chargeInfo}.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = WhmConcepts.DivineBenisonUsage,
                    Priority = shouldApplyForTankBuster ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryExecuteAquaveil(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.Aquaveil, config,
            c => c.Defensive.EnableAquaveil))
            return false;

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank is null)
            return false;

        if (StatusHelper.HasStatus(tank, StatusHelper.StatusIds.Aquaveil))
            return false;

        if (Vector3.DistanceSquared(player.Position, tank.Position) >
            WHMActions.Aquaveil.RangeSquared)
            return false;

        var tankHpPct = context.PartyHelper.GetHpPercent(tank);
        var tankDamageRate = context.DamageIntakeService.GetDamageRate(tank.EntityId, 3f);

        // Proactive application: Apply if tank is taking significant sustained damage
        // even if HP is still high (anticipate tank buster)
        var shouldApplyProactively = config.Defensive.EnableProactiveCooldowns &&
                                     tankDamageRate >= config.Defensive.ProactiveAquaveilDamageRate;

        // Check timeline/pattern detector for predicted tank buster (unified API)
        var tankBusterImminentAquaveil = TimelineHelper.IsTankBusterImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            config.Healing,
            out var aquaveilTankBusterSource);
        // For pattern-based detection, also check if this is the correct tank
        var shouldApplyForTankBuster = tankBusterImminentAquaveil &&
            (aquaveilTankBusterSource == "Timeline" ||
             context.BossMechanicDetector?.PredictedTankBuster?.TargetTankEntityId == tank.EntityId);

        // Standard application: Apply if tank HP is below threshold
        var shouldApplyStandard = tankHpPct < 0.90f;

        if (!shouldApplyProactively && !shouldApplyStandard && !shouldApplyForTankBuster)
            return false;

        var tankName = tank.Name?.TextValue ?? "Unknown";
        if (ActionExecutor.ExecuteOgcd(context, WHMActions.Aquaveil, tank.GameObjectId,
            tankName, tank.CurrentHp))
        {
            string reason;
            string logReason;

            if (shouldApplyForTankBuster)
            {
                var tbPrediction = TimelineHelper.GetNextTankBuster(
                    context.TimelineService, context.BossMechanicDetector, config.Healing);
                var secondsUntil = tbPrediction?.secondsUntil ?? 0;
                reason = $"tank buster in {secondsUntil:F1}s ({aquaveilTankBusterSource})";
                logReason = $"Tank buster predicted ({aquaveilTankBusterSource})";
            }
            else if (shouldApplyProactively)
            {
                reason = $"proactive, DPS {tankDamageRate:F0}";
                logReason = "Proactive (high damage rate)";
            }
            else
            {
                reason = $"{tankHpPct:P0} HP";
                logReason = "Standard (HP threshold)";
            }

            context.Debug.DefensiveState = $"Aquaveil on {tankName} ({reason})";

            // Log defensive decision
            context.LogDefensiveDecision(
                tankName,
                tankHpPct,
                "Aquaveil",
                tankDamageRate,
                logReason);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = shouldApplyForTankBuster
                    ? $"Pre-tankbuster mitigation on {tankName}"
                    : $"Damage reduction on {tankName} at {tankHpPct:P0}";

                var factors = new[]
                {
                    $"Tank HP: {tankHpPct:P0}",
                    $"Tank damage rate: {tankDamageRate:F0} DPS",
                    shouldApplyForTankBuster ? $"Tank buster predicted via {aquaveilTankBusterSource}" :
                    shouldApplyProactively ? "High sustained damage on tank" :
                    $"HP below 90% threshold",
                    "15% damage reduction for 8 seconds",
                };

                var alternatives = shouldApplyForTankBuster
                    ? new[] { "Divine Benison (shield instead)", "Let tank handle it" }
                    : new[] { "Hold for tank buster", "Use Divine Benison first" };

                var tip = shouldApplyForTankBuster
                    ? "Aquaveil's 15% mitigation is great before tank busters - it reduces damage before shields absorb!"
                    : "Aquaveil is best used proactively when the tank is taking sustained damage.";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = WHMActions.Aquaveil.ActionId,
                    ActionName = "Aquaveil",
                    Category = "Defensive",
                    TargetName = tankName,
                    ShortReason = shortReason,
                    DetailedReason = $"Aquaveil provides 15% damage reduction on {tankName} for 8 seconds. {(shouldApplyForTankBuster ? $"Used proactively before predicted tank buster ({aquaveilTankBusterSource}). " : "")}Tank HP: {tankHpPct:P0}, damage rate: {tankDamageRate:F0} DPS.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = WhmConcepts.AquaveilUsage,
                    Priority = shouldApplyForTankBuster ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryExecuteLiturgyOfTheBell(IApolloContext context, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.LiturgyOfTheBell, config,
            c => c.Defensive.EnableLiturgyOfTheBell))
            return false;

        if (injuredCount < 2)
            return false;

        var tank = context.PartyHelper.FindTankInParty(player);
        Vector3 targetPosition;
        string targetName;

        if (tank is not null)
        {
            var distance = Vector3.Distance(player.Position, tank.Position);
            if (distance > WHMActions.LiturgyOfTheBell.Range)
            {
                targetPosition = player.Position;
                targetName = player.Name?.TextValue ?? "Unknown";
            }
            else
            {
                targetPosition = tank.Position;
                targetName = tank.Name?.TextValue ?? "Unknown";
            }
        }
        else
        {
            targetPosition = player.Position;
            targetName = player.Name?.TextValue ?? "Unknown";
        }

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        if (config.PartyCoordination.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(config.PartyCoordination.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.DefensiveState = "Bell skipped (remote mit)";
            return false;
        }

        // Burst awareness: Delay mitigations during burst windows unless emergency
        if (config.PartyCoordination.EnableHealerBurstAwareness &&
            config.PartyCoordination.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var (avgHpPercent, _, _) = context.PartyHealthMetrics;
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpPercent > config.Healing.GcdEmergencyThreshold)
            {
                context.Debug.DefensiveState = $"Bell delayed (burst active)";
                return false;
            }
        }

        if (ActionExecutor.ExecuteGroundTargeted(context, WHMActions.LiturgyOfTheBell, targetPosition,
            targetName, tank?.CurrentHp ?? player.CurrentHp))
        {
            context.Debug.DefensiveState = $"Bell placed at {targetName} ({injuredCount} injured)";
            partyCoord?.OnCooldownUsed(WHMActions.LiturgyOfTheBell.ActionId, 180_000);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Liturgy placed near {targetName} - {injuredCount} injured";

                var factors = new[]
                {
                    $"Injured count: {injuredCount}",
                    $"Placement: Near {targetName}",
                    "Heals party when they take damage",
                    "5 stacks, triggers on damage",
                    "180 second cooldown",
                };

                var alternatives = _liturgyOfTheBellAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = WHMActions.LiturgyOfTheBell.ActionId,
                    ActionName = "Liturgy of the Bell",
                    Category = "Defensive",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Liturgy of the Bell placed near {targetName}. This ground-targeted ability heals party members when they take damage, with 5 charges that trigger automatically. Used because {injuredCount} party members are injured and more damage is expected.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Place Liturgy where the party will stack - it triggers on damage, so it's perfect for multi-hit raidwides!",
                    ConceptId = WhmConcepts.LiturgyOfTheBellUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }
}
