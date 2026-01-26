using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.EchidnaCore.Modules;

/// <summary>
/// Handles the Viper damage rotation.
/// Manages Reawaken sequences, twinblade combos, dual wield combos, and resource building.
/// </summary>
public sealed class DamageModule : IEchidnaModule
{
    public int Priority => 30; // Lowest priority - damage after utility
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IEchidnaContext context, bool isMoving)
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
            FFXIVConstants.MeleeTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // oGCD Phase - weave damage oGCDs during GCD
        if (context.CanExecuteOgcd)
        {
            if (TryOgcdDamage(context, target, enemyCount))
                return true;
        }

        // GCD Phase
        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        // === REAWAKEN STATE ===
        if (context.IsReawakened)
        {
            // Reawaken sequence: Generation GCDs with Legacy oGCDs
            if (TryReawakenGcd(context, target))
                return true;
        }

        // === NORMAL STATE ===

        // Priority 1: Reawaken (enter burst mode)
        if (TryReawaken(context, target))
            return true;

        // Priority 2: Continue twinblade combo (DreadCombo in progress)
        if (TryTwinbladeCombo(context, target, enemyCount))
            return true;

        // Priority 3: Uncoiled Fury (use Rattling Coils)
        if (TryUncoiledFury(context, target))
            return true;

        // Priority 4: Vicewinder/Vicepit (start twinblade combo)
        if (TryVicewinder(context, target, enemyCount))
            return true;

        // Priority 5: Maintain Noxious Gnash debuff (via Vicewinder or combo)
        // Vicewinder applies it, but we check here for low duration
        if (ShouldRefreshNoxiousGnash(context))
        {
            // If Vicewinder available, use it to refresh
            if (TryVicewinder(context, target, enemyCount, forceUse: true))
                return true;
        }

        // Priority 6: Dual wield combo
        if (TryDualWieldCombo(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IEchidnaContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Damage

    private bool TryOgcdDamage(IEchidnaContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Poised oGCDs (from twinblade combos)
        if (TryPoisedOgcd(context, target, enemyCount))
            return true;

        // Priority 2: Uncoiled follow-ups
        if (TryUncoiledOgcd(context, target))
            return true;

        return false;
    }

    private bool TryPoisedOgcd(IEchidnaContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Twinfang (from Hunter's Coil or Hunter's Den)
        if (context.HasPoisedForTwinfang)
        {
            var action = useAoe ? VPRActions.TwinfangBite : VPRActions.Twinfang;
            if (level >= action.MinLevel && context.ActionService.IsActionReady(action.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = action.Name;

                    // Training: Record Twinfang decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Using {action.Name} (Poised for Twinfang proc)",
                            "Twinfang/TwinfangBite are oGCDs granted by using Hunter's Coil/Den in twinblade combos. " +
                            "Weave immediately when the proc appears for maximum damage.")
                        .Factors(new[] { "Poised for Twinfang active", "oGCD window available" })
                        .Alternatives(new[] { "No reason to hold" })
                        .Tip("Always weave Twinfang immediately when Poised for Twinfang is active.")
                        .Concept("vpr.twinfang_twinblood")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.twinfang_twinblood", true, "Twinblade oGCD");

                    return true;
                }
            }
        }

        // Twinblood (from Swiftskin's Coil or Swiftskin's Den)
        if (context.HasPoisedForTwinblood)
        {
            var action = useAoe ? VPRActions.TwinbloodBite : VPRActions.Twinblood;
            if (level >= action.MinLevel && context.ActionService.IsActionReady(action.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = action.Name;

                    // Training: Record Twinblood decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Using {action.Name} (Poised for Twinblood proc)",
                            "Twinblood/TwinbloodBite are oGCDs granted by using Swiftskin's Coil/Den in twinblade combos. " +
                            "Weave immediately when the proc appears for maximum damage.")
                        .Factors(new[] { "Poised for Twinblood active", "oGCD window available" })
                        .Alternatives(new[] { "No reason to hold" })
                        .Tip("Always weave Twinblood immediately when Poised for Twinblood is active.")
                        .Concept("vpr.twinfang_twinblood")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.twinfang_twinblood", true, "Twinblade oGCD");

                    return true;
                }
            }
        }

        return false;
    }

    private bool TryUncoiledOgcd(IEchidnaContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // After Uncoiled Fury: Twinfang -> Twinblood
        // Check for the follow-up state (tracked via status)

        // Uncoiled Twinfang first
        if (level >= VPRActions.UncoiledTwinfang.MinLevel)
        {
            if (context.ActionService.IsActionReady(VPRActions.UncoiledTwinfang.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(VPRActions.UncoiledTwinfang, target.GameObjectId))
                {
                    context.Debug.PlannedAction = VPRActions.UncoiledTwinfang.Name;
                    context.Debug.DamageState = "Uncoiled Twinfang";

                    // Training: Record Uncoiled Twinfang decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(VPRActions.UncoiledTwinfang.ActionId, VPRActions.UncoiledTwinfang.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason("Using Uncoiled Twinfang (Uncoiled Fury follow-up)",
                            "Uncoiled Twinfang is the first oGCD follow-up after Uncoiled Fury. " +
                            "Weave immediately for bonus damage during the Uncoiled Fury sequence.")
                        .Factors(new[] { "Uncoiled Fury active", "oGCD window available" })
                        .Alternatives(new[] { "No reason to hold" })
                        .Tip("After Uncoiled Fury, weave Twinfang then Twinblood for the full sequence.")
                        .Concept("vpr.uncoiled_fury")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.uncoiled_fury", true, "Uncoiled follow-up");

                    return true;
                }
            }
        }

        // Uncoiled Twinblood second
        if (level >= VPRActions.UncoiledTwinblood.MinLevel)
        {
            if (context.ActionService.IsActionReady(VPRActions.UncoiledTwinblood.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(VPRActions.UncoiledTwinblood, target.GameObjectId))
                {
                    context.Debug.PlannedAction = VPRActions.UncoiledTwinblood.Name;
                    context.Debug.DamageState = "Uncoiled Twinblood";

                    // Training: Record Uncoiled Twinblood decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(VPRActions.UncoiledTwinblood.ActionId, VPRActions.UncoiledTwinblood.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason("Using Uncoiled Twinblood (Uncoiled Fury follow-up)",
                            "Uncoiled Twinblood is the second oGCD follow-up after Uncoiled Fury. " +
                            "Completes the Uncoiled Fury sequence for maximum damage.")
                        .Factors(new[] { "Uncoiled Twinfang used", "oGCD window available" })
                        .Alternatives(new[] { "No reason to hold" })
                        .Tip("Always complete the full Uncoiled Fury → Twinfang → Twinblood sequence.")
                        .Concept("vpr.uncoiled_fury")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.uncoiled_fury", true, "Uncoiled follow-up");

                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Reawaken Sequence

    private bool TryReawaken(IEchidnaContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < VPRActions.Reawaken.MinLevel)
            return false;

        // Already in Reawaken
        if (context.IsReawakened)
            return false;

        // Need 50 Serpent Offerings OR Ready to Reawaken buff
        if (context.SerpentOffering < 50 && !context.HasReadyToReawaken)
        {
            context.Debug.DamageState = $"Need 50 Offerings ({context.SerpentOffering}/50)";
            return false;
        }

        // Optimal timing: Have both buffs active with good duration
        if (!context.HasHuntersInstinct || context.HuntersInstinctRemaining < 10f)
        {
            context.Debug.DamageState = "Waiting for Hunter's Instinct";
            return false;
        }

        if (!context.HasSwiftscaled || context.SwiftscaledRemaining < 10f)
        {
            context.Debug.DamageState = "Waiting for Swiftscaled";
            return false;
        }

        // Make sure Noxious Gnash is on target
        if (!context.HasNoxiousGnash || context.NoxiousGnashRemaining < 10f)
        {
            context.Debug.DamageState = "Need Noxious Gnash refresh";
            return false;
        }

        if (!context.ActionService.IsActionReady(VPRActions.Reawaken.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(VPRActions.Reawaken, target.GameObjectId))
        {
            context.Debug.PlannedAction = VPRActions.Reawaken.Name;
            context.Debug.DamageState = "Entering Reawaken";

            // Training: Record Reawaken entry decision
            var entryReason = context.HasReadyToReawaken ? "Ready to Reawaken proc (free entry)" :
                              $"Serpent Offering: {context.SerpentOffering}/50";
            TrainingHelper.Decision(context.TrainingService)
                .Action(VPRActions.Reawaken.ActionId, VPRActions.Reawaken.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason($"Entering Reawaken ({entryReason})",
                    "Reawaken is VPR's burst phase. Grants 5 Anguine Tribute stacks for Generation GCDs. " +
                    "Each Generation grants a Legacy oGCD for weaving. Finish with Ouroboros.")
                .Factors(new[] { entryReason, $"Hunter's Instinct: {context.HuntersInstinctRemaining:F1}s", $"Swiftscaled: {context.SwiftscaledRemaining:F1}s", $"Noxious Gnash: {context.NoxiousGnashRemaining:F1}s" })
                .Alternatives(new[] { "Wait for buff refresh", "Wait for Serpent's Ire" })
                .Tip("Enter Reawaken with good buff duration. Use after Serpent's Ire for Ready to Reawaken proc.")
                .Concept("vpr.reawaken_entry")
                .Record();
            context.TrainingService?.RecordConceptApplication("vpr.reawaken_entry", true, "Burst phase entry");

            return true;
        }

        return false;
    }

    private bool TryReawakenGcd(IEchidnaContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Get the correct Generation based on Anguine Tribute count
        var action = VPRActions.GetGenerationGcd(context.AnguineTribute);

        // Ouroboros is the finisher at 1 tribute
        if (context.AnguineTribute == 1 && level >= VPRActions.Ouroboros.MinLevel)
        {
            action = VPRActions.Ouroboros;
        }

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (Tribute: {context.AnguineTribute})";

            // Training: Record Generation/Ouroboros decision
            var isOuroboros = action == VPRActions.Ouroboros;
            if (isOuroboros)
            {
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Using Ouroboros (Reawaken finisher)",
                        "Ouroboros is the powerful finisher that ends the Reawaken phase. " +
                        "Use at 1 Anguine Tribute remaining after all Generation GCDs.")
                    .Factors(new[] { "1 Anguine Tribute remaining", "Ending Reawaken phase" })
                    .Alternatives(new[] { "Use more Generations first" })
                    .Tip("Ouroboros ends Reawaken. Make sure you've used all 4 Generation GCDs first.")
                    .Concept("vpr.generation_sequence")
                    .Record();
            }
            else
            {
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Using {action.Name} (Anguine Tribute: {context.AnguineTribute})",
                        "Generation GCDs are your Reawaken burst rotation. Each consumes 1 Anguine Tribute " +
                        "and grants a Legacy oGCD for weaving. Execute all 4 Generations before Ouroboros.")
                    .Factors(new[] { $"Anguine Tribute: {context.AnguineTribute}", "Reawaken active" })
                    .Alternatives(new[] { "No reason to hold during Reawaken" })
                    .Tip("Weave Legacy oGCDs between Generation GCDs for maximum burst damage.")
                    .Concept("vpr.generation_sequence")
                    .Record();
            }
            context.TrainingService?.RecordConceptApplication("vpr.generation_sequence", true,
                isOuroboros ? "Reawaken finisher" : "Reawaken GCD");

            return true;
        }

        return false;
    }

    #endregion

    #region Twinblade Combo

    private bool TryTwinbladeCombo(IEchidnaContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Check DreadCombo state for twinblade continuation
        switch (context.DreadCombo)
        {
            case VPRActions.DreadCombo.DreadwindyReady:
            case VPRActions.DreadCombo.HunterCoilReady:
                // Use Hunter's Coil
                if (level >= VPRActions.HuntersCoil.MinLevel)
                {
                    if (context.ActionService.IsActionReady(VPRActions.HuntersCoil.ActionId))
                    {
                        if (context.ActionService.ExecuteGcd(VPRActions.HuntersCoil, target.GameObjectId))
                        {
                            context.Debug.PlannedAction = VPRActions.HuntersCoil.Name;
                            context.Debug.DamageState = "Hunter's Coil (Twinblade)";

                            // Training: Record Hunter's Coil decision
                            TrainingHelper.Decision(context.TrainingService)
                                .Action(VPRActions.HuntersCoil.ActionId, VPRActions.HuntersCoil.Name)
                                .AsMeleeDamage()
                                .Target(target.Name?.TextValue ?? "Target")
                                .Reason("Using Hunter's Coil (Twinblade combo)",
                                    "Hunter's Coil is part of the twinblade combo after Vicewinder. " +
                                    "Grants Poised for Twinfang for an oGCD weave opportunity.")
                                .Factors(new[] { $"DreadCombo: {context.DreadCombo}", "Twinblade combo active" })
                                .Alternatives(new[] { "Use Swiftskin's Coil instead" })
                                .Tip("Hunter's Coil grants Twinfang proc. Weave it before your next GCD.")
                                .Concept("vpr.dread_combo")
                                .Record();
                            context.TrainingService?.RecordConceptApplication("vpr.dread_combo", true, "Twinblade combo");

                            return true;
                        }
                    }
                }
                // Fallback to Swiftskin's Coil
                if (level >= VPRActions.SwiftskinsCoil.MinLevel)
                {
                    if (context.ActionService.IsActionReady(VPRActions.SwiftskinsCoil.ActionId))
                    {
                        if (context.ActionService.ExecuteGcd(VPRActions.SwiftskinsCoil, target.GameObjectId))
                        {
                            context.Debug.PlannedAction = VPRActions.SwiftskinsCoil.Name;
                            context.Debug.DamageState = "Swiftskin's Coil (Twinblade)";

                            // Training: Record Swiftskin's Coil decision
                            TrainingHelper.Decision(context.TrainingService)
                                .Action(VPRActions.SwiftskinsCoil.ActionId, VPRActions.SwiftskinsCoil.Name)
                                .AsMeleeDamage()
                                .Target(target.Name?.TextValue ?? "Target")
                                .Reason("Using Swiftskin's Coil (Twinblade combo)",
                                    "Swiftskin's Coil is part of the twinblade combo after Vicewinder. " +
                                    "Grants Poised for Twinblood for an oGCD weave opportunity.")
                                .Factors(new[] { $"DreadCombo: {context.DreadCombo}", "Twinblade combo active" })
                                .Alternatives(new[] { "Use Hunter's Coil instead" })
                                .Tip("Swiftskin's Coil grants Twinblood proc. Weave it before your next GCD.")
                                .Concept("vpr.dread_combo")
                                .Record();
                            context.TrainingService?.RecordConceptApplication("vpr.dread_combo", true, "Twinblade combo");

                            return true;
                        }
                    }
                }
                break;

            case VPRActions.DreadCombo.SwiftskinCoilReady:
                // Use Swiftskin's Coil
                if (level >= VPRActions.SwiftskinsCoil.MinLevel)
                {
                    if (context.ActionService.IsActionReady(VPRActions.SwiftskinsCoil.ActionId))
                    {
                        if (context.ActionService.ExecuteGcd(VPRActions.SwiftskinsCoil, target.GameObjectId))
                        {
                            context.Debug.PlannedAction = VPRActions.SwiftskinsCoil.Name;
                            context.Debug.DamageState = "Swiftskin's Coil (Twinblade)";

                            // Training: Record Swiftskin's Coil decision
                            TrainingHelper.Decision(context.TrainingService)
                                .Action(VPRActions.SwiftskinsCoil.ActionId, VPRActions.SwiftskinsCoil.Name)
                                .AsMeleeDamage()
                                .Target(target.Name?.TextValue ?? "Target")
                                .Reason("Using Swiftskin's Coil (Twinblade combo continuation)",
                                    "Swiftskin's Coil completes the twinblade combo. " +
                                    "Grants Poised for Twinblood for an oGCD weave opportunity.")
                                .Factors(new[] { "SwiftskinCoilReady state", "Continuing twinblade combo" })
                                .Alternatives(new[] { "No alternatives - continue combo" })
                                .Tip("Complete the twinblade combo and weave the Twinblood proc.")
                                .Concept("vpr.dread_combo")
                                .Record();
                            context.TrainingService?.RecordConceptApplication("vpr.dread_combo", true, "Twinblade combo");

                            return true;
                        }
                    }
                }
                break;

            case VPRActions.DreadCombo.PitReady:
            case VPRActions.DreadCombo.HunterDenReady:
                // AoE: Use Hunter's Den
                if (level >= VPRActions.HuntersDen.MinLevel)
                {
                    if (context.ActionService.IsActionReady(VPRActions.HuntersDen.ActionId))
                    {
                        if (context.ActionService.ExecuteGcd(VPRActions.HuntersDen, player.GameObjectId))
                        {
                            context.Debug.PlannedAction = VPRActions.HuntersDen.Name;
                            context.Debug.DamageState = "Hunter's Den (AoE Twinblade)";

                            // Training: Record Hunter's Den decision
                            TrainingHelper.Decision(context.TrainingService)
                                .Action(VPRActions.HuntersDen.ActionId, VPRActions.HuntersDen.Name)
                                .AsAoE(enemyCount)
                                .Reason("Using Hunter's Den (AoE Twinblade combo)",
                                    "Hunter's Den is the AoE version of Hunter's Coil. " +
                                    "Use in AoE situations after Vicepit. Grants Poised for TwinfangBite.")
                                .Factors(new[] { $"Enemies: {enemyCount}", "AoE twinblade combo" })
                                .Alternatives(new[] { "Use Swiftskin's Den instead" })
                                .Tip("Hunter's Den grants TwinfangBite proc for AoE oGCD damage.")
                                .Concept("vpr.dread_combo")
                                .Record();
                            context.TrainingService?.RecordConceptApplication("vpr.dread_combo", true, "AoE Twinblade");

                            return true;
                        }
                    }
                }
                break;

            case VPRActions.DreadCombo.SwiftskinDenReady:
                // AoE: Use Swiftskin's Den
                if (level >= VPRActions.SwiftskinsDen.MinLevel)
                {
                    if (context.ActionService.IsActionReady(VPRActions.SwiftskinsDen.ActionId))
                    {
                        if (context.ActionService.ExecuteGcd(VPRActions.SwiftskinsDen, player.GameObjectId))
                        {
                            context.Debug.PlannedAction = VPRActions.SwiftskinsDen.Name;
                            context.Debug.DamageState = "Swiftskin's Den (AoE Twinblade)";

                            // Training: Record Swiftskin's Den decision
                            TrainingHelper.Decision(context.TrainingService)
                                .Action(VPRActions.SwiftskinsDen.ActionId, VPRActions.SwiftskinsDen.Name)
                                .AsAoE(enemyCount)
                                .Reason("Using Swiftskin's Den (AoE Twinblade combo)",
                                    "Swiftskin's Den completes the AoE twinblade combo. " +
                                    "Grants Poised for TwinbloodBite for AoE oGCD damage.")
                                .Factors(new[] { $"Enemies: {enemyCount}", "AoE twinblade continuation" })
                                .Alternatives(new[] { "No alternatives - continue combo" })
                                .Tip("Complete the AoE twinblade combo and weave the TwinbloodBite proc.")
                                .Concept("vpr.dread_combo")
                                .Record();
                            context.TrainingService?.RecordConceptApplication("vpr.dread_combo", true, "AoE Twinblade");

                            return true;
                        }
                    }
                }
                break;
        }

        return false;
    }

    private bool TryVicewinder(IEchidnaContext context, IBattleChara target, int enemyCount, bool forceUse = false)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Don't start new twinblade if DreadCombo is in progress
        if (context.DreadCombo != VPRActions.DreadCombo.None && !forceUse)
            return false;

        if (useAoe && level >= VPRActions.Vicepit.MinLevel)
        {
            if (context.ActionService.IsActionReady(VPRActions.Vicepit.ActionId))
            {
                if (context.ActionService.ExecuteGcd(VPRActions.Vicepit, player.GameObjectId))
                {
                    context.Debug.PlannedAction = VPRActions.Vicepit.Name;
                    context.Debug.DamageState = "Vicepit (AoE Twinblade start)";

                    // Training: Record Vicepit decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(VPRActions.Vicepit.ActionId, VPRActions.Vicepit.Name)
                        .AsAoE(enemyCount)
                        .Reason("Using Vicepit (AoE Twinblade starter)",
                            "Vicepit starts the AoE twinblade combo. Applies Noxious Gnash to targets " +
                            "and grants +1 Rattling Coil stack. Follow with Hunter's Den / Swiftskin's Den.")
                        .Factors(new[] { $"Enemies: {enemyCount}", $"Rattling Coils: {context.RattlingCoils}" })
                        .Alternatives(new[] { "Use Vicewinder for ST", "Continue dual wield combo" })
                        .Tip("Vicepit applies Noxious Gnash and builds Rattling Coils for Uncoiled Fury.")
                        .Concept("vpr.vicewinder")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.vicewinder", true, "AoE Twinblade starter");
                    context.TrainingService?.RecordConceptApplication("vpr.noxious_gnash", true, "Debuff application");

                    return true;
                }
            }
        }

        if (level >= VPRActions.Vicewinder.MinLevel)
        {
            if (context.ActionService.IsActionReady(VPRActions.Vicewinder.ActionId))
            {
                if (context.ActionService.ExecuteGcd(VPRActions.Vicewinder, target.GameObjectId))
                {
                    context.Debug.PlannedAction = VPRActions.Vicewinder.Name;
                    context.Debug.DamageState = "Vicewinder (Twinblade start)";

                    // Training: Record Vicewinder decision
                    var reason = forceUse ? "Refreshing Noxious Gnash" : "Starting twinblade combo";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(VPRActions.Vicewinder.ActionId, VPRActions.Vicewinder.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Using Vicewinder ({reason})",
                            "Vicewinder starts the twinblade combo. Applies/refreshes Noxious Gnash (+10% damage debuff) " +
                            "and grants +1 Rattling Coil stack. Follow with Hunter's Coil / Swiftskin's Coil.")
                        .Factors(new[] { $"Noxious Gnash: {(context.HasNoxiousGnash ? $"{context.NoxiousGnashRemaining:F1}s" : "Not applied")}", $"Rattling Coils: {context.RattlingCoils}" })
                        .Alternatives(new[] { "Continue dual wield combo", "Wait for charge" })
                        .Tip("Vicewinder is your main Noxious Gnash applicator. Keep the debuff active at all times.")
                        .Concept("vpr.vicewinder")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.vicewinder", true, "Twinblade starter");
                    context.TrainingService?.RecordConceptApplication("vpr.noxious_gnash", true, "Debuff application");

                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Uncoiled Fury

    private bool TryUncoiledFury(IEchidnaContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < VPRActions.UncoiledFury.MinLevel)
            return false;

        // Need Rattling Coils
        if (context.RattlingCoils <= 0)
            return false;

        // Don't use during Reawaken
        if (context.IsReawakened)
            return false;

        // Good to use when:
        // 1. At range (movement)
        // 2. Have max coils (would overcap)
        // 3. As filler when other options unavailable

        // Check if at range
        var dx = player.Position.X - target.Position.X;
        var dz = player.Position.Z - target.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dz * dz);

        // Use at range or if capped on coils
        bool shouldUse = distance > FFXIVConstants.MeleeTargetingRange ||
                         context.RattlingCoils >= 3 ||
                         context.IsMoving;

        if (!shouldUse)
            return false;

        if (!context.ActionService.IsActionReady(VPRActions.UncoiledFury.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(VPRActions.UncoiledFury, target.GameObjectId))
        {
            context.Debug.PlannedAction = VPRActions.UncoiledFury.Name;
            context.Debug.DamageState = $"Uncoiled Fury (Coils: {context.RattlingCoils})";

            // Training: Record Uncoiled Fury decision
            var reason = context.RattlingCoils >= 3 ? "Coils capped (prevent overcap)" :
                         context.IsMoving ? "Movement GCD" :
                         "Ranged GCD option";
            TrainingHelper.Decision(context.TrainingService)
                .Action(VPRActions.UncoiledFury.ActionId, VPRActions.UncoiledFury.Name)
                .AsMeleeResource("Rattling Coils", context.RattlingCoils)
                .Reason($"Using Uncoiled Fury ({reason})",
                    "Uncoiled Fury consumes 1 Rattling Coil for a ranged GCD. Use during movement, " +
                    "at range, or when capped on coils. Follow with Uncoiled Twinfang → Twinblood.")
                .Factors(new[] { $"Rattling Coils: {context.RattlingCoils}", reason })
                .Alternatives(new[] { "Save for movement", "Use melee GCDs when in range" })
                .Tip("Uncoiled Fury is your movement tool. Save coils for forced disengages when possible.")
                .Concept("vpr.rattling_coil")
                .Record();
            context.TrainingService?.RecordConceptApplication("vpr.rattling_coil", true, "Coil spending");

            return true;
        }

        return false;
    }

    #endregion

    #region Dual Wield Combo

    private bool TryDualWieldCombo(IEchidnaContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        if (useAoe && level >= VPRActions.SteelMaw.MinLevel)
        {
            return TryAoeDualWieldCombo(context, player, enemyCount);
        }

        return TrySingleTargetDualWieldCombo(context, target);
    }

    private bool TrySingleTargetDualWieldCombo(IEchidnaContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine combo state and next action
        ActionDefinition action;
        string comboInfo;

        // Check for finisher (step 3) - based on venom buffs
        if (context.ComboStep == 2)
        {
            // From Hunter's Sting path
            if (context.LastComboAction == VPRActions.HuntersSting.ActionId)
            {
                // Check venom to determine positional
                if (context.HasHindstungVenom)
                {
                    action = VPRActions.HindstingStrike; // Rear
                    comboInfo = "Hindsting (rear)";
                }
                else if (context.HasFlankstungVenom)
                {
                    action = VPRActions.FlankstingStrike; // Flank
                    comboInfo = "Flanksting (flank)";
                }
                else
                {
                    // No venom - use flank as default
                    action = VPRActions.FlankstingStrike;
                    comboInfo = "Flanksting (default)";
                }
            }
            // From Swiftskin's Sting path
            else if (context.LastComboAction == VPRActions.SwiftskinsString.ActionId)
            {
                if (context.HasHindsbaneVenom)
                {
                    action = VPRActions.HindsbaneFang; // Rear
                    comboInfo = "Hindsbane (rear)";
                }
                else if (context.HasFlanksbaneVenom)
                {
                    action = VPRActions.FlanksbaneFang; // Flank
                    comboInfo = "Flanksbane (flank)";
                }
                else
                {
                    // No venom - use flank as default
                    action = VPRActions.FlanksbaneFang;
                    comboInfo = "Flanksbane (default)";
                }
            }
            else
            {
                // Unknown combo state, start fresh
                action = GetStarterAction(context);
                comboInfo = "Restart combo";
            }
        }
        // Second hit (step 2)
        else if (context.ComboStep == 1)
        {
            if (context.LastComboAction == VPRActions.SteelFangs.ActionId)
            {
                action = VPRActions.HuntersSting;
                comboInfo = "Hunter's Sting";
            }
            else if (context.LastComboAction == VPRActions.ReavingFangs.ActionId)
            {
                action = VPRActions.SwiftskinsString;
                comboInfo = "Swiftskin's Sting";
            }
            else
            {
                // Unknown combo state, start fresh
                action = GetStarterAction(context);
                comboInfo = "Restart combo";
            }
        }
        // Combo starter (step 1)
        else
        {
            action = GetStarterAction(context);
            comboInfo = action == VPRActions.SteelFangs ? "Steel Fangs" : "Reaving Fangs";
        }

        if (level < action.MinLevel)
        {
            // Fall back to basic action
            action = VPRActions.SteelFangs;
            comboInfo = "Steel Fangs (level)";
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{comboInfo} (combo {context.ComboStep + 1})";

            // Training: Record dual wield combo decision
            var isFinisher = context.ComboStep == 2;
            var isPositional = action == VPRActions.HindstingStrike || action == VPRActions.FlankstingStrike ||
                               action == VPRActions.HindsbaneFang || action == VPRActions.FlanksbaneFang;

            if (isPositional)
            {
                // Positional finisher
                var isRear = action == VPRActions.HindstingStrike || action == VPRActions.HindsbaneFang;
                var positionalName = isRear ? "rear" : "flank";
                var hitPositional = isRear ? context.IsAtRear : context.IsAtFlank;
                hitPositional = hitPositional || context.HasTrueNorth || context.TargetHasPositionalImmunity;

                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsPositional(hitPositional, positionalName)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Using {action.Name} (dual wield finisher)",
                        $"{action.Name} is a {positionalName} positional finisher. Venom buffs indicate which positional " +
                        "to use: Hindstung/Hindsbane = rear, Flankstung/Flanksbane = flank.")
                    .Factors(new[] { $"Combo step: {context.ComboStep + 1}", hitPositional ? "Positional hit" : "Positional missed", $"Positional: {positionalName}" })
                    .Alternatives(new[] { "Use other finisher for different positional" })
                    .Tip("Follow venom buffs to know which positional is required. Use True North if out of position.")
                    .Concept("vpr.positionals")
                    .Record();
                context.TrainingService?.RecordConceptApplication("vpr.positionals", hitPositional, "Dual wield positional");
            }
            else if (isFinisher)
            {
                // Non-positional finisher (shouldn't happen in ST, but handle it)
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsCombo(context.ComboStep + 1)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Using {action.Name} (combo finisher)",
                        "Dual wield combo finisher grants Serpent Offering gauge toward Reawaken.")
                    .Factors(new[] { $"Combo step: {context.ComboStep + 1}" })
                    .Alternatives(new[] { "No alternatives" })
                    .Tip("Complete your combo to build Serpent Offering for Reawaken.")
                    .Concept("vpr.combo_basics")
                    .Record();
                context.TrainingService?.RecordConceptApplication("vpr.combo_basics", true, "Combo finisher");
            }
            else if (context.ComboStep == 1)
            {
                // Second hit (Hunter's Sting / Swiftskin's Sting)
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsCombo(context.ComboStep + 1)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Using {action.Name} (combo continuation)",
                        $"{action.Name} continues the dual wield combo and maintains your buffs. " +
                        "Hunter's Sting path = Hunter's Instinct, Swiftskin's Sting path = Swiftscaled.")
                    .Factors(new[] { $"Combo step: {context.ComboStep + 1}", "Continuing combo" })
                    .Alternatives(new[] { "Use other path for different buff" })
                    .Tip("Alternate between Steel/Reaving paths to maintain both Hunter's Instinct and Swiftscaled.")
                    .Concept("vpr.buff_cycling")
                    .Record();
                context.TrainingService?.RecordConceptApplication("vpr.buff_cycling", true, "Buff maintenance");
            }
            else
            {
                // Combo starter
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsCombo(context.ComboStep + 1)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Using {action.Name} (combo starter)",
                        $"{action.Name} starts the dual wield combo. Steel path refreshes Hunter's Instinct, " +
                        "Reaving path refreshes Swiftscaled. Enhanced versions (Honed Steel/Reavers) deal more damage.")
                    .Factors(new[] { $"Honed Steel: {context.HasHonedSteel}", $"Honed Reavers: {context.HasHonedReavers}" })
                    .Alternatives(new[] { "Use other starter for different buff path" })
                    .Tip("Start with the enhanced version when available. Prioritize the buff with shorter duration.")
                    .Concept("vpr.combo_basics")
                    .Record();
                context.TrainingService?.RecordConceptApplication("vpr.combo_basics", true, "Combo starter");
            }

            return true;
        }

        return false;
    }

    private bool TryAoeDualWieldCombo(IEchidnaContext context, IPlayerCharacter player, int enemyCount)
    {
        var level = player.Level;
        ActionDefinition action;
        string comboInfo;

        // Finisher (step 3)
        if (context.ComboStep == 2)
        {
            if (context.LastComboAction == VPRActions.HuntersBite.ActionId)
            {
                action = VPRActions.JaggedMaw;
                comboInfo = "Jagged Maw";
            }
            else if (context.LastComboAction == VPRActions.SwiftskinsBite.ActionId)
            {
                action = VPRActions.BloodiedMaw;
                comboInfo = "Bloodied Maw";
            }
            else
            {
                action = GetAoeStarterAction(context);
                comboInfo = "Restart AoE combo";
            }
        }
        // Second hit (step 2)
        else if (context.ComboStep == 1)
        {
            if (context.LastComboAction == VPRActions.SteelMaw.ActionId)
            {
                action = VPRActions.HuntersBite;
                comboInfo = "Hunter's Bite";
            }
            else if (context.LastComboAction == VPRActions.ReavingMaw.ActionId)
            {
                action = VPRActions.SwiftskinsBite;
                comboInfo = "Swiftskin's Bite";
            }
            else
            {
                action = GetAoeStarterAction(context);
                comboInfo = "Restart AoE combo";
            }
        }
        // Starter (step 1)
        else
        {
            action = GetAoeStarterAction(context);
            comboInfo = action == VPRActions.SteelMaw ? "Steel Maw" : "Reaving Maw";
        }

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{comboInfo} (AoE combo {context.ComboStep + 1})";

            // Training: Record AoE dual wield combo decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsAoE(enemyCount)
                .Reason($"Using {action.Name} (AoE combo step {context.ComboStep + 1})",
                    "VPR's AoE combo mirrors the single-target rotation. Steel Maw → Hunter's Bite → Jagged Maw " +
                    "or Reaving Maw → Swiftskin's Bite → Bloodied Maw. Maintains buffs and builds Serpent Offering.")
                .Factors(new[] { $"Enemies: {enemyCount}", $"Combo step: {context.ComboStep + 1}" })
                .Alternatives(new[] { "Use ST combo for single target" })
                .Tip("Use AoE combo at 3+ enemies. Same buff rotation logic as single-target.")
                .Concept("vpr.combo_basics")
                .Record();
            context.TrainingService?.RecordConceptApplication("vpr.combo_basics", true, "AoE combo");

            return true;
        }

        return false;
    }

    private ActionDefinition GetStarterAction(IEchidnaContext context)
    {
        // Use enhanced version if available
        if (context.HasHonedReavers)
            return VPRActions.ReavingFangs;
        if (context.HasHonedSteel)
            return VPRActions.SteelFangs;

        // Alternate based on which buff we need
        // If missing Hunter's Instinct, use Steel Fangs path
        // If missing Swiftscaled, use Reaving Fangs path
        if (!context.HasHuntersInstinct || context.HuntersInstinctRemaining < context.SwiftscaledRemaining)
            return VPRActions.SteelFangs;

        return VPRActions.ReavingFangs;
    }

    private ActionDefinition GetAoeStarterAction(IEchidnaContext context)
    {
        // Use enhanced version if available
        if (context.HasHonedReavers)
            return VPRActions.ReavingMaw;
        if (context.HasHonedSteel)
            return VPRActions.SteelMaw;

        // Alternate based on which buff we need
        if (!context.HasHuntersInstinct || context.HuntersInstinctRemaining < context.SwiftscaledRemaining)
            return VPRActions.SteelMaw;

        return VPRActions.ReavingMaw;
    }

    #endregion

    #region Helpers

    private bool ShouldRefreshNoxiousGnash(IEchidnaContext context)
    {
        // Refresh if missing or about to expire
        return !context.HasNoxiousGnash || context.NoxiousGnashRemaining < 5f;
    }

    #endregion
}
