using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Services;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.ThanatosCore.Modules;

/// <summary>
/// Handles the Reaper buff management.
/// Manages Arcane Circle (party buff), Enshroud (burst state),
/// and Plentiful Harvest (Immortal Sacrifice consumption).
/// </summary>
public sealed class BuffModule : IThanatosModule
{
    public int Priority => 20; // After role actions
    public string Name => "Buff";

    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public bool TryExecute(IThanatosContext context, bool isMoving)
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

        // Priority 1: Arcane Circle (party buff)
        if (TryArcaneCircle(context))
            return true;

        // Priority 2: Enshroud (burst state)
        if (TryEnshroud(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IThanatosContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IThanatosContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region Arcane Circle

    private bool TryArcaneCircle(IThanatosContext context)
    {
        if (!context.Configuration.Reaper.EnableArcaneCircle)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.ArcaneCircle.MinLevel)
            return false;

        // Already have Arcane Circle active
        if (context.HasArcaneCircle)
        {
            context.Debug.BuffState = $"AC active ({context.ArcaneCircleRemaining:F1}s)";
            return false;
        }

        // Check if Arcane Circle is ready
        if (!context.ActionService.IsActionReady(RPRActions.ArcaneCircle.ActionId))
        {
            context.Debug.BuffState = "Arcane Circle on cooldown";
            return false;
        }

        // Hold for party burst window if pooling is enabled
        if (context.Configuration.Reaper.EnableBurstPooling &&
            ShouldHoldForBurst(context.Configuration.Reaper.ArcaneCircleHoldTime))
        {
            context.Debug.BuffState = "Holding Arcane Circle for burst window";
            return false;
        }

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Arcane Circle (phase soon)";
            return false;
        }

        // Requirements for optimal Arcane Circle usage:
        // 1. Have Death's Design on target (damage buff)
        // 2. Have at least 50 Shroud or be in good resource state
        if (!context.HasDeathsDesign)
        {
            context.Debug.BuffState = "Waiting for Death's Design";
            return false;
        }

        // Party coordination: Synchronize with other Olympus instances
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if our buffs are aligned with remote instances
            // If significantly desynced (e.g., death recovery), use independently
            if (!partyCoord.IsRaidBuffAligned(RPRActions.ArcaneCircle.ActionId))
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

            // Announce our intent to use Arcane Circle
            partyCoord.AnnounceRaidBuffIntent(RPRActions.ArcaneCircle.ActionId);
        }

        // Use Arcane Circle
        if (context.ActionService.ExecuteOgcd(RPRActions.ArcaneCircle, player.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.ArcaneCircle.Name;
            context.Debug.BuffState = "Activating Arcane Circle";

            // Notify coordination service that we used the raid buff
            partyCoord?.OnRaidBuffUsed(RPRActions.ArcaneCircle.ActionId, 120_000);

            // Training: Record Arcane Circle decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(RPRActions.ArcaneCircle.ActionId, RPRActions.ArcaneCircle.Name)
                .AsMeleeBurst()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason("Activating Arcane Circle (+3% party damage for 20s)",
                    "Arcane Circle is RPR's party buff. Grants Bloodsown Circle for personal damage and " +
                    "builds Immortal Sacrifice stacks from party GCDs for Plentiful Harvest.")
                .Factors(new[] { "Death's Design active", "120s cooldown ready", "Party burst timing" })
                .Alternatives(new[] { "Hold for phase timing", "Wait for other raid buffs" })
                .Tip("Arcane Circle grants stacks from party GCDs. Use when the party will be actively attacking.")
                .Concept("rpr_arcane_circle")
                .Record();
            context.TrainingService?.RecordConceptApplication("rpr_arcane_circle", true, "Party burst activation");

            return true;
        }

        return false;
    }

    #endregion

    #region Enshroud

    private bool TryEnshroud(IThanatosContext context)
    {
        if (!context.Configuration.Reaper.EnableEnshroud)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.Enshroud.MinLevel)
            return false;

        // Already in Enshroud
        if (context.IsEnshrouded)
        {
            context.Debug.BuffState = context.Debug.GetEnshroudState();
            return false;
        }

        // Can't Enshroud during Soul Reaver
        if (context.HasSoulReaver)
        {
            context.Debug.BuffState = "In Soul Reaver state";
            return false;
        }

        // Need 50 Shroud to Enshroud
        if (context.Shroud < 50)
        {
            context.Debug.BuffState = $"Need 50 Shroud ({context.Shroud}/50)";
            return false;
        }

        // Check if Enshroud is ready
        if (!context.ActionService.IsActionReady(RPRActions.Enshroud.ActionId))
        {
            context.Debug.BuffState = "Enshroud on cooldown";
            return false;
        }

        // Hold Enshroud activation for burst when burst is imminent (unless capping on Shroud)
        if (context.Configuration.Reaper.EnableBurstPooling && ShouldHoldForBurst(context.Configuration.Reaper.ArcaneCircleHoldTime) && context.Shroud < 90)
        {
            context.Debug.BuffState = "Holding Enshroud for burst";
            return false;
        }

        // Optimal Enshroud timing:
        // 1. During Arcane Circle window for maximum burst
        // 2. Or when about to cap on Shroud
        bool shouldEnshroud = context.HasArcaneCircle ||
                              context.Shroud >= 90 ||
                              (context.HasDeathsDesign && context.DeathsDesignRemaining > 15f);

        if (!shouldEnshroud)
        {
            context.Debug.BuffState = "Waiting for burst window";
            return false;
        }

        // Make sure Death's Design is on target with good duration
        if (!context.HasDeathsDesign || context.DeathsDesignRemaining < 10f)
        {
            context.Debug.BuffState = "Need Death's Design refresh";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(RPRActions.Enshroud, player.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.Enshroud.Name;
            context.Debug.BuffState = "Entering Enshroud";

            // Training: Record Enshroud decision
            var reason = context.HasArcaneCircle ? "Arcane Circle active" :
                         context.Shroud >= 90 ? "Shroud gauge nearly full" :
                         "Death's Design has good duration";
            TrainingHelper.Decision(context.TrainingService)
                .Action(RPRActions.Enshroud.ActionId, RPRActions.Enshroud.Name)
                .AsMeleeBurst()
                .Target(player.Name?.TextValue ?? "Self")
                .Reason($"Entering Enshroud ({reason})",
                    "Enshroud transforms your rotation into high-damage Void/Cross Reaping GCDs. " +
                    "Grants 5 Lemure Shroud stacks. Build Void Shroud with Reaping GCDs for Lemure's Slice. " +
                    "Finish with Communio → Perfectio for maximum burst.")
                .Factors(new[] { $"Shroud: {context.Shroud}/50", reason, $"Death's Design: {context.DeathsDesignRemaining:F1}s" })
                .Alternatives(new[] { "Wait for Arcane Circle", "Save for burst window" })
                .Tip("Enshroud is your primary burst phase. Prioritize during Arcane Circle window.")
                .Concept("rpr_enshroud")
                .Record();
            context.TrainingService?.RecordConceptApplication("rpr_enshroud", true, "Burst phase entry");

            return true;
        }

        return false;
    }

    #endregion
}
