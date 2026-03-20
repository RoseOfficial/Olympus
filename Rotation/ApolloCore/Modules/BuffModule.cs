using System;
using System.Numerics;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Handles buff and utility oGCDs for the WHM rotation.
/// Includes Thin Air, Presence of Mind, Assize, Asylum, Lucid Dreaming, Surecast, Aetherial Shift.
/// </summary>
public sealed class BuffModule : IApolloModule
{
    private const int RaiseMpCost = 2400;

    // Training explanation arrays
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

    public int Priority => 30; // After healing/defensive, before damage
    public string Name => "Buffs";

    public bool TryExecute(IApolloContext context, bool isMoving)
    {
        if (!context.CanExecuteOgcd)
            return false;

        // Priority 1: Thin Air before expensive casts
        if (TryExecuteThinAir(context))
            return true;

        // Priority 2: Presence of Mind on cooldown (DPS buff)
        if (TryExecutePresenceOfMind(context))
            return true;

        // Priority 3: Asylum on cooldown (ground-targeted HoT)
        if (TryExecuteAsylum(context))
            return true;

        // Priority 4: Assize on cooldown (DPS oGCD that also heals)
        if (TryExecuteAssize(context))
            return true;

        // Priority 5: Lucid Dreaming for MP
        if (TryExecuteLucidDreaming(context))
            return true;

        // Priority 6: Surecast for knockback immunity
        if (TryExecuteSurecast(context))
            return true;

        // Priority 7: Aetherial Shift for gap closing
        if (!isMoving)
            TryExecuteAetherialShift(context);

        return false;
    }

    public void UpdateDebugState(IApolloContext context)
    {
        // Update Thin Air state
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableThinAir)
        {
            context.Debug.ThinAirState = "Disabled";
        }
        else if (player.Level < WHMActions.ThinAir.MinLevel)
        {
            context.Debug.ThinAirState = $"Level {player.Level} < 58";
        }
        else if (context.HasThinAir)
        {
            context.Debug.ThinAirState = "Already active";
        }
        else if (!context.ActionService.IsActionReady(WHMActions.ThinAir.ActionId))
        {
            context.Debug.ThinAirState = "On cooldown";
        }
        else
        {
            context.Debug.ThinAirState = "Ready";
        }
    }

    private bool TryExecuteThinAir(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableThinAir)
        {
            context.Debug.ThinAirState = "Disabled";
            return false;
        }

        // CNJ max level is 30, ThinAir requires 58 — implicit CNJ guard via level check
        if (player.Level < WHMActions.ThinAir.MinLevel)
        {
            context.Debug.ThinAirState = $"Level {player.Level} < 58";
            return false;
        }

        if (!context.ActionService.IsActionReady(WHMActions.ThinAir.ActionId))
        {
            context.Debug.ThinAirState = "On cooldown";
            return false;
        }

        if (context.HasThinAir)
        {
            context.Debug.ThinAirState = "Already active";
            return false;
        }

        // Get charge information for smarter usage
        var currentCharges = context.ActionService.GetCurrentCharges(WHMActions.ThinAir.ActionId);
        var maxCharges = context.ActionService.GetMaxCharges(WHMActions.ThinAir.ActionId, 0);
        var isAtMaxCharges = currentCharges >= maxCharges && maxCharges > 0;
        var chargeInfo = $"{currentCharges}/{maxCharges}";

        var shouldUseThinAir = false;
        var usageReason = "";

        // Priority 0: At max charges - always use to avoid wasting charge regen
        // If an expensive spell is incoming, prefer that; otherwise spend on next GCD (even Glare/Dia)
        if (isAtMaxCharges)
        {
            shouldUseThinAir = true;
            usageReason = WillCastExpensiveSpell(context)
                ? $"Avoiding cap, expensive spell incoming ({chargeInfo} charges)"
                : $"Avoiding cap, spending on next GCD ({chargeInfo} charges)";
            context.Debug.ThinAirState = usageReason;
        }

        // Priority 1: MP Conservation Mode - use Thin Air for any expensive spell when running low
        if (!shouldUseThinAir && config.Buffs.EnableMpConservation)
        {
            var secondsUntilOom = context.MpForecastService.SecondsUntilOom(RaiseMpCost);
            if (secondsUntilOom < 30f && context.MpForecastService.IsInConservationMode)
            {
                // Use Thin Air for any upcoming expensive spell to conserve MP
                if (WillCastExpensiveSpell(context))
                {
                    shouldUseThinAir = true;
                    usageReason = $"MP Conservation (OOM in {secondsUntilOom:F0}s) ({chargeInfo})";
                    context.Debug.ThinAirState = usageReason;
                }
            }
        }

        // Priority 2: Raise incoming (highest MP cost at 2400)
        if (!shouldUseThinAir && config.Resurrection.EnableRaise && player.CurrentMp >= RaiseMpCost)
        {
            var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
            if (deadMember is not null)
            {
                var swiftcastReady = context.ActionService.IsActionReady(RoleActions.Swiftcast.ActionId);

                if (context.HasSwiftcast || swiftcastReady || config.Resurrection.AllowHardcastRaise)
                {
                    shouldUseThinAir = true;
                    usageReason = $"For Raise ({chargeInfo})";
                    context.Debug.ThinAirState = usageReason;
                }
            }
        }

        // Priority 3: High-cost AoE heal incoming
        if (!shouldUseThinAir && config.EnableHealing)
        {
            var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
            var medicaHealAmount = WHMActions.Medica.EstimateHealAmount(mind, det, wd, player.Level);

            var (injuredCount, _, _, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, medicaHealAmount);

            if (injuredCount >= config.Healing.AoEHealMinTargets)
            {
                shouldUseThinAir = true;
                usageReason = $"For AoE Heal ({chargeInfo})";
                context.Debug.ThinAirState = usageReason;
            }
        }

        // Priority 4: High-cost single heal incoming
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
                    context.Debug.ThinAirState = usageReason;
                }
            }
        }

        if (!shouldUseThinAir)
        {
            context.Debug.ThinAirState = $"Not needed ({chargeInfo})";
            return false;
        }

        if (ActionExecutor.ExecuteOgcd(context, WHMActions.ThinAir, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentMp))
        {
            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = $"Thin Air - {usageReason}";

                var factors = new[]
                {
                    $"Charges: {chargeInfo}",
                    $"Current MP: {player.CurrentMp:N0}",
                    usageReason,
                    "Makes next spell cost 0 MP",
                };

                var alternatives = _thinAirAlternatives;

                var tip = isAtMaxCharges
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
                    DetailedReason = $"Thin Air makes the next GCD spell free (0 MP cost). {usageReason}. Current MP: {player.CurrentMp:N0}, charges: {chargeInfo}.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = WhmConcepts.OgcdWeaving,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        context.Debug.ThinAirState = "Execution failed";
        return false;
    }

    private bool TryExecutePresenceOfMind(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        // Presence of Mind is a WHM-only ability (requires level 30 WHM).
        // CNJ max level is 30, but PresenceOfMind is not available to CNJ in the game data —
        // adding an explicit guard here rather than relying on IsActionReady as an implicit gate.
        if (player.ClassJob.RowId == JobRegistry.Conjurer)
            return false;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.PresenceOfMind, config,
            c => c.Buffs.EnablePresenceOfMind))
            return false;

        // Check if we should delay PoM for an incoming raise
        if (config.Buffs.DelayPoMForRaise && config.Resurrection.EnableRaise)
        {
            var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
            if (deadMember is not null)
            {
                // Check if Swiftcast is ready or coming soon
                var swiftcastReady = context.ActionService.IsActionReady(RoleActions.Swiftcast.ActionId);
                var swiftcastCooldown = context.ActionService.GetCooldownRemaining(RoleActions.Swiftcast.ActionId);

                // Don't use PoM if Swiftcast is about to be ready and we need to raise
                if (!swiftcastReady && swiftcastCooldown <= config.Buffs.PoMRaiseDelayCooldown)
                {
                    context.Debug.PoMState = $"Delayed for Raise (Swiftcast in {swiftcastCooldown:F1}s)";
                    return false;
                }
            }
        }

        // Check if we should wait to stack PoM with Assize
        if (config.Buffs.StackPoMWithAssize && player.Level >= WHMActions.Assize.MinLevel)
        {
            var assizeReady = context.ActionService.IsActionReady(WHMActions.Assize.ActionId);
            var assizeCooldown = context.ActionService.GetCooldownRemaining(WHMActions.Assize.ActionId);

            // If Assize is almost ready (within 5s), delay PoM to stack them
            if (!assizeReady && assizeCooldown <= 5f && assizeCooldown > 0)
            {
                context.Debug.PoMState = $"Waiting for Assize ({assizeCooldown:F1}s)";
                return false;
            }
        }

        if (ActionExecutor.ExecuteOgcd(context, WHMActions.PresenceOfMind, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp))
        {
            context.Debug.PoMState = "Executed";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = "Presence of Mind - 20% spell speed buff";

                var factors = _presenceOfMindFactors;
                var alternatives = _presenceOfMindAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = WHMActions.PresenceOfMind.ActionId,
                    ActionName = "Presence of Mind",
                    Category = "Buff",
                    TargetName = null,
                    ShortReason = shortReason,
                    DetailedReason = "Presence of Mind increases spell speed by 20% for 15 seconds. This means more Glares (DPS) and faster emergency heals. Used on cooldown for maximum value.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Presence of Mind is your DPS buff - use it on cooldown during damage phases!",
                    ConceptId = WhmConcepts.DpsOptimization,
                    Priority = ExplanationPriority.Low,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryExecuteAsylum(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Healing.EnableAsylum)
        {
            context.Debug.AsylumState = "Disabled";
            return false;
        }

        if (player.Level < WHMActions.Asylum.MinLevel)
        {
            context.Debug.AsylumState = $"Level {player.Level} < {WHMActions.Asylum.MinLevel}";
            return false;
        }

        if (!context.ActionService.IsActionReady(WHMActions.Asylum.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(WHMActions.Asylum.ActionId);
            context.Debug.AsylumState = $"CD: {cd:F1}s";
            return false;
        }

        // Check if a raidwide is coming (timeline-aware)
        // Asylum is a HoT that needs time to tick, so deploy 5-8 seconds before raidwide
        var raidwideInfo = TimelineHelper.GetNextRaidwide(
            context.TimelineService,
            context.BossMechanicDetector,
            config.Healing);

        var shouldDeployForRaidwide = false;
        var raidwideSource = "None";
        if (raidwideInfo.HasValue)
        {
            var secondsUntil = raidwideInfo.Value.secondsUntil;
            raidwideSource = raidwideInfo.Value.source;

            // Ideal window: 5-8 seconds before raidwide (HoT needs time to tick)
            // Too close (<3s): HoT won't have time to provide meaningful healing
            // Too far (>8s): Save for later, Asylum might not cover the mechanic
            if (secondsUntil >= 3f && secondsUntil <= 8f)
            {
                shouldDeployForRaidwide = true;
            }
            else if (secondsUntil < 3f)
            {
                context.Debug.AsylumState = $"Too late for raidwide in {secondsUntil:F1}s ({raidwideSource})";
                // Don't return false here - fall through to standard logic
            }
        }

        // Burst awareness: Deploy Asylum proactively before burst windows
        // HoTs provide sustained healing during high-damage DPS phases
        var shouldDeployForBurst = false;
        var partyCoord = context.PartyCoordinationService;
        if (config.PartyCoordination.EnableHealerBurstAwareness &&
            config.PartyCoordination.PreferShieldsBeforeBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            // Deploy Asylum 3-8 seconds before burst (similar to raidwide logic)
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
            {
                shouldDeployForBurst = true;
            }
        }

        // Standard deployment: only if party actually needs healing.
        // Raidwide and burst proactive paths bypass this check intentionally.
        if (!shouldDeployForRaidwide && !shouldDeployForBurst)
        {
            var (_, _, injuredCount) = context.PartyHealthMetrics;
            if (injuredCount == 0)
            {
                context.Debug.AsylumState = "Holding (party healthy)";
                return false;
            }
        }

        var tank = context.PartyHelper.FindTankInParty(player);
        Vector3 targetPosition;

        if (tank is not null)
        {
            var tankName = tank.Name?.TextValue ?? "Unknown";
            var distance = Vector3.Distance(player.Position, tank.Position);
            if (distance > WHMActions.Asylum.Range)
            {
                context.Debug.AsylumState = $"Tank out of range ({distance:F1}y > {WHMActions.Asylum.Range}y)";
                context.Debug.AsylumTarget = tankName;
                return false;
            }
            targetPosition = tank.Position;
            context.Debug.AsylumTarget = tankName;
        }
        else
        {
            targetPosition = player.Position;
            context.Debug.AsylumTarget = "Self";
        }

        // Check if another Olympus healer already has a ground effect in this area
        if (partyCoord?.WouldOverlapWithRemoteGroundEffect(
            targetPosition,
            WHMActions.Asylum.ActionId,
            config.PartyCoordination.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.AsylumState = "Skipped (area covered by co-healer)";
            return false;
        }

        // Execute with appropriate reason
        var reason = shouldDeployForRaidwide
            ? $"pre-raidwide via {raidwideSource}"
            : shouldDeployForBurst
                ? "pre-burst"
                : $"on {context.Debug.AsylumTarget}";

        if (ActionExecutor.ExecuteGroundTargeted(context, WHMActions.Asylum, targetPosition,
            context.Debug.AsylumTarget, tank?.CurrentHp ?? player.CurrentHp,
            $"Asylum ({reason})"))
        {
            // Broadcast ground effect placement to other Olympus instances
            partyCoord?.OnGroundEffectPlaced(WHMActions.Asylum.ActionId, targetPosition);

            context.Debug.AsylumState = shouldDeployForRaidwide
                ? $"Pre-raidwide ({raidwideSource})"
                : shouldDeployForBurst
                    ? "Pre-burst"
                    : "Executed";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var shortReason = shouldDeployForRaidwide
                    ? $"Pre-raidwide Asylum near {context.Debug.AsylumTarget}"
                    : shouldDeployForBurst
                        ? $"Pre-burst Asylum near {context.Debug.AsylumTarget}"
                        : $"Asylum placed near {context.Debug.AsylumTarget}";

                var factors = new[]
                {
                    $"Placement: Near {context.Debug.AsylumTarget}",
                    "100 potency HoT every 3s for 24s",
                    "10% healing increase inside",
                    shouldDeployForRaidwide ? $"Raidwide predicted in {raidwideInfo?.secondsUntil:F1}s via {raidwideSource}" :
                    shouldDeployForBurst ? "DPS burst window approaching" :
                    "General healing support",
                };

                var alternatives = new[]
                {
                    "Wait for better positioning",
                    "Save for raidwide phase",
                    "Use direct heals instead",
                };

                var tip = shouldDeployForRaidwide
                    ? "Deploying Asylum 5-8 seconds before raidwides lets the HoT tick before damage, and the healing boost helps recovery!"
                    : "Place Asylum where the party will stack - the healing boost affects all heals inside!";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = WHMActions.Asylum.ActionId,
                    ActionName = "Asylum",
                    Category = "Healing",
                    TargetName = context.Debug.AsylumTarget,
                    ShortReason = shortReason,
                    DetailedReason = $"Asylum placed near {context.Debug.AsylumTarget}. This ground-targeted HoT heals for 100 potency every 3 seconds and increases healing received by 10%. {(shouldDeployForRaidwide ? $"Deployed proactively before predicted raidwide ({raidwideSource})." : shouldDeployForBurst ? "Deployed before DPS burst window for sustained healing." : "")}",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = WhmConcepts.ProactiveHealing,
                    Priority = shouldDeployForRaidwide ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        context.Debug.AsylumState = "Execution failed";
        return false;
    }

    private bool TryExecuteAssize(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.Assize, config,
            c => c.Healing.EnableAssize))
            return false;

        return ActionExecutor.ExecuteOgcd(context, WHMActions.Assize, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp);
    }

    private bool TryExecuteLucidDreaming(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;
        var mpPercent = context.MpForecastService.MpPercent;

        if (!config.Buffs.EnableLucidDreaming)
            return false;

        if (player.Level < RoleActions.LucidDreaming.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(RoleActions.LucidDreaming.ActionId))
            return false;

        // Already have Lucid Dreaming active
        if (context.MpForecastService.IsLucidDreamingActive)
            return false;

        var shouldUseLucid = false;
        var reason = string.Empty;

        // Priority 1: Predictive Lucid Dreaming (check MP exhaustion forecast)
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

        // Priority 2: Threshold-based logic (fallback)
        if (!shouldUseLucid)
        {
            // Determine threshold based on context
            var threshold = 0.70f;

            // Lower threshold in conservation mode - use Lucid earlier
            if (context.MpForecastService.IsInConservationMode)
                threshold = 0.80f;

            // Raise prep mode - use Lucid even earlier to build MP for raise
            if (config.Buffs.EnableRaisePrepMode && ShouldEnterRaisePrepMode(context))
                threshold = 0.90f;

            if (mpPercent < threshold)
            {
                shouldUseLucid = true;
                reason = $"MP below {threshold:P0} threshold";
            }
        }

        if (!shouldUseLucid)
            return false;

        if (ActionExecutor.ExecuteOgcd(context, RoleActions.LucidDreaming, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentMp, reason))
        {
            context.Debug.LucidState = reason;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if we should enter raise preparation mode.
    /// Active when there's a dead party member and MP is low.
    /// </summary>
    private bool ShouldEnterRaisePrepMode(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        // Check if there's someone to raise
        var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
        if (deadMember is null)
            return false;

        // Check if MP is below raise prep threshold
        var mpPercent = context.MpForecastService.MpPercent;
        return mpPercent < config.Buffs.RaisePrepMpThreshold;
    }

    private bool TryExecuteSurecast(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.RoleActions.EnableSurecast)
        {
            context.Debug.SurecastState = "Disabled";
            return false;
        }

        // Manual mode (0) - never auto-execute
        if (config.RoleActions.SurecastMode == 0)
        {
            context.Debug.SurecastState = "Manual mode";
            return false;
        }

        if (player.Level < RoleActions.Surecast.MinLevel)
        {
            context.Debug.SurecastState = $"Level {player.Level} < {RoleActions.Surecast.MinLevel}";
            return false;
        }

        if (StatusHelper.HasStatus(player, StatusHelper.StatusIds.Surecast))
        {
            context.Debug.SurecastState = "Already active";
            return false;
        }

        if (!context.ActionService.IsActionReady(RoleActions.Surecast.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(RoleActions.Surecast.ActionId);
            context.Debug.SurecastState = $"CD: {cd:F1}s";
            return false;
        }

        // Mode 1: Use on cooldown in combat
        if (config.RoleActions.SurecastMode == 1)
        {
            if (ActionExecutor.ExecuteOgcd(context, RoleActions.Surecast, player.GameObjectId,
                player.Name?.TextValue ?? "Unknown", player.CurrentHp))
            {
                context.Debug.SurecastState = "Executed";
                return true;
            }
        }

        context.Debug.SurecastState = "Ready";
        return false;
    }

    /// <summary>
    /// Checks if an expensive spell (800+ MP) is likely to be cast soon.
    /// Used to decide when to use Thin Air for MP conservation.
    /// </summary>
    private bool WillCastExpensiveSpell(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        // Check for incoming raise
        if (config.Resurrection.EnableRaise)
        {
            var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
            if (deadMember is not null)
                return true;
        }

        // Check for incoming AoE heal (Medica, Medica II, Cure III, etc. are 800-1000 MP)
        if (config.EnableHealing)
        {
            var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
            var healAmount = WHMActions.Medica.EstimateHealAmount(mind, det, wd, player.Level);
            var (injuredCount, _, _, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, healAmount);
            if (injuredCount >= config.Healing.AoEHealMinTargets)
                return true;
        }

        // Check for incoming single-target heal on low HP target
        if (config.EnableHealing && player.Level >= WHMActions.CureII.MinLevel)
        {
            var target = context.PartyHelper.FindLowestHpPartyMember(player);
            if (target is not null)
            {
                var hpPercent = context.PartyHelper.GetHpPercent(target);
                // Use GCD emergency threshold as indicator of upcoming expensive heal
                if (hpPercent < config.Healing.GcdEmergencyThreshold)
                    return true;
            }
        }

        return false;
    }

    private void TryExecuteAetherialShift(IApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableAetherialShift)
            return;

        if (!ActionValidator.IsAvailable(player, context.ActionService, WHMActions.AetherialShift))
            return;

        const float dashDistance = 15f;
        var spellRange = WHMActions.Stone.Range;
        var target = context.TargetingService.FindEnemy(
            config.Targeting.EnemyStrategy,
            spellRange + dashDistance,
            player);

        if (target is null)
            return;

        var distance = Vector3.Distance(player.Position, target.Position);

        if (distance <= spellRange)
            return;

        if (distance < 0.01f)
            return;

        var toTarget = Vector3.Normalize(target.Position - player.Position);
        var playerForward = new Vector3(
            MathF.Sin(player.Rotation),
            0,
            MathF.Cos(player.Rotation));
        var dot = Vector3.Dot(playerForward, new Vector3(toTarget.X, 0, toTarget.Z));

        if (dot < 0.7f)
            return;

        ActionExecutor.ExecuteOgcd(context, WHMActions.AetherialShift, player.GameObjectId,
            target.Name?.TextValue ?? "Unknown", target.CurrentHp,
            "Aetherial Shift (gap close)");
    }
}
