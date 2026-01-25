using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.CirceCore.Modules;

/// <summary>
/// Handles the Red Mage damage rotation.
/// Manages Dualcast flow, melee combo, finishers, and mana balance.
/// </summary>
public sealed class DamageModule : ICirceModule
{
    public int Priority => 30; // Lower priority than buffs (higher number = lower priority)
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(ICirceContext context, bool isMoving)
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
            FFXIVConstants.CasterTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        var useAoe = enemyCount >= AoeThreshold;

        // === PRIORITY 1: RESOLUTION (after Scorch) ===
        if (context.IsResolutionReady && level >= RDMActions.Resolution.MinLevel)
        {
            if (TryResolution(context, target))
                return true;
        }

        // === PRIORITY 2: SCORCH (after Verflare/Verholy) ===
        if (context.IsScorchReady && level >= RDMActions.Scorch.MinLevel)
        {
            if (TryScorch(context, target))
                return true;
        }

        // === PRIORITY 3: GRAND IMPACT (when ready) ===
        if (context.IsGrandImpactReady && level >= RDMActions.GrandImpact.MinLevel)
        {
            if (TryGrandImpact(context, target))
                return true;
        }

        // === PRIORITY 4: VERFLARE/VERHOLY (after Redoublement) ===
        if (context.IsFinisherReady && level >= RDMActions.Verflare.MinLevel)
        {
            if (TryFinisher(context, target))
                return true;
        }

        // === PRIORITY 5: MELEE COMBO (Enchanted Riposte → Zwerchhau → Redoublement) ===
        if (context.IsInMeleeCombo)
        {
            if (TryMeleeCombo(context, target))
                return true;
        }

        // === PRIORITY 6: START MELEE COMBO (at 50|50 mana) ===
        if (context.CanStartMeleeCombo && !context.IsInMeleeCombo)
        {
            if (TryStartMeleeCombo(context, target))
                return true;
        }

        // === PRIORITY 7: DUALCAST CONSUMER (Verthunder/Veraero or Proc) ===
        if (context.HasDualcast || context.HasSwiftcast || context.HasAcceleration)
        {
            if (TryDualcastConsumer(context, target, useAoe))
                return true;
        }

        // === PRIORITY 8: HARDCAST FILLER (Jolt or AoE) ===
        if (TryHardcastFiller(context, target, useAoe, isMoving))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(ICirceContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Finisher Sequence

    private bool TryResolution(ICirceContext context, IBattleChara target)
    {
        if (context.ActionService.ExecuteGcd(RDMActions.Resolution, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Resolution.Name;
            context.Debug.DamageState = "Resolution (final finisher)";

            // Training Mode integration
            CasterTrainingHelper.RecordBurstDecision(
                context.TrainingService,
                RDMActions.Resolution.ActionId,
                RDMActions.Resolution.Name,
                target.Name?.TextValue,
                "Resolution - final finisher",
                "Resolution is the final GCD in the finisher sequence after Scorch. It deals massive " +
                "damage and completes your melee combo burst. Always use when available.",
                new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        "After Scorch", "Finisher sequence complete" },
                new[] { "Must use - combo will drop" },
                "Resolution is your finisher's finisher. Never let the combo drop before using it.",
                RdmConcepts.ScorchResolution);

            context.TrainingService?.RecordConceptApplication(RdmConcepts.ScorchResolution, true, "Resolution completed finisher");

            return true;
        }
        return false;
    }

    private bool TryScorch(ICirceContext context, IBattleChara target)
    {
        if (context.ActionService.ExecuteGcd(RDMActions.Scorch, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Scorch.Name;
            context.Debug.DamageState = "Scorch (after Verflare/Verholy)";

            // Training Mode integration
            CasterTrainingHelper.RecordBurstDecision(
                context.TrainingService,
                RDMActions.Scorch.ActionId,
                RDMActions.Scorch.Name,
                target.Name?.TextValue,
                "Scorch - post-finisher burst",
                "Scorch becomes available after using Verflare or Verholy. It's a high-damage GCD " +
                "that leads into Resolution. Use immediately to continue the burst.",
                new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        "After Verflare/Verholy", "Resolution follows" },
                new[] { "Must use - combo will drop" },
                "Scorch is mandatory after Verflare/Verholy. It sets up Resolution for massive damage.",
                RdmConcepts.ScorchResolution);

            context.TrainingService?.RecordConceptApplication(RdmConcepts.ScorchResolution, true, "Scorch used in finisher");

            return true;
        }
        return false;
    }

    private bool TryGrandImpact(ICirceContext context, IBattleChara target)
    {
        if (context.ActionService.ExecuteGcd(RDMActions.GrandImpact, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.GrandImpact.Name;
            context.Debug.DamageState = "Grand Impact";

            // Training Mode integration
            CasterTrainingHelper.RecordBurstDecision(
                context.TrainingService,
                RDMActions.GrandImpact.ActionId,
                RDMActions.GrandImpact.Name,
                target.Name?.TextValue,
                "Grand Impact - special proc",
                "Grand Impact becomes available from Acceleration III procs. It's a powerful instant " +
                "GCD that should be used when ready. Don't waste the proc.",
                new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        "Grand Impact Ready active" },
                new[] { "Must use before proc expires" },
                "Grand Impact is free damage from Acceleration. Use it as soon as it procs.",
                RdmConcepts.GrandImpact);

            context.TrainingService?.RecordConceptApplication(RdmConcepts.GrandImpact, true, "Grand Impact proc consumed");

            return true;
        }
        return false;
    }

    private bool TryFinisher(ICirceContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        // Select finisher based on mana balance
        // Use the one that generates the LOWER mana type
        var finisher = RDMActions.GetFinisher(level, context.BlackMana, context.WhiteMana);

        if (context.ActionService.ExecuteGcd(finisher, target.GameObjectId))
        {
            context.Debug.PlannedAction = finisher.Name;
            context.Debug.DamageState = $"{finisher.Name} (finisher)";

            // Training Mode integration
            var isVerflare = finisher.ActionId == RDMActions.Verflare.ActionId;
            CasterTrainingHelper.RecordBurstDecision(
                context.TrainingService,
                finisher.ActionId,
                finisher.Name,
                target.Name?.TextValue,
                $"{finisher.Name} - melee finisher",
                $"{finisher.Name} is your melee combo finisher after Redoublement. Choose based on mana: " +
                $"Verflare adds Black Mana, Verholy adds White Mana. Pick the lower one to stay balanced.",
                new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        isVerflare ? "Chose Verflare (Black lower)" : "Chose Verholy (White lower)",
                        "Leads to Scorch → Resolution" },
                new[] { isVerflare ? "Use Verholy instead" : "Use Verflare instead" },
                "Pick the finisher that generates your lower mana type to maintain balance.",
                RdmConcepts.FinisherSelection);

            context.TrainingService?.RecordConceptApplication(RdmConcepts.FinisherSelection, true, "Correct finisher chosen");

            return true;
        }
        return false;
    }

    #endregion

    #region Melee Combo

    private bool TryMeleeCombo(ICirceContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        // Determine which combo action is next based on combo state
        switch (context.MeleeComboStep)
        {
            case 1: // After Riposte, use Zwerchhau
                if (level >= RDMActions.EnchantedZwerchhau.MinLevel)
                {
                    if (context.ActionService.ExecuteGcd(RDMActions.EnchantedZwerchhau, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = RDMActions.EnchantedZwerchhau.Name;
                        context.Debug.DamageState = "Enchanted Zwerchhau (combo 2)";

                        // Training Mode integration
                        CasterTrainingHelper.RecordMeleeComboDecision(
                            context.TrainingService,
                            RDMActions.EnchantedZwerchhau.ActionId,
                            RDMActions.EnchantedZwerchhau.Name,
                            target.Name?.TextValue,
                            2,
                            "Zwerchhau - combo step 2",
                            "Enchanted Zwerchhau is the second hit in your melee combo. It costs 15 " +
                            "Black and White Mana. Continue the combo to Redoublement.",
                            new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                                    "Step 2 of 3", "Redoublement next" },
                            new[] { "Continue combo", "Don't drop it" },
                            "Always complete your melee combo. Dropping it wastes mana and DPS.",
                            RdmConcepts.ComboProgression);

                        context.TrainingService?.RecordConceptApplication(RdmConcepts.ComboProgression, true, "Combo step 2 executed");

                        return true;
                    }
                }
                break;

            case 2: // After Zwerchhau, use Redoublement
                if (level >= RDMActions.EnchantedRedoublement.MinLevel)
                {
                    if (context.ActionService.ExecuteGcd(RDMActions.EnchantedRedoublement, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = RDMActions.EnchantedRedoublement.Name;
                        context.Debug.DamageState = "Enchanted Redoublement (combo 3)";

                        // Training Mode integration
                        CasterTrainingHelper.RecordMeleeComboDecision(
                            context.TrainingService,
                            RDMActions.EnchantedRedoublement.ActionId,
                            RDMActions.EnchantedRedoublement.Name,
                            target.Name?.TextValue,
                            3,
                            "Redoublement - combo step 3",
                            "Enchanted Redoublement is the final hit in your melee combo. It costs 15 " +
                            "Black and White Mana. This leads into Verflare/Verholy finisher.",
                            new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                                    "Step 3 of 3", "Finisher next" },
                            new[] { "Use finisher immediately" },
                            "After Redoublement, use Verflare/Verholy based on your lower mana.",
                            RdmConcepts.ComboProgression);

                        context.TrainingService?.RecordConceptApplication(RdmConcepts.ComboProgression, true, "Combo step 3 executed");

                        return true;
                    }
                }
                break;
        }

        return false;
    }

    private bool TryStartMeleeCombo(ICirceContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        // Check conditions for entering melee combo
        // Prefer to enter during burst windows or when mana is high
        var inBurst = context.HasEmbolden || context.HasManafication;
        var highMana = context.LowerMana >= 80;
        var verySoon = context.LowerMana >= 90; // About to cap

        // Always enter if about to cap on mana or in burst
        // Otherwise wait a bit to align with buffs
        if (!inBurst && !highMana && !verySoon)
        {
            // Check if Embolden is coming off cooldown soon
            if (context.EmboldenReady)
            {
                context.Debug.DamageState = "Hold melee for Embolden";
                return false;
            }
        }

        // Start with Enchanted Riposte
        if (context.ActionService.ExecuteGcd(RDMActions.EnchantedRiposte, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.EnchantedRiposte.Name;
            context.Debug.DamageState = "Enchanted Riposte (combo start)";

            // Training Mode integration
            CasterTrainingHelper.RecordMeleeComboDecision(
                context.TrainingService,
                RDMActions.EnchantedRiposte.ActionId,
                RDMActions.EnchantedRiposte.Name,
                target.Name?.TextValue,
                1,
                "Riposte - melee combo entry",
                "Enchanted Riposte starts your melee combo. Enter at 50|50 mana or higher. Best used " +
                "during burst windows (Embolden active). Costs 20 Black and White Mana.",
                new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        inBurst ? "In burst window" : "Not in burst",
                        $"Mana after combo: ~{context.BlackMana - 50}|{context.WhiteMana - 50}" },
                new[] { "Wait for Embolden", "Build more mana" },
                "Enter melee combo at 50|50+ mana. Ideally align with Embolden for maximum damage.",
                RdmConcepts.MeleeEntry);

            context.TrainingService?.RecordConceptApplication(RdmConcepts.MeleeEntry, true, "Melee combo started");

            return true;
        }

        return false;
    }

    #endregion

    #region Dualcast Consumer

    private bool TryDualcastConsumer(ICirceContext context, IBattleChara target, bool useAoe)
    {
        var level = context.Player.Level;

        // Priority for instant cast:
        // 1. Use proc if available and about to expire
        // 2. Use proc to balance mana
        // 3. Use long spell to balance mana

        // Check for procs
        var procSpell = RDMActions.GetProcSpell(
            level,
            context.HasVerfire,
            context.HasVerstone,
            context.VerfireRemaining,
            context.VerstoneRemaining,
            context.BlackMana,
            context.WhiteMana);

        // If we have Acceleration, procs are guaranteed after the long spell
        // So prefer the long spell to generate procs
        if (context.HasAcceleration && !context.HasAnyProc)
        {
            // Use long spell to generate procs
            return TryLongSpell(context, target, useAoe);
        }

        // If proc is about to expire, use it
        if (procSpell != null)
        {
            var procExpiring = (context.HasVerfire && context.VerfireRemaining < 5f) ||
                               (context.HasVerstone && context.VerstoneRemaining < 5f);

            if (procExpiring)
            {
                if (context.ActionService.ExecuteGcd(procSpell, target.GameObjectId))
                {
                    context.Debug.PlannedAction = procSpell.Name;
                    context.Debug.DamageState = $"{procSpell.Name} (expiring proc)";

                    // Training Mode integration
                    var isVerfire = procSpell.ActionId == RDMActions.Verfire.ActionId;
                    CasterTrainingHelper.RecordProcDecision(
                        context.TrainingService,
                        procSpell.ActionId,
                        procSpell.Name,
                        isVerfire ? "Verfire" : "Verstone",
                        target.Name?.TextValue,
                        $"{procSpell.Name} - expiring proc usage",
                        $"{procSpell.Name} is about to expire. Using it now to avoid wasting the proc. " +
                        $"Procs last 30 seconds and provide instant cast + mana generation.",
                        new[] { $"Proc remaining: {(isVerfire ? context.VerfireRemaining : context.VerstoneRemaining):F1}s",
                                $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}" },
                        new[] { "Let it expire (waste)" },
                        "Always use procs before they expire. Verfire/Verstone are too valuable to waste.",
                        isVerfire ? RdmConcepts.VerfireProc : RdmConcepts.VerstoneProc);

                    context.TrainingService?.RecordConceptApplication(
                        isVerfire ? RdmConcepts.VerfireProc : RdmConcepts.VerstoneProc,
                        true, "Expiring proc consumed");

                    return true;
                }
            }
        }

        // Use long spell with Dualcast
        return TryLongSpell(context, target, useAoe);
    }

    private bool TryLongSpell(ICirceContext context, IBattleChara target, bool useAoe)
    {
        var level = context.Player.Level;

        if (useAoe && level >= RDMActions.Impact.MinLevel)
        {
            // Use Impact for AoE
            if (context.ActionService.ExecuteGcd(RDMActions.Impact, target.GameObjectId))
            {
                context.Debug.PlannedAction = RDMActions.Impact.Name;
                context.Debug.DamageState = "Impact (AoE)";

                // Training Mode integration
                CasterTrainingHelper.RecordAoeDecision(
                    context.TrainingService,
                    RDMActions.Impact.ActionId,
                    RDMActions.Impact.Name,
                    context.TargetingService.CountEnemiesInRange(5f, context.Player),
                    "Impact - AoE Dualcast consumer",
                    "Impact is your AoE Dualcast consumer. Use it when 3+ enemies are nearby. " +
                    "It generates both Black and White Mana evenly.",
                    new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                            context.HasDualcast ? "Dualcast active" : "Swiftcast/Acceleration" },
                    new[] { "Use single target spells" },
                    "Use Impact at 3+ targets. Below that, single target rotation is better.",
                    RdmConcepts.AoeRotation);

                context.TrainingService?.RecordConceptApplication(RdmConcepts.AoeRotation, true, "AoE rotation used");

                return true;
            }
        }

        // Single target: Use Verthunder or Veraero based on mana balance
        var longSpell = RDMActions.GetBalancedLongSpell(level, context.BlackMana, context.WhiteMana);

        if (context.ActionService.ExecuteGcd(longSpell, target.GameObjectId))
        {
            context.Debug.PlannedAction = longSpell.Name;
            context.Debug.DamageState = $"{longSpell.Name} (Dualcast)";

            // Training Mode integration
            var isVerthunder = longSpell.ActionId == RDMActions.Verthunder3.ActionId || longSpell.ActionId == RDMActions.Verthunder.ActionId;
            CasterTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                longSpell.ActionId,
                longSpell.Name,
                target.Name?.TextValue,
                $"{longSpell.Name} - Dualcast consumer",
                $"Using {longSpell.Name} with Dualcast for instant cast. Choose based on mana balance: " +
                $"Verthunder adds Black Mana, Veraero adds White Mana. Pick the lower one.",
                new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        isVerthunder ? "Chose Verthunder (Black lower)" : "Chose Veraero (White lower)",
                        context.HasDualcast ? "Dualcast active" : "Swiftcast/Acceleration" },
                new[] { isVerthunder ? "Use Veraero instead" : "Use Verthunder instead" },
                "Always pick the spell that generates your lower mana type to stay balanced.",
                RdmConcepts.DualcastConsumption);

            context.TrainingService?.RecordConceptApplication(RdmConcepts.DualcastConsumption, true, "Dualcast consumed correctly");

            return true;
        }

        return false;
    }

    #endregion

    #region Hardcast Filler

    private bool TryHardcastFiller(ICirceContext context, IBattleChara target, bool useAoe, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        // If moving and can't slidecast, use instant options
        if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            // Check for procs first (they're instant)
            var procSpell = RDMActions.GetProcSpell(
                level,
                context.HasVerfire,
                context.HasVerstone,
                context.VerfireRemaining,
                context.VerstoneRemaining,
                context.BlackMana,
                context.WhiteMana);

            if (procSpell != null)
            {
                if (context.ActionService.ExecuteGcd(procSpell, target.GameObjectId))
                {
                    context.Debug.PlannedAction = procSpell.Name;
                    context.Debug.DamageState = $"{procSpell.Name} (movement)";

                    // Training Mode integration
                    var isVerfire = procSpell.ActionId == RDMActions.Verfire.ActionId;
                    CasterTrainingHelper.RecordMovementDecision(
                        context.TrainingService,
                        procSpell.ActionId,
                        procSpell.Name,
                        target.Name?.TextValue,
                        $"{procSpell.Name} - movement instant",
                        $"Using {procSpell.Name} while moving since it's an instant cast proc. " +
                        $"Procs are perfect for movement as they don't require casting.",
                        new[] { "Moving", $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                                isVerfire ? "Verfire proc available" : "Verstone proc available" },
                        new[] { "Swiftcast for long spell", "Slidecast" },
                        "Use procs during movement. They're instant and generate mana.",
                        isVerfire ? RdmConcepts.VerfireProc : RdmConcepts.VerstoneProc);

                    context.TrainingService?.RecordConceptApplication(
                        isVerfire ? RdmConcepts.VerfireProc : RdmConcepts.VerstoneProc,
                        true, "Proc used during movement");

                    return true;
                }
            }

            // Use Swiftcast for movement if available
            if (context.SwiftcastReady)
            {
                if (context.ActionService.ExecuteOgcd(RDMActions.Swiftcast, player.GameObjectId))
                {
                    context.Debug.DamageState = "Swiftcast for movement";

                    // Training Mode integration
                    CasterTrainingHelper.RecordMovementDecision(
                        context.TrainingService,
                        RDMActions.Swiftcast.ActionId,
                        RDMActions.Swiftcast.Name,
                        null,
                        "Swiftcast - movement tool",
                        "Using Swiftcast to enable an instant long spell while moving. This lets you " +
                        "maintain uptime during mechanics. Follow with Verthunder/Veraero.",
                        new[] { "Moving", "No procs available",
                                $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}" },
                        new[] { "Wait for slidecast", "Use Acceleration" },
                        "Swiftcast is valuable for movement. Use it when you have no procs.",
                        RdmConcepts.SwiftcastUsage);

                    context.TrainingService?.RecordConceptApplication(RdmConcepts.SwiftcastUsage, true, "Swiftcast for movement");

                    return true;
                }
            }

            // No instant option available, wait for slidecast window
            context.Debug.DamageState = "Moving, need instant";
            return false;
        }

        // Use proc if available (as filler to generate Dualcast while using the proc)
        var filler = RDMActions.GetProcSpell(
            level,
            context.HasVerfire,
            context.HasVerstone,
            context.VerfireRemaining,
            context.VerstoneRemaining,
            context.BlackMana,
            context.WhiteMana);

        if (filler != null)
        {
            if (context.ActionService.ExecuteGcd(filler, target.GameObjectId))
            {
                context.Debug.PlannedAction = filler.Name;
                context.Debug.DamageState = $"{filler.Name} (proc filler)";

                // Training Mode integration
                var isVerfire = filler.ActionId == RDMActions.Verfire.ActionId;
                CasterTrainingHelper.RecordProcDecision(
                    context.TrainingService,
                    filler.ActionId,
                    filler.Name,
                    isVerfire ? "Verfire" : "Verstone",
                    target.Name?.TextValue,
                    $"{filler.Name} - proc as hardcast filler",
                    $"Using {filler.Name} as your hardcast filler because it's available. Procs are " +
                    $"instant, so they're ideal for the 'short spell' in Dualcast flow.",
                    new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                            isVerfire ? "Verfire available" : "Verstone available",
                            "Grants Dualcast" },
                    new[] { "Use Jolt instead", "Save proc for movement" },
                    "Procs are the best hardcast filler since they're instant and generate mana.",
                    RdmConcepts.ProcPriority);

                context.TrainingService?.RecordConceptApplication(RdmConcepts.ProcPriority, true, "Proc used as filler");

                return true;
            }
        }

        // No proc, use Jolt for single target or Verthunder II/Veraero II for AoE
        if (useAoe)
        {
            var aoeHardcast = RDMActions.GetAoeHardcast(level, context.BlackMana, context.WhiteMana);
            if (context.ActionService.ExecuteGcd(aoeHardcast, target.GameObjectId))
            {
                context.Debug.PlannedAction = aoeHardcast.Name;
                context.Debug.DamageState = $"{aoeHardcast.Name} (AoE hardcast)";
                return true;
            }
        }

        // Standard Jolt filler
        var jolt = RDMActions.GetJoltSpell(level);
        if (context.ActionService.ExecuteGcd(jolt, target.GameObjectId))
        {
            context.Debug.PlannedAction = jolt.Name;
            context.Debug.DamageState = jolt.Name;

            // Training Mode integration
            CasterTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                jolt.ActionId,
                jolt.Name,
                target.Name?.TextValue,
                "Jolt - Dualcast starter",
                "Jolt is your default hardcast filler when no procs are available. It has a short " +
                "cast time and generates equal Black and White Mana, granting Dualcast.",
                new[] { $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        "No procs available", "Grants Dualcast" },
                new[] { "Use proc if available" },
                "Jolt is the fallback filler. Procs are always better if available.",
                RdmConcepts.DualcastMechanic);

            context.TrainingService?.RecordConceptApplication(RdmConcepts.DualcastMechanic, true, "Dualcast generated");

            return true;
        }

        return false;
    }

    #endregion
}
