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
        if (context.ActionService.IsActionReady(RoleActions.Interject.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.Interject.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(RoleActions.Interject, target.GameObjectId))
            {
                context.Debug.PlannedAction = RoleActions.Interject.Name;
                context.Debug.MitigationState = "Interrupted cast";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Interject.ActionId, RoleActions.Interject.Name)
                    .AsDefensive()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        $"Interject used to interrupt {target.Name?.TextValue}'s cast. Stopping interruptible casts prevents avoidable damage.",
                        "Interject is your dedicated interrupt. Interruptible casts are marked in the duty finder and cause significant damage if they complete.")
                    .Factors("Target casting an interruptible ability", "Interject available", "Interrupt is highest priority utility")
                    .Alternatives("Low Blow (stun interrupt, longer cooldown)", "Do nothing (take avoidable damage)")
                    .Tip("Always interrupt interruptible casts — this is one of a tank's key responsibilities. Check for the purple cast bar.")
                    .Concept(WarConcepts.TankSwap)
                    .Record();

                context.TrainingService?.RecordConceptApplication(WarConcepts.TankSwap, true, "Cast interrupted");

                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        // Try Low Blow as backup (stun can interrupt some casts)
        if (level >= 12 && context.ActionService.IsActionReady(RoleActions.LowBlow.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.LowBlow.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(RoleActions.LowBlow, target.GameObjectId))
            {
                context.Debug.PlannedAction = RoleActions.LowBlow.Name;
                context.Debug.MitigationState = "Stunned (interrupt)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.LowBlow.ActionId, RoleActions.LowBlow.Name)
                    .AsDefensive()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        $"Low Blow used as backup interrupt (stun) on {target.Name?.TextValue}'s cast. Interject was unavailable.",
                        "Low Blow stuns the target, which can interrupt their cast. It has a longer cooldown than Interject — use it only when Interject is on cooldown.")
                    .Factors("Target casting interruptible ability", "Interject on cooldown", "Low Blow stun as fallback interrupt")
                    .Alternatives("Interject (preferred, shorter cooldown)", "Do nothing (take avoidable damage)")
                    .Tip("Keep both Interject and Low Blow in mind for back-to-back interrupt situations. Tanks are often responsible for all interrupts.")
                    .Concept(WarConcepts.TankSwap)
                    .Record();

                context.TrainingService?.RecordConceptApplication(WarConcepts.TankSwap, true, "Stun interrupt fallback");

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
        if (context.Configuration.Tank.EnableBloodWhetting &&
            level >= WARActions.RawIntuition.MinLevel &&
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
        if (level >= RoleActions.Rampart.MinLevel &&
            !context.HasActiveMitigation &&
            !context.StatusHelper.HasRampart(player))
        {
            if (context.ActionService.IsActionReady(RoleActions.Rampart.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(RoleActions.Rampart, player.GameObjectId))
                {
                    context.Debug.PlannedAction = RoleActions.Rampart.Name;
                    context.Debug.MitigationState = $"Proactive Rampart ({reason})";
                    return true;
                }
            }
        }

        // Priority 3: Vengeance/Damnation (major CD for big hits)
        if (context.Configuration.Tank.EnableVengeance &&
            level >= WARActions.Vengeance.MinLevel &&
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
        if (!context.Configuration.Tank.EnableHolmgang) return false;

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
        if (!context.Configuration.Tank.EnableVengeance) return false;

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

        if (level < RoleActions.Rampart.MinLevel)
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

        if (!context.ActionService.IsActionReady(RoleActions.Rampart.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(RoleActions.Rampart, player.GameObjectId))
        {
            context.Debug.PlannedAction = RoleActions.Rampart.Name;
            context.Debug.MitigationState = $"Rampart ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(RoleActions.Rampart.ActionId, 90_000);

            TrainingHelper.Decision(context.TrainingService)
                .Action(RoleActions.Rampart.ActionId, RoleActions.Rampart.Name)
                .AsMitigation(hpPercent)
                .Reason(
                    $"Rampart activated for 20% damage reduction. HP at {hpPercent:P0} and taking sustained damage.",
                    "Rampart is a role action giving 20% mitigation for 20 seconds, with a 90s recast. It's your most accessible mitigation — use it frequently during heavy damage phases.")
                .Factors($"HP at {hpPercent:P0}", $"Damage rate: {damageRate:F1}/s", "No major mitigation currently active")
                .Alternatives("Use Vengeance (stronger but longer cooldown)", "Rely on Bloodwhetting (less mitigation, more healing)")
                .Tip("Use Rampart early and often — the 90s cooldown means you can often get two uses in a long boss fight. Don't save it for 'the big hit'.")
                .Concept(WarConcepts.MitigationStacking)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.MitigationStacking, true, "Rampart mitigation");

            return true;
        }

        return false;
    }

    #endregion

    #region Short Cooldowns

    private bool TryBloodwhetting(IAresContext context, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableBloodWhetting) return false;

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
        if (!context.Configuration.Tank.EnableThrillOfBattle) return false;

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

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.ThrillOfBattle.ActionId, WARActions.ThrillOfBattle.Name)
                .AsMitigation(hpPercent)
                .Reason(
                    $"Thrill of Battle used at {hpPercent:P0} HP. Temporarily increases maximum HP by 20% and restores proportional HP.",
                    "Thrill of Battle boosts max HP by 20% for 10 seconds and immediately heals the equivalent amount. It's effectively a 20% HP buffer — great when you're mid-range HP but expect a big hit.")
                .Factors($"HP at {hpPercent:P0}", "Thrill of Battle available", "20% max HP boost + heal")
                .Alternatives("Use Rampart (mitigation instead of HP boost)", "Wait for healers (puts strain on party)")
                .Tip("Thrill of Battle is unique — it inflates your HP pool temporarily. Use it before tankbusters or when healers are busy. The heal is especially good at 60-70% HP.")
                .Concept(WarConcepts.ThrillOfBattle)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.ThrillOfBattle, true, "HP boost and mitigation");

            return true;
        }

        return false;
    }

    private bool TryEquilibrium(IAresContext context, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableEquilibrium) return false;

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

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.Equilibrium.ActionId, WARActions.Equilibrium.Name)
                .AsMitigation(hpPercent)
                .Reason(
                    $"Equilibrium used at {hpPercent:P0} HP. Heals you for 1200 potency instantly and applies a 200-potency regen.",
                    "Equilibrium is a powerful self-heal (1200 potency heal + regen) on a 60s cooldown. Warriors can heal themselves effectively — use Equilibrium when low to reduce healer burden.")
                .Factors($"HP at {hpPercent:P0}", "Equilibrium available", "Strong self-heal reduces healer GCD usage")
                .Alternatives("Wait for healer (puts burden on healers)", "Use Bloodwhetting (mitigation + healing, lower threshold)")
                .Tip("Equilibrium is a free heal that saves healer GCDs. Use it at or below 50% HP whenever it's available. Warriors are self-sufficient tanks.")
                .Concept(WarConcepts.Equilibrium)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.Equilibrium, true, "Self-heal to reduce healer load");

            return true;
        }

        return false;
    }

    private bool TryArmsLength(IAresContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.ArmsLength.MinLevel)
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

        if (level < RoleActions.Reprisal.MinLevel)
            return false;

        // Reprisal targets an enemy — bail if no target
        var target = context.CurrentTarget;
        if (target == null)
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

        // Only use Reprisal when there is incoming damage pressure
        // Require multiple injured party members or low average HP
        var (avgHp, lowestHp, injuredCount) = context.PartyHealthMetrics;
        if (injuredCount < 3 && avgHp > 0.85f)
            return false;

        // Use Reprisal as a party mitigation tool
        // Best used before raidwides or during pulls with multiple enemies

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);

        if (!context.ActionService.IsActionReady(RoleActions.Reprisal.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(RoleActions.Reprisal, target.EntityId))
        {
            context.Debug.PlannedAction = RoleActions.Reprisal.Name;
            context.Debug.MitigationState = $"Reprisal ({enemyCount} enemies)";
            partyCoord?.OnCooldownUsed(RoleActions.Reprisal.ActionId, 60_000);

            TrainingHelper.Decision(context.TrainingService)
                .Action(RoleActions.Reprisal.ActionId, RoleActions.Reprisal.Name)
                .AsPartyMit()
                .Reason(
                    $"Reprisal used to reduce enemy damage by 10% for {enemyCount} enemies. Party mitigation for multi-target pulls.",
                    "Reprisal reduces the targets' damage dealt by 10% for 10 seconds. Since it affects all enemies in range, it's exceptionally strong during dungeon pulls.")
                .Factors($"{enemyCount} enemies in range", "Reprisal available", "60s cooldown — frequent use in pulls")
                .Alternatives("Save for raidwide (depends on content)", "Rely on personal mitigation (loses party value)")
                .Tip("Use Reprisal frequently in dungeon pulls — it reduces damage from every enemy hitting you. In raids, coordinate with other tanks for raidwide coverage.")
                .Concept(WarConcepts.PartyProtection)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.PartyProtection, true, "Party mitigation deployed");

            return true;
        }

        return false;
    }

    private bool TryShakeItOff(IAresContext context)
    {
        if (!context.Configuration.Tank.EnableShakeItOff) return false;

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
        if (injuredCount < 3 && avgHp > 0.85f)
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
        if (!context.Configuration.Tank.EnableNascentFlash) return false;

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

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.NascentFlash.ActionId, WARActions.NascentFlash.Name)
                .AsPartyMit()
                .Target(flashTarget.Name?.TextValue)
                .Reason(
                    $"Nascent Flash used on {flashTarget.Name?.TextValue} ({targetHp:P0} HP) — grants them mitigation and heals you both when you attack.",
                    "Nascent Flash gives an ally a 16% mitigation shield and triggers a heal-on-hit effect for 8 seconds. Warriors are unique in being able to support allies with their attacks.")
                .Factors($"Ally {flashTarget.Name?.TextValue} at {targetHp:P0} HP", "Your own HP above 60%", "25s cooldown — reactive support")
                .Alternatives("Shake It Off (party-wide, different trigger)", "Focus on your own survival (less team support)")
                .Tip("Use Nascent Flash on DPS or healers taking spike damage. It gives them a shield and every weapon skill you land heals them.")
                .Concept(WarConcepts.NascentFlash)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.NascentFlash, true, "Ally protection with heal-on-hit");

            return true;
        }

        return false;
    }

    #endregion
}
