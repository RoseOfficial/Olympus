using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ThanatosCore.Context;

namespace Olympus.Rotation.ThanatosCore.Modules;

/// <summary>
/// Handles the Reaper damage rotation.
/// Manages Enshroud sequences, Soul Reaver, combo actions, and resource building.
/// </summary>
public sealed class DamageModule : IThanatosModule
{
    public int Priority => 30; // Lowest priority - damage after utility
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IThanatosContext context, bool isMoving)
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

        // === ENSHROUD STATE ===
        if (context.IsEnshrouded)
        {
            // Priority 1: Perfectio (post-Communio proc)
            if (TryPerfectio(context, target))
                return true;

            // Priority 2: Communio (Enshroud finisher at 1 Lemure Shroud)
            if (TryCommunio(context, target))
                return true;

            // Priority 3: Void/Cross Reaping (Enshroud GCDs)
            if (TryEnshroudGcd(context, target, enemyCount))
                return true;
        }

        // === NORMAL STATE ===

        // Priority 4: Perfectio Parata proc (if carried outside Enshroud)
        if (TryPerfectio(context, target))
            return true;

        // Priority 5: Plentiful Harvest (consume Immortal Sacrifice)
        if (TryPlentifulHarvest(context, target))
            return true;

        // Priority 6: Soul Reaver GCDs (Gibbet/Gallows/Guillotine)
        if (TrySoulReaverGcd(context, target, enemyCount))
            return true;

        // Priority 7: Harvest Moon (if Soulsow available and ranged)
        if (TryHarvestMoon(context, target))
            return true;

        // Priority 8: Shadow of Death / Whorl of Death (maintain debuff)
        if (TryDeathsDesign(context, target, enemyCount))
            return true;

        // Priority 9: Soul Slice / Soul Scythe (build Soul gauge)
        if (TrySoulBuilder(context, target, enemyCount))
            return true;

        // Priority 10: Basic combo rotation
        if (TryBasicCombo(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IThanatosContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Damage

    private bool TryOgcdDamage(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // During Enshroud: Lemure's Slice/Scythe
        if (context.IsEnshrouded)
        {
            if (TryLemuresSlice(context, target, enemyCount))
                return true;

            // Sacrificium (Dawntrail)
            if (TrySacrificium(context, target))
                return true;
        }

        // Soul spenders (outside Enshroud and Soul Reaver)
        if (!context.IsEnshrouded && !context.HasSoulReaver)
        {
            if (TrySoulSpender(context, target, enemyCount))
                return true;
        }

        return false;
    }

    private bool TryLemuresSlice(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.LemuresSlice.MinLevel)
            return false;

        // Need 2 Void Shroud
        if (context.VoidShroud < 2)
            return false;

        var useAoe = enemyCount >= AoeThreshold;
        var action = useAoe ? RPRActions.LemuresScythe : RPRActions.LemuresSlice;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (Void: {context.VoidShroud})";

            // Training: Record Lemure's Slice/Scythe decision
            MeleeDpsTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                action.ActionId,
                action.Name,
                target.Name?.TextValue ?? "Target",
                $"Using {action.Name} during Enshroud",
                $"{action.Name} consumes 2 Void Shroud stacks for bonus damage during Enshroud. " +
                "Build Void Shroud by using Void/Cross Reaping GCDs, then spend with Lemure's Slice.",
                new[] { $"Void Shroud: {context.VoidShroud}/2", "Enshroud active", "oGCD window" },
                new[] { "Wait for more Void Shroud" },
                "Weave Lemure's Slice between Reaping GCDs. Build 2 Void Shroud, then spend.",
                "rpr_lemure_slice");
            context.TrainingService?.RecordConceptApplication("rpr_lemure_slice", true, "Enshroud oGCD");

            return true;
        }

        return false;
    }

    private bool TrySacrificium(IThanatosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.Sacrificium.MinLevel)
            return false;

        // Requires Oblatio proc
        if (!context.HasOblatio)
            return false;

        if (!context.ActionService.IsActionReady(RPRActions.Sacrificium.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(RPRActions.Sacrificium, target.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.Sacrificium.Name;
            context.Debug.DamageState = "Sacrificium";

            // Training: Record Sacrificium decision
            MeleeDpsTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                RPRActions.Sacrificium.ActionId,
                RPRActions.Sacrificium.Name,
                target.Name?.TextValue ?? "Target",
                "Using Sacrificium (Oblatio proc)",
                "Sacrificium is a high-potency oGCD available during Enshroud when Oblatio proc is active. " +
                "Oblatio is granted at the start of Enshroud. Use before Communio finisher.",
                new[] { "Oblatio proc active", "Enshroud active", "oGCD window" },
                new[] { "No reason to hold" },
                "Use Sacrificium immediately when you have Oblatio proc during Enshroud.",
                "rpr_sacrificium");
            context.TrainingService?.RecordConceptApplication("rpr_sacrificium", true, "Enshroud proc");

            return true;
        }

        return false;
    }

    private bool TrySoulSpender(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need 50 Soul to spend
        if (context.Soul < 50)
            return false;

        // Prioritize Gluttony (2 Soul Reaver stacks, 60s CD)
        if (level >= RPRActions.Gluttony.MinLevel)
        {
            if (context.ActionService.IsActionReady(RPRActions.Gluttony.ActionId))
            {
                // Only use Gluttony if we can spend the Soul Reaver stacks
                // (not about to enter Enshroud)
                if (context.Shroud < 50 || !context.ActionService.IsActionReady(RPRActions.Enshroud.ActionId))
                {
                    if (context.ActionService.ExecuteOgcd(RPRActions.Gluttony, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = RPRActions.Gluttony.Name;
                        context.Debug.DamageState = "Gluttony (2 Soul Reaver)";

                        // Training: Record Gluttony decision
                        MeleeDpsTrainingHelper.RecordResourceDecision(
                            context.TrainingService,
                            RPRActions.Gluttony.ActionId,
                            RPRActions.Gluttony.Name,
                            "Soul",
                            context.Soul,
                            "Using Gluttony for 2 Soul Reaver stacks",
                            "Gluttony is your premium Soul spender, granting 2 Soul Reaver stacks on a 60s cooldown. " +
                            "Soul Reaver enables Gibbet/Gallows finishers for high damage and Shroud generation.",
                            new[] { $"Soul: {context.Soul}/50", "Gluttony ready", "Not entering Enshroud soon" },
                            new[] { "Wait for Enshroud alignment", "Save for burst" },
                            "Prioritize Gluttony over Blood Stalk. Use before Enshroud to maximize Soul Reaver value.",
                            "rpr_gluttony");
                        context.TrainingService?.RecordConceptApplication("rpr_gluttony", true, "Premium Soul spender");

                        return true;
                    }
                }
            }
        }

        // Unveiled variants if we have Enhanced buffs
        if (level >= RPRActions.UnveiledGibbet.MinLevel)
        {
            if (context.HasEnhancedGibbet)
            {
                if (context.ActionService.IsActionReady(RPRActions.UnveiledGibbet.ActionId))
                {
                    if (context.ActionService.ExecuteOgcd(RPRActions.UnveiledGibbet, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = RPRActions.UnveiledGibbet.Name;
                        context.Debug.DamageState = "Unveiled Gibbet";

                        // Training: Record Unveiled Gibbet decision
                        MeleeDpsTrainingHelper.RecordResourceDecision(
                            context.TrainingService,
                            RPRActions.UnveiledGibbet.ActionId,
                            RPRActions.UnveiledGibbet.Name,
                            "Soul",
                            context.Soul,
                            "Using Unveiled Gibbet (Enhanced Gibbet buff)",
                            "Unveiled Gibbet/Gallows are enhanced versions of Blood Stalk that appear when you have " +
                            "the matching Enhanced buff. They deal more damage and grant 1 Soul Reaver stack.",
                            new[] { $"Soul: {context.Soul}/50", "Enhanced Gibbet active", "oGCD window" },
                            new[] { "Wait for Gluttony" },
                            "Use Unveiled variants when Enhanced buffs are active for better damage than Blood Stalk.",
                            "rpr_unveiled");
                        context.TrainingService?.RecordConceptApplication("rpr_unveiled", true, "Enhanced Soul spender");

                        return true;
                    }
                }
            }
            else if (context.HasEnhancedGallows)
            {
                if (context.ActionService.IsActionReady(RPRActions.UnveiledGallows.ActionId))
                {
                    if (context.ActionService.ExecuteOgcd(RPRActions.UnveiledGallows, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = RPRActions.UnveiledGallows.Name;
                        context.Debug.DamageState = "Unveiled Gallows";

                        // Training: Record Unveiled Gallows decision
                        MeleeDpsTrainingHelper.RecordResourceDecision(
                            context.TrainingService,
                            RPRActions.UnveiledGallows.ActionId,
                            RPRActions.UnveiledGallows.Name,
                            "Soul",
                            context.Soul,
                            "Using Unveiled Gallows (Enhanced Gallows buff)",
                            "Unveiled Gallows appears when Enhanced Gallows is active. Use this instead of Blood Stalk " +
                            "for bonus damage while still gaining 1 Soul Reaver stack.",
                            new[] { $"Soul: {context.Soul}/50", "Enhanced Gallows active", "oGCD window" },
                            new[] { "Wait for Gluttony" },
                            "Follow the Enhanced buff - it tells you which finisher you used last for tracking.",
                            "rpr_unveiled");
                        context.TrainingService?.RecordConceptApplication("rpr_unveiled", true, "Enhanced Soul spender");

                        return true;
                    }
                }
            }
        }

        // Basic Blood Stalk / Grim Swathe
        var useAoe = enemyCount >= AoeThreshold && level >= RPRActions.GrimSwathe.MinLevel;
        var action = useAoe ? RPRActions.GrimSwathe : RPRActions.BloodStalk;

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (1 Soul Reaver)";

            // Training: Record Blood Stalk / Grim Swathe decision
            MeleeDpsTrainingHelper.RecordResourceDecision(
                context.TrainingService,
                action.ActionId,
                action.Name,
                "Soul",
                context.Soul,
                $"Using {action.Name} for 1 Soul Reaver stack",
                $"{action.Name} spends 50 Soul to grant 1 Soul Reaver stack. Use when Gluttony is on cooldown " +
                "and no Enhanced buffs are available. Grim Swathe is the AoE version.",
                new[] { $"Soul: {context.Soul}/50", "Gluttony on cooldown", "No Enhanced buffs" },
                new[] { "Wait for Gluttony", "Wait for Enhanced buff" },
                "Blood Stalk is your fallback Soul spender. Gluttony and Unveiled variants are stronger.",
                "rpr_blood_stalk");
            context.TrainingService?.RecordConceptApplication("rpr_blood_stalk", true, "Basic Soul spender");

            return true;
        }

        return false;
    }

    #endregion

    #region Enshroud GCDs

    private bool TryPerfectio(IThanatosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.Perfectio.MinLevel)
            return false;

        // Requires Perfectio Parata proc (from Communio)
        if (!context.HasPerfectioParata)
            return false;

        if (!context.ActionService.IsActionReady(RPRActions.Perfectio.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(RPRActions.Perfectio, target.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.Perfectio.Name;
            context.Debug.DamageState = "Perfectio";

            // Training: Record Perfectio decision
            MeleeDpsTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                RPRActions.Perfectio.ActionId,
                RPRActions.Perfectio.Name,
                target.Name?.TextValue ?? "Target",
                "Using Perfectio (Communio proc)",
                "Perfectio is RPR's highest potency GCD, granted by Perfectio Parata after using Communio. " +
                "This is the true finisher of your Enshroud burst phase.",
                new[] { "Perfectio Parata proc active", "Highest priority GCD" },
                new[] { "No reason to hold" },
                "Always use Perfectio immediately when available. It's your strongest single GCD.",
                "rpr_perfectio");
            context.TrainingService?.RecordConceptApplication("rpr_perfectio", true, "Enshroud finisher proc");

            return true;
        }

        return false;
    }

    private bool TryCommunio(IThanatosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.Communio.MinLevel)
            return false;

        // Only use at 1 Lemure Shroud remaining (or if timer is low)
        if (context.LemureShroud > 1 && context.EnshroudTimer > 5f)
            return false;

        if (!context.ActionService.IsActionReady(RPRActions.Communio.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(RPRActions.Communio, target.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.Communio.Name;
            context.Debug.DamageState = "Communio (Enshroud finisher)";

            // Training: Record Communio decision
            MeleeDpsTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                RPRActions.Communio.ActionId,
                RPRActions.Communio.Name,
                target.Name?.TextValue ?? "Target",
                "Using Communio (Enshroud finisher)",
                "Communio ends your Enshroud phase with high damage and grants Perfectio Parata proc. " +
                "Use at 1 Lemure Shroud remaining after spending all Void Shroud on Lemure's Slice.",
                new[] { $"Lemure Shroud: {context.LemureShroud}", $"Timer: {context.EnshroudTimer:F1}s", "Ending Enshroud" },
                new[] { "Use more Reaping GCDs first", "Build more Void Shroud" },
                "Communio is the Enshroud finisher. Don't use it early - spend all Lemure and Void Shroud first.",
                "rpr_communio");
            context.TrainingService?.RecordConceptApplication("rpr_communio", true, "Enshroud finisher");

            return true;
        }

        return false;
    }

    private bool TryEnshroudGcd(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need Lemure Shroud to use Enshroud GCDs
        if (context.LemureShroud <= 0)
            return false;

        var useAoe = enemyCount >= AoeThreshold;
        ActionDefinition action;

        if (useAoe)
        {
            action = RPRActions.GrimReaping;
        }
        else
        {
            // Use enhanced version if available
            if (context.HasEnhancedVoidReaping)
                action = RPRActions.VoidReaping;
            else if (context.HasEnhancedCrossReaping)
                action = RPRActions.CrossReaping;
            else
                action = RPRActions.VoidReaping; // Default to Void Reaping
        }

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (L:{context.LemureShroud})";

            // Training: Record Enshroud GCD decision
            var isEnhanced = (action == RPRActions.VoidReaping && context.HasEnhancedVoidReaping) ||
                            (action == RPRActions.CrossReaping && context.HasEnhancedCrossReaping);
            MeleeDpsTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                action.ActionId,
                action.Name,
                target.Name?.TextValue ?? "Target",
                $"Using {action.Name} during Enshroud{(isEnhanced ? " (Enhanced)" : "")}",
                "Void Reaping and Cross Reaping are your Enshroud GCDs. Each consumes 1 Lemure Shroud and " +
                "grants 1 Void Shroud. Alternate between them following Enhanced buffs for bonus damage.",
                new[] { $"Lemure Shroud: {context.LemureShroud}", $"Void Shroud: {context.VoidShroud}", isEnhanced ? "Enhanced buff active" : "Default choice" },
                new[] { "Use Communio if last Lemure" },
                "Follow Enhanced buffs for 10% bonus damage. Weave Lemure's Slice at 2 Void Shroud.",
                "rpr_reaping");
            context.TrainingService?.RecordConceptApplication("rpr_reaping", true, "Enshroud GCD rotation");

            return true;
        }

        return false;
    }

    #endregion

    #region Soul Reaver GCDs

    private bool TrySoulReaverGcd(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need Soul Reaver to use these
        if (!context.HasSoulReaver)
            return false;

        if (level < RPRActions.Gibbet.MinLevel)
            return false;

        var useAoe = enemyCount >= AoeThreshold;
        ActionDefinition action;

        if (useAoe)
        {
            action = RPRActions.Guillotine;
        }
        else
        {
            // Follow the enhanced buff for optimal damage
            if (context.HasEnhancedGibbet)
                action = RPRActions.Gibbet;
            else if (context.HasEnhancedGallows)
                action = RPRActions.Gallows;
            else
            {
                // No enhanced buff - choose based on positional
                // Gibbet = flank, Gallows = rear
                if (context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity)
                    action = RPRActions.Gibbet;
                else if (context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity)
                    action = RPRActions.Gallows;
                else
                    action = RPRActions.Gibbet; // Default
            }
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        string positional = "";
        if (!useAoe)
        {
            positional = action == RPRActions.Gibbet ? " (flank)" : " (rear)";
        }

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name}{positional} [SR:{context.SoulReaverStacks}]";

            // Training: Record Soul Reaver GCD decision
            if (useAoe)
            {
                // Guillotine has no positional
                MeleeDpsTrainingHelper.RecordAoeDecision(
                    context.TrainingService,
                    action.ActionId,
                    action.Name,
                    context.Debug.NearbyEnemies,
                    $"Using {action.Name} (AoE Soul Reaver)",
                    "Guillotine is the AoE Soul Reaver spender. Use instead of Gibbet/Gallows when 3+ enemies.",
                    new[] { $"Soul Reaver stacks: {context.SoulReaverStacks}", $"Enemies: {context.Debug.NearbyEnemies}" },
                    new[] { "Use Gibbet/Gallows for ST" },
                    "Guillotine has no positional. Use for AoE, then continue with AoE combo.",
                    "rpr_guillotine");
                context.TrainingService?.RecordConceptApplication("rpr_guillotine", true, "AoE Soul Reaver");
            }
            else
            {
                // Gibbet/Gallows have positionals
                var isGibbet = action == RPRActions.Gibbet;
                var correctPosition = isGibbet ? "flank" : "rear";
                var hitPositional = isGibbet ? context.IsAtFlank : context.IsAtRear;
                hitPositional = hitPositional || context.HasTrueNorth || context.TargetHasPositionalImmunity;

                MeleeDpsTrainingHelper.RecordPositionalDecision(
                    context.TrainingService,
                    action.ActionId,
                    action.Name,
                    target.Name?.TextValue ?? "Target",
                    hitPositional,
                    correctPosition,
                    $"Using {action.Name} (Soul Reaver finisher)",
                    $"{action.Name} is a Soul Reaver finisher with a {correctPosition} positional. " +
                    "Grants 10 Shroud gauge and Enhanced buff for the opposite finisher.",
                    new[] { $"Soul Reaver stacks: {context.SoulReaverStacks}", context.HasEnhancedGibbet ? "Enhanced Gibbet" : context.HasEnhancedGallows ? "Enhanced Gallows" : "No enhanced buff", hitPositional ? "Positional hit" : "Positional missed" },
                    new[] { "Use other finisher if Enhanced" },
                    "Follow Enhanced buffs when available. Each finisher grants the opposite Enhanced buff.",
                    isGibbet ? "rpr_gibbet" : "rpr_gallows");
                context.TrainingService?.RecordConceptApplication(isGibbet ? "rpr_gibbet" : "rpr_gallows", hitPositional, "Soul Reaver finisher");
            }

            return true;
        }

        return false;
    }

    #endregion

    #region Plentiful Harvest

    private bool TryPlentifulHarvest(IThanatosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.PlentifulHarvest.MinLevel)
            return false;

        // Requires Immortal Sacrifice stacks
        if (context.ImmortalSacrificeStacks <= 0)
            return false;

        // Don't use during Soul Reaver
        if (context.HasSoulReaver)
            return false;

        // Don't use during Enshroud
        if (context.IsEnshrouded)
            return false;

        if (!context.ActionService.IsActionReady(RPRActions.PlentifulHarvest.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(RPRActions.PlentifulHarvest, target.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.PlentifulHarvest.Name;
            context.Debug.DamageState = $"Plentiful Harvest ({context.ImmortalSacrificeStacks} stacks)";

            // Training: Record Plentiful Harvest decision
            MeleeDpsTrainingHelper.RecordResourceDecision(
                context.TrainingService,
                RPRActions.PlentifulHarvest.ActionId,
                RPRActions.PlentifulHarvest.Name,
                "Immortal Sacrifice",
                context.ImmortalSacrificeStacks,
                $"Using Plentiful Harvest ({context.ImmortalSacrificeStacks} stacks)",
                "Plentiful Harvest consumes Immortal Sacrifice stacks gained during Arcane Circle. " +
                "Damage scales with stacks (max 8). Grants 50 Shroud gauge.",
                new[] { $"Immortal Sacrifice: {context.ImmortalSacrificeStacks}", "Not in Soul Reaver", "Not in Enshroud" },
                new[] { "Wait for more stacks", "Use during burst window" },
                "Use after Arcane Circle ends to consume all stacks. Grants 50 Shroud toward next Enshroud.",
                "rpr_plentiful_harvest");
            context.TrainingService?.RecordConceptApplication("rpr_plentiful_harvest", true, "Immortal Sacrifice consumer");

            return true;
        }

        return false;
    }

    #endregion

    #region Harvest Moon

    private bool TryHarvestMoon(IThanatosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.HarvestMoon.MinLevel)
            return false;

        // Requires Soulsow buff
        if (!context.HasSoulsow)
            return false;

        // Check distance - use Harvest Moon as ranged filler
        var dx = player.Position.X - target.Position.X;
        var dz = player.Position.Z - target.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dz * dz);

        // Only use at range or during movement
        if (distance <= FFXIVConstants.MeleeTargetingRange && !context.IsMoving)
            return false;

        if (!context.ActionService.IsActionReady(RPRActions.HarvestMoon.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(RPRActions.HarvestMoon, target.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.HarvestMoon.Name;
            context.Debug.DamageState = "Harvest Moon (ranged)";

            // Training: Record Harvest Moon decision
            MeleeDpsTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                RPRActions.HarvestMoon.ActionId,
                RPRActions.HarvestMoon.Name,
                target.Name?.TextValue ?? "Target",
                "Using Harvest Moon (ranged filler)",
                "Harvest Moon is a ranged GCD available when Soulsow buff is active. " +
                "Use during forced disengages or movement phases to maintain GCD uptime.",
                new[] { "Soulsow buff active", context.IsMoving ? "Moving" : "Out of melee range", "Ranged GCD option" },
                new[] { "Use melee GCDs when in range" },
                "Use Soulsow before pulls or during downtime. Harvest Moon is your ranged backup.",
                "rpr_harvest_moon");
            context.TrainingService?.RecordConceptApplication("rpr_harvest_moon", true, "Ranged GCD option");

            return true;
        }

        return false;
    }

    #endregion

    #region Death's Design

    private bool TryDeathsDesign(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Check if we need to apply/refresh Death's Design
        // Refresh at < 5s remaining to avoid clipping
        if (context.HasDeathsDesign && context.DeathsDesignRemaining > 5f)
            return false;

        var useAoe = enemyCount >= AoeThreshold && level >= RPRActions.WhorlOfDeath.MinLevel;
        var action = useAoe ? RPRActions.WhorlOfDeath : RPRActions.ShadowOfDeath;

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = context.HasDeathsDesign
                ? $"{action.Name} (refresh {context.DeathsDesignRemaining:F1}s)"
                : $"{action.Name} (apply)";

            // Training: Record Death's Design decision
            var isRefresh = context.HasDeathsDesign;
            MeleeDpsTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                action.ActionId,
                action.Name,
                target.Name?.TextValue ?? "Target",
                isRefresh ? $"Refreshing Death's Design ({context.DeathsDesignRemaining:F1}s remaining)" : "Applying Death's Design",
                "Death's Design is a 10% damage buff debuff that must be maintained on the target. " +
                "Shadow of Death (ST) or Whorl of Death (AoE) applies/refreshes it for 30s and grants 10 Soul.",
                new[] { isRefresh ? $"Remaining: {context.DeathsDesignRemaining:F1}s" : "Not applied", "Grants 10 Soul", "+10% damage debuff" },
                new[] { "Refresh above 5s wastes duration" },
                "Maintain Death's Design at all times. Refresh below 5s remaining to avoid clipping.",
                "rpr_deaths_design");
            context.TrainingService?.RecordConceptApplication("rpr_deaths_design", true, "Damage debuff maintenance");

            return true;
        }

        return false;
    }

    #endregion

    #region Soul Builder

    private bool TrySoulBuilder(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Soul Slice / Soul Scythe grants 50 Soul
        // Use when Soul < 50 to enable spenders
        if (context.Soul >= 50)
            return false;

        var useAoe = enemyCount >= AoeThreshold && level >= RPRActions.SoulScythe.MinLevel;
        var action = useAoe ? RPRActions.SoulScythe : RPRActions.SoulSlice;

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (+50 Soul)";

            // Training: Record Soul Builder decision
            MeleeDpsTrainingHelper.RecordResourceDecision(
                context.TrainingService,
                action.ActionId,
                action.Name,
                "Soul",
                context.Soul,
                $"Using {action.Name} to build Soul gauge",
                $"{action.Name} grants 50 Soul gauge on a charge system. Use to reach 50 Soul for spenders. " +
                "Soul Slice (ST) and Soul Scythe (AoE) share charges.",
                new[] { $"Soul: {context.Soul}/50", "Need 50 for spenders", "Charge available" },
                new[] { "Already have 50+ Soul" },
                "Use Soul Slice/Scythe when below 50 Soul to enable Gluttony/Blood Stalk.",
                "rpr_soul_slice");
            context.TrainingService?.RecordConceptApplication("rpr_soul_slice", true, "Soul gauge builder");

            return true;
        }

        return false;
    }

    #endregion

    #region Basic Combo

    private bool TryBasicCombo(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        ActionDefinition action;

        if (useAoe && level >= RPRActions.SpinningScythe.MinLevel)
        {
            // AoE combo: Spinning Scythe -> Nightmare Scythe
            if (context.ComboStep == 1 && context.LastComboAction == RPRActions.SpinningScythe.ActionId &&
                level >= RPRActions.NightmareScythe.MinLevel)
            {
                action = RPRActions.NightmareScythe;
            }
            else
            {
                action = RPRActions.SpinningScythe;
            }
        }
        else
        {
            // Single target combo: Slice -> Waxing Slice -> Infernal Slice
            if (context.ComboStep == 2 && context.LastComboAction == RPRActions.WaxingSlice.ActionId &&
                level >= RPRActions.InfernalSlice.MinLevel)
            {
                action = RPRActions.InfernalSlice;
            }
            else if (context.ComboStep == 1 && context.LastComboAction == RPRActions.Slice.ActionId &&
                     level >= RPRActions.WaxingSlice.MinLevel)
            {
                action = RPRActions.WaxingSlice;
            }
            else
            {
                action = RPRActions.Slice;
            }
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (combo {context.ComboStep + 1})";

            // Training: Record Basic Combo decision
            var comboType = useAoe ? "AoE" : "Single Target";
            var comboSequence = useAoe
                ? "Spinning Scythe → Nightmare Scythe"
                : "Slice → Waxing Slice → Infernal Slice";
            MeleeDpsTrainingHelper.RecordDamageDecision(
                context.TrainingService,
                action.ActionId,
                action.Name,
                target.Name?.TextValue ?? "Target",
                $"Using {action.Name} ({comboType} combo step {context.ComboStep + 1})",
                $"RPR's basic {comboType.ToLower()} combo: {comboSequence}. " +
                "Combo finishers grant 10 Soul gauge. Use when Soul Slice charges are depleted.",
                new[] { $"Combo step: {context.ComboStep + 1}", $"Soul: {context.Soul}", comboType },
                new[] { "Use Soul Slice if available" },
                "Basic combo is filler. Prioritize Soul Slice for faster Soul generation.",
                useAoe ? "rpr_aoe_combo" : "rpr_st_combo");
            context.TrainingService?.RecordConceptApplication(useAoe ? "rpr_aoe_combo" : "rpr_st_combo", true, "Basic combo filler");

            return true;
        }

        return false;
    }

    #endregion
}
