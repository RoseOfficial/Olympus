using System;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.RoleActionHelpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Handles buff and utility oGCDs for the WHM rotation (scheduler-driven).
/// </summary>
public sealed class BuffModule : IApolloModule
{
    private const int RaiseMpCost = 2400;

    private static readonly string[] _thinAirAlternatives =
    {
        "Save for Raise (2400 MP saved)",
        "Save for AoE heal (1000 MP saved)",
        "Use for single-target heal",
    };

    private static readonly string[] _presenceOfMindFactors =
    {
        "20% spell speed increase for 15 seconds",
        "Affects both damage spells and heals",
        "More DPS and faster emergency response",
        "120 second cooldown",
    };

    private static readonly string[] _presenceOfMindAlternatives =
    {
        "Hold for burst window",
        "Stack with Assize for more casts",
        "Save for healing emergency",
    };

    public int Priority => 30;
    public string Name => "Buffs";

    public bool TryExecute(IApolloContext context, bool isMoving) => false;

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) return;

        // Push priorities mirror the legacy first-true-wins ordering inside this module.
        // Healing/Defensive use lower numbers so they always win in the unified queue.
        TryPushThinAir(context, scheduler);
        TryPushPresenceOfMind(context, scheduler);
        TryPushAsylum(context, scheduler);
        TryPushAssize(context, scheduler);
        TryPushLucidDreaming(context, scheduler);
        TryPushSurecast(context, scheduler);
        if (!isMoving)
            TryPushAetherialShift(context, scheduler);
    }

    public void UpdateDebugState(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableThinAir) context.Debug.ThinAirState = "Disabled";
        else if (player.Level < WHMActions.ThinAir.MinLevel) context.Debug.ThinAirState = $"Level {player.Level} < 58";
        else if (context.HasThinAir) context.Debug.ThinAirState = "Already active";
        else if (!context.ActionService.IsActionReady(WHMActions.ThinAir.ActionId)) context.Debug.ThinAirState = "On cooldown";
        else context.Debug.ThinAirState = "Ready";
    }

    private void TryPushThinAir(IApolloContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableThinAir) { context.Debug.ThinAirState = "Disabled"; return; }
        if (player.Level < WHMActions.ThinAir.MinLevel) { context.Debug.ThinAirState = $"Level {player.Level} < 58"; return; }
        if (!context.ActionService.IsActionReady(WHMActions.ThinAir.ActionId)) { context.Debug.ThinAirState = "On cooldown"; return; }
        if (context.HasThinAir) { context.Debug.ThinAirState = "Already active"; return; }

        var currentCharges = context.ActionService.GetCurrentCharges(WHMActions.ThinAir.ActionId);
        var maxCharges = context.ActionService.GetMaxCharges(WHMActions.ThinAir.ActionId, 0);
        var isAtMaxCharges = currentCharges >= maxCharges && maxCharges > 0;
        var chargeInfo = $"{currentCharges}/{maxCharges}";

        var shouldUseThinAir = false;
        var usageReason = "";

        if (isAtMaxCharges)
        {
            shouldUseThinAir = true;
            usageReason = WillCastExpensiveSpell(context)
                ? $"Avoiding cap, expensive spell incoming ({chargeInfo} charges)"
                : $"Avoiding cap, spending on next GCD ({chargeInfo} charges)";
        }

        if (!shouldUseThinAir && config.Buffs.EnableMpConservation)
        {
            var secondsUntilOom = context.MpForecastService.SecondsUntilOom(RaiseMpCost);
            if (secondsUntilOom < 30f && context.MpForecastService.IsInConservationMode && WillCastExpensiveSpell(context))
            {
                shouldUseThinAir = true;
                usageReason = $"MP Conservation (OOM in {secondsUntilOom:F0}s) ({chargeInfo})";
            }
        }

        if (!shouldUseThinAir && config.Resurrection.EnableRaise && player.CurrentMp >= RaiseMpCost)
        {
            var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
            if (deadMember is not null)
            {
                var swiftcastReady = RoleActionGates.SwiftcastReady(context);
                if (context.HasSwiftcast || swiftcastReady || config.Resurrection.AllowHardcastRaise)
                {
                    shouldUseThinAir = true;
                    usageReason = $"For Raise ({chargeInfo})";
                }
            }
        }

        if (!shouldUseThinAir && config.EnableHealing)
        {
            var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
            var medicaHealAmount = WHMActions.Medica.EstimateHealAmount(mind, det, wd, player.Level);
            var (injuredCount, _, _, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, medicaHealAmount);
            if (injuredCount >= config.Healing.AoEHealMinTargets)
            {
                shouldUseThinAir = true;
                usageReason = $"For AoE Heal ({chargeInfo})";
            }
        }

        if (!shouldUseThinAir && config.EnableHealing && player.Level >= WHMActions.CureII.MinLevel)
        {
            var target = context.PartyHelper.FindLowestHpPartyMember(player);
            if (target is not null)
            {
                var hpPercent = context.PartyHelper.GetHpPercent(target);
                if (hpPercent < 0.80f)
                {
                    shouldUseThinAir = true;
                    usageReason = $"For Cure II ({chargeInfo})";
                }
            }
        }

        if (!shouldUseThinAir) { context.Debug.ThinAirState = $"Not needed ({chargeInfo})"; return; }

        var capturedReason = usageReason;
        var capturedChargeInfo = chargeInfo;
        var capturedIsAtMaxCharges = isAtMaxCharges;

        scheduler.PushOgcd(ApolloAbilities.ThinAir, player.GameObjectId, priority: 200,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WHMActions.ThinAir.Name;
                context.Debug.ThinAirState = capturedReason;

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Thin Air - {capturedReason}";
                    var factors = new[]
                    {
                        $"Charges: {capturedChargeInfo}",
                        $"Current MP: {player.CurrentMp:N0}",
                        capturedReason,
                        "Makes next spell cost 0 MP",
                    };

                    var tip = capturedIsAtMaxCharges
                        ? "Don't let Thin Air charges cap - use them for any expensive spell!"
                        : "Thin Air is best used for Raise or AoE heals to maximize MP savings.";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.ThinAir.ActionId,
                        ActionName = "Thin Air",
                        Category = "Buff",
                        TargetName = null,
                        ShortReason = shortReason,
                        DetailedReason = $"Thin Air makes the next GCD spell free (0 MP cost). {capturedReason}. Current MP: {player.CurrentMp:N0}, charges: {capturedChargeInfo}.",
                        Factors = factors,
                        Alternatives = _thinAirAlternatives,
                        Tip = tip,
                        ConceptId = WhmConcepts.OgcdWeaving,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }

    private void TryPushPresenceOfMind(IApolloContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (player.ClassJob.RowId == JobRegistry.Conjurer) return;
        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.PresenceOfMind, config,
            c => c.Buffs.EnablePresenceOfMind))
            return;

        if (config.Buffs.DelayPoMForRaise && config.Resurrection.EnableRaise)
        {
            var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
            if (deadMember is not null)
            {
                var swiftcastReady = RoleActionGates.SwiftcastReady(context);
                var swiftcastCooldown = context.ActionService.GetCooldownRemaining(RoleActions.Swiftcast.ActionId);
                if (!swiftcastReady && swiftcastCooldown <= config.Buffs.PoMRaiseDelayCooldown)
                {
                    context.Debug.PoMState = $"Delayed for Raise (Swiftcast in {swiftcastCooldown:F1}s)";
                    return;
                }
            }
        }

        if (config.Buffs.StackPoMWithAssize && player.Level >= WHMActions.Assize.MinLevel)
        {
            var assizeReady = context.ActionService.IsActionReady(WHMActions.Assize.ActionId);
            var assizeCooldown = context.ActionService.GetCooldownRemaining(WHMActions.Assize.ActionId);
            if (!assizeReady && assizeCooldown <= 5f && assizeCooldown > 0)
            {
                context.Debug.PoMState = $"Waiting for Assize ({assizeCooldown:F1}s)";
                return;
            }
        }

        scheduler.PushOgcd(ApolloAbilities.PresenceOfMind, player.GameObjectId, priority: 210,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WHMActions.PresenceOfMind.Name;
                context.Debug.PoMState = "Executed";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.PresenceOfMind.ActionId,
                        ActionName = "Presence of Mind",
                        Category = "Buff",
                        TargetName = null,
                        ShortReason = "Presence of Mind - 20% spell speed buff",
                        DetailedReason = "Presence of Mind increases spell speed by 20% for 15 seconds. This means more Glares (DPS) and faster emergency heals. Used on cooldown for maximum value.",
                        Factors = _presenceOfMindFactors,
                        Alternatives = _presenceOfMindAlternatives,
                        Tip = "Presence of Mind is your DPS buff - use it on cooldown during damage phases!",
                        ConceptId = WhmConcepts.DpsOptimization,
                        Priority = ExplanationPriority.Low,
                    });
                }
            });
    }

    private void TryPushAsylum(IApolloContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Healing.EnableAsylum) { context.Debug.AsylumState = "Disabled"; return; }
        if (player.Level < WHMActions.Asylum.MinLevel)
        {
            context.Debug.AsylumState = $"Level {player.Level} < {WHMActions.Asylum.MinLevel}";
            return;
        }
        if (!context.ActionService.IsActionReady(WHMActions.Asylum.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(WHMActions.Asylum.ActionId);
            context.Debug.AsylumState = $"CD: {cd:F1}s";
            return;
        }

        var raidwideInfo = TimelineHelper.GetNextRaidwide(
            context.TimelineService, context.BossMechanicDetector, config);
        var shouldDeployForRaidwide = false;
        var raidwideSource = "None";
        if (raidwideInfo.HasValue)
        {
            var secondsUntil = raidwideInfo.Value.secondsUntil;
            raidwideSource = raidwideInfo.Value.source;
            if (secondsUntil >= 3f && secondsUntil <= 8f) shouldDeployForRaidwide = true;
            else if (secondsUntil < 3f)
            {
                context.Debug.AsylumState = $"Too late for raidwide in {secondsUntil:F1}s ({raidwideSource})";
            }
        }

        var shouldDeployForBurst = false;
        var partyCoord = context.PartyCoordinationService;
        if (config.PartyCoordination.EnableHealerBurstAwareness &&
            config.PartyCoordination.PreferShieldsBeforeBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
                shouldDeployForBurst = true;
        }

        if (!shouldDeployForRaidwide && !shouldDeployForBurst)
        {
            var (_, _, injuredCount) = context.PartyHealthMetrics;
            if (injuredCount == 0) { context.Debug.AsylumState = "Holding (party healthy)"; return; }
        }

        var tank = context.PartyHelper.FindTankInParty(player);
        Vector3 targetPosition;
        string targetName;

        if (tank is not null)
        {
            targetName = tank.Name?.TextValue ?? "Unknown";
            var distance = Vector3.Distance(player.Position, tank.Position);
            if (distance > WHMActions.Asylum.Range)
            {
                context.Debug.AsylumState = $"Tank out of range ({distance:F1}y > {WHMActions.Asylum.Range}y)";
                context.Debug.AsylumTarget = targetName;
                return;
            }
            targetPosition = tank.Position;
            context.Debug.AsylumTarget = targetName;
        }
        else
        {
            targetPosition = player.Position;
            targetName = "Self";
            context.Debug.AsylumTarget = "Self";
        }

        if (partyCoord?.WouldOverlapWithRemoteGroundEffect(
            targetPosition, WHMActions.Asylum.ActionId,
            config.PartyCoordination.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.AsylumState = "Skipped (area covered by co-healer)";
            return;
        }

        var capturedTargetName = targetName;
        var capturedIsRaidwide = shouldDeployForRaidwide;
        var capturedIsBurst = shouldDeployForBurst;
        var capturedRaidwideSource = raidwideSource;
        var capturedRaidwideInfo = raidwideInfo;
        var capturedTank = tank;

        scheduler.PushGroundTargetedOgcd(ApolloAbilities.Asylum, targetPosition, priority: 95,
            onDispatched: _ =>
            {
                partyCoord?.OnGroundEffectPlaced(WHMActions.Asylum.ActionId, targetPosition);

                var reason = capturedIsRaidwide
                    ? $"pre-raidwide via {capturedRaidwideSource}"
                    : capturedIsBurst
                        ? "pre-burst"
                        : $"on {capturedTargetName}";
                context.Debug.PlannedAction = $"Asylum ({reason})";
                context.Debug.AsylumState = capturedIsRaidwide
                    ? $"Pre-raidwide ({capturedRaidwideSource})"
                    : capturedIsBurst
                        ? "Pre-burst"
                        : "Executed";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = capturedIsRaidwide
                        ? $"Pre-raidwide Asylum near {capturedTargetName}"
                        : capturedIsBurst
                            ? $"Pre-burst Asylum near {capturedTargetName}"
                            : $"Asylum placed near {capturedTargetName}";

                    var factors = new[]
                    {
                        $"Placement: Near {capturedTargetName}",
                        "100 potency HoT every 3s for 24s",
                        "10% healing increase inside",
                        capturedIsRaidwide ? $"Raidwide predicted in {capturedRaidwideInfo?.secondsUntil:F1}s via {capturedRaidwideSource}" :
                        capturedIsBurst ? "DPS burst window approaching" :
                        "General healing support",
                    };

                    var alternatives = new[]
                    {
                        "Wait for better positioning",
                        "Save for raidwide phase",
                        "Use direct heals instead",
                    };

                    var tip = capturedIsRaidwide
                        ? "Deploying Asylum 5-8 seconds before raidwides lets the HoT tick before damage, and the healing boost helps recovery!"
                        : "Place Asylum where the party will stack - the healing boost affects all heals inside!";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.Asylum.ActionId,
                        ActionName = "Asylum",
                        Category = "Healing",
                        TargetName = capturedTargetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Asylum placed near {capturedTargetName}. This ground-targeted HoT heals for 100 potency every 3 seconds and increases healing received by 10%. {(capturedIsRaidwide ? $"Deployed proactively before predicted raidwide ({capturedRaidwideSource})." : capturedIsBurst ? "Deployed before DPS burst window for sustained healing." : "")}",
                        Factors = factors,
                        Alternatives = alternatives,
                        Tip = tip,
                        ConceptId = WhmConcepts.ProactiveHealing,
                        Priority = capturedIsRaidwide ? ExplanationPriority.High : ExplanationPriority.Normal,
                    });
                }
            });
    }

    private void TryPushAssize(IApolloContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.Assize, config,
            c => c.Healing.EnableAssize))
            return;

        scheduler.PushOgcd(ApolloAbilities.AssizeBuff, player.GameObjectId, priority: 220,
            onDispatched: _ => { context.Debug.PlannedAction = WHMActions.Assize.Name; });
    }

    private void TryPushLucidDreaming(IApolloContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;
        var mpPercent = context.MpForecastService.MpPercent;

        if (!config.HealerShared.EnableLucidDreaming) return;
        if (player.Level < RoleActions.LucidDreaming.MinLevel) return;
        if (!context.ActionService.IsActionReady(RoleActions.LucidDreaming.ActionId)) return;
        if (context.MpForecastService.IsLucidDreamingActive) return;

        var shouldUseLucid = false;
        var reason = string.Empty;

        if (config.Buffs.EnablePredictiveLucid)
        {
            var timeUntilLow = context.MpForecastService.GetTimeUntilMpBelowThreshold(
                config.Buffs.LucidPredictionThreshold);
            if (timeUntilLow <= config.Buffs.LucidPredictionLookahead)
            {
                shouldUseLucid = true;
                reason = $"Predictive (MP below {config.Buffs.LucidPredictionThreshold} in {timeUntilLow:F0}s)";
            }
        }

        if (!shouldUseLucid)
        {
            var threshold = 0.70f;
            if (context.MpForecastService.IsInConservationMode) threshold = 0.80f;
            if (config.Buffs.EnableRaisePrepMode && ShouldEnterRaisePrepMode(context)) threshold = 0.90f;
            if (mpPercent < threshold)
            {
                shouldUseLucid = true;
                reason = $"MP below {threshold:P0} threshold";
            }
        }

        if (!shouldUseLucid) return;

        var capturedReason = reason;
        scheduler.PushOgcd(ApolloAbilities.LucidDreaming, player.GameObjectId, priority: 230,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.LucidDreaming.Name;
                context.Debug.LucidState = capturedReason;
            });
    }

    private bool ShouldEnterRaisePrepMode(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
        if (deadMember is null) return false;

        var mpPercent = context.MpForecastService.MpPercent;
        return mpPercent < config.Buffs.RaisePrepMpThreshold;
    }

    private void TryPushSurecast(IApolloContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.RoleActions.EnableSurecast) { context.Debug.SurecastState = "Disabled"; return; }
        if (config.RoleActions.SurecastMode == 0) { context.Debug.SurecastState = "Manual mode"; return; }
        if (player.Level < RoleActions.Surecast.MinLevel)
        {
            context.Debug.SurecastState = $"Level {player.Level} < {RoleActions.Surecast.MinLevel}";
            return;
        }
        if (StatusHelper.HasStatus(player, StatusHelper.StatusIds.Surecast)) { context.Debug.SurecastState = "Already active"; return; }
        if (!context.ActionService.IsActionReady(RoleActions.Surecast.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(RoleActions.Surecast.ActionId);
            context.Debug.SurecastState = $"CD: {cd:F1}s";
            return;
        }

        if (config.RoleActions.SurecastMode == 1)
        {
            scheduler.PushOgcd(ApolloAbilities.Surecast, player.GameObjectId, priority: 240,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RoleActions.Surecast.Name;
                    context.Debug.SurecastState = "Executed";
                });
        }
        else
        {
            context.Debug.SurecastState = "Ready";
        }
    }

    private bool WillCastExpensiveSpell(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (config.Resurrection.EnableRaise)
        {
            var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
            if (deadMember is not null) return true;
        }

        if (config.EnableHealing)
        {
            var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
            var healAmount = WHMActions.Medica.EstimateHealAmount(mind, det, wd, player.Level);
            var (injuredCount, _, _, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, healAmount);
            if (injuredCount >= config.Healing.AoEHealMinTargets) return true;
        }

        if (config.EnableHealing && player.Level >= WHMActions.CureII.MinLevel)
        {
            var target = context.PartyHelper.FindLowestHpPartyMember(player);
            if (target is not null)
            {
                var hpPercent = context.PartyHelper.GetHpPercent(target);
                if (hpPercent < config.Healing.GcdEmergencyThreshold) return true;
            }
        }

        return false;
    }

    private void TryPushAetherialShift(IApolloContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableAetherialShift) return;
        if (!ActionValidator.IsAvailable(player, context.ActionService, WHMActions.AetherialShift)) return;

        const float dashDistance = 15f;
        var spellRange = WHMActions.Stone.Range;
        var target = context.TargetingService.FindEnemy(
            config.Targeting.EnemyStrategy, spellRange + dashDistance, player);
        if (target is null) return;

        var distance = Vector3.Distance(player.Position, target.Position);
        if (distance <= spellRange) return;
        if (distance < 0.01f) return;

        var toTarget = Vector3.Normalize(target.Position - player.Position);
        var playerForward = new Vector3(MathF.Sin(player.Rotation), 0, MathF.Cos(player.Rotation));
        var dot = Vector3.Dot(playerForward, new Vector3(toTarget.X, 0, toTarget.Z));
        if (dot < 0.7f) return;

        scheduler.PushOgcd(ApolloAbilities.AetherialShift, player.GameObjectId, priority: 250,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WHMActions.AetherialShift.Name + " (gap close)";
            });
    }
}
