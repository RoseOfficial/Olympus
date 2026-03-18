using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.KratosCore.Modules;

/// <summary>
/// Handles the Monk buff management.
/// Manages Riddle of Fire (burst window), Brotherhood (party buff),
/// Perfect Balance (Blitz setup), and Riddle of Wind (auto-attacks).
/// </summary>
public sealed class BuffModule : IKratosModule
{
    public int Priority => 20; // After role actions
    public string Name => "Buff";

    public bool TryExecute(IKratosContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        // Only use buff actions during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Priority 1: Riddle of Fire (personal burst)
        if (TryRiddleOfFire(context))
            return true;

        // Priority 2: Brotherhood (party buff)
        if (TryBrotherhood(context))
            return true;

        // Priority 3: Perfect Balance (Blitz setup)
        if (TryPerfectBalance(context))
            return true;

        // Priority 4: Riddle of Wind (auto-attack speed)
        if (TryRiddleOfWind(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IKratosContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IKratosContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region Riddle of Fire

    private bool TryRiddleOfFire(IKratosContext context)
    {
        if (!context.Configuration.Monk.EnableRiddleOfFire)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.RiddleOfFire.MinLevel)
            return false;

        // Already have Riddle of Fire active
        if (context.HasRiddleOfFire)
        {
            context.Debug.BuffState = $"RoF active ({context.RiddleOfFireRemaining:F1}s)";
            return false;
        }

        // Requirements for optimal Riddle of Fire usage:
        // 1. Disciplined Fist should be active (personal damage buff)
        // 2. Ideally align with Brotherhood window
        if (!context.HasDisciplinedFist)
        {
            context.Debug.BuffState = "Waiting for Disciplined Fist";
            return false;
        }

        // Check if Riddle of Fire is ready
        if (!context.ActionService.IsActionReady(MNKActions.RiddleOfFire.ActionId))
        {
            context.Debug.BuffState = "RoF on cooldown";
            return false;
        }

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Riddle of Fire (phase soon)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(MNKActions.RiddleOfFire, player.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.RiddleOfFire.Name;
            context.Debug.BuffState = "Activating Riddle of Fire";

            // Training: Record Riddle of Fire decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MNKActions.RiddleOfFire.ActionId, MNKActions.RiddleOfFire.Name)
                .AsMeleeBurst()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason("Activating Riddle of Fire (+15% damage for 20s)",
                    "Riddle of Fire is MNK's primary burst window. Use during Brotherhood and before Perfect Balance. " +
                    "Grants Fire's Rumination proc at high levels for extra damage.")
                .Factors(new[] { "Disciplined Fist active", "60s cooldown ready", "Starting burst window" })
                .Alternatives(new[] { "Hold for Brotherhood alignment", "Hold for phase timing" })
                .Tip("Riddle of Fire is your most important personal buff. Use on cooldown in most cases.")
                .Concept("mnk_riddle_of_fire")
                .Record();
            context.TrainingService?.RecordConceptApplication("mnk_riddle_of_fire", true, "Personal burst activation");

            return true;
        }

        return false;
    }

    #endregion

    #region Brotherhood

    private bool TryBrotherhood(IKratosContext context)
    {
        if (!context.Configuration.Monk.EnableBrotherhood)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.Brotherhood.MinLevel)
            return false;

        // Already have Brotherhood active
        if (context.HasBrotherhood)
        {
            context.Debug.BuffState = "Brotherhood active";
            return false;
        }

        // Brotherhood is a party buff - ideally use with Riddle of Fire
        // But don't hold it too long
        if (!context.ActionService.IsActionReady(MNKActions.Brotherhood.ActionId))
        {
            context.Debug.BuffState = "Brotherhood on cooldown";
            return false;
        }

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Brotherhood (phase soon)";
            return false;
        }

        // Best timing: Use with or just before Riddle of Fire
        // If RoF is active or ready, use Brotherhood
        if (!context.HasRiddleOfFire &&
            context.ActionService.IsActionReady(MNKActions.RiddleOfFire.ActionId) == false)
        {
            // RoF on cooldown and not active - wait for alignment if possible
            // But don't wait too long (Brotherhood has 120s CD)
            context.Debug.BuffState = "Waiting for RoF alignment";
            return false;
        }

        // Party coordination: Synchronize with other Olympus instances
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if our buffs are aligned with remote instances
            // If significantly desynced (e.g., death recovery), use independently
            if (!partyCoord.IsRaidBuffAligned(MNKActions.Brotherhood.ActionId))
            {
                context.Debug.BuffState = "Raid buffs desynced, using independently";
                // Fall through to execute - don't try to align when heavily desynced
            }
            // Check if another DPS is about to use a raid buff
            // If so, align our burst with theirs
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
            {
                context.Debug.BuffState = "Aligning with party burst";
                // Fall through to execute and announce our intent
            }

            // Announce our intent to use Brotherhood
            partyCoord.AnnounceRaidBuffIntent(MNKActions.Brotherhood.ActionId);
        }

        if (context.ActionService.ExecuteOgcd(MNKActions.Brotherhood, player.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.Brotherhood.Name;
            context.Debug.BuffState = "Activating Brotherhood";

            // Notify coordination service that we used the raid buff
            partyCoord?.OnRaidBuffUsed(MNKActions.Brotherhood.ActionId, 120_000);

            // Training: Record Brotherhood decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MNKActions.Brotherhood.ActionId, MNKActions.Brotherhood.Name)
                .AsRaidBuff()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason("Activating Brotherhood (+5% party damage for 20s)",
                    "Brotherhood is MNK's party-wide damage buff. Coordinate with other raid buffs. " +
                    "Also increases Chakra generation while active. Best used with Riddle of Fire.")
                .Factors(new[] { "120s cooldown ready", context.HasRiddleOfFire ? "Riddle of Fire active" : "Aligning with party", "Party buff timing" })
                .Alternatives(new[] { "Hold for phase timing", "Wait for other raid buffs" })
                .Tip("Brotherhood is a party buff. Coordinate with DRG, BRD, DNC for maximum party damage.")
                .Concept("mnk_brotherhood")
                .Record();
            context.TrainingService?.RecordConceptApplication("mnk_brotherhood", true, "Party burst activation");

            return true;
        }

        return false;
    }

    #endregion

    #region Perfect Balance

    private bool TryPerfectBalance(IKratosContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.PerfectBalance.MinLevel)
            return false;

        // Already in Perfect Balance
        if (context.HasPerfectBalance)
        {
            context.Debug.BuffState = $"PB active ({context.PerfectBalanceStacks} stacks)";
            return false;
        }

        // Don't use if we already have 3 Beast Chakra (about to Blitz)
        if (context.BeastChakraCount >= 3)
        {
            context.Debug.BuffState = "Ready to Blitz";
            return false;
        }

        // Optimal Perfect Balance usage:
        // 1. Use during Riddle of Fire window for burst
        // 2. Build towards the Blitz we need based on Nadi state

        // Check if Perfect Balance is ready
        if (!context.ActionService.IsActionReady(MNKActions.PerfectBalance.ActionId))
        {
            context.Debug.BuffState = "PB on cooldown";
            return false;
        }

        // Best used during Riddle of Fire for maximum burst
        // But don't hold charges too long (2 charges at level 82+)
        bool shouldUsePB = context.HasRiddleOfFire ||
                           (context.HasDisciplinedFist && !context.ActionService.IsActionReady(MNKActions.RiddleOfFire.ActionId));

        if (!shouldUsePB)
        {
            context.Debug.BuffState = "Waiting for RoF for PB";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(MNKActions.PerfectBalance, player.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.PerfectBalance.Name;
            context.Debug.BuffState = "Activating Perfect Balance";

            // Training: Record Perfect Balance decision
            var targetBlitz = DetermineTargetBlitz(context);
            TrainingHelper.Decision(context.TrainingService)
                .Action(MNKActions.PerfectBalance.ActionId, MNKActions.PerfectBalance.Name)
                .AsMeleeBurst()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason($"Activating Perfect Balance (building {targetBlitz})",
                    "Perfect Balance lets you use any form GCD regardless of current form. " +
                    "Use to build 3 Beast Chakra for a Blitz. 2 charges at level 82+.")
                .Factors(new[] { context.HasRiddleOfFire ? "Riddle of Fire active" : "Burst window", $"Building {targetBlitz}", $"Nadi: {(context.HasLunarNadi ? "Lunar" : "")} {(context.HasSolarNadi ? "Solar" : "")}" })
                .Alternatives(new[] { "Wait for Riddle of Fire", "Hold charge for emergency" })
                .Tip("Use PB during RoF for maximum burst. Build Lunar → Solar → Phantom Rush.")
                .Concept("mnk_perfect_balance")
                .Record();
            context.TrainingService?.RecordConceptApplication("mnk_perfect_balance", true, "Blitz setup");

            return true;
        }

        return false;
    }

    private static string DetermineTargetBlitz(IKratosContext context)
    {
        if (context.HasBothNadi)
            return "Phantom Rush";
        if (!context.HasLunarNadi)
            return "Elixir Field (Lunar)";
        if (!context.HasSolarNadi)
            return "Rising Phoenix (Solar)";
        return "Phantom Rush";
    }

    #endregion

    #region Riddle of Wind

    private bool TryRiddleOfWind(IKratosContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.RiddleOfWind.MinLevel)
            return false;

        // Already have Riddle of Wind active
        if (context.HasRiddleOfWind)
        {
            context.Debug.BuffState = "RoW active";
            return false;
        }

        // Riddle of Wind increases auto-attack speed
        // Use during burst windows or as filler
        if (!context.ActionService.IsActionReady(MNKActions.RiddleOfWind.ActionId))
        {
            context.Debug.BuffState = "RoW on cooldown";
            return false;
        }

        // Use freely as a damage increase
        if (context.ActionService.ExecuteOgcd(MNKActions.RiddleOfWind, player.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.RiddleOfWind.Name;
            context.Debug.BuffState = "Activating Riddle of Wind";

            // Training: Record Riddle of Wind decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MNKActions.RiddleOfWind.ActionId, MNKActions.RiddleOfWind.Name)
                .AsMeleeDamage()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason("Activating Riddle of Wind (+50% auto-attack speed)",
                    "Riddle of Wind increases auto-attack speed for 15s. " +
                    "Use on cooldown for passive damage. Grants Wind's Rumination proc.")
                .Factors(new[] { "90s cooldown ready", "Passive damage increase", "Grants Wind's Rumination" })
                .Alternatives(new[] { "No reason to hold", "Low priority buff" })
                .Tip("Riddle of Wind is free damage. Use on cooldown, weave between GCDs.")
                .Concept("mnk_riddle_of_wind")
                .Record();
            context.TrainingService?.RecordConceptApplication("mnk_riddle_of_wind", true, "Auto-attack buff");

            return true;
        }

        return false;
    }

    #endregion
}
