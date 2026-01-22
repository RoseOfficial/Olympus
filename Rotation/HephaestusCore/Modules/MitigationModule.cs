using Olympus.Data;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Services.Party;
using Olympus.Timeline;

namespace Olympus.Rotation.HephaestusCore.Modules;

/// <summary>
/// Handles the Gunbreaker defensive rotation.
/// Manages personal mitigation, party mitigation, and Heart of Corundum with intelligent usage.
/// </summary>
public sealed class MitigationModule : IHephaestusModule
{
    public int Priority => 10; // High priority for defensives
    public string Name => "Mitigation";

    public bool TryExecute(IHephaestusContext context, bool isMoving)
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

        // CRITICAL: Skip mitigation during Superbolide
        if (context.HasSuperbolide)
        {
            context.Debug.MitigationState = "Superbolide active";
            return false;
        }

        // Timeline-aware proactive mitigation (if timeline data available)
        if (TryTimelineAwareMitigation(context, hpPercent))
            return true;

        // Priority 1: Emergency invulnerability (Superbolide)
        if (TrySuperbolide(context, hpPercent))
            return true;

        // Priority 2: Heart of Corundum (intelligent usage)
        if (TryHeartOfCorundum(context, hpPercent, damageRate))
            return true;

        // Priority 3: Major cooldown (Nebula/Great Nebula)
        if (TryNebula(context, hpPercent, damageRate))
            return true;

        // Priority 4: Rampart (role action)
        if (TryRampart(context, hpPercent, damageRate))
            return true;

        // Priority 5: Camouflage (parry buff)
        if (TryCamouflage(context, hpPercent))
            return true;

        // Priority 6: Aurora (self-HoT)
        if (TryAurora(context, hpPercent))
            return true;

        // Priority 7: Heart of Light (party magic mitigation)
        if (TryHeartOfLight(context))
            return true;

        // Priority 8: Reprisal (party mitigation via enemy debuff)
        if (TryReprisal(context))
            return true;

        // Priority 9: Arm's Length for knockback immunity
        if (TryArmsLength(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IHephaestusContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Interrupt

    /// <summary>
    /// Attempts to interrupt an enemy cast using Interject or Low Blow.
    /// Coordinates with other Olympus instances to prevent duplicate interrupts.
    /// </summary>
    private bool TryInterrupt(IHephaestusContext context)
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
        if (context.ActionService.IsActionReady(GNBActions.Interject.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, GNBActions.Interject.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(GNBActions.Interject, target.GameObjectId))
            {
                context.Debug.PlannedAction = GNBActions.Interject.Name;
                context.Debug.MitigationState = "Interrupted cast";
                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        // Try Low Blow as backup (stun can interrupt some casts)
        if (level >= 12 && context.ActionService.IsActionReady(GNBActions.LowBlow.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, GNBActions.LowBlow.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(GNBActions.LowBlow, target.GameObjectId))
            {
                context.Debug.PlannedAction = GNBActions.LowBlow.Name;
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
    private bool TryTimelineAwareMitigation(IHephaestusContext context, float hpPercent)
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

        // Don't stack with Superbolide
        if (context.HasSuperbolide)
            return false;

        // Priority 1: Heart of Corundum/Stone (short CD)
        if (level >= GNBActions.HeartOfStone.MinLevel &&
            !context.HasHeartOfCorundum)
        {
            var heartAction = GNBActions.GetHeartAction(level);
            if (context.ActionService.IsActionReady(heartAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(heartAction, player.GameObjectId))
                {
                    context.Debug.PlannedAction = heartAction.Name;
                    context.Debug.MitigationState = $"Proactive Heart ({reason})";
                    return true;
                }
            }
        }

        // Priority 2: Rampart (if no active mitigation)
        if (level >= GNBActions.Rampart.MinLevel &&
            !context.HasActiveMitigation &&
            !context.StatusHelper.HasRampart(player))
        {
            if (context.ActionService.IsActionReady(GNBActions.Rampart.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(GNBActions.Rampart, player.GameObjectId))
                {
                    context.Debug.PlannedAction = GNBActions.Rampart.Name;
                    context.Debug.MitigationState = $"Proactive Rampart ({reason})";
                    return true;
                }
            }
        }

        // Priority 3: Nebula/Great Nebula (major CD for big hits)
        if (level >= GNBActions.Nebula.MinLevel &&
            !context.HasNebula)
        {
            var nebulaAction = GNBActions.GetNebulaAction(level);
            if (context.ActionService.IsActionReady(nebulaAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(nebulaAction, player.GameObjectId))
                {
                    context.Debug.PlannedAction = nebulaAction.Name;
                    context.Debug.MitigationState = $"Proactive Nebula ({reason})";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Emergency Mitigation

    private bool TrySuperbolide(IHephaestusContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Superbolide.MinLevel)
            return false;

        // Only use Superbolide in emergencies
        if (hpPercent > 0.15f)
            return false;

        // Don't use if already under Superbolide
        if (context.HasSuperbolide)
            return false;

        if (!context.ActionService.IsActionReady(GNBActions.Superbolide.ActionId))
            return false;

        // Check if another tank recently used an invuln (coordination)
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableInvulnerabilityCoordination &&
            partyCoord?.WasInvulnerabilityUsedRecently(tankConfig.InvulnerabilityStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Superbolide delayed (remote invuln)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(GNBActions.Superbolide, player.GameObjectId))
        {
            context.Debug.PlannedAction = GNBActions.Superbolide.Name;
            context.Debug.MitigationState = $"Emergency invuln ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(GNBActions.Superbolide.ActionId, 360_000);
            return true;
        }

        return false;
    }

    #endregion

    #region Heart of Corundum - Intelligent Usage

    /// <summary>
    /// Heart of Corundum intelligence matrix.
    /// GNB's signature short defensive (25s cooldown).
    /// - Base: 15% damage reduction for 4s
    /// - Catharsis: Heal when HP falls below 50%
    /// - Clarity of Corundum: Extended duration by 4s
    /// Unlike DRK's TBN, there's no DPS consideration - use liberally.
    /// </summary>
    private bool TryHeartOfCorundum(IHephaestusContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        // Use appropriate Heart action
        var heartAction = GNBActions.GetHeartAction(level);
        if (level < GNBActions.HeartOfStone.MinLevel)
            return false;

        // Already have Heart active
        if (context.HasHeartOfCorundum)
            return false;

        // Don't use during Superbolide
        if (context.HasSuperbolide)
            return false;

        // Heart of Corundum Decision Matrix:
        // Unlike TBN, HoC has no damage/resource consideration
        // 25s cooldown is short enough to use frequently

        // 1. Emergency: HP < 40% - always use for survival
        if (hpPercent < 0.40f)
        {
            return ExecuteHeart(context, player.GameObjectId, heartAction, "Emergency Heart");
        }

        // 2. Main tank taking damage: HP < 60%
        if (context.IsMainTank && hpPercent < 0.60f && damageRate > 0.02f)
        {
            return ExecuteHeart(context, player.GameObjectId, heartAction, "Reactive Heart");
        }

        // 3. Proactive usage: HP < 80% while tanking
        if (context.IsMainTank && hpPercent < 0.80f && damageRate > 0.01f)
        {
            return ExecuteHeart(context, player.GameObjectId, heartAction, "Proactive Heart");
        }

        // 4. Consider Heart on party members (future: co-tank support)
        var heartTarget = context.PartyHelper.FindHeartOfCorundumTarget(player, 0.60f);
        if (heartTarget != null && heartTarget.GameObjectId != player.GameObjectId)
        {
            // Check distance (Heart range is 30y)
            var dx = player.Position.X - heartTarget.Position.X;
            var dy = player.Position.Y - heartTarget.Position.Y;
            var dz = player.Position.Z - heartTarget.Position.Z;
            var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

            if (distance <= 30f)
            {
                var targetHp = context.PartyHelper.GetHpPercent(heartTarget);
                return ExecuteHeart(context, heartTarget.GameObjectId, heartAction, $"Heart on ally ({targetHp:P0} HP)");
            }
        }

        return false;
    }

    private bool ExecuteHeart(IHephaestusContext context, ulong targetId,
        Models.Action.ActionDefinition action, string reason)
    {
        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(action, targetId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.MitigationState = reason;
            return true;
        }
        return false;
    }

    #endregion

    #region Major Cooldowns

    private bool TryNebula(IHephaestusContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine which Nebula variant to use
        var nebulaAction = GNBActions.GetNebulaAction(level);

        if (level < GNBActions.Nebula.MinLevel)
            return false;

        // Check if we should use major cooldown
        if (!context.TankCooldownService.ShouldUseMajorCooldown(hpPercent, damageRate))
            return false;

        // Don't stack with Superbolide
        if (context.HasSuperbolide)
            return false;

        // Don't stack with existing Nebula
        if (context.HasNebula)
            return false;

        // Check if another tank recently used a personal defensive (coordination)
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Nebula delayed (remote tank mit)";
            return false;
        }

        if (!context.ActionService.IsActionReady(nebulaAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(nebulaAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = nebulaAction.Name;
            context.Debug.MitigationState = $"Major CD ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(nebulaAction.ActionId, 120_000);
            return true;
        }

        return false;
    }

    private bool TryRampart(IHephaestusContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Rampart.MinLevel)
            return false;

        // Check if we should use mitigation
        if (!context.TankCooldownService.ShouldUseMitigation(hpPercent, damageRate, context.HasActiveMitigation))
            return false;

        // Don't stack with Superbolide
        if (context.HasSuperbolide)
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

        if (!context.ActionService.IsActionReady(GNBActions.Rampart.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(GNBActions.Rampart, player.GameObjectId))
        {
            context.Debug.PlannedAction = GNBActions.Rampart.Name;
            context.Debug.MitigationState = $"Rampart ({hpPercent:P0} HP)";
            partyCoord?.OnCooldownUsed(GNBActions.Rampart.ActionId, 90_000);
            return true;
        }

        return false;
    }

    #endregion

    #region Short Cooldowns

    private bool TryCamouflage(IHephaestusContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Camouflage.MinLevel)
            return false;

        // Camouflage gives 50% parry rate + 10% DR
        // Use when HP is low
        if (hpPercent > context.Configuration.Tank.MitigationThreshold)
            return false;

        // Don't stack with Superbolide
        if (context.HasSuperbolide)
            return false;

        // Don't use if already have Camouflage active
        if (context.HasCamouflage)
            return false;

        if (!context.ActionService.IsActionReady(GNBActions.Camouflage.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(GNBActions.Camouflage, player.GameObjectId))
        {
            context.Debug.PlannedAction = GNBActions.Camouflage.Name;
            context.Debug.MitigationState = $"Camouflage ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryAurora(IHephaestusContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Aurora.MinLevel)
            return false;

        // Aurora is a HoT with 2 charges
        // Use on self when injured, or on allies

        // First, check if we need it ourselves
        if (hpPercent < 0.70f && !context.HasAurora)
        {
            if (context.ActionService.IsActionReady(GNBActions.Aurora.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(GNBActions.Aurora, player.GameObjectId))
                {
                    context.Debug.PlannedAction = GNBActions.Aurora.Name;
                    context.Debug.MitigationState = $"Self Aurora ({hpPercent:P0} HP)";
                    return true;
                }
            }
        }

        // If we're fine, look for party members who need help
        if (hpPercent >= 0.70f)
        {
            var auroraTarget = context.PartyHelper.FindAuroraTarget(player, 0.70f);
            if (auroraTarget != null && auroraTarget.GameObjectId != player.GameObjectId)
            {
                // Check distance (Aurora range is 30y)
                var dx = player.Position.X - auroraTarget.Position.X;
                var dy = player.Position.Y - auroraTarget.Position.Y;
                var dz = player.Position.Z - auroraTarget.Position.Z;
                var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

                if (distance <= 30f && context.ActionService.IsActionReady(GNBActions.Aurora.ActionId))
                {
                    if (context.ActionService.ExecuteOgcd(GNBActions.Aurora, auroraTarget.GameObjectId))
                    {
                        var targetHp = context.PartyHelper.GetHpPercent(auroraTarget);
                        context.Debug.PlannedAction = GNBActions.Aurora.Name;
                        context.Debug.MitigationState = $"Aurora ({targetHp:P0} HP ally)";
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool TryArmsLength(IHephaestusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.ArmsLength.MinLevel)
            return false;

        // Arm's Length is primarily used for knockback immunity
        // For now, we'll skip automatic usage - requires mechanic detection
        // Could be enhanced with boss mechanic detection in the future

        return false;
    }

    #endregion

    #region Party Mitigation

    private bool TryHeartOfLight(IHephaestusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.HeartOfLight.MinLevel)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Heart of Light skipped (remote mit)";
            return false;
        }

        // Heart of Light is party-wide magic damage reduction
        // Best used before raidwides

        // Check if multiple party members are injured (sign of incoming damage)
        var (avgHp, lowestHp, injuredCount) = context.PartyHealthMetrics;

        // Use when multiple party members are injured
        if (injuredCount < 3 || avgHp > 0.85f)
            return false;

        if (!context.ActionService.IsActionReady(GNBActions.HeartOfLight.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(GNBActions.HeartOfLight, player.GameObjectId))
        {
            context.Debug.PlannedAction = GNBActions.HeartOfLight.Name;
            context.Debug.MitigationState = $"Heart of Light ({injuredCount} injured)";
            partyCoord?.OnCooldownUsed(GNBActions.HeartOfLight.ActionId, 90_000);
            return true;
        }

        return false;
    }

    private bool TryReprisal(IHephaestusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Reprisal.MinLevel)
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

        if (!context.ActionService.IsActionReady(GNBActions.Reprisal.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(GNBActions.Reprisal, player.GameObjectId))
        {
            context.Debug.PlannedAction = GNBActions.Reprisal.Name;
            context.Debug.MitigationState = $"Reprisal ({enemyCount} enemies)";
            partyCoord?.OnCooldownUsed(GNBActions.Reprisal.ActionId, 60_000);
            return true;
        }

        return false;
    }

    #endregion
}
