using System;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Handles all defensive cooldowns for the WHM rotation (scheduler-driven).
/// </summary>
public sealed class DefensiveModule : IApolloModule
{
    public int Priority => 20;
    public string Name => "Defensive";

    private static readonly string[] _liturgyOfTheBellAlternatives =
    {
        "Temperance (mitigation + healing boost)",
        "AoE heals (direct healing)",
        "Save for bigger damage phase",
    };

    public bool TryExecute(IApolloContext context, bool isMoving) => false;

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) return;

        var (avgHpPercent, _, injuredCount) = context.PartyHealthMetrics;

        TryPushDivineCaress(context, scheduler);
        TryPushTemperance(context, scheduler, avgHpPercent, injuredCount);
        TryPushPlenaryIndulgence(context, scheduler, injuredCount);
        TryPushDivineBenison(context, scheduler);
        TryPushAquaveil(context, scheduler);
        TryPushLiturgyOfTheBell(context, scheduler, injuredCount);

        context.Debug.DefensiveState = $"Idle (avg HP {avgHpPercent:P0}, {injuredCount} injured)";
    }

    public void UpdateDebugState(IApolloContext context)
    {
        if (!context.InCombat) return;

        var player = context.Player;
        var config = context.Configuration;
        var (avgHpPercent, _, injuredCount) = context.PartyHealthMetrics;
        var partyEntityIds = context.PartyHelper.GetAllPartyMembers(context.Player).Select(m => m.EntityId);
        var partyDamageRate = context.DamageIntakeService.GetPartyMemberDamageRate(partyEntityIds, 5f);
        var dmgRateStr = partyDamageRate > 0 ? $", DPS {partyDamageRate:F0}" : "";

        if (!config.EnableHealing || !config.Defensive.EnableTemperance)
            context.Debug.TemperanceState = "Disabled";
        else if (player.Level < WHMActions.Temperance.MinLevel)
            context.Debug.TemperanceState = $"Level {player.Level} < {WHMActions.Temperance.MinLevel}";
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

    private void TryPushDivineCaress(IApolloContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Defensive.EnableDivineCaress) return;
        if (player.Level < WHMActions.DivineCaress.MinLevel) return;
        if (!StatusHelper.HasDivineGrace(player)) return;
        if (!context.ActionService.IsActionReady(WHMActions.DivineCaress.ActionId)) return;

        scheduler.PushOgcd(ApolloAbilities.DivineCaress, player.GameObjectId, priority: 90,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WHMActions.DivineCaress.Name;
                context.Debug.DefensiveState = "Divine Caress (triggered)";
            });
    }

    private unsafe void TryPushTemperance(IApolloContext context, RotationScheduler scheduler, float avgHpPercent, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Defensive.EnableTemperance) { context.Debug.TemperanceState = "Disabled"; return; }
        if (player.Level < WHMActions.Temperance.MinLevel)
        {
            context.Debug.TemperanceState = $"Level {player.Level} < {WHMActions.Temperance.MinLevel}";
            return;
        }
        if (!context.ActionService.IsActionReady(WHMActions.Temperance.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(WHMActions.Temperance.ActionId);
            context.Debug.TemperanceState = $"CD {cd:F1}s";
            return;
        }

        var partyEntityIds = context.PartyHelper.GetAllPartyMembers(context.Player).Select(m => m.EntityId);
        var partyDamageRate = context.DamageIntakeService.GetPartyMemberDamageRate(partyEntityIds, 5f);
        var highDamageIntake = config.Defensive.UseDynamicDefensiveThresholds &&
                               partyDamageRate >= config.Defensive.DamageSpikeTriggerRate;
        var damageSpikeImminent = config.Defensive.UseTemperanceTrendAnalysis &&
                                  context.DamageTrendService.IsDamageSpikeImminent(0.8f);
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, config, out var raidwideSource);

        var effectiveThreshold = highDamageIntake || damageSpikeImminent || raidwideImminent
            ? config.Defensive.DefensiveCooldownThreshold + 0.10f
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
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        if (config.PartyCoordination.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(config.PartyCoordination.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.TemperanceState = "Skipped (remote mit active)";
            return;
        }

        if (config.PartyCoordination.EnableHealerBurstAwareness &&
            config.PartyCoordination.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpPercent > config.Healing.GcdEmergencyThreshold)
            {
                context.Debug.TemperanceState = $"Delayed (burst active, {burstState.SecondsRemaining:F1}s remaining)";
                return;
            }
        }

        var actionManager = ActionManager.Instance();
        if (actionManager is not null)
        {
            var status = actionManager->GetActionStatus(ActionType.Action, WHMActions.Temperance.ActionId);
            if (status != 0)
            {
                context.Debug.TemperanceState = $"Blocked (status={status})";
                return;
            }
        }

        var capturedAvgHp = avgHpPercent;
        var capturedInjured = injuredCount;
        var capturedDamageRate = partyDamageRate;
        var capturedRaidwideImminent = raidwideImminent;
        var capturedDamageSpikeImminent = damageSpikeImminent;
        var capturedHighDamageIntake = highDamageIntake;
        var capturedRaidwideSource = raidwideSource;
        var capturedEffectiveThreshold = effectiveThreshold;

        scheduler.PushOgcd(ApolloAbilities.Temperance, player.GameObjectId, priority: 80,
            onDispatched: _ =>
            {
                var execDmgRateStr = capturedDamageRate > 0 ? $", DPS {capturedDamageRate:F0}" : "";
                var reason = capturedRaidwideImminent ? $"raidwide predicted ({capturedRaidwideSource})" :
                             capturedDamageSpikeImminent ? "spike imminent" :
                             capturedHighDamageIntake ? "damage spike" : $"{capturedInjured} injured";
                context.Debug.PlannedAction = WHMActions.Temperance.Name;
                context.Debug.TemperanceState = $"Executing ({capturedInjured} injured, avg HP {capturedAvgHp:P0}{execDmgRateStr})";
                context.Debug.DefensiveState = $"Temperance ({reason}, avg HP {capturedAvgHp:P0})";
                partyCoord?.OnCooldownUsed(WHMActions.Temperance.ActionId, 120_000);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = capturedRaidwideImminent
                        ? $"Pre-raidwide Temperance ({capturedRaidwideSource})"
                        : $"Temperance - {capturedInjured} injured, {capturedAvgHp:P0} avg HP";

                    var factors = new[]
                    {
                        $"Party average HP: {capturedAvgHp:P0}",
                        $"Injured count: {capturedInjured}",
                        $"Party damage rate: {capturedDamageRate:F0} DPS",
                        $"Effective threshold: {capturedEffectiveThreshold:P0}",
                        capturedRaidwideImminent ? $"Raidwide predicted via {capturedRaidwideSource}" :
                        capturedDamageSpikeImminent ? "Damage spike imminent (trend analysis)" :
                        capturedHighDamageIntake ? "High party damage intake detected" :
                        "Multiple party members injured",
                    };

                    var alternatives = capturedRaidwideImminent
                        ? new[] { "Liturgy of the Bell (reactive healing)", "Save for later raidwide" }
                        : new[] { "AoE heals instead", "Wait for better timing", "Save for emergency" };

                    var tip = capturedRaidwideImminent
                        ? "Using Temperance BEFORE raidwides provides both mitigation and healing boost - maximize value!"
                        : "Temperance is your major raid cooldown - don't hold it forever, but use it when the party needs help!";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.Temperance.ActionId,
                        ActionName = "Temperance",
                        Category = "Defensive",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Temperance provides 10% damage reduction and 20% healing boost for 20 seconds. Used because {reason}. Party average HP was {capturedAvgHp:P0} with {capturedInjured} injured members.",
                        Factors = factors,
                        Alternatives = alternatives,
                        Tip = tip,
                        ConceptId = WhmConcepts.TemperanceUsage,
                        Priority = capturedRaidwideImminent || capturedDamageSpikeImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });
                }
            });
    }

    private void TryPushPlenaryIndulgence(IApolloContext context, RotationScheduler scheduler, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.PlenaryIndulgence, config,
            c => c.EnableHealing && c.Defensive.EnablePlenaryIndulgence))
            return;

        var shouldUse = config.Defensive.UseDefensivesWithAoEHeals && injuredCount >= config.Healing.AoEHealMinTargets;
        if (!shouldUse) return;

        var capturedInjured = injuredCount;

        scheduler.PushOgcd(ApolloAbilities.PlenaryIndulgence, player.GameObjectId, priority: 100,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WHMActions.PlenaryIndulgence.Name;
                context.Debug.DefensiveState = $"Plenary Indulgence ({capturedInjured} injured, pre-AoE heal)";
            });
    }

    private void TryPushDivineBenison(IApolloContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.DivineBenison, config,
            c => c.EnableHealing && c.Defensive.EnableDivineBenison))
            return;

        var currentCharges = context.ActionService.GetCurrentCharges(WHMActions.DivineBenison.ActionId);
        var maxCharges = context.ActionService.GetMaxCharges(WHMActions.DivineBenison.ActionId, 0);
        var isAtMaxCharges = currentCharges >= maxCharges && maxCharges > 0;

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank is null) return;
        if (StatusHelper.HasStatus(tank, StatusHelper.StatusIds.DivineBenison)) return;
        if (Vector3.DistanceSquared(player.Position, tank.Position) > WHMActions.DivineBenison.RangeSquared) return;

        var tankHpPct = context.PartyHelper.GetHpPercent(tank);
        var tankDamageRate = context.DamageIntakeService.GetDamageRate(tank.EntityId, 3f);

        var shouldApplyProactively = config.Defensive.EnableProactiveCooldowns &&
                                     tankDamageRate >= config.Defensive.ProactiveBenisonDamageRate;

        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService, context.BossMechanicDetector, config, out var tankBusterSource);
        var shouldApplyForTankBuster = tankBusterImminent &&
            (tankBusterSource == "Timeline" ||
             context.BossMechanicDetector?.PredictedTankBuster?.TargetTankEntityId == tank.EntityId);

        var hpThreshold = isAtMaxCharges ? 0.98f : 0.95f;
        var shouldApplyStandard = tankHpPct < hpThreshold;
        var shouldApplyToAvoidCap = isAtMaxCharges && tankDamageRate > 0;

        if (!shouldApplyProactively && !shouldApplyStandard && !shouldApplyToAvoidCap && !shouldApplyForTankBuster)
            return;

        var capturedTank = tank;
        var capturedTankHpPct = tankHpPct;
        var capturedTankDamageRate = tankDamageRate;
        var capturedShouldApplyProactively = shouldApplyProactively;
        var capturedShouldApplyForTankBuster = shouldApplyForTankBuster;
        var capturedShouldApplyToAvoidCap = shouldApplyToAvoidCap;
        var capturedShouldApplyStandard = shouldApplyStandard;
        var capturedTankBusterSource = tankBusterSource;
        var capturedChargeInfo = $"{currentCharges}/{maxCharges}";
        var capturedHpThreshold = hpThreshold;

        scheduler.PushOgcd(ApolloAbilities.DivineBenison, tank.GameObjectId, priority: 110,
            onDispatched: _ =>
            {
                var tankName = capturedTank.Name?.TextValue ?? "Unknown";
                string reason;
                string logReason;

                if (capturedShouldApplyForTankBuster)
                {
                    var tbPrediction = TimelineHelper.GetNextTankBuster(
                        context.TimelineService, context.BossMechanicDetector, config);
                    var secondsUntil = tbPrediction?.secondsUntil ?? 0;
                    reason = $"tank buster in {secondsUntil:F1}s ({capturedTankBusterSource}) ({capturedChargeInfo})";
                    logReason = $"Tank buster predicted ({capturedTankBusterSource}) ({capturedChargeInfo})";
                }
                else if (capturedShouldApplyToAvoidCap && !capturedShouldApplyProactively && !capturedShouldApplyStandard)
                {
                    reason = $"avoiding cap ({capturedChargeInfo} charges)";
                    logReason = $"At max charges - using to avoid cap ({capturedChargeInfo})";
                }
                else if (capturedShouldApplyProactively)
                {
                    reason = $"proactive, DPS {capturedTankDamageRate:F0} ({capturedChargeInfo})";
                    logReason = $"Proactive (high damage rate) ({capturedChargeInfo})";
                }
                else
                {
                    reason = $"{capturedTankHpPct:P0} HP ({capturedChargeInfo})";
                    logReason = $"Standard (HP threshold) ({capturedChargeInfo})";
                }

                context.Debug.PlannedAction = WHMActions.DivineBenison.Name;
                context.Debug.DefensiveState = $"Divine Benison on {tankName} ({reason})";
                context.LogDefensiveDecision(tankName, capturedTankHpPct, "Divine Benison", capturedTankDamageRate, logReason);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = capturedShouldApplyForTankBuster
                        ? $"Pre-tankbuster shield on {tankName}"
                        : capturedShouldApplyToAvoidCap
                            ? $"Avoiding charge cap on {tankName}"
                            : $"Shield on {tankName} at {capturedTankHpPct:P0}";

                    var factors = new[]
                    {
                        $"Tank HP: {capturedTankHpPct:P0}",
                        $"Tank damage rate: {capturedTankDamageRate:F0} DPS",
                        $"Charges: {capturedChargeInfo}",
                        capturedShouldApplyForTankBuster ? $"Tank buster predicted via {capturedTankBusterSource}" :
                        capturedShouldApplyToAvoidCap ? "At max charges - avoiding waste" :
                        capturedShouldApplyProactively ? "High sustained damage on tank" :
                        $"HP below {capturedHpThreshold:P0} threshold",
                    };

                    var alternatives = capturedShouldApplyForTankBuster
                        ? new[] { "Aquaveil (longer mitigation)", "Save for next tank buster" }
                        : new[] { "Hold for tank buster", "Use on different target" };

                    var tip = capturedShouldApplyForTankBuster
                        ? "Divine Benison before tank busters provides a solid shield - stack with other mitigations!"
                        : "Divine Benison has charges - don't let them cap! Use proactively on the tank.";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.DivineBenison.ActionId,
                        ActionName = "Divine Benison",
                        Category = "Defensive",
                        TargetName = tankName,
                        ShortReason = shortReason,
                        DetailedReason = $"Divine Benison provides a 500 potency shield on {tankName}. {(capturedShouldApplyForTankBuster ? $"Used proactively before predicted tank buster ({capturedTankBusterSource}). " : capturedShouldApplyToAvoidCap ? "Used to avoid wasting charge regeneration. " : "")}Tank HP: {capturedTankHpPct:P0}, damage rate: {capturedTankDamageRate:F0} DPS. Charges: {capturedChargeInfo}.",
                        Factors = factors,
                        Alternatives = alternatives,
                        Tip = tip,
                        ConceptId = WhmConcepts.DivineBenisonUsage,
                        Priority = capturedShouldApplyForTankBuster ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });
                }
            });
    }

    private void TryPushAquaveil(IApolloContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.Aquaveil, config,
            c => c.EnableHealing && c.Defensive.EnableAquaveil))
            return;

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank is null) return;
        if (StatusHelper.HasStatus(tank, StatusHelper.StatusIds.Aquaveil)) return;
        if (Vector3.DistanceSquared(player.Position, tank.Position) > WHMActions.Aquaveil.RangeSquared) return;

        var tankHpPct = context.PartyHelper.GetHpPercent(tank);
        var tankDamageRate = context.DamageIntakeService.GetDamageRate(tank.EntityId, 3f);

        var shouldApplyProactively = config.Defensive.EnableProactiveCooldowns &&
                                     tankDamageRate >= config.Defensive.ProactiveAquaveilDamageRate;

        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService, context.BossMechanicDetector, config, out var aquaveilTankBusterSource);
        var shouldApplyForTankBuster = tankBusterImminent &&
            (aquaveilTankBusterSource == "Timeline" ||
             context.BossMechanicDetector?.PredictedTankBuster?.TargetTankEntityId == tank.EntityId);

        var shouldApplyStandard = tankHpPct < 0.90f;

        if (!shouldApplyProactively && !shouldApplyStandard && !shouldApplyForTankBuster) return;

        var capturedTank = tank;
        var capturedTankHpPct = tankHpPct;
        var capturedTankDamageRate = tankDamageRate;
        var capturedShouldApplyProactively = shouldApplyProactively;
        var capturedShouldApplyForTankBuster = shouldApplyForTankBuster;
        var capturedTankBusterSource = aquaveilTankBusterSource;

        scheduler.PushOgcd(ApolloAbilities.Aquaveil, tank.GameObjectId, priority: 120,
            onDispatched: _ =>
            {
                var tankName = capturedTank.Name?.TextValue ?? "Unknown";
                string reason;
                string logReason;

                if (capturedShouldApplyForTankBuster)
                {
                    var tbPrediction = TimelineHelper.GetNextTankBuster(
                        context.TimelineService, context.BossMechanicDetector, config);
                    var secondsUntil = tbPrediction?.secondsUntil ?? 0;
                    reason = $"tank buster in {secondsUntil:F1}s ({capturedTankBusterSource})";
                    logReason = $"Tank buster predicted ({capturedTankBusterSource})";
                }
                else if (capturedShouldApplyProactively)
                {
                    reason = $"proactive, DPS {capturedTankDamageRate:F0}";
                    logReason = "Proactive (high damage rate)";
                }
                else
                {
                    reason = $"{capturedTankHpPct:P0} HP";
                    logReason = "Standard (HP threshold)";
                }

                context.Debug.PlannedAction = WHMActions.Aquaveil.Name;
                context.Debug.DefensiveState = $"Aquaveil on {tankName} ({reason})";
                context.LogDefensiveDecision(tankName, capturedTankHpPct, "Aquaveil", capturedTankDamageRate, logReason);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = capturedShouldApplyForTankBuster
                        ? $"Pre-tankbuster mitigation on {tankName}"
                        : $"Damage reduction on {tankName} at {capturedTankHpPct:P0}";

                    var factors = new[]
                    {
                        $"Tank HP: {capturedTankHpPct:P0}",
                        $"Tank damage rate: {capturedTankDamageRate:F0} DPS",
                        capturedShouldApplyForTankBuster ? $"Tank buster predicted via {capturedTankBusterSource}" :
                        capturedShouldApplyProactively ? "High sustained damage on tank" :
                        $"HP below 90% threshold",
                        "15% damage reduction for 8 seconds",
                    };

                    var alternatives = capturedShouldApplyForTankBuster
                        ? new[] { "Divine Benison (shield instead)", "Let tank handle it" }
                        : new[] { "Hold for tank buster", "Use Divine Benison first" };

                    var tip = capturedShouldApplyForTankBuster
                        ? "Aquaveil's 15% mitigation is great before tank busters - it reduces damage before shields absorb!"
                        : "Aquaveil is best used proactively when the tank is taking sustained damage.";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.Aquaveil.ActionId,
                        ActionName = "Aquaveil",
                        Category = "Defensive",
                        TargetName = tankName,
                        ShortReason = shortReason,
                        DetailedReason = $"Aquaveil provides 15% damage reduction on {tankName} for 8 seconds. {(capturedShouldApplyForTankBuster ? $"Used proactively before predicted tank buster ({capturedTankBusterSource}). " : "")}Tank HP: {capturedTankHpPct:P0}, damage rate: {capturedTankDamageRate:F0} DPS.",
                        Factors = factors,
                        Alternatives = alternatives,
                        Tip = tip,
                        ConceptId = WhmConcepts.AquaveilUsage,
                        Priority = capturedShouldApplyForTankBuster ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });
                }
            });
    }

    private void TryPushLiturgyOfTheBell(IApolloContext context, RotationScheduler scheduler, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.LiturgyOfTheBell, config,
            c => c.Defensive.EnableLiturgyOfTheBell))
            return;
        if (injuredCount < 2) return;

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

        var partyCoord = context.PartyCoordinationService;
        if (config.PartyCoordination.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(config.PartyCoordination.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.DefensiveState = "Bell skipped (remote mit)";
            return;
        }

        if (config.PartyCoordination.EnableHealerBurstAwareness &&
            config.PartyCoordination.DelayMitigationsDuringBurst &&
            partyCoord != null)
        {
            var (avgHpPercent, _, _) = context.PartyHealthMetrics;
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsActive && avgHpPercent > config.Healing.GcdEmergencyThreshold)
            {
                context.Debug.DefensiveState = "Bell delayed (burst active)";
                return;
            }
        }

        var capturedTargetName = targetName;
        var capturedInjured = injuredCount;

        scheduler.PushGroundTargetedOgcd(ApolloAbilities.LiturgyOfTheBell, targetPosition, priority: 130,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WHMActions.LiturgyOfTheBell.Name;
                context.Debug.DefensiveState = $"Bell placed at {capturedTargetName} ({capturedInjured} injured)";
                partyCoord?.OnCooldownUsed(WHMActions.LiturgyOfTheBell.ActionId, 180_000);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Liturgy placed near {capturedTargetName} - {capturedInjured} injured";
                    var factors = new[]
                    {
                        $"Injured count: {capturedInjured}",
                        $"Placement: Near {capturedTargetName}",
                        "Heals party when they take damage",
                        "5 stacks, triggers on damage",
                        "180 second cooldown",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.LiturgyOfTheBell.ActionId,
                        ActionName = "Liturgy of the Bell",
                        Category = "Defensive",
                        TargetName = capturedTargetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Liturgy of the Bell placed near {capturedTargetName}. This ground-targeted ability heals party members when they take damage, with 5 charges that trigger automatically. Used because {capturedInjured} party members are injured and more damage is expected.",
                        Factors = factors,
                        Alternatives = _liturgyOfTheBellAlternatives,
                        Tip = "Place Liturgy where the party will stack - it triggers on damage, so it's perfect for multi-hit raidwides!",
                        ConceptId = WhmConcepts.LiturgyOfTheBellUsage,
                        Priority = ExplanationPriority.High,
                    });
                }
            });
    }
}
