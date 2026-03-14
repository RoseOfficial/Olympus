using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Handles all healing logic for the Scholar rotation.
/// Includes GCD heals, oGCD heals, shields, and Aetherflow healing abilities.
/// </summary>
public sealed class HealingModule : IAthenaModule
{
    public int Priority => 10; // High priority for healing
    public string Name => "Healing";

    // Training explanation arrays
    private static readonly string[] _recitationAlternatives =
    {
        "Use Recitation with different follow-up",
        "Save for emergency (guaranteed crit heal)",
        "Hold for raidwide (Recitation + Indom)",
    };

    private static readonly string[] _excogitationAlternatives =
    {
        "Lustrate (immediate heal, same cost)",
        "Save Aetherflow for Indomitability (AoE)",
        "GCD heal (Adloquium for shield)",
    };

    private static readonly string[] _lustrateAlternatives =
    {
        "Excogitation (proactive, auto-triggers)",
        "Adloquium (GCD, adds shield)",
        "Wait for fairy abilities",
    };

    private static readonly string[] _indomitabilityAlternatives =
    {
        "Succor (GCD, adds shields)",
        "Whispering Dawn (fairy HoT)",
        "Fey Blessing (fairy burst)",
    };

    private static readonly string[] _sacredSoilAlternatives =
    {
        "Succor (GCD shield, no mitigation)",
        "Expedient (sprint + mitigation)",
        "Save Aetherflow for Indomitability",
    };

    private static readonly string[] _protractionAlternatives =
    {
        "Lustrate (direct heal)",
        "Excogitation (proactive)",
        "Adloquium (shield + heal)",
    };

    private static readonly string[] _emergencyTacticsAlternatives =
    {
        "Lustrate (uses Aetherflow)",
        "Wait for shield to break",
        "Use Physick (no shield component)",
    };

    private static readonly string[] _succorAlternatives =
    {
        "Indomitability (oGCD, no shield)",
        "Whispering Dawn (fairy HoT)",
        "Sacred Soil (mitigation + HoT)",
    };

    public bool TryExecute(AthenaContext context, bool isMoving)
    {
        // Clear frame-scoped coordination state to allow new reservations
        context.HealingCoordination.Clear();

        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing)
            return false;

        // oGCD heals first (free healing, no GCD cost)
        if (context.CanExecuteOgcd)
        {
            // Priority 1: Recitation + Excogitation combo
            if (TryRecitationCombo(context))
                return true;

            // Priority 2: Excogitation (proactive)
            if (TryExcogitation(context))
                return true;

            // Priority 3: Lustrate (emergency single-target)
            if (TryLustrate(context))
                return true;

            // Priority 4: Indomitability (AoE oGCD)
            if (TryIndomitability(context))
                return true;

            // Priority 5: Sacred Soil
            if (TrySacredSoil(context))
                return true;

            // Priority 6: Protraction
            if (TryProtraction(context))
                return true;

            // Priority 7: Emergency Tactics (before GCD heal)
            if (TryEmergencyTactics(context))
                return true;
        }

        // GCD heals
        if (context.CanExecuteGcd && !isMoving)
        {
            // Priority 8: AoE healing (Succor/Concitation)
            if (TryAoEHeal(context))
                return true;

            // Priority 9: Single-target healing (Adloquium/Physick)
            if (TrySingleTargetHeal(context))
                return true;
        }

        return false;
    }

    public void UpdateDebugState(AthenaContext context)
    {
        context.Debug.AetherflowStacks = context.AetherflowService.CurrentStacks;
    }

    #region oGCD Healing

    private bool TryRecitationCombo(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableRecitation)
            return false;

        if (player.Level < SCHActions.Recitation.MinLevel)
            return false;

        // Already have Recitation active - handled by other methods
        if (context.StatusHelper.HasRecitation(player))
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Recitation.ActionId))
            return false;

        // Check if we have a good target for the follow-up
        bool shouldUseRecitation = config.RecitationPriority switch
        {
            RecitationPriority.Excogitation => ShouldUseExcogitation(context),
            RecitationPriority.Indomitability => ShouldUseIndomitability(context),
            RecitationPriority.Adloquium => ShouldUseSingleTargetHeal(context),
            RecitationPriority.Succor => ShouldUseAoEHeal(context),
            _ => false
        };

        if (!shouldUseRecitation)
            return false;

        var action = SCHActions.Recitation;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Recitation";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var followUp = config.RecitationPriority switch
                {
                    RecitationPriority.Excogitation => "Excogitation",
                    RecitationPriority.Indomitability => "Indomitability",
                    RecitationPriority.Adloquium => "Adloquium",
                    RecitationPriority.Succor => "Succor",
                    _ => "Unknown"
                };

                var shortReason = $"Recitation for guaranteed crit {followUp}";

                var factors = new[]
                {
                    $"Next ability: {followUp} (configured priority)",
                    "Guarantees critical heal on next applicable spell",
                    "No Aetherflow cost when paired with Aetherflow abilities",
                    $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                };

                var alternatives = _recitationAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Recitation",
                    Category = "Healing",
                    TargetName = null,
                    ShortReason = shortReason,
                    DetailedReason = $"Recitation guarantees a critical heal on the next Adloquium, Succor, Indomitability, or Excogitation. Also removes Aetherflow cost. Planning to follow with {followUp} for maximum value.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Recitation is best used before Excogitation (crit Excog) or before raidwides with Indomitability. The free Aetherflow cost is a nice bonus!",
                    ConceptId = SchConcepts.RecitationUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryExcogitation(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableExcogitation)
            return false;

        if (player.Level < SCHActions.Excogitation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Excogitation.ActionId))
            return false;

        // Skip if no Aetherflow (unless Recitation is active)
        if (!context.StatusHelper.HasRecitation(player))
        {
            if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
                return false;
        }

        var target = context.PartyHelper.FindExcogitationTarget(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        // Timeline-aware: proactively use before tank busters
        var tankBusterImminent = TimelineHelper.IsTankBusterImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Use if HP is low OR tank buster is imminent
        if (hpPercent > config.ExcogitationThreshold && !tankBusterImminent)
            return false;

        var action = SCHActions.Excogitation;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            var hasRecitation = context.StatusHelper.HasRecitation(player);
            if (!hasRecitation)
                context.AetherflowService.ConsumeStack();

            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Excogitation";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var shortReason = tankBusterImminent
                    ? $"Excog on {targetName} before tankbuster!"
                    : $"Excog on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.ExcogitationThreshold:P0}",
                    tankBusterImminent ? "Tank buster imminent!" : "No incoming damage predicted",
                    hasRecitation ? "Recitation active (guaranteed crit, free)" : $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                    "Auto-triggers at 50% HP or lower",
                };

                var alternatives = _excogitationAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Excogitation",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Excogitation on {targetName} at {hpPercent:P0} HP. {(tankBusterImminent ? "Tank buster detected - proactive Excog provides safety net. " : "")}Excog triggers automatically when target drops below 50% HP, providing a 800 potency heal. {(hasRecitation ? "Recitation made this free and guaranteed critical!" : $"Cost 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining).")}",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Excogitation is SCH's best tank maintenance tool. Apply before damage for automatic healing. Pair with Recitation for massive crit heals!",
                    ConceptId = SchConcepts.ExcogitationUsage,
                    Priority = tankBusterImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryLustrate(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableLustrate)
            return false;

        if (player.Level < SCHActions.Lustrate.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Lustrate.ActionId))
            return false;

        // Respect Aetherflow reserve
        if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.LustrateThreshold)
            return false;

        var action = SCHActions.Lustrate;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.AetherflowService.ConsumeStack();

            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Lustrate";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var shortReason = hpPercent < 0.3f
                    ? $"Emergency Lustrate on {targetName}!"
                    : $"Lustrate on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.LustrateThreshold:P0}",
                    $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                    "600 potency instant heal",
                    "oGCD - can weave without clipping",
                };

                var alternatives = _lustrateAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Lustrate",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Lustrate on {targetName} at {hpPercent:P0} HP. Lustrate is SCH's emergency single-target oGCD heal at 600 potency. Used 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining). Lustrate is for reactive healing when someone is already low.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Lustrate is best for emergencies. For planned damage, Excogitation is usually better since it's proactive and higher potency (800). Save at least 1 Aetherflow for emergencies!",
                    ConceptId = SchConcepts.LustrateUsage,
                    Priority = hpPercent < 0.3f ? ExplanationPriority.Critical : ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryIndomitability(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableIndomitability)
            return false;

        if (player.Level < SCHActions.Indomitability.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Indomitability.ActionId))
            return false;

        // Skip if no Aetherflow (unless Recitation is active)
        if (!context.StatusHelper.HasRecitation(player))
        {
            if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
                return false;
        }

        if (!ShouldUseIndomitability(context))
            return false;

        var action = SCHActions.Indomitability;

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
        {
            context.Debug.IndomitabilityState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            var hasRecitation = context.StatusHelper.HasRecitation(player);
            if (!hasRecitation)
                context.AetherflowService.ConsumeStack();

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Indomitability";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

                var shortReason = $"Indomitability - {injuredCount} injured, avg HP {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    hasRecitation ? "Recitation active (guaranteed crit, free)" : $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                    "400 potency AoE heal",
                    "oGCD - can weave without clipping",
                };

                var alternatives = _indomitabilityAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Indomitability",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Indomitability to heal {injuredCount} party members at {avgHp:P0} average HP. 400 potency AoE heal, instant oGCD. {(hasRecitation ? "Recitation made this free and guaranteed critical!" : $"Cost 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining).")} Best used after raidwides when multiple party members are injured.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Indomitability is your primary AoE oGCD heal. Pair with Recitation for burst healing. Use after raidwides rather than before (shields go before, heals go after).",
                    ConceptId = SchConcepts.IndomitabilityUsage,
                    Priority = avgHp < 0.5f ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TrySacredSoil(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableSacredSoil)
            return false;

        if (player.Level < SCHActions.SacredSoil.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.SacredSoil.ActionId))
            return false;

        // Respect Aetherflow reserve
        if (context.AetherflowService.CurrentStacks <= config.AetherflowReserve)
            return false;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Timeline-aware: proactively place before raidwides
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Burst awareness: Deploy Sacred Soil proactively before burst windows
        // Mit + regen provides sustained healing during high-damage DPS phases
        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness &&
            coordConfig.PreferShieldsBeforeBurst &&
            partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            // Deploy Sacred Soil 3-8 seconds before burst (similar to raidwide logic)
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
            {
                burstImminent = true;
            }
        }

        // Use if party HP is low OR raidwide is imminent OR burst is imminent
        if (avgHp > config.SacredSoilThreshold && !raidwideImminent && !burstImminent)
            return false;

        // Count party members in range
        int membersInRange = 0;
        foreach (var member in context.PartyHelper.GetPartyMembers(player))
        {
            if (Vector3.DistanceSquared(player.Position, member.Position) <= SCHActions.SacredSoil.RadiusSquared)
                membersInRange++;
        }

        if (membersInRange < config.SacredSoilMinTargets)
            return false;

        // Check if another instance recently used a party mitigation (cooldown coordination)
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasPartyMitigationUsedRecently(coordConfig.CooldownOverlapWindowSeconds) == true)
        {
            context.Debug.PlanningState = "Sacred Soil skipped (remote mit)";
            return false;
        }

        // Check if another Olympus healer already has a ground effect in this area
        if (partyCoord?.WouldOverlapWithRemoteGroundEffect(
            player.Position,
            SCHActions.SacredSoil.ActionId,
            coordConfig.GroundEffectOverlapThreshold) == true)
        {
            context.Debug.PlanningState = "Sacred Soil skipped (area covered)";
            return false;
        }

        // Place at player's position (ground target at self)
        var action = SCHActions.SacredSoil;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.AetherflowService.ConsumeStack();
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Sacred Soil";
            partyCoord?.OnCooldownUsed(action.ActionId, 30_000);
            // Broadcast ground effect placement to other Olympus instances
            partyCoord?.OnGroundEffectPlaced(action.ActionId, player.Position);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                string trigger;
                if (raidwideImminent) trigger = "Raidwide imminent";
                else if (burstImminent) trigger = "DPS burst window imminent";
                else trigger = $"Party HP low ({avgHp:P0})";

                var shortReason = $"Sacred Soil - {trigger}";

                var factors = new[]
                {
                    trigger,
                    $"Party avg HP: {avgHp:P0}",
                    $"Members in range: {membersInRange}",
                    $"Aetherflow stacks: {context.AetherflowService.CurrentStacks}/3",
                    "10% damage reduction + HoT (at 78+)",
                };

                var alternatives = _sacredSoilAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Sacred Soil",
                    Category = "Defensive",
                    TargetName = "Ground",
                    ShortReason = shortReason,
                    DetailedReason = $"Sacred Soil placed for {membersInRange} party members. {trigger}. Sacred Soil provides 10% damage reduction and at level 78+ adds a healing-over-time effect (100 potency per tick). Cost 1 Aetherflow stack ({context.AetherflowService.CurrentStacks}/3 remaining). Best used proactively before damage hits.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Sacred Soil is one of SCH's best mitigation tools. At 78+, the HoT makes it extremely valuable. Place it before raidwides, not after!",
                    ConceptId = SchConcepts.SacredSoilUsage,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryProtraction(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableProtraction)
            return false;

        if (player.Level < SCHActions.Protraction.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Protraction.ActionId))
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.ProtractionThreshold)
            return false;

        var action = SCHActions.Protraction;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            // Protraction increases max HP and heals by 10% - estimate as a moderate heal
            var healAmount = 1000; // Rough estimate for 10% max HP heal
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Protraction";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var shortReason = $"Protraction on {targetName} at {hpPercent:P0}";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Threshold: {config.ProtractionThreshold:P0}",
                    "Increases max HP by 10%",
                    "Restores HP equal to the increase",
                    "10s duration, enhances healing received",
                };

                var alternatives = _protractionAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = "Protraction",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Protraction on {targetName} at {hpPercent:P0} HP. Protraction increases max HP by 10% and heals for the same amount. The 10s buff also increases healing received, making follow-up heals more effective. Free oGCD with no resource cost.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Protraction is a free oGCD that effectively heals and buffs healing received. Great before big damage on a single target!",
                    ConceptId = SchConcepts.EmergencyHealing,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryEmergencyTactics(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableEmergencyTactics)
            return false;

        if (player.Level < SCHActions.EmergencyTactics.MinLevel)
            return false;

        // Already have Emergency Tactics active
        if (context.StatusHelper.HasEmergencyTactics(player))
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.EmergencyTactics.ActionId))
            return false;

        // Use when we need raw healing, not shields
        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent > config.EmergencyTacticsThreshold)
            return false;

        // Check if target already has Galvanize (shield would be wasted)
        if (context.StatusHelper.HasGalvanize(target))
        {
            var action = SCHActions.EmergencyTactics;
            if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Emergency Tactics";

                // Training mode: capture explanation
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = target.Name?.TextValue ?? "Unknown";
                    var shortReason = $"Emergency Tactics - {targetName} already shielded";

                    var factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
                        $"Threshold: {config.EmergencyTacticsThreshold:P0}",
                        "Target has Galvanize (shield)",
                        "Converts next shield spell to pure heal",
                        "Prevents shield overwrite waste",
                    };

                    var alternatives = _emergencyTacticsAlternatives;

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.Now,
                        ActionId = action.ActionId,
                        ActionName = "Emergency Tactics",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Emergency Tactics before healing {targetName} at {hpPercent:P0}. Target already has Galvanize shield, so using Adloquium would overwrite it (wasting the shield). Emergency Tactics converts the shield portion to healing, getting full value from the spell.",
                        Factors = factors,
                        Alternatives = alternatives,
                        Tip = "Emergency Tactics prevents shield waste when the target already has a shield. It's also useful when you need raw healing instead of shields after a raidwide.",
                        ConceptId = SchConcepts.EmergencyTacticsUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }

                return true;
            }
        }

        return false;
    }

    #endregion

    #region GCD Healing

    private bool TryAoEHeal(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableSuccor)
            return false;

        if (!ShouldUseAoEHeal(context))
            return false;

        // Choose between Succor and Concitation (Seraphism upgrade)
        ActionDefinition action;
        if (context.FairyStateManager.IsSeraphOrSeraphismActive && player.Level >= SCHActions.Concitation.MinLevel)
        {
            action = SCHActions.Concitation;
        }
        else if (player.Level >= SCHActions.Succor.MinLevel)
        {
            action = SCHActions.Succor;
        }
        else
        {
            return false;
        }

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        var castTimeMs = (int)(action.CastTime * 1000);
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, castTimeMs))
        {
            context.Debug.AoEHealState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "AoE Heal";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
                var raidwideImminent = TimelineHelper.IsRaidwideImminent(
                    context.TimelineService,
                    context.BossMechanicDetector,
                    context.Configuration.Healing,
                    out _);

                var isSeraphism = action.ActionId == SCHActions.Concitation.ActionId;
                var shortReason = raidwideImminent
                    ? $"{action.Name} - pre-shield for raidwide!"
                    : $"{action.Name} - {injuredCount} injured at {avgHp:P0}";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    raidwideImminent ? "Raidwide damage incoming!" : "No raidwide predicted",
                    isSeraphism ? "Seraphism active - using Concitation" : "Using Succor",
                    "Provides heal + Galvanize shield",
                };

                var alternatives = _succorAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = action.Name,
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"{action.Name} cast for {injuredCount} injured party members at {avgHp:P0} average HP. {(raidwideImminent ? "Pre-shielding before incoming raidwide damage. " : "")}Succor/Concitation provides both healing (200 potency) and a Galvanize shield (320 potency). The shield absorbs damage, making it valuable before damage hits.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Succor is best used BEFORE damage (pre-shield) rather than after. After raidwides, prefer oGCD heals like Indomitability to save your GCD for damage.",
                    ConceptId = SchConcepts.SuccorUsage,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TrySingleTargetHeal(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        // Check if at least one GCD heal is enabled
        if (!config.EnableAdloquium && !config.EnablePhysick)
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        // Choose between Adloquium and Physick
        ActionDefinition? action = null;

        // Check for Manifestation (Seraphism upgrade of Adlo) - controlled by EnableAdloquium
        if (config.EnableAdloquium && context.FairyStateManager.IsSeraphOrSeraphismActive && player.Level >= SCHActions.Manifestation.MinLevel)
        {
            if (hpPercent <= config.AdloquiumThreshold)
            {
                // Avoid overwriting Sage shields
                if (!config.AvoidOverwritingSageShields || !HasSageShield(context, target))
                {
                    action = SCHActions.Manifestation;
                }
            }
        }
        // Adloquium for shields
        else if (config.EnableAdloquium && player.Level >= SCHActions.Adloquium.MinLevel && hpPercent <= config.AdloquiumThreshold)
        {
            // Avoid overwriting existing Galvanize or Sage shields
            if (!context.StatusHelper.HasGalvanize(target))
            {
                if (!config.AvoidOverwritingSageShields || !HasSageShield(context, target))
                {
                    action = SCHActions.Adloquium;
                }
            }
        }

        // Fall back to Physick for raw healing
        if (action == null && config.EnablePhysick && hpPercent <= config.PhysickThreshold)
        {
            action = SCHActions.Physick;
        }

        if (action == null)
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var healAmount = action.HealPotency * 10; // Rough estimate
            var castTimeMs = (int)(action.CastTime * 1000);
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, castTimeMs);

            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Single Heal";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var isAdlo = action.ActionId == SCHActions.Adloquium.ActionId || action.ActionId == SCHActions.Manifestation.ActionId;
                var isPhysick = action.ActionId == SCHActions.Physick.ActionId;

                string shortReason;
                string[] factors;
                string tip;
                string conceptId;

                if (isAdlo)
                {
                    shortReason = $"{action.Name} on {targetName} at {hpPercent:P0}";
                    factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
                        $"Threshold: {config.AdloquiumThreshold:P0}",
                        "Provides heal + Galvanize shield",
                        "Shield can crit for Catalyze bonus",
                        $"Target had no existing shield",
                    };
                    tip = "Adloquium is your primary single-target GCD heal. The shield is valuable before damage. Critical Adlos create massive shields with Catalyze!";
                    conceptId = SchConcepts.AdloquiumUsage;
                }
                else
                {
                    shortReason = $"Physick on {targetName} at {hpPercent:P0}";
                    factors = new[]
                    {
                        $"Target HP: {hpPercent:P0}",
                        $"Threshold: {config.PhysickThreshold:P0}",
                        "Pure healing (no shield)",
                        "Low MP cost",
                        "Used when shield not needed/available",
                    };
                    tip = "Physick is generally weak. Use Adloquium for shields or oGCDs like Lustrate when possible. Physick is a last resort.";
                    conceptId = SchConcepts.AdloquiumUsage;
                }

                var alternatives = new[]
                {
                    "Lustrate (oGCD, uses Aetherflow)",
                    "Excogitation (proactive)",
                    isPhysick ? "Adloquium (adds shield)" : "Physick (no shield needed)",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = action.Name,
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"{action.Name} on {targetName} at {hpPercent:P0} HP. {(isAdlo ? "Adloquium provides 300 potency heal plus a 540 potency Galvanize shield (or 810 with crit Catalyze). " : "Physick provides 450 potency heal but no shield. It's SCH's weakest GCD heal option. ")}GCD heals should be used sparingly - prefer oGCD heals when available.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = conceptId,
                    Priority = hpPercent < 0.3f ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    #endregion

    #region Helper Methods

    private bool ShouldUseExcogitation(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (player.Level < SCHActions.Excogitation.MinLevel)
            return false;

        var target = context.PartyHelper.FindExcogitationTarget(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        return hpPercent <= config.ExcogitationThreshold;
    }

    private bool ShouldUseIndomitability(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        return avgHp <= config.AoEHealThreshold && injuredCount >= config.AoEHealMinTargets;
    }

    private bool ShouldUseSingleTargetHeal(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        return hpPercent <= config.AdloquiumThreshold;
    }

    private bool ShouldUseAoEHeal(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        var (count, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, 0);
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Timeline-aware: pre-shield before raidwides
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            context.Configuration.Healing,
            out _);

        // Use if party needs healing OR raidwide is imminent (for pre-shielding)
        return (avgHp <= config.AoEHealThreshold && count >= config.AoEHealMinTargets) || raidwideImminent;
    }

    private static bool HasSageShield(AthenaContext context, IBattleChara target)
    {
        // Eukrasian Diagnosis status ID
        const ushort EukrasianDiagnosisStatusId = 2607;
        // Eukrasian Prognosis status ID
        const ushort EukrasianPrognosisStatusId = 2609;

        if (target.StatusList == null)
            return false;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == EukrasianDiagnosisStatusId ||
                status.StatusId == EukrasianPrognosisStatusId)
                return true;
        }
        return false;
    }

    #endregion
}
