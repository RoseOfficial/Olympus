using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Services.Party;
using Olympus.Services.Training;
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

        // Clemency is a GCD heal — check it before the oGCD gate
        if (context.CanExecuteGcd && TryClemency(context))
            return true;

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

        // Humanize: wait a short time into the cast before interrupting (0.3–0.7s, varies per enemy/cast)
        var delaySeed = (int)(target.EntityId * 2654435761u ^ (uint)(target.TotalCastTime * 1000f));
        var interruptDelay = 0.3f + ((delaySeed & 0xFFFF) / 65535f) * 0.4f;
        if (target.CurrentCastTime < interruptDelay)
            return false;

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
        if (context.Configuration.Tank.EnableInterject &&
            context.ActionService.IsActionReady(RoleActions.Interject.ActionId))
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
                    .AsInterrupt()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        $"Interrupted {target.Name?.TextValue}'s cast.",
                        "Interject silences an enemy cast. Always interrupt dangerous casts - uninterrupted abilities can cause wipes.")
                    .Factors("Enemy casting an interruptible ability", "Interject available", $"Target: {target.Name?.TextValue}")
                    .Alternatives("Low Blow (stun backup if Interject on CD)", "Ignore interrupt (high risk in most content)")
                    .Tip("Interject is a 30s CD. Prioritize interrupting dangerous casts over damage actions - the DPS loss is always worth preventing a wipe.")
                    .Concept(PldConcepts.TankSwap)
                    .Record();

                context.TrainingService?.RecordConceptApplication(PldConcepts.TankSwap, wasSuccessful: true);

                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        // Try Low Blow as backup (stun can interrupt some casts)
        if (context.Configuration.Tank.EnableLowBlow &&
            level >= 12 && context.ActionService.IsActionReady(RoleActions.LowBlow.ActionId))
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
                    .AsInterrupt()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        $"Low Blow used to stun {target.Name?.TextValue}'s cast (Interject unavailable).",
                        "Low Blow stuns enemies. It can interrupt casts as a backup when Interject is on cooldown. Stun is less reliable than silence but still effective.")
                    .Factors("Enemy casting interruptible ability", "Interject on cooldown", "Low Blow available", $"Target: {target.Name?.TextValue}")
                    .Alternatives("Wait for Interject (may be too slow)", "Use Interject if it comes off CD in time")
                    .Tip("Keep both Interject and Low Blow available for back-to-back interrupt windows. Low Blow's 25s CD makes it a reliable backup.")
                    .Concept(PldConcepts.TankSwap)
                    .Record();

                context.TrainingService?.RecordConceptApplication(PldConcepts.TankSwap, wasSuccessful: true);

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
        if (context.Configuration.Tank.EnableSheltron &&
            level >= PLDActions.Sheltron.MinLevel &&
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

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(sheltronAction.ActionId, sheltronAction.Name)
                        .AsTankResource(context.OathGauge)
                        .Reason(
                            $"Proactive {sheltronAction.Name} before predicted tankbuster in {secondsUntil:F1}s.",
                            $"{sheltronAction.Name} costs 50 Oath Gauge and provides a powerful short defensive. Pre-stacking it 1.5-4s before a tankbuster maximizes its coverage.")
                        .Factors($"Tankbuster predicted in {secondsUntil:F1}s (high confidence)", $"Oath Gauge: {context.OathGauge}", "No Sheltron already active")
                        .Alternatives("Rampart (longer cooldown, more reduction)", "Wait until hit (reactive is less safe)")
                        .Tip("Pre-stack Sheltron just before tankbusters. It's gauge-based so there's no cooldown waste - and it should be active when the hit lands.")
                        .Concept(PldConcepts.MitigationStacking)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(PldConcepts.MitigationStacking, wasSuccessful: true);

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
                        .Reason(
                            $"Proactive Rampart before predicted tankbuster in {secondsUntil:F1}s.",
                            "Rampart reduces damage taken by 20% for 20 seconds. Pre-stacking it 1.5-4s before a tankbuster ensures the 20% reduction is active for the hit.")
                        .Factors($"Tankbuster predicted in {secondsUntil:F1}s (high confidence)", "No active mitigation buffs", "Rampart available")
                        .Alternatives("Sheltron (costs gauge, shorter but useful)", "Sentinel (stronger but save for bigger hits)")
                        .Tip("Rampart is a cornerstone mitigation tool. Use it proactively when the timeline predicts an incoming tankbuster.")
                        .Concept(PldConcepts.MitigationStacking)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(PldConcepts.MitigationStacking, wasSuccessful: true);

                    return true;
                }
            }
        }

        // Priority 3: Sentinel/Guardian (major CD for big hits)
        if (context.Configuration.Tank.EnableSentinel &&
            level >= PLDActions.Sentinel.MinLevel &&
            !context.StatusHelper.HasSentinel(player))
        {
            var sentinelAction = PLDActions.GetSentinelAction(level);
            if (context.ActionService.IsActionReady(sentinelAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(sentinelAction, player.GameObjectId))
                {
                    context.Debug.PlannedAction = sentinelAction.Name;
                    context.Debug.MitigationState = $"Proactive Sentinel ({reason})";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(sentinelAction.ActionId, sentinelAction.Name)
                        .AsMitigation(hpPercent)
                        .Reason(
                            $"Proactive {sentinelAction.Name} before predicted tankbuster in {secondsUntil:F1}s.",
                            $"{sentinelAction.Name} is your strongest regular mitigation. Pre-stacking it before a predicted tankbuster ensures maximum damage reduction.")
                        .Factors($"Tankbuster predicted in {secondsUntil:F1}s (high confidence)", "No existing Sentinel buff", $"{sentinelAction.Name} available")
                        .Alternatives("Rampart (weaker but shorter CD)", "Hallowed Ground (save for emergencies)")
                        .Tip($"Use {sentinelAction.Name} proactively for predictable big hits. Reactive use wastes precious reaction time and may cause deaths.")
                        .Concept(PldConcepts.InvulnTiming)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(PldConcepts.InvulnTiming, wasSuccessful: true);

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
        if (!context.Configuration.Tank.EnableHallowedGround) return false;

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
            TrainingHelper.Decision(context.TrainingService)
                .Action(PLDActions.HallowedGround.ActionId, PLDActions.HallowedGround.Name)
                .AsInvuln(hpPercent)
                .Reason(
                    $"Emergency at {hpPercent:P0} HP",
                    "Hallowed Ground provides 10 seconds of complete invulnerability. Used at critical HP to survive otherwise lethal damage.")
                .Factors($"HP at {hpPercent:P0} (below 15% threshold)", "No other tank invuln used recently", "Would die without invuln")
                .Alternatives("Sentinel (40% reduction, but may not be enough)", "Wait for healer (risky at this HP)")
                .Tip("Use Hallowed Ground when HP drops critically low. Unlike other tank invulns, it has no drawback - just prevents all damage for 10 seconds.")
                .Concept("pld_hallowed_ground")
                .Record();

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
        if (!context.Configuration.Tank.EnableSentinel) return false;

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
            TrainingHelper.Decision(context.TrainingService)
                .Action(sentinelAction.ActionId, sentinelAction.Name)
                .AsMitigation(hpPercent)
                .Target("Self")
                .Reason(
                    $"Major cooldown at {hpPercent:P0} HP",
                    $"{sentinelAction.Name} reduces damage taken by 30% for 15 seconds. Your strongest regular mitigation - use for tankbusters and heavy damage phases.")
                .Factors($"HP at {hpPercent:P0}", $"Damage rate: {damageRate:F0} DPS", "No Hallowed Ground active", "No existing Sentinel buff")
                .Alternatives("Rampart (20% reduction, shorter cooldown)", "Sheltron (gauge-based, shorter duration)", "Wait for healer")
                .Tip("Sentinel is your go-to for predictable big hits. Its 120s cooldown means you can use it almost every tankbuster in most fights.")
                .Concept("pld_sentinel")
                .Record();

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

        if (level < RoleActions.Rampart.MinLevel)
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
                    $"Rampart at {hpPercent:P0} HP to reduce incoming damage.",
                    "Rampart reduces damage taken by 20% for 20 seconds. Use it on cooldown when taking moderate to heavy damage as it's a reliable mitigation tool.")
                .Factors($"HP at {hpPercent:P0}", $"Damage rate: {damageRate:F0} DPS", "No active Rampart buff", "No Hallowed Ground active")
                .Alternatives("Sentinel (stronger, longer CD)", "Sheltron (gauge-based, shorter duration)")
                .Tip("Rampart is your most frequently available major mitigation (90s CD). Use it consistently to maintain uptime on damage reduction.")
                .Concept(PldConcepts.Sentinel)
                .Record();

            context.TrainingService?.RecordConceptApplication(PldConcepts.Sentinel, wasSuccessful: true);

            return true;
        }

        return false;
    }

    #endregion

    #region Short Cooldowns

    private bool TrySheltron(IThemisContext context, float hpPercent)
    {
        if (!context.Configuration.Tank.EnableSheltron) return false;

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
            TrainingHelper.Decision(context.TrainingService)
                .Action(sheltronAction.ActionId, sheltronAction.Name)
                .AsTankResource(context.OathGauge)
                .Reason(
                    $"Spent {50} Oath Gauge at {hpPercent:P0} HP",
                    $"{sheltronAction.Name} costs 50 Oath Gauge and provides a short but powerful defensive buff. Use liberally since gauge regenerates passively.")
                .Factors($"Oath Gauge: {context.OathGauge}", $"HP at {hpPercent:P0}", "No Sheltron already active", "Not under Hallowed Ground")
                .Alternatives("Save gauge (risk taking more damage)", "Wait for bigger hit (may overcap gauge)")
                .Tip("Oath Gauge regenerates from auto-attacks. Don't sit at 100 gauge - spend Sheltron frequently to avoid waste.")
                .Concept("pld_sheltron")
                .Record();

            // Mastery: Record successful gauge usage
            context.TrainingService?.RecordConceptApplication("pld_sheltron", true, "Used Oath Gauge effectively");

            return true;
        }

        return false;
    }

    private bool TryBulwark(IThemisContext context, float hpPercent, float damageRate)
    {
        if (!context.Configuration.Tank.EnableBulwark) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Bulwark.MinLevel)
            return false;

        // Bulwark is good for sustained damage, not tank busters
        // Use when taking moderate consistent damage
        if (damageRate < 300f && hpPercent > 0.80f)
            return false;

        // Don't stack with Hallowed Ground
        if (context.HasHallowedGround)
            return false;

        // Check if Bulwark is active
        if (context.StatusHelper.HasBulwark(player))
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.Bulwark.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.Bulwark, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.Bulwark.Name;
            context.Debug.MitigationState = "Bulwark (sustained damage)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(PLDActions.Bulwark.ActionId, PLDActions.Bulwark.Name)
                .AsMitigation(hpPercent)
                .Reason(
                    $"Bulwark at {hpPercent:P0} HP under sustained {damageRate:F0} DPS.",
                    "Bulwark provides 100% block rate for 10 seconds. Best used during sustained auto-attack phases rather than for tankbusters.")
                .Factors($"HP at {hpPercent:P0}", $"Damage rate: {damageRate:F0} DPS (sustained)", "No existing Bulwark buff", "No Hallowed Ground active")
                .Alternatives("Sheltron (more reliable as it provides a block chance buff)", "Rampart (flat % reduction, more predictable)")
                .Tip("Bulwark is unique in providing 100% block rate rather than flat reduction. It excels during trash pulls and sustained auto-attack phases.")
                .Concept(PldConcepts.Bulwark)
                .Record();

            context.TrainingService?.RecordConceptApplication(PldConcepts.Bulwark, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryArmsLength(IThemisContext context)
    {
        if (!context.Configuration.Tank.EnableArmsLength) return false;

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

    private bool TryReprisal(IThemisContext context)
    {
        if (!context.Configuration.Tank.EnableReprisal) return false;

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
                    $"Reprisal applied with {enemyCount} enemies nearby.",
                    "Reprisal reduces enemies' damage output by 10% for 10 seconds. Excellent for AoE damage phases and before raidwides.")
                .Factors($"Enemy count: {enemyCount}", "No other party mitigation recently active", "Reprisal available")
                .Alternatives("Divine Veil (party shield instead of debuff)", "Wait for raidwide (may miss window)")
                .Tip("Reprisal is a party mitigation tool, not just for yourself. Use it whenever multiple enemies are attacking the party.")
                .Concept(PldConcepts.PartyProtection)
                .Record();

            context.TrainingService?.RecordConceptApplication(PldConcepts.PartyProtection, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryDivineVeil(IThemisContext context)
    {
        if (!context.Configuration.Tank.EnableDivineVeil) return false;

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
        if (injuredCount < 3 && avgHp > 0.85f)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.DivineVeil.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.DivineVeil, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.DivineVeil.Name;
            context.Debug.MitigationState = $"Divine Veil ({injuredCount} injured)";
            partyCoord?.OnCooldownUsed(PLDActions.DivineVeil.ActionId, 90_000);

            // Training: Record party mitigation decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(PLDActions.DivineVeil.ActionId, PLDActions.DivineVeil.Name)
                .AsPartyMit()
                .Reason(
                    $"Protecting {injuredCount} injured party members",
                    "Divine Veil applies a barrier to all nearby party members when you receive a heal. Excellent for raidwide damage.")
                .Factors($"{injuredCount} party members injured", $"Average party HP: {avgHp:P0}", "No other party mitigation active", "Ready to be triggered by healer")
                .Alternatives("Reprisal (reduces enemy damage instead)", "Wait for healer cooldowns")
                .Tip("Divine Veil needs to be triggered by receiving a heal. Communicate with healers or use Clemency to self-trigger if needed.")
                .Concept("pld_divine_veil")
                .Record();

            // Mastery: Record successful party protection
            context.TrainingService?.RecordConceptApplication("pld_divine_veil", true, "Deployed party shield");

            return true;
        }

        return false;
    }

    private bool TryCover(IThemisContext context, float myHpPercent)
    {
        if (!context.Configuration.Tank.EnableCover) return false;

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

            TrainingHelper.Decision(context.TrainingService)
                .Action(PLDActions.Cover.ActionId, PLDActions.Cover.Name)
                .AsPartyMit()
                .Target(coverTarget.Name?.TextValue)
                .Reason(
                    $"Cover used to protect {coverTarget.Name?.TextValue} at {targetHp:P0} HP.",
                    "Cover makes you take all damage directed at a party member for 12 seconds. Use it to protect a critically low ally that you can't let die.")
                .Factors($"Target HP: {targetHp:P0} (below 40% threshold)", $"Your HP: {myHpPercent:P0} (above 60% threshold)", $"Target within 10y: {coverTarget.Name?.TextValue}")
                .Alternatives("Hallowed Ground (protect yourself instead)", "Let healer handle it (may not be fast enough)")
                .Tip("Cover is a powerful party protection tool but dangerous if you're low HP. Only use it when you're healthy enough to absorb the redirected damage.")
                .Concept(PldConcepts.Cover)
                .Record();

            context.TrainingService?.RecordConceptApplication(PldConcepts.Cover, wasSuccessful: true);

            return true;
        }

        return false;
    }

    #endregion

    #region Clemency (Emergency GCD Heal)

    private bool TryClemency(IThemisContext context)
    {
        if (!context.Configuration.Tank.EnableClemency)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Clemency.MinLevel)
            return false;

        // Check MP — Clemency costs 2000 MP
        if (player.CurrentMp < 2000)
            return false;

        var clemencyThreshold = context.Configuration.Tank.ClemencyThreshold;

        // Find the lowest HP party member (including self)
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

        // Only use Clemency if someone is below the configured threshold
        if (target == null || lowestHp >= clemencyThreshold)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.Clemency.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(PLDActions.Clemency, target.GameObjectId))
        {
            var targetName = target.Name?.TextValue ?? "Unknown";
            context.Debug.PlannedAction = PLDActions.Clemency.Name;
            context.Debug.MitigationState = $"Clemency ({targetName} at {lowestHp:P0} HP)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(PLDActions.Clemency.ActionId, PLDActions.Clemency.Name)
                .AsHealing(lowestHp)
                .Target(targetName)
                .Reason(
                    $"Emergency Clemency on {targetName} at {lowestHp:P0} HP.",
                    "Clemency is a GCD heal that costs DPS uptime. Only use it when a party member is critically low and healers cannot recover in time.")
                .Factors($"Target HP: {lowestHp:P0} (below {clemencyThreshold:P0} threshold)", $"MP: {player.CurrentMp}/10000", "GCD available for Clemency cast")
                .Alternatives("Trust healers to recover", "Use mitigation oGCDs instead", "Hallowed Ground if self is the target")
                .Tip("Clemency is a DPS loss — only use it in true emergencies. If you find yourself using it frequently, consider adjusting the HP threshold.")
                .Concept(PldConcepts.PartyProtection)
                .Record();

            context.TrainingService?.RecordConceptApplication(PldConcepts.PartyProtection, wasSuccessful: true);

            return true;
        }

        return false;
    }

    #endregion
}
