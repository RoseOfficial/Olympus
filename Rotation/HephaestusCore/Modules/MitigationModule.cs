using System;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
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

    public bool TryExecute(IHephaestusContext context, bool isMoving) => false;

    public void UpdateDebugState(IHephaestusContext context)
    {
        // Debug state updated during TryExecute
    }

    public void CollectCandidates(IHephaestusContext context, RotationScheduler scheduler, bool isMoving)
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

        var player = context.Player;
        var hpPercent = (float)player.CurrentHp / player.MaxHp;
        var damageRate = context.DamageIntakeService.GetDamageRate(player.EntityId);

        context.TankCooldownService.Update(hpPercent, damageRate);

        if (context.HasSuperbolide)
        {
            context.Debug.MitigationState = "Superbolide active";
            return;
        }

        // Timeline-aware proactive pushes before normal ladder
        TryPushTimelineAwareMitigation(context, scheduler, hpPercent);

        // Normal priority ladder
        TryPushInterrupt(context, scheduler);
        TryPushSuperbolide(context, scheduler, hpPercent);
        TryPushHeartOfCorundum(context, scheduler, hpPercent, damageRate);
        TryPushNebula(context, scheduler, hpPercent, damageRate);
        TryPushRampart(context, scheduler, hpPercent, damageRate);
        TryPushCamouflage(context, scheduler, hpPercent);
        TryPushAurora(context, scheduler, hpPercent);
        TryPushHeartOfLight(context, scheduler);
        TryPushReprisal(context, scheduler);
        TryPushArmsLength(context, scheduler);
    }

    #region CollectCandidates helpers

    private static void TryPushTimelineAwareMitigation(IHephaestusContext context, RotationScheduler scheduler, float hpPercent)
    {
        var nextTB = context.TimelineService?.NextTankBuster;
        if (nextTB?.IsSoon != true || !nextTB.Value.IsHighConfidence)
            return;

        var secondsUntil = nextTB.Value.SecondsUntil;
        if (secondsUntil < 1.5f || secondsUntil > 4.0f)
            return;

        if (context.HasSuperbolide)
            return;

        var player = context.Player;
        var level = player.Level;
        var reason = $"TB in {secondsUntil:F1}s";

        // Priority 1: Heart of Corundum/Stone (short CD, push at timeline priority 1)
        if (level >= GNBActions.HeartOfStone.MinLevel && !context.HasHeartOfCorundum)
        {
            scheduler.PushOgcd(
                GnbAbilities.HeartOfCorundum,
                player.GameObjectId,
                priority: 1,
                onDispatched: _ =>
                {
                    var heartAction = GNBActions.GetHeartAction(level);
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
                });
        }

        // Priority 2: Rampart (no active mitigation)
        if (level >= RoleActions.Rampart.MinLevel && !context.HasActiveMitigation && !context.StatusHelper.HasRampart(player))
        {
            scheduler.PushOgcd(
                GnbAbilities.Rampart,
                player.GameObjectId,
                priority: 2,
                onDispatched: _ =>
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
                        .Concept("gnb_rampart")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_rampart", true, "Proactive Rampart timeline");
                });
        }

        // Priority 3: Nebula/Great Nebula (major CD for big hits)
        if (level >= GNBActions.Nebula.MinLevel && !context.HasNebula)
        {
            scheduler.PushOgcd(
                GnbAbilities.Nebula,
                player.GameObjectId,
                priority: 3,
                onDispatched: _ =>
                {
                    var nebulaAction = GNBActions.GetNebulaAction(level);
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
                });
        }
    }

    private static void TryPushInterrupt(IHephaestusContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.Interject.MinLevel && level < RoleActions.LowBlow.MinLevel)
            return;

        var target = context.CurrentTarget;
        if (target == null)
            return;

        if (!target.IsCasting || !target.IsCastInterruptible)
            return;

        // Humanize: wait a short time into the cast before interrupting
        var delaySeed = (int)(target.EntityId * 2654435761u ^ (uint)(target.TotalCastTime * 1000f));
        var interruptDelay = 0.3f + ((delaySeed & 0xFFFF) / 65535f) * 0.4f;
        if (target.CurrentCastTime < interruptDelay)
            return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;

        if (coordConfig.EnableInterruptCoordination &&
            partyCoord?.IsInterruptTargetReservedByOther(target.EntityId) == true)
        {
            context.Debug.MitigationState = "Interrupt reserved by other";
            return;
        }

        var remainingCastTime = (target.TotalCastTime - target.CurrentCastTime) * 1000f;
        var castTimeMs = (int)remainingCastTime;
        var targetId = target.EntityId;

        if (level >= RoleActions.Interject.MinLevel)
        {
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.Interject.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return;
                }
            }

            scheduler.PushOgcd(
                GnbAbilities.Interject,
                target.GameObjectId,
                priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RoleActions.Interject.Name;
                    context.Debug.MitigationState = "Interrupted cast";
                });
        }
        else if (level >= RoleActions.LowBlow.MinLevel)
        {
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.LowBlow.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.MitigationState = "Failed to reserve interrupt";
                    return;
                }
            }

            scheduler.PushOgcd(
                GnbAbilities.LowBlow,
                target.GameObjectId,
                priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RoleActions.LowBlow.Name;
                    context.Debug.MitigationState = "Stunned (interrupt)";
                });
        }
    }

    private static void TryPushSuperbolide(IHephaestusContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableSuperbolide)
            return;

        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Superbolide.MinLevel)
            return;

        if (hpPercent > 0.15f)
            return;

        if (context.HasSuperbolide)
            return;

        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableInvulnerabilityCoordination &&
            partyCoord?.WasInvulnerabilityUsedRecently(tankConfig.InvulnerabilityStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Superbolide delayed (remote invuln)";
            return;
        }

        scheduler.PushOgcd(
            GnbAbilities.Superbolide,
            player.GameObjectId,
            priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.Superbolide.Name;
                context.Debug.MitigationState = $"Emergency invuln ({hpPercent:P0} HP)";
                partyCoord?.OnCooldownUsed(GNBActions.Superbolide.ActionId, 360_000);

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
            });
    }

    private static void TryPushHeartOfCorundum(IHephaestusContext context, RotationScheduler scheduler, float hpPercent, float damageRate)
    {
        if (!context.Configuration.Tank.EnableHeartOfCorundum)
            return;

        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.HeartOfStone.MinLevel)
            return;

        if (context.HasHeartOfCorundum)
            return;

        if (context.HasSuperbolide)
            return;

        ulong heartTargetId = player.GameObjectId;
        string heartReason;

        if (hpPercent < 0.40f)
        {
            heartReason = "Emergency Heart";
        }
        else if (context.IsMainTank && hpPercent < 0.60f && damageRate > 0.02f)
        {
            heartReason = "Reactive Heart";
        }
        else if (context.IsMainTank && hpPercent < context.Configuration.Tank.HeartOfCorundumThreshold && damageRate > 0.01f)
        {
            heartReason = "Proactive Heart";
        }
        else
        {
            // Check party members
            var heartTarget = context.PartyHelper.FindHeartOfCorundumTarget(player, 0.60f);
            if (heartTarget == null || heartTarget.GameObjectId == player.GameObjectId)
                return;

            var dx = player.Position.X - heartTarget.Position.X;
            var dy = player.Position.Y - heartTarget.Position.Y;
            var dz = player.Position.Z - heartTarget.Position.Z;
            var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (distance > 30f)
                return;

            heartTargetId = heartTarget.GameObjectId;
            var targetHp = context.PartyHelper.GetHpPercent(heartTarget);
            heartReason = $"Heart on ally ({targetHp:P0} HP)";
        }

        var capturedTargetId = heartTargetId;
        var capturedReason = heartReason;
        scheduler.PushOgcd(
            GnbAbilities.HeartOfCorundum,
            heartTargetId,
            priority: 3,
            onDispatched: _ =>
            {
                var heartAction = GNBActions.GetHeartAction(level);
                context.Debug.PlannedAction = heartAction.Name;
                context.Debug.MitigationState = capturedReason;

                var isSelf = capturedTargetId == player.GameObjectId;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(heartAction.ActionId, heartAction.Name)
                    .AsMitigation(hpPercent)
                    .Target(isSelf ? "Self" : "Ally")
                    .Reason(
                        capturedReason,
                        $"{heartAction.Name} is GNB's signature short defensive (25s cooldown). " +
                        "Provides 15% damage reduction for 4s, plus Catharsis (heal when HP falls below 50%) and Clarity of Corundum (extended duration). " +
                        "Unlike TBN, there's no DPS cost - use liberally for yourself and allies.")
                    .Factors(capturedReason, "25s cooldown allows frequent use", "Can target party members for support")
                    .Alternatives("Save for bigger hit (but short CD means it will be back)", "Use on different target")
                    .Tip("Heart of Corundum is very forgiving - the short cooldown means you should use it frequently rather than saving it.")
                    .Concept("gnb_heart_of_corundum")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_heart_of_corundum", true, capturedReason);
            });
    }

    private static void TryPushNebula(IHephaestusContext context, RotationScheduler scheduler, float hpPercent, float damageRate)
    {
        if (!context.Configuration.Tank.EnableNebula)
            return;

        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Nebula.MinLevel)
            return;

        if (!context.TankCooldownService.ShouldUseMajorCooldown(hpPercent, damageRate))
            return;

        if (context.HasSuperbolide)
            return;

        if (context.HasNebula)
            return;

        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Nebula delayed (remote tank mit)";
            return;
        }

        scheduler.PushOgcd(
            GnbAbilities.Nebula,
            player.GameObjectId,
            priority: 4,
            onDispatched: _ =>
            {
                var nebulaAction = GNBActions.GetNebulaAction(level);
                context.Debug.PlannedAction = nebulaAction.Name;
                context.Debug.MitigationState = $"Major CD ({hpPercent:P0} HP)";
                partyCoord?.OnCooldownUsed(nebulaAction.ActionId, 120_000);

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
            });
    }

    private static void TryPushRampart(IHephaestusContext context, RotationScheduler scheduler, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.Rampart.MinLevel)
            return;

        if (!context.TankCooldownService.ShouldUseMitigation(hpPercent, damageRate, context.HasActiveMitigation))
            return;

        if (context.HasSuperbolide)
            return;

        if (context.StatusHelper.HasRampart(player))
            return;

        if (!context.Configuration.Tank.UseRampartOnCooldown && context.HasActiveMitigation)
            return;

        var partyCoord = context.PartyCoordinationService;
        var tankConfig = context.Configuration.Tank;
        if (tankConfig.EnableDefensiveCoordination &&
            partyCoord?.WasPersonalDefensiveUsedRecently(tankConfig.DefensiveStaggerWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Rampart delayed (remote tank mit)";
            return;
        }

        scheduler.PushOgcd(
            GnbAbilities.Rampart,
            player.GameObjectId,
            priority: 5,
            onDispatched: _ =>
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
                    .Concept("gnb_rampart")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_rampart", true, "Rampart defensive cooldown");
            });
    }

    private static void TryPushCamouflage(IHephaestusContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableCamouflage)
            return;

        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Camouflage.MinLevel)
            return;

        if (hpPercent > context.Configuration.Tank.MitigationThreshold)
            return;

        if (context.HasSuperbolide)
            return;

        if (context.HasCamouflage)
            return;

        scheduler.PushOgcd(
            GnbAbilities.Camouflage,
            player.GameObjectId,
            priority: 6,
            onDispatched: _ =>
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
            });
    }

    private static void TryPushAurora(IHephaestusContext context, RotationScheduler scheduler, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableAurora)
            return;

        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Aurora.MinLevel)
            return;

        if (hpPercent < 0.70f && !context.HasAurora)
        {
            scheduler.PushOgcd(
                GnbAbilities.Aurora,
                player.GameObjectId,
                priority: 7,
                onDispatched: _ =>
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
                });
        }
        else if (hpPercent >= 0.70f)
        {
            var auroraTarget = context.PartyHelper.FindAuroraTarget(player, 0.70f);
            if (auroraTarget != null && auroraTarget.GameObjectId != player.GameObjectId)
            {
                var dx = player.Position.X - auroraTarget.Position.X;
                var dy = player.Position.Y - auroraTarget.Position.Y;
                var dz = player.Position.Z - auroraTarget.Position.Z;
                var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (distance <= 30f)
                {
                    var capturedTarget = auroraTarget;
                    scheduler.PushOgcd(
                        GnbAbilities.Aurora,
                        auroraTarget.GameObjectId,
                        priority: 7,
                        onDispatched: _ =>
                        {
                            var targetHp = context.PartyHelper.GetHpPercent(capturedTarget);
                            context.Debug.PlannedAction = GNBActions.Aurora.Name;
                            context.Debug.MitigationState = $"Aurora ({targetHp:P0} HP ally)";

                            TrainingHelper.Decision(context.TrainingService)
                                .Action(GNBActions.Aurora.ActionId, GNBActions.Aurora.Name)
                                .AsMitigation(targetHp)
                                .Target(capturedTarget.Name?.TextValue ?? "Ally")
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
                        });
                }
            }
        }
    }

    private static void TryPushHeartOfLight(IHephaestusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableHeartOfLight)
            return;

        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.HeartOfLight.MinLevel)
            return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Heart of Light skipped (remote mit)";
            return;
        }

        var (avgHp, _, injuredCount) = context.PartyHealthMetrics;
        if (injuredCount < 3 && avgHp > 0.85f)
            return;

        scheduler.PushOgcd(
            GnbAbilities.HeartOfLight,
            player.GameObjectId,
            priority: 8,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.HeartOfLight.Name;
                context.Debug.MitigationState = $"Heart of Light ({injuredCount} injured)";
                partyCoord?.OnCooldownUsed(GNBActions.HeartOfLight.ActionId, 90_000);

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
            });
    }

    private static void TryPushReprisal(IHephaestusContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.Reprisal.MinLevel)
            return;

        var target = context.CurrentTarget;
        if (target == null)
            return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.MitigationState = "Reprisal skipped (remote mit)";
            return;
        }

        var (avgHp, _, injuredCount) = context.PartyHealthMetrics;
        if (injuredCount < 3 && avgHp > 0.85f)
            return;

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        var capturedTarget = target;

        scheduler.PushOgcd(
            GnbAbilities.Reprisal,
            target.EntityId,
            priority: 9,
            onDispatched: _ =>
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
                    .Factors($"{enemyCount} enemies in range", $"{injuredCount} party members injured", $"Party average HP: {avgHp:P0}", "Reprisal available", "60s cooldown - deploy during damage pressure")
                    .Alternatives("Save for raidwide (depends on content)", "Heart of Light (magic only)", "Rely on personal mitigation (loses party value)")
                    .Tip("Use Reprisal during dungeon pulls - it reduces damage from every enemy hitting you. In raids, coordinate with your co-tank for raidwide coverage.")
                    .Concept("gnb_heart_of_light")
                    .Record();

                context.TrainingService?.RecordConceptApplication("gnb_heart_of_light", true, "Party mitigation deployed");
            });
    }

    private static void TryPushArmsLength(IHephaestusContext context, RotationScheduler scheduler)
    {
        var level = context.Player.Level;
        if (level < RoleActions.ArmsLength.MinLevel)
            return;

        // Arm's Length is primarily used for knockback immunity.
        // Automatic usage requires mechanic detection — skipped for now.
    }

    #endregion
}
