using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AresCore.Context;
using Olympus.Services.Party;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.AresCore.Modules;

/// <summary>
/// Handles the Warrior defensive rotation.
/// Manages personal mitigation, party mitigation, and invulnerability.
/// </summary>
public sealed class MitigationModule : IAresModule
{
    public int Priority => 10; // High priority for defensives
    public string Name => "Mitigation";

    public bool TryExecute(IAresContext context, bool isMoving)
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

        // Priority 1: Emergency invulnerability (Holmgang)
        if (TryHolmgang(context, hpPercent))
            return true;

        // Priority 2: Major cooldown (Vengeance/Damnation)
        if (TryMajorCooldown(context, hpPercent, damageRate))
            return true;

        // Priority 3: Rampart (role action)
        if (TryRampart(context, hpPercent, damageRate))
            return true;

        // Priority 4: Short cooldown (Bloodwhetting/Raw Intuition)
        if (TryBloodwhetting(context, hpPercent))
            return true;

        // Priority 5: Thrill of Battle (HP boost + heal)
        if (TryThrillOfBattle(context, hpPercent))
            return true;

        // Priority 6: Equilibrium (self-heal)
        if (TryEquilibrium(context, hpPercent))
            return true;

        // Priority 7: Arm's Length for knockback immunity
        if (TryArmsLength(context))
            return true;

        // Priority 8: Party mitigation (Reprisal)
        if (TryReprisal(context))
            return true;

        // Priority 9: Shake It Off for party protection
        if (TryShakeItOff(context))
            return true;

        // Priority 10: Nascent Flash for protecting low HP ally
        if (TryNascentFlash(context, hpPercent))
            return true;

        return false;
    }

    public void UpdateDebugState(IAresContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Interrupt

    /// <summary>
    /// Attempts to interrupt an enemy cast using Interject or Low Blow.
    /// Coordinates with other Olympus instances to prevent duplicate interrupts.
    /// </summary>
    private bool TryInterrupt(IAresContext context)
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
        if (context.ActionService.IsActionReady(WARActions.Interject.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, WARActions.Interject.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(WARActions.Interject, target.GameObjectId))
            {
                context.Debug.PlannedAction = WARActions.Interject.Name;
                context.Debug.MitigationState = "Interrupted cast";
                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        // Try Low Blow as backup (stun can interrupt some casts)
        if (level >= 12 && context.ActionService.IsActionReady(WARActions.LowBlow.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, WARActions.LowBlow.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(WARActions.LowBlow, target.GameObjectId))
            {
                context.Debug.PlannedAction = WARActions.LowBlow.Name;
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
    private bool TryTimelineAwareMitigation(IAresContext context, float hpPercent)
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

        // Don't stack with Holmgang
        if (context.HasHolmgang)
            return false;

        // Priority 1: Bloodwhetting/Raw Intuition (short CD)
        if (level >= WARActions.RawIntuition.MinLevel &&
            !context.HasBloodwhetting)
        {
            var bloodwhettingAction = WARActions.GetBloodwhettingAction(level);
            if (context.ActionService.IsActionReady(bloodwhettingAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(bloodwhettingAction, player.GameObjectId))
                {
                    context.Debug.PlannedAction = bloodwhettingAction.Name;
                    context.Debug.MitigationState = $"Proactive Bloodwhetting ({reason})";
                    return true;
                }
            }
        }

        // Priority 2: Rampart (if no active mitigation)
        if (level >= WARActions.Rampart.MinLevel &&
            !context.HasActiveMitigation &&
            !context.StatusHelper.HasRampart(player))
        {
            if (context.ActionService.IsActionReady(WARActions.Rampart.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(WARActions.Rampart, player.GameObjectId))
                {
                    context.Debug.PlannedAction = WARActions.Rampart.Name;
                    context.Debug.MitigationState = $"Proactive Rampart ({reason})";
                    return true;
                }
            }
        }

        // Priority 3: Vengeance/Damnation (major CD for big hits)
        if (level >= WARActions.Vengeance.MinLevel &&
            !context.HasVengeance)
        {
            var vengeanceAction = WARActions.GetVengeanceAction(level);
            if (context.ActionService.IsActionReady(vengeanceAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(vengeanceAction, player.GameObjectId))
                {
                    context.Debug.PlannedAction = vengeanceAction.Name;
                    context.Debug.MitigationState = $"Proactive Vengeance ({reason})";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Emergency Mitigation

    private bool TryHolmgang(IAresContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Holmgang.MinLevel)
            return false;

        // Only use Holmgang in emergencies
        if (hpPercent > 0.15f)
            return false;

        // Don't use if already under Holmgang
        if (context.HasHolmgang)
            return false;

        // Find target for Holmgang (requires a target)
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            6f, // Holmgang range
            player);

        if (target == null)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Holmgang.ActionId))
            return false;

        // Check if another tank recently used an invuln (coordination)
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableInvulnerabilityCoordination &&
            partyCoord?.WasInvulnerabilityUsedRecently(tankConfig.InvulnerabilityStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Holmgang delayed (remote invuln)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(WARActions.Holmgang, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Holmgang.Name;
            context.Debug.MitigationState = $"Emergency invuln ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(WARActions.Holmgang.ActionId, 240_000);

            // Training: Record emergency invuln
            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.Holmgang.ActionId, WARActions.Holmgang.Name)
                .AsInvuln(hpPercent)
                .Reason(
                    "Emergency Holmgang - HP critically low. Holmgang prevents death for 10 seconds.",
                    "Holmgang binds you and the target, preventing your HP from dropping below 1. 240s cooldown - use only in emergencies.")
                .Factors($"HP critically low ({hpPercent:P0})", "No other mitigation available", $"Target: {target.Name?.TextValue}")
                .Alternatives("Trust healers (risky at this HP)", "Use other cooldowns first (may be too slow)")
                .Tip("Holmgang is your emergency button. At 15% HP or below, use it immediately. Healers can then safely heal you during the 10-second window.")
                .Concept("war_holmgang")
                .Record();

            context.TrainingService?.RecordConceptApplication("war_holmgang", true, "Emergency invuln activation");

            return true;
        }

        return false;
    }

    #endregion

    #region Major Cooldowns

    private bool TryMajorCooldown(IAresContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine which Vengeance variant to use
        var vengeanceAction = WARActions.GetVengeanceAction(level);

        if (level < WARActions.Vengeance.MinLevel)
            return false;

        // Check if we should use major cooldown
        if (!context.TankCooldownService.ShouldUseMajorCooldown(hpPercent, damageRate))
            return false;

        // Don't stack with Holmgang
        if (context.HasHolmgang)
            return false;

        // Don't stack with existing Vengeance/Damnation
        if (context.HasVengeance)
            return false;

        // Check if another tank recently used a personal defensive (coordination)
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Vengeance delayed (remote tank mit)";
            return false;
        }

        if (!context.ActionService.IsActionReady(vengeanceAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(vengeanceAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = vengeanceAction.Name;
            context.Debug.MitigationState = $"Major CD ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(vengeanceAction.ActionId, 120_000);

            // Training: Record major cooldown
            TrainingHelper.Decision(context.TrainingService)
                .Action(vengeanceAction.ActionId, vengeanceAction.Name)
                .AsMitigation(hpPercent)
                .Reason(
                    $"{vengeanceAction.Name} activated for heavy damage mitigation. 30% damage reduction + counter-attack damage.",
                    "Vengeance/Damnation is your strongest defensive cooldown. 30% mitigation for 15 seconds with 120s recast. Also deals damage when hit.")
                .Factors($"HP at {hpPercent:P0}", $"Taking heavy damage (rate: {damageRate:F1}/s)", "No major mitigation active")
                .Alternatives("Use Rampart first (shorter cooldown)", "Wait for tankbuster (may die first)")
                .Tip("Use Vengeance for sustained heavy damage or big tankbusters. The counter-attack damage adds value in dungeon pulls.")
                .Concept("war_vengeance")
                .Record();

            context.TrainingService?.RecordConceptApplication("war_vengeance", true, "Major cooldown usage");

            return true;
        }

        return false;
    }

    private bool TryRampart(IAresContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Rampart.MinLevel)
            return false;

        // Check if we should use mitigation
        if (!context.TankCooldownService.ShouldUseMitigation(hpPercent, damageRate, context.HasActiveMitigation))
            return false;

        // Don't stack with Holmgang
        if (context.HasHolmgang)
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

        if (!context.ActionService.IsActionReady(WARActions.Rampart.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Rampart, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Rampart.Name;
            context.Debug.MitigationState = $"Rampart ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(WARActions.Rampart.ActionId, 90_000);
            return true;
        }

        return false;
    }

    #endregion

    #region Short Cooldowns

    private bool TryBloodwhetting(IAresContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.RawIntuition.MinLevel)
            return false;

        // Check if we should use short cooldown
        // Bloodwhetting is very strong and can be used more aggressively
        if (hpPercent > context.Configuration.Tank.MitigationThreshold)
            return false;

        // Don't use if already have Bloodwhetting/Raw Intuition active
        if (context.HasBloodwhetting)
            return false;

        // Don't stack with Holmgang (waste)
        if (context.HasHolmgang)
            return false;

        // Get the appropriate action
        var bloodwhettingAction = WARActions.GetBloodwhettingAction(level);

        // Check if another tank recently used a personal defensive (coordination)
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Bloodwhetting delayed (remote tank mit)";
            return false;
        }

        if (!context.ActionService.IsActionReady(bloodwhettingAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(bloodwhettingAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = bloodwhettingAction.Name;
            context.Debug.MitigationState = $"Bloodwhetting ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(bloodwhettingAction.ActionId, 25_000);

            // Training: Record Bloodwhetting
            TrainingHelper.Decision(context.TrainingService)
                .Action(bloodwhettingAction.ActionId, bloodwhettingAction.Name)
                .AsMitigation(hpPercent)
                .Reason(
                    "Bloodwhetting activated for damage reduction and self-healing. Warriors sustain through their attacks.",
                    "Bloodwhetting provides 10% mitigation, a barrier, AND heals you for 400 potency per weaponskill. 25s recast makes it your bread-and-butter defensive.")
                .Factors($"HP at {hpPercent:P0}", "Short cooldown available", "Mitigation + healing combo")
                .Alternatives("Save for bigger hit (25s CD means frequent use)", "Rely on healers (unnecessary strain)")
                .Tip("Use Bloodwhetting frequently - it's your signature sustain tool. The healing scales with damage dealt, so use it during AoE for massive recovery.")
                .Concept("war_bloodwhetting")
                .Record();

            context.TrainingService?.RecordConceptApplication("war_bloodwhetting", true, "Short cooldown with healing");

            return true;
        }

        return false;
    }

    private bool TryThrillOfBattle(IAresContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.ThrillOfBattle.MinLevel)
            return false;

        // Thrill of Battle is good for moderate damage or preemptive HP boost
        if (hpPercent > 0.70f)
            return false;

        // Don't stack with Holmgang
        if (context.HasHolmgang)
            return false;

        // Check if Thrill is active
        if (context.StatusHelper.HasThrillOfBattle(player))
            return false;

        if (!context.ActionService.IsActionReady(WARActions.ThrillOfBattle.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.ThrillOfBattle, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.ThrillOfBattle.Name;
            context.Debug.MitigationState = $"Thrill ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryEquilibrium(IAresContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Equilibrium.MinLevel)
            return false;

        // Equilibrium is a strong self-heal, use when HP is low
        if (hpPercent > 0.50f)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Equilibrium.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Equilibrium, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Equilibrium.Name;
            context.Debug.MitigationState = $"Equilibrium ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryArmsLength(IAresContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.ArmsLength.MinLevel)
            return false;

        // Arm's Length is primarily used for knockback immunity
        // For now, we'll skip automatic usage - requires mechanic detection
        // Could be enhanced with boss mechanic detection in the future

        return false;
    }

    #endregion

    #region Party Mitigation

    private bool TryReprisal(IAresContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Reprisal.MinLevel)
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

        if (!context.ActionService.IsActionReady(WARActions.Reprisal.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Reprisal, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Reprisal.Name;
            context.Debug.MitigationState = $"Reprisal ({enemyCount} enemies)";
            partyCoord?.OnCooldownUsed(WARActions.Reprisal.ActionId, 60_000);
            return true;
        }

        return false;
    }

    private bool TryShakeItOff(IAresContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.ShakeItOff.MinLevel)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Shake It Off skipped (remote mit)";
            return false;
        }

        // Shake It Off is a party shield and self-cleanse
        // Use proactively when party health is low
        var (avgHp, lowestHp, injuredCount) = context.PartyHealthMetrics;

        // Use when multiple party members are injured
        if (injuredCount < 3 || avgHp > 0.85f)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.ShakeItOff.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.ShakeItOff, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.ShakeItOff.Name;
            context.Debug.MitigationState = $"Shake It Off ({injuredCount} injured)";
            partyCoord?.OnCooldownUsed(WARActions.ShakeItOff.ActionId, 90_000);

            // Training: Record party mitigation
            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.ShakeItOff.ActionId, WARActions.ShakeItOff.Name)
                .AsPartyMit()
                .Reason(
                    $"Shake It Off provides party-wide shield and cleanses your own debuffs. {injuredCount} party members injured.",
                    "Shake It Off gives the whole party a 15% max HP barrier (more if you have personal buffs to consume). Also cleanses debuffs from you.")
                .Factors($"{injuredCount} party members injured", $"Party average HP: {avgHp:P0}", "Party needs protection")
                .Alternatives("Save for raidwide (may lose party members)", "Let healers handle it (adds healer stress)")
                .Tip("Use Shake It Off before raidwides for maximum value. The barrier lasts 30 seconds, so you can pre-shield.")
                .Concept("war_shake_it_off")
                .Record();

            context.TrainingService?.RecordConceptApplication("war_shake_it_off", true, "Party shield deployment");

            return true;
        }

        return false;
    }

    private bool TryNascentFlash(IAresContext context, float myHpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.NascentFlash.MinLevel)
            return false;

        // Don't use Nascent Flash if we're low HP ourselves
        if (myHpPercent < 0.60f)
            return false;

        // Find a party member who needs protection
        var flashTarget = context.PartyHelper.FindNascentFlashTarget(player, 0.60f);
        if (flashTarget == null)
            return false;

        // Check distance to target (Nascent Flash range is 30y)
        var dx = player.Position.X - flashTarget.Position.X;
        var dy = player.Position.Y - flashTarget.Position.Y;
        var dz = player.Position.Z - flashTarget.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (distance > 30f)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.NascentFlash.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.NascentFlash, flashTarget.GameObjectId))
        {
            var targetHp = context.PartyHelper.GetHpPercent(flashTarget);
            context.Debug.PlannedAction = WARActions.NascentFlash.Name;
            context.Debug.MitigationState = $"Nascent Flash ({targetHp:P0} HP ally)";
            return true;
        }

        return false;
    }

    #endregion
}
