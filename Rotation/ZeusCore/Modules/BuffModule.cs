using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ZeusCore.Abilities;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Services;
using Olympus.Services.Training;
using Olympus.Services.Party;

namespace Olympus.Rotation.ZeusCore.Modules;

/// <summary>
/// Handles the Dragoon buff management (scheduler-driven).
/// Manages Lance Charge (personal damage), Battle Litany (party crit), and Life Surge.
/// </summary>
public sealed class BuffModule : IZeusModule
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

    public bool TryExecute(IZeusContext context, bool isMoving) => false;

    public void UpdateDebugState(IZeusContext context) { }

    public void CollectCandidates(IZeusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }

        TryPushLifeSurge(context, scheduler);
        TryPushLanceCharge(context, scheduler);
        TryPushBattleLitany(context, scheduler);
    }

    private void TryPushLifeSurge(IZeusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Dragoon.EnableLifeSurge) return;

        var player = context.Player;
        var level = player.Level;
        if (level < DRGActions.LifeSurge.MinLevel) return;

        if (context.HasLifeSurge)
        {
            context.Debug.BuffState = "Life Surge active";
            return;
        }

        if (!context.ActionService.IsActionReady(DRGActions.LifeSurge.ActionId))
        {
            context.Debug.BuffState = "Life Surge on cooldown";
            return;
        }

        var shouldUseLifeSurge = false;
        if (context.HasFangAndClawBared || context.HasWheelInMotion)
            shouldUseLifeSurge = true;
        else if (context.LastComboAction == DRGActions.VorpalThrust.ActionId &&
                 context.ComboTimeRemaining > 0)
            shouldUseLifeSurge = true;
        else if (context.LastComboAction == DRGActions.SonicThrust.ActionId &&
                 context.ComboTimeRemaining > 0 &&
                 level >= DRGActions.CoerthanTorment.MinLevel)
            shouldUseLifeSurge = true;

        if (!shouldUseLifeSurge)
        {
            context.Debug.BuffState = "Waiting for high-potency GCD";
            return;
        }

        scheduler.PushOgcd(ZeusAbilities.LifeSurge, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRGActions.LifeSurge.Name;
                context.Debug.BuffState = "Activating Life Surge";

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
            });
    }

    private void TryPushLanceCharge(IZeusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Dragoon.EnableLanceCharge) return;

        var player = context.Player;
        var level = player.Level;
        if (level < DRGActions.LanceCharge.MinLevel) return;

        if (context.HasLanceCharge)
        {
            context.Debug.BuffState = $"Lance Charge active ({context.LanceChargeRemaining:F1}s)";
            return;
        }

        if (!context.ActionService.IsActionReady(DRGActions.LanceCharge.ActionId))
        {
            context.Debug.BuffState = "Lance Charge on cooldown";
            return;
        }

        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Lance Charge (phase soon)";
            return;
        }

        if (ShouldHoldForBurst(context.Configuration.Dragoon.BattleLitanyHoldTime))
        {
            context.Debug.BuffState = "Holding Lance Charge for burst";
            return;
        }

        if (!context.HasPowerSurge)
        {
            context.Debug.BuffState = "Waiting for Power Surge";
            return;
        }

        scheduler.PushOgcd(ZeusAbilities.LanceCharge, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRGActions.LanceCharge.Name;
                context.Debug.BuffState = "Activating Lance Charge";

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
            });
    }

    private void TryPushBattleLitany(IZeusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Dragoon.EnableBattleLitany) return;

        var player = context.Player;
        var level = player.Level;
        if (level < DRGActions.BattleLitany.MinLevel) return;

        if (context.HasBattleLitany)
        {
            context.Debug.BuffState = $"Battle Litany active ({context.BattleLitanyRemaining:F1}s)";
            return;
        }

        if (!context.ActionService.IsActionReady(DRGActions.BattleLitany.ActionId))
        {
            context.Debug.BuffState = "Battle Litany on cooldown";
            return;
        }

        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Battle Litany (phase soon)";
            return;
        }

        if (ShouldHoldForBurst(context.Configuration.Dragoon.BattleLitanyHoldTime))
        {
            context.Debug.BuffState = "Holding Battle Litany for burst";
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            if (!partyCoord.IsRaidBuffAligned(DRGActions.BattleLitany.ActionId))
            {
                context.Debug.BuffState = "Raid buffs desynced, using independently";
            }
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
            {
                context.Debug.BuffState = "Aligning with party burst";
            }

            partyCoord.AnnounceRaidBuffIntent(DRGActions.BattleLitany.ActionId);
        }

        var shouldUseLitany = context.HasLanceCharge ||
                              context.ActionService.IsActionReady(DRGActions.LanceCharge.ActionId);

        if (!shouldUseLitany)
        {
            var lanceChargeCdRemaining = context.ActionService.GetCooldownRemaining(DRGActions.LanceCharge.ActionId);
            if (lanceChargeCdRemaining > 30f)
            {
                shouldUseLitany = true;
                context.Debug.BuffState = $"Lance Charge on long CD ({lanceChargeCdRemaining:F0}s) — firing Battle Litany";
            }
        }

        if (!shouldUseLitany)
        {
            context.Debug.BuffState = "Waiting for Lance Charge alignment";
            return;
        }

        scheduler.PushOgcd(ZeusAbilities.BattleLitany, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRGActions.BattleLitany.Name;
                context.Debug.BuffState = "Activating Battle Litany";

                partyCoord?.OnRaidBuffUsed(DRGActions.BattleLitany.ActionId, 120_000);

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
            });
    }
}
