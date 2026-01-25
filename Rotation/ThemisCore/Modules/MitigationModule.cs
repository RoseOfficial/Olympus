using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Services.Party;
using Olympus.Timeline;

namespace Olympus.Rotation.ThemisCore.Modules;

/// <summary>
/// Handles the Paladin defensive rotation.
/// Manages personal mitigation, party mitigation, and invulnerability.
/// </summary>
public sealed class MitigationModule : IThemisModule
{
    public int Priority => 10; // High priority for defensives
    public string Name => "Mitigation";

    public bool TryExecute(IThemisContext context, bool isMoving)
    {
        if (!context.Configuration.Tank.EnableMitigation)
        {
            context.Debug.MitigationState = "Disabled";
            return false;
        }

        if (!context.InCombat)
        {
            context.Debug.MitigationState = "Not in combat";
            return false;
        }

        // Only use defensives during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        // Priority 0: Interrupt enemy casts (highest priority)
        if (TryInterrupt(context))
            return true;

        var player = context.Player;
        var level = player.Level;
        var hpPercent = (float)player.CurrentHp / player.MaxHp;
        var damageRate = context.DamageIntakeService.GetDamageRate(player.EntityId);

        // Update cooldown service
        context.TankCooldownService.Update(hpPercent, damageRate);

        // Timeline-aware proactive mitigation (if timeline data available)
        if (TryTimelineAwareMitigation(context, hpPercent))
            return true;

        // Priority 1: Emergency invulnerability (Hallowed Ground)
        if (TryHallowedGround(context, hpPercent))
            return true;

        // Priority 2: Major cooldown (Sentinel/Guardian)
        if (TryMajorCooldown(context, hpPercent, damageRate))
            return true;

        // Priority 3: Rampart (role action)
        if (TryRampart(context, hpPercent, damageRate))
            return true;

        // Priority 4: Short cooldown (Sheltron/Holy Sheltron)
        if (TrySheltron(context, hpPercent))
            return true;

        // Priority 5: Bulwark (if available and not overlapping)
        if (TryBulwark(context, hpPercent, damageRate))
            return true;

        // Priority 6: Arm's Length for knockback immunity
        if (TryArmsLength(context))
            return true;

        // Priority 7: Party mitigation (Reprisal)
        if (TryReprisal(context))
            return true;

        // Priority 8: Divine Veil for party protection
        if (TryDivineVeil(context))
            return true;

        // Priority 9: Cover for protecting low HP ally
        if (TryCover(context, hpPercent))
            return true;

        return false;
    }

    public void UpdateDebugState(IThemisContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Interrupt

    /// <summary>
    /// Attempts to interrupt an enemy cast using Interject or Low Blow.
    /// Coordinates with other Olympus instances to prevent duplicate interrupts.
    /// </summary>
    private bool TryInterrupt(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Need at least Interject (Lv.18)
        if (level < 18)
            return false;

        // Check current target for interruptible cast
        var target = context.CurrentTarget;
        if (target == null)
            return false;

        // Check if target is casting something interruptible
        if (!target.IsCasting)
            return false;

        // Check the cast interruptible flag (game indicates this)
        if (!target.IsCastInterruptible)
            return false;

        var targetId = target.EntityId;
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;

        // Check IPC reservation
        if (coordConfig.EnableInterruptCoordination &&
            partyCoord?.IsInterruptTargetReservedByOther(targetId) == true)
        {
            context.Debug.MitigationState = "Interrupt reserved by other";
            return false;
        }

        // Calculate remaining cast time in milliseconds
        var remainingCastTime = (target.TotalCastTime - target.CurrentCastTime) * 1000f;
        var castTimeMs = (int)remainingCastTime;

        // Try Interject first (dedicated interrupt)
        if (context.ActionService.IsActionReady(PLDActions.Interject.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, PLDActions.Interject.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(PLDActions.Interject, target.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.Interject.Name;
                context.Debug.MitigationState = "Interrupted cast";
                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        // Try Low Blow as backup (stun can interrupt some casts)
        if (level >= 12 && context.ActionService.IsActionReady(PLDActions.LowBlow.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, PLDActions.LowBlow.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(PLDActions.LowBlow, target.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.LowBlow.Name;
                context.Debug.MitigationState = "Stunned (interrupt)";
                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        return false;
    }

    #endregion

    #region Timeline-Aware Mitigation

    /// <summary>
    /// Proactively uses defensive cooldowns when timeline predicts incoming tankbuster.
    /// Pre-stacks mitigation 1.5-4 seconds before predicted damage.
    /// </summary>
    private bool TryTimelineAwareMitigation(IThemisContext context, float hpPercent)
    {
        var nextTB = context.TimelineService?.NextTankBuster;
        if (nextTB?.IsSoon != true || !nextTB.Value.IsHighConfidence)
            return false;

        var secondsUntil = nextTB.Value.SecondsUntil;

        // Pre-stack window: 1.5-4 seconds before tankbuster
        if (secondsUntil < 1.5f || secondsUntil > 4.0f)
            return false;

        var player = context.Player;
        var level = player.Level;
        var reason = $"TB in {secondsUntil:F1}s";

        // Don't stack with Hallowed Ground
        if (context.HasHallowedGround)
            return false;

        // Priority 1: Holy Sheltron (short CD, gauge-based)
        if (level >= PLDActions.Sheltron.MinLevel &&
            context.OathGauge >= 50 &&
            !context.StatusHelper.HasSheltron(player))
        {
            var sheltronAction = PLDActions.GetSheltronAction(level);
            if (context.ActionService.IsActionReady(sheltronAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(sheltronAction, player.GameObjectId))
                {
                    context.Debug.PlannedAction = sheltronAction.Name;
                    context.Debug.MitigationState = $"Proactive Sheltron ({reason})";
                    return true;
                }
            }
        }

        // Priority 2: Rampart (if no active mitigation)
        if (level >= PLDActions.Rampart.MinLevel &&
            !context.HasActiveMitigation &&
            !context.StatusHelper.HasRampart(player))
        {
            if (context.ActionService.IsActionReady(PLDActions.Rampart.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(PLDActions.Rampart, player.GameObjectId))
                {
                    context.Debug.PlannedAction = PLDActions.Rampart.Name;
                    context.Debug.MitigationState = $"Proactive Rampart ({reason})";
                    return true;
                }
            }
        }

        // Priority 3: Sentinel/Guardian (major CD for big hits)
        if (level >= PLDActions.Sentinel.MinLevel &&
            !context.StatusHelper.HasSentinel(player))
        {
            var sentinelAction = PLDActions.GetSentinelAction(level);
            if (context.ActionService.IsActionReady(sentinelAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(sentinelAction, player.GameObjectId))
                {
                    context.Debug.PlannedAction = sentinelAction.Name;
                    context.Debug.MitigationState = $"Proactive Sentinel ({reason})";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Emergency Mitigation

    private bool TryHallowedGround(IThemisContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.HallowedGround.MinLevel)
            return false;

        // Only use Hallowed Ground in emergencies
        if (hpPercent > 0.15f)
            return false;

        // Don't use if already under Hallowed Ground
        if (context.HasHallowedGround)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.HallowedGround.ActionId))
            return false;

        // Check if another tank recently used an invuln (coordination)
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableInvulnerabilityCoordination &&
            partyCoord?.WasInvulnerabilityUsedRecently(tankConfig.InvulnerabilityStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Hallowed delayed (remote invuln)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(PLDActions.HallowedGround, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.HallowedGround.Name;
            context.Debug.MitigationState = $"Emergency invuln ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(PLDActions.HallowedGround.ActionId, 420_000);

            // Training: Record emergency invuln decision
            TankTrainingHelper.RecordInvulnDecision(
                context.TrainingService,
                PLDActions.HallowedGround.ActionId,
                PLDActions.HallowedGround.Name,
                hpPercent,
                $"Emergency at {hpPercent:P0} HP",
                "Hallowed Ground provides 10 seconds of complete invulnerability. Used at critical HP to survive otherwise lethal damage.",
                new[] { $"HP at {hpPercent:P0} (below 15% threshold)", "No other tank invuln used recently", "Would die without invuln" },
                new[] { "Sentinel (40% reduction, but may not be enough)", "Wait for healer (risky at this HP)" },
                "Use Hallowed Ground when HP drops critically low. Unlike other tank invulns, it has no drawback - just prevents all damage for 10 seconds.",
                "pld_hallowed_ground");

            // Mastery: Record successful emergency response
            context.TrainingService?.RecordConceptApplication("pld_hallowed_ground", true, "Used at critical HP");

            return true;
        }

        return false;
    }

    #endregion

    #region Major Cooldowns

    private bool TryMajorCooldown(IThemisContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine which Sentinel variant to use
        var sentinelAction = PLDActions.GetSentinelAction(level);

        if (level < PLDActions.Sentinel.MinLevel)
            return false;

        // Check if we should use major cooldown
        if (!context.TankCooldownService.ShouldUseMajorCooldown(hpPercent, damageRate))
            return false;

        // Don't stack with Hallowed Ground
        if (context.HasHallowedGround)
            return false;

        // Don't stack with existing Sentinel/Guardian
        if (context.StatusHelper.HasSentinel(player))
            return false;

        // Check if another tank recently used a personal defensive (coordination)
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Sentinel delayed (remote tank mit)";
            return false;
        }

        if (!context.ActionService.IsActionReady(sentinelAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(sentinelAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = sentinelAction.Name;
            context.Debug.MitigationState = $"Major CD ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(sentinelAction.ActionId, 120_000);

            // Training: Record major cooldown decision
            TankTrainingHelper.RecordMitigationDecision(
                context.TrainingService,
                sentinelAction.ActionId,
                sentinelAction.Name,
                null,
                hpPercent,
                $"Major cooldown at {hpPercent:P0} HP",
                $"{sentinelAction.Name} reduces damage taken by 30% for 15 seconds. Your strongest regular mitigation - use for tankbusters and heavy damage phases.",
                new[] { $"HP at {hpPercent:P0}", $"Damage rate: {damageRate:F0} DPS", "No Hallowed Ground active", "No existing Sentinel buff" },
                new[] { "Rampart (20% reduction, shorter cooldown)", "Sheltron (gauge-based, shorter duration)", "Wait for healer" },
                "Sentinel is your go-to for predictable big hits. Its 120s cooldown means you can use it almost every tankbuster in most fights.",
                "pld_sentinel");

            // Mastery: Record successful mitigation timing
            context.TrainingService?.RecordConceptApplication("pld_sentinel", true, $"Used at {hpPercent:P0} HP");

            return true;
        }

        return false;
    }

    private bool TryRampart(IThemisContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Rampart.MinLevel)
            return false;

        // Check if we should use mitigation
        if (!context.TankCooldownService.ShouldUseMitigation(hpPercent, damageRate, context.HasActiveMitigation))
            return false;

        // Don't stack with Hallowed Ground
        if (context.HasHallowedGround)
            return false;

        // Don't use if already have Rampart
        if (context.StatusHelper.HasRampart(player))
            return false;

        // Use Rampart on cooldown if configured, or only when needed
        if (!context.Configuration.Tank.UseRampartOnCooldown && context.HasActiveMitigation)
            return false;

        // Check if another tank recently used a personal defensive (coordination)
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Rampart delayed (remote tank mit)";
            return false;
        }

        if (!context.ActionService.IsActionReady(PLDActions.Rampart.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.Rampart, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.Rampart.Name;
            context.Debug.MitigationState = $"Rampart ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(PLDActions.Rampart.ActionId, 90_000);
            return true;
        }

        return false;
    }

    #endregion

    #region Short Cooldowns

    private bool TrySheltron(IThemisContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Sheltron.MinLevel)
            return false;

        // Check if we should use short cooldown
        if (!context.TankCooldownService.ShouldUseShortCooldown(
            hpPercent,
            context.OathGauge,
            context.Configuration.Tank.SheltronMinGauge))
            return false;

        // Don't use if already have Sheltron active
        if (context.StatusHelper.HasSheltron(player))
            return false;

        // Don't stack with Hallowed Ground (waste of gauge)
        if (context.HasHallowedGround)
            return false;

        // Get the appropriate Sheltron action
        var sheltronAction = PLDActions.GetSheltronAction(level);

        if (!context.ActionService.IsActionReady(sheltronAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(sheltronAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = sheltronAction.Name;
            context.Debug.MitigationState = $"Sheltron ({context.OathGauge} gauge)";

            // Training: Record gauge-based mitigation
            TankTrainingHelper.RecordResourceDecision(
                context.TrainingService,
                sheltronAction.ActionId,
                sheltronAction.Name,
                context.OathGauge,
                $"Spent {50} Oath Gauge at {hpPercent:P0} HP",
                $"{sheltronAction.Name} costs 50 Oath Gauge and provides a short but powerful defensive buff. Use liberally since gauge regenerates passively.",
                new[] { $"Oath Gauge: {context.OathGauge}", $"HP at {hpPercent:P0}", "No Sheltron already active", "Not under Hallowed Ground" },
                new[] { "Save gauge (risk taking more damage)", "Wait for bigger hit (may overcap gauge)" },
                "Oath Gauge regenerates from auto-attacks. Don't sit at 100 gauge - spend Sheltron frequently to avoid waste.",
                "pld_sheltron");

            // Mastery: Record successful gauge usage
            context.TrainingService?.RecordConceptApplication("pld_sheltron", true, "Used Oath Gauge effectively");

            return true;
        }

        return false;
    }

    private bool TryBulwark(IThemisContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Bulwark.MinLevel)
            return false;

        // Bulwark is good for sustained damage, not tank busters
        // Use when taking moderate consistent damage
        if (damageRate < 300f || hpPercent > 0.80f)
            return false;

        // Don't stack with Hallowed Ground
        if (context.HasHallowedGround)
            return false;

        // Check if Bulwark is active
        if (context.StatusHelper.HasStatus(player, PLDActions.StatusIds.Bulwark))
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.Bulwark.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.Bulwark, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.Bulwark.Name;
            context.Debug.MitigationState = "Bulwark (sustained damage)";
            return true;
        }

        return false;
    }

    private bool TryArmsLength(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.ArmsLength.MinLevel)
            return false;

        // Arm's Length is primarily used for knockback immunity
        // For now, we'll skip automatic usage - requires mechanic detection
        // Could be enhanced with boss mechanic detection in the future

        return false;
    }

    #endregion

    #region Party Mitigation

    private bool TryReprisal(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Reprisal.MinLevel)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Reprisal skipped (remote mit)";
            return false;
        }

        // Use Reprisal as a party mitigation tool
        // Best used before raidwides or during pulls with multiple enemies

        // Check if there are multiple enemies nearby
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        if (enemyCount < 2)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.Reprisal.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.Reprisal, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.Reprisal.Name;
            context.Debug.MitigationState = $"Reprisal ({enemyCount} enemies)";
            partyCoord?.OnCooldownUsed(PLDActions.Reprisal.ActionId, 60_000);
            return true;
        }

        return false;
    }

    private bool TryDivineVeil(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.DivineVeil.MinLevel)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Divine Veil skipped (remote mit)";
            return false;
        }

        // Divine Veil needs to be triggered by a heal
        // Use proactively when party health is low
        var (avgHp, lowestHp, injuredCount) = context.PartyHealthMetrics;

        // Use when multiple party members are injured
        if (injuredCount < 3 || avgHp > 0.85f)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.DivineVeil.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.DivineVeil, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.DivineVeil.Name;
            context.Debug.MitigationState = $"Divine Veil ({injuredCount} injured)";
            partyCoord?.OnCooldownUsed(PLDActions.DivineVeil.ActionId, 90_000);

            // Training: Record party mitigation decision
            TankTrainingHelper.RecordPartyMitigationDecision(
                context.TrainingService,
                PLDActions.DivineVeil.ActionId,
                PLDActions.DivineVeil.Name,
                $"Protecting {injuredCount} injured party members",
                "Divine Veil applies a barrier to all nearby party members when you receive a heal. Excellent for raidwide damage.",
                new[] { $"{injuredCount} party members injured", $"Average party HP: {avgHp:P0}", "No other party mitigation active", "Ready to be triggered by healer" },
                new[] { "Reprisal (reduces enemy damage instead)", "Wait for healer cooldowns" },
                "Divine Veil needs to be triggered by receiving a heal. Communicate with healers or use Clemency to self-trigger if needed.",
                "pld_divine_veil");

            // Mastery: Record successful party protection
            context.TrainingService?.RecordConceptApplication("pld_divine_veil", true, "Deployed party shield");

            return true;
        }

        return false;
    }

    private bool TryCover(IThemisContext context, float myHpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Cover.MinLevel)
            return false;

        // Don't cover if we're low HP ourselves
        if (myHpPercent < 0.60f)
            return false;

        // Find a party member who needs covering
        var coverTarget = context.PartyHelper.FindCoverTarget(player, 0.40f);
        if (coverTarget == null)
            return false;

        // Check distance to target (Cover range is 10y)
        var dx = player.Position.X - coverTarget.Position.X;
        var dy = player.Position.Y - coverTarget.Position.Y;
        var dz = player.Position.Z - coverTarget.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (distance > 10f)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.Cover.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.Cover, coverTarget.GameObjectId))
        {
            var targetHp = context.PartyHelper.GetHpPercent(coverTarget);
            context.Debug.PlannedAction = PLDActions.Cover.Name;
            context.Debug.MitigationState = $"Cover ({targetHp:P0} HP ally)";
            return true;
        }

        return false;
    }

    #endregion
}
