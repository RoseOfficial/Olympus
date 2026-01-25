using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.CalliopeCore.Modules;

/// <summary>
/// Handles the Bard damage rotation.
/// Manages procs, DoTs, Apex Arrow, and filler GCDs.
/// </summary>
public sealed class DamageModule : ICalliopeModule
{
    public int Priority => 30; // Lowest priority - damage after buffs
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    // DoT refresh window (refresh between 3-7s remaining)
    private const float DotRefreshMin = 3f;
    private const float DotRefreshMax = 7f;

    public bool TryExecute(ICalliopeContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // oGCD: Interrupt enemy casts (highest priority)
        if (context.CanExecuteOgcd && TryInterrupt(context, target))
            return true;

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(12f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // GCD Phase
        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        // === PROC PRIORITY (highest damage GCDs) ===

        // Priority 1: Resonant Arrow (after Barrage)
        if (TryResonantArrow(context, target))
            return true;

        // Priority 2: Radiant Encore (after Radiant Finale)
        if (TryRadiantEncore(context, target))
            return true;

        // Priority 3: Blast Arrow (after 80+ Soul Voice Apex Arrow)
        if (TryBlastArrow(context, target))
            return true;

        // Priority 4: Refulgent Arrow with Barrage (triple damage)
        if (TryBarragedRefulgent(context, target, enemyCount))
            return true;

        // Priority 5: Refulgent Arrow (Hawk's Eye proc)
        if (TryRefulgentArrow(context, target, enemyCount))
            return true;

        // === RESOURCE SPENDERS ===

        // Priority 6: Apex Arrow at 80+ during burst, or 100 to avoid overcap
        if (TryApexArrow(context, target))
            return true;

        // === DoT MANAGEMENT ===

        // Priority 7: Iron Jaws to refresh DoTs (or snapshot buffs)
        if (TryIronJaws(context, target))
            return true;

        // Priority 8: Apply DoTs if missing
        if (TryApplyDots(context, target))
            return true;

        // === FILLER ===

        // Priority 9: Filler GCD (Burst Shot / Heavy Shot)
        if (TryFiller(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(ICalliopeContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Procs

    private bool TryResonantArrow(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.ResonantArrow.MinLevel)
            return false;

        if (!context.HasResonantArrowReady)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.ResonantArrow.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BRDActions.ResonantArrow, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.ResonantArrow.Name;
            context.Debug.DamageState = "Resonant Arrow (Barrage follow-up)";

            // Training: Record Resonant Arrow decision
            RangedDpsTrainingHelper.RecordProcDecision(
                context.TrainingService,
                BRDActions.ResonantArrow.ActionId,
                BRDActions.ResonantArrow.Name,
                "Resonant Arrow Ready",
                target.Name?.TextValue ?? "Target",
                "Resonant Arrow (Barrage follow-up)",
                "Resonant Arrow is granted after using Barraged Refulgent Arrow. High potency proc that must be used " +
                "before it expires. Always part of the Barrage burst sequence.",
                new[] { "Resonant Arrow Ready active", "Barrage sequence" },
                new[] { "No alternatives - must use before expiring" },
                "After Barrage → Refulgent Arrow, always follow with Resonant Arrow immediately.",
                BrdConcepts.ResonantArrow);
            context.TrainingService?.RecordConceptApplication(BrdConcepts.ResonantArrow, true, "Proc consumption");
            context.TrainingService?.RecordConceptApplication(BrdConcepts.Barrage, true, "Barrage sequence completion");

            return true;
        }

        return false;
    }

    private bool TryRadiantEncore(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.RadiantEncore.MinLevel)
            return false;

        if (!context.HasRadiantEncoreReady)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.RadiantEncore.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BRDActions.RadiantEncore, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.RadiantEncore.Name;
            context.Debug.DamageState = "Radiant Encore (RF follow-up)";

            // Training: Record Radiant Encore decision
            RangedDpsTrainingHelper.RecordProcDecision(
                context.TrainingService,
                BRDActions.RadiantEncore.ActionId,
                BRDActions.RadiantEncore.Name,
                "Radiant Encore Ready",
                target.Name?.TextValue ?? "Target",
                "Radiant Encore (Radiant Finale follow-up)",
                "Radiant Encore is granted after using Radiant Finale. Potency scales with Coda used (same as RF). " +
                "Must use before it expires. Part of the 2-minute burst sequence.",
                new[] { "Radiant Encore Ready active", "Radiant Finale sequence" },
                new[] { "No alternatives - must use before expiring" },
                "After Radiant Finale, use Radiant Encore during the burst window for extra damage.",
                BrdConcepts.RadiantEncore);
            context.TrainingService?.RecordConceptApplication(BrdConcepts.RadiantEncore, true, "Proc consumption");

            return true;
        }

        return false;
    }

    private bool TryBlastArrow(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.BlastArrow.MinLevel)
            return false;

        if (!context.HasBlastArrowReady)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.BlastArrow.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BRDActions.BlastArrow, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.BlastArrow.Name;
            context.Debug.DamageState = "Blast Arrow (Apex follow-up)";

            // Training: Record Blast Arrow decision
            RangedDpsTrainingHelper.RecordProcDecision(
                context.TrainingService,
                BRDActions.BlastArrow.ActionId,
                BRDActions.BlastArrow.Name,
                "Blast Arrow Ready",
                target.Name?.TextValue ?? "Target",
                "Blast Arrow (Apex Arrow follow-up)",
                "Blast Arrow is granted after using Apex Arrow at 80+ Soul Voice. Very high potency follow-up. " +
                "Always use immediately after Apex Arrow. Part of the Soul Voice spending sequence.",
                new[] { "Blast Arrow Ready active", "Apex Arrow sequence" },
                new[] { "No alternatives - must use before expiring" },
                "After Apex Arrow (80+ SV), always follow with Blast Arrow for massive damage.",
                BrdConcepts.BlastArrow);
            context.TrainingService?.RecordConceptApplication(BrdConcepts.BlastArrow, true, "Proc consumption");
            context.TrainingService?.RecordConceptApplication(BrdConcepts.SoulVoiceGauge, true, "Soul Voice spending");

            return true;
        }

        return false;
    }

    private bool TryBarragedRefulgent(ICalliopeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Must have Barrage active
        if (!context.HasBarrage)
            return false;

        // Must have Hawk's Eye for Refulgent
        if (!context.HasHawksEye)
            return false;

        // For AoE, Barrage still best used on Refulgent (higher potency per hit)
        var action = BRDActions.GetProcAction(level);

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = $"Barrage + {action.Name}";
            context.Debug.DamageState = "Barraged Refulgent Arrow";

            // Training: Record Barraged Refulgent decision
            RangedDpsTrainingHelper.RecordProcDecision(
                context.TrainingService,
                action.ActionId,
                $"Barrage + {action.Name}",
                "Barrage + Hawk's Eye",
                target.Name?.TextValue ?? "Target",
                "Barraged Refulgent Arrow (triple damage!)",
                "Barraged Refulgent Arrow hits 3 times for massive burst damage. This is BRD's biggest single hit. " +
                "Always wait for Hawk's Eye proc after Barrage. Grants Resonant Arrow Ready.",
                new[] { "Barrage active", "Hawk's Eye (Straight Shot Ready) active", context.HasRagingStrikes ? "RS active" : "" },
                new[] { "No alternatives - this is the optimal Barrage usage" },
                "Barrage + Refulgent Arrow is your biggest burst. Always use during Raging Strikes.",
                BrdConcepts.Barrage);
            context.TrainingService?.RecordConceptApplication(BrdConcepts.Barrage, true, "Barrage consumption");
            context.TrainingService?.RecordConceptApplication(BrdConcepts.RefulgentArrow, true, "Proc usage with Barrage");
            context.TrainingService?.RecordConceptApplication(BrdConcepts.StraightShotReady, true, "Hawk's Eye consumption");

            return true;
        }

        return false;
    }

    private bool TryRefulgentArrow(ICalliopeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (!context.HasHawksEye)
            return false;

        // Use Shadowbite for AoE
        if (enemyCount >= AoeThreshold && level >= BRDActions.Shadowbite.MinLevel)
        {
            if (context.ActionService.IsActionReady(BRDActions.Shadowbite.ActionId))
            {
                if (context.ActionService.ExecuteGcd(BRDActions.Shadowbite, target.GameObjectId))
                {
                    context.Debug.PlannedAction = BRDActions.Shadowbite.Name;
                    context.Debug.DamageState = "Shadowbite (AoE proc)";

                    // Training: Record Shadowbite decision
                    RangedDpsTrainingHelper.RecordAoeDecision(
                        context.TrainingService,
                        BRDActions.Shadowbite.ActionId,
                        BRDActions.Shadowbite.Name,
                        enemyCount,
                        $"Shadowbite (AoE proc, {enemyCount} targets)",
                        "Shadowbite is the AoE version of Refulgent Arrow, consuming Hawk's Eye. " +
                        "Use at 3+ targets instead of Refulgent Arrow for better total damage.",
                        new[] { "Hawk's Eye active", $"Enemies: {enemyCount}" },
                        new[] { "Use Refulgent for single target" },
                        "At 3+ enemies, consume Hawk's Eye procs with Shadowbite instead of Refulgent Arrow.",
                        BrdConcepts.StraightShotReady);
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.StraightShotReady, true, "AoE proc usage");

                    return true;
                }
            }
        }

        // Single target proc
        var action = BRDActions.GetProcAction(level);

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = "Refulgent Arrow (proc)";

            // Training: Record Refulgent Arrow decision
            RangedDpsTrainingHelper.RecordProcDecision(
                context.TrainingService,
                action.ActionId,
                action.Name,
                "Hawk's Eye",
                target.Name?.TextValue ?? "Target",
                "Refulgent Arrow (Hawk's Eye proc)",
                "Refulgent Arrow is BRD's proc GCD, replacing Burst Shot when Hawk's Eye (Straight Shot Ready) is active. " +
                "Higher potency than Burst Shot. Procs randomly from Burst Shot or guaranteed from song mechanics.",
                new[] { "Hawk's Eye active", "Single target" },
                new[] { "Continue using Burst Shot if no proc" },
                "Always use Refulgent Arrow when Hawk's Eye procs. It's free extra damage.",
                BrdConcepts.RefulgentArrow);
            context.TrainingService?.RecordConceptApplication(BrdConcepts.RefulgentArrow, true, "Proc consumption");
            context.TrainingService?.RecordConceptApplication(BrdConcepts.StraightShotReady, true, "Hawk's Eye consumption");

            return true;
        }

        return false;
    }

    #endregion

    #region Apex Arrow

    private bool TryApexArrow(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.ApexArrow.MinLevel)
            return false;

        // Use at 80+ during burst for Blast Arrow follow-up
        // Or use at 100 to avoid overcapping
        bool shouldUse = false;

        if (context.SoulVoice >= 100)
        {
            shouldUse = true;
        }
        else if (context.SoulVoice >= 80)
        {
            // During burst window or no buffs to wait for
            shouldUse = context.HasRagingStrikes ||
                        !context.ActionService.IsActionReady(BRDActions.RagingStrikes.ActionId);
        }

        if (!shouldUse)
        {
            context.Debug.DamageState = $"Apex Arrow: {context.SoulVoice}/80";
            return false;
        }

        if (!context.ActionService.IsActionReady(BRDActions.ApexArrow.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BRDActions.ApexArrow, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.ApexArrow.Name;
            context.Debug.DamageState = $"Apex Arrow ({context.SoulVoice} SV)";

            // Training: Record Apex Arrow decision
            var reason = context.SoulVoice >= 100 ? "Preventing overcap" :
                         context.HasRagingStrikes ? "Burst window" : "80+ Soul Voice";
            RangedDpsTrainingHelper.RecordResourceDecision(
                context.TrainingService,
                BRDActions.ApexArrow.ActionId,
                BRDActions.ApexArrow.Name,
                "Soul Voice",
                context.SoulVoice,
                $"Apex Arrow ({context.SoulVoice} SV, {reason})",
                "Apex Arrow spends Soul Voice gauge. Use at 80+ during burst for Blast Arrow follow-up (highest potency). " +
                "Use at 100 to prevent overcapping. Soul Voice builds from song Repertoire procs.",
                new[] { $"Soul Voice: {context.SoulVoice}/100", context.HasRagingStrikes ? "RS active" : "No burst buffs", "Grants Blast Arrow Ready at 80+" },
                new[] { "Wait for burst window", "Wait for 100 SV to avoid overcap" },
                "Use Apex Arrow at 80+ during burst windows, or at 100 to prevent overcapping. Always follow with Blast Arrow.",
                BrdConcepts.ApexArrow);
            context.TrainingService?.RecordConceptApplication(BrdConcepts.ApexArrow, true, "Soul Voice spending");
            context.TrainingService?.RecordConceptApplication(BrdConcepts.SoulVoiceGauge, true, "Gauge management");
            context.TrainingService?.RecordConceptApplication(BrdConcepts.SoulVoiceOvercapping, context.SoulVoice >= 100, "Overcap prevention");

            return true;
        }

        return false;
    }

    #endregion

    #region DoT Management

    private bool TryIronJaws(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.IronJaws.MinLevel)
            return false;

        // Need both DoTs on target to refresh
        if (!context.HasCausticBite || !context.HasStormbite)
            return false;

        // Refresh in the window (3-7s remaining)
        bool needsRefresh = context.CausticBiteRemaining <= DotRefreshMax ||
                            context.StormbiteRemaining <= DotRefreshMax;

        // Snapshot buffs with Iron Jaws during burst
        bool snapshotBuffs = context.HasRagingStrikes &&
                             context.CausticBiteRemaining < 20f; // Don't clip too early

        if (!needsRefresh && !snapshotBuffs)
            return false;

        // Don't let DoTs fall off
        if (context.CausticBiteRemaining < DotRefreshMin || context.StormbiteRemaining < DotRefreshMin)
            needsRefresh = true;

        if (!needsRefresh && !snapshotBuffs)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.IronJaws.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BRDActions.IronJaws, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.IronJaws.Name;
            var reason = snapshotBuffs ? "snapshot buffs" : "refresh";
            context.Debug.DamageState = $"Iron Jaws ({reason})";

            // Training: Record Iron Jaws decision
            var minDotRemaining = System.Math.Min(context.CausticBiteRemaining, context.StormbiteRemaining);
            RangedDpsTrainingHelper.RecordDotDecision(
                context.TrainingService,
                BRDActions.IronJaws.ActionId,
                BRDActions.IronJaws.Name,
                target.Name?.TextValue ?? "Target",
                minDotRemaining,
                $"Iron Jaws ({reason}, DoTs: {minDotRemaining:F1}s)",
                snapshotBuffs
                    ? "Iron Jaws refreshes both DoTs and snapshots current buffs. Use during Raging Strikes to extend buffed DoTs. " +
                      "This is a DPS gain even if DoTs have significant time remaining."
                    : "Iron Jaws refreshes both Caustic Bite and Stormbite with a single GCD. " +
                      "Refresh between 3-7s remaining to avoid letting DoTs fall off or clipping too early.",
                new[] { $"Caustic Bite: {context.CausticBiteRemaining:F1}s", $"Stormbite: {context.StormbiteRemaining:F1}s", snapshotBuffs ? "RS active - snapshotting" : "Normal refresh" },
                new[] { "Wait for DoTs to drop lower", "Apply DoTs manually if missing" },
                snapshotBuffs
                    ? "Snapshot Raging Strikes with Iron Jaws for 20s of buffed DoT damage."
                    : "Refresh DoTs with Iron Jaws between 3-7s remaining.",
                BrdConcepts.IronJaws);
            context.TrainingService?.RecordConceptApplication(BrdConcepts.IronJaws, true, snapshotBuffs ? "Buff snapshot" : "DoT refresh");

            return true;
        }

        return false;
    }

    private bool TryApplyDots(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Apply Stormbite first (higher potency DoT)
        if (!context.HasStormbite && level >= BRDActions.Windbite.MinLevel)
        {
            var stormAction = BRDActions.GetStormbite(level);
            if (context.ActionService.IsActionReady(stormAction.ActionId))
            {
                if (context.ActionService.ExecuteGcd(stormAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = stormAction.Name;
                    context.Debug.DamageState = $"{stormAction.Name} applied";

                    // Training: Record Stormbite decision
                    RangedDpsTrainingHelper.RecordDotDecision(
                        context.TrainingService,
                        stormAction.ActionId,
                        stormAction.Name,
                        target.Name?.TextValue ?? "Target",
                        0f,
                        $"{stormAction.Name} applied (higher potency DoT)",
                        "Stormbite (Windbite upgrade) is BRD's higher potency DoT. Apply first when DoTs are missing. " +
                        "Both DoTs snapshot buffs when applied. Maintain 100% uptime on both DoTs.",
                        new[] { "DoT not on target", "Applied before Caustic Bite" },
                        new[] { "Use Iron Jaws if both DoTs present" },
                        "Apply Stormbite first, then Caustic Bite. Maintain both DoTs at all times.",
                        BrdConcepts.Stormbite);
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.Stormbite, true, "DoT application");

                    return true;
                }
            }
        }

        // Apply Caustic Bite
        if (!context.HasCausticBite && level >= BRDActions.VenomousBite.MinLevel)
        {
            var causticAction = BRDActions.GetCausticBite(level);
            if (context.ActionService.IsActionReady(causticAction.ActionId))
            {
                if (context.ActionService.ExecuteGcd(causticAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = causticAction.Name;
                    context.Debug.DamageState = $"{causticAction.Name} applied";

                    // Training: Record Caustic Bite decision
                    RangedDpsTrainingHelper.RecordDotDecision(
                        context.TrainingService,
                        causticAction.ActionId,
                        causticAction.Name,
                        target.Name?.TextValue ?? "Target",
                        0f,
                        $"{causticAction.Name} applied (poison DoT)",
                        "Caustic Bite (Venomous Bite upgrade) is BRD's poison DoT. Apply second after Stormbite. " +
                        "Both DoTs snapshot buffs when applied. Maintain 100% uptime on both DoTs.",
                        new[] { "DoT not on target", "Applied after Stormbite" },
                        new[] { "Use Iron Jaws if both DoTs present" },
                        "Apply Caustic Bite after Stormbite. Keep both DoTs up at all times.",
                        BrdConcepts.CausticBite);
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.CausticBite, true, "DoT application");

                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Filler

    private bool TryFiller(ICalliopeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE filler
        if (enemyCount >= AoeThreshold && level >= BRDActions.QuickNock.MinLevel)
        {
            var aoeAction = BRDActions.GetAoeFiller(level);
            if (context.ActionService.IsActionReady(aoeAction.ActionId))
            {
                if (context.ActionService.ExecuteGcd(aoeAction, player.GameObjectId))
                {
                    context.Debug.PlannedAction = aoeAction.Name;
                    context.Debug.DamageState = $"{aoeAction.Name} (AoE filler)";

                    // Training: Record AoE filler decision
                    RangedDpsTrainingHelper.RecordAoeDecision(
                        context.TrainingService,
                        aoeAction.ActionId,
                        aoeAction.Name,
                        enemyCount,
                        $"{aoeAction.Name} (AoE filler, {enemyCount} targets)",
                        "Ladonsbite (Quick Nock upgrade) is BRD's AoE filler GCD. Use at 3+ enemies. " +
                        "Can proc Hawk's Eye for Shadowbite. Use Rain of Death for oGCDs in AoE.",
                        new[] { $"Enemies: {enemyCount}", "No higher priority actions" },
                        new[] { "Use Burst Shot for single target" },
                        "At 3+ enemies, spam Ladonsbite as your filler GCD instead of Burst Shot.",
                        BrdConcepts.StraightShotReady);
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.StraightShotReady, false, "AoE filler usage");

                    return true;
                }
            }
        }

        // Single target filler
        var action = BRDActions.GetFiller(level);

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (filler)";

            // Training: Record filler decision
            RangedDpsTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                action.ActionId,
                action.Name,
                target.Name?.TextValue ?? "Target",
                $"{action.Name} (single target filler)",
                "Burst Shot (Heavy Shot upgrade) is BRD's single target filler GCD. Use when no procs are active " +
                "and no higher priority actions are available. Can proc Hawk's Eye for Refulgent Arrow.",
                new[] { "No procs active", "DoTs maintained", "No higher priority actions" },
                new[] { "Use Refulgent Arrow if Hawk's Eye procs" },
                "Burst Shot is your filler. It can proc Hawk's Eye, enabling Refulgent Arrow.",
                BrdConcepts.StraightShotReady);
            context.TrainingService?.RecordConceptApplication(BrdConcepts.StraightShotReady, false, "Filler usage");

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
    private bool TryInterrupt(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Need at least Head Graze (Lv.24)
        if (level < BRDActions.HeadGraze.MinLevel)
            return false;

        // Check if target is casting something interruptible
        if (!target.IsCasting)
            return false;

        // Check the cast interruptible flag (game indicates this)
        if (!target.IsCastInterruptible)
            return false;

        var targetId = target.EntityId;
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
        if (context.ActionService.IsActionReady(BRDActions.HeadGraze.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, BRDActions.HeadGraze.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.DamageState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(BRDActions.HeadGraze, target.GameObjectId))
            {
                context.Debug.PlannedAction = BRDActions.HeadGraze.Name;
                context.Debug.DamageState = "Interrupted cast";

                // Training: Record Head Graze decision
                RangedDpsTrainingHelper.RecordUtilityDecision(
                    context.TrainingService,
                    BRDActions.HeadGraze.ActionId,
                    BRDActions.HeadGraze.Name,
                    target.Name?.TextValue ?? "Target",
                    "Head Graze (interrupt)",
                    "Head Graze interrupts enemy casts. Use on interruptible abilities (indicated by flashing cast bar). " +
                    "Coordinate with party to avoid wasting multiple interrupts on the same cast.",
                    new[] { "Target casting interruptible ability", "30s cooldown ready" },
                    new[] { "Let party member interrupt" },
                    "Watch for interruptible casts. Some mechanics require interrupts to avoid party damage.",
                    BrdConcepts.PartyUtility);
                context.TrainingService?.RecordConceptApplication(BrdConcepts.PartyUtility, true, "Interrupt execution");

                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        return false;
    }

    #endregion
}
