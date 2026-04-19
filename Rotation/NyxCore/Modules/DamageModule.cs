using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight DPS rotation.
/// Manages Darkside maintenance, combo actions, and gauge spending.
/// </summary>
public sealed class DamageModule : INyxModule
{
    public int Priority => 30; // Lower priority - damage comes after survival
    public string Name => "Damage";

    public bool TryExecute(INyxContext context, bool isMoving)
    {
        if (!context.Configuration.Tank.EnableDamage)
        {
            context.Debug.DamageState = "Disabled";
            return false;
        }

        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return false;
        }

        // Gaze-safety: player has no target, PauseWhenNoTarget is on.
        if (context.TargetingService.IsDamageTargetingPaused())
        {
            context.Debug.DamageState = "Paused (no target)";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target — melee range first via game API, fall back to gap-closer range for engagement
        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            DRKActions.HardSlash.ActionId,
            player);

        var engageTarget = target ?? context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            20f,
            player);

        if (engageTarget == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Out of melee range: try Shadowstride (oGCD gap close) or Unmend (ranged GCD to open weave window)
        if (target == null && engageTarget != null)
        {
            var gapCloseBlocked = context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(engageTarget, player);
            if (gapCloseBlocked)
                context.Debug.DamageState = $"Shadowstride blocked: {context.TargetingService.GapCloserSafety.LastBlockReason}";
            if (!gapCloseBlocked && context.CanExecuteOgcd && TryShadowstride(context, engageTarget.GameObjectId))
                return true;
            if (context.CanExecuteGcd && TryUnmend(context, engageTarget.GameObjectId))
                return true;
        }

        // Only run full rotation when a melee-range target is available
        if (target == null)
        {
            context.Debug.DamageState = "Target out of melee range";
            return false;
        }

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);

        // oGCD Phase - Darkside maintenance and damage oGCDs
        if (context.CanExecuteOgcd)
        {
            // Priority 1: Edge/Flood of Shadow with Dark Arts (FREE)
            if (TryDarkArtsProc(context, enemyCount, target.GameObjectId))
                return true;

            // Priority 2: Shadowbringer (high potency oGCD)
            if (TryShadowbringer(context, target.GameObjectId))
                return true;

            // Priority 3: Salted Earth (ground DoT)
            if (TrySaltedEarth(context))
                return true;

            // Priority 4: Salt and Darkness (enhance Salted Earth)
            if (TrySaltAndDarkness(context))
                return true;

            // Priority 5: Edge/Flood of Shadow for Darkside maintenance
            if (TryDarksideMaintenance(context, enemyCount, target.GameObjectId))
                return true;

            // Priority 6: Carve and Spit
            if (TryCarveAndSpit(context, target.GameObjectId))
                return true;

            // Priority 7: Abyssal Drain (AoE situations)
            if (TryAbyssalDrain(context, enemyCount, target.GameObjectId))
                return true;

            // Priority 8: Shadowstride (gap closer / damage filler).
            // Still gate the in-melee filler use — Shadowstride moves the player, which
            // is dangerous during spread markers even when already in melee.
            if (!context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, player)
                && TryShadowstride(context, target.GameObjectId))
                return true;
        }

        // GCD Phase
        if (context.CanExecuteGcd)
        {
            // Priority 1: Delirium combo (Lv.96+)
            if (TryDeliriumCombo(context, enemyCount, target.GameObjectId))
                return true;

            // Priority 2: Disesteem (after Torcleaver)
            if (TryDisesteem(context, target.GameObjectId))
                return true;

            // Priority 3: Blood Gauge spenders
            if (TryBloodSpender(context, enemyCount, target.GameObjectId))
                return true;

            // Priority 4: Combo actions
            if (TryCombo(context, enemyCount, target.GameObjectId))
                return true;
        }

        return false;
    }

    public void UpdateDebugState(INyxContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Actions

    /// <summary>
    /// Use Dark Arts proc (free Edge/Flood from broken TBN).
    /// This is highest priority to not waste the proc.
    /// </summary>
    private bool TryDarkArtsProc(INyxContext context, int enemyCount, ulong targetId)
    {
        if (!context.HasDarkArts)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Dark Arts grants a free Edge/Flood of Shadow
        var action = context.Configuration.Tank.EnableAoEDamage && enemyCount >= context.Configuration.Tank.AoEMinTargets
            ? DRKActions.GetFloodAction(level) : DRKActions.GetEdgeAction(level);

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(action, targetId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = "Dark Arts proc!";

            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsTankBurst()
                .Reason(
                    "Dark Arts proc — free Edge/Flood of Shadow from broken The Blackest Night shield.",
                    "When TBN's shield breaks, you get Dark Arts which makes the next Edge/Flood of Shadow cost no MP. This is highest priority to avoid wasting the proc.")
                .Factors("Dark Arts active", "Edge/Flood free (no MP cost)", "Highest priority oGCD")
                .Alternatives("Delay (Dark Arts will expire)", "Use other oGCD (wastes free damage)")
                .Tip("Never let Dark Arts expire. Use Edge/Flood immediately when you see the proc. This effectively makes TBN damage-neutral when its shield breaks.")
                .Concept(DrkConcepts.DarkArts)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.DarkArts, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryShadowbringer(INyxContext context, ulong targetId)
    {
        if (!context.Configuration.Tank.EnableShadowbringer) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Shadowbringer.MinLevel)
            return false;

        // Shadowbringer has 2 charges, use when available
        if (!context.ActionService.IsActionReady(DRKActions.Shadowbringer.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.Shadowbringer, targetId))
        {
            context.Debug.PlannedAction = DRKActions.Shadowbringer.Name;
            context.Debug.DamageState = "Shadowbringer";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRKActions.Shadowbringer.ActionId, DRKActions.Shadowbringer.Name)
                .AsTankBurst()
                .Target(context.CurrentTarget?.Name.TextValue)
                .Reason(
                    "Shadowbringer — high potency oGCD with 2 charges, use on cooldown.",
                    "Shadowbringer has 2 charges and deals 600 potency each. Use on cooldown to maximize DPS. Hold one charge if a 2-minute window is less than 30 seconds away.")
                .Factors("Charge available", "High potency oGCD", "Darkside active")
                .Alternatives("Hold for burst window (loses a use)", "Other oGCDs (lower potency)")
                .Tip("Shadowbringer has 2 charges so you have flexibility. Use on cooldown during normal play, but consider holding for 2-minute burst windows.")
                .Concept(DrkConcepts.Shadowbringer)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.Shadowbringer, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TrySaltedEarth(INyxContext context)
    {
        if (!context.Configuration.Tank.EnableSaltedEarth) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.SaltedEarth.MinLevel)
            return false;

        // Salted Earth is a ground DoT, use on cooldown
        if (!context.ActionService.IsActionReady(DRKActions.SaltedEarth.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.SaltedEarth, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.SaltedEarth.Name;
            context.Debug.DamageState = "Salted Earth";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRKActions.SaltedEarth.ActionId, DRKActions.SaltedEarth.Name)
                .AsTankDamage()
                .Reason(
                    "Salted Earth — ground AoE DoT, place on cooldown for sustained damage.",
                    "Salted Earth creates a ground AoE that ticks for 50 potency per second for 15 seconds. Place it under the boss on cooldown for free sustained damage.")
                .Factors("Salted Earth ready", "Ground DoT for sustained damage", "Boss stationary")
                .Alternatives("Skip (loses significant DPS)", "Save for AoE (loses single-target uptime)")
                .Tip("Always place Salted Earth under the boss. Combine with Salt and Darkness to enhance it. The DoT ticks automatically while you execute your rotation.")
                .Concept(DrkConcepts.SaltedEarth)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.SaltedEarth, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TrySaltAndDarkness(INyxContext context)
    {
        if (!context.Configuration.Tank.EnableSaltedEarth) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.SaltAndDarkness.MinLevel)
            return false;

        // Salt and Darkness requires Salted Earth to be active
        // Since we can't easily track ground effects, use when off cooldown
        // The game will fail silently if Salted Earth isn't active
        if (!context.ActionService.IsActionReady(DRKActions.SaltAndDarkness.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.SaltAndDarkness, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.SaltAndDarkness.Name;
            context.Debug.DamageState = "Salt and Darkness";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRKActions.SaltAndDarkness.ActionId, DRKActions.SaltAndDarkness.Name)
                .AsTankDamage()
                .Reason(
                    "Salt and Darkness — enhances Salted Earth for bonus potency.",
                    "Salt and Darkness deals 500 potency and empowers the active Salted Earth to deal extra damage. Always use it after placing Salted Earth.")
                .Factors("Salt and Darkness ready", "Enhances Salted Earth uptime", "Bonus potency oGCD")
                .Alternatives("Skip (loses bonus Salted Earth damage)", "Other oGCDs (lower synergy)")
                .Tip("Use Salt and Darkness after Salted Earth every time. The cooldown lines up naturally with Salted Earth's cooldown.")
                .Concept(DrkConcepts.SaltedEarth)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.SaltedEarth, wasSuccessful: true);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Darkside maintenance logic.
    /// Edge/Flood of Shadow grants 30s Darkside.
    /// Critical to maintain 100% uptime for 10% damage buff.
    /// </summary>
    private bool TryDarksideMaintenance(INyxContext context, int enemyCount, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Need MP for Edge/Flood
        if (!context.HasEnoughMpForEdge)
        {
            context.Debug.DamageState = "Low MP, can't maintain Darkside";
            return false;
        }

        var action = context.Configuration.Tank.EnableAoEDamage && enemyCount >= context.Configuration.Tank.AoEMinTargets
            ? DRKActions.GetFloodAction(level) : DRKActions.GetEdgeAction(level);

        // Priority 1: Darkside about to expire (< 10s)
        if (context.HasDarkside && context.DarksideRemaining < 10f && context.DarksideRemaining > 0f)
        {
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(action, targetId))
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = $"Darkside refresh ({context.DarksideRemaining:F1}s)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsTankResource(context.CurrentMp)
                        .Target(context.CurrentTarget?.Name.TextValue)
                        .Reason(
                            $"Darkside refresh — {context.DarksideRemaining:F1}s remaining, use Edge/Flood to extend.",
                            "Darkside provides a 10% damage buff and must be kept active at all times. Each Edge/Flood of Shadow grants 30 seconds. Refresh before it expires.")
                        .Factors($"Darkside at {context.DarksideRemaining:F1}s", "About to expire", $"MP: {context.CurrentMp}")
                        .Alternatives("Let Darkside fall (10% damage loss)", "Delay until next oGCD window (risky)")
                        .Tip("Darkside is your most important buff. Refresh it when it has <10s remaining using Edge/Flood. Losing Darkside is a significant DPS loss.")
                        .Concept(DrkConcepts.DarksideMaintenance)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(DrkConcepts.DarksideMaintenance, wasSuccessful: true);

                    return true;
                }
            }
        }

        // Priority 2: No Darkside at all
        if (!context.HasDarkside)
        {
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(action, targetId))
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = "Darkside activate";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsTankResource(context.CurrentMp)
                        .Target(context.CurrentTarget?.Name.TextValue)
                        .Reason(
                            "Darkside inactive — activate immediately with Edge/Flood of Shadow.",
                            "Darkside is a 10% damage buff that must be maintained. Use Edge/Flood to activate it as soon as possible whenever it's not active.")
                        .Factors("No Darkside buff", "10% damage buff missing", $"MP: {context.CurrentMp}")
                        .Alternatives("Continue without Darkside (10% damage loss)", "Use other oGCDs first (wastes opportunity)")
                        .Tip("Never fight without Darkside. Activate it at the start of combat and maintain it throughout. Each Edge/Flood grants 30 seconds of Darkside.")
                        .Concept(DrkConcepts.Darkside)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(DrkConcepts.Darkside, wasSuccessful: true);

                    return true;
                }
            }
        }

        // Priority 3: MP dump to avoid overcap (>= 9400 MP)
        if (context.CurrentMp >= 9400)
        {
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(action, targetId))
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = "MP dump";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsTankResource(context.CurrentMp)
                        .Target(context.CurrentTarget?.Name.TextValue)
                        .Reason(
                            $"MP dump — at {context.CurrentMp} MP, spending to avoid overcap and extend Darkside.",
                            "Edge/Flood of Shadow costs 3000 MP and also extends Darkside. Spend MP when near the cap (10000) to avoid wasting mana from natural regeneration.")
                        .Factors($"MP at {context.CurrentMp} (near cap)", "Avoiding MP overcap", "Darkside extension bonus")
                        .Alternatives("Hold MP (will overcap and waste)", "Use TBN (different purpose)")
                        .Tip("DRK generates MP from Blood Weapon and combo actions. Spend it with Edge/Flood before reaching 10000 to never waste MP.")
                        .Concept(DrkConcepts.EdgeOfShadow)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(DrkConcepts.EdgeOfShadow, wasSuccessful: true);

                    return true;
                }
            }
        }

        return false;
    }

    private bool TryCarveAndSpit(INyxContext context, ulong targetId)
    {
        if (!context.Configuration.Tank.EnableCarveAndSpit) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.CarveAndSpit.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.CarveAndSpit.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.CarveAndSpit, targetId))
        {
            context.Debug.PlannedAction = DRKActions.CarveAndSpit.Name;
            context.Debug.DamageState = "Carve and Spit";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRKActions.CarveAndSpit.ActionId, DRKActions.CarveAndSpit.Name)
                .AsTankDamage()
                .Target(context.CurrentTarget?.Name.TextValue)
                .Reason(
                    "Carve and Spit — single-target oGCD that restores MP. Use on cooldown.",
                    "Carve and Spit deals 512 potency and restores 600 MP. Shares cooldown with Abyssal Drain. In single-target, always prefer Carve and Spit for the MP restoration.")
                .Factors("Carve and Spit ready", "Single target", "MP restoration on use")
                .Alternatives("Abyssal Drain (AoE only)", "Skip (loses MP and damage)")
                .Tip("Carve and Spit is your primary MP restoration tool alongside Blood Weapon. Use on cooldown in single-target fights to sustain Edge of Shadow usage.")
                .Concept(DrkConcepts.CarveAndSpit)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.CarveAndSpit, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryAbyssalDrain(INyxContext context, int enemyCount, ulong targetId)
    {
        if (!context.Configuration.Tank.EnableAbyssalDrain) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.AbyssalDrain.MinLevel)
            return false;

        // Abyssal Drain is better in AoE situations (shares cooldown with Carve and Spit)
        if (enemyCount < 2)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.AbyssalDrain.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.AbyssalDrain, targetId))
        {
            context.Debug.PlannedAction = DRKActions.AbyssalDrain.Name;
            context.Debug.DamageState = $"Abyssal Drain ({enemyCount} enemies)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRKActions.AbyssalDrain.ActionId, DRKActions.AbyssalDrain.Name)
                .AsAoE(enemyCount)
                .Target(context.CurrentTarget?.Name.TextValue)
                .Reason(
                    $"Abyssal Drain — AoE oGCD hitting {enemyCount} enemies, better than Carve and Spit in AoE.",
                    "Abyssal Drain deals 240 potency per enemy and restores 600 MP. In AoE situations (3+ enemies), it outperforms Carve and Spit. Shares their cooldown, so choose based on enemy count.")
                .Factors($"{enemyCount} enemies in range", "AoE oGCD preferred", "MP restoration on hit")
                .Alternatives("Carve and Spit (single-target only)", "Skip (loses AoE damage and MP)")
                .Tip("Switch to Abyssal Drain when fighting 3+ enemies. It hits all of them for significant AoE damage while restoring MP.")
                .Concept(DrkConcepts.CarveAndSpit)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.CarveAndSpit, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryShadowstride(INyxContext context, ulong targetId)
    {
        if (!context.Configuration.Tank.EnableShadowstride) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Shadowstride.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.Shadowstride.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.Shadowstride, targetId))
        {
            context.Debug.PlannedAction = DRKActions.Shadowstride.Name;
            context.Debug.DamageState = "Shadowstride";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRKActions.Shadowstride.ActionId, DRKActions.Shadowstride.Name)
                .AsTankDamage()
                .Target(context.CurrentTarget?.Name.TextValue)
                .Reason(
                    "Shadowstride — gap closer oGCD to engage or reposition to target.",
                    "Shadowstride teleports you to the target and deals damage. Use it to close gaps quickly or as a filler oGCD when in melee range. Generates no resources.")
                .Factors("Target available", "Shadowstride ready", "Melee engagement or repositioning")
                .Alternatives("Unmend (ranged GCD, lower damage)", "Walk to target (loses GCDs)")
                .Tip("Shadowstride is your primary mobility tool. Use it to engage quickly or reposition. It deals damage on use, making it a minor DPS bonus too.")
                .Concept(DrkConcepts.EdgeOfShadow)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.EdgeOfShadow, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryUnmend(INyxContext context, ulong targetId)
    {
        var level = context.Player.Level;

        if (level < DRKActions.Unmend.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.Unmend.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DRKActions.Unmend, targetId))
        {
            context.Debug.PlannedAction = DRKActions.Unmend.Name;
            context.Debug.DamageState = "Unmend (ranged)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRKActions.Unmend.ActionId, DRKActions.Unmend.Name)
                .AsTankDamage()
                .Target(context.CurrentTarget?.Name.TextValue)
                .Reason(
                    "Unmend — ranged pull GCD to engage when out of melee range.",
                    "Unmend is a ranged GCD that deals modest damage and pulls enemies. Use it to open combat from range or when you can't close the gap with Shadowstride.")
                .Factors("Out of melee range", "Shadowstride unavailable or on cooldown", "Need to apply damage/enmity")
                .Alternatives("Shadowstride (better gap closer, deals more damage)", "Wait to enter melee range")
                .Tip("Use Unmend only to pull from range. Once in melee, stick to your combo. Unmend has low potency compared to melee actions.")
                .Concept(DrkConcepts.Darkside)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.Darkside, wasSuccessful: true);

            return true;
        }

        return false;
    }

    #endregion

    #region GCD Actions

    /// <summary>
    /// Delirium combo at Lv.96+.
    /// ST: Scarlet Delirium -> Comeuppance -> Torcleaver -> Disesteem
    /// AoE: Impalement (standalone self-centered AoE replacing Quietus)
    /// </summary>
    private bool TryDeliriumCombo(INyxContext context, int enemyCount, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.ScarletDelirium.MinLevel)
            return false;

        // Only available during Delirium
        if (!context.HasDelirium)
            return false;

        // AoE branch: Impalement at Lv.96+ when threshold met
        if (context.Configuration.Tank.EnableAoEDamage &&
            enemyCount >= context.Configuration.Tank.AoEMinTargets &&
            level >= DRKActions.Impalement.MinLevel &&
            context.ActionService.IsActionReady(DRKActions.Impalement.ActionId))
        {
            if (context.ActionService.ExecuteGcd(DRKActions.Impalement, targetId))
            {
                context.Debug.PlannedAction = DRKActions.Impalement.Name;
                context.Debug.DamageState = $"Impalement ({enemyCount} enemies)";
                return true;
            }
        }

        // The game handles combo tracking for Delirium combo
        // Just try to use Scarlet Delirium - it will be replaced by combo actions
        if (context.ActionService.IsActionReady(DRKActions.ScarletDelirium.ActionId))
        {
            if (context.ActionService.ExecuteGcd(DRKActions.ScarletDelirium, targetId))
            {
                context.Debug.PlannedAction = "Delirium Combo";
                context.Debug.DamageState = "Delirium combo";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(DRKActions.ScarletDelirium.ActionId, "Delirium Combo")
                    .AsTankBurst()
                    .Target(context.CurrentTarget?.Name.TextValue)
                    .Reason(
                        "Delirium combo — Scarlet Delirium → Comeuppance → Torcleaver sequence for maximum burst damage.",
                        "At Lv.96+, Delirium enables a 3-step combo: Scarlet Delirium → Comeuppance → Torcleaver. Each step deals high potency. Complete all 3 steps before Delirium expires, then use Disesteem for the finisher.")
                    .Factors("Delirium active", "Lv.96+ Scarlet Delirium combo enabled", "Highest GCD potency available")
                    .Alternatives("Bloodspiller (weaker, pre-96 option)", "Regular combo (massive damage loss during Delirium)")
                    .Tip("During Delirium at Lv.96+, always execute Scarlet Delirium → Comeuppance → Torcleaver → Disesteem. Never use regular combo during Delirium.")
                    .Concept(DrkConcepts.Delirium)
                    .Record();

                context.TrainingService?.RecordConceptApplication(DrkConcepts.Delirium, wasSuccessful: true);

                return true;
            }
        }

        return false;
    }

    private bool TryDisesteem(INyxContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Disesteem.MinLevel)
            return false;

        // Disesteem requires Scornful Edge buff (from Torcleaver)
        if (!context.HasScornfulEdge)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.Disesteem.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DRKActions.Disesteem, targetId))
        {
            context.Debug.PlannedAction = DRKActions.Disesteem.Name;
            context.Debug.DamageState = "Disesteem";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRKActions.Disesteem.ActionId, DRKActions.Disesteem.Name)
                .AsTankBurst()
                .Target(context.CurrentTarget?.Name.TextValue)
                .Reason(
                    "Disesteem — Delirium combo finisher with Scornful Edge buff, deals massive AoE damage.",
                    "Disesteem is the finisher after Torcleaver in the Delirium combo. It requires the Scornful Edge buff and deals 1000 potency in a cone AoE. Always complete the combo: Scarlet Delirium → Comeuppance → Torcleaver → Disesteem.")
                .Factors("Scornful Edge buff active (from Torcleaver)", "Delirium combo finisher", "Highest potency GCD")
                .Alternatives("Skip (wastes Scornful Edge buff)", "Use other GCDs (massively lower potency)")
                .Tip("Disesteem is the crown jewel of the Delirium combo. Always execute all 4 steps: Scarlet Delirium, Comeuppance, Torcleaver, then Disesteem.")
                .Concept(DrkConcepts.Disesteem)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.Disesteem, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryBloodSpender(INyxContext context, int enemyCount, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Check if we should spend Blood Gauge
        var shouldSpendBlood = false;

        // During Delirium (pre-96), Bloodspiller is free
        if (context.HasDelirium && level >= DRKActions.Bloodspiller.MinLevel && level < DRKActions.ScarletDelirium.MinLevel)
        {
            shouldSpendBlood = true;
        }
        // Have gauge >= 50 (avoid overcap)
        else if (context.BloodGauge >= DRKActions.BloodspillerCost)
        {
            // Spend if near cap or during burst
            if (context.BloodGauge >= 80 || context.HasDelirium)
                shouldSpendBlood = true;

            // Also spend if we'd overcap from combo finisher
            if (context.ComboStep == 2 && context.BloodGauge >= 80)
                shouldSpendBlood = true;
        }

        if (!shouldSpendBlood)
            return false;

        // Choose between Bloodspiller (ST) and Quietus (AoE)
        if (context.Configuration.Tank.EnableAoEDamage &&
            enemyCount >= context.Configuration.Tank.AoEMinTargets && level >= DRKActions.Quietus.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRKActions.Quietus.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRKActions.Quietus, targetId))
                {
                    context.Debug.PlannedAction = DRKActions.Quietus.Name;
                    context.Debug.DamageState = $"Quietus ({context.BloodGauge} Blood)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DRKActions.Quietus.ActionId, DRKActions.Quietus.Name)
                        .AsTankResource(context.BloodGauge)
                        .Target(context.CurrentTarget?.Name.TextValue)
                        .Reason(
                            $"Quietus — AoE Blood Gauge spender at {context.BloodGauge} Blood with {enemyCount} enemies.",
                            "Quietus costs 50 Blood Gauge and hits all nearby enemies. In AoE situations (3+ enemies), Quietus outperforms Bloodspiller. Spend Blood when high to prevent overcapping from Stalwart Soul.")
                        .Factors($"Blood Gauge at {context.BloodGauge}", $"{enemyCount} enemies in range", "AoE Blood spender preferred")
                        .Alternatives("Bloodspiller (single-target only)", "Continue AoE combo (may overcap Blood)")
                        .Tip("In AoE, Quietus is your Blood spender. Stalwart Soul also generates Blood, so spend it before reaching 100 to avoid overcapping.")
                        .Concept(DrkConcepts.BloodGauge)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(DrkConcepts.BloodGauge, wasSuccessful: true);

                    return true;
                }
            }
        }
        else if (level >= DRKActions.Bloodspiller.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRKActions.Bloodspiller.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRKActions.Bloodspiller, targetId))
                {
                    context.Debug.PlannedAction = DRKActions.Bloodspiller.Name;
                    var isDuringDelirium = context.HasDelirium;
                    context.Debug.DamageState = isDuringDelirium ? "Free Bloodspiller!" : $"Bloodspiller ({context.BloodGauge} Blood)";

                    if (isDuringDelirium)
                    {
                        // Training: Record burst window spending
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(DRKActions.Bloodspiller.ActionId, DRKActions.Bloodspiller.Name)
                            .AsTankBurst()
                            .Reason(
                                "Free Bloodspiller during Delirium - guaranteed crit/direct hit with no Blood cost.",
                                "Delirium (pre-Lv.96) grants 3 free Bloodspillers. Each one is a guaranteed crit + direct hit. Spam Bloodspiller during Delirium for massive burst damage.")
                            .Factors("Delirium active", "Bloodspiller free", "Guaranteed crit + direct hit")
                            .Alternatives("Use combo (massive damage loss)", "Wait (Delirium will expire)")
                            .Tip("During Delirium, Bloodspiller is your highest priority GCD. Don't let stacks expire - use all 3 before Delirium ends.")
                            .Concept(DrkConcepts.Delirium)
                            .Record();

                        context.TrainingService?.RecordConceptApplication(DrkConcepts.Delirium, wasSuccessful: true);
                    }
                    else
                    {
                        // Training: Record gauge spending
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(DRKActions.Bloodspiller.ActionId, DRKActions.Bloodspiller.Name)
                            .AsTankResource(context.BloodGauge)
                            .Reason(
                                $"Bloodspiller to spend Blood Gauge at {context.BloodGauge}.",
                                "Bloodspiller costs 50 Blood Gauge and deals high potency damage. Avoid overcapping Blood - use Bloodspiller when at or near 100 gauge.")
                            .Factors($"Blood Gauge at {context.BloodGauge}", "Near overcap", "High potency spender")
                            .Alternatives("Continue combo (may overcap)", "Save for Delirium (loses damage if overcap)")
                            .Tip("Spend Blood when above 70-80 gauge to prevent overcapping from combo finishers. Each Souleater grants 20 Blood.")
                            .Concept(DrkConcepts.BloodGauge)
                            .Record();

                        context.TrainingService?.RecordConceptApplication(DrkConcepts.BloodGauge, wasSuccessful: true);
                    }

                    return true;
                }
            }
        }

        return false;
    }

    private bool TryCombo(INyxContext context, int enemyCount, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE combo (3+ enemies)
        if (context.Configuration.Tank.EnableAoEDamage &&
            enemyCount >= context.Configuration.Tank.AoEMinTargets && level >= DRKActions.Unleash.MinLevel)
        {
            return TryAoECombo(context, targetId);
        }

        // Single-target combo
        return TrySingleTargetCombo(context, targetId);
    }

    private bool TrySingleTargetCombo(INyxContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Combo step 2: Souleater (finisher)
        if (context.ComboStep == 2 && level >= DRKActions.Souleater.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRKActions.Souleater.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRKActions.Souleater, targetId))
                {
                    context.Debug.PlannedAction = DRKActions.Souleater.Name;
                    context.Debug.DamageState = "Souleater (combo)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DRKActions.Souleater.ActionId, DRKActions.Souleater.Name)
                        .AsCombo(2)
                        .Target(context.CurrentTarget?.Name.TextValue)
                        .Reason(
                            "Souleater — combo finisher that generates 20 Blood Gauge.",
                            "Souleater is the third and final step of the Hard Slash → Syphon Strike → Souleater combo. It generates 20 Blood Gauge and deals the highest potency of the combo. Always complete the full combo.")
                        .Factors("Combo step 2 active", "Generates 20 Blood Gauge", "Highest potency in combo")
                        .Alternatives("Break combo (massive damage loss)", "Spend Blood first if near cap")
                        .Tip("Complete the full 3-step combo every time. Souleater grants 20 Blood which you need for Bloodspiller. Syphon Strike also restores MP.")
                        .Concept(DrkConcepts.BloodGauge)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(DrkConcepts.BloodGauge, wasSuccessful: true);

                    return true;
                }
            }
        }

        // Combo step 1: Syphon Strike
        if (context.ComboStep == 1 && level >= DRKActions.SyphonStrike.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRKActions.SyphonStrike.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRKActions.SyphonStrike, targetId))
                {
                    context.Debug.PlannedAction = DRKActions.SyphonStrike.Name;
                    context.Debug.DamageState = "Syphon Strike (combo)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DRKActions.SyphonStrike.ActionId, DRKActions.SyphonStrike.Name)
                        .AsCombo(1)
                        .Target(context.CurrentTarget?.Name.TextValue)
                        .Reason(
                            "Syphon Strike — combo step 2, restores MP and continues to Souleater.",
                            "Syphon Strike is the second step of the Hard Slash → Syphon Strike → Souleater combo. It restores 600 MP and continues the combo chain. Always follow with Souleater.")
                        .Factors("Combo step 1 active", "Restores 600 MP", "Continues combo chain")
                        .Alternatives("Break combo (loses MP and Souleater Blood)", "Skip to Souleater (not available without full combo)")
                        .Tip("Syphon Strike restores precious MP used for Edge/Flood of Shadow. The full combo is: Hard Slash → Syphon Strike → Souleater.")
                        .Concept(DrkConcepts.Darkside)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(DrkConcepts.Darkside, wasSuccessful: true);

                    return true;
                }
            }
        }

        // Combo starter: Hard Slash
        if (context.ActionService.IsActionReady(DRKActions.HardSlash.ActionId))
        {
            if (context.ActionService.ExecuteGcd(DRKActions.HardSlash, targetId))
            {
                context.Debug.PlannedAction = DRKActions.HardSlash.Name;
                context.Debug.DamageState = "Hard Slash (start)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(DRKActions.HardSlash.ActionId, DRKActions.HardSlash.Name)
                    .AsCombo(0)
                    .Target(context.CurrentTarget?.Name.TextValue)
                    .Reason(
                        "Hard Slash — combo starter, begins the Hard Slash → Syphon Strike → Souleater chain.",
                        "Hard Slash starts the primary single-target combo. Follow with Syphon Strike then Souleater for MP restoration and Blood Gauge generation. This is your filler when no higher priority GCDs are available.")
                    .Factors("No higher priority GCD available", "Combo starter", "Enables Syphon Strike")
                    .Alternatives("Bloodspiller if near Blood cap", "Delirium combo if Delirium active")
                    .Tip("Hard Slash begins your bread-and-butter combo. Keep it cycling to generate Blood Gauge and restore MP. Interrupt the combo only for Bloodspiller or Delirium combo.")
                    .Concept(DrkConcepts.Darkside)
                    .Record();

                context.TrainingService?.RecordConceptApplication(DrkConcepts.Darkside, wasSuccessful: true);

                return true;
            }
        }

        return false;
    }

    private bool TryAoECombo(INyxContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Combo step 1: Stalwart Soul
        if (context.ComboStep == 1 && level >= DRKActions.StalwartSoul.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRKActions.StalwartSoul.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRKActions.StalwartSoul, targetId))
                {
                    context.Debug.PlannedAction = DRKActions.StalwartSoul.Name;
                    context.Debug.DamageState = "Stalwart Soul (AoE combo)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DRKActions.StalwartSoul.ActionId, DRKActions.StalwartSoul.Name)
                        .AsCombo(1)
                        .Target(context.CurrentTarget?.Name.TextValue)
                        .Reason(
                            "Stalwart Soul — AoE combo finisher, restores MP and generates Blood Gauge.",
                            "Stalwart Soul is the second step of the Unleash → Stalwart Soul AoE combo. It restores 600 MP and generates 20 Blood Gauge. Always follow Unleash with Stalwart Soul in AoE situations.")
                        .Factors("AoE combo step 1 active", "Restores 600 MP", "Generates 20 Blood Gauge")
                        .Alternatives("Break combo (loses resources)", "Single-target combo (lower AoE damage)")
                        .Tip("In AoE, keep the Unleash → Stalwart Soul combo cycling. Spend Blood with Quietus when above 50 to prevent overcap.")
                        .Concept(DrkConcepts.BloodGauge)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(DrkConcepts.BloodGauge, wasSuccessful: true);

                    return true;
                }
            }
        }

        // AoE starter: Unleash
        if (context.ActionService.IsActionReady(DRKActions.Unleash.ActionId))
        {
            if (context.ActionService.ExecuteGcd(DRKActions.Unleash, targetId))
            {
                context.Debug.PlannedAction = DRKActions.Unleash.Name;
                context.Debug.DamageState = "Unleash (AoE start)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(DRKActions.Unleash.ActionId, DRKActions.Unleash.Name)
                    .AsCombo(0)
                    .Target(context.CurrentTarget?.Name.TextValue)
                    .Reason(
                        "Unleash — AoE combo starter hitting all nearby enemies.",
                        "Unleash is the first step of the AoE combo. Follow with Stalwart Soul for MP restoration and Blood Gauge. Use the AoE combo when fighting 3+ enemies for more total damage than single-target actions.")
                    .Factors("3+ enemies in range", "AoE combo starter", "Enables Stalwart Soul")
                    .Alternatives("Hard Slash combo (single-target only)", "Quietus if Blood gauge is high")
                    .Tip("Switch to Unleash → Stalwart Soul when there are 3 or more enemies. This deals more total damage than single-target actions in AoE situations.")
                    .Concept(DrkConcepts.Darkside)
                    .Record();

                context.TrainingService?.RecordConceptApplication(DrkConcepts.Darkside, wasSuccessful: true);

                return true;
            }
        }

        return false;
    }

    #endregion
}
