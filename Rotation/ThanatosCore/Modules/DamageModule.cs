using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThanatosCore.Abilities;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.ThanatosCore.Modules;

/// <summary>
/// Handles the Reaper damage rotation (scheduler-driven).
/// Manages Enshroud sequences, Soul Reaver, combo actions, resource building.
/// </summary>
public sealed class DamageModule : IThanatosModule
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

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public bool TryExecute(IThanatosContext context, bool isMoving) => false;

    public void UpdateDebugState(IThanatosContext context) { }

    public void CollectCandidates(IThanatosContext context, RotationScheduler scheduler, bool isMoving)
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
        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            RPRActions.Slice.ActionId,
            player);
        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return;
        }

        var aoeEnabled = context.Configuration.Reaper.EnableAoERotation;
        var aoeThreshold = context.Configuration.Reaper.AoEMinTargets;
        var rawEnemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = rawEnemyCount;
        var enemyCount = aoeEnabled ? rawEnemyCount : 0;

        // oGCDs (Lemure during Enshroud, Sacrificium proc, Soul spenders outside)
        if (context.IsEnshrouded)
        {
            TryPushLemuresSlice(context, scheduler, target, enemyCount);
            TryPushSacrificium(context, scheduler, target);
        }
        if (!context.IsEnshrouded && !context.HasSoulReaver)
            TryPushSoulSpender(context, scheduler, target, enemyCount);

        // GCDs (priority order matters)
        if (context.IsEnshrouded)
        {
            TryPushPerfectio(context, scheduler, target);
            TryPushCommunio(context, scheduler, target);
            TryPushEnshroudGcd(context, scheduler, target, enemyCount);
        }

        // Outside Enshroud (or fallback if IsEnshrouded paths fail)
        TryPushPerfectio(context, scheduler, target); // Perfectio Parata can carry outside
        TryPushPlentifulHarvest(context, scheduler, target);
        TryPushSoulReaverGcd(context, scheduler, target, enemyCount);
        TryPushHarvestMoon(context, scheduler, target);
        TryPushDeathsDesign(context, scheduler, target, enemyCount);
        TryPushSoulBuilder(context, scheduler, target, enemyCount);
        TryPushBasicCombo(context, scheduler, target, enemyCount);
    }

    #region oGCDs

    private void TryPushLemuresSlice(IThanatosContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Reaper.EnableLemureAbilities) return;
        var player = context.Player;
        if (player.Level < RPRActions.LemuresSlice.MinLevel) return;
        if (context.VoidShroud < 2) return;

        var aoeThreshold = context.Configuration.Reaper.AoEMinTargets;
        var useAoe = enemyCount >= aoeThreshold;
        var action = useAoe ? RPRActions.LemuresScythe : RPRActions.LemuresSlice;
        var ability = useAoe ? ThanatosAbilities.LemuresScythe : ThanatosAbilities.LemuresSlice;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        scheduler.PushOgcd(ability, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (Void: {context.VoidShroud})";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Using {action.Name} during Enshroud",
                        $"{action.Name} consumes 2 Void Shroud stacks for bonus damage during Enshroud. " +
                        "Build Void Shroud by using Void/Cross Reaping GCDs, then spend with Lemure's Slice.")
                    .Factors(new[] { $"Void Shroud: {context.VoidShroud}/2", "Enshroud active", "oGCD window" })
                    .Alternatives(new[] { "Wait for more Void Shroud" })
                    .Tip("Weave Lemure's Slice between Reaping GCDs. Build 2 Void Shroud, then spend.")
                    .Concept("rpr_lemure_slice")
                    .Record();
                context.TrainingService?.RecordConceptApplication("rpr_lemure_slice", true, "Enshroud oGCD");
            });
    }

    private void TryPushSacrificium(IThanatosContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Reaper.EnableEnshroud) return;
        var player = context.Player;
        if (player.Level < RPRActions.Sacrificium.MinLevel) return;
        if (!context.HasOblatio) return;
        if (!context.ActionService.IsActionReady(RPRActions.Sacrificium.ActionId)) return;

        scheduler.PushOgcd(ThanatosAbilities.Sacrificium, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RPRActions.Sacrificium.Name;
                context.Debug.DamageState = "Sacrificium";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(RPRActions.Sacrificium.ActionId, RPRActions.Sacrificium.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Using Sacrificium (Oblatio proc)",
                        "Sacrificium is a high-potency oGCD available during Enshroud when Oblatio proc is active. " +
                        "Oblatio is granted at the start of Enshroud. Use before Communio finisher.")
                    .Factors(new[] { "Oblatio proc active", "Enshroud active", "oGCD window" })
                    .Alternatives(new[] { "No reason to hold" })
                    .Tip("Use Sacrificium immediately when you have Oblatio proc during Enshroud.")
                    .Concept("rpr_sacrificium")
                    .Record();
                context.TrainingService?.RecordConceptApplication("rpr_sacrificium", true, "Enshroud proc");
            });
    }

    private void TryPushSoulSpender(IThanatosContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        if (context.Soul < 50) return;

        // Gluttony preferred (60s CD, 2 SR stacks)
        if (level >= RPRActions.Gluttony.MinLevel && context.Configuration.Reaper.EnableGluttony
            && context.ActionService.IsActionReady(RPRActions.Gluttony.ActionId)
            && !(context.Configuration.Reaper.EnableBurstPooling && ShouldHoldForBurst(8f))
            && context.Shroud < 50 && !context.ActionService.IsActionReady(RPRActions.Enshroud.ActionId))
        {
            scheduler.PushOgcd(ThanatosAbilities.Gluttony, target.GameObjectId, priority: 2,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = RPRActions.Gluttony.Name;
                    context.Debug.DamageState = "Gluttony (2 Soul Reaver)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RPRActions.Gluttony.ActionId, RPRActions.Gluttony.Name)
                        .AsMeleeResource("Soul", context.Soul)
                        .Reason("Using Gluttony for 2 Soul Reaver stacks",
                            "Gluttony is your premium Soul spender, granting 2 Soul Reaver stacks on a 60s cooldown. " +
                            "Soul Reaver enables Gibbet/Gallows finishers for high damage and Shroud generation.")
                        .Factors(new[] { $"Soul: {context.Soul}/50", "Gluttony ready", "Not entering Enshroud soon" })
                        .Alternatives(new[] { "Wait for Enshroud alignment", "Save for burst" })
                        .Tip("Prioritize Gluttony over Blood Stalk. Use before Enshroud to maximize Soul Reaver value.")
                        .Concept("rpr_gluttony")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("rpr_gluttony", true, "Premium Soul spender");
                });
            return;
        }

        // Unveiled variants
        if (level >= RPRActions.UnveiledGibbet.MinLevel)
        {
            if (context.HasEnhancedGibbet
                && context.ActionService.IsActionReady(RPRActions.UnveiledGibbet.ActionId))
            {
                scheduler.PushOgcd(ThanatosAbilities.UnveiledGibbet, target.GameObjectId, priority: 3,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = RPRActions.UnveiledGibbet.Name;
                        context.Debug.DamageState = "Unveiled Gibbet";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(RPRActions.UnveiledGibbet.ActionId, RPRActions.UnveiledGibbet.Name)
                            .AsMeleeResource("Soul", context.Soul)
                            .Reason("Using Unveiled Gibbet (Enhanced Gibbet buff)",
                                "Unveiled Gibbet/Gallows are enhanced versions of Blood Stalk that appear when you have " +
                                "the matching Enhanced buff. They deal more damage and grant 1 Soul Reaver stack.")
                            .Factors(new[] { $"Soul: {context.Soul}/50", "Enhanced Gibbet active", "oGCD window" })
                            .Alternatives(new[] { "Wait for Gluttony" })
                            .Tip("Use Unveiled variants when Enhanced buffs are active for better damage than Blood Stalk.")
                            .Concept("rpr_unveiled")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("rpr_unveiled", true, "Enhanced Soul spender");
                    });
                return;
            }
            if (context.HasEnhancedGallows
                && context.ActionService.IsActionReady(RPRActions.UnveiledGallows.ActionId))
            {
                scheduler.PushOgcd(ThanatosAbilities.UnveiledGallows, target.GameObjectId, priority: 3,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = RPRActions.UnveiledGallows.Name;
                        context.Debug.DamageState = "Unveiled Gallows";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(RPRActions.UnveiledGallows.ActionId, RPRActions.UnveiledGallows.Name)
                            .AsMeleeResource("Soul", context.Soul)
                            .Reason("Using Unveiled Gallows (Enhanced Gallows buff)",
                                "Unveiled Gallows appears when Enhanced Gallows is active. Use this instead of Blood Stalk " +
                                "for bonus damage while still gaining 1 Soul Reaver stack.")
                            .Factors(new[] { $"Soul: {context.Soul}/50", "Enhanced Gallows active", "oGCD window" })
                            .Alternatives(new[] { "Wait for Gluttony" })
                            .Tip("Follow the Enhanced buff - it tells you which finisher you used last for tracking.")
                            .Concept("rpr_unveiled")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("rpr_unveiled", true, "Enhanced Soul spender");
                    });
                return;
            }
        }

        // Basic Blood Stalk / Grim Swathe
        var aoeThreshold = context.Configuration.Reaper.AoEMinTargets;
        var useAoeBs = enemyCount >= aoeThreshold && level >= RPRActions.GrimSwathe.MinLevel;
        var bsAction = useAoeBs ? RPRActions.GrimSwathe : RPRActions.BloodStalk;
        var bsAbility = useAoeBs ? ThanatosAbilities.GrimSwathe : ThanatosAbilities.BloodStalk;
        if (level < bsAction.MinLevel) return;
        if (!context.ActionService.IsActionReady(bsAction.ActionId)) return;

        scheduler.PushOgcd(bsAbility, target.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = bsAction.Name;
                context.Debug.DamageState = $"{bsAction.Name} (1 Soul Reaver)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(bsAction.ActionId, bsAction.Name)
                    .AsMeleeResource("Soul", context.Soul)
                    .Reason($"Using {bsAction.Name} for 1 Soul Reaver stack",
                        $"{bsAction.Name} spends 50 Soul to grant 1 Soul Reaver stack. Use when Gluttony is on cooldown " +
                        "and no Enhanced buffs are available. Grim Swathe is the AoE version.")
                    .Factors(new[] { $"Soul: {context.Soul}/50", "Gluttony on cooldown", "No Enhanced buffs" })
                    .Alternatives(new[] { "Wait for Gluttony", "Wait for Enhanced buff" })
                    .Tip("Blood Stalk is your fallback Soul spender. Gluttony and Unveiled variants are stronger.")
                    .Concept("rpr_blood_stalk")
                    .Record();
                context.TrainingService?.RecordConceptApplication("rpr_blood_stalk", true, "Basic Soul spender");
            });
    }

    #endregion

    #region GCDs

    private void TryPushPerfectio(IThanatosContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Reaper.EnablePerfectio) return;
        var player = context.Player;
        if (player.Level < RPRActions.Perfectio.MinLevel) return;
        if (!context.HasPerfectioParata) return;
        if (!context.ActionService.IsActionReady(RPRActions.Perfectio.ActionId)) return;

        scheduler.PushGcd(ThanatosAbilities.Perfectio, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RPRActions.Perfectio.Name;
                context.Debug.DamageState = "Perfectio";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(RPRActions.Perfectio.ActionId, RPRActions.Perfectio.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Using Perfectio (Communio proc)",
                        "Perfectio is RPR's highest potency GCD, granted by Perfectio Parata after using Communio. " +
                        "This is the true finisher of your Enshroud burst phase.")
                    .Factors(new[] { "Perfectio Parata proc active", "Highest priority GCD" })
                    .Alternatives(new[] { "No reason to hold" })
                    .Tip("Always use Perfectio immediately when available. It's your strongest single GCD.")
                    .Concept("rpr_perfectio")
                    .Record();
                context.TrainingService?.RecordConceptApplication("rpr_perfectio", true, "Enshroud finisher proc");
            });
    }

    private void TryPushCommunio(IThanatosContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Reaper.EnableCommunio) return;
        var player = context.Player;
        if (player.Level < RPRActions.Communio.MinLevel) return;
        if (context.LemureShroud > 1 && context.EnshroudTimer > 5f) return;
        if (!context.ActionService.IsActionReady(RPRActions.Communio.ActionId)) return;

        scheduler.PushGcd(ThanatosAbilities.Communio, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RPRActions.Communio.Name;
                context.Debug.DamageState = "Communio (Enshroud finisher)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(RPRActions.Communio.ActionId, RPRActions.Communio.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Using Communio (Enshroud finisher)",
                        "Communio ends your Enshroud phase with high damage and grants Perfectio Parata proc. " +
                        "Use at 1 Lemure Shroud remaining after spending all Void Shroud on Lemure's Slice.")
                    .Factors(new[] { $"Lemure Shroud: {context.LemureShroud}", $"Timer: {context.EnshroudTimer:F1}s", "Ending Enshroud" })
                    .Alternatives(new[] { "Use more Reaping GCDs first", "Build more Void Shroud" })
                    .Tip("Communio is the Enshroud finisher. Don't use it early - spend all Lemure and Void Shroud first.")
                    .Concept("rpr_communio")
                    .Record();
                context.TrainingService?.RecordConceptApplication("rpr_communio", true, "Enshroud finisher");
            });
    }

    private void TryPushEnshroudGcd(IThanatosContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Reaper.EnableEnshroud) return;
        var player = context.Player;
        if (context.LemureShroud <= 0) return;

        var aoeThreshold = context.Configuration.Reaper.AoEMinTargets;
        var useAoe = enemyCount >= aoeThreshold;
        ActionDefinition action;
        AbilityBehavior ability;
        if (useAoe)
        {
            action = RPRActions.GrimReaping;
            ability = ThanatosAbilities.GrimReaping;
        }
        else if (context.HasEnhancedVoidReaping)
        {
            action = RPRActions.VoidReaping;
            ability = ThanatosAbilities.VoidReaping;
        }
        else if (context.HasEnhancedCrossReaping)
        {
            action = RPRActions.CrossReaping;
            ability = ThanatosAbilities.CrossReaping;
        }
        else
        {
            action = RPRActions.VoidReaping;
            ability = ThanatosAbilities.VoidReaping;
        }

        if (player.Level < action.MinLevel) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        scheduler.PushGcd(ability, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (L:{context.LemureShroud})";

                var isEnhanced = (action == RPRActions.VoidReaping && context.HasEnhancedVoidReaping) ||
                                (action == RPRActions.CrossReaping && context.HasEnhancedCrossReaping);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Using {action.Name} during Enshroud{(isEnhanced ? " (Enhanced)" : "")}",
                        "Void Reaping and Cross Reaping are your Enshroud GCDs. Each consumes 1 Lemure Shroud and " +
                        "grants 1 Void Shroud. Alternate between them following Enhanced buffs for bonus damage.")
                    .Factors(new[] { $"Lemure Shroud: {context.LemureShroud}", $"Void Shroud: {context.VoidShroud}", isEnhanced ? "Enhanced buff active" : "Default choice" })
                    .Alternatives(new[] { "Use Communio if last Lemure" })
                    .Tip("Follow Enhanced buffs for 10% bonus damage. Weave Lemure's Slice at 2 Void Shroud.")
                    .Concept("rpr_reaping")
                    .Record();
                context.TrainingService?.RecordConceptApplication("rpr_reaping", true, "Enshroud GCD rotation");
            });
    }

    private void TryPushSoulReaverGcd(IThanatosContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Reaper.EnableSoulReaver) return;
        var player = context.Player;
        if (!context.HasSoulReaver) return;
        if (player.Level < RPRActions.Gibbet.MinLevel) return;

        var aoeThreshold = context.Configuration.Reaper.AoEMinTargets;
        var useAoe = enemyCount >= aoeThreshold;
        ActionDefinition action;
        AbilityBehavior ability;

        if (useAoe)
        {
            action = RPRActions.Guillotine;
            ability = ThanatosAbilities.Guillotine;
        }
        else if (context.HasEnhancedGibbet)
        {
            action = RPRActions.Gibbet;
            ability = ThanatosAbilities.Gibbet;
        }
        else if (context.HasEnhancedGallows)
        {
            action = RPRActions.Gallows;
            ability = ThanatosAbilities.Gallows;
        }
        else if (context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity)
        {
            action = RPRActions.Gibbet;
            ability = ThanatosAbilities.Gibbet;
        }
        else if (context.IsAtRear)
        {
            action = RPRActions.Gallows;
            ability = ThanatosAbilities.Gallows;
        }
        else
        {
            action = RPRActions.Gibbet;
            ability = ThanatosAbilities.Gibbet;
        }

        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        var positional = useAoe ? "" : (action == RPRActions.Gibbet ? " (flank)" : " (rear)");

        scheduler.PushGcd(ability, target.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name}{positional} [SR:{context.SoulReaverStacks}]";

                if (useAoe)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsAoE(context.Debug.NearbyEnemies)
                        .Reason($"Using {action.Name} (AoE Soul Reaver)",
                            "Guillotine is the AoE Soul Reaver spender. Use instead of Gibbet/Gallows when 3+ enemies.")
                        .Factors(new[] { $"Soul Reaver stacks: {context.SoulReaverStacks}", $"Enemies: {context.Debug.NearbyEnemies}" })
                        .Alternatives(new[] { "Use Gibbet/Gallows for ST" })
                        .Tip("Guillotine has no positional. Use for AoE, then continue with AoE combo.")
                        .Concept("rpr_guillotine")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("rpr_guillotine", true, "AoE Soul Reaver");
                }
                else
                {
                    var isGibbet = action == RPRActions.Gibbet;
                    var correctPosition = isGibbet ? "flank" : "rear";
                    var hitPositional = isGibbet ? context.IsAtFlank : context.IsAtRear;
                    hitPositional = hitPositional || context.HasTrueNorth || context.TargetHasPositionalImmunity;

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsPositional(hitPositional, correctPosition)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Using {action.Name} (Soul Reaver finisher)",
                            $"{action.Name} is a Soul Reaver finisher with a {correctPosition} positional. " +
                            "Grants 10 Shroud gauge and Enhanced buff for the opposite finisher.")
                        .Factors(new[] { $"Soul Reaver stacks: {context.SoulReaverStacks}", context.HasEnhancedGibbet ? "Enhanced Gibbet" : context.HasEnhancedGallows ? "Enhanced Gallows" : "No enhanced buff", hitPositional ? "Positional hit" : "Positional missed" })
                        .Alternatives(new[] { "Use other finisher if Enhanced" })
                        .Tip("Follow Enhanced buffs when available. Each finisher grants the opposite Enhanced buff.")
                        .Concept(isGibbet ? "rpr_gibbet" : "rpr_gallows")
                        .Record();
                    context.TrainingService?.RecordConceptApplication(isGibbet ? "rpr_gibbet" : "rpr_gallows", hitPositional, "Soul Reaver finisher");
                }
            });
    }

    private void TryPushPlentifulHarvest(IThanatosContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Reaper.EnablePlentifulHarvest) return;
        var player = context.Player;
        if (player.Level < RPRActions.PlentifulHarvest.MinLevel) return;
        if (context.ImmortalSacrificeStacks <= 0) return;
        if (context.HasSoulReaver) return;
        if (context.IsEnshrouded) return;
        if (!context.ActionService.IsActionReady(RPRActions.PlentifulHarvest.ActionId)) return;

        scheduler.PushGcd(ThanatosAbilities.PlentifulHarvest, target.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RPRActions.PlentifulHarvest.Name;
                context.Debug.DamageState = $"Plentiful Harvest ({context.ImmortalSacrificeStacks} stacks)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(RPRActions.PlentifulHarvest.ActionId, RPRActions.PlentifulHarvest.Name)
                    .AsMeleeResource("Immortal Sacrifice", context.ImmortalSacrificeStacks)
                    .Reason($"Using Plentiful Harvest ({context.ImmortalSacrificeStacks} stacks)",
                        "Plentiful Harvest consumes Immortal Sacrifice stacks gained during Arcane Circle. " +
                        "Damage scales with stacks (max 8). Grants 50 Shroud gauge.")
                    .Factors(new[] { $"Immortal Sacrifice: {context.ImmortalSacrificeStacks}", "Not in Soul Reaver", "Not in Enshroud" })
                    .Alternatives(new[] { "Wait for more stacks", "Use during burst window" })
                    .Tip("Use after Arcane Circle ends to consume all stacks. Grants 50 Shroud toward next Enshroud.")
                    .Concept("rpr_plentiful_harvest")
                    .Record();
                context.TrainingService?.RecordConceptApplication("rpr_plentiful_harvest", true, "Immortal Sacrifice consumer");
            });
    }

    private void TryPushHarvestMoon(IThanatosContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Reaper.EnableHarvestMoon) return;
        var player = context.Player;
        if (player.Level < RPRActions.HarvestMoon.MinLevel) return;
        if (!context.HasSoulsow) return;
        if (DistanceHelper.IsActionInRange(RPRActions.Slice.ActionId, player, target) && !context.IsMoving) return;
        if (!context.ActionService.IsActionReady(RPRActions.HarvestMoon.ActionId)) return;

        scheduler.PushGcd(ThanatosAbilities.HarvestMoon, target.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RPRActions.HarvestMoon.Name;
                context.Debug.DamageState = "Harvest Moon (ranged)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(RPRActions.HarvestMoon.ActionId, RPRActions.HarvestMoon.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Using Harvest Moon (ranged filler)",
                        "Harvest Moon is a ranged GCD available when Soulsow buff is active. " +
                        "Use during forced disengages or movement phases to maintain GCD uptime.")
                    .Factors(new[] { "Soulsow buff active", context.IsMoving ? "Moving" : "Out of melee range", "Ranged GCD option" })
                    .Alternatives(new[] { "Use melee GCDs when in range" })
                    .Tip("Use Soulsow before pulls or during downtime. Harvest Moon is your ranged backup.")
                    .Concept("rpr_harvest_moon")
                    .Record();
                context.TrainingService?.RecordConceptApplication("rpr_harvest_moon", true, "Ranged GCD option");
            });
    }

    private void TryPushDeathsDesign(IThanatosContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        if (context.HasDeathsDesign && context.DeathsDesignRemaining > 5f) return;

        var aoeThreshold = context.Configuration.Reaper.AoEMinTargets;
        var useAoe = enemyCount >= aoeThreshold && level >= RPRActions.WhorlOfDeath.MinLevel;
        var action = useAoe ? RPRActions.WhorlOfDeath : RPRActions.ShadowOfDeath;
        var ability = useAoe ? ThanatosAbilities.WhorlOfDeath : ThanatosAbilities.ShadowOfDeath;
        if (level < action.MinLevel) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        scheduler.PushGcd(ability, target.GameObjectId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = context.HasDeathsDesign
                    ? $"{action.Name} (refresh {context.DeathsDesignRemaining:F1}s)"
                    : $"{action.Name} (apply)";

                var isRefresh = context.HasDeathsDesign;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason(isRefresh ? $"Refreshing Death's Design ({context.DeathsDesignRemaining:F1}s remaining)" : "Applying Death's Design",
                        "Death's Design is a 10% damage buff debuff that must be maintained on the target. " +
                        "Shadow of Death (ST) or Whorl of Death (AoE) applies/refreshes it for 30s and grants 10 Soul.")
                    .Factors(new[] { isRefresh ? $"Remaining: {context.DeathsDesignRemaining:F1}s" : "Not applied", "Grants 10 Soul", "+10% damage debuff" })
                    .Alternatives(new[] { "Refresh above 5s wastes duration" })
                    .Tip("Maintain Death's Design at all times. Refresh below 5s remaining to avoid clipping.")
                    .Concept("rpr_deaths_design")
                    .Record();
                context.TrainingService?.RecordConceptApplication("rpr_deaths_design", true, "Damage debuff maintenance");
            });
    }

    private void TryPushSoulBuilder(IThanatosContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        if (context.Soul >= 100) return;

        var aoeThreshold = context.Configuration.Reaper.AoEMinTargets;
        var useAoe = enemyCount >= aoeThreshold && level >= RPRActions.SoulScythe.MinLevel;
        var action = useAoe ? RPRActions.SoulScythe : RPRActions.SoulSlice;
        var ability = useAoe ? ThanatosAbilities.SoulScythe : ThanatosAbilities.SoulSlice;
        if (level < action.MinLevel) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        scheduler.PushGcd(ability, target.GameObjectId, priority: 8,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (+50 Soul)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeResource("Soul", context.Soul)
                    .Reason($"Using {action.Name} to build Soul gauge",
                        $"{action.Name} grants 50 Soul gauge on a charge system. Use to reach 50 Soul for spenders. " +
                        "Soul Slice (ST) and Soul Scythe (AoE) share charges.")
                    .Factors(new[] { $"Soul: {context.Soul}/50", "Need 50 for spenders", "Charge available" })
                    .Alternatives(new[] { "Already have 50+ Soul" })
                    .Tip("Use Soul Slice/Scythe when below 50 Soul to enable Gluttony/Blood Stalk.")
                    .Concept("rpr_soul_slice")
                    .Record();
                context.TrainingService?.RecordConceptApplication("rpr_soul_slice", true, "Soul gauge builder");
            });
    }

    private void TryPushBasicCombo(IThanatosContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var aoeThreshold = context.Configuration.Reaper.AoEMinTargets;
        var useAoe = enemyCount >= aoeThreshold;

        ActionDefinition action;
        AbilityBehavior ability;

        if (useAoe && level >= RPRActions.SpinningScythe.MinLevel)
        {
            if (context.ComboStep == 1 && context.LastComboAction == RPRActions.SpinningScythe.ActionId
                && level >= RPRActions.NightmareScythe.MinLevel)
            {
                action = RPRActions.NightmareScythe;
                ability = ThanatosAbilities.NightmareScythe;
            }
            else
            {
                action = RPRActions.SpinningScythe;
                ability = ThanatosAbilities.SpinningScythe;
            }
        }
        else
        {
            if (context.ComboStep == 2 && context.LastComboAction == RPRActions.WaxingSlice.ActionId
                && level >= RPRActions.InfernalSlice.MinLevel)
            {
                action = RPRActions.InfernalSlice;
                ability = ThanatosAbilities.InfernalSlice;
            }
            else if (context.ComboStep == 1 && context.LastComboAction == RPRActions.Slice.ActionId
                && level >= RPRActions.WaxingSlice.MinLevel)
            {
                action = RPRActions.WaxingSlice;
                ability = ThanatosAbilities.WaxingSlice;
            }
            else
            {
                action = RPRActions.Slice;
                ability = ThanatosAbilities.Slice;
            }
        }

        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        scheduler.PushGcd(ability, target.GameObjectId, priority: 9,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (combo {context.ComboStep + 1})";

                var comboType = useAoe ? "AoE" : "Single Target";
                var comboSequence = useAoe
                    ? "Spinning Scythe → Nightmare Scythe"
                    : "Slice → Waxing Slice → Infernal Slice";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Using {action.Name} ({comboType} combo step {context.ComboStep + 1})",
                        $"RPR's basic {comboType.ToLower()} combo: {comboSequence}. " +
                        "Combo finishers grant 10 Soul gauge. Use when Soul Slice charges are depleted.")
                    .Factors(new[] { $"Combo step: {context.ComboStep + 1}", $"Soul: {context.Soul}", comboType })
                    .Alternatives(new[] { "Use Soul Slice if available" })
                    .Tip("Basic combo is filler. Prioritize Soul Slice for faster Soul generation.")
                    .Concept(useAoe ? "rpr_aoe_combo" : "rpr_st_combo")
                    .Record();
                context.TrainingService?.RecordConceptApplication(useAoe ? "rpr_aoe_combo" : "rpr_st_combo", true, "Basic combo filler");
            });
    }

    #endregion
}
