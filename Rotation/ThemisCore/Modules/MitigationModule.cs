using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThemisCore.Abilities;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Services.Party;
using Olympus.Services.Training;
using Olympus.Timeline;

namespace Olympus.Rotation.ThemisCore.Modules;

/// <summary>
/// Handles the Paladin defensive rotation (scheduler-driven).
/// Manages personal mitigation, party mitigation, and invulnerability.
/// </summary>
public sealed class MitigationModule : IThemisModule
{
    public int Priority => 10;
    public string Name => "Mitigation";

    public bool TryExecute(IThemisContext context, bool isMoving) => false;

    public void UpdateDebugState(IThemisContext context)
    {
        // Debug state updated during CollectCandidates
    }

    #region CollectCandidates (scheduler path)

    public void CollectCandidates(IThemisContext context, RotationScheduler scheduler, bool isMoving)
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

        // GCD-only: Clemency emergency heal
        TryPushClemency(context, scheduler);

        // oGCDs
        TryPushInterrupt(context, scheduler);

        var player = context.Player;
        var hpPercent = (float)player.CurrentHp / player.MaxHp;
        var damageRate = context.DamageIntakeService.GetDamageRate(player.EntityId);

        context.TankCooldownService.Update(hpPercent, damageRate);

        // Timeline-aware proactive mit (high priority if tank buster imminent)
        TryPushTimelineAwareMitigation(context, scheduler, hpPercent);

        TryPushHallowedGround(context, scheduler, hpPercent);
        TryPushMajorCooldown(context, scheduler, hpPercent, damageRate);
        TryPushRampart(context, scheduler, hpPercent, damageRate);
        TryPushSheltron(context, scheduler, hpPercent);
        TryPushBulwark(context, scheduler, hpPercent, damageRate);
        TryPushReprisal(context, scheduler);
        TryPushDivineVeil(context, scheduler);
        TryPushCover(context, scheduler, hpPercent);
    }

    #endregion

    #region Clemency (GCD heal)

    private void TryPushClemency(IThemisContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableClemency) return;

        var player = context.Player;
        if (player.Level < PLDActions.Clemency.MinLevel) return;
        if (player.CurrentMp < 2000) return;
        if (!context.ActionService.IsActionReady(PLDActions.Clemency.ActionId)) return;

        var clemencyThreshold = context.Configuration.Tank.ClemencyThreshold;

        IBattleChara? target = null;
        float lowestHp = 1f;
        foreach (var member in context.PartyHelper.GetAllPartyMembers(player))
        {
            var hp = context.PartyHelper.GetHpPercent(member);
            if (hp > 0 && hp < lowestHp)
            {
                lowestHp = hp;
                target = member;
            }
        }

        if (target == null || lowestHp >= clemencyThreshold) return;

        var targetName = target.Name?.TextValue ?? "Unknown";
        var hpCopy = lowestHp;
        var mpCopy = player.CurrentMp;

        scheduler.PushGcd(ThemisAbilities.Clemency, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.Clemency.Name;
                context.Debug.MitigationState = $"Clemency ({targetName} at {hpCopy:P0} HP)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.Clemency.ActionId, PLDActions.Clemency.Name)
                    .AsHealing(hpCopy)
                    .Target(targetName)
                    .Reason(
                        $"Emergency Clemency on {targetName} at {hpCopy:P0} HP.",
                        "Clemency is a GCD heal. Only use in true emergencies.")
                    .Factors($"Target HP: {hpCopy:P0} (below {clemencyThreshold:P0})", $"MP: {mpCopy}/10000")
                    .Alternatives("Trust healers", "Use mitigation oGCDs instead", "Hallowed Ground if self-targeted")
                    .Tip("Clemency is a DPS loss — only in emergencies.")
                    .Concept(PldConcepts.PartyProtection)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.PartyProtection, wasSuccessful: true);
            });
    }

    #endregion

    #region Interrupt

    private void TryPushInterrupt(IThemisContext context, RotationScheduler scheduler)
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

        if (coordConfig.EnableInterruptCoordination &&
            partyCoord?.IsInterruptTargetReservedByOther(targetId) == true)
        {
            context.Debug.MitigationState = "Interrupt reserved by other";
            return;
        }

        var remainingCastTime = (target.TotalCastTime - target.CurrentCastTime) * 1000f;
        var castTimeMs = (int)remainingCastTime;
        var targetName = target.Name?.TextValue;

        // Interject first
        if (context.Configuration.Tank.EnableInterject &&
            context.ActionService.IsActionReady(RoleActions.Interject.ActionId))
        {
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.Interject.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return;
                }
            }

            scheduler.PushOgcd(ThemisAbilities.Interject, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RoleActions.Interject.Name;
                    context.Debug.MitigationState = "Interrupted cast";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Interject.ActionId, RoleActions.Interject.Name)
                        .AsInterrupt()
                        .Target(targetName)
                        .Reason($"Interrupted {targetName}'s cast.", "Interject silences enemy casts.")
                        .Factors("Enemy casting interruptible", "Interject available")
                        .Alternatives("Low Blow (stun backup)")
                        .Tip("Interject is 30s CD. Prioritize interrupts over damage.")
                        .Concept(PldConcepts.TankSwap)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(PldConcepts.TankSwap, wasSuccessful: true);
                });
            return;
        }

        // Low Blow backup
        if (context.Configuration.Tank.EnableLowBlow &&
            player.Level >= 12 && context.ActionService.IsActionReady(RoleActions.LowBlow.ActionId))
        {
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.LowBlow.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return;
                }
            }

            scheduler.PushOgcd(ThemisAbilities.LowBlow, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RoleActions.LowBlow.Name;
                    context.Debug.MitigationState = "Stunned (interrupt)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.LowBlow.ActionId, RoleActions.LowBlow.Name)
                        .AsInterrupt()
                        .Target(targetName)
                        .Reason($"Low Blow used to stun {targetName}'s cast.", "Low Blow stuns enemies, interrupting casts as backup.")
                        .Factors("Enemy casting interruptible", "Interject on cooldown", "Low Blow available")
                        .Alternatives("Wait for Interject")
                        .Tip("Low Blow's 25s CD makes it a reliable backup.")
                        .Concept(PldConcepts.TankSwap)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(PldConcepts.TankSwap, wasSuccessful: true);
                });
        }
    }

    #endregion

    #region Timeline-Aware Mitigation

    private void TryPushTimelineAwareMitigation(IThemisContext context, RotationScheduler scheduler, float hpPercent)
    {
        var nextTB = context.TimelineService?.NextTankBuster;
        if (nextTB?.IsSoon != true || !nextTB.Value.IsHighConfidence) return;

        var secondsUntil = nextTB.Value.SecondsUntil;
        if (secondsUntil < 1.5f || secondsUntil > 4.0f) return;

        var player = context.Player;
        var level = player.Level;
        var reason = $"TB in {secondsUntil:F1}s";
        if (context.HasHallowedGround) return;

        // Priority 1: Holy Sheltron (short CD, gauge-based)
        if (context.Configuration.Tank.EnableSheltron &&
            level >= PLDActions.Sheltron.MinLevel &&
            context.OathGauge >= 50 &&
            !context.StatusHelper.HasSheltron(player))
        {
            var sheltronAction = PLDActions.GetSheltronAction(level);
            if (context.ActionService.IsActionReady(sheltronAction.ActionId))
            {
                var sec = secondsUntil;
                var gauge = context.OathGauge;
                scheduler.PushOgcd(ThemisAbilities.Sheltron, player.GameObjectId, priority: 2,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = sheltronAction.Name;
                        context.Debug.MitigationState = $"Proactive Sheltron ({reason})";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(sheltronAction.ActionId, sheltronAction.Name)
                            .AsTankResource(gauge)
                            .Reason(
                                $"Proactive {sheltronAction.Name} before predicted tankbuster in {sec:F1}s.",
                                $"{sheltronAction.Name} costs 50 Oath Gauge, powerful short defensive.")
                            .Factors($"Tankbuster in {sec:F1}s", $"Oath Gauge: {gauge}", "No Sheltron active")
                            .Alternatives("Rampart (longer CD)", "Wait (reactive)")
                            .Tip("Pre-stack Sheltron just before tankbusters.")
                            .Concept(PldConcepts.MitigationStacking)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(PldConcepts.MitigationStacking, wasSuccessful: true);
                    });
                return;
            }
        }

        // Priority 2: Rampart
        if (level >= RoleActions.Rampart.MinLevel &&
            !context.HasActiveMitigation &&
            !context.StatusHelper.HasRampart(player))
        {
            if (context.ActionService.IsActionReady(RoleActions.Rampart.ActionId))
            {
                var sec = secondsUntil;
                scheduler.PushOgcd(ThemisAbilities.Rampart, player.GameObjectId, priority: 2,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = RoleActions.Rampart.Name;
                        context.Debug.MitigationState = $"Proactive Rampart ({reason})";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(RoleActions.Rampart.ActionId, RoleActions.Rampart.Name)
                            .AsMitigation(hpPercent)
                            .Reason($"Proactive Rampart before predicted tankbuster in {sec:F1}s.", "Rampart reduces damage taken by 20% for 20s.")
                            .Factors($"Tankbuster in {sec:F1}s", "No active mitigation", "Rampart available")
                            .Alternatives("Sheltron", "Sentinel (save for bigger hits)")
                            .Tip("Rampart is a cornerstone mitigation tool.")
                            .Concept(PldConcepts.MitigationStacking)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(PldConcepts.MitigationStacking, wasSuccessful: true);
                    });
                return;
            }
        }

        // Priority 3: Sentinel/Guardian (major CD)
        if (context.Configuration.Tank.EnableSentinel &&
            level >= PLDActions.Sentinel.MinLevel &&
            !context.StatusHelper.HasSentinel(player))
        {
            var sentinelAction = PLDActions.GetSentinelAction(level);
            if (context.ActionService.IsActionReady(sentinelAction.ActionId))
            {
                var sec = secondsUntil;
                scheduler.PushOgcd(ThemisAbilities.Sentinel, player.GameObjectId, priority: 2,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = sentinelAction.Name;
                        context.Debug.MitigationState = $"Proactive Sentinel ({reason})";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(sentinelAction.ActionId, sentinelAction.Name)
                            .AsMitigation(hpPercent)
                            .Reason($"Proactive {sentinelAction.Name} before predicted tankbuster in {sec:F1}s.", "Strongest regular mitigation.")
                            .Factors($"Tankbuster in {sec:F1}s", "No existing Sentinel buff")
                            .Alternatives("Rampart (weaker, shorter CD)", "Hallowed Ground")
                            .Tip($"Use {sentinelAction.Name} proactively for predictable big hits.")
                            .Concept(PldConcepts.InvulnTiming)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(PldConcepts.InvulnTiming, wasSuccessful: true);
                    });
            }
        }
    }

    #endregion

    #region Emergency Mitigation (Hallowed Ground)

    private void TryPushHallowedGround(IThemisContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableHallowedGround) return;
        var player = context.Player;
        if (player.Level < PLDActions.HallowedGround.MinLevel) return;
        if (hpPercent > 0.15f) return;
        if (context.HasHallowedGround) return;
        if (!context.ActionService.IsActionReady(PLDActions.HallowedGround.ActionId)) return;

        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableInvulnerabilityCoordination &&
            partyCoord?.WasInvulnerabilityUsedRecently(tankConfig.InvulnerabilityStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Hallowed delayed (remote invuln)";
            return;
        }

        var hp = hpPercent;
        scheduler.PushOgcd(ThemisAbilities.HallowedGround, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.HallowedGround.Name;
                context.Debug.MitigationState = $"Emergency invuln ({hp:P0} HP)";
                partyCoord?.OnCooldownUsed(PLDActions.HallowedGround.ActionId, 420_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.HallowedGround.ActionId, PLDActions.HallowedGround.Name)
                    .AsInvuln(hp)
                    .Reason($"Emergency at {hp:P0} HP", "Hallowed Ground provides 10s of complete invulnerability.")
                    .Factors($"HP at {hp:P0} (below 15%)", "No other tank invuln used recently")
                    .Alternatives("Sentinel (may not be enough)", "Wait for healer (risky)")
                    .Tip("Use Hallowed Ground when HP drops critically low.")
                    .Concept("pld_hallowed_ground")
                    .Record();
                context.TrainingService?.RecordConceptApplication("pld_hallowed_ground", true, "Used at critical HP");
            });
    }

    #endregion

    #region Major Cooldowns

    private void TryPushMajorCooldown(IThemisContext context, RotationScheduler scheduler, float hpPercent, float damageRate)
    {
        if (!context.Configuration.Tank.EnableSentinel) return;
        var player = context.Player;
        var level = player.Level;
        var sentinelAction = PLDActions.GetSentinelAction(level);
        if (level < PLDActions.Sentinel.MinLevel) return;
        if (!context.TankCooldownService.ShouldUseMajorCooldown(hpPercent, damageRate)) return;
        if (context.HasHallowedGround) return;
        if (context.StatusHelper.HasSentinel(player)) return;

        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Sentinel delayed (remote tank mit)";
            return;
        }

        if (!context.ActionService.IsActionReady(sentinelAction.ActionId)) return;

        var hp = hpPercent;
        var dr = damageRate;
        scheduler.PushOgcd(ThemisAbilities.Sentinel, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = sentinelAction.Name;
                context.Debug.MitigationState = $"Major CD ({hp:P0} HP)";
                partyCoord?.OnCooldownUsed(sentinelAction.ActionId, 120_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(sentinelAction.ActionId, sentinelAction.Name)
                    .AsMitigation(hp)
                    .Target("Self")
                    .Reason($"Major cooldown at {hp:P0} HP", $"{sentinelAction.Name} strongest regular mitigation.")
                    .Factors($"HP at {hp:P0}", $"Damage rate: {dr:F0} DPS")
                    .Alternatives("Rampart", "Sheltron", "Wait for healer")
                    .Tip($"{sentinelAction.Name} for predictable big hits.")
                    .Concept("pld_sentinel")
                    .Record();
                context.TrainingService?.RecordConceptApplication("pld_sentinel", true, $"Used at {hp:P0} HP");
            });
    }

    private void TryPushRampart(IThemisContext context, RotationScheduler scheduler, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;
        if (level < RoleActions.Rampart.MinLevel) return;
        if (!context.TankCooldownService.ShouldUseMitigation(hpPercent, damageRate, context.HasActiveMitigation)) return;
        if (context.HasHallowedGround) return;
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
        var dr = damageRate;
        scheduler.PushOgcd(ThemisAbilities.Rampart, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Rampart.Name;
                context.Debug.MitigationState = $"Rampart ({hp:P0} HP)";
                partyCoord?.OnCooldownUsed(RoleActions.Rampart.ActionId, 90_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Rampart.ActionId, RoleActions.Rampart.Name)
                    .AsMitigation(hp)
                    .Reason($"Rampart at {hp:P0} HP.", "Rampart reduces damage taken by 20% for 20s.")
                    .Factors($"HP at {hp:P0}", $"Damage rate: {dr:F0} DPS")
                    .Alternatives("Sentinel", "Sheltron")
                    .Tip("Rampart is your most frequently available major mitigation (90s CD).")
                    .Concept(PldConcepts.Sentinel)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.Sentinel, wasSuccessful: true);
            });
    }

    #endregion

    #region Short Cooldowns

    private void TryPushSheltron(IThemisContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableSheltron) return;
        var player = context.Player;
        var level = player.Level;
        if (level < PLDActions.Sheltron.MinLevel) return;
        if (!context.TankCooldownService.ShouldUseShortCooldown(hpPercent, context.OathGauge, context.Configuration.Tank.SheltronMinGauge)) return;
        if (context.StatusHelper.HasSheltron(player)) return;
        if (context.HasHallowedGround) return;

        var sheltronAction = PLDActions.GetSheltronAction(level);
        if (!context.ActionService.IsActionReady(sheltronAction.ActionId)) return;

        var hp = hpPercent;
        var gauge = context.OathGauge;
        scheduler.PushOgcd(ThemisAbilities.Sheltron, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = sheltronAction.Name;
                context.Debug.MitigationState = $"Sheltron ({gauge} gauge)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(sheltronAction.ActionId, sheltronAction.Name)
                    .AsTankResource(gauge)
                    .Reason($"Spent 50 Oath Gauge at {hp:P0} HP", $"{sheltronAction.Name} powerful short defensive.")
                    .Factors($"Oath Gauge: {gauge}", $"HP at {hp:P0}")
                    .Alternatives("Save gauge", "Wait for bigger hit")
                    .Tip("Oath Gauge regenerates passively. Spend Sheltron frequently.")
                    .Concept("pld_sheltron")
                    .Record();
                context.TrainingService?.RecordConceptApplication("pld_sheltron", true, "Used Oath Gauge effectively");
            });
    }

    private void TryPushBulwark(IThemisContext context, RotationScheduler scheduler, float hpPercent, float damageRate)
    {
        if (!context.Configuration.Tank.EnableBulwark) return;
        var player = context.Player;
        var level = player.Level;
        if (level < PLDActions.Bulwark.MinLevel) return;
        if (damageRate < 300f && hpPercent > 0.80f) return;
        if (context.HasHallowedGround) return;
        if (context.StatusHelper.HasBulwark(player)) return;
        if (!context.ActionService.IsActionReady(PLDActions.Bulwark.ActionId)) return;

        var hp = hpPercent;
        var dr = damageRate;
        scheduler.PushOgcd(ThemisAbilities.Bulwark, player.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.Bulwark.Name;
                context.Debug.MitigationState = "Bulwark (sustained damage)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.Bulwark.ActionId, PLDActions.Bulwark.Name)
                    .AsMitigation(hp)
                    .Reason($"Bulwark at {hp:P0} HP under sustained {dr:F0} DPS.", "Bulwark 100% block rate for 10s.")
                    .Factors($"HP at {hp:P0}", $"Damage rate: {dr:F0} DPS")
                    .Alternatives("Sheltron", "Rampart")
                    .Tip("Bulwark excels during sustained auto-attack phases.")
                    .Concept(PldConcepts.Bulwark)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.Bulwark, wasSuccessful: true);
            });
    }

    #endregion

    #region Party Mitigation

    private void TryPushReprisal(IThemisContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableReprisal) return;
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

        var (avgHp, lowestHp, injuredCount) = context.PartyHealthMetrics;
        if (injuredCount < 3 && avgHp > 0.85f) return;

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        if (!context.ActionService.IsActionReady(RoleActions.Reprisal.ActionId)) return;

        scheduler.PushOgcd(ThemisAbilities.Reprisal, target.EntityId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Reprisal.Name;
                context.Debug.MitigationState = $"Reprisal ({enemyCount} enemies)";
                partyCoord?.OnCooldownUsed(RoleActions.Reprisal.ActionId, 60_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Reprisal.ActionId, RoleActions.Reprisal.Name)
                    .AsPartyMit()
                    .Reason($"Reprisal applied with {enemyCount} enemies nearby.", "Reprisal reduces enemy damage output by 10% for 10s.")
                    .Factors($"Enemy count: {enemyCount}", "Reprisal available")
                    .Alternatives("Divine Veil", "Wait for raidwide")
                    .Tip("Reprisal is a party mitigation tool.")
                    .Concept(PldConcepts.PartyProtection)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.PartyProtection, wasSuccessful: true);
            });
    }

    private void TryPushDivineVeil(IThemisContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableDivineVeil) return;
        var player = context.Player;
        if (player.Level < PLDActions.DivineVeil.MinLevel) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Divine Veil skipped (remote mit)";
            return;
        }

        var (avgHp, _, injuredCount) = context.PartyHealthMetrics;
        if (injuredCount < 3 && avgHp > 0.85f) return;
        if (!context.ActionService.IsActionReady(PLDActions.DivineVeil.ActionId)) return;

        var avg = avgHp;
        var injured = injuredCount;
        scheduler.PushOgcd(ThemisAbilities.DivineVeil, player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.DivineVeil.Name;
                context.Debug.MitigationState = $"Divine Veil ({injured} injured)";
                partyCoord?.OnCooldownUsed(PLDActions.DivineVeil.ActionId, 90_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.DivineVeil.ActionId, PLDActions.DivineVeil.Name)
                    .AsPartyMit()
                    .Reason($"Protecting {injured} injured party members", "Divine Veil barrier when healed.")
                    .Factors($"{injured} party members injured", $"Average party HP: {avg:P0}")
                    .Alternatives("Reprisal", "Wait for healer cooldowns")
                    .Tip("Divine Veil needs to be triggered by receiving a heal.")
                    .Concept("pld_divine_veil")
                    .Record();
                context.TrainingService?.RecordConceptApplication("pld_divine_veil", true, "Deployed party shield");
            });
    }

    private void TryPushCover(IThemisContext context, RotationScheduler scheduler, float myHpPercent)
    {
        if (!context.Configuration.Tank.EnableCover) return;
        var player = context.Player;
        if (player.Level < PLDActions.Cover.MinLevel) return;
        if (myHpPercent < 0.60f) return;

        var coverTarget = context.PartyHelper.FindCoverTarget(player, 0.40f);
        if (coverTarget == null) return;

        var dx = player.Position.X - coverTarget.Position.X;
        var dy = player.Position.Y - coverTarget.Position.Y;
        var dz = player.Position.Z - coverTarget.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (distance > 10f) return;
        if (!context.ActionService.IsActionReady(PLDActions.Cover.ActionId)) return;

        var targetName = coverTarget.Name?.TextValue;
        var targetHp = context.PartyHelper.GetHpPercent(coverTarget);
        var myHp = myHpPercent;
        scheduler.PushOgcd(ThemisAbilities.Cover, coverTarget.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.Cover.Name;
                context.Debug.MitigationState = $"Cover ({targetHp:P0} HP ally)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.Cover.ActionId, PLDActions.Cover.Name)
                    .AsPartyMit()
                    .Target(targetName)
                    .Reason($"Cover used to protect {targetName} at {targetHp:P0} HP.", "Cover redirects damage.")
                    .Factors($"Target HP: {targetHp:P0}", $"Your HP: {myHp:P0}", $"Target within 10y: {targetName}")
                    .Alternatives("Hallowed Ground (protect self)", "Let healer handle it")
                    .Tip("Cover is powerful but dangerous if you're low HP.")
                    .Concept(PldConcepts.Cover)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.Cover, wasSuccessful: true);
            });
    }

    #endregion
}
