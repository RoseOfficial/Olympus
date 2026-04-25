using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThanatosCore.Abilities;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.ThanatosCore.Modules;

/// <summary>
/// Handles the Reaper buff management (scheduler-driven).
/// Manages Arcane Circle (party buff) and Enshroud (burst state).
/// </summary>
public sealed class BuffModule : IThanatosModule
{
    public int Priority => 20;
    public string Name => "Buff";

    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public bool TryExecute(IThanatosContext context, bool isMoving) => false;

    public void UpdateDebugState(IThanatosContext context) { }

    public void CollectCandidates(IThanatosContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }

        TryPushArcaneCircle(context, scheduler);
        TryPushEnshroud(context, scheduler);
    }

    private void TryPushArcaneCircle(IThanatosContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Reaper.EnableArcaneCircle) return;
        var player = context.Player;
        if (player.Level < RPRActions.ArcaneCircle.MinLevel) return;
        if (context.HasArcaneCircle)
        {
            context.Debug.BuffState = $"AC active ({context.ArcaneCircleRemaining:F1}s)";
            return;
        }
        if (!context.ActionService.IsActionReady(RPRActions.ArcaneCircle.ActionId)) return;

        if (context.Configuration.Reaper.EnableBurstPooling &&
            ShouldHoldForBurst(context.Configuration.Reaper.ArcaneCircleHoldTime))
        {
            context.Debug.BuffState = "Holding Arcane Circle for burst window";
            return;
        }
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Arcane Circle (phase soon)";
            return;
        }
        if (!context.HasDeathsDesign)
        {
            context.Debug.BuffState = "Waiting for Death's Design";
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            if (!partyCoord.IsRaidBuffAligned(RPRActions.ArcaneCircle.ActionId))
                context.Debug.BuffState = "Raid buffs desynced, using independently";
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
                context.Debug.BuffState = "Aligning with party burst";
            partyCoord.AnnounceRaidBuffIntent(RPRActions.ArcaneCircle.ActionId);
        }

        scheduler.PushOgcd(ThanatosAbilities.ArcaneCircle, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RPRActions.ArcaneCircle.Name;
                context.Debug.BuffState = "Activating Arcane Circle";
                partyCoord?.OnRaidBuffUsed(RPRActions.ArcaneCircle.ActionId, 120_000);

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
            });
    }

    private void TryPushEnshroud(IThanatosContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Reaper.EnableEnshroud) return;
        var player = context.Player;
        if (player.Level < RPRActions.Enshroud.MinLevel) return;
        if (context.IsEnshrouded)
        {
            context.Debug.BuffState = context.Debug.GetEnshroudState();
            return;
        }
        if (context.HasSoulReaver)
        {
            context.Debug.BuffState = "In Soul Reaver state";
            return;
        }
        if (context.Shroud < 50)
        {
            context.Debug.BuffState = $"Need 50 Shroud ({context.Shroud}/50)";
            return;
        }
        if (!context.ActionService.IsActionReady(RPRActions.Enshroud.ActionId)) return;

        if (context.Configuration.Reaper.EnableBurstPooling
            && ShouldHoldForBurst(context.Configuration.Reaper.ArcaneCircleHoldTime)
            && context.Shroud < 90)
        {
            context.Debug.BuffState = "Holding Enshroud for burst";
            return;
        }

        bool shouldEnshroud = context.HasArcaneCircle
                              || context.Shroud >= 90
                              || (context.HasDeathsDesign && context.DeathsDesignRemaining > 15f);
        if (!shouldEnshroud)
        {
            context.Debug.BuffState = "Waiting for burst window";
            return;
        }
        if (!context.HasDeathsDesign || context.DeathsDesignRemaining < 10f)
        {
            context.Debug.BuffState = "Need Death's Design refresh";
            return;
        }

        scheduler.PushOgcd(ThanatosAbilities.Enshroud, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RPRActions.Enshroud.Name;
                context.Debug.BuffState = "Entering Enshroud";

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
            });
    }
}
