using Olympus.Data;
using Olympus.Rotation.AresCore.Abilities;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.RoleActionHelpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Party;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.AresCore.Modules;

/// <summary>
/// Handles the Warrior defensive rotation (scheduler-driven).
/// </summary>
public sealed class MitigationModule : IAresModule
{
    public int Priority => 10;
    public string Name => "Mitigation";

    public bool TryExecute(IAresContext context, bool isMoving) => false;

    public void UpdateDebugState(IAresContext context) { }

    public void CollectCandidates(IAresContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.Configuration.Tank.EnableMitigation)
        {
            context.Debug.MitigationState = "Disabled";
            return;
        }

        if (!context.InCombat)
        {
            context.Debug.MitigationState = "Not in combat";
            return;
        }

        TryPushInterrupt(context, scheduler);

        var player = context.Player;
        var hpPercent = (float)player.CurrentHp / player.MaxHp;
        var damageRate = context.DamageIntakeService.GetDamageRate(player.EntityId);
        context.TankCooldownService.Update(hpPercent, damageRate);

        TryPushTimelineAwareMitigation(context, scheduler, hpPercent);
        TryPushHolmgang(context, scheduler, hpPercent);
        TryPushMajorCooldown(context, scheduler, hpPercent, damageRate);
        TryPushRampart(context, scheduler, hpPercent, damageRate);
        TryPushBloodwhetting(context, scheduler, hpPercent);
        TryPushThrillOfBattle(context, scheduler, hpPercent);
        TryPushEquilibrium(context, scheduler, hpPercent);
        TryPushReprisal(context, scheduler);
        TryPushShakeItOff(context, scheduler);
        TryPushNascentFlash(context, scheduler, hpPercent);
    }

    private void TryPushInterrupt(IAresContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < 18) return;

        var target = context.CurrentTarget;
        if (target == null) return;
        if (!target.IsCasting) return;
        if (!target.IsCastInterruptible) return;

        var targetId = target.EntityId;
        var delaySeed = (int)(target.EntityId * 2654435761u ^ (uint)(target.TotalCastTime * 1000f));
        var interruptDelay = 0.3f + ((delaySeed & 0xFFFF) / 65535f) * 0.4f;
        if (target.CurrentCastTime < interruptDelay) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableInterruptCoordination && partyCoord?.IsInterruptTargetReservedByOther(targetId) == true)
        {
            context.Debug.MitigationState = "Interrupt reserved by other";
            return;
        }

        var remainingCastTime = (target.TotalCastTime - target.CurrentCastTime) * 1000f;
        var castTimeMs = (int)remainingCastTime;
        var targetName = target.Name?.TextValue;

        if (context.ActionService.IsActionReady(RoleActions.Interject.ActionId))
        {
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.Interject.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return;
                }
            }
            scheduler.PushOgcd(AresAbilities.Interject, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RoleActions.Interject.Name;
                    context.Debug.MitigationState = "Interrupted cast";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Interject.ActionId, RoleActions.Interject.Name)
                        .AsDefensive()
                        .Target(targetName)
                        .Reason($"Interject used to interrupt {targetName}'s cast.", "Interject silences enemy casts.")
                        .Factors("Target casting interruptible", "Interject available")
                        .Alternatives("Low Blow (stun backup)")
                        .Tip("Always interrupt interruptible casts.")
                        .Concept(WarConcepts.TankSwap)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(WarConcepts.TankSwap, true, "Cast interrupted");
                });
            return;
        }

        if (player.Level >= 12 && context.ActionService.IsActionReady(RoleActions.LowBlow.ActionId))
        {
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.LowBlow.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return;
                }
            }
            scheduler.PushOgcd(AresAbilities.LowBlow, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RoleActions.LowBlow.Name;
                    context.Debug.MitigationState = "Stunned (interrupt)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.LowBlow.ActionId, RoleActions.LowBlow.Name)
                        .AsDefensive()
                        .Target(targetName)
                        .Reason($"Low Blow used to stun {targetName}'s cast.", "Low Blow stuns as interrupt backup.")
                        .Factors("Interject on CD", "Low Blow available")
                        .Alternatives("Wait for Interject")
                        .Tip("Low Blow is a reliable interrupt backup.")
                        .Concept(WarConcepts.TankSwap)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(WarConcepts.TankSwap, true, "Stun interrupt fallback");
                });
        }
    }

    private void TryPushTimelineAwareMitigation(IAresContext context, RotationScheduler scheduler, float hpPercent)
    {
        var nextTB = context.TimelineService?.NextTankBuster;
        if (nextTB?.IsSoon != true || !nextTB.Value.IsHighConfidence) return;
        var secondsUntil = nextTB.Value.SecondsUntil;
        if (secondsUntil < 1.5f || secondsUntil > 4.0f) return;

        var player = context.Player;
        var level = player.Level;
        if (context.HasHolmgang) return;

        // Priority 1: Bloodwhetting
        if (context.Configuration.Tank.EnableBloodWhetting &&
            level >= WARActions.RawIntuition.MinLevel && !context.HasBloodwhetting)
        {
            var action = WARActions.GetBloodwhettingAction(level);
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                var sec = secondsUntil;
                scheduler.PushOgcd(AresAbilities.RawIntuition, player.GameObjectId, priority: 2,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = action.Name;
                        context.Debug.MitigationState = $"Proactive Bloodwhetting (TB in {sec:F1}s)";
                    });
                return;
            }
        }

        // Priority 2: Rampart
        if (level >= RoleActions.Rampart.MinLevel && !context.HasActiveMitigation && !context.StatusHelper.HasRampart(player))
        {
            if (context.ActionService.IsActionReady(RoleActions.Rampart.ActionId))
            {
                var sec = secondsUntil;
                scheduler.PushOgcd(AresAbilities.Rampart, player.GameObjectId, priority: 2,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = RoleActions.Rampart.Name;
                        context.Debug.MitigationState = $"Proactive Rampart (TB in {sec:F1}s)";
                    });
                return;
            }
        }

        // Priority 3: Vengeance
        if (context.Configuration.Tank.EnableVengeance && level >= WARActions.Vengeance.MinLevel && !context.HasVengeance)
        {
            var action = WARActions.GetVengeanceAction(level);
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                var sec = secondsUntil;
                scheduler.PushOgcd(AresAbilities.Vengeance, player.GameObjectId, priority: 2,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = action.Name;
                        context.Debug.MitigationState = $"Proactive Vengeance (TB in {sec:F1}s)";
                    });
            }
        }
    }

    private void TryPushHolmgang(IAresContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableHolmgang) return;
        var player = context.Player;
        if (player.Level < WARActions.Holmgang.MinLevel) return;
        if (hpPercent > 0.15f) return;
        if (context.HasHolmgang) return;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            6f,
            player);
        if (target == null) return;
        if (!context.ActionService.IsActionReady(WARActions.Holmgang.ActionId)) return;

        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableInvulnerabilityCoordination &&
            partyCoord?.WasInvulnerabilityUsedRecently(tankConfig.InvulnerabilityStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Holmgang delayed (remote invuln)";
            return;
        }

        var targetName = target.Name?.TextValue;
        var hp = hpPercent;
        scheduler.PushOgcd(AresAbilities.Holmgang, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.Holmgang.Name;
                context.Debug.MitigationState = $"Emergency invuln ({hp:P0} HP)";
                partyCoord?.OnCooldownUsed(WARActions.Holmgang.ActionId, 240_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.Holmgang.ActionId, WARActions.Holmgang.Name)
                    .AsInvuln(hp)
                    .Reason("Emergency Holmgang - HP critically low.", "Holmgang prevents your HP from dropping below 1 for 10s.")
                    .Factors($"HP critically low ({hp:P0})", $"Target: {targetName}")
                    .Alternatives("Trust healers", "Use other cooldowns")
                    .Tip("Holmgang is your emergency button.")
                    .Concept("war_holmgang")
                    .Record();
                context.TrainingService?.RecordConceptApplication("war_holmgang", true, "Emergency invuln activation");
            });
    }

    private void TryPushMajorCooldown(IAresContext context, RotationScheduler scheduler, float hpPercent, float damageRate)
    {
        if (!context.Configuration.Tank.EnableVengeance) return;
        var player = context.Player;
        var level = player.Level;
        if (level < WARActions.Vengeance.MinLevel) return;
        if (!context.TankCooldownService.ShouldUseMajorCooldown(hpPercent, damageRate)) return;
        if (context.HasHolmgang) return;
        if (context.HasVengeance) return;

        var action = WARActions.GetVengeanceAction(level);
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Vengeance delayed (remote tank mit)";
            return;
        }
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        var hp = hpPercent;
        var dr = damageRate;
        scheduler.PushOgcd(AresAbilities.Vengeance, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.MitigationState = $"Major CD ({hp:P0} HP)";
                partyCoord?.OnCooldownUsed(action.ActionId, 120_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMitigation(hp)
                    .Reason($"{action.Name} at {hp:P0} HP.", "30% damage reduction + counter-attack damage.")
                    .Factors($"HP at {hp:P0}", $"Damage rate: {dr:F1}/s")
                    .Alternatives("Rampart", "Wait for tankbuster")
                    .Tip("Vengeance for sustained heavy damage.")
                    .Concept("war_vengeance")
                    .Record();
                context.TrainingService?.RecordConceptApplication("war_vengeance", true, "Major cooldown usage");
            });
    }

    private void TryPushRampart(IAresContext context, RotationScheduler scheduler, float hpPercent, float damageRate)
    {
        if (!context.TankCooldownService.ShouldUseMitigation(hpPercent, damageRate, context.HasActiveMitigation)) return;
        if (context.HasHolmgang) return;
        if (!context.Configuration.Tank.UseRampartOnCooldown && context.HasActiveMitigation) return;

        var hp = hpPercent;
        var dr = damageRate;
        RoleActionPushers.TryPushRampart(
            context, scheduler, AresAbilities.Rampart,
            priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Rampart.Name;
                context.Debug.MitigationState = $"Rampart ({hp:P0} HP)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Rampart.ActionId, RoleActions.Rampart.Name)
                    .AsMitigation(hp)
                    .Reason($"Rampart at {hp:P0} HP.", "20% mitigation for 20s.")
                    .Factors($"HP at {hp:P0}", $"Damage rate: {dr:F1}/s")
                    .Alternatives("Vengeance", "Bloodwhetting")
                    .Tip("Use Rampart frequently — 90s CD.")
                    .Concept(WarConcepts.MitigationStacking)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.MitigationStacking, true, "Rampart mitigation");
            });
    }

    private void TryPushBloodwhetting(IAresContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableBloodWhetting) return;
        var player = context.Player;
        var level = player.Level;
        if (level < WARActions.RawIntuition.MinLevel) return;
        if (hpPercent > context.Configuration.Tank.MitigationThreshold) return;
        if (context.HasBloodwhetting) return;
        if (context.HasHolmgang) return;

        var action = WARActions.GetBloodwhettingAction(level);
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Bloodwhetting delayed (remote tank mit)";
            return;
        }
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        var hp = hpPercent;
        scheduler.PushOgcd(AresAbilities.RawIntuition, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.MitigationState = $"Bloodwhetting ({hp:P0} HP)";
                partyCoord?.OnCooldownUsed(action.ActionId, 25_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMitigation(hp)
                    .Reason("Bloodwhetting for damage reduction + self-healing.", "10% mit, barrier, heals 400 potency per weaponskill.")
                    .Factors($"HP at {hp:P0}", "Short cooldown available")
                    .Alternatives("Save for bigger hit")
                    .Tip("Use Bloodwhetting frequently.")
                    .Concept("war_bloodwhetting")
                    .Record();
                context.TrainingService?.RecordConceptApplication("war_bloodwhetting", true, "Short cooldown with healing");
            });
    }

    private void TryPushThrillOfBattle(IAresContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableThrillOfBattle) return;
        var player = context.Player;
        if (player.Level < WARActions.ThrillOfBattle.MinLevel) return;
        if (hpPercent > 0.70f) return;
        if (context.HasHolmgang) return;
        if (context.StatusHelper.HasThrillOfBattle(player)) return;
        if (!context.ActionService.IsActionReady(WARActions.ThrillOfBattle.ActionId)) return;

        var hp = hpPercent;
        scheduler.PushOgcd(AresAbilities.ThrillOfBattle, player.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.ThrillOfBattle.Name;
                context.Debug.MitigationState = $"Thrill ({hp:P0} HP)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.ThrillOfBattle.ActionId, WARActions.ThrillOfBattle.Name)
                    .AsMitigation(hp)
                    .Reason($"Thrill of Battle at {hp:P0} HP.", "20% max HP boost + heal.")
                    .Factors($"HP at {hp:P0}")
                    .Alternatives("Rampart", "Wait for healer")
                    .Tip("Inflates HP pool temporarily.")
                    .Concept(WarConcepts.ThrillOfBattle)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.ThrillOfBattle, true, "HP boost");
            });
    }

    private void TryPushEquilibrium(IAresContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableEquilibrium) return;
        var player = context.Player;
        if (player.Level < WARActions.Equilibrium.MinLevel) return;
        if (hpPercent > 0.50f) return;
        if (!context.ActionService.IsActionReady(WARActions.Equilibrium.ActionId)) return;

        var hp = hpPercent;
        scheduler.PushOgcd(AresAbilities.Equilibrium, player.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.Equilibrium.Name;
                context.Debug.MitigationState = $"Equilibrium ({hp:P0} HP)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.Equilibrium.ActionId, WARActions.Equilibrium.Name)
                    .AsMitigation(hp)
                    .Reason($"Equilibrium at {hp:P0} HP.", "1200 potency heal + regen.")
                    .Factors($"HP at {hp:P0}")
                    .Alternatives("Wait for healer", "Bloodwhetting")
                    .Tip("Equilibrium is a free heal.")
                    .Concept(WarConcepts.Equilibrium)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.Equilibrium, true, "Self-heal");
            });
    }

    private void TryPushReprisal(IAresContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < RoleActions.Reprisal.MinLevel) return;

        var target = context.CurrentTarget;
        if (target == null) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasActionUsedByOther(RoleActions.Reprisal.ActionId, 10f) == true)
        {
            context.Debug.MitigationState = "Reprisal skipped (remote Reprisal up)";
            return;
        }

        var (avgHp, _, injuredCount) = context.PartyHealthMetrics;
        if (injuredCount < 3 && avgHp > 0.85f) return;

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        if (!context.ActionService.IsActionReady(RoleActions.Reprisal.ActionId)) return;

        scheduler.PushOgcd(AresAbilities.Reprisal, target.EntityId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Reprisal.Name;
                context.Debug.MitigationState = $"Reprisal ({enemyCount} enemies)";
                partyCoord?.OnCooldownUsed(RoleActions.Reprisal.ActionId, 60_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Reprisal.ActionId, RoleActions.Reprisal.Name)
                    .AsPartyMit()
                    .Reason($"Reprisal - 10% damage reduction on {enemyCount} enemies.", "Party mitigation for multi-target pulls.")
                    .Factors($"{enemyCount} enemies", "Reprisal available")
                    .Alternatives("Save for raidwide")
                    .Tip("Use Reprisal frequently in pulls.")
                    .Concept(WarConcepts.PartyProtection)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.PartyProtection, true, "Party mitigation");
            });
    }

    private void TryPushShakeItOff(IAresContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableShakeItOff) return;
        var player = context.Player;
        if (player.Level < WARActions.ShakeItOff.MinLevel) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Shake It Off skipped (remote mit)";
            return;
        }

        var (avgHp, _, injuredCount) = context.PartyHealthMetrics;
        if (injuredCount < 3 && avgHp > 0.85f) return;
        if (!context.ActionService.IsActionReady(WARActions.ShakeItOff.ActionId)) return;

        var avg = avgHp;
        var injured = injuredCount;
        scheduler.PushOgcd(AresAbilities.ShakeItOff, player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.ShakeItOff.Name;
                context.Debug.MitigationState = $"Shake It Off ({injured} injured)";
                partyCoord?.OnCooldownUsed(WARActions.ShakeItOff.ActionId, 90_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.ShakeItOff.ActionId, WARActions.ShakeItOff.Name)
                    .AsPartyMit()
                    .Reason($"Shake It Off party shield ({injured} injured).", "Party barrier + self cleanse.")
                    .Factors($"{injured} injured", $"Avg HP: {avg:P0}")
                    .Alternatives("Save for raidwide")
                    .Tip("Use before raidwides.")
                    .Concept("war_shake_it_off")
                    .Record();
                context.TrainingService?.RecordConceptApplication("war_shake_it_off", true, "Party shield");
            });
    }

    private void TryPushNascentFlash(IAresContext context, RotationScheduler scheduler, float myHpPercent)
    {
        if (!context.Configuration.Tank.EnableNascentFlash) return;
        var player = context.Player;
        if (player.Level < WARActions.NascentFlash.MinLevel) return;
        if (myHpPercent < 0.60f) return;

        var flashTarget = context.PartyHelper.FindNascentFlashTarget(player, 0.60f);
        if (flashTarget == null) return;

        var dx = player.Position.X - flashTarget.Position.X;
        var dy = player.Position.Y - flashTarget.Position.Y;
        var dz = player.Position.Z - flashTarget.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (distance > 30f) return;
        if (!context.ActionService.IsActionReady(WARActions.NascentFlash.ActionId)) return;

        var targetName = flashTarget.Name?.TextValue;
        var targetHp = context.PartyHelper.GetHpPercent(flashTarget);
        scheduler.PushOgcd(AresAbilities.NascentFlash, flashTarget.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.NascentFlash.Name;
                context.Debug.MitigationState = $"Nascent Flash ({targetHp:P0} HP ally)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.NascentFlash.ActionId, WARActions.NascentFlash.Name)
                    .AsPartyMit()
                    .Target(targetName)
                    .Reason($"Nascent Flash on {targetName} ({targetHp:P0} HP).", "Ally mitigation + heal-on-hit.")
                    .Factors($"Ally at {targetHp:P0} HP")
                    .Alternatives("Shake It Off")
                    .Tip("Nascent Flash for spike damage on allies.")
                    .Concept(WarConcepts.NascentFlash)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.NascentFlash, true, "Ally protection");
            });
    }
}
