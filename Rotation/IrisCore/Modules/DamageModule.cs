using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.IrisCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.IrisCore.Modules;

/// <summary>
/// Handles Pictomancer GCD rotation.
/// Manages base combo, subtractive combo, hammer combo, paint spenders, and finishers.
/// </summary>
public sealed class DamageModule : IIrisModule
{
    public int Priority => 30; // Lower priority than buffs
    public string Name => "Damage";

    public bool TryExecute(IIrisContext context, bool isMoving)
    {
        // Allow painting motifs out of combat
        if (!context.InCombat)
        {
            // Try to prepaint motifs before combat
            if (TryPrepaintMotif(context))
                return true;

            context.Debug.DamageState = "Not in combat";
            return false;
        }

        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        // Don't interrupt casts unless we can slidecast
        if (context.IsCasting && !context.CanSlidecast)
        {
            context.Debug.DamageState = "Casting";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target for damage GCDs
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Priority 1: Star Prism during Starstruck (instant, high potency)
        if (TryStarPrism(context, target))
            return true;

        // Priority 2: Rainbow Drip with Rainbow Bright (instant)
        if (TryRainbowDrip(context, target, isMoving))
            return true;

        // Priority 3: Hammer Combo (all instant, use during burst)
        if (TryHammerCombo(context, target))
            return true;

        // Priority 4: Comet in Black (instant, high damage with Black Paint)
        if (TryCometInBlack(context, target, isMoving))
            return true;

        // Priority 5: Holy in White for movement (instant)
        if (TryHolyInWhite(context, target, isMoving))
            return true;

        // Priority 6: Subtractive Combo (when buff is active)
        if (TrySubtractiveCombo(context, target))
            return true;

        // Priority 7: Base Combo
        if (TryBaseCombo(context, target))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IIrisContext context)
    {
        // Debug state updated during TryExecute
    }

    #region GCD Actions

    private bool TryPrepaintMotif(IIrisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Already casting
        if (context.IsCasting)
            return false;

        // Priority: Landscape > Creature > Weapon (for burst preparation)
        if (context.NeedsLandscapeMotif && level >= PCTActions.LandscapeMotif.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(PCTActions.StarrySkyMotif, player.GameObjectId))
            {
                context.Debug.PlannedAction = PCTActions.StarrySkyMotif.Name;
                context.Debug.DamageState = "Painting Starry Sky";

                // Training Mode integration
                CasterTrainingHelper.RecordDamageDecision(
                    context.TrainingService,
                    PCTActions.StarrySkyMotif.ActionId,
                    PCTActions.StarrySkyMotif.Name,
                    null,
                    "Starry Sky Motif - landscape prepaint",
                    "Painting Starry Sky before combat prepares Landscape canvas for Starry Muse. " +
                    "This is highest priority prepaint as it enables your raid buff.",
                    new[] { "Pre-combat", "Landscape needed", "Enables Starry Muse" },
                    new[] { "Paint Creature first", "Paint Weapon first" },
                    "Always paint Landscape (Starry Sky) first - it enables your 2-minute burst.",
                    PctConcepts.CanvasPrepull);

                context.TrainingService?.RecordConceptApplication(PctConcepts.CanvasPrepull, true, "Landscape canvas prepared");

                return true;
            }
        }

        if (context.NeedsCreatureMotif && level >= PCTActions.CreatureMotif.MinLevel)
        {
            var motif = PCTActions.GetCreatureMotif(level, 0);
            if (context.ActionService.ExecuteGcd(motif, player.GameObjectId))
            {
                context.Debug.PlannedAction = motif.Name;
                context.Debug.DamageState = $"Painting {motif.Name}";

                // Training Mode integration
                CasterTrainingHelper.RecordDamageDecision(
                    context.TrainingService,
                    motif.ActionId,
                    motif.Name,
                    null,
                    "Creature Motif - prepaint",
                    $"Painting {motif.Name} prepares Creature canvas for Living Muse. Creatures scale " +
                    "with level (Pom → Wing → Claw → Maw). This enables portrait abilities.",
                    new[] { "Pre-combat", "Creature needed", "Enables Living Muse" },
                    new[] { "Paint Landscape first", "Paint Weapon first" },
                    "Paint Creature second priority after Landscape for maximum burst damage.",
                    PctConcepts.CanvasPrepull);

                context.TrainingService?.RecordConceptApplication(PctConcepts.CanvasPrepull, true, "Creature canvas prepared");

                return true;
            }
        }

        if (context.NeedsWeaponMotif && level >= PCTActions.WeaponMotif.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(PCTActions.HammerMotif, player.GameObjectId))
            {
                context.Debug.PlannedAction = PCTActions.HammerMotif.Name;
                context.Debug.DamageState = "Painting Hammer";

                // Training Mode integration
                CasterTrainingHelper.RecordDamageDecision(
                    context.TrainingService,
                    PCTActions.HammerMotif.ActionId,
                    PCTActions.HammerMotif.Name,
                    null,
                    "Hammer Motif - weapon prepaint",
                    "Painting Hammer prepares Weapon canvas for Striking Muse. This enables the " +
                    "powerful instant hammer combo (Stamp → Brush → Polish).",
                    new[] { "Pre-combat", "Weapon needed", "Enables Hammer Time" },
                    new[] { "Paint Landscape first", "Paint Creature first" },
                    "Paint Weapon last in prepull. Hammer combo is great but Landscape/Creature are higher priority.",
                    PctConcepts.CanvasPrepull);

                context.TrainingService?.RecordConceptApplication(PctConcepts.CanvasPrepull, true, "Weapon canvas prepared");

                return true;
            }
        }

        return false;
    }

    private bool TryStarPrism(IIrisContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.StarPrism.MinLevel)
            return false;

        // Requires Starstruck buff
        if (!context.HasStarstruck)
            return false;

        if (context.ActionService.ExecuteGcd(PCTActions.StarPrism, target.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.StarPrism.Name;
            context.Debug.DamageState = "Star Prism";

            // Training Mode integration
            CasterTrainingHelper.RecordBurstDecision(
                context.TrainingService,
                PCTActions.StarPrism.ActionId,
                PCTActions.StarPrism.Name,
                target.Name?.TextValue,
                "Star Prism - burst finisher",
                "Star Prism is your highest potency single-target GCD, available during Starstruck buff " +
                "from Starry Muse. It's instant cast and deals massive damage. Always use before it expires.",
                new[] { "Starstruck active", $"Palette Gauge: {context.PaletteGauge}", "In burst window" },
                new[] { "None - must use before buff expires" },
                "Star Prism is your biggest hit. Never let Starstruck expire without using it.",
                PctConcepts.FinisherPriority);

            context.TrainingService?.RecordConceptApplication(PctConcepts.FinisherPriority, true, "Star Prism finisher used");

            return true;
        }

        return false;
    }

    private bool TryRainbowDrip(IIrisContext context, IBattleChara target, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.RainbowDrip.MinLevel)
            return false;

        // With Rainbow Bright, it's instant
        if (!context.HasRainbowBright)
        {
            // Without the buff, it has a long cast time
            // Only use if not moving and during burst
            if (isMoving)
                return false;

            // Only hardcast during burst window or if no other options
            if (!context.IsInBurstWindow)
                return false;
        }

        if (context.ActionService.ExecuteGcd(PCTActions.RainbowDrip, target.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.RainbowDrip.Name;
            context.Debug.DamageState = context.HasRainbowBright ? "Rainbow Drip (instant)" : "Rainbow Drip (hardcast)";

            // Training Mode integration
            if (context.HasRainbowBright)
            {
                CasterTrainingHelper.RecordBurstDecision(
                    context.TrainingService,
                    PCTActions.RainbowDrip.ActionId,
                    PCTActions.RainbowDrip.Name,
                    target.Name?.TextValue,
                    "Rainbow Drip - instant with Rainbow Bright",
                    "Rainbow Drip with Rainbow Bright buff is instant cast and high damage. Use it when " +
                    "the proc is active for a powerful instant GCD during movement or burst windows.",
                    new[] { "Rainbow Bright active", $"Palette Gauge: {context.PaletteGauge}",
                            context.IsInBurstWindow ? "In burst window" : "Outside burst" },
                    new[] { "None - use proc before it expires" },
                    "Rainbow Bright makes Rainbow Drip instant. Don't waste the proc.",
                    PctConcepts.RainbowDrip);

                context.TrainingService?.RecordConceptApplication(PctConcepts.RainbowDrip, true, "Rainbow Bright proc consumed");
            }
            else
            {
                CasterTrainingHelper.RecordDamageDecision(
                    context.TrainingService,
                    PCTActions.RainbowDrip.ActionId,
                    PCTActions.RainbowDrip.Name,
                    target.Name?.TextValue,
                    "Rainbow Drip - hardcast filler",
                    "Rainbow Drip without Rainbow Bright has a long cast time. Only hardcast during burst " +
                    "windows when you have no movement. In normal rotation, wait for the proc.",
                    new[] { "No Rainbow Bright", "Hardcasting", context.IsInBurstWindow ? "In burst window" : "Outside burst" },
                    new[] { "Wait for Rainbow Bright proc", "Use combo instead" },
                    "Hardcast Rainbow Drip only during burst windows. Normally wait for the instant proc.",
                    PctConcepts.RainbowDrip);
            }

            return true;
        }

        return false;
    }

    private bool TryHammerCombo(IIrisContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.HammerStamp.MinLevel)
            return false;

        // Need Hammer Time buff to use hammer combo
        if (!context.HasHammerTime && !context.IsInHammerCombo)
            return false;

        // Get the next hammer action
        var hammerAction = PCTActions.GetHammerComboAction(context.HammerComboStep, level);
        if (hammerAction == null)
            return false;

        if (context.ActionService.ExecuteGcd(hammerAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = hammerAction.Name;
            context.Debug.DamageState = $"Hammer ({hammerAction.Name})";

            // Training Mode integration
            var stepName = context.HammerComboStep switch
            {
                0 => "Stamp (step 1)",
                1 => "Brush (step 2)",
                2 => "Polish (step 3)",
                _ => "Unknown"
            };

            CasterTrainingHelper.RecordBurstDecision(
                context.TrainingService,
                hammerAction.ActionId,
                hammerAction.Name,
                target.Name?.TextValue,
                $"Hammer combo - {stepName}",
                $"Hammer combo {stepName} is instant cast and high damage. Complete all 3 hits " +
                "(Stamp → Brush → Polish) before Hammer Time expires. All hits are instant.",
                new[] { $"Hammer Step: {context.HammerComboStep}", $"Hammer Time Stacks: {context.HammerTimeStacks}",
                        context.IsInBurstWindow ? "In burst window" : "Outside burst" },
                new[] { "Don't drop combo" },
                "Complete the hammer combo before Hammer Time expires. All 3 hits are instant and high damage.",
                PctConcepts.HammerCombo);

            context.TrainingService?.RecordConceptApplication(PctConcepts.HammerCombo, true, $"Hammer {stepName} executed");

            return true;
        }

        return false;
    }

    private bool TryCometInBlack(IIrisContext context, IBattleChara target, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.CometInBlack.MinLevel)
            return false;

        // Requires Black Paint
        if (!context.HasBlackPaint)
            return false;

        // Comet is instant, good for movement
        if (context.ActionService.ExecuteGcd(PCTActions.CometInBlack, target.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.CometInBlack.Name;
            context.Debug.DamageState = "Comet in Black";

            // Training Mode integration
            CasterTrainingHelper.RecordBurstDecision(
                context.TrainingService,
                PCTActions.CometInBlack.ActionId,
                PCTActions.CometInBlack.Name,
                target.Name?.TextValue,
                "Comet in Black - black paint spender",
                "Comet in Black consumes Black Paint for high instant damage. Black Paint is generated " +
                "via Subtractive Palette during Monochrome Tones. It's your highest potency paint spender.",
                new[] { "Black Paint available", $"White Paint: {context.WhitePaint}",
                        context.IsInBurstWindow ? "In burst window" : "Outside burst" },
                new[] { "None - use Black Paint when available" },
                "Always use Comet in Black when you have Black Paint. It's higher damage than Holy in White.",
                PctConcepts.CometInBlack);

            context.TrainingService?.RecordConceptApplication(PctConcepts.CometInBlack, true, "Black Paint consumed");

            return true;
        }

        return false;
    }

    private bool TryHolyInWhite(IIrisContext context, IBattleChara target, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.HolyInWhite.MinLevel)
            return false;

        // Requires White Paint
        if (!context.HasWhitePaint)
            return false;

        // Holy is instant, use for movement or when paint is capping
        // Prioritize using for movement or if close to cap (4+ stacks)
        if (!isMoving && context.WhitePaint < 4)
        {
            // Don't waste paint if we have better options
            if (!context.IsInBurstWindow)
                return false;
        }

        if (context.ActionService.ExecuteGcd(PCTActions.HolyInWhite, target.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.HolyInWhite.Name;
            context.Debug.DamageState = $"Holy in White ({context.WhitePaint - 1} paint)";

            // Training Mode integration
            if (isMoving)
            {
                CasterTrainingHelper.RecordMovementDecision(
                    context.TrainingService,
                    PCTActions.HolyInWhite.ActionId,
                    PCTActions.HolyInWhite.Name,
                    target.Name?.TextValue,
                    "Holy in White - movement instant",
                    "Holy in White is instant cast, perfect for movement. Using White Paint to maintain " +
                    "uptime while moving. This prevents DPS loss during mechanics.",
                    new[] { "Moving", $"White Paint: {context.WhitePaint}", $"Paint after: {context.WhitePaint - 1}" },
                    new[] { "Slidecast combo instead", "Wait for movement to end" },
                    "Use Holy in White for movement. Save some paint for burst windows when possible.",
                    PctConcepts.MovementOptimization);

                context.TrainingService?.RecordConceptApplication(PctConcepts.MovementOptimization, true, "Paint used for movement");
            }
            else
            {
                CasterTrainingHelper.RecordDamageDecision(
                    context.TrainingService,
                    PCTActions.HolyInWhite.ActionId,
                    PCTActions.HolyInWhite.Name,
                    target.Name?.TextValue,
                    "Holy in White - paint spender",
                    "Holy in White consumes White Paint for instant damage. Using at 4+ stacks to prevent " +
                    "overcapping or during burst windows for extra damage. White Paint builds from combos.",
                    new[] { $"White Paint: {context.WhitePaint}", context.WhitePaint >= 4 ? "Overcap risk" : "Burst damage",
                            context.IsInBurstWindow ? "In burst window" : "Outside burst" },
                    new[] { "Save for movement", "Use during burst instead" },
                    "Don't cap at 5 White Paint. Use Holy in White during burst or at 4+ stacks.",
                    PctConcepts.HolyInWhite);

                context.TrainingService?.RecordConceptApplication(PctConcepts.HolyInWhite, true, "White Paint consumed");
            }

            return true;
        }

        return false;
    }

    private bool TrySubtractiveCombo(IIrisContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.BlizzardInCyan.MinLevel)
            return false;

        // Need Subtractive Palette buff or Subtractive Spectrum
        if (!context.HasSubtractivePalette && !context.HasSubtractiveSpectrum)
            return false;

        // Get the appropriate combo action
        var comboAction = PCTActions.GetSubtractiveComboAction(context.BaseComboStep, context.ShouldUseAoe, level);

        if (context.ActionService.ExecuteGcd(comboAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = comboAction.Name;
            context.Debug.DamageState = $"Subtractive ({comboAction.Name})";

            // Training Mode integration
            var stepName = context.BaseComboStep switch
            {
                0 => "Blizzard in Cyan (step 1)",
                1 => "Stone in Yellow (step 2)",
                2 => "Thunder in Magenta (step 3)",
                _ => "Unknown"
            };

            if (context.ShouldUseAoe)
            {
                CasterTrainingHelper.RecordAoeDecision(
                    context.TrainingService,
                    comboAction.ActionId,
                    comboAction.Name,
                    context.NearbyEnemyCount,
                    $"Subtractive AoE combo - {stepName}",
                    $"Subtractive AoE combo deals higher damage than base combo. Complete all 3 hits " +
                    "(Cyan → Yellow → Magenta) to build Palette Gauge and White Paint.",
                    new[] { $"Combo Step: {context.BaseComboStep}", $"Enemies: {context.NearbyEnemyCount}",
                            $"Subtractive Palette remaining: {context.SubtractivePaletteRemaining:F1}s" },
                    new[] { "Switch to single target" },
                    "Complete subtractive combo before the buff expires. AoE at 3+ targets.",
                    PctConcepts.SubtractiveCombo);
            }
            else
            {
                CasterTrainingHelper.RecordDamageDecision(
                    context.TrainingService,
                    comboAction.ActionId,
                    comboAction.Name,
                    target.Name?.TextValue,
                    $"Subtractive combo - {stepName}",
                    $"Subtractive combo {stepName} is enhanced by Subtractive Palette buff. Complete " +
                    "all 3 hits (Cyan → Yellow → Magenta) before the buff expires. Higher damage than base.",
                    new[] { $"Combo Step: {context.BaseComboStep}",
                            $"Subtractive Palette remaining: {context.SubtractivePaletteRemaining:F1}s",
                            $"Palette Gauge: {context.PaletteGauge}" },
                    new[] { "Don't drop combo" },
                    "Complete subtractive combo before Subtractive Palette buff expires.",
                    PctConcepts.SubtractiveCombo);
            }

            context.TrainingService?.RecordConceptApplication(PctConcepts.SubtractiveCombo, true, $"Subtractive {stepName} executed");

            return true;
        }

        return false;
    }

    private bool TryBaseCombo(IIrisContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Get the appropriate combo action based on step and AoE status
        var comboAction = PCTActions.GetBaseComboAction(context.BaseComboStep, context.ShouldUseAoe, level);

        if (context.ActionService.ExecuteGcd(comboAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = comboAction.Name;
            context.Debug.DamageState = $"Base Combo ({comboAction.Name})";

            // Training Mode integration
            var stepName = context.BaseComboStep switch
            {
                0 => "Fire in Red (step 1)",
                1 => "Aero in Green (step 2)",
                2 => "Water in Blue (step 3)",
                _ => "Unknown"
            };

            if (context.ShouldUseAoe)
            {
                CasterTrainingHelper.RecordAoeDecision(
                    context.TrainingService,
                    comboAction.ActionId,
                    comboAction.Name,
                    context.NearbyEnemyCount,
                    $"Base AoE combo - {stepName}",
                    $"Base AoE combo for multiple targets. Complete all 3 hits (Red → Green → Blue) " +
                    "to build Palette Gauge and White Paint. Use Subtractive Palette at 50+ gauge.",
                    new[] { $"Combo Step: {context.BaseComboStep}", $"Enemies: {context.NearbyEnemyCount}",
                            $"Palette Gauge: {context.PaletteGauge}", $"White Paint: {context.WhitePaint}" },
                    new[] { "Switch to single target" },
                    "Complete base combo to build resources. AoE at 3+ targets.",
                    PctConcepts.AoeRotation);
            }
            else
            {
                CasterTrainingHelper.RecordDamageDecision(
                    context.TrainingService,
                    comboAction.ActionId,
                    comboAction.Name,
                    target.Name?.TextValue,
                    $"Base combo - {stepName}",
                    $"Base combo {stepName} is your standard filler rotation. Complete all 3 hits " +
                    "(Red → Green → Blue) to build 25 Palette Gauge and 1 White Paint per combo.",
                    new[] { $"Combo Step: {context.BaseComboStep}", $"Palette Gauge: {context.PaletteGauge}",
                            $"White Paint: {context.WhitePaint}" },
                    new[] { "Use Subtractive at 50+ gauge" },
                    "Complete base combo to build resources. Use Subtractive Palette at 50+ gauge.",
                    PctConcepts.ComboBasics);
            }

            context.TrainingService?.RecordConceptApplication(PctConcepts.ComboBasics, true, $"Base {stepName} executed");

            return true;
        }

        return false;
    }

    #endregion
}
