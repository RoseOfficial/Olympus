using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Services.Party;
using Olympus.Services.Training;
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

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(heartAction.ActionId, heartAction.Name)
                        .AsMitigation(hpPercent)
                        .Target("Self")
                        .Reason(
                            $"Proactive {heartAction.Name} - {reason}",
                            $"Timeline analysis predicts a tankbuster in {secondsUntil:F1} seconds. " +
                            $"{heartAction.Name} is being pre-applied to be active when the damage lands. " +
                            "Pre-stacking mitigation 1.5-4 seconds early ensures full coverage during the hit.")
                        .Factors(reason, "Timeline prediction high confidence", "Pre-stacking 1.5-4s before impact")
                        .Alternatives("React after damage (risky)", "Wait for healer to handle (pressure on healer)")
                        .Tip("Proactive mitigation is always better than reactive. When you know a tankbuster is coming, use defensives 2-3 seconds early for full coverage.")
                        .Concept("gnb_heart_of_corundum")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_heart_of_corundum", true, "Proactive timeline mitigation");

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

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Rampart.ActionId, RoleActions.Rampart.Name)
                        .AsMitigation(hpPercent)
                        .Target("Self")
                        .Reason(
                            $"Proactive Rampart - {reason}",
                            $"Timeline analysis predicts a tankbuster in {secondsUntil:F1} seconds. " +
                            "Rampart (20% damage reduction for 20 seconds) is being pre-applied with no active mitigation. " +
                            "Using Rampart early ensures it's active and contributes its full value during the hit.")
                        .Factors(reason, "No active mitigation currently", "Timeline prediction high confidence")
                        .Alternatives("Stack with Heart of Corundum for more coverage", "Use Nebula for bigger hits")
                        .Tip("Pre-stacking Rampart before predicted tankbusters is a core tanking skill. Plan your mitigation cooldown rotation around fight timelines.")
                        .Concept("gnb_nebula")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_nebula", true, "Proactive Rampart timeline");

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

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(nebulaAction.ActionId, nebulaAction.Name)
                        .AsMitigation(hpPercent)
                        .Target("Self")
                        .Reason(
                            $"Proactive {nebulaAction.Name} - {reason}",
                            $"Timeline analysis predicts a tankbuster in {secondsUntil:F1} seconds. " +
                            $"{nebulaAction.Name} (30% damage reduction) is being pre-applied for the incoming heavy hit. " +
                            "This is GNB's strongest personal cooldown and should be saved for the hardest hits.")
                        .Factors(reason, "Timeline prediction high confidence", $"Pre-applying {nebulaAction.Name} for maximum coverage")
                        .Alternatives("Stack Heart of Corundum too for extra mitigation", "Save Nebula for an even bigger hit later")
                        .Tip($"Use {nebulaAction.Name} for your hardest-hitting tankbusters. With proactive stacking, it provides full 30% DR when damage lands.")
                        .Concept("gnb_nebula")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_nebula", true, "Proactive Nebula timeline");

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

            // Training: Record Superbolide decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(GNBActions.Superbolide.ActionId, GNBActions.Superbolide.Name)
                .AsInvuln(hpPercent)
                .Reason(
                    $"Emergency Superbolide at {hpPercent:P0} HP",
                    "Superbolide is GNB's invulnerability that grants immunity for 10 seconds but drops HP to 1. " +
                    "Used in emergencies when death is imminent and healers need time to stabilize. " +
                    "Unlike other tank invulns, Superbolide doesn't require specific healer actions to survive - you simply need healing back up before the effect ends.")
                .Factors($"HP critical ({hpPercent:P0})", "Death imminent without invuln", "6-minute cooldown is worth using to prevent wipe")
                .Alternatives("Use Nebula (only 30% DR, not enough)", "Use Heart of Corundum (only 15% DR)", "Die and cause potential wipe")
                .Tip("Superbolide sets HP to 1 - coordinate with healers so they're ready to heal you back up. Don't panic if you see your HP drop!")
                .Concept("gnb_superbolide")
                .Record();
            context.TrainingService?.RecordConceptApplication("gnb_superbolide", true, "Emergency invulnerability");

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
        if (!context.Configuration.Tank.EnableHeartOfCorundum) return false;

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

        // 3. Proactive usage: HP below the configured threshold while tanking
        if (context.IsMainTank && hpPercent < context.Configuration.Tank.HeartOfCorundumThreshold && damageRate > 0.01f)
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

            // Training: Record Heart of Corundum/Stone decision
            var hpPercent = (float)context.Player.CurrentHp / context.Player.MaxHp;
            var isSelf = targetId == context.Player.GameObjectId;
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsMitigation(hpPercent)
                .Target(isSelf ? "Self" : "Ally")
                .Reason(
                    reason,
                    $"{action.Name} is GNB's signature short defensive (25s cooldown). " +
                    "Provides 15% damage reduction for 4s, plus Catharsis (heal when HP falls below 50%) and Clarity of Corundum (extended duration). " +
                    "Unlike TBN, there's no DPS cost - use liberally for yourself and allies.")
                .Factors(reason, "25s cooldown allows frequent use", "Can target party members for support")
                .Alternatives("Save for bigger hit (but short CD means it will be back)", "Use on different target")
                .Tip("Heart of Corundum is very forgiving - the short cooldown means you should use it frequently rather than saving it.")
                .Concept("gnb_heart_of_corundum")
                .Record();
            context.TrainingService?.RecordConceptApplication("gnb_heart_of_corundum", true, reason);

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

            // Training: Record Nebula decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(nebulaAction.ActionId, nebulaAction.Name)
                .AsMitigation(hpPercent)
                .Target("Self")
                .Reason(
                    $"Nebula at {hpPercent:P0} HP",
                    $"{nebulaAction.Name} is GNB's major defensive cooldown providing 30% damage reduction for 15 seconds. " +
                    "Great Nebula (Lv.92+) also adds a heal-over-time effect. " +
                    "Best used for tankbusters or sustained heavy damage periods.")
                .Factors($"HP at {hpPercent:P0}", $"Taking significant damage (rate: {damageRate:F3})", "2-minute cooldown available")
                .Alternatives("Use Rampart instead (only 20% DR)", "Stack with Heart of Corundum for more mitigation")
                .Tip("Nebula is your strongest personal defensive - don't hold it too long. Plan its usage around known tankbusters.")
                .Concept("gnb_nebula")
                .Record();
            context.TrainingService?.RecordConceptApplication("gnb_nebula", true, "Major defensive cooldown");

            return true;
        }

        return false;
    }

    private bool TryRampart(IHephaestusContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.Rampart.MinLevel)
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
                .Target("Self")
                .Reason(
                    $"Rampart at {hpPercent:P0} HP",
                    "Rampart is a role action providing 20% damage reduction for 20 seconds (90s cooldown). " +
                    "It's weaker than Nebula (30%) but has a shorter cooldown, making it a reliable mid-tier defensive. " +
                    "Best used when taking significant sustained damage or before a predictable tankbuster.")
                .Factors($"HP at {hpPercent:P0}", $"Damage rate elevated ({damageRate:F3})", "90s cooldown available")
                .Alternatives("Stack with Nebula for bigger hits", "Use Heart of Corundum (shorter CD, different mitigation type)")
                .Tip("Rampart and Nebula don't share a cooldown - you can use both together for major tankbusters requiring heavy mitigation.")
                .Concept("gnb_nebula")
                .Record();
            context.TrainingService?.RecordConceptApplication("gnb_nebula", true, "Rampart defensive cooldown");

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

            TrainingHelper.Decision(context.TrainingService)
                .Action(GNBActions.Camouflage.ActionId, GNBActions.Camouflage.Name)
                .AsMitigation(hpPercent)
                .Target("Self")
                .Reason(
                    $"Camouflage at {hpPercent:P0} HP",
                    "Camouflage gives 50% parry rate increase for 20 seconds (90s cooldown), plus 10% damage reduction. " +
                    "Parry mitigation is variable (depends on incoming hit type and proc), but the baseline 10% DR is consistent. " +
                    "Use it as a supplementary defensive when HP is low.")
                .Factors($"HP at {hpPercent:P0}", "Camouflage ready", "50% parry boost + 10% base DR")
                .Alternatives("Heart of Corundum (reliable 15% DR)", "Rampart (20% DR, more consistent)")
                .Tip("Camouflage's parry mitigation is somewhat RNG-based on physical attacks. It's a useful cooldown but prioritize more reliable mitigation first.")
                .Concept("gnb_camouflage")
                .Record();
            context.TrainingService?.RecordConceptApplication("gnb_camouflage", true, "Camouflage defensive");

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

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(GNBActions.Aurora.ActionId, GNBActions.Aurora.Name)
                        .AsMitigation(hpPercent)
                        .Target("Self")
                        .Reason(
                            $"Self Aurora at {hpPercent:P0} HP",
                            "Aurora applies a healing-over-time (HoT) that restores HP over 18 seconds. " +
                            "With 2 charges (45s recharge each), it can be used frequently for sustained self-healing. " +
                            "Using on self when HP is low reduces pressure on the healer.")
                        .Factors($"HP at {hpPercent:P0} (below 70%)", "Aurora not already active", "HoT provides sustained recovery")
                        .Alternatives("Use on an ally who needs healing more", "Let healer handle (might be occupied)")
                        .Tip("Aurora has 2 charges - use one on yourself when injured and keep one available for a party member in need.")
                        .Concept("gnb_aurora")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_aurora", true, "Self Aurora heal");

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

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(GNBActions.Aurora.ActionId, GNBActions.Aurora.Name)
                            .AsMitigation(targetHp)
                            .Target(auroraTarget.Name?.TextValue ?? "Ally")
                            .Reason(
                                $"Aurora on ally at {targetHp:P0} HP",
                                "Aurora can be targeted on any party member within 30 yards, making it a supportive tool for a tank. " +
                                "With 2 charges, you can afford to share healing with injured allies when you are healthy. " +
                                "This reduces healer GCD pressure during heavy damage phases.")
                            .Factors($"Ally HP at {targetHp:P0} (below 70%)", "Self HP healthy (above 70%)", "Aurora charge available")
                            .Alternatives("Use another charge later if self HP drops", "Save for co-tank during tankbuster")
                            .Tip("As a tank, using Aurora on low-HP party members is a valuable contribution - tanks rarely use AoE healing tools, but Aurora is an exception.")
                            .Concept("gnb_aurora")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("gnb_aurora", true, "Ally Aurora heal");

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

        if (level < RoleActions.ArmsLength.MinLevel)
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
        if (!context.Configuration.Tank.EnableHeartOfLight) return false;

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
        if (injuredCount < 3 && avgHp > 0.85f)
            return false;

        if (!context.ActionService.IsActionReady(GNBActions.HeartOfLight.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(GNBActions.HeartOfLight, player.GameObjectId))
        {
            context.Debug.PlannedAction = GNBActions.HeartOfLight.Name;
            context.Debug.MitigationState = $"Heart of Light ({injuredCount} injured)";
            partyCoord?.OnCooldownUsed(GNBActions.HeartOfLight.ActionId, 90_000);

            // Training: Record Heart of Light decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(GNBActions.HeartOfLight.ActionId, GNBActions.HeartOfLight.Name)
                .AsPartyMit()
                .Reason(
                    $"Party mitigation with {injuredCount} injured members",
                    "Heart of Light is GNB's party-wide magic damage reduction (10% for 15s). " +
                    "Best used before predictable raidwide magic damage to reduce healing burden. " +
                    "Coordinate with other tank's party mitigation to avoid overlap.")
                .Factors($"{injuredCount} party members injured", $"Average party HP: {avgHp:P0}", "Expecting more damage or recovering from raidwide")
                .Alternatives("Save for next raidwide", "Let healers handle with their tools", "Use Reprisal instead (requires enemy target)")
                .Tip("Heart of Light only affects magic damage - check if incoming damage is physical or magical when planning mitigation.")
                .Concept("gnb_heart_of_light")
                .Record();
            context.TrainingService?.RecordConceptApplication("gnb_heart_of_light", true, "Party magic mitigation");

            return true;
        }

        return false;
    }

    private bool TryReprisal(IHephaestusContext context)
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
                    $"Reprisal used to reduce enemy damage by 10% for {enemyCount} enemies. Party mitigation during heavy damage phase.",
                    "Reprisal reduces the targets' damage dealt by 10% for 10 seconds. Since it affects all enemies in range, it's exceptionally strong during dungeon pulls and raidwides.")
                .Factors($"{enemyCount} enemies in range", $"{injuredCount} party members injured", $"Party average HP: {avgHp:P0}", "Reprisal available", "60s cooldown — deploy during damage pressure")
                .Alternatives("Save for raidwide (depends on content)", "Heart of Light (magic only)", "Rely on personal mitigation (loses party value)")
                .Tip("Use Reprisal during dungeon pulls — it reduces damage from every enemy hitting you. In raids, coordinate with your co-tank for raidwide coverage.")
                .Concept("gnb_heart_of_light")
                .Record();

            context.TrainingService?.RecordConceptApplication("gnb_heart_of_light", true, "Party mitigation deployed");

            return true;
        }

        return false;
    }

    #endregion
}
