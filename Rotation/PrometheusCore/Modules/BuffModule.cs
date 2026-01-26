using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.PrometheusCore.Context;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.PrometheusCore.Modules;

/// <summary>
/// Handles Machinist buff management and oGCD optimization.
/// Manages Wildfire, Barrel Stabilizer, Reassemble, Hypercharge, and Queen.
/// </summary>
public sealed class BuffModule : IPrometheusModule
{
    public int Priority => 20; // Higher priority than damage
    public string Name => "Buff";

    public bool TryExecute(IPrometheusContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        if (!context.CanExecuteOgcd)
        {
            context.Debug.BuffState = "oGCD not ready";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target for targeted oGCDs
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.BuffState = "No target";
            return false;
        }

        // Priority 1: Wildfire (align with Hypercharge burst)
        if (TryWildfire(context, target))
            return true;

        // Priority 2: Barrel Stabilizer (generates Heat and Full Metal Machinist)
        if (TryBarrelStabilizer(context))
            return true;

        // Priority 3: Reassemble (before high-potency tool actions)
        if (TryReassemble(context))
            return true;

        // Priority 4: Hypercharge (when Heat >= 50)
        if (TryHypercharge(context))
            return true;

        // Priority 5: Automaton Queen (when Battery >= 50)
        if (TryAutomatonQueen(context))
            return true;

        // Priority 6: Gauss Round / Ricochet (dump charges during Overheated)
        if (TryGaussRoundRicochet(context, target))
            return true;

        context.Debug.BuffState = "No buff action";
        return false;
    }

    public void UpdateDebugState(IPrometheusContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IPrometheusContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region Buff Actions

    private bool TryWildfire(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MCHActions.Wildfire.MinLevel)
            return false;

        // Don't reapply if already on target
        if (context.HasWildfire)
            return false;

        // Optimal: Use with Hypercharge for maximum hits
        // Use when Hypercharge is about to be used or already active
        var shouldUse = context.IsOverheated ||
                        (context.Heat >= 50 && context.ActionService.IsActionReady(MCHActions.Hypercharge.ActionId));

        if (!shouldUse)
        {
            context.Debug.BuffState = "Waiting for Hypercharge alignment";
            return false;
        }

        if (!context.ActionService.IsActionReady(MCHActions.Wildfire.ActionId))
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Wildfire (phase soon)";
            return false;
        }

        // Party coordination: Align with party burst window
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if party is about to burst - if so, hold briefly to align
            if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
            {
                context.Debug.BuffState = "Aligning Wildfire with party burst";
                // Fall through to execute - we want to burst WITH the party
            }

            // Announce our intent to use Wildfire burst
            partyCoord.AnnounceRaidBuffIntent(MCHActions.Wildfire.ActionId);
        }

        if (context.ActionService.ExecuteOgcd(MCHActions.Wildfire, target.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.Wildfire.Name;
            context.Debug.BuffState = "Wildfire applied";

            // Notify coordination service that we used the burst
            partyCoord?.OnRaidBuffUsed(MCHActions.Wildfire.ActionId, 120_000);

            // Training: Record Wildfire decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MCHActions.Wildfire.ActionId, MCHActions.Wildfire.Name)
                .AsRangedBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Wildfire applied (Hypercharge burst window)",
                    "Wildfire is MCH's 2-minute burst. It accumulates damage based on weapon skills landed during its duration. " +
                    "Always pair with Hypercharge to maximize Heat Blast hits. 10s duration, aim for 6 GCDs inside.")
                .Factors(context.IsOverheated ? "Overheated active" : "Heat >= 50", "Hypercharge ready/active", "120s cooldown ready")
                .Alternatives("Hold for party raid buffs", "Hold for phase timing")
                .Tip("Wildfire counts GCDs landed. Use with Hypercharge for 5-6 Heat Blasts inside the window.")
                .Concept("mch.wildfire_placement")
                .Record();
            context.TrainingService?.RecordConceptApplication("mch.wildfire_placement", true, "Burst window activation");
            context.TrainingService?.RecordConceptApplication("mch.burst_party_sync", true, "Party coordination");

            return true;
        }

        return false;
    }

    private bool TryBarrelStabilizer(IPrometheusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MCHActions.BarrelStabilizer.MinLevel)
            return false;

        // Use when Heat < 50 to avoid overcapping
        if (context.Heat > 50)
        {
            context.Debug.BuffState = "Heat too high for Barrel Stabilizer";
            return false;
        }

        if (!context.ActionService.IsActionReady(MCHActions.BarrelStabilizer.ActionId))
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Barrel Stabilizer (phase soon)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(MCHActions.BarrelStabilizer, player.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.BarrelStabilizer.Name;
            context.Debug.BuffState = "Barrel Stabilizer used (+50 Heat)";

            // Training: Record Barrel Stabilizer decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MCHActions.BarrelStabilizer.ActionId, MCHActions.BarrelStabilizer.Name)
                .AsRangedResource("Heat", context.Heat)
                .Reason(
                    $"Barrel Stabilizer used (Heat: {context.Heat} → {context.Heat + 50})",
                    "Barrel Stabilizer grants +50 Heat instantly, enabling Hypercharge. At Lv.100, also grants Full Metal Machinist " +
                    "for Full Metal Field. Use on cooldown, but avoid overcapping Heat above 50.")
                .Factors($"Heat: {context.Heat}/100", "120s cooldown ready", "Won't overcap Heat")
                .Alternatives("Wait if Heat > 50", "Hold for phase timing")
                .Tip("Use Barrel Stabilizer on cooldown. At Lv.100, follow with Full Metal Field before Hypercharge.")
                .Concept("mch.heat_gauge")
                .Record();
            context.TrainingService?.RecordConceptApplication("mch.heat_gauge", true, "Heat generation");
            context.TrainingService?.RecordConceptApplication("mch.gauge_overcapping", context.Heat <= 50, "Overcap prevention");

            return true;
        }

        return false;
    }

    private bool TryReassemble(IPrometheusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MCHActions.Reassemble.MinLevel)
            return false;

        // Already have Reassemble active
        if (context.HasReassemble)
            return false;

        // Don't overcap charges - use if at max
        if (context.ReassembleCharges == 0)
            return false;

        // Use before high-potency tool actions
        // Check if Drill, Air Anchor, Chain Saw, or Excavator are coming up
        bool hasHighPotencyReady = false;

        if (level >= MCHActions.Drill.MinLevel && context.ActionService.IsActionReady(MCHActions.Drill.ActionId))
            hasHighPotencyReady = true;

        if (level >= MCHActions.AirAnchor.MinLevel && context.ActionService.IsActionReady(MCHActions.AirAnchor.ActionId))
            hasHighPotencyReady = true;

        if (level >= MCHActions.ChainSaw.MinLevel && context.ActionService.IsActionReady(MCHActions.ChainSaw.ActionId))
            hasHighPotencyReady = true;

        if (context.HasExcavatorReady)
            hasHighPotencyReady = true;

        // Also use before Full Metal Field
        if (context.HasFullMetalMachinist)
            hasHighPotencyReady = true;

        // If we have max charges and nothing ready yet, still use to avoid overcap
        bool shouldUse = hasHighPotencyReady || (context.ReassembleCharges >= 2);

        if (!shouldUse)
            return false;

        if (!context.ActionService.IsActionReady(MCHActions.Reassemble.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(MCHActions.Reassemble, player.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.Reassemble.Name;
            context.Debug.BuffState = "Reassemble (charges: " + context.ReassembleCharges + ")";

            // Training: Record Reassemble decision
            var reason = hasHighPotencyReady ? "High-potency action ready" : "Preventing charge overcap";
            TrainingHelper.Decision(context.TrainingService)
                .Action(MCHActions.Reassemble.ActionId, MCHActions.Reassemble.Name)
                .AsRangedBurst()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason(
                    $"Reassemble activated ({reason})",
                    "Reassemble guarantees critical direct hit on next weaponskill. Prioritize Drill, Air Anchor, Chain Saw, " +
                    "Excavator, or Full Metal Field. Has 2 charges at Lv.84+, avoid overcapping.")
                .Factors($"Charges: {context.ReassembleCharges}", reason)
                .Alternatives("Save for higher potency action", "Wait for Drill/Air Anchor/Chain Saw")
                .Tip("Use Reassemble before Drill (highest priority), then Air Anchor, Chain Saw, or Excavator.")
                .Concept("mch.reassemble_priority")
                .Record();
            context.TrainingService?.RecordConceptApplication("mch.reassemble_priority", hasHighPotencyReady, "Optimal Reassemble target");
            context.TrainingService?.RecordConceptApplication("mch.reassemble_charges", context.ReassembleCharges < 2, "Charge management");

            return true;
        }

        return false;
    }

    private bool TryHypercharge(IPrometheusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MCHActions.Hypercharge.MinLevel)
            return false;

        // Already overheated
        if (context.IsOverheated)
            return false;

        // Need 50 Heat
        if (context.Heat < 50)
        {
            context.Debug.BuffState = $"Need 50 Heat ({context.Heat}/50)";
            return false;
        }

        // Don't use if we'd clip a tool action
        // Check if Drill, Air Anchor, or Chain Saw are about to come off cooldown
        // Hypercharge is ~10s of Heat Blast spam, so check if tools are ready in next few seconds
        // For simplicity, check if tools are ready now and skip Hypercharge

        if (level >= MCHActions.Drill.MinLevel && context.ActionService.IsActionReady(MCHActions.Drill.ActionId))
        {
            context.Debug.BuffState = "Drill ready - use first";
            return false;
        }

        if (level >= MCHActions.AirAnchor.MinLevel && context.ActionService.IsActionReady(MCHActions.AirAnchor.ActionId))
        {
            context.Debug.BuffState = "Air Anchor ready - use first";
            return false;
        }

        if (level >= MCHActions.ChainSaw.MinLevel && context.ActionService.IsActionReady(MCHActions.ChainSaw.ActionId))
        {
            context.Debug.BuffState = "Chain Saw ready - use first";
            return false;
        }

        if (!context.ActionService.IsActionReady(MCHActions.Hypercharge.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(MCHActions.Hypercharge, player.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.Hypercharge.Name;
            context.Debug.BuffState = "Hypercharge activated";

            // Training: Record Hypercharge decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MCHActions.Hypercharge.ActionId, MCHActions.Hypercharge.Name)
                .AsRangedBurst()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason(
                    $"Hypercharge activated (Heat: {context.Heat} → {context.Heat - 50})",
                    "Hypercharge spends 50 Heat to enter Overheated state for ~10s. During Overheated, use Heat Blast (1.5s GCD) " +
                    "and weave Gauss Round/Ricochet. Always pair with Wildfire for maximum burst.")
                .Factors($"Heat: {context.Heat}/100", "No tool actions imminent", "Wildfire window optimal")
                .Alternatives("Wait for Drill/Air Anchor/Chain Saw", "Hold for Wildfire alignment")
                .Tip("Enter Hypercharge when tools are on cooldown. Spam Heat Blast and weave oGCDs during Overheated.")
                .Concept("mch.hypercharge_activation")
                .Record();
            context.TrainingService?.RecordConceptApplication("mch.hypercharge_activation", true, "Burst phase entry");
            context.TrainingService?.RecordConceptApplication("mch.hypercharge_timing", true, "Tool cooldown check");

            return true;
        }

        return false;
    }

    private bool TryAutomatonQueen(IPrometheusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Check for Queen or Rook
        var petAction = MCHActions.GetPetSummon(level);
        if (level < petAction.MinLevel)
            return false;

        // Queen already active
        if (context.IsQueenActive)
            return false;

        // Need at least 50 Battery
        if (context.Battery < 50)
        {
            context.Debug.BuffState = $"Need 50 Battery ({context.Battery}/50)";
            return false;
        }

        // Ideally summon at 100 Battery for maximum damage
        // But don't overcap - summon at 90+ to be safe
        bool shouldSummon = context.Battery >= 90 ||
                            (context.Battery >= 50 && context.Battery < 60); // Use at minimum to avoid overcap from tools

        // If we're at 100, always summon
        if (context.Battery >= 100)
            shouldSummon = true;

        if (!shouldSummon)
        {
            context.Debug.BuffState = $"Building Battery ({context.Battery}/100)";
            return false;
        }

        if (!context.ActionService.IsActionReady(petAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(petAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = petAction.Name;
            context.Debug.BuffState = $"{petAction.Name} summoned ({context.Battery} Battery)";

            // Training: Record Queen summon decision
            var batteryReason = context.Battery >= 100 ? "Maximum Battery" :
                                context.Battery >= 90 ? "Near-maximum Battery" :
                                "Preventing overcap";
            TrainingHelper.Decision(context.TrainingService)
                .Action(petAction.ActionId, petAction.Name)
                .AsRangedResource("Battery", context.Battery)
                .Reason(
                    $"{petAction.Name} summoned ({batteryReason})",
                    "Automaton Queen scales with Battery spent (50-100). Higher Battery = stronger Queen. " +
                    "Summon at 90-100 Battery for maximum damage, but don't let Battery overcap from tool actions.")
                .Factors($"Battery: {context.Battery}/100", batteryReason)
                .Alternatives("Wait for 100 Battery", "Use earlier if about to overcap")
                .Tip("Summon Queen at 90-100 Battery for maximum damage. Air Anchor and Chain Saw grant +20 Battery each.")
                .Concept("mch.queen_summoning")
                .Record();
            context.TrainingService?.RecordConceptApplication("mch.queen_summoning", true, "Pet deployment");
            context.TrainingService?.RecordConceptApplication("mch.battery_gauge", context.Battery >= 90, "Optimal Battery usage");
            context.TrainingService?.RecordConceptApplication("mch.queen_damage_scaling", context.Battery >= 90, "Battery maximization");

            return true;
        }

        return false;
    }

    private bool TryGaussRoundRicochet(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority: Dump charges during Overheated (Heat Blast reduces CD)
        // Otherwise, just avoid overcapping

        var gaussAction = MCHActions.GetGaussRound(level);
        var ricochetAction = MCHActions.GetRicochet(level);

        // Try Gauss Round first if more charges
        if (level >= gaussAction.MinLevel && context.GaussRoundCharges > 0)
        {
            // Use during Overheated or if at max charges
            bool shouldUse = context.IsOverheated || context.GaussRoundCharges >= 3;

            if (shouldUse && context.ActionService.IsActionReady(gaussAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(gaussAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = gaussAction.Name;
                    context.Debug.BuffState = $"{gaussAction.Name} (charges: {context.GaussRoundCharges})";

                    // Training: Record Gauss Round decision
                    var gaussReason = context.IsOverheated ? "Weaving during Overheated" : "Preventing charge overcap";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(gaussAction.ActionId, gaussAction.Name)
                        .AsRangedDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason(
                            $"{gaussAction.Name} ({gaussReason})",
                            "Gauss Round is a charge-based oGCD (3 charges max). Weave between Heat Blasts during Overheated. " +
                            "Heat Blast reduces its cooldown, so dump charges aggressively during Hypercharge.")
                        .Factors($"Charges: {context.GaussRoundCharges}/3", gaussReason)
                        .Alternatives("Save for Overheated phase", "Alternate with Ricochet")
                        .Tip("During Overheated, alternate Gauss Round and Ricochet between Heat Blasts for optimal weaving.")
                        .Concept("mch.ogcd_weaving")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("mch.ogcd_weaving", context.IsOverheated, "Overheated weaving");

                    return true;
                }
            }
        }

        // Try Ricochet
        if (level >= ricochetAction.MinLevel && context.RicochetCharges > 0)
        {
            bool shouldUse = context.IsOverheated || context.RicochetCharges >= 3;

            if (shouldUse && context.ActionService.IsActionReady(ricochetAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(ricochetAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = ricochetAction.Name;
                    context.Debug.BuffState = $"{ricochetAction.Name} (charges: {context.RicochetCharges})";

                    // Training: Record Ricochet decision
                    var ricochetReason = context.IsOverheated ? "Weaving during Overheated" : "Preventing charge overcap";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(ricochetAction.ActionId, ricochetAction.Name)
                        .AsRangedDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason(
                            $"{ricochetAction.Name} ({ricochetReason})",
                            "Ricochet is a charge-based AoE oGCD (3 charges max). Weave between Heat Blasts during Overheated. " +
                            "Heat Blast reduces its cooldown, so dump charges aggressively during Hypercharge.")
                        .Factors($"Charges: {context.RicochetCharges}/3", ricochetReason)
                        .Alternatives("Save for Overheated phase", "Alternate with Gauss Round")
                        .Tip("During Overheated, alternate Ricochet and Gauss Round between Heat Blasts for optimal weaving.")
                        .Concept("mch.ogcd_weaving")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("mch.ogcd_weaving", context.IsOverheated, "Overheated weaving");

                    return true;
                }
            }
        }

        return false;
    }

    #endregion
}
