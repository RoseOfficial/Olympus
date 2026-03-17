using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AresCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AresCore.Modules;

/// <summary>
/// Handles the Warrior damage rotation.
/// Manages combo chains, Beast Gauge spending, and burst windows.
/// </summary>
public sealed class DamageModule : IAresModule
{
    public int Priority => 30; // Lowest priority - damage after utility
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IAresContext context, bool isMoving)
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

        var player = context.Player;
        var level = player.Level;

        // Find target — melee range first via game API, fall back to gap-closer range for engagement
        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            WARActions.HeavySwing.ActionId,
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

        // Out of melee range: try Onslaught (gap close) then Tomahawk (ranged attack)
        if (target == null && engageTarget != null)
        {
            if (context.CanExecuteOgcd && TryOnslaught(context, engageTarget))
                return true;
            if (context.CanExecuteGcd && TryTomahawk(context, engageTarget))
                return true;
        }

        // Only run full rotation when a melee-range target is available
        if (target == null)
        {
            context.Debug.DamageState = "Target out of melee range";
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

        // Priority 1: Primal Rend (during Inner Release)
        if (TryPrimalRend(context, target))
            return true;

        // Priority 2: Primal Ruination (follow-up to Primal Rend)
        if (TryPrimalRuination(context, target))
            return true;

        // Priority 3: Inner Chaos (Nascent Chaos active)
        if (TryInnerChaos(context, target, enemyCount))
            return true;

        // Priority 4: Fell Cleave / Decimate (gauge spender)
        if (TryGaugeSpender(context, target, enemyCount))
            return true;

        // Priority 5: Surging Tempest refresh (if low)
        if (TrySurgingTempestRefresh(context, target, enemyCount))
            return true;

        // Priority 6: Main combo / AoE combo
        if (TryCombo(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IAresContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Damage

    private bool TryOgcdDamage(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Upheaval (single target oGCD)
        if (TryUpheaval(context, target, enemyCount))
            return true;

        // Priority 2: Orogeny (AoE oGCD)
        if (TryOrogeny(context, target, enemyCount))
            return true;

        // Priority 3: Onslaught (gap closer / extra damage)
        if (TryOnslaught(context, target))
            return true;

        return false;
    }

    private bool TryUpheaval(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Upheaval.MinLevel)
            return false;

        // Prefer Orogeny for AoE
        if (enemyCount >= AoeThreshold && level >= WARActions.Orogeny.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Upheaval.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Upheaval, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Upheaval.Name;
            context.Debug.DamageState = "Upheaval";

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.Upheaval.ActionId, WARActions.Upheaval.Name)
                .AsTankDamage()
                .Target(target.Name?.TextValue)
                .Reason(
                    "Upheaval deals heavy oGCD burst damage on a single target. Use it on cooldown.",
                    "Upheaval is a 400-potency oGCD that costs no resources. Use it every 30 seconds as part of your weave rotation.")
                .Factors("Upheaval ready", "Single target situation", "No Orogeny at level or fewer than 3 enemies")
                .Alternatives("Skip and lose potency (no reason to hold)", "Use Orogeny instead (AoE only)")
                .Tip("Upheaval is a free damage boost — use it on cooldown during every GCD window. Never hold it.")
                .Concept(WarConcepts.Upheaval)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.Upheaval, true, "oGCD burst damage");

            return true;
        }

        return false;
    }

    private bool TryOrogeny(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Orogeny.MinLevel)
            return false;

        // Only use in AoE situations
        if (enemyCount < AoeThreshold)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Orogeny.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Orogeny, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Orogeny.Name;
            context.Debug.DamageState = $"Orogeny ({enemyCount} enemies)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.Orogeny.ActionId, WARActions.Orogeny.Name)
                .AsTankDamage()
                .Target(target.Name?.TextValue)
                .Reason(
                    $"Orogeny deals AoE oGCD damage hitting {enemyCount} enemies simultaneously.",
                    "Orogeny replaces Upheaval in AoE situations — same potency per target but hits everything around you. Use it on cooldown when pulling packs.")
                .Factors($"{enemyCount} enemies in range", "AoE threshold met (3+)", "Orogeny preferred over Upheaval for multi-target")
                .Alternatives("Use Upheaval (single-target only)", "Skip (lose free AoE damage)")
                .Tip("In dungeon pulls, Orogeny is your free AoE damage button. Hit it every 30 seconds while tanking large packs.")
                .Concept(WarConcepts.Orogeny)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.Orogeny, true, "AoE oGCD burst");

            return true;
        }

        return false;
    }

    private bool TryOnslaught(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Onslaught.MinLevel)
            return false;

        // Use Onslaught to weave extra damage
        // At level 88+, has 3 charges - can use more freely
        if (!context.ActionService.IsActionReady(WARActions.Onslaught.ActionId))
            return false;

        // Check range — gap close if out of melee range but within 20y, else weave as damage
        var inMelee = DistanceHelper.IsActionInRange(WARActions.HeavySwing.ActionId, player, target);
        var dx = player.Position.X - target.Position.X;
        var dy = player.Position.Y - target.Position.Y;
        var dz = player.Position.Z - target.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (!inMelee && distance <= 20f)
        {
            // Gap close to target
            if (context.ActionService.ExecuteOgcd(WARActions.Onslaught, target.GameObjectId))
            {
                context.Debug.PlannedAction = WARActions.Onslaught.Name;
                context.Debug.DamageState = "Onslaught (gap close)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.Onslaught.ActionId, WARActions.Onslaught.Name)
                    .AsTankDamage()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        "Onslaught used as gap closer — you're out of melee range and need to re-engage.",
                        "Onslaught is both a gap closer and a damage tool. Use it to close the distance when you drift out of melee range to avoid losing GCD time.")
                    .Factors("Out of melee range", $"Target within 20y ({distance:F1}y)", "Gap close re-engagement")
                    .Alternatives("Run to target (slower, lose GCD time)", "Use Tomahawk (ranged but lower priority)")
                    .Tip("Use Onslaught to stay glued to the boss. A charge wasted on gap-closing is still worth it to maintain uptime.")
                    .Concept(WarConcepts.Onslaught)
                    .Record();

                context.TrainingService?.RecordConceptApplication(WarConcepts.Onslaught, true, "Gap close re-engagement");

                return true;
            }
        }
        else if (inMelee)
        {
            // In melee range — use as damage weave at all levels (level >= 88 guard removed)
            if (context.ActionService.ExecuteOgcd(WARActions.Onslaught, target.GameObjectId))
            {
                context.Debug.PlannedAction = WARActions.Onslaught.Name;
                context.Debug.DamageState = "Onslaught (weave)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.Onslaught.ActionId, WARActions.Onslaught.Name)
                    .AsTankDamage()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        "Onslaught woven as extra damage. At high levels you have 3 charges — use them freely for additional potency.",
                        "Onslaught is 100 potency with no GCD cost. Weave it freely when in melee range, especially during Inner Release windows for extra burst.")
                    .Factors("In melee range", "Charge available", "oGCD weave slot open")
                    .Alternatives("Hold charges (loses potency)", "Save all charges for gap close (low-risk in most fights)")
                    .Tip("With 3 Onslaught charges at level 88+, use them regularly. The damage adds up over a fight.")
                    .Concept(WarConcepts.Onslaught)
                    .Record();

                context.TrainingService?.RecordConceptApplication(WarConcepts.Onslaught, true, "oGCD weave damage");

                return true;
            }
        }

        return false;
    }

    private bool TryTomahawk(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Tomahawk.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Tomahawk.ActionId))
            return false;

        // Tomahawk is a ranged attack — only use when out of melee range
        if (DistanceHelper.IsActionInRange(WARActions.HeavySwing.ActionId, player, target))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.Tomahawk, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Tomahawk.Name;
            context.Debug.DamageState = "Tomahawk (ranged)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.Tomahawk.ActionId, WARActions.Tomahawk.Name)
                .AsTankDamage()
                .Target(target.Name?.TextValue)
                .Reason(
                    "Tomahawk used as a ranged attack — you're outside melee range and can't close with Onslaught.",
                    "Tomahawk is your only ranged GCD. Use it when you can't reach the target to keep dealing damage and maintain enmity.")
                .Factors("Out of melee range", "Onslaught not available or on cooldown", "Ranged attack keeps uptime")
                .Alternatives("Do nothing (waste GCD entirely)", "Move closer (may take too long)")
                .Tip("Tomahawk keeps your GCD rolling when you're forced out of melee range. Onslaught is better for gap-closing when charges are available.")
                .Concept(WarConcepts.FellCleave)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.FellCleave, true, "Ranged GCD uptime");

            return true;
        }

        return false;
    }

    #endregion

    #region Primal Abilities

    private bool TryPrimalRend(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.PrimalRend.MinLevel)
            return false;

        // Primal Rend Ready is granted by Inner Release
        if (!context.HasPrimalRendReady)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.PrimalRend.ActionId))
            return false;

        // Primal Rend is a ranged GCD (20y) - can use even at distance
        if (context.ActionService.ExecuteGcd(WARActions.PrimalRend, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.PrimalRend.Name;
            context.Debug.DamageState = "Primal Rend";

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.PrimalRend.ActionId, WARActions.PrimalRend.Name)
                .AsTankBurst()
                .Target(target.Name?.TextValue)
                .Reason(
                    "Primal Rend used while Primal Rend Ready is active. This is Inner Release's bonus GCD — highest potency in the burst window.",
                    "Primal Rend Ready is granted by Inner Release. Primal Rend is a 700-potency ranged GCD that must be used before the buff expires. It unlocks Primal Ruination.")
                .Factors("Primal Rend Ready active (from Inner Release)", "Highest potency GCD available", "Unlocks Primal Ruination follow-up")
                .Alternatives("Use Fell Cleave instead (wastes Primal Rend Ready)", "Delay (buff will expire)")
                .Tip("Always use Primal Rend immediately when Primal Rend Ready is active. It's your strongest single hit and is followed by Primal Ruination.")
                .Concept(WarConcepts.PrimalRend)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.PrimalRend, true, "Inner Release bonus GCD");

            return true;
        }

        return false;
    }

    private bool TryPrimalRuination(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.PrimalRuination.MinLevel)
            return false;

        // Primal Ruination Ready is granted after using Primal Rend
        if (!context.HasPrimalRuinationReady)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.PrimalRuination.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.PrimalRuination, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.PrimalRuination.Name;
            context.Debug.DamageState = "Primal Ruination";

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.PrimalRuination.ActionId, WARActions.PrimalRuination.Name)
                .AsTankBurst()
                .Target(target.Name?.TextValue)
                .Reason(
                    "Primal Ruination used as follow-up to Primal Rend. This completes the Inner Release burst chain.",
                    "Primal Ruination Ready is granted by using Primal Rend. It's a massive AoE GCD that caps the burst window. Always use it immediately after Primal Rend.")
                .Factors("Primal Ruination Ready active (follow-up to Primal Rend)", "Burst chain continuation", "Must be consumed before buff expires")
                .Alternatives("Use Fell Cleave instead (wastes Primal Ruination Ready)", "Delay (buff will expire)")
                .Tip("Primal Ruination follows Primal Rend in every Inner Release window. The sequence is: Inner Release → Fell Cleave × 3 → Primal Rend → Primal Ruination.")
                .Concept(WarConcepts.PrimalRend)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.PrimalRend, true, "Primal Ruination burst chain");

            return true;
        }

        return false;
    }

    #endregion

    #region Inner Chaos

    private bool TryInnerChaos(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Nascent Chaos enables either Inner Chaos (ST) or Chaotic Cyclone (AoE)
        if (!context.HasNascentChaos)
            return false;

        // Choose ST or AoE based on enemy count
        if (enemyCount >= AoeThreshold && level >= WARActions.ChaoticCyclone.MinLevel)
        {
            return TryChaoticCyclone(context, target, enemyCount);
        }
        else if (level >= WARActions.InnerChaos.MinLevel)
        {
            return TryInnerChaosST(context, target);
        }

        return false;
    }

    private bool TryInnerChaosST(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;

        if (!context.ActionService.IsActionReady(WARActions.InnerChaos.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.InnerChaos, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.InnerChaos.Name;
            context.Debug.DamageState = "Inner Chaos";

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.InnerChaos.ActionId, WARActions.InnerChaos.Name)
                .AsTankBurst()
                .Target(target.Name?.TextValue)
                .Reason(
                    "Inner Chaos used while Nascent Chaos is active. This is your highest single-target potency GCD and is free (no gauge cost).",
                    "Nascent Chaos is granted by using Infuriate during Inner Release. Inner Chaos is a 660-potency guaranteed crit + direct hit GCD that costs 0 gauge.")
                .Factors("Nascent Chaos active", "Single target situation", "Free gauge cost — guaranteed crit + direct hit")
                .Alternatives("Use Fell Cleave instead (wastes Nascent Chaos)", "Delay (buff will expire)")
                .Tip("Always use Inner Chaos when Nascent Chaos is active. Using Infuriate during Inner Release to get this is a core rotation decision.")
                .Concept(WarConcepts.InnerChaos)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.InnerChaos, true, "Nascent Chaos burst GCD");

            return true;
        }

        return false;
    }

    private bool TryChaoticCyclone(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;

        if (!context.ActionService.IsActionReady(WARActions.ChaoticCyclone.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.ChaoticCyclone, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.ChaoticCyclone.Name;
            context.Debug.DamageState = $"Chaotic Cyclone ({enemyCount} enemies)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.ChaoticCyclone.ActionId, WARActions.ChaoticCyclone.Name)
                .AsTankBurst()
                .Target(target.Name?.TextValue)
                .Reason(
                    $"Chaotic Cyclone used while Nascent Chaos is active in AoE ({enemyCount} enemies). Free guaranteed crit AoE GCD.",
                    "Chaotic Cyclone is the AoE version of Inner Chaos — same guaranteed crit + direct hit, no gauge cost, but hits all enemies. Use it over Inner Chaos when 3+ enemies are present.")
                .Factors($"Nascent Chaos active", $"{enemyCount} enemies in range", "AoE preferred over single target")
                .Alternatives("Use Inner Chaos (single target only)", "Use Decimate (costs gauge, lower priority)")
                .Tip("In AoE situations with Nascent Chaos, always use Chaotic Cyclone over Inner Chaos. More targets means more total damage.")
                .Concept(WarConcepts.NascentChaos)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.NascentChaos, true, "Chaotic Cyclone AoE burst");

            return true;
        }

        return false;
    }

    #endregion

    #region Gauge Spenders

    private bool TryGaugeSpender(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // During Inner Release, all gauge spenders are free (no cost)
        // Outside IR, need 50 gauge

        bool canSpend = context.HasInnerRelease || context.BeastGauge >= 50;
        if (!canSpend)
            return false;

        // Choose ST or AoE spender
        if (enemyCount >= AoeThreshold)
        {
            return TryDecimate(context, target, enemyCount);
        }
        else
        {
            return TryFellCleave(context, target);
        }
    }

    private bool TryFellCleave(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Get the appropriate Fell Cleave action
        var fellCleaveAction = WARActions.GetFellCleaveAction(level);

        if (level < WARActions.FellCleave.MinLevel)
        {
            // Pre-54, use Inner Beast instead
            if (level >= WARActions.InnerBeast.MinLevel)
            {
                fellCleaveAction = WARActions.InnerBeast;
            }
            else
            {
                return false;
            }
        }

        if (!context.ActionService.IsActionReady(fellCleaveAction.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(fellCleaveAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = fellCleaveAction.Name;
            if (context.HasInnerRelease)
            {
                context.Debug.DamageState = $"{fellCleaveAction.Name} (IR: {context.InnerReleaseStacks} stacks)";

                // Training: Record Inner Release burst
                TrainingHelper.Decision(context.TrainingService)
                    .Action(fellCleaveAction.ActionId, fellCleaveAction.Name)
                    .AsTankBurst()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        $"Fell Cleave during Inner Release - guaranteed critical direct hit with no gauge cost. {context.InnerReleaseStacks} stacks remaining.",
                        "During Inner Release, Fell Cleave costs 0 gauge and is guaranteed to crit + direct hit. Spam it to use all 3 stacks.")
                    .Factors($"Inner Release active ({context.InnerReleaseStacks} stacks)", "Fell Cleave free (0 gauge cost)", "Guaranteed crit + direct hit")
                    .Alternatives("Use other GCDs (wastes IR stacks)", "Save gauge for later (IR makes it free anyway)")
                    .Tip("During Inner Release, spam Fell Cleave to burn all 3 stacks. Don't use other GCDs - you're wasting massive damage.")
                    .Concept("war_inner_release")
                    .Record();

                context.TrainingService?.RecordConceptApplication("war_inner_release", true, "Burst window execution");
            }
            else
            {
                context.Debug.DamageState = $"{fellCleaveAction.Name} ({context.BeastGauge} gauge)";

                // Training: Record gauge spending
                TrainingHelper.Decision(context.TrainingService)
                    .Action(fellCleaveAction.ActionId, fellCleaveAction.Name)
                    .AsTankResource(context.BeastGauge)
                    .Reason(
                        $"Fell Cleave to spend Beast Gauge. {context.BeastGauge} gauge available.",
                        "Fell Cleave is your main gauge spender. Use it when at 50+ gauge to prevent overcapping during combos.")
                    .Factors($"Beast Gauge at {context.BeastGauge}", "50 gauge cost", "High potency single-target")
                    .Alternatives("Hold for Inner Release (may overcap)", "Use combo instead (lower damage)")
                    .Tip("Don't overcap Beast Gauge - use Fell Cleave when at 50+ gauge. Save gauge only if Inner Release is coming very soon.")
                    .Concept("war_infuriate_gauge")
                    .Record();

                context.TrainingService?.RecordConceptApplication("war_infuriate_gauge", true, "Gauge spending");
            }
            return true;
        }

        return false;
    }

    private bool TryDecimate(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Get the appropriate Decimate action
        var decimateAction = WARActions.GetDecimateAction(level);

        if (level < WARActions.SteelCyclone.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(decimateAction.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(decimateAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = decimateAction.Name;
            if (context.HasInnerRelease)
            {
                context.Debug.DamageState = $"{decimateAction.Name} ({enemyCount} enemies, IR)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(decimateAction.ActionId, decimateAction.Name)
                    .AsTankBurst()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        $"{decimateAction.Name} during Inner Release — free AoE gauge spender hitting {enemyCount} enemies.",
                        "During Inner Release, Decimate costs 0 gauge and is guaranteed to crit + direct hit. It's the AoE equivalent of Fell Cleave during the burst window.")
                    .Factors($"Inner Release active ({context.InnerReleaseStacks} stacks)", $"{enemyCount} enemies in range", "Free gauge cost — guaranteed crit + direct hit")
                    .Alternatives("Use Fell Cleave (single target only)", "Skip AoE (waste IR value on multi-target)")
                    .Tip("During Inner Release with multiple targets, always use Decimate instead of Fell Cleave. You hit all enemies for free.")
                    .Concept(WarConcepts.IRWindow)
                    .Record();

                context.TrainingService?.RecordConceptApplication(WarConcepts.IRWindow, true, "AoE burst during Inner Release");
            }
            else
            {
                context.Debug.DamageState = $"{decimateAction.Name} ({enemyCount} enemies)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(decimateAction.ActionId, decimateAction.Name)
                    .AsTankResource(context.BeastGauge)
                    .Target(target.Name?.TextValue)
                    .Reason(
                        $"{decimateAction.Name} to spend Beast Gauge on {enemyCount} enemies. AoE gauge spender for multi-target pulls.",
                        "Decimate spends 50 Beast Gauge to deal AoE damage. In large pulls it's significantly better than Fell Cleave since the potency hits all targets.")
                    .Factors($"Beast Gauge at {context.BeastGauge}", $"{enemyCount} enemies in range", "50 gauge cost — AoE preferred")
                    .Alternatives("Use Fell Cleave (single target, wastes AoE opportunity)", "Hold gauge (risks overcap)")
                    .Tip("With 3+ enemies, always Decimate over Fell Cleave. The 50 gauge is the same cost but Decimate hits everyone.")
                    .Concept(WarConcepts.BeastGauge)
                    .Record();

                context.TrainingService?.RecordConceptApplication(WarConcepts.BeastGauge, true, "AoE gauge spending");
            }
            return true;
        }

        return false;
    }

    #endregion

    #region Surging Tempest Management

    private bool TrySurgingTempestRefresh(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Surging Tempest is granted by:
        // - Storm's Eye (ST combo finisher, level 50+)
        // - Mythril Tempest (AoE combo finisher, level 40+)

        // Only refresh if buff is about to expire
        if (context.HasSurgingTempest && context.SurgingTempestRemaining > 10f)
            return false;

        // During Inner Release, prefer gauge spenders over refreshing
        if (context.HasInnerRelease && context.InnerReleaseStacks > 0)
            return false;

        // Check if we can finish the combo
        if (enemyCount >= AoeThreshold)
        {
            return TryMythrilTempestFinish(context, target, enemyCount);
        }
        else
        {
            return TryStormsEyeFinish(context, target);
        }
    }

    private bool TryStormsEyeFinish(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.StormsEye.MinLevel)
            return false;

        // Storm's Eye is the 3rd hit of the combo
        // Need to check if we're at the right combo step
        if (context.ComboStep != 2 || context.LastComboAction != WARActions.Maim.ActionId)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.StormsEye.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.StormsEye, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.StormsEye.Name;
            context.Debug.DamageState = "Storm's Eye (refresh buff)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.StormsEye.ActionId, WARActions.StormsEye.Name)
                .AsTankDamage()
                .Target(target.Name?.TextValue)
                .Reason(
                    $"Storm's Eye used to refresh Surging Tempest. Buff has {context.SurgingTempestRemaining:F1}s remaining — must be refreshed before it expires.",
                    "Surging Tempest increases all damage dealt by 10%. Storm's Eye refreshes it as the combo finisher. Never let Surging Tempest fall off.")
                .Factors(
                    context.HasSurgingTempest
                        ? $"Surging Tempest at {context.SurgingTempestRemaining:F1}s (needs refresh)"
                        : "Surging Tempest not active (first application)",
                    "At combo step 3 (after Maim)",
                    "10% damage buff must be maintained")
                .Alternatives("Use Storm's Path (grants more gauge, but lets buff expire)", "Skip combo (loses buff and damage)")
                .Tip("Choose Storm's Eye when Surging Tempest is below 10s or missing. Choose Storm's Path when the buff has plenty of time left.")
                .Concept(WarConcepts.SurgingTempest)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.SurgingTempest, true, "Surging Tempest refresh");

            return true;
        }

        return false;
    }

    private bool TryMythrilTempestFinish(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.MythrilTempest.MinLevel)
            return false;

        // Mythril Tempest is the 2nd hit of the AoE combo
        if (context.ComboStep != 1 || context.LastComboAction != WARActions.Overpower.ActionId)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.MythrilTempest.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.MythrilTempest, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.MythrilTempest.Name;
            context.Debug.DamageState = $"Mythril Tempest ({enemyCount} enemies, refresh buff)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.MythrilTempest.ActionId, WARActions.MythrilTempest.Name)
                .AsTankDamage()
                .Target(target.Name?.TextValue)
                .Reason(
                    $"Mythril Tempest used to refresh Surging Tempest in AoE situation. {enemyCount} enemies present, buff has {context.SurgingTempestRemaining:F1}s remaining.",
                    "Surging Tempest applies to all your damage including AoE. Mythril Tempest is the AoE combo finisher that refreshes the buff while hitting all enemies.")
                .Factors(
                    context.HasSurgingTempest
                        ? $"Surging Tempest at {context.SurgingTempestRemaining:F1}s (needs refresh)"
                        : "Surging Tempest not active",
                    $"{enemyCount} enemies in range",
                    "AoE combo step 2 (after Overpower)")
                .Alternatives("Single target combo (misses AoE damage)", "Prioritize gauge spenders (buff expires)")
                .Tip("In AoE pulls, always complete Overpower → Mythril Tempest to keep Surging Tempest active. The buff affects all your AoE damage too.")
                .Concept(WarConcepts.SurgingTempest)
                .Record();

            context.TrainingService?.RecordConceptApplication(WarConcepts.SurgingTempest, true, "AoE Surging Tempest refresh");

            return true;
        }

        return false;
    }

    #endregion

    #region Main Combo

    private bool TryCombo(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE rotation for 3+ enemies
        if (enemyCount >= AoeThreshold)
        {
            return TryAoeCombo(context, target, enemyCount);
        }

        // Single target combo
        return TrySingleTargetCombo(context, target);
    }

    private bool TrySingleTargetCombo(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // ST Combo: Heavy Swing -> Maim -> Storm's Path/Eye
        // Storm's Eye grants Surging Tempest (damage buff)
        // Storm's Path grants more gauge

        // Combo step 3: Finisher
        if (context.ComboStep == 2 && context.LastComboAction == WARActions.Maim.ActionId)
        {
            // Choose finisher based on Surging Tempest status
            // Need to refresh if buff is low (< 10s) or missing
            bool needsSurgingTempest = !context.HasSurgingTempest || context.SurgingTempestRemaining < 10f;
            var finisherAction = WARActions.GetComboFinisher(level, needsSurgingTempest);

            if (context.ActionService.IsActionReady(finisherAction.ActionId))
            {
                if (context.ActionService.ExecuteGcd(finisherAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = finisherAction.Name;
                    context.Debug.DamageState = $"{finisherAction.Name} (combo 3)";

                    var isStormsEye = finisherAction.ActionId == WARActions.StormsEye.ActionId;
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(finisherAction.ActionId, finisherAction.Name)
                        .AsTankDamage()
                        .Target(target.Name?.TextValue)
                        .Reason(
                            isStormsEye
                                ? "Storm's Eye used as combo finisher — grants Beast Gauge and refreshes Surging Tempest."
                                : "Storm's Path used as combo finisher — grants 20 Beast Gauge (more than Storm's Eye).",
                            isStormsEye
                                ? "Storm's Eye gives 10 gauge and refreshes Surging Tempest (your 10% damage buff). Choose it when the buff needs refreshing."
                                : "Storm's Path gives 20 gauge and builds toward Fell Cleave. Choose it when Surging Tempest has plenty of time remaining.")
                        .Factors(
                            "Combo step 3 (after Maim)",
                            isStormsEye
                                ? $"Surging Tempest has {context.SurgingTempestRemaining:F1}s (needs refresh)"
                                : $"Surging Tempest OK ({context.SurgingTempestRemaining:F1}s), maximizing gauge")
                        .Alternatives(
                            isStormsEye
                                ? "Storm's Path (more gauge, but buff expires)"
                                : "Storm's Eye (refresh buff, but less gauge)")
                        .Tip("Alternate finishers: Storm's Eye when Surging Tempest is low (< 10s), Storm's Path when the buff is healthy and you need gauge.")
                        .Concept(WarConcepts.SurgingTempest)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(WarConcepts.SurgingTempest, true, "Combo finisher selection");

                    return true;
                }
            }
        }

        // Combo step 2: Maim
        if (context.ComboStep == 1 && context.LastComboAction == WARActions.HeavySwing.ActionId)
        {
            if (level >= WARActions.Maim.MinLevel)
            {
                if (context.ActionService.IsActionReady(WARActions.Maim.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(WARActions.Maim, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = WARActions.Maim.Name;
                        context.Debug.DamageState = "Maim (combo 2)";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(WARActions.Maim.ActionId, WARActions.Maim.Name)
                            .AsTankDamage()
                            .Target(target.Name?.TextValue)
                            .Reason(
                                "Maim is combo step 2 — follows Heavy Swing and leads to Storm's Path or Storm's Eye.",
                                "The Warrior ST combo is Heavy Swing → Maim → Storm's Path/Eye. Maim is the middle step that grants 10 Beast Gauge and sets up the finisher choice.")
                            .Factors("Combo step 2 (after Heavy Swing)", "Grants 10 Beast Gauge", "Unlocks Storm's Path and Storm's Eye")
                            .Alternatives("Skip combo (reset to Heavy Swing, wastes combo bonus)", "Break combo early (significant damage loss)")
                            .Tip("Never break your combo chain — always follow Heavy Swing with Maim, then choose your finisher.")
                            .Concept(WarConcepts.BeastGauge)
                            .Record();

                        context.TrainingService?.RecordConceptApplication(WarConcepts.BeastGauge, true, "Combo gauge generation");

                        return true;
                    }
                }
            }
        }

        // Combo step 1: Heavy Swing (always available)
        if (context.ActionService.IsActionReady(WARActions.HeavySwing.ActionId))
        {
            if (context.ActionService.ExecuteGcd(WARActions.HeavySwing, target.GameObjectId))
            {
                context.Debug.PlannedAction = WARActions.HeavySwing.Name;
                context.Debug.DamageState = "Heavy Swing (combo 1)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.HeavySwing.ActionId, WARActions.HeavySwing.Name)
                    .AsTankDamage()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        "Heavy Swing starts or restarts the single-target combo chain.",
                        "Heavy Swing is the first GCD of the Warrior ST combo: Heavy Swing → Maim → Storm's Path/Eye. It's your bread-and-butter filler when no higher-priority actions are available.")
                    .Factors("Combo step 1 — starts the chain", "No higher priority GCDs available", "ST rotation foundation")
                    .Alternatives("Use Inner Chaos (only with Nascent Chaos)", "Use Fell Cleave (only with 50 gauge)")
                    .Tip("The WAR combo flows naturally. Heavy Swing → Maim → finisher, then back to Heavy Swing. Keep the chain going to generate gauge.")
                    .Concept(WarConcepts.BeastGauge)
                    .Record();

                context.TrainingService?.RecordConceptApplication(WarConcepts.BeastGauge, true, "Combo chain start");

                return true;
            }
        }

        return false;
    }

    private bool TryAoeCombo(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE Combo: Overpower -> Mythril Tempest

        // Combo step 2: Mythril Tempest
        if (context.ComboStep == 1 && context.LastComboAction == WARActions.Overpower.ActionId)
        {
            if (level >= WARActions.MythrilTempest.MinLevel)
            {
                if (context.ActionService.IsActionReady(WARActions.MythrilTempest.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(WARActions.MythrilTempest, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = WARActions.MythrilTempest.Name;
                        context.Debug.DamageState = $"Mythril Tempest ({enemyCount} enemies, combo 2)";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(WARActions.MythrilTempest.ActionId, WARActions.MythrilTempest.Name)
                            .AsTankDamage()
                            .Target(target.Name?.TextValue)
                            .Reason(
                                $"Mythril Tempest completes the AoE combo — grants 20 Beast Gauge and applies Surging Tempest to {enemyCount} enemies.",
                                "Overpower → Mythril Tempest is the AoE combo. Mythril Tempest grants 20 Beast Gauge and refreshes Surging Tempest. Always complete the combo in AoE pulls.")
                            .Factors($"{enemyCount} enemies in range", "AoE combo step 2 (after Overpower)", "Grants 20 Beast Gauge + Surging Tempest")
                            .Alternatives("Single target combo (misses AoE gauge generation)", "Stop at Overpower (less gauge)")
                            .Tip("In dungeon pulls, spam Overpower → Mythril Tempest to build gauge quickly and keep Surging Tempest active.")
                            .Concept(WarConcepts.BeastGauge)
                            .Record();

                        context.TrainingService?.RecordConceptApplication(WarConcepts.BeastGauge, true, "AoE combo gauge generation");

                        return true;
                    }
                }
            }
        }

        // Combo step 1: Overpower
        if (level >= WARActions.Overpower.MinLevel)
        {
            if (context.ActionService.IsActionReady(WARActions.Overpower.ActionId))
            {
                if (context.ActionService.ExecuteGcd(WARActions.Overpower, target.GameObjectId))
                {
                    context.Debug.PlannedAction = WARActions.Overpower.Name;
                    context.Debug.DamageState = $"Overpower ({enemyCount} enemies, combo 1)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(WARActions.Overpower.ActionId, WARActions.Overpower.Name)
                        .AsTankDamage()
                        .Target(target.Name?.TextValue)
                        .Reason(
                            $"Overpower starts the AoE combo for {enemyCount} enemies. Use it to build gauge and set up Mythril Tempest.",
                            "Overpower is step 1 of the AoE combo. It deals AoE damage in a cone and leads to Mythril Tempest which grants gauge and Surging Tempest.")
                        .Factors($"{enemyCount} enemies in range", "AoE combo step 1", "Sets up Mythril Tempest")
                        .Alternatives("Heavy Swing (single target, loses AoE value)", "Skip (miss gauge generation opportunity)")
                        .Tip("With 3+ enemies, use Overpower to start the AoE chain. Follow with Mythril Tempest to complete the combo.")
                        .Concept(WarConcepts.BeastGauge)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(WarConcepts.BeastGauge, true, "AoE combo start");

                    return true;
                }
            }
        }

        // Fallback to single target if AoE not available
        return TrySingleTargetCombo(context, target);
    }

    #endregion
}
