using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.KratosCore.Abilities;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.KratosCore.Modules;

/// <summary>
/// Handles the Monk buff management (scheduler-driven).
/// Manages Riddle of Fire, Brotherhood, Perfect Balance, Riddle of Wind.
/// </summary>
public sealed class BuffModule : IKratosModule
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

    public bool TryExecute(IKratosContext context, bool isMoving) => false;

    public void UpdateDebugState(IKratosContext context) { }

    public void CollectCandidates(IKratosContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }

        TryPushRiddleOfFire(context, scheduler);
        TryPushBrotherhood(context, scheduler);
        TryPushPerfectBalance(context, scheduler);
        TryPushRiddleOfWind(context, scheduler);
    }

    private void TryPushRiddleOfFire(IKratosContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Monk.EnableRiddleOfFire) return;
        var player = context.Player;
        if (player.Level < MNKActions.RiddleOfFire.MinLevel) return;

        if (context.HasRiddleOfFire)
        {
            context.Debug.BuffState = $"RoF active ({context.RiddleOfFireRemaining:F1}s)";
            return;
        }
        if (!context.HasDisciplinedFist)
        {
            context.Debug.BuffState = "Waiting for Disciplined Fist";
            return;
        }
        if (!context.ActionService.IsActionReady(MNKActions.RiddleOfFire.ActionId)) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Riddle of Fire (phase soon)";
            return;
        }
        if (ShouldHoldForBurst())
        {
            context.Debug.BuffState = "Holding Riddle of Fire for burst";
            return;
        }

        scheduler.PushOgcd(KratosAbilities.RiddleOfFire, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MNKActions.RiddleOfFire.Name;
                context.Debug.BuffState = "Activating Riddle of Fire";

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
            });
    }

    private void TryPushBrotherhood(IKratosContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Monk.EnableBrotherhood) return;
        var player = context.Player;
        if (player.Level < MNKActions.Brotherhood.MinLevel) return;

        if (context.HasBrotherhood)
        {
            context.Debug.BuffState = "Brotherhood active";
            return;
        }
        if (!context.ActionService.IsActionReady(MNKActions.Brotherhood.ActionId)) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Brotherhood (phase soon)";
            return;
        }
        if (ShouldHoldForBurst(context.Configuration.Monk.BrotherhoodHoldTime))
        {
            context.Debug.BuffState = "Holding Brotherhood for burst";
            return;
        }
        if (!context.HasRiddleOfFire &&
            !context.ActionService.IsActionReady(MNKActions.RiddleOfFire.ActionId))
        {
            context.Debug.BuffState = "Waiting for RoF alignment";
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            if (!partyCoord.IsRaidBuffAligned(MNKActions.Brotherhood.ActionId))
                context.Debug.BuffState = "Raid buffs desynced, using independently";
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
                context.Debug.BuffState = "Aligning with party burst";
            partyCoord.AnnounceRaidBuffIntent(MNKActions.Brotherhood.ActionId);
        }

        scheduler.PushOgcd(KratosAbilities.Brotherhood, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MNKActions.Brotherhood.Name;
                context.Debug.BuffState = "Activating Brotherhood";
                partyCoord?.OnRaidBuffUsed(MNKActions.Brotherhood.ActionId, 120_000);

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
            });
    }

    private void TryPushPerfectBalance(IKratosContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Monk.EnablePerfectBalance) return;
        var player = context.Player;
        if (player.Level < MNKActions.PerfectBalance.MinLevel) return;

        if (context.HasPerfectBalance)
        {
            context.Debug.BuffState = $"PB active ({context.PerfectBalanceStacks} stacks)";
            return;
        }
        if (context.BeastChakraCount >= 3)
        {
            context.Debug.BuffState = "Ready to Blitz";
            return;
        }
        if (!context.ActionService.IsActionReady(MNKActions.PerfectBalance.ActionId)) return;

        bool shouldUsePB = context.HasRiddleOfFire ||
                           (context.HasDisciplinedFist && !context.ActionService.IsActionReady(MNKActions.RiddleOfFire.ActionId));
        if (!shouldUsePB)
        {
            context.Debug.BuffState = "Waiting for RoF for PB";
            return;
        }

        scheduler.PushOgcd(KratosAbilities.PerfectBalance, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MNKActions.PerfectBalance.Name;
                context.Debug.BuffState = "Activating Perfect Balance";

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
            });
    }

    private static string DetermineTargetBlitz(IKratosContext context)
    {
        if (context.HasBothNadi) return "Phantom Rush";
        if (!context.HasLunarNadi) return "Elixir Field (Lunar)";
        if (!context.HasSolarNadi) return "Rising Phoenix (Solar)";
        return "Phantom Rush";
    }

    private void TryPushRiddleOfWind(IKratosContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Monk.EnableRiddleOfWind) return;
        var player = context.Player;
        if (player.Level < MNKActions.RiddleOfWind.MinLevel) return;

        if (context.HasRiddleOfWind)
        {
            context.Debug.BuffState = "RoW active";
            return;
        }
        if (!context.ActionService.IsActionReady(MNKActions.RiddleOfWind.ActionId)) return;

        scheduler.PushOgcd(KratosAbilities.RiddleOfWind, player.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MNKActions.RiddleOfWind.Name;
                context.Debug.BuffState = "Activating Riddle of Wind";

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
            });
    }
}
