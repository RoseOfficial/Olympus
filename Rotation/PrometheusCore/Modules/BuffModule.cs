using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.PrometheusCore.Abilities;
using Olympus.Rotation.PrometheusCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.PrometheusCore.Modules;

/// <summary>
/// Handles Machinist buff management and oGCD optimization (scheduler-driven).
/// Manages Wildfire, Barrel Stabilizer, Reassemble, Hypercharge, Queen, Gauss/Ricochet.
/// </summary>
public sealed class BuffModule : IPrometheusModule
{
    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool IsInBurst => BurstHoldHelper.IsInBurst(_burstWindowService);
    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public int Priority => 20;
    public string Name => "Buff";

    public bool TryExecute(IPrometheusContext context, bool isMoving) => false;

    public void UpdateDebugState(IPrometheusContext context) { }

    public void CollectCandidates(IPrometheusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }

        var player = context.Player;
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);
        if (target == null)
        {
            context.Debug.BuffState = "No target";
            return;
        }

        TryPushWildfire(context, scheduler, target);
        TryPushBarrelStabilizer(context, scheduler);
        TryPushReassemble(context, scheduler);
        TryPushHypercharge(context, scheduler);
        TryPushAutomatonQueen(context, scheduler);
        TryPushGaussRoundRicochet(context, scheduler, target);
    }

    private void TryPushWildfire(IPrometheusContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Machinist.EnableWildfire) return;
        var player = context.Player;
        if (player.Level < MCHActions.Wildfire.MinLevel) return;
        if (context.HasWildfire) return;

        var shouldUse = context.IsOverheated
                        || (context.Heat >= 50 && context.ActionService.IsActionReady(MCHActions.Hypercharge.ActionId));
        if (!shouldUse)
        {
            context.Debug.BuffState = "Waiting for Hypercharge alignment";
            return;
        }
        if (!context.ActionService.IsActionReady(MCHActions.Wildfire.ActionId)) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Wildfire (phase soon)";
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
                context.Debug.BuffState = "Aligning Wildfire with party burst";
            partyCoord.AnnounceRaidBuffIntent(MCHActions.Wildfire.ActionId);
        }

        scheduler.PushOgcd(PrometheusAbilities.Wildfire, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MCHActions.Wildfire.Name;
                context.Debug.BuffState = "Wildfire applied";
                partyCoord?.OnRaidBuffUsed(MCHActions.Wildfire.ActionId, 120_000);

                TrainingHelper.Decision(context.TrainingService)
                    .Action(MCHActions.Wildfire.ActionId, MCHActions.Wildfire.Name)
                    .AsRangedBurst()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Wildfire applied (Hypercharge burst window)",
                        "Wildfire is MCH's 2-minute burst. It accumulates damage based on weapon skills landed during its duration. " +
                        "Always pair with Hypercharge to maximize Heat Blast hits. 10s duration, aim for 6 GCDs inside.")
                    .Factors(context.IsOverheated ? "Overheated active" : "Heat >= 50", "Hypercharge ready/active", "120s cooldown ready")
                    .Alternatives("Hold for party raid buffs", "Hold for phase timing")
                    .Tip("Wildfire counts GCDs landed. Use with Hypercharge for 5-6 Heat Blasts inside the window.")
                    .Concept("mch.wildfire_placement")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.wildfire_placement", true, "Burst window activation");
                context.TrainingService?.RecordConceptApplication("mch.burst_party_sync", true, "Party coordination");
            });
    }

    private void TryPushBarrelStabilizer(IPrometheusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Machinist.EnableBarrelStabilizer) return;
        var player = context.Player;
        if (player.Level < MCHActions.BarrelStabilizer.MinLevel) return;
        if (context.Heat > 70) return;
        if (!context.ActionService.IsActionReady(MCHActions.BarrelStabilizer.ActionId)) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Barrel Stabilizer (phase soon)";
            return;
        }

        scheduler.PushOgcd(PrometheusAbilities.BarrelStabilizer, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MCHActions.BarrelStabilizer.Name;
                context.Debug.BuffState = "Barrel Stabilizer used (+50 Heat)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(MCHActions.BarrelStabilizer.ActionId, MCHActions.BarrelStabilizer.Name)
                    .AsRangedResource("Heat", context.Heat)
                    .Reason($"Barrel Stabilizer used (Heat: {context.Heat} → {context.Heat + 50})",
                        "Barrel Stabilizer grants +50 Heat instantly, enabling Hypercharge. At Lv.100, also grants Full Metal Machinist " +
                        "for Full Metal Field. Use on cooldown, but avoid overcapping Heat above 50.")
                    .Factors($"Heat: {context.Heat}/100", "120s cooldown ready", "Won't overcap Heat")
                    .Alternatives("Wait if Heat > 50", "Hold for phase timing")
                    .Tip("Use Barrel Stabilizer on cooldown. At Lv.100, follow with Full Metal Field before Hypercharge.")
                    .Concept("mch.heat_gauge")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.heat_gauge", true, "Heat generation");
                context.TrainingService?.RecordConceptApplication("mch.gauge_overcapping", context.Heat <= 50, "Overcap prevention");
            });
    }

    private void TryPushReassemble(IPrometheusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Machinist.EnableReassemble) return;
        var player = context.Player;
        var level = player.Level;
        if (level < MCHActions.Reassemble.MinLevel) return;
        if (context.HasReassemble) return;
        if (context.ReassembleCharges == 0) return;
        if (context.IsOverheated) return;

        var strategy = context.Configuration.Machinist.ReassembleStrategy;
        if (strategy == ReassembleStrategy.Delay) return;

        var enemyCount = context.TargetingService.CountEnemiesInRange(12f, player);
        var nextGcdId = PredictNextGcd(context, enemyCount);

        var nextIsBlessed = IsBlessedTool(nextGcdId);
        var nextIsAnyWeaponskill = nextGcdId != 0 && !IsOverheatedGcd(nextGcdId);
        var atMaxCharges = context.ReassembleCharges >= 2;

        bool shouldUse = strategy switch
        {
            ReassembleStrategy.Automatic => nextIsBlessed || (atMaxCharges && nextIsAnyWeaponskill),
            ReassembleStrategy.Any => nextIsAnyWeaponskill,
            ReassembleStrategy.HoldOne => nextIsBlessed && atMaxCharges,
            _ => false,
        };
        if (!shouldUse) return;
        if (!context.ActionService.IsActionReady(MCHActions.Reassemble.ActionId)) return;

        scheduler.PushOgcd(PrometheusAbilities.Reassemble, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MCHActions.Reassemble.Name;
                context.Debug.BuffState = nextIsBlessed
                    ? $"Reassemble → next GCD (charges: {context.ReassembleCharges})"
                    : $"Reassemble (overcap save, charges: {context.ReassembleCharges})";

                var reason = nextIsBlessed ? "High-potency next GCD" : "Preventing charge overcap";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(MCHActions.Reassemble.ActionId, MCHActions.Reassemble.Name)
                    .AsRangedBurst()
                    .Target(player.Name?.TextValue ?? "Self")
                    .Reason($"Reassemble activated ({reason})",
                        "Reassemble guarantees critical direct hit on next weaponskill. Pairs best with Drill, Air Anchor, Chain Saw, " +
                        "Excavator, or Full Metal Field. Has 2 charges at Lv.84+, avoid overcapping.")
                    .Factors($"Charges: {context.ReassembleCharges}", reason, $"Strategy: {strategy}")
                    .Alternatives("Save for higher potency action", "Switch strategy to HoldOne to keep a manual charge")
                    .Tip("Use Reassemble before Drill (highest priority), then Air Anchor, Chain Saw, Excavator, or Full Metal Field.")
                    .Concept("mch.reassemble_priority")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.reassemble_priority", nextIsBlessed, "Optimal Reassemble target");
                context.TrainingService?.RecordConceptApplication("mch.reassemble_charges", !atMaxCharges, "Charge management");
            });
    }

    private uint PredictNextGcd(IPrometheusContext context, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var cfg = context.Configuration.Machinist;
        var useAoe = cfg.EnableAoERotation && enemyCount >= cfg.AoEMinTargets;

        if (context.IsOverheated)
        {
            if (useAoe && cfg.EnableAutoCrossbow)
                return MCHActions.GetOverheatedGcd((byte)level, true).ActionId;
            if (!useAoe && cfg.EnableHeatBlast)
                return MCHActions.HeatBlast.ActionId;
            return 0;
        }

        if (cfg.EnableFullMetalField && context.HasFullMetalMachinist
            && level >= MCHActions.FullMetalField.MinLevel
            && context.ActionService.IsActionReady(MCHActions.FullMetalField.ActionId))
            return MCHActions.FullMetalField.ActionId;

        if (cfg.EnableExcavator && context.HasExcavatorReady
            && level >= MCHActions.Excavator.MinLevel
            && context.ActionService.IsActionReady(MCHActions.Excavator.ActionId))
            return MCHActions.Excavator.ActionId;

        if (cfg.EnableDrill)
        {
            if (useAoe && level >= MCHActions.Bioblaster.MinLevel
                && (!context.HasBioblaster || context.BioblasterRemaining < 3f)
                && context.ActionService.IsActionReady(MCHActions.Bioblaster.ActionId))
                return MCHActions.Bioblaster.ActionId;
            if (level >= MCHActions.Drill.MinLevel && context.DrillCharges > 0
                && context.ActionService.IsActionReady(MCHActions.Drill.ActionId))
                return MCHActions.Drill.ActionId;
        }

        if (cfg.EnableAirAnchor)
        {
            var aa = MCHActions.GetAirAnchor((byte)level);
            if (level >= aa.MinLevel && context.Battery <= 80
                && context.ActionService.IsActionReady(aa.ActionId))
                return aa.ActionId;
        }

        if (cfg.EnableChainSaw && level >= MCHActions.ChainSaw.MinLevel && context.Battery <= 80
            && context.ActionService.IsActionReady(MCHActions.ChainSaw.ActionId))
            return MCHActions.ChainSaw.ActionId;

        if (useAoe && level >= MCHActions.SpreadShot.MinLevel)
            return MCHActions.GetAoeAction((byte)level).ActionId;
        if (context.ComboStep == 2) return MCHActions.GetComboFinisher((byte)level).ActionId;
        if (context.ComboStep == 1) return MCHActions.GetComboSecond((byte)level).ActionId;
        return MCHActions.GetComboStarter((byte)level).ActionId;
    }

    private static bool IsBlessedTool(uint actionId)
    {
        if (actionId == 0) return false;
        return actionId == MCHActions.Drill.ActionId
            || actionId == MCHActions.Bioblaster.ActionId
            || actionId == MCHActions.AirAnchor.ActionId
            || actionId == MCHActions.HotShot.ActionId
            || actionId == MCHActions.ChainSaw.ActionId
            || actionId == MCHActions.Excavator.ActionId
            || actionId == MCHActions.FullMetalField.ActionId;
    }

    private static bool IsOverheatedGcd(uint actionId) =>
        actionId == MCHActions.HeatBlast.ActionId
        || actionId == MCHActions.BlazingShot.ActionId
        || actionId == MCHActions.AutoCrossbow.ActionId;

    private void TryPushHypercharge(IPrometheusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Machinist.EnableHypercharge) return;
        var player = context.Player;
        var level = player.Level;
        if (level < MCHActions.Hypercharge.MinLevel) return;
        if (context.IsOverheated) return;
        if (context.Heat < 50) return;
        if (context.Configuration.Machinist.EnableBurstPooling && ShouldHoldForBurst(8f) && context.Heat < 90)
        {
            context.Debug.BuffState = $"Holding Hypercharge for burst ({context.Heat} Heat)";
            return;
        }

        var hyperchargeWindow = 10f;
        bool toolComingDuringWindow = false;
        if (level >= MCHActions.Drill.MinLevel)
        {
            var cd = context.ActionService.GetCooldownRemaining(MCHActions.Drill.ActionId);
            if (cd > 0 && cd < hyperchargeWindow) toolComingDuringWindow = true;
        }
        if (level >= MCHActions.AirAnchor.MinLevel)
        {
            var cd = context.ActionService.GetCooldownRemaining(MCHActions.AirAnchor.ActionId);
            if (cd > 0 && cd < hyperchargeWindow) toolComingDuringWindow = true;
        }
        if (level >= MCHActions.ChainSaw.MinLevel)
        {
            var cd = context.ActionService.GetCooldownRemaining(MCHActions.ChainSaw.ActionId);
            if (cd > 0 && cd < hyperchargeWindow) toolComingDuringWindow = true;
        }
        if (toolComingDuringWindow)
        {
            context.Debug.BuffState = "Holding Hypercharge (tool coming off CD)";
            return;
        }
        if (!context.ActionService.IsActionReady(MCHActions.Hypercharge.ActionId)) return;

        scheduler.PushOgcd(PrometheusAbilities.Hypercharge, player.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MCHActions.Hypercharge.Name;
                context.Debug.BuffState = "Hypercharge activated";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(MCHActions.Hypercharge.ActionId, MCHActions.Hypercharge.Name)
                    .AsRangedBurst()
                    .Target(player.Name?.TextValue ?? "Self")
                    .Reason($"Hypercharge activated (Heat: {context.Heat} → {context.Heat - 50})",
                        "Hypercharge spends 50 Heat to enter Overheated state for ~10s. During Overheated, use Heat Blast (1.5s GCD) " +
                        "and weave Gauss Round/Ricochet. Always pair with Wildfire for maximum burst.")
                    .Factors($"Heat: {context.Heat}/100", "No tool actions imminent", "Wildfire window optimal")
                    .Alternatives("Wait for Drill/Air Anchor/Chain Saw", "Hold for Wildfire alignment")
                    .Tip("Enter Hypercharge when tools are on cooldown. Spam Heat Blast and weave oGCDs during Overheated.")
                    .Concept("mch.hypercharge_activation")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.hypercharge_activation", true, "Burst phase entry");
                context.TrainingService?.RecordConceptApplication("mch.hypercharge_timing", true, "Tool cooldown check");
            });
    }

    private void TryPushAutomatonQueen(IPrometheusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Machinist.EnableAutomatonQueen) return;
        var player = context.Player;
        var level = player.Level;
        var petAction = MCHActions.GetPetSummon((byte)level);
        if (level < petAction.MinLevel) return;
        if (context.IsQueenActive) return;

        var batteryMin = context.Configuration.Machinist.BatteryMinGauge;
        var batteryOvercap = context.Configuration.Machinist.BatteryOvercapThreshold;
        if (context.Battery < batteryMin) return;

        bool shouldSummon = context.Battery >= batteryOvercap || context.Battery >= 100;
        if (!shouldSummon && IsInBurst && context.Battery >= batteryMin) shouldSummon = true;
        if (!shouldSummon && context.Configuration.Machinist.SaveBatteryForBurst
            && ShouldHoldForBurst(8f) && context.Battery < batteryOvercap) return;
        if (!shouldSummon) return;
        if (!context.ActionService.IsActionReady(petAction.ActionId)) return;

        scheduler.PushOgcd(PrometheusAbilities.AutomatonQueen, player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = petAction.Name;
                context.Debug.BuffState = $"{petAction.Name} summoned ({context.Battery} Battery)";

                var batteryReason = context.Battery >= 100 ? "Maximum Battery"
                                  : context.Battery >= 90 ? "Near-maximum Battery" : "Preventing overcap";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(petAction.ActionId, petAction.Name)
                    .AsRangedResource("Battery", context.Battery)
                    .Reason($"{petAction.Name} summoned ({batteryReason})",
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
            });
    }

    private void TryPushGaussRoundRicochet(IPrometheusContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Machinist.EnableGaussRicochet) return;
        var player = context.Player;
        var level = player.Level;

        var gaussAction = MCHActions.GetGaussRound((byte)level);
        var ricochetAction = MCHActions.GetRicochet((byte)level);

        if (level >= gaussAction.MinLevel && context.GaussRoundCharges > 0)
        {
            bool shouldUse = context.IsOverheated || context.GaussRoundCharges >= 2;
            if (shouldUse && context.ActionService.IsActionReady(gaussAction.ActionId))
            {
                scheduler.PushOgcd(PrometheusAbilities.GaussRound, target.GameObjectId, priority: 6,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = gaussAction.Name;
                        context.Debug.BuffState = $"{gaussAction.Name} (charges: {context.GaussRoundCharges})";

                        var gaussReason = context.IsOverheated ? "Weaving during Overheated" : "Preventing charge overcap";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(gaussAction.ActionId, gaussAction.Name)
                            .AsRangedDamage()
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"{gaussAction.Name} ({gaussReason})",
                                "Gauss Round is a charge-based oGCD (3 charges max). Weave between Heat Blasts during Overheated. " +
                                "Heat Blast reduces its cooldown, so dump charges aggressively during Hypercharge.")
                            .Factors($"Charges: {context.GaussRoundCharges}/3", gaussReason)
                            .Alternatives("Save for Overheated phase", "Alternate with Ricochet")
                            .Tip("During Overheated, alternate Gauss Round and Ricochet between Heat Blasts for optimal weaving.")
                            .Concept("mch.ogcd_weaving")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("mch.ogcd_weaving", context.IsOverheated, "Overheated weaving");
                    });
            }
        }

        if (level >= ricochetAction.MinLevel && context.RicochetCharges > 0)
        {
            bool shouldUse = context.IsOverheated || context.RicochetCharges >= 2;
            if (shouldUse && context.ActionService.IsActionReady(ricochetAction.ActionId))
            {
                scheduler.PushOgcd(PrometheusAbilities.Ricochet, target.GameObjectId, priority: 6,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = ricochetAction.Name;
                        context.Debug.BuffState = $"{ricochetAction.Name} (charges: {context.RicochetCharges})";

                        var ricochetReason = context.IsOverheated ? "Weaving during Overheated" : "Preventing charge overcap";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(ricochetAction.ActionId, ricochetAction.Name)
                            .AsRangedDamage()
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"{ricochetAction.Name} ({ricochetReason})",
                                "Ricochet is a charge-based AoE oGCD (3 charges max). Weave between Heat Blasts during Overheated. " +
                                "Heat Blast reduces its cooldown, so dump charges aggressively during Hypercharge.")
                            .Factors($"Charges: {context.RicochetCharges}/3", ricochetReason)
                            .Alternatives("Save for Overheated phase", "Alternate with Gauss Round")
                            .Tip("During Overheated, alternate Ricochet and Gauss Round between Heat Blasts for optimal weaving.")
                            .Concept("mch.ogcd_weaving")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("mch.ogcd_weaving", context.IsOverheated, "Overheated weaving");
                    });
            }
        }
    }
}
