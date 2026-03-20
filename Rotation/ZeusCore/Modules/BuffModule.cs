using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Services;
using Olympus.Services.Training;
using Olympus.Services.Party;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.ZeusCore.Modules;

/// <summary>
/// Handles the Dragoon buff management.
/// Manages Lance Charge (personal damage), Battle Litany (party crit),
/// Life Surge (guaranteed crit), and Dragon Sight (tether buff).
/// </summary>
public sealed class BuffModule : IZeusModule
{
    public int Priority => 20; // Higher priority than damage
    public string Name => "Buff";

    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public bool TryExecute(IZeusContext context, bool isMoving)
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

        // Priority 1: Life Surge (before high-potency GCDs)
        if (TryLifeSurge(context))
            return true;

        // Priority 2: Lance Charge (personal burst)
        if (TryLanceCharge(context))
            return true;

        // Priority 3: Battle Litany (party buff)
        if (TryBattleLitany(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IZeusContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Life Surge

    private bool TryLifeSurge(IZeusContext context)
    {
        if (!context.Configuration.Dragoon.EnableLifeSurge)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DRGActions.LifeSurge.MinLevel)
            return false;

        // Already have Life Surge active
        if (context.HasLifeSurge)
        {
            context.Debug.BuffState = "Life Surge active";
            return false;
        }

        // Check if Life Surge is ready
        if (!context.ActionService.IsActionReady(DRGActions.LifeSurge.ActionId))
        {
            context.Debug.BuffState = "Life Surge on cooldown";
            return false;
        }

        // Use Life Surge before high-potency GCDs:
        // - Heavens' Thrust / Full Thrust (after Vorpal)
        // - Drakesbane / Wheeling Thrust / Fang and Claw (finisher procs)
        // - Coerthan Torment (AoE finisher)

        var shouldUseLifeSurge = false;

        // About to use a finisher proc
        if (context.HasFangAndClawBared || context.HasWheelInMotion)
        {
            shouldUseLifeSurge = true;
        }
        // About to use Heavens' Thrust / Full Thrust (after Vorpal Thrust in combo)
        else if (context.LastComboAction == DRGActions.VorpalThrust.ActionId &&
                 context.ComboTimeRemaining > 0)
        {
            shouldUseLifeSurge = true;
        }
        // About to use Coerthan Torment (after Sonic Thrust in AoE combo)
        else if (context.LastComboAction == DRGActions.SonicThrust.ActionId &&
                 context.ComboTimeRemaining > 0 &&
                 level >= DRGActions.CoerthanTorment.MinLevel)
        {
            shouldUseLifeSurge = true;
        }

        if (!shouldUseLifeSurge)
        {
            context.Debug.BuffState = "Waiting for high-potency GCD";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(DRGActions.LifeSurge, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.LifeSurge.Name;
            context.Debug.BuffState = "Activating Life Surge";

            // Training: Record Life Surge decision
            var procReason = context.HasFangAndClawBared ? "Fang and Claw ready" :
                             context.HasWheelInMotion ? "Wheeling Thrust ready" :
                             context.LastComboAction == DRGActions.VorpalThrust.ActionId ? "Heavens' Thrust coming" :
                             "Coerthan Torment coming";
            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.LifeSurge.ActionId, DRGActions.LifeSurge.Name)
                .AsMeleeBurst()
                .Target("Self")
                .Reason($"Life Surge before high-potency GCD ({procReason})",
                    "Life Surge guarantees your next GCD will critical hit. " +
                    "Always use it before your highest potency abilities: Heavens' Thrust/Full Thrust, " +
                    "Drakesbane/Fang and Claw/Wheeling Thrust, or Coerthan Torment in AoE.")
                .Factors(new[] { procReason, "Guaranteed critical hit", "40s cooldown (2 charges at Lv.88+)" })
                .Alternatives(new[] { "Use on lower potency GCD (wastes potential)", "Hold for later (might overcap charges)" })
                .Tip("Life Surge should never sit at max charges. Use it before every Heavens' Thrust or finisher proc.")
                .Concept("drg_life_surge")
                .Record();
            context.TrainingService?.RecordConceptApplication("drg_life_surge", true, "Guaranteed crit optimization");

            return true;
        }

        return false;
    }

    #endregion

    #region Lance Charge

    private bool TryLanceCharge(IZeusContext context)
    {
        if (!context.Configuration.Dragoon.EnableLanceCharge)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DRGActions.LanceCharge.MinLevel)
            return false;

        // Already have Lance Charge active
        if (context.HasLanceCharge)
        {
            context.Debug.BuffState = $"Lance Charge active ({context.LanceChargeRemaining:F1}s)";
            return false;
        }

        // Check if Lance Charge is ready
        if (!context.ActionService.IsActionReady(DRGActions.LanceCharge.ActionId))
        {
            context.Debug.BuffState = "Lance Charge on cooldown";
            return false;
        }

        // Timeline: Don't waste burst before phase transition
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Lance Charge (phase soon)";
            return false;
        }

        // Hold for burst window when burst is imminent
        if (ShouldHoldForBurst(context.Configuration.Dragoon.BattleLitanyHoldTime))
        {
            context.Debug.BuffState = "Holding Lance Charge for burst";
            return false;
        }

        // Requirements for optimal Lance Charge usage:
        // 1. Power Surge should be active (personal damage buff)
        // 2. Ideally align with Battle Litany for maximum burst
        if (!context.HasPowerSurge)
        {
            context.Debug.BuffState = "Waiting for Power Surge";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(DRGActions.LanceCharge, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.LanceCharge.Name;
            context.Debug.BuffState = "Activating Lance Charge";

            // Training: Record Lance Charge decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.LanceCharge.ActionId, DRGActions.LanceCharge.Name)
                .AsMeleeBurst()
                .Target("Self")
                .Reason("Activating Lance Charge (+10% damage for 20s)",
                    "Lance Charge is DRG's main personal damage buff (+10% for 20 seconds). " +
                    "Use it on cooldown when Power Surge is active, ideally aligned with Battle Litany " +
                    "for maximum burst damage during your Life of the Dragon phase.")
                .Factors(new[] { "Power Surge active", "60s cooldown ready", "Starting burst window" })
                .Alternatives(new[] { "Wait for Battle Litany (minor optimization)", "Wait for Life of Dragon (don't hold too long)" })
                .Tip("Lance Charge and Battle Litany should align every 2 minutes. Press them together for maximum party benefit.")
                .Concept("drg_lance_charge")
                .Record();
            context.TrainingService?.RecordConceptApplication("drg_lance_charge", true, "Personal burst activation");

            return true;
        }

        return false;
    }

    #endregion

    #region Battle Litany

    private bool TryBattleLitany(IZeusContext context)
    {
        if (!context.Configuration.Dragoon.EnableBattleLitany)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DRGActions.BattleLitany.MinLevel)
            return false;

        // Already have Battle Litany active
        if (context.HasBattleLitany)
        {
            context.Debug.BuffState = $"Battle Litany active ({context.BattleLitanyRemaining:F1}s)";
            return false;
        }

        // Check if Battle Litany is ready
        if (!context.ActionService.IsActionReady(DRGActions.BattleLitany.ActionId))
        {
            context.Debug.BuffState = "Battle Litany on cooldown";
            return false;
        }

        // Timeline: Don't waste burst before phase transition
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Battle Litany (phase soon)";
            return false;
        }

        // Hold for burst window when burst is imminent
        if (ShouldHoldForBurst(context.Configuration.Dragoon.BattleLitanyHoldTime))
        {
            context.Debug.BuffState = "Holding Battle Litany for burst";
            return false;
        }

        // Party coordination: Synchronize with other Olympus instances
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if our buffs are aligned with remote instances
            // If significantly desynced (e.g., death recovery), use independently
            if (!partyCoord.IsRaidBuffAligned(DRGActions.BattleLitany.ActionId))
            {
                context.Debug.BuffState = "Raid buffs desynced, using independently";
                // Fall through to execute - don't try to align when heavily desynced
            }
            // Check if another DPS is about to use a raid buff
            // If so, align our burst with theirs
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
            {
                // Another player is about to burst - align with them
                context.Debug.BuffState = "Aligning with party burst";
                // Fall through to execute and announce our intent
            }

            // Announce our intent to use Battle Litany
            partyCoord.AnnounceRaidBuffIntent(DRGActions.BattleLitany.ActionId);
        }

        // Battle Litany is a party buff - use with Lance Charge for maximum burst
        // But don't hold it too long (120s CD)
        var shouldUseLitany = context.HasLanceCharge ||
                              context.ActionService.IsActionReady(DRGActions.LanceCharge.ActionId);

        if (!shouldUseLitany)
        {
            // Emergency override: if Lance Charge has a long cooldown (> 30s remaining),
            // we've likely been waiting through a wipe/reset. Fire Battle Litany to avoid
            // holding it indefinitely — 30s of lost alignment is better than never using it.
            var lanceChargeCdRemaining = context.ActionService.GetCooldownRemaining(DRGActions.LanceCharge.ActionId);
            if (lanceChargeCdRemaining > 30f)
            {
                shouldUseLitany = true;
                context.Debug.BuffState = $"Lance Charge on long CD ({lanceChargeCdRemaining:F0}s) — firing Battle Litany";
            }
        }

        if (!shouldUseLitany)
        {
            // If Lance Charge is on cooldown and we don't have it, wait for alignment
            // unless we've been waiting too long
            context.Debug.BuffState = "Waiting for Lance Charge alignment";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(DRGActions.BattleLitany, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.BattleLitany.Name;
            context.Debug.BuffState = "Activating Battle Litany";

            // Notify coordination service that we used the raid buff
            partyCoord?.OnRaidBuffUsed(DRGActions.BattleLitany.ActionId, 120_000);

            // Training: Record Battle Litany decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.BattleLitany.ActionId, DRGActions.BattleLitany.Name)
                .AsRaidBuff()
                .Target("Party-wide critical hit rate buff")
                .Reason("Battle Litany is DRG's raid buff, giving +10% critical hit rate to all party members for 20 seconds.",
                    "This is one of the strongest party buffs in the game. Coordinate with other raid buffs (Divination, " +
                    "Chain Stratagem, Brotherhood) for maximum party damage during burst windows.")
                .Factors(new[] {
                    "120s cooldown ready",
                    context.HasLanceCharge ? "Aligned with Lance Charge" : "Lance Charge ready to use together",
                    "Party burst window timing"
                })
                .Alternatives(new[] { "Wait for other raid buffs (risk delaying too long)", "Use off-cooldown (minor optimization loss)" })
                .Tip("Battle Litany benefits the whole party - coordinate with other raid buffs for the biggest burst windows.")
                .Concept("drg_battle_litany")
                .Record();
            context.TrainingService?.RecordConceptApplication("drg_battle_litany", true, "Party raid buff");

            return true;
        }

        return false;
    }

    #endregion
}
