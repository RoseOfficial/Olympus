using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Services.Party;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight defensive rotation (scheduler-driven).
/// </summary>
public sealed class MitigationModule : INyxModule
{
    public int Priority => 10;
    public string Name => "Mitigation";

    public bool TryExecute(INyxContext context, bool isMoving) => false;
    public void UpdateDebugState(INyxContext context) { }

    public void CollectCandidates(INyxContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.Configuration.Tank.EnableMitigation) { context.Debug.MitigationState = "Disabled"; return; }
        if (!context.InCombat) { context.Debug.MitigationState = "Not in combat"; return; }

        TryPushInterrupt(context, scheduler);

        var player = context.Player;
        var hpPercent = (float)player.CurrentHp / player.MaxHp;
        var damageRate = context.DamageIntakeService.GetDamageRate(player.EntityId);
        context.TankCooldownService.Update(hpPercent, damageRate);

        TryPushTimelineAwareMitigation(context, scheduler, hpPercent);
        TryPushLivingDead(context, scheduler, hpPercent);
        TryPushShadowWall(context, scheduler, hpPercent, damageRate);
        TryPushRampart(context, scheduler, hpPercent, damageRate);
        TryPushDarkMind(context, scheduler, hpPercent);
        TryPushTBN(context, scheduler, hpPercent);
        TryPushOblation(context, scheduler, hpPercent);
        TryPushDarkMissionary(context, scheduler);
        TryPushReprisal(context, scheduler);
    }

    private void TryPushInterrupt(INyxContext context, RotationScheduler scheduler)
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

        var castTimeMs = (int)((target.TotalCastTime - target.CurrentCastTime) * 1000f);
        var targetName = target.Name?.TextValue;

        if (context.ActionService.IsActionReady(RoleActions.Interject.ActionId))
        {
            if (coordConfig.EnableInterruptCoordination &&
                !(partyCoord?.ReserveInterruptTarget(targetId, RoleActions.Interject.ActionId, castTimeMs) ?? false))
            {
                context.Debug.MitigationState = "Failed to reserve interrupt";
                return;
            }
            scheduler.PushOgcd(NyxAbilities.Interject, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RoleActions.Interject.Name;
                    context.Debug.MitigationState = "Interrupted cast";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Interject.ActionId, RoleActions.Interject.Name)
                        .AsInterrupt().Target(targetName)
                        .Reason($"Interrupted {targetName}.", "Interject silences.")
                        .Factors("Interruptible cast")
                        .Alternatives("Low Blow")
                        .Tip("Always interrupt interruptible casts.")
                        .Concept(DrkConcepts.TankSwap).Record();
                    context.TrainingService?.RecordConceptApplication(DrkConcepts.TankSwap, true);
                });
            return;
        }

        if (player.Level >= 12 && context.ActionService.IsActionReady(RoleActions.LowBlow.ActionId))
        {
            if (coordConfig.EnableInterruptCoordination &&
                !(partyCoord?.ReserveInterruptTarget(targetId, RoleActions.LowBlow.ActionId, castTimeMs) ?? false))
            {
                context.Debug.MitigationState = "Failed to reserve interrupt";
                return;
            }
            scheduler.PushOgcd(NyxAbilities.LowBlow, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RoleActions.LowBlow.Name;
                    context.Debug.MitigationState = "Stunned (interrupt)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.LowBlow.ActionId, RoleActions.LowBlow.Name)
                        .AsInterrupt().Target(targetName)
                        .Reason($"Low Blow to stun {targetName}.", "Stun as interrupt backup.")
                        .Factors("Interject on CD")
                        .Alternatives("Wait for Interject")
                        .Tip("Low Blow is reliable backup.")
                        .Concept(DrkConcepts.TankSwap).Record();
                    context.TrainingService?.RecordConceptApplication(DrkConcepts.TankSwap, true);
                });
        }
    }

    private void TryPushTimelineAwareMitigation(INyxContext context, RotationScheduler scheduler, float hpPercent)
    {
        var nextTB = context.TimelineService?.NextTankBuster;
        if (nextTB?.IsSoon != true || !nextTB.Value.IsHighConfidence) return;
        var secondsUntil = nextTB.Value.SecondsUntil;
        if (secondsUntil < 1.5f || secondsUntil > 4.0f) return;

        var player = context.Player;
        var level = player.Level;
        if (context.HasLivingDead) return;

        // Priority 1: Rampart
        if (level >= RoleActions.Rampart.MinLevel && !context.HasActiveMitigation && !context.StatusHelper.HasRampart(player))
        {
            if (context.ActionService.IsActionReady(RoleActions.Rampart.ActionId))
            {
                var sec = secondsUntil;
                scheduler.PushOgcd(NyxAbilities.Rampart, player.GameObjectId, priority: 2,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = RoleActions.Rampart.Name;
                        context.Debug.MitigationState = $"Proactive Rampart (TB in {sec:F1}s)";
                    });
                return;
            }
        }

        // Priority 2: Shadow Wall / Shadowed Vigil
        if (context.Configuration.Tank.EnableShadowWall &&
            level >= DRKActions.ShadowWall.MinLevel && !context.HasShadowWall)
        {
            var action = level >= 92 ? DRKActions.ShadowedVigil : DRKActions.ShadowWall;
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                var sec = secondsUntil;
                scheduler.PushOgcd(NyxAbilities.ShadowWall, player.GameObjectId, priority: 2,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = action.Name;
                        context.Debug.MitigationState = $"Proactive Shadow Wall (TB in {sec:F1}s)";
                    });
            }
        }
    }

    private void TryPushLivingDead(INyxContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableLivingDead) return;
        var player = context.Player;
        if (player.Level < DRKActions.LivingDead.MinLevel) return;
        if (hpPercent > 0.15f) return;
        if (context.HasLivingDead) return;
        if (context.HasWalkingDead) return;
        if (!context.ActionService.IsActionReady(DRKActions.LivingDead.ActionId)) return;

        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableInvulnerabilityCoordination &&
            partyCoord?.WasInvulnerabilityUsedRecently(tankConfig.InvulnerabilityStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Living Dead delayed (remote invuln)";
            return;
        }

        var hp = hpPercent;
        scheduler.PushOgcd(NyxAbilities.LivingDead, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.LivingDead.Name;
                context.Debug.MitigationState = $"Emergency invuln ({hp:P0} HP)";
                partyCoord?.OnCooldownUsed(DRKActions.LivingDead.ActionId, 300_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DRKActions.LivingDead.ActionId, DRKActions.LivingDead.Name).AsInvuln(hp)
                    .Reason("Emergency Living Dead.", "10s of invulnerability, but Walking Dead after requires full heal.")
                    .Factors($"HP at {hp:P0}")
                    .Alternatives("Trust healers")
                    .Tip("Living Dead must be followed by full heal during Walking Dead.")
                    .Concept("drk_living_dead").Record();
                context.TrainingService?.RecordConceptApplication("drk_living_dead", true, "Emergency invuln");
            });
    }

    private void TryPushShadowWall(INyxContext context, RotationScheduler scheduler, float hpPercent, float damageRate)
    {
        if (!context.Configuration.Tank.EnableShadowWall) return;
        var player = context.Player;
        var level = player.Level;
        if (level < DRKActions.ShadowWall.MinLevel) return;
        if (!context.TankCooldownService.ShouldUseMajorCooldown(hpPercent, damageRate)) return;
        if (context.HasLivingDead) return;
        if (context.HasWalkingDead) return;
        if (context.HasShadowWall) return;

        var action = level >= 92 ? DRKActions.ShadowedVigil : DRKActions.ShadowWall;
        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Shadow Wall delayed (remote tank mit)";
            return;
        }
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        var hp = hpPercent;
        scheduler.PushOgcd(NyxAbilities.ShadowWall, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.MitigationState = $"Major CD ({hp:P0} HP)";
                partyCoord?.OnCooldownUsed(action.ActionId, 120_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name).AsMitigation(hp)
                    .Reason($"{action.Name} at {hp:P0} HP.", "30% damage reduction.")
                    .Factors($"HP at {hp:P0}")
                    .Alternatives("Rampart")
                    .Tip("Use Shadow Wall for sustained heavy damage.")
                    .Concept("drk_shadow_wall").Record();
                context.TrainingService?.RecordConceptApplication("drk_shadow_wall", true, "Major cooldown");
            });
    }

    private void TryPushRampart(INyxContext context, RotationScheduler scheduler, float hpPercent, float damageRate)
    {
        var player = context.Player;
        if (player.Level < RoleActions.Rampart.MinLevel) return;
        if (!context.TankCooldownService.ShouldUseMitigation(hpPercent, damageRate, context.HasActiveMitigation)) return;
        if (context.HasLivingDead) return;
        if (context.HasWalkingDead) return;
        if (context.StatusHelper.HasRampart(player)) return;
        if (!context.Configuration.Tank.UseRampartOnCooldown && context.HasActiveMitigation) return;

        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Rampart delayed (remote tank mit)";
            return;
        }
        if (!context.ActionService.IsActionReady(RoleActions.Rampart.ActionId)) return;

        var hp = hpPercent;
        scheduler.PushOgcd(NyxAbilities.Rampart, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Rampart.Name;
                context.Debug.MitigationState = $"Rampart ({hp:P0} HP)";
                partyCoord?.OnCooldownUsed(RoleActions.Rampart.ActionId, 90_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Rampart.ActionId, RoleActions.Rampart.Name).AsMitigation(hp)
                    .Reason($"Rampart at {hp:P0} HP.", "20% mitigation for 20s.")
                    .Factors($"HP at {hp:P0}")
                    .Alternatives("Shadow Wall")
                    .Tip("Use Rampart frequently.")
                    .Concept(DrkConcepts.MitigationStacking).Record();
                context.TrainingService?.RecordConceptApplication(DrkConcepts.MitigationStacking, true);
            });
    }

    private void TryPushDarkMind(INyxContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableDarkMind) return;
        var player = context.Player;
        if (player.Level < DRKActions.DarkMind.MinLevel) return;
        if (context.HasDarkMind) return;
        if (context.HasLivingDead) return;
        if (context.HasWalkingDead) return;
        // Use when taking magic damage — approximate via damage rate
        if (hpPercent > 0.70f) return;
        if (!context.ActionService.IsActionReady(DRKActions.DarkMind.ActionId)) return;

        scheduler.PushOgcd(NyxAbilities.DarkMind, player.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.DarkMind.Name;
                context.Debug.MitigationState = $"Dark Mind ({hpPercent:P0} HP)";
            });
    }

    private void TryPushTBN(INyxContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableTheBlackestNight) return;
        var player = context.Player;
        if (player.Level < DRKActions.TheBlackestNight.MinLevel) return;
        if (context.HasTheBlackestNight) return;
        if (context.HasWalkingDead) return;
        if (!context.HasEnoughMpForTbn) return;
        // Use proactively before predictable damage or at moderate HP
        if (hpPercent > context.Configuration.Tank.TBNThreshold) return;
        if (!context.ActionService.IsActionReady(DRKActions.TheBlackestNight.ActionId)) return;

        scheduler.PushOgcd(NyxAbilities.TheBlackestNight, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.TheBlackestNight.Name;
                context.Debug.MitigationState = $"TBN shield ({hpPercent:P0} HP)";
            });
    }

    private void TryPushOblation(INyxContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableOblation) return;
        var player = context.Player;
        if (player.Level < DRKActions.Oblation.MinLevel) return;
        if (context.HasOblation) return;
        if (context.HasWalkingDead) return;
        if (hpPercent > context.Configuration.Tank.MitigationThreshold) return;
        if (!context.ActionService.IsActionReady(DRKActions.Oblation.ActionId)) return;

        scheduler.PushOgcd(NyxAbilities.Oblation, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.Oblation.Name;
                context.Debug.MitigationState = $"Oblation ({hpPercent:P0} HP)";
            });
    }

    private void TryPushDarkMissionary(INyxContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableDarkMissionary) return;
        var player = context.Player;
        if (player.Level < DRKActions.DarkMissionary.MinLevel) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Dark Missionary skipped (remote mit)";
            return;
        }

        var (avgHp, _, injuredCount) = context.PartyHealthMetrics;
        if (injuredCount < 3 && avgHp > 0.85f) return;
        if (!context.ActionService.IsActionReady(DRKActions.DarkMissionary.ActionId)) return;

        var injured = injuredCount;
        scheduler.PushOgcd(NyxAbilities.DarkMissionary, player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.DarkMissionary.Name;
                context.Debug.MitigationState = $"Dark Missionary ({injured} injured)";
                partyCoord?.OnCooldownUsed(DRKActions.DarkMissionary.ActionId, 90_000);
            });
    }

    private void TryPushReprisal(INyxContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < RoleActions.Reprisal.MinLevel) return;
        var target = context.CurrentTarget;
        if (target == null) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Reprisal skipped (remote mit)";
            return;
        }

        var (avgHp, _, injuredCount) = context.PartyHealthMetrics;
        if (injuredCount < 3 && avgHp > 0.85f) return;

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        if (!context.ActionService.IsActionReady(RoleActions.Reprisal.ActionId)) return;

        scheduler.PushOgcd(NyxAbilities.Reprisal, target.EntityId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Reprisal.Name;
                context.Debug.MitigationState = $"Reprisal ({enemyCount} enemies)";
                partyCoord?.OnCooldownUsed(RoleActions.Reprisal.ActionId, 60_000);
            });
    }
}
