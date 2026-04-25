using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.PrometheusCore.Abilities;
using Olympus.Rotation.PrometheusCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.PrometheusCore.Modules;

/// <summary>
/// Handles the Machinist damage rotation (scheduler-driven).
/// Manages tool actions, Heat Blast spam during Overheated, 1-2-3 combo, Head Graze.
/// </summary>
public sealed class DamageModule : IPrometheusModule
{
    public int Priority => 30;
    public string Name => "Damage";

    private readonly IBurstWindowService? _burstWindowService;
    private readonly ISmartAoEService? _smartAoEService;

    public DamageModule(IBurstWindowService? burstWindowService = null, ISmartAoEService? smartAoEService = null)
    {
        _burstWindowService = burstWindowService;
        _smartAoEService = smartAoEService;
    }

    public bool TryExecute(IPrometheusContext context, bool isMoving) => false;

    public void UpdateDebugState(IPrometheusContext context) { }

    public void CollectCandidates(IPrometheusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return;
        }
        if (context.TargetingService.IsDamageTargetingPaused())
        {
            context.Debug.DamageState = "Paused (no target)";
            return;
        }
        if (context.Configuration.Targeting.SuppressDamageOnForcedMovement
            && PlayerSafetyHelper.IsForcedMovementActive(context.Player))
        {
            context.Debug.DamageState = "Paused (forced movement)";
            return;
        }

        var player = context.Player;
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);
        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return;
        }

        var aoeEnabled = context.Configuration.Machinist.EnableAoERotation;
        var aoeThreshold = context.Configuration.Machinist.AoEMinTargets;
        var rawEnemyCount = context.TargetingService.CountEnemiesInRange(12f, player);
        context.Debug.NearbyEnemies = rawEnemyCount;
        var enemyCount = aoeEnabled ? rawEnemyCount : 0;

        // oGCDs
        TryPushInterrupt(context, scheduler, target);

        // GCD priority order
        if (context.IsOverheated)
            TryPushOverheatedGcd(context, scheduler, target, enemyCount);
        TryPushFullMetalField(context, scheduler, target);
        TryPushExcavator(context, scheduler, target);
        TryPushDrill(context, scheduler, target, enemyCount);
        TryPushAirAnchor(context, scheduler, target);
        TryPushChainSaw(context, scheduler, target);
        TryPushCombo(context, scheduler, target, enemyCount);
    }

    #region GCDs

    private void TryPushOverheatedGcd(IPrometheusContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var aoeThreshold = context.Configuration.Machinist.AoEMinTargets;
        var useAoe = enemyCount >= aoeThreshold;
        if (useAoe && !context.Configuration.Machinist.EnableAutoCrossbow) return;
        if (!useAoe && !context.Configuration.Machinist.EnableHeatBlast) return;

        var action = MCHActions.GetOverheatedGcd((byte)level, useAoe);
        if (level < action.MinLevel) action = MCHActions.HeatBlast;
        var ability = useAoe ? PrometheusAbilities.AutoCrossbow : PrometheusAbilities.HeatBlast;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : action.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(ability, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (Overheat: {context.OverheatRemaining:F1}s)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsRangedBurst()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"{action.Name} (Overheat remaining: {context.OverheatRemaining:F1}s)",
                        $"{action.Name} is MCH's Overheated GCD with a 1.5s recast. Spam during Hypercharge and weave " +
                        "Gauss Round/Ricochet between each. Reduces Gauss/Ricochet cooldown by 15s per use.")
                    .Factors($"Overheated: {context.OverheatRemaining:F1}s remaining", useAoe ? $"AoE mode ({enemyCount} enemies)" : "Single target")
                    .Alternatives("No alternatives during Overheated")
                    .Tip("During Overheated, spam Heat Blast (or Auto Crossbow for AoE) and weave oGCDs between each.")
                    .Concept("mch.heat_blast_rotation")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.heat_blast_rotation", true, "Overheated rotation");
                context.TrainingService?.RecordConceptApplication("mch.overheated_state", true, "Burst phase execution");
            });
    }

    private void TryPushFullMetalField(IPrometheusContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Machinist.EnableFullMetalField) return;
        var player = context.Player;
        if (player.Level < MCHActions.FullMetalField.MinLevel) return;
        if (!context.HasFullMetalMachinist) return;
        if (!context.ActionService.IsActionReady(MCHActions.FullMetalField.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : MCHActions.FullMetalField.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(PrometheusAbilities.FullMetalField, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MCHActions.FullMetalField.Name;
                context.Debug.DamageState = "Full Metal Field (proc)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(MCHActions.FullMetalField.ActionId, MCHActions.FullMetalField.Name)
                    .AsProc("Full Metal Machinist")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Full Metal Field (proc from Barrel Stabilizer)",
                        "Full Metal Field is granted by Barrel Stabilizer at Lv.100. High potency AoE attack. " +
                        "Use before entering Hypercharge to avoid losing the proc during Overheated GCDs.")
                    .Factors("Full Metal Machinist buff active", "Lv.100 ability")
                    .Alternatives("Use Reassemble first if available")
                    .Tip("Use Full Metal Field after Barrel Stabilizer, before Hypercharge. Benefits from Reassemble.")
                    .Concept("mch.proc_tracking")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.proc_tracking", true, "Proc consumption");
            });
    }

    private void TryPushExcavator(IPrometheusContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Machinist.EnableExcavator) return;
        var player = context.Player;
        if (player.Level < MCHActions.Excavator.MinLevel) return;
        if (!context.HasExcavatorReady) return;
        if (!context.ActionService.IsActionReady(MCHActions.Excavator.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : MCHActions.Excavator.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(PrometheusAbilities.Excavator, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MCHActions.Excavator.Name;
                context.Debug.DamageState = "Excavator (+20 Battery)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(MCHActions.Excavator.ActionId, MCHActions.Excavator.Name)
                    .AsProc("Excavator Ready")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Excavator (proc from Chain Saw, +20 Battery)",
                        "Excavator is granted by Chain Saw at Lv.96. High potency attack that also grants +20 Battery. " +
                        "Use before the buff expires. Benefits from Reassemble.")
                    .Factors("Excavator Ready buff active", $"Battery: {context.Battery}/100", "Lv.96 ability")
                    .Alternatives("Use Reassemble first if available")
                    .Tip("Use Excavator after Chain Saw. Don't let the proc expire. Battery gain helps Queen summoning.")
                    .Concept("mch.proc_tracking")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.proc_tracking", true, "Proc consumption");
                context.TrainingService?.RecordConceptApplication("mch.battery_accumulation", true, "Battery building");
            });
    }

    private void TryPushDrill(IPrometheusContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Machinist.EnableDrill) return;
        var player = context.Player;
        var level = player.Level;
        var aoeThreshold = context.Configuration.Machinist.AoEMinTargets;
        var useAoe = enemyCount >= aoeThreshold;

        if (useAoe && level >= MCHActions.Bioblaster.MinLevel
            && (!context.HasBioblaster || context.BioblasterRemaining < 3f)
            && context.ActionService.IsActionReady(MCHActions.Bioblaster.ActionId))
        {
            var castTime = context.HasSwiftcast ? 0f : MCHActions.Bioblaster.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }

            scheduler.PushGcd(PrometheusAbilities.Bioblaster, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = MCHActions.Bioblaster.Name;
                    context.Debug.DamageState = "Bioblaster (DoT AoE)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(MCHActions.Bioblaster.ActionId, MCHActions.Bioblaster.Name)
                        .AsAoE(enemyCount)
                        .Reason($"Bioblaster (AoE DoT, {enemyCount} targets)",
                            "Bioblaster applies a DoT to enemies in a cone. Use instead of Drill in AoE situations. " +
                            "Shares recast with Drill. Refresh when DoT is about to expire (<3s).")
                        .Factors($"Enemies: {enemyCount}", context.HasBioblaster ? $"DoT: {context.BioblasterRemaining:F1}s" : "DoT not applied")
                        .Alternatives("Use Drill for single target")
                        .Tip("In AoE (3+ targets), use Bioblaster instead of Drill. Keep the DoT active.")
                        .Concept("mch.aoe_rotation")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("mch.aoe_rotation", true, "AoE tool usage");
                    context.TrainingService?.RecordConceptApplication("mch.target_count_threshold", enemyCount >= 3, "AoE threshold");
                });
            return;
        }

        if (level < MCHActions.Drill.MinLevel) return;
        if (context.DrillCharges == 0) return;
        if (!context.ActionService.IsActionReady(MCHActions.Drill.ActionId)) return;
        var stCastTime = context.HasSwiftcast ? 0f : MCHActions.Drill.CastTime;
        if (MechanicCastGate.ShouldBlock(context, stCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(PrometheusAbilities.Drill, target.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MCHActions.Drill.Name;
                context.Debug.DamageState = $"Drill (charges: {context.DrillCharges})";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(MCHActions.Drill.ActionId, MCHActions.Drill.Name)
                    .AsRangedDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Drill (charges: {context.DrillCharges})",
                        "Drill is MCH's highest priority tool action. Has 2 charges at Lv.98+. Always use with Reassemble " +
                        "for guaranteed crit/DH. Don't let charges overcap.")
                    .Factors($"Charges: {context.DrillCharges}", context.HasReassemble ? "Reassemble active" : "No Reassemble")
                    .Alternatives("Wait for Reassemble", "Use Air Anchor/Chain Saw first")
                    .Tip("Drill is the best Reassemble target. Prioritize Drill over Air Anchor and Chain Saw.")
                    .Concept("mch.drill_priority")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.drill_priority", true, "Tool priority");
            });
    }

    private void TryPushAirAnchor(IPrometheusContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Machinist.EnableAirAnchor) return;
        var player = context.Player;
        var level = player.Level;
        var action = MCHActions.GetAirAnchor((byte)level);
        if (level < action.MinLevel) return;
        if (context.Battery > 80) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : action.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(PrometheusAbilities.AirAnchor, target.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (+20 Battery)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsRangedResource("Battery", context.Battery)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"{action.Name} (Battery: {context.Battery} → {context.Battery + 20})",
                        "Air Anchor is a high-potency tool that grants +20 Battery. Use on cooldown, but check Battery " +
                        "to avoid overcapping. Benefits from Reassemble (after Drill).")
                    .Factors($"Battery: {context.Battery}/100", "Won't overcap Battery", context.HasReassemble ? "Reassemble active" : "No Reassemble")
                    .Alternatives("Wait if Battery > 80", "Prioritize Drill")
                    .Tip("Air Anchor builds Battery for Queen. Don't use if Battery > 80 to avoid overcapping.")
                    .Concept("mch.air_anchor_usage")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.air_anchor_usage", true, "Tool usage");
                context.TrainingService?.RecordConceptApplication("mch.battery_accumulation", true, "Battery building");
                context.TrainingService?.RecordConceptApplication("mch.gauge_overcapping", context.Battery <= 80, "Overcap prevention");
            });
    }

    private void TryPushChainSaw(IPrometheusContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Machinist.EnableChainSaw) return;
        var player = context.Player;
        if (player.Level < MCHActions.ChainSaw.MinLevel) return;
        if (context.Battery > 80) return;
        if (!context.ActionService.IsActionReady(MCHActions.ChainSaw.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : MCHActions.ChainSaw.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(PrometheusAbilities.ChainSaw, target.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = MCHActions.ChainSaw.Name;
                context.Debug.DamageState = "Chain Saw (+20 Battery)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(MCHActions.ChainSaw.ActionId, MCHActions.ChainSaw.Name)
                    .AsRangedResource("Battery", context.Battery)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Chain Saw (Battery: {context.Battery} → {context.Battery + 20})",
                        "Chain Saw is a high-potency tool that grants +20 Battery and Excavator Ready (Lv.96+). " +
                        "Use on cooldown, but check Battery to avoid overcapping. Benefits from Reassemble.")
                    .Factors($"Battery: {context.Battery}/100", "Won't overcap Battery", "Grants Excavator Ready")
                    .Alternatives("Wait if Battery > 80", "Prioritize Drill")
                    .Tip("Chain Saw grants Excavator Ready. Use Excavator before the buff expires for extra damage + Battery.")
                    .Concept("mch.chain_saw_usage")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.chain_saw_usage", true, "Tool usage");
                context.TrainingService?.RecordConceptApplication("mch.battery_accumulation", true, "Battery building");
                context.TrainingService?.RecordConceptApplication("mch.gauge_overcapping", context.Battery <= 80, "Overcap prevention");
            });
    }

    private void TryPushCombo(IPrometheusContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var aoeThreshold = context.Configuration.Machinist.AoEMinTargets;
        var useAoe = enemyCount >= aoeThreshold;

        if (useAoe && level >= MCHActions.SpreadShot.MinLevel)
        {
            var aoeAction = MCHActions.GetAoeAction((byte)level);
            if (level < aoeAction.MinLevel) aoeAction = MCHActions.SpreadShot;
            if (!context.ActionService.IsActionReady(aoeAction.ActionId)) return;
            var castTime = context.HasSwiftcast ? 0f : aoeAction.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }

            scheduler.PushGcd(PrometheusAbilities.SpreadShot, player.GameObjectId, priority: 8,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = aoeAction.Name;
                    context.Debug.DamageState = $"{aoeAction.Name} (AoE)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(aoeAction.ActionId, aoeAction.Name)
                        .AsAoE(enemyCount)
                        .Reason($"{aoeAction.Name} (AoE filler)",
                            "Scattergun (Lv.82+) or Spread Shot is MCH's AoE filler. Grants +10 Heat per use. " +
                            "Use at 3+ enemies instead of the single-target combo.")
                        .Factors($"Enemies: {enemyCount}", $"Heat: {context.Heat}/100")
                        .Alternatives("Use single-target combo for 1-2 targets")
                        .Tip("At 3+ enemies, spam Scattergun instead of the combo. Builds Heat faster for Hypercharge.")
                        .Concept("mch.aoe_rotation")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("mch.aoe_rotation", true, "AoE rotation");
                    context.TrainingService?.RecordConceptApplication("mch.target_count_threshold", true, "AoE threshold");
                });
            return;
        }

        // ST combo: pick the right step
        ActionDefinition action;
        AbilityBehavior ability;
        bool isFinisher = false;
        if (context.ComboStep == 2
            && (context.LastComboAction == MCHActions.HeatedSlugShot.ActionId
                || context.LastComboAction == MCHActions.SlugShot.ActionId))
        {
            action = MCHActions.GetComboFinisher((byte)level);
            ability = PrometheusAbilities.CleanShot;
            isFinisher = true;
        }
        else if (context.ComboStep == 1
            && (context.LastComboAction == MCHActions.HeatedSplitShot.ActionId
                || context.LastComboAction == MCHActions.SplitShot.ActionId))
        {
            action = MCHActions.GetComboSecond((byte)level);
            ability = PrometheusAbilities.SlugShot;
        }
        else
        {
            action = MCHActions.GetComboStarter((byte)level);
            ability = PrometheusAbilities.SplitShot;
        }

        if (level < action.MinLevel) { action = MCHActions.SplitShot; ability = PrometheusAbilities.SplitShot; }
        if (!context.ActionService.IsActionReady(action.ActionId)) return;
        var comboCastTime = context.HasSwiftcast ? 0f : action.CastTime;
        if (MechanicCastGate.ShouldBlock(context, comboCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(ability, target.GameObjectId, priority: 9,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (combo {context.ComboStep + 1})";

                var conceptId = isFinisher ? "mch.gauge_interactions" : "mch.heat_gauge";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsRangedDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"{action.Name} (combo step {context.ComboStep + 1})",
                        isFinisher
                            ? "Clean Shot is the combo finisher. Grants +5 Heat and +10 Battery. Complete the combo to maximize gauge generation."
                            : "MCH's 1-2-3 combo builds Heat (+5 per hit) for Hypercharge. Keep the combo rolling between tool actions.")
                    .Factors($"Combo step: {context.ComboStep + 1}", $"Heat: {context.Heat}/100", isFinisher ? $"Battery: {context.Battery}/100" : "")
                    .Alternatives("Use tool actions when ready")
                    .Tip(isFinisher
                        ? "Clean Shot grants +10 Battery on top of +5 Heat. Don't drop the combo."
                        : "Keep the combo going between tool actions. Each hit grants +5 Heat toward Hypercharge.")
                    .Concept(conceptId)
                    .Record();
                context.TrainingService?.RecordConceptApplication(conceptId, true, "Combo execution");
            });
    }

    #endregion

    #region Interrupt

    private void TryPushInterrupt(IPrometheusContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.RangedShared.EnableHeadGraze) return;
        var player = context.Player;
        if (player.Level < RoleActions.HeadGraze.MinLevel) return;
        if (!target.IsCasting) return;
        if (!target.IsCastInterruptible) return;

        var targetId = target.EntityId;
        var delaySeed = (int)(target.EntityId * 2654435761u ^ (uint)(target.TotalCastTime * 1000f));
        var interruptDelay = 0.3f + ((delaySeed & 0xFFFF) / 65535f) * 0.4f;
        if (target.CurrentCastTime < interruptDelay) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableInterruptCoordination
            && partyCoord?.IsInterruptTargetReservedByOther(targetId) == true)
        {
            context.Debug.DamageState = "Interrupt reserved by other";
            return;
        }
        if (!context.ActionService.IsActionReady(RoleActions.HeadGraze.ActionId)) return;

        var remainingCastTime = (target.TotalCastTime - target.CurrentCastTime) * 1000f;
        var castTimeMs = (int)remainingCastTime;
        if (coordConfig.EnableInterruptCoordination)
        {
            if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.HeadGraze.ActionId, castTimeMs) ?? false)
            {
                context.Debug.DamageState = "Failed to reserve interrupt";
                return;
            }
        }

        scheduler.PushOgcd(PrometheusAbilities.HeadGraze, target.GameObjectId, priority: 0,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.HeadGraze.Name;
                context.Debug.DamageState = "Interrupted cast";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.HeadGraze.ActionId, RoleActions.HeadGraze.Name)
                    .AsInterrupt()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Head Graze (interrupt)",
                        "Head Graze interrupts enemy casts. Use on interruptible abilities (indicated by flashing cast bar). " +
                        "Coordinate with party to avoid wasting multiple interrupts on the same cast.")
                    .Factors("Target casting interruptible ability", "30s cooldown ready")
                    .Alternatives("Let party member interrupt")
                    .Tip("Watch for interruptible casts. Some mechanics require interrupts to avoid party damage.")
                    .Concept("mch.interrupt_usage")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.interrupt_usage", true, "Interrupt execution");
            });
    }

    #endregion
}
