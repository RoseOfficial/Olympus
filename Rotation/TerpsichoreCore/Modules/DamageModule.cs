using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.TerpsichoreCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.TerpsichoreCore.Modules;

/// <summary>
/// Handles the Dancer GCD damage rotation.
/// Manages procs, Esprit spenders, Tillana, and filler GCDs.
/// Extends BaseDpsDamageModule for shared damage module patterns.
/// </summary>
public sealed class DamageModule : BaseDpsDamageModule<ITerpsichoreContext>, ITerpsichoreModule
{
    public DamageModule(IBurstWindowService? burstWindowService = null) : base(burstWindowService) { }

    #region Abstract Method Implementations

    /// <summary>
    /// Ranged physical targeting range (25y).
    /// </summary>
    protected override float GetTargetingRange() => FFXIVConstants.RangedTargetingRange;

    /// <summary>
    /// AoE count range for DNC (5y for Fan Dance abilities).
    /// </summary>
    protected override float GetAoECountRange() => 5f;

    /// <summary>
    /// Sets the damage state in the debug display.
    /// </summary>
    protected override void SetDamageState(ITerpsichoreContext context, string state) =>
        context.Debug.DamageState = state;

    /// <summary>
    /// Sets the nearby enemy count in the debug display.
    /// </summary>
    protected override void SetNearbyEnemies(ITerpsichoreContext context, int count) =>
        context.Debug.NearbyEnemies = count;

    /// <summary>
    /// Sets the planned action name in the debug display.
    /// </summary>
    protected override void SetPlannedAction(ITerpsichoreContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override bool IsAoEEnabled(ITerpsichoreContext context) =>
        context.Configuration.Dancer.EnableAoERotation;

    protected override int GetConfiguredAoEThreshold(ITerpsichoreContext context) =>
        context.Configuration.Dancer.AoEMinTargets;

    /// <summary>
    /// Pre-execute checks for Dancer - also checks if dancing (handled by BuffModule).
    /// </summary>
    protected override bool PreExecuteChecks(ITerpsichoreContext context)
    {
        if (!base.PreExecuteChecks(context))
            return false;

        // Don't interrupt dances with GCDs (handled by BuffModule)
        if (context.IsDancing)
        {
            SetDamageState(context, "Dancing...");
            return false;
        }

        return true;
    }

    /// <summary>
    /// oGCD damage for Dancer - primarily interrupt handling.
    /// </summary>
    protected override bool TryOgcdDamage(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        // Interrupt enemy casts (highest priority)
        if (TryInterrupt(context, target))
            return true;

        return false;
    }

    /// <summary>
    /// Main GCD damage rotation for Dancer.
    /// Handles procs, Esprit spenders, and filler GCDs.
    /// </summary>
    protected override bool TryGcdDamage(ITerpsichoreContext context, IBattleChara target, int enemyCount, bool isMoving)
    {
        // === HIGHEST PRIORITY PROCS ===

        // Priority 1: Starfall Dance (expires with Devilment)
        if (TryStarfallDance(context, target))
            return true;

        // Priority 2: Finishing Move (Lv.96+)
        if (TryFinishingMove(context))
            return true;

        // Priority 3: Last Dance (Lv.92+)
        if (TryLastDance(context, target))
            return true;

        // Priority 4: Tillana (after Technical Finish)
        if (TryTillana(context))
            return true;

        // === ESPRIT SPENDERS ===

        // Priority 5: Dance of the Dawn (Lv.100, replaces Saber Dance during buff)
        if (TryDanceOfTheDawn(context, target))
            return true;

        // Priority 6: Saber Dance (Esprit >= 80 to avoid overcap, or >= 50 during burst)
        if (TrySaberDance(context, target, enemyCount))
            return true;

        // === PROC CONSUMERS ===

        // Priority 7: Fountainfall (Silken Flow proc)
        if (TryFountainfall(context, target, enemyCount))
            return true;

        // Priority 8: Reverse Cascade (Silken Symmetry proc)
        if (TryReverseCascade(context, target, enemyCount))
            return true;

        // === COMBO FILLER ===

        // Priority 9: Fountain (combo finisher)
        if (TryFountain(context, target, enemyCount))
            return true;

        // Priority 10: Cascade (combo starter / filler)
        if (TryCascade(context, target, enemyCount))
            return true;

        return false;
    }

    #endregion

    #region High Priority Procs

    private bool TryStarfallDance(ITerpsichoreContext context, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableStarfallDance) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.StarfallDance.MinLevel)
            return false;

        if (!context.HasFlourishingStarfall)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.StarfallDance.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.StarfallDance, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.StarfallDance.Name;
            context.Debug.DamageState = "Starfall Dance";

            // Training: Record Starfall Dance decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.StarfallDance.ActionId, DNCActions.StarfallDance.Name)
                .AsRangedBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Flourishing Starfall proc - highest priority GCD",
                    "Starfall Dance is granted by Devilment (Flourishing Starfall buff) and expires when Devilment " +
                    "ends. It's your highest potency GCD and must be used during the Devilment window. Has fall-off " +
                    "damage for AoE.")
                .Factors("Flourishing Starfall active", "Highest priority GCD", "Expires with Devilment")
                .Alternatives("No Starfall proc")
                .Tip("Never let Starfall Dance expire - use it immediately after Devilment.")
                .Concept(DncConcepts.StarfallDance)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.StarfallDance, true, "Burst GCD used");

            return true;
        }

        return false;
    }

    private bool TryFinishingMove(ITerpsichoreContext context)
    {
        if (!context.Configuration.Dancer.EnableFinishingMove)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.FinishingMove.MinLevel)
            return false;

        if (!context.HasFinishingMoveReady)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.FinishingMove.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.FinishingMove, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.FinishingMove.Name;
            context.Debug.DamageState = "Finishing Move";

            // Training: Record Finishing Move decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.FinishingMove.ActionId, DNCActions.FinishingMove.Name)
                .AsRangedBurst()
                .Reason(
                    "Finishing Move Ready proc",
                    "Finishing Move (Lv.96+) is granted after Standard Finish. It's a high-potency GCD that should " +
                    "be used before Standard Finish buff expires. Prioritize it over normal GCDs during your rotation.")
                .Factors("Finishing Move Ready buff", "High potency GCD", "Granted by Standard Finish")
                .Alternatives("No Finishing Move Ready proc")
                .Tip("Use Finishing Move before the buff expires - don't let it fall off.")
                .Concept(DncConcepts.FinishingMove)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.FinishingMove, true, "Proc consumed");

            return true;
        }

        return false;
    }

    private bool TryLastDance(ITerpsichoreContext context, IBattleChara target)
    {
        if (!context.Configuration.Dancer.EnableLastDance) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.LastDance.MinLevel)
            return false;

        if (!context.HasLastDanceReady)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.LastDance.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.LastDance, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.LastDance.Name;
            context.Debug.DamageState = "Last Dance";

            // Training: Record Last Dance decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.LastDance.ActionId, DNCActions.LastDance.Name)
                .AsRangedBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Last Dance Ready proc",
                    "Last Dance (Lv.92+) is granted after Technical Finish or Tillana. It's a high-potency GCD " +
                    "that extends your burst phase. Use before the buff expires.")
                .Factors("Last Dance Ready buff", "High potency burst GCD", "Granted by Technical/Tillana")
                .Alternatives("No Last Dance Ready proc")
                .Tip("Use Last Dance during burst windows - it's part of your Technical Step sequence.")
                .Concept(DncConcepts.LastDance)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.LastDance, true, "Proc consumed");

            return true;
        }

        return false;
    }

    private bool TryTillana(ITerpsichoreContext context)
    {
        if (!context.Configuration.Dancer.EnableTillana) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.Tillana.MinLevel)
            return false;

        if (!context.HasFlourishingFinish)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.Tillana.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.Tillana, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Tillana.Name;
            context.Debug.DamageState = "Tillana";

            // Training: Record Tillana decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.Tillana.ActionId, DNCActions.Tillana.Name)
                .AsRangedBurst()
                .Reason(
                    "Flourishing Finish proc - Technical sequence",
                    "Tillana is granted by Technical Finish (Flourishing Finish buff). It's a high-potency GCD " +
                    "that also grants Last Dance Ready. Use during the Technical Finish window to extend burst phase.")
                .Factors("Flourishing Finish active", "Part of Technical sequence", "Grants Last Dance Ready")
                .Alternatives("No Flourishing Finish proc")
                .Tip("Tillana is part of your 2-minute burst - use it during Technical Finish window.")
                .Concept(DncConcepts.Tillana)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.Tillana, true, "Burst GCD used");

            return true;
        }

        return false;
    }

    #endregion

    #region Esprit Spenders

    private bool TryDanceOfTheDawn(ITerpsichoreContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.DanceOfTheDawn.MinLevel)
            return false;

        if (!context.HasDanceOfTheDawnReady)
            return false;

        // Costs 50 Esprit
        if (context.Esprit < 50)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.DanceOfTheDawn.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.DanceOfTheDawn, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.DanceOfTheDawn.Name;
            context.Debug.DamageState = $"Dance of the Dawn ({context.Esprit} Esprit)";

            // Training: Record Dance of the Dawn decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.DanceOfTheDawn.ActionId, DNCActions.DanceOfTheDawn.Name)
                .AsRangedResource("Esprit", context.Esprit)
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Dance of the Dawn Ready - enhanced Esprit spender",
                    "Dance of the Dawn (Lv.100) is an enhanced version of Saber Dance granted during Technical Finish " +
                    "window. It costs 50 Esprit and has higher potency. Always use this over Saber Dance when available.")
                .Factors("Dance of the Dawn Ready", $"Esprit: {context.Esprit}/100", "Higher potency than Saber Dance")
                .Alternatives("No Dance of the Dawn Ready proc", "Insufficient Esprit")
                .Tip("Use Dance of the Dawn during Technical Finish - it replaces Saber Dance with higher damage.")
                .Concept(DncConcepts.EspritGauge)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.EspritGauge, true, "Esprit spent");

            return true;
        }

        return false;
    }

    private bool TrySaberDance(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Dancer.EnableSaberDance) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.SaberDance.MinLevel)
            return false;

        // Skip if Dance of the Dawn is available (higher potency)
        if (context.HasDanceOfTheDawnReady && level >= DNCActions.DanceOfTheDawn.MinLevel)
            return false;

        // Costs 50 Esprit
        if (context.Esprit < 50)
        {
            context.Debug.DamageState = $"Saber Dance: {context.Esprit}/50 Esprit";
            return false;
        }

        // Hold Saber Dance for burst when Esprit is not near cap
        if (context.Configuration.Dancer.EnableBurstPooling && ShouldHoldForBurst(8f) && context.Esprit < 80)
        {
            context.Debug.DamageState = $"Holding Esprit for burst ({context.Esprit}/80)";
            return false;
        }

        // Use at 80+ to prevent overcap
        // Or use at 50+ during burst (IsInBurst covers Devilment/TechnicalFinish overlap)
        bool shouldUse = context.Esprit >= 80 ||
                         (context.Esprit >= 50 && (IsInBurst || context.HasDevilment || context.HasTechnicalFinish));

        if (!shouldUse)
        {
            context.Debug.DamageState = $"Holding Esprit ({context.Esprit}/50)";
            return false;
        }

        if (!context.ActionService.IsActionReady(DNCActions.SaberDance.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.SaberDance, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.SaberDance.Name;
            context.Debug.DamageState = $"Saber Dance ({context.Esprit} Esprit)";

            // Training: Record Saber Dance decision
            var saberReason = context.Esprit >= 80 ? "Preventing Esprit overcap" :
                context.HasDevilment || context.HasTechnicalFinish ? "Burst window active" : "Esprit dump";
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.SaberDance.ActionId, DNCActions.SaberDance.Name)
                .AsRangedResource("Esprit", context.Esprit)
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    $"Saber Dance ({saberReason})",
                    "Saber Dance is the primary Esprit spender, costing 50 Esprit. Use at 80+ Esprit to prevent " +
                    "overcapping, or at 50+ during burst windows. Esprit is generated by you and your dance partner " +
                    "dealing damage.")
                .Factors($"Esprit: {context.Esprit}/100", context.HasDevilment ? "Burst window" : "Preventing overcap", "50 Esprit cost")
                .Alternatives("Esprit < 50", "Dance of the Dawn available")
                .Tip("Never let Esprit overcap - use Saber Dance at 80+ or during burst at 50+.")
                .Concept(DncConcepts.SaberDance)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.SaberDance, true, "Esprit spent");
            if (context.Esprit >= 80)
                context.TrainingService?.RecordConceptApplication(DncConcepts.EspritOvercapping, true, "Prevented overcap");

            return true;
        }

        return false;
    }

    #endregion

    #region Proc Consumers

    private bool TryFountainfall(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Dancer.EnableProcs) return false;

        var player = context.Player;
        var level = player.Level;

        if (!context.HasSilkenFlow)
            return false;

        // Use AoE version for 3+ targets
        if (enemyCount >= AoeThreshold && level >= DNCActions.Bloodshower.MinLevel)
        {
            if (context.ActionService.IsActionReady(DNCActions.Bloodshower.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DNCActions.Bloodshower, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.Bloodshower.Name;
                    context.Debug.DamageState = "Bloodshower (AoE Flow)";

                    // Training: Record Bloodshower (AoE Flow) decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.Bloodshower.ActionId, DNCActions.Bloodshower.Name)
                        .AsProc("Silken Flow")
                        .Reason(
                            $"Silken Flow AoE proc ({enemyCount} targets)",
                            "Bloodshower is the AoE version of Fountainfall, consuming the Silken Flow proc. " +
                            "Use for 3+ targets to maximize damage. Consumes the same proc as Fountainfall.")
                        .Factors("Silken Flow proc active", $"{enemyCount} enemies", "AoE proc consumer")
                        .Alternatives("No Silken Flow proc", "Single target")
                        .Tip("Use Bloodshower at 3+ targets instead of Fountainfall.")
                        .Concept(DncConcepts.SilkenFlow)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DncConcepts.SilkenFlow, true, "Proc consumed");

                    return true;
                }
            }
        }

        // Single target
        if (level < DNCActions.Fountainfall.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.Fountainfall.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.Fountainfall, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Fountainfall.Name;
            context.Debug.DamageState = "Fountainfall (Flow)";

            // Training: Record Fountainfall decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.Fountainfall.ActionId, DNCActions.Fountainfall.Name)
                .AsProc("Silken Flow")
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Silken Flow proc - higher potency than combo",
                    "Fountainfall consumes the Silken Flow proc from Fountain or Flourish. It has higher potency " +
                    "than the basic combo and should be used before the proc expires. Generates 1 Feather.")
                .Factors("Silken Flow proc active", "Higher potency than Fountain", "Generates 1 Feather")
                .Alternatives("No Silken Flow proc")
                .Tip("Always consume Silken Flow procs - they have higher priority than basic combos.")
                .Concept(DncConcepts.SilkenFlow)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.SilkenFlow, true, "Proc consumed");

            return true;
        }

        return false;
    }

    private bool TryReverseCascade(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Dancer.EnableProcs) return false;

        var player = context.Player;
        var level = player.Level;

        if (!context.HasSilkenSymmetry)
            return false;

        // Use AoE version for 3+ targets
        if (enemyCount >= AoeThreshold && level >= DNCActions.RisingWindmill.MinLevel)
        {
            if (context.ActionService.IsActionReady(DNCActions.RisingWindmill.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DNCActions.RisingWindmill, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.RisingWindmill.Name;
                    context.Debug.DamageState = "Rising Windmill (AoE Symmetry)";

                    // Training: Record Rising Windmill (AoE Symmetry) decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.RisingWindmill.ActionId, DNCActions.RisingWindmill.Name)
                        .AsProc("Silken Symmetry")
                        .Reason(
                            $"Silken Symmetry AoE proc ({enemyCount} targets)",
                            "Rising Windmill is the AoE version of Reverse Cascade, consuming the Silken Symmetry proc. " +
                            "Use for 3+ targets to maximize damage. Consumes the same proc as Reverse Cascade.")
                        .Factors("Silken Symmetry proc active", $"{enemyCount} enemies", "AoE proc consumer")
                        .Alternatives("No Silken Symmetry proc", "Single target")
                        .Tip("Use Rising Windmill at 3+ targets instead of Reverse Cascade.")
                        .Concept(DncConcepts.SilkenSymmetry)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DncConcepts.SilkenSymmetry, true, "Proc consumed");

                    return true;
                }
            }
        }

        // Single target
        if (level < DNCActions.ReverseCascade.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.ReverseCascade.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.ReverseCascade, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.ReverseCascade.Name;
            context.Debug.DamageState = "Reverse Cascade (Symmetry)";

            // Training: Record Reverse Cascade decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.ReverseCascade.ActionId, DNCActions.ReverseCascade.Name)
                .AsProc("Silken Symmetry")
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Silken Symmetry proc - higher potency than combo",
                    "Reverse Cascade consumes the Silken Symmetry proc from Cascade or Flourish. It has higher potency " +
                    "than the basic combo and should be used before the proc expires. Generates 1 Feather.")
                .Factors("Silken Symmetry proc active", "Higher potency than Cascade", "Generates 1 Feather")
                .Alternatives("No Silken Symmetry proc")
                .Tip("Always consume Silken Symmetry procs - they have higher priority than basic combos.")
                .Concept(DncConcepts.SilkenSymmetry)
                .Record();
            context.TrainingService?.RecordConceptApplication(DncConcepts.SilkenSymmetry, true, "Proc consumed");

            return true;
        }

        return false;
    }

    #endregion

    #region Combo Filler

    private bool TryFountain(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Use AoE combo finisher for 3+ targets
        if (enemyCount >= AoeThreshold && level >= DNCActions.Bladeshower.MinLevel)
        {
            // Check if we're in AoE combo (last action was Windmill)
            if (context.ComboTimeRemaining > 0 && context.LastComboAction == DNCActions.Windmill.ActionId)
            {
                if (context.ActionService.IsActionReady(DNCActions.Bladeshower.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DNCActions.Bladeshower, player.GameObjectId))
                    {
                        context.Debug.PlannedAction = DNCActions.Bladeshower.Name;
                        context.Debug.DamageState = "Bladeshower (AoE combo)";

                        // Training: Record Bladeshower (AoE combo) decision
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(DNCActions.Bladeshower.ActionId, DNCActions.Bladeshower.Name)
                            .AsAoE(enemyCount)
                            .Reason(
                                "AoE combo finisher",
                                "Bladeshower is the AoE combo finisher after Windmill. It can proc Silken Flow " +
                                "for Bloodshower. Use for 3+ targets.")
                            .Factors("Windmill combo active", $"{enemyCount} enemies", "Can proc Silken Flow")
                            .Alternatives("Not in Windmill combo", "Single target")
                            .Tip("Complete the Windmill → Bladeshower combo for AoE damage.")
                            .Concept(DncConcepts.DanceExecution)
                            .Record();

                        return true;
                    }
                }
            }
        }

        // Single target combo finisher
        if (level < DNCActions.Fountain.MinLevel)
            return false;

        // Only use if we're in combo (last action was Cascade)
        if (context.ComboTimeRemaining <= 0 || context.LastComboAction != DNCActions.Cascade.ActionId)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.Fountain.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.Fountain, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Fountain.Name;
            context.Debug.DamageState = "Fountain (combo)";

            // Training: Record Fountain (combo finisher) decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.Fountain.ActionId, DNCActions.Fountain.Name)
                .AsRangedDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Combo finisher after Cascade",
                    "Fountain is the single-target combo finisher after Cascade. It can proc Silken Flow for " +
                    "Fountainfall. Complete the combo to maximize damage and generate procs.")
                .Factors("Cascade combo active", "Single target", "Can proc Silken Flow")
                .Alternatives("Not in Cascade combo")
                .Tip("Complete the Cascade → Fountain combo - don't break it for other GCDs.")
                .Concept(DncConcepts.DanceExecution)
                .Record();

            return true;
        }

        return false;
    }

    private bool TryCascade(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Use AoE starter for 3+ targets
        if (enemyCount >= AoeThreshold && level >= DNCActions.Windmill.MinLevel)
        {
            if (context.ActionService.IsActionReady(DNCActions.Windmill.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DNCActions.Windmill, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.Windmill.Name;
                    context.Debug.DamageState = "Windmill (AoE filler)";

                    // Training: Record Windmill (AoE starter) decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DNCActions.Windmill.ActionId, DNCActions.Windmill.Name)
                        .AsAoE(enemyCount)
                        .Reason(
                            "AoE combo starter",
                            "Windmill is the AoE combo starter. Follow with Bladeshower. It can proc Silken " +
                            "Symmetry for Rising Windmill. Use for 3+ targets.")
                        .Factors($"{enemyCount} enemies", "AoE combo starter", "Can proc Silken Symmetry")
                        .Alternatives("Single target (use Cascade)")
                        .Tip("Start the Windmill → Bladeshower combo for AoE situations.")
                        .Concept(DncConcepts.DanceExecution)
                        .Record();

                    return true;
                }
            }
        }

        // Single target - Cascade as filler
        if (!context.ActionService.IsActionReady(DNCActions.Cascade.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.Cascade, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Cascade.Name;
            context.Debug.DamageState = "Cascade (filler)";

            // Training: Record Cascade (combo starter / filler) decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DNCActions.Cascade.ActionId, DNCActions.Cascade.Name)
                .AsRangedDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(
                    "Combo starter / filler GCD",
                    "Cascade is the single-target combo starter. Follow with Fountain. It can proc Silken " +
                    "Symmetry for Reverse Cascade. This is your basic filler when no procs are available.")
                .Factors("Single target", "Combo starter", "Can proc Silken Symmetry")
                .Alternatives("3+ enemies (use Windmill)")
                .Tip("Cascade → Fountain is your basic combo - use when no procs are available.")
                .Concept(DncConcepts.DanceExecution)
                .Record();

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
    private bool TryInterrupt(ITerpsichoreContext context, IBattleChara target)
    {
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

                // Training: Record interrupt decision
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.HeadGraze.ActionId, RoleActions.HeadGraze.Name)
                    .AsInterrupt()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason(
                        "Interrupted enemy cast",
                        "Head Graze interrupts interruptible enemy casts. Watch for the pulsing cast bar indicating " +
                        "an interruptible ability. Coordinated with other Olympus instances to avoid duplicate interrupts.")
                    .Factors("Enemy casting", "Cast is interruptible", "Not reserved by other player")
                    .Alternatives("Cast not interruptible", "Already interrupted")
                    .Tip("Watch for interruptible casts - Head Graze can prevent dangerous enemy abilities.")
                    .Concept(DncConcepts.PartyUtility)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DncConcepts.PartyUtility, true, "Interrupt used");

                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        return false;
    }

    #endregion
}
