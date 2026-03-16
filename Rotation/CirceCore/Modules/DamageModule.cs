using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.CirceCore.Modules;

/// <summary>
/// Handles the Red Mage damage rotation.
/// Manages Dualcast flow, melee combo, finishers, and mana balance.
/// </summary>
public sealed class DamageModule : BaseDpsDamageModule<ICirceContext>, ICirceModule
{
    public DamageModule(IBurstWindowService? burstWindowService = null) : base(burstWindowService) { }

    #region Abstract Method Implementations

    protected override float GetTargetingRange() => FFXIVConstants.CasterTargetingRange;

    protected override float GetAoECountRange() => 5f;

    protected override void SetDamageState(ICirceContext context, string state) =>
        context.Debug.DamageState = state;

    protected override void SetNearbyEnemies(ICirceContext context, int count) =>
        context.Debug.NearbyEnemies = count;

    protected override void SetPlannedAction(ICirceContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override bool IsAoEEnabled(ICirceContext context) =>
        context.Configuration.RedMage.EnableAoERotation;

    protected override int GetConfiguredAoEThreshold(ICirceContext context) =>
        context.Configuration.RedMage.AoEMinTargets;

    /// <summary>
    /// RDM has no damage oGCDs - all abilities are GCDs or utility oGCDs in BuffModule.
    /// </summary>
    protected override bool TryOgcdDamage(ICirceContext context, IBattleChara target, int enemyCount)
    {
        return false;
    }

    protected override bool TryGcdDamage(ICirceContext context, IBattleChara target, int enemyCount, bool isMoving)
    {
        var level = context.Player.Level;
        var useAoe = ShouldUseAoE(enemyCount);

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

        return false;
    }

    #endregion

    #region Finisher Sequence

    private bool TryResolution(ICirceContext context, IBattleChara target)
    {
        if (context.ActionService.ExecuteGcd(RDMActions.Resolution, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Resolution.Name;
            context.Debug.DamageState = "Resolution (final finisher)";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.Resolution.ActionId, RDMActions.Resolution.Name)
                .AsCasterBurst()
                .Target(target.Name?.TextValue)
                .Reason("Resolution - final finisher",
                    "Resolution is the final GCD in the finisher sequence after Scorch. It deals massive " +
                    "damage and completes your melee combo burst. Always use when available.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        "After Scorch", "Finisher sequence complete")
                .Alternatives("Must use - combo will drop")
                .Tip("Resolution is your finisher's finisher. Never let the combo drop before using it.")
                .Concept(RdmConcepts.ScorchResolution)
                .Record();

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
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.Scorch.ActionId, RDMActions.Scorch.Name)
                .AsCasterBurst()
                .Target(target.Name?.TextValue)
                .Reason("Scorch - post-finisher burst",
                    "Scorch becomes available after using Verflare or Verholy. It's a high-damage GCD " +
                    "that leads into Resolution. Use immediately to continue the burst.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        "After Verflare/Verholy", "Resolution follows")
                .Alternatives("Must use - combo will drop")
                .Tip("Scorch is mandatory after Verflare/Verholy. It sets up Resolution for massive damage.")
                .Concept(RdmConcepts.ScorchResolution)
                .Record();

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
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.GrandImpact.ActionId, RDMActions.GrandImpact.Name)
                .AsCasterBurst()
                .Target(target.Name?.TextValue)
                .Reason("Grand Impact - special proc",
                    "Grand Impact becomes available from Acceleration III procs. It's a powerful instant " +
                    "GCD that should be used when ready. Don't waste the proc.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        "Grand Impact Ready active")
                .Alternatives("Must use before proc expires")
                .Tip("Grand Impact is free damage from Acceleration. Use it as soon as it procs.")
                .Concept(RdmConcepts.GrandImpact)
                .Record();

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
            TrainingHelper.Decision(context.TrainingService)
                .Action(finisher.ActionId, finisher.Name)
                .AsCasterBurst()
                .Target(target.Name?.TextValue)
                .Reason($"{finisher.Name} - melee finisher",
                    $"{finisher.Name} is your melee combo finisher after Redoublement. Choose based on mana: " +
                    $"Verflare adds Black Mana, Verholy adds White Mana. Pick the lower one to stay balanced.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        isVerflare ? "Chose Verflare (Black lower)" : "Chose Verholy (White lower)",
                        "Leads to Scorch → Resolution")
                .Alternatives(isVerflare ? "Use Verholy instead" : "Use Verflare instead")
                .Tip("Pick the finisher that generates your lower mana type to maintain balance.")
                .Concept(RdmConcepts.FinisherSelection)
                .Record();

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
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(RDMActions.EnchantedZwerchhau.ActionId, RDMActions.EnchantedZwerchhau.Name)
                            .AsMeleeCombo(2)
                            .Target(target.Name?.TextValue)
                            .Reason("Zwerchhau - combo step 2",
                                "Enchanted Zwerchhau is the second hit in your melee combo. It costs 15 " +
                                "Black and White Mana. Continue the combo to Redoublement.")
                            .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                                    "Step 2 of 3", "Redoublement next")
                            .Alternatives("Continue combo", "Don't drop it")
                            .Tip("Always complete your melee combo. Dropping it wastes mana and DPS.")
                            .Concept(RdmConcepts.ComboProgression)
                            .Record();

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
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(RDMActions.EnchantedRedoublement.ActionId, RDMActions.EnchantedRedoublement.Name)
                            .AsMeleeCombo(3)
                            .Target(target.Name?.TextValue)
                            .Reason("Redoublement - combo step 3",
                                "Enchanted Redoublement is the final hit in your melee combo. It costs 15 " +
                                "Black and White Mana. This leads into Verflare/Verholy finisher.")
                            .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                                    "Step 3 of 3", "Finisher next")
                            .Alternatives("Use finisher immediately")
                            .Tip("After Redoublement, use Verflare/Verholy based on your lower mana.")
                            .Concept(RdmConcepts.ComboProgression)
                            .Record();

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
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.EnchantedRiposte.ActionId, RDMActions.EnchantedRiposte.Name)
                .AsMeleeCombo(1)
                .Target(target.Name?.TextValue)
                .Reason("Riposte - melee combo entry",
                    "Enchanted Riposte starts your melee combo. Enter at 50|50 mana or higher. Best used " +
                    "during burst windows (Embolden active). Costs 20 Black and White Mana.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        inBurst ? "In burst window" : "Not in burst",
                        $"Mana after combo: ~{context.BlackMana - 50}|{context.WhiteMana - 50}")
                .Alternatives("Wait for Embolden", "Build more mana")
                .Tip("Enter melee combo at 50|50+ mana. Ideally align with Embolden for maximum damage.")
                .Concept(RdmConcepts.MeleeEntry)
                .Record();

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
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(procSpell.ActionId, procSpell.Name)
                        .AsCasterProc(isVerfire ? "Verfire" : "Verstone")
                        .Target(target.Name?.TextValue)
                        .Reason($"{procSpell.Name} - expiring proc usage",
                            $"{procSpell.Name} is about to expire. Using it now to avoid wasting the proc. " +
                            $"Procs last 30 seconds and provide instant cast + mana generation.")
                        .Factors($"Proc remaining: {(isVerfire ? context.VerfireRemaining : context.VerstoneRemaining):F1}s",
                                $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}")
                        .Alternatives("Let it expire (waste)")
                        .Tip("Always use procs before they expire. Verfire/Verstone are too valuable to waste.")
                        .Concept(isVerfire ? RdmConcepts.VerfireProc : RdmConcepts.VerstoneProc)
                        .Record();

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
                var enemyCount = context.TargetingService.CountEnemiesInRange(5f, context.Player);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RDMActions.Impact.ActionId, RDMActions.Impact.Name)
                    .AsAoE(enemyCount)
                    .Reason("Impact - AoE Dualcast consumer",
                        "Impact is your AoE Dualcast consumer. Use it when 3+ enemies are nearby. " +
                        "It generates both Black and White Mana evenly.")
                    .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                            context.HasDualcast ? "Dualcast active" : "Swiftcast/Acceleration")
                    .Alternatives("Use single target spells")
                    .Tip("Use Impact at 3+ targets. Below that, single target rotation is better.")
                    .Concept(RdmConcepts.AoeRotation)
                    .Record();

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
            TrainingHelper.Decision(context.TrainingService)
                .Action(longSpell.ActionId, longSpell.Name)
                .AsCasterDamage()
                .Target(target.Name?.TextValue)
                .Reason($"{longSpell.Name} - Dualcast consumer",
                    $"Using {longSpell.Name} with Dualcast for instant cast. Choose based on mana balance: " +
                    $"Verthunder adds Black Mana, Veraero adds White Mana. Pick the lower one.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        isVerthunder ? "Chose Verthunder (Black lower)" : "Chose Veraero (White lower)",
                        context.HasDualcast ? "Dualcast active" : "Swiftcast/Acceleration")
                .Alternatives(isVerthunder ? "Use Veraero instead" : "Use Verthunder instead")
                .Tip("Always pick the spell that generates your lower mana type to stay balanced.")
                .Concept(RdmConcepts.DualcastConsumption)
                .Record();

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
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(procSpell.ActionId, procSpell.Name)
                        .AsMovement()
                        .Target(target.Name?.TextValue)
                        .Reason($"{procSpell.Name} - movement instant",
                            $"Using {procSpell.Name} while moving since it's an instant cast proc. " +
                            $"Procs are perfect for movement as they don't require casting.")
                        .Factors("Moving", $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                                isVerfire ? "Verfire proc available" : "Verstone proc available")
                        .Alternatives("Swiftcast for long spell", "Slidecast")
                        .Tip("Use procs during movement. They're instant and generate mana.")
                        .Concept(isVerfire ? RdmConcepts.VerfireProc : RdmConcepts.VerstoneProc)
                        .Record();

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
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RDMActions.Swiftcast.ActionId, RDMActions.Swiftcast.Name)
                        .AsMovement()
                        .Reason("Swiftcast - movement tool",
                            "Using Swiftcast to enable an instant long spell while moving. This lets you " +
                            "maintain uptime during mechanics. Follow with Verthunder/Veraero.")
                        .Factors("Moving", "No procs available",
                                $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}")
                        .Alternatives("Wait for slidecast", "Use Acceleration")
                        .Tip("Swiftcast is valuable for movement. Use it when you have no procs.")
                        .Concept(RdmConcepts.SwiftcastUsage)
                        .Record();

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
                TrainingHelper.Decision(context.TrainingService)
                    .Action(filler.ActionId, filler.Name)
                    .AsCasterProc(isVerfire ? "Verfire" : "Verstone")
                    .Target(target.Name?.TextValue)
                    .Reason($"{filler.Name} - proc as hardcast filler",
                        $"Using {filler.Name} as your hardcast filler because it's available. Procs are " +
                        $"instant, so they're ideal for the 'short spell' in Dualcast flow.")
                    .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                            isVerfire ? "Verfire available" : "Verstone available",
                            "Grants Dualcast")
                    .Alternatives("Use Jolt instead", "Save proc for movement")
                    .Tip("Procs are the best hardcast filler since they're instant and generate mana.")
                    .Concept(RdmConcepts.ProcPriority)
                    .Record();

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
            TrainingHelper.Decision(context.TrainingService)
                .Action(jolt.ActionId, jolt.Name)
                .AsCasterDamage()
                .Target(target.Name?.TextValue)
                .Reason("Jolt - Dualcast starter",
                    "Jolt is your default hardcast filler when no procs are available. It has a short " +
                    "cast time and generates equal Black and White Mana, granting Dualcast.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        "No procs available", "Grants Dualcast")
                .Alternatives("Use proc if available")
                .Tip("Jolt is the fallback filler. Procs are always better if available.")
                .Concept(RdmConcepts.DualcastMechanic)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.DualcastMechanic, true, "Dualcast generated");

            return true;
        }

        return false;
    }

    #endregion
}
