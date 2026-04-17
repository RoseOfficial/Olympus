using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.PrometheusCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.PrometheusCore.Modules;

/// <summary>
/// Handles the Machinist damage rotation.
/// Manages tool actions, Heat Blast spam during Overheated, and 1-2-3 combo.
/// Extends BaseDpsDamageModule for shared damage module patterns.
/// </summary>
public sealed class DamageModule : BaseDpsDamageModule<IPrometheusContext>, IPrometheusModule
{
    public DamageModule(IBurstWindowService? burstWindowService = null, ISmartAoEService? smartAoEService = null) : base(burstWindowService, smartAoEService) { }

    #region Abstract Method Implementations

    /// <summary>
    /// Ranged physical targeting range (25y).
    /// </summary>
    protected override float GetTargetingRange() => FFXIVConstants.RangedTargetingRange;

    /// <summary>
    /// AoE count range for MCH (12y for most AoE abilities like Spread Shot).
    /// </summary>
    protected override float GetAoECountRange() => 12f;

    /// <summary>
    /// Sets the damage state in the debug display.
    /// </summary>
    protected override void SetDamageState(IPrometheusContext context, string state) =>
        context.Debug.DamageState = state;

    /// <summary>
    /// Sets the nearby enemy count in the debug display.
    /// </summary>
    protected override void SetNearbyEnemies(IPrometheusContext context, int count) =>
        context.Debug.NearbyEnemies = count;

    /// <summary>
    /// Sets the planned action name in the debug display.
    /// </summary>
    protected override void SetPlannedAction(IPrometheusContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override bool IsAoEEnabled(IPrometheusContext context) =>
        context.Configuration.Machinist.EnableAoERotation;

    protected override int GetConfiguredAoEThreshold(IPrometheusContext context) =>
        context.Configuration.Machinist.AoEMinTargets;

    /// <summary>
    /// oGCD damage for Machinist - primarily interrupt handling.
    /// </summary>
    protected override bool TryOgcdDamage(IPrometheusContext context, IBattleChara target, int enemyCount)
    {
        // Interrupt enemy casts (highest priority)
        if (TryInterrupt(context, target))
            return true;

        return false;
    }

    /// <summary>
    /// Main GCD damage rotation for Machinist.
    /// Handles Overheated state, tool actions, and combo rotation.
    /// </summary>
    protected override bool TryGcdDamage(IPrometheusContext context, IBattleChara target, int enemyCount, bool isMoving)
    {
        // === OVERHEATED STATE ===
        if (context.IsOverheated)
        {
            if (TryOverheatedGcd(context, target, enemyCount))
                return true;
        }

        // === NORMAL STATE ===

        // Priority 1: Full Metal Field (proc from Barrel Stabilizer at Lv.100)
        if (TryFullMetalField(context, target))
            return true;

        // Priority 2: Excavator (proc from Chain Saw at Lv.96)
        if (TryExcavator(context, target))
            return true;

        // Priority 3: Drill (highest priority tool)
        if (TryDrill(context, target, enemyCount))
            return true;

        // Priority 4: Air Anchor (+20 Battery)
        if (TryAirAnchor(context, target))
            return true;

        // Priority 5: Chain Saw (+20 Battery, grants Excavator Ready)
        if (TryChainSaw(context, target))
            return true;

        // Priority 6: AoE or Single Target Combo
        if (TryCombo(context, target, enemyCount))
            return true;

        return false;
    }

    #endregion

    #region Overheated State

    private bool TryOverheatedGcd(IPrometheusContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        if (useAoe && !context.Configuration.Machinist.EnableAutoCrossbow)
            return false;
        if (!useAoe && !context.Configuration.Machinist.EnableHeatBlast)
            return false;

        var action = MCHActions.GetOverheatedGcd(level, useAoe);

        if (level < action.MinLevel)
        {
            // Fallback to Heat Blast
            action = MCHActions.HeatBlast;
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
        {
            context.Debug.DamageState = "Heat action not ready";
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (Overheat: {context.OverheatRemaining:F1}s)";

            // Training: Record Heat Blast/Auto Crossbow decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsRangedBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    $"{action.Name} (Overheat remaining: {context.OverheatRemaining:F1}s)",
                    $"{action.Name} is MCH's Overheated GCD with a 1.5s recast. Spam during Hypercharge and weave " +
                    "Gauss Round/Ricochet between each. Reduces Gauss/Ricochet cooldown by 15s per use.")
                .Factors($"Overheated: {context.OverheatRemaining:F1}s remaining", useAoe ? $"AoE mode ({enemyCount} enemies)" : "Single target")
                .Alternatives("No alternatives during Overheated")
                .Tip("During Overheated, spam Heat Blast (or Auto Crossbow for AoE) and weave oGCDs between each.")
                .Concept("mch.heat_blast_rotation")
                .Record();
            context.TrainingService?.RecordConceptApplication("mch.heat_blast_rotation", true, "Overheated rotation");
            context.TrainingService?.RecordConceptApplication("mch.overheated_state", true, "Burst phase execution");

            return true;
        }

        return false;
    }

    #endregion

    #region Tool Actions

    private bool TryFullMetalField(IPrometheusContext context, IBattleChara target)
    {
        if (!context.Configuration.Machinist.EnableFullMetalField) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < MCHActions.FullMetalField.MinLevel)
            return false;

        if (!context.HasFullMetalMachinist)
            return false;

        if (!context.ActionService.IsActionReady(MCHActions.FullMetalField.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MCHActions.FullMetalField, target.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.FullMetalField.Name;
            context.Debug.DamageState = "Full Metal Field (proc)";

            // Training: Record Full Metal Field decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MCHActions.FullMetalField.ActionId, MCHActions.FullMetalField.Name)
                .AsProc("Full Metal Machinist")
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Full Metal Field (proc from Barrel Stabilizer)",
                    "Full Metal Field is granted by Barrel Stabilizer at Lv.100. High potency AoE attack. " +
                    "Use before entering Hypercharge to avoid losing the proc during Overheated GCDs.")
                .Factors("Full Metal Machinist buff active", "Lv.100 ability")
                .Alternatives("Use Reassemble first if available")
                .Tip("Use Full Metal Field after Barrel Stabilizer, before Hypercharge. Benefits from Reassemble.")
                .Concept("mch.proc_tracking")
                .Record();
            context.TrainingService?.RecordConceptApplication("mch.proc_tracking", true, "Proc consumption");

            return true;
        }

        return false;
    }

    private bool TryExcavator(IPrometheusContext context, IBattleChara target)
    {
        if (!context.Configuration.Machinist.EnableExcavator) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < MCHActions.Excavator.MinLevel)
            return false;

        if (!context.HasExcavatorReady)
            return false;

        if (!context.ActionService.IsActionReady(MCHActions.Excavator.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MCHActions.Excavator, target.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.Excavator.Name;
            context.Debug.DamageState = "Excavator (+20 Battery)";

            // Training: Record Excavator decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MCHActions.Excavator.ActionId, MCHActions.Excavator.Name)
                .AsProc("Excavator Ready")
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Excavator (proc from Chain Saw, +20 Battery)",
                    "Excavator is granted by Chain Saw at Lv.96. High potency attack that also grants +20 Battery. " +
                    "Use before the buff expires. Benefits from Reassemble.")
                .Factors("Excavator Ready buff active", $"Battery: {context.Battery}/100", "Lv.96 ability")
                .Alternatives("Use Reassemble first if available")
                .Tip("Use Excavator after Chain Saw. Don't let the proc expire. Battery gain helps Queen summoning.")
                .Concept("mch.proc_tracking")
                .Record();
            context.TrainingService?.RecordConceptApplication("mch.proc_tracking", true, "Proc consumption");
            context.TrainingService?.RecordConceptApplication("mch.battery_accumulation", true, "Battery building");

            return true;
        }

        return false;
    }

    private bool TryDrill(IPrometheusContext context, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Machinist.EnableDrill)
            return false;

        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Use Bioblaster for AoE if available
        if (useAoe && level >= MCHActions.Bioblaster.MinLevel)
        {
            // Check if Bioblaster DoT needs refresh
            if (!context.HasBioblaster || context.BioblasterRemaining < 3f)
            {
                if (context.ActionService.IsActionReady(MCHActions.Bioblaster.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(MCHActions.Bioblaster, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = MCHActions.Bioblaster.Name;
                        context.Debug.DamageState = "Bioblaster (DoT AoE)";

                        // Training: Record Bioblaster decision
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(MCHActions.Bioblaster.ActionId, MCHActions.Bioblaster.Name)
                            .AsAoE(enemyCount)
                            .Reason(
                                $"Bioblaster (AoE DoT, {enemyCount} targets)",
                                "Bioblaster applies a DoT to enemies in a cone. Use instead of Drill in AoE situations. " +
                                "Shares recast with Drill. Refresh when DoT is about to expire (<3s).")
                            .Factors($"Enemies: {enemyCount}", context.HasBioblaster ? $"DoT: {context.BioblasterRemaining:F1}s" : "DoT not applied")
                            .Alternatives("Use Drill for single target")
                            .Tip("In AoE (3+ targets), use Bioblaster instead of Drill. Keep the DoT active.")
                            .Concept("mch.aoe_rotation")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("mch.aoe_rotation", true, "AoE tool usage");
                        context.TrainingService?.RecordConceptApplication("mch.target_count_threshold", enemyCount >= 3, "AoE threshold");

                        return true;
                    }
                }
            }
        }

        // Single target Drill
        if (level < MCHActions.Drill.MinLevel)
            return false;

        if (context.DrillCharges == 0)
            return false;

        if (!context.ActionService.IsActionReady(MCHActions.Drill.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MCHActions.Drill, target.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.Drill.Name;
            context.Debug.DamageState = $"Drill (charges: {context.DrillCharges})";

            // Training: Record Drill decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MCHActions.Drill.ActionId, MCHActions.Drill.Name)
                .AsRangedDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    $"Drill (charges: {context.DrillCharges})",
                    "Drill is MCH's highest priority tool action. Has 2 charges at Lv.98+. Always use with Reassemble " +
                    "for guaranteed crit/DH. Don't let charges overcap.")
                .Factors($"Charges: {context.DrillCharges}", context.HasReassemble ? "Reassemble active" : "No Reassemble")
                .Alternatives("Wait for Reassemble", "Use Air Anchor/Chain Saw first")
                .Tip("Drill is the best Reassemble target. Prioritize Drill over Air Anchor and Chain Saw.")
                .Concept("mch.drill_priority")
                .Record();
            context.TrainingService?.RecordConceptApplication("mch.drill_priority", true, "Tool priority");

            return true;
        }

        return false;
    }

    private bool TryAirAnchor(IPrometheusContext context, IBattleChara target)
    {
        if (!context.Configuration.Machinist.EnableAirAnchor)
            return false;

        var player = context.Player;
        var level = player.Level;

        var action = MCHActions.GetAirAnchor(level);

        if (level < action.MinLevel)
            return false;

        // Don't use if we'd overcap Battery (Air Anchor gives +20)
        if (context.Battery > 80)
        {
            context.Debug.DamageState = "Battery too high for Air Anchor";
            return false;
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (+20 Battery)";

            // Training: Record Air Anchor decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsRangedResource("Battery", context.Battery)
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    $"{action.Name} (Battery: {context.Battery} → {context.Battery + 20})",
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

            return true;
        }

        return false;
    }

    private bool TryChainSaw(IPrometheusContext context, IBattleChara target)
    {
        if (!context.Configuration.Machinist.EnableChainSaw)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < MCHActions.ChainSaw.MinLevel)
            return false;

        // Don't use if we'd overcap Battery (Chain Saw gives +20)
        if (context.Battery > 80)
        {
            context.Debug.DamageState = "Battery too high for Chain Saw";
            return false;
        }

        if (!context.ActionService.IsActionReady(MCHActions.ChainSaw.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MCHActions.ChainSaw, target.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.ChainSaw.Name;
            context.Debug.DamageState = "Chain Saw (+20 Battery)";

            // Training: Record Chain Saw decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MCHActions.ChainSaw.ActionId, MCHActions.ChainSaw.Name)
                .AsRangedResource("Battery", context.Battery)
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    $"Chain Saw (Battery: {context.Battery} → {context.Battery + 20})",
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

            return true;
        }

        return false;
    }

    #endregion

    #region Combo

    private bool TryCombo(IPrometheusContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        if (useAoe)
        {
            return TryAoeCombo(context, target);
        }

        return TrySingleTargetCombo(context, target);
    }

    private bool TrySingleTargetCombo(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine combo action based on step
        Models.Action.ActionDefinition action;
        string comboInfo;

        // Step 3: Finisher
        if (context.ComboStep == 2 &&
            (context.LastComboAction == MCHActions.HeatedSlugShot.ActionId ||
             context.LastComboAction == MCHActions.SlugShot.ActionId))
        {
            action = MCHActions.GetComboFinisher(level);
            comboInfo = $"{action.Name} (+5 Heat, +10 Battery)";
        }
        // Step 2: Second hit
        else if (context.ComboStep == 1 &&
                 (context.LastComboAction == MCHActions.HeatedSplitShot.ActionId ||
                  context.LastComboAction == MCHActions.SplitShot.ActionId))
        {
            action = MCHActions.GetComboSecond(level);
            comboInfo = $"{action.Name} (+5 Heat)";
        }
        // Step 1: Starter
        else
        {
            action = MCHActions.GetComboStarter(level);
            comboInfo = $"{action.Name} (+5 Heat)";
        }

        if (level < action.MinLevel)
        {
            // Fall back to basic action
            action = MCHActions.SplitShot;
            comboInfo = "Split Shot";
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{comboInfo} (combo {context.ComboStep + 1})";

            // Training: Record combo decision
            var isFinisher = context.ComboStep == 2;
            var conceptId = isFinisher ? "mch.gauge_interactions" : "mch.heat_gauge";
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsRangedDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    $"{action.Name} (combo step {context.ComboStep + 1})",
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

            return true;
        }

        return false;
    }

    private bool TryAoeCombo(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE is simpler - just spam Scattergun/Spread Shot
        var action = MCHActions.GetAoeAction(level);

        if (level < action.MinLevel)
        {
            // Fall back to Spread Shot
            action = MCHActions.SpreadShot;
        }

        if (level < MCHActions.SpreadShot.MinLevel)
        {
            // Too low level for AoE, use single target
            return TrySingleTargetCombo(context, target);
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (AoE)";

            // Training: Record AoE combo decision
            var aoeEnemyCount = context.TargetingService.CountEnemiesInRange(12f, player);
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsAoE(aoeEnemyCount)
                .Reason(
                    $"{action.Name} (AoE filler)",
                    "Scattergun (Lv.82+) or Spread Shot is MCH's AoE filler. Grants +10 Heat per use. " +
                    "Use at 3+ enemies instead of the single-target combo.")
                .Factors($"Enemies: {aoeEnemyCount}", $"Heat: {context.Heat}/100")
                .Alternatives("Use single-target combo for 1-2 targets")
                .Tip("At 3+ enemies, spam Scattergun instead of the combo. Builds Heat faster for Hypercharge.")
                .Concept("mch.aoe_rotation")
                .Record();
            context.TrainingService?.RecordConceptApplication("mch.aoe_rotation", true, "AoE rotation");
            context.TrainingService?.RecordConceptApplication("mch.target_count_threshold", true, "AoE threshold");

            return true;
        }

        return false;
    }

    #endregion

    #region Interrupt

    /// <summary>
    /// Attempts to interrupt an enemy cast using Head Graze.
    /// Coordinates with other Olympus instances to prevent duplicate interrupts.
    /// </summary>
    private bool TryInterrupt(IPrometheusContext context, IBattleChara target)
    {
        if (!context.Configuration.RangedShared.EnableHeadGraze)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Need at least Head Graze (Lv.24)
        if (level < RoleActions.HeadGraze.MinLevel)
            return false;

        // Check if target is casting something interruptible
        if (!target.IsCasting)
            return false;

        // Check the cast interruptible flag (game indicates this)
        if (!target.IsCastInterruptible)
            return false;

        var targetId = target.EntityId;

        // Humanize: wait a short time into the cast before interrupting (0.3–0.7s, varies per enemy/cast)
        var delaySeed = (int)(target.EntityId * 2654435761u ^ (uint)(target.TotalCastTime * 1000f));
        var interruptDelay = 0.3f + ((delaySeed & 0xFFFF) / 65535f) * 0.4f;
        if (target.CurrentCastTime < interruptDelay)
            return false;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;

        // Check IPC reservation
        if (coordConfig.EnableInterruptCoordination &&
            partyCoord?.IsInterruptTargetReservedByOther(targetId) == true)
        {
            context.Debug.DamageState = "Interrupt reserved by other";
            return false;
        }

        // Calculate remaining cast time in milliseconds
        var remainingCastTime = (target.TotalCastTime - target.CurrentCastTime) * 1000f;
        var castTimeMs = (int)remainingCastTime;

        // Try Head Graze
        if (context.ActionService.IsActionReady(RoleActions.HeadGraze.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.HeadGraze.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.DamageState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(RoleActions.HeadGraze, target.GameObjectId))
            {
                context.Debug.PlannedAction = RoleActions.HeadGraze.Name;
                context.Debug.DamageState = "Interrupted cast";

                // Training: Record Head Graze decision
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.HeadGraze.ActionId, RoleActions.HeadGraze.Name)
                    .AsInterrupt()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason(
                        "Head Graze (interrupt)",
                        "Head Graze interrupts enemy casts. Use on interruptible abilities (indicated by flashing cast bar). " +
                        "Coordinate with party to avoid wasting multiple interrupts on the same cast.")
                    .Factors("Target casting interruptible ability", "30s cooldown ready")
                    .Alternatives("Let party member interrupt")
                    .Tip("Watch for interruptible casts. Some mechanics require interrupts to avoid party damage.")
                    .Concept("mch.interrupt_usage")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mch.interrupt_usage", true, "Interrupt execution");

                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        return false;
    }

    #endregion

    #region Smart AoE

    protected override uint GetNextDirectionalAoEActionId(IPrometheusContext context, IBattleChara target, int enemyCount)
    {
        var level = context.Player.Level;

        // Chain Saw is a line AoE — only when ready (not on cooldown)
        if (level >= MCHActions.ChainSaw.MinLevel
            && context.ActionService.IsActionReady(MCHActions.ChainSaw.ActionId))
            return MCHActions.ChainSaw.ActionId;

        // Bioblaster is a line AoE DoT — when ready and multiple targets
        if (enemyCount >= 2 && level >= MCHActions.Bioblaster.MinLevel
            && context.ActionService.IsActionReady(MCHActions.Bioblaster.ActionId))
            return MCHActions.Bioblaster.ActionId;

        if (enemyCount < AoeThreshold) return 0;

        // AoE cone: Scattergun > Spread Shot (GCDs, always "ready" when GCD is up)
        if (context.CanExecuteGcd)
        {
            if (level >= MCHActions.Scattergun.MinLevel)
                return MCHActions.Scattergun.ActionId;
            if (level >= MCHActions.SpreadShot.MinLevel)
                return MCHActions.SpreadShot.ActionId;
        }

        return 0;
    }

    #endregion
}
