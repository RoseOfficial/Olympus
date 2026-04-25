using Olympus.Data;
using Olympus.Rotation.AresCore.Abilities;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AresCore.Modules;

/// <summary>
/// Handles the Warrior damage rotation (scheduler-driven).
/// </summary>
public sealed class DamageModule : IAresModule
{
    public int Priority => 30;
    public string Name => "Damage";

    public bool TryExecute(IAresContext context, bool isMoving) => false;

    public void UpdateDebugState(IAresContext context) { }

    public void CollectCandidates(IAresContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.Configuration.Tank.EnableDamage)
        {
            context.Debug.DamageState = "Disabled";
            return;
        }

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

        var player = context.Player;

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
            return;
        }

        // Out-of-melee: Onslaught (gap close) + Tomahawk (ranged filler)
        if (target == null)
        {
            TryPushOnslaughtGapClose(context, scheduler, engageTarget.GameObjectId, engageTarget);
            TryPushTomahawk(context, scheduler, engageTarget.GameObjectId, engageTarget);
            return;
        }

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // oGCD pushes
        TryPushPrimalWrath(context, scheduler, target.GameObjectId, enemyCount);
        TryPushUpheaval(context, scheduler, target.GameObjectId, enemyCount);
        TryPushOrogeny(context, scheduler, target.GameObjectId, enemyCount);
        TryPushOnslaughtWeave(context, scheduler, target.GameObjectId, target);

        // GCD pushes — higher priority burst first, then combo
        TryPushPrimalRend(context, scheduler, target.GameObjectId);
        TryPushPrimalRuination(context, scheduler, target.GameObjectId);
        TryPushInnerChaos(context, scheduler, target.GameObjectId, enemyCount);
        TryPushGaugeSpender(context, scheduler, target.GameObjectId, enemyCount);
        TryPushSurgingTempestRefresh(context, scheduler, target.GameObjectId, enemyCount);
        TryPushCombo(context, scheduler, target.GameObjectId, enemyCount);
    }

    #region Out-of-melee

    private void TryPushOnslaughtGapClose(IAresContext context, RotationScheduler scheduler, ulong targetId,
                                          Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        if (!context.Configuration.Tank.EnableOnslaught) return;
        var player = context.Player;
        if (player.Level < WARActions.Onslaught.MinLevel) return;
        if (!context.ActionService.IsActionReady(WARActions.Onslaught.ActionId)) return;
        if (context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, player))
        {
            context.Debug.DamageState = $"Onslaught blocked: {context.TargetingService.GapCloserSafety.LastBlockReason}";
            return;
        }

        scheduler.PushOgcd(AresAbilities.Onslaught, targetId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.Onslaught.Name;
                context.Debug.DamageState = "Onslaught (gap close)";
            });
    }

    private void TryPushTomahawk(IAresContext context, RotationScheduler scheduler, ulong targetId,
                                 Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        if (player.Level < WARActions.Tomahawk.MinLevel) return;
        if (!context.ActionService.IsActionReady(WARActions.Tomahawk.ActionId)) return;
        if (DistanceHelper.IsActionInRange(WARActions.HeavySwing.ActionId, player, target)) return;

        scheduler.PushGcd(AresAbilities.Tomahawk, targetId, priority: 10,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.Tomahawk.Name;
                context.Debug.DamageState = "Tomahawk (ranged)";
            });
    }

    #endregion

    #region oGCDs

    private void TryPushPrimalWrath(IAresContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        if (!context.Configuration.Tank.EnablePrimalWrath) return;
        if (context.Player.Level < WARActions.PrimalWrath.MinLevel) return;
        if (!context.HasWrathful) return;
        if (!context.ActionService.IsActionReady(WARActions.PrimalWrath.ActionId)) return;

        scheduler.PushOgcd(AresAbilities.PrimalWrath, targetId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.PrimalWrath.Name;
                context.Debug.DamageState = $"Primal Wrath ({enemyCount} enemies)";
            });
    }

    private void TryPushUpheaval(IAresContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        if (!context.Configuration.Tank.EnableOrogeny) return;
        var level = context.Player.Level;
        if (level < WARActions.Upheaval.MinLevel) return;

        // Prefer Orogeny for AoE
        if (context.Configuration.Tank.EnableAoEDamage &&
            enemyCount >= context.Configuration.Tank.AoEMinTargets &&
            level >= WARActions.Orogeny.MinLevel) return;

        if (!context.ActionService.IsActionReady(WARActions.Upheaval.ActionId)) return;

        scheduler.PushOgcd(AresAbilities.Upheaval, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.Upheaval.Name;
                context.Debug.DamageState = "Upheaval";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.Upheaval.ActionId, WARActions.Upheaval.Name)
                    .AsTankDamage()
                    .Reason("Upheaval on cooldown.", "400-potency oGCD on 30s CD.")
                    .Factors("Upheaval ready", "Single target")
                    .Alternatives("Skip (no reason to hold)")
                    .Tip("Use on cooldown.")
                    .Concept(WarConcepts.Upheaval)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.Upheaval, true, "oGCD burst");
            });
    }

    private void TryPushOrogeny(IAresContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        if (!context.Configuration.Tank.EnableOrogeny) return;
        var level = context.Player.Level;
        if (level < WARActions.Orogeny.MinLevel) return;
        if (!context.Configuration.Tank.EnableAoEDamage) return;
        if (enemyCount < context.Configuration.Tank.AoEMinTargets) return;
        if (!context.ActionService.IsActionReady(WARActions.Orogeny.ActionId)) return;

        scheduler.PushOgcd(AresAbilities.Orogeny, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.Orogeny.Name;
                context.Debug.DamageState = $"Orogeny ({enemyCount} enemies)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.Orogeny.ActionId, WARActions.Orogeny.Name)
                    .AsTankDamage()
                    .Reason($"Orogeny AoE oGCD ({enemyCount} enemies).", "Replaces Upheaval in AoE.")
                    .Factors($"{enemyCount} enemies", "AoE threshold met")
                    .Alternatives("Upheaval (ST only)")
                    .Tip("Free AoE damage in pulls.")
                    .Concept(WarConcepts.Orogeny)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.Orogeny, true, "AoE oGCD");
            });
    }

    private void TryPushOnslaughtWeave(IAresContext context, RotationScheduler scheduler, ulong targetId,
                                       Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        if (!context.Configuration.Tank.EnableOnslaught) return;
        if (!context.Configuration.Tank.AutoOnslaught) return;
        var player = context.Player;
        if (player.Level < WARActions.Onslaught.MinLevel) return;
        if (!context.ActionService.IsActionReady(WARActions.Onslaught.ActionId)) return;
        if (context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, player))
        {
            context.Debug.DamageState = $"Onslaught blocked: {context.TargetingService.GapCloserSafety.LastBlockReason}";
            return;
        }
        if (!DistanceHelper.IsActionInRange(WARActions.HeavySwing.ActionId, player, target)) return;

        scheduler.PushOgcd(AresAbilities.Onslaught, targetId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.Onslaught.Name;
                context.Debug.DamageState = "Onslaught (weave)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.Onslaught.ActionId, WARActions.Onslaught.Name)
                    .AsTankDamage()
                    .Reason("Onslaught woven as extra damage.", "100 potency, no GCD cost.")
                    .Factors("In melee range", "Charge available")
                    .Alternatives("Hold charges")
                    .Tip("3 charges at Lv.88+.")
                    .Concept(WarConcepts.Onslaught)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.Onslaught, true, "oGCD weave");
            });
    }

    #endregion

    #region Primal chain

    private void TryPushPrimalRend(IAresContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (!context.Configuration.Tank.EnablePrimalRend) return;
        if (!context.Configuration.Tank.AutoPrimalRend) return;
        var level = context.Player.Level;
        if (level < WARActions.PrimalRend.MinLevel) return;
        if (!context.HasPrimalRendReady) return;
        if (!context.ActionService.IsActionReady(WARActions.PrimalRend.ActionId)) return;

        scheduler.PushGcd(AresAbilities.PrimalRend, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.PrimalRend.Name;
                context.Debug.DamageState = "Primal Rend";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.PrimalRend.ActionId, WARActions.PrimalRend.Name)
                    .AsTankBurst()
                    .Reason("Primal Rend Ready active.", "700-potency ranged GCD, unlocks Primal Ruination.")
                    .Factors("Primal Rend Ready active", "Highest potency GCD")
                    .Alternatives("Use Fell Cleave (wastes proc)")
                    .Tip("Always use Primal Rend immediately when ready.")
                    .Concept(WarConcepts.PrimalRend)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.PrimalRend, true, "Inner Release bonus GCD");
            });
    }

    private void TryPushPrimalRuination(IAresContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (!context.Configuration.Tank.EnablePrimalRuination) return;
        var level = context.Player.Level;
        if (level < WARActions.PrimalRuination.MinLevel) return;
        if (!context.HasPrimalRuinationReady) return;
        if (!context.ActionService.IsActionReady(WARActions.PrimalRuination.ActionId)) return;

        scheduler.PushGcd(AresAbilities.PrimalRuination, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.PrimalRuination.Name;
                context.Debug.DamageState = "Primal Ruination";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.PrimalRuination.ActionId, WARActions.PrimalRuination.Name)
                    .AsTankBurst()
                    .Reason("Primal Ruination - follow-up to Primal Rend.", "Completes Inner Release burst chain.")
                    .Factors("Primal Ruination Ready", "Burst chain")
                    .Alternatives("Use Fell Cleave (wastes proc)")
                    .Tip("Follows Primal Rend in every Inner Release window.")
                    .Concept(WarConcepts.PrimalRend)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.PrimalRend, true, "Burst chain");
            });
    }

    #endregion

    #region Inner Chaos

    private void TryPushInnerChaos(IAresContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        if (!context.HasNascentChaos) return;
        var level = context.Player.Level;

        // AoE path
        if (context.Configuration.Tank.EnableAoEDamage &&
            enemyCount >= context.Configuration.Tank.AoEMinTargets &&
            level >= WARActions.ChaoticCyclone.MinLevel)
        {
            if (!context.ActionService.IsActionReady(WARActions.ChaoticCyclone.ActionId)) return;
            scheduler.PushGcd(AresAbilities.ChaoticCyclone, targetId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = WARActions.ChaoticCyclone.Name;
                    context.Debug.DamageState = $"Chaotic Cyclone ({enemyCount} enemies)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(WARActions.ChaoticCyclone.ActionId, WARActions.ChaoticCyclone.Name)
                        .AsTankBurst()
                        .Reason($"Chaotic Cyclone AoE ({enemyCount}).", "Free guaranteed crit AoE GCD.")
                        .Factors("Nascent Chaos active", $"{enemyCount} enemies")
                        .Alternatives("Inner Chaos (ST only)")
                        .Tip("In AoE, always Chaotic Cyclone over Inner Chaos.")
                        .Concept(WarConcepts.NascentChaos)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(WarConcepts.NascentChaos, true, "Chaotic Cyclone AoE burst");
                });
            return;
        }

        // ST path
        if (level < WARActions.InnerChaos.MinLevel) return;
        if (!context.ActionService.IsActionReady(WARActions.InnerChaos.ActionId)) return;
        scheduler.PushGcd(AresAbilities.InnerChaos, targetId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.InnerChaos.Name;
                context.Debug.DamageState = "Inner Chaos";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.InnerChaos.ActionId, WARActions.InnerChaos.Name)
                    .AsTankBurst()
                    .Reason("Inner Chaos on Nascent Chaos.", "660-potency free guaranteed crit + direct hit.")
                    .Factors("Nascent Chaos active", "Single target")
                    .Alternatives("Fell Cleave (wastes proc)")
                    .Tip("Always use Inner Chaos when Nascent Chaos is active.")
                    .Concept(WarConcepts.InnerChaos)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.InnerChaos, true, "Nascent Chaos burst GCD");
            });
    }

    #endregion

    #region Gauge spenders

    private void TryPushGaugeSpender(IAresContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        bool atCap = context.BeastGauge >= context.Configuration.Tank.BeastGaugeCap;
        bool canSpend = context.HasInnerRelease || context.BeastGauge >= 50 || atCap;
        if (!canSpend) return;

        var level = context.Player.Level;

        // AoE: Decimate
        if (context.Configuration.Tank.EnableAoEDamage &&
            enemyCount >= context.Configuration.Tank.AoEMinTargets)
        {
            var decimateAction = WARActions.GetDecimateAction(level);
            if (level < WARActions.SteelCyclone.MinLevel) return;
            if (!context.ActionService.IsActionReady(decimateAction.ActionId)) return;

            var irStacks = context.InnerReleaseStacks;
            var gauge = context.BeastGauge;
            var hasIR = context.HasInnerRelease;
            var behavior = level >= WARActions.Decimate.MinLevel ? AresAbilities.Decimate : AresAbilities.SteelCyclone;

            scheduler.PushGcd(behavior, targetId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = decimateAction.Name;
                    context.Debug.DamageState = hasIR
                        ? $"{decimateAction.Name} ({enemyCount} enemies, IR)"
                        : $"{decimateAction.Name} ({enemyCount} enemies)";
                    if (hasIR)
                    {
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(decimateAction.ActionId, decimateAction.Name)
                            .AsTankBurst()
                            .Reason($"{decimateAction.Name} during Inner Release ({enemyCount}).", "Free guaranteed crit AoE.")
                            .Factors($"IR stacks: {irStacks}", $"{enemyCount} enemies")
                            .Alternatives("Fell Cleave (ST only)")
                            .Tip("In AoE IR, always Decimate.")
                            .Concept(WarConcepts.IRWindow)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(WarConcepts.IRWindow, true, "AoE IR burst");
                    }
                    else
                    {
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(decimateAction.ActionId, decimateAction.Name)
                            .AsTankResource(gauge)
                            .Reason($"{decimateAction.Name} AoE gauge spend ({enemyCount}).", "50 gauge AoE damage.")
                            .Factors($"Gauge: {gauge}", $"{enemyCount} enemies")
                            .Alternatives("Fell Cleave (ST, wastes AoE opportunity)")
                            .Tip("With 3+ enemies, Decimate over Fell Cleave.")
                            .Concept(WarConcepts.BeastGauge)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(WarConcepts.BeastGauge, true, "AoE gauge spending");
                    }
                });
            return;
        }

        // ST: Fell Cleave (or Inner Beast pre-54)
        var fellCleaveAction = WARActions.GetFellCleaveAction(level);
        if (level < WARActions.FellCleave.MinLevel)
        {
            if (level < WARActions.InnerBeast.MinLevel) return;
            fellCleaveAction = WARActions.InnerBeast;
        }

        if (!context.ActionService.IsActionReady(fellCleaveAction.ActionId)) return;

        var irStacksB = context.InnerReleaseStacks;
        var gaugeB = context.BeastGauge;
        var hasIRB = context.HasInnerRelease;
        var behaviorB = level >= WARActions.FellCleave.MinLevel ? AresAbilities.FellCleave : AresAbilities.InnerBeast;

        scheduler.PushGcd(behaviorB, targetId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = fellCleaveAction.Name;
                context.Debug.DamageState = hasIRB
                    ? $"{fellCleaveAction.Name} (IR: {irStacksB} stacks)"
                    : $"{fellCleaveAction.Name} ({gaugeB} gauge)";
                if (hasIRB)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(fellCleaveAction.ActionId, fellCleaveAction.Name)
                        .AsTankBurst()
                        .Reason($"Fell Cleave during IR ({irStacksB} stacks).", "Free guaranteed crit + direct hit.")
                        .Factors($"IR stacks: {irStacksB}")
                        .Alternatives("Other GCDs (wastes IR stacks)")
                        .Tip("Spam Fell Cleave during IR.")
                        .Concept("war_inner_release")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("war_inner_release", true, "Burst window");
                }
                else
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(fellCleaveAction.ActionId, fellCleaveAction.Name)
                        .AsTankResource(gaugeB)
                        .Reason($"Fell Cleave gauge spend ({gaugeB}).", "50 gauge ST damage.")
                        .Factors($"Gauge: {gaugeB}")
                        .Alternatives("Hold for IR (may overcap)")
                        .Tip("Don't overcap gauge.")
                        .Concept("war_infuriate_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("war_infuriate_gauge", true, "Gauge spending");
                }
            });
    }

    #endregion

    #region Surging Tempest refresh

    private void TryPushSurgingTempestRefresh(IAresContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        if (context.HasSurgingTempest && context.SurgingTempestRemaining > 10f) return;
        if (context.HasInnerRelease && context.InnerReleaseStacks > 0) return;

        var level = context.Player.Level;

        // AoE path: Mythril Tempest
        if (context.Configuration.Tank.EnableAoEDamage &&
            enemyCount >= context.Configuration.Tank.AoEMinTargets)
        {
            if (level < WARActions.MythrilTempest.MinLevel) return;
            if (context.ComboStep != 1 || context.LastComboAction != WARActions.Overpower.ActionId) return;
            if (!context.ActionService.IsActionReady(WARActions.MythrilTempest.ActionId)) return;

            var rem = context.SurgingTempestRemaining;
            scheduler.PushGcd(AresAbilities.MythrilTempest, targetId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = WARActions.MythrilTempest.Name;
                    context.Debug.DamageState = $"Mythril Tempest ({enemyCount} enemies, refresh)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(WARActions.MythrilTempest.ActionId, WARActions.MythrilTempest.Name)
                        .AsTankDamage()
                        .Reason($"Mythril Tempest refresh ({enemyCount}).", "Refreshes Surging Tempest in AoE.")
                        .Factors($"Surging Tempest: {rem:F1}s", $"{enemyCount} enemies")
                        .Alternatives("ST combo (misses AoE)")
                        .Tip("Keep Surging Tempest active.")
                        .Concept(WarConcepts.SurgingTempest)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(WarConcepts.SurgingTempest, true, "AoE refresh");
                });
            return;
        }

        // ST path: Storm's Eye
        if (level < WARActions.StormsEye.MinLevel) return;
        if (context.ComboStep != 2 || context.LastComboAction != WARActions.Maim.ActionId) return;
        if (!context.ActionService.IsActionReady(WARActions.StormsEye.ActionId)) return;

        var remST = context.SurgingTempestRemaining;
        scheduler.PushGcd(AresAbilities.StormsEye, targetId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.StormsEye.Name;
                context.Debug.DamageState = "Storm's Eye (refresh)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.StormsEye.ActionId, WARActions.StormsEye.Name)
                    .AsTankDamage()
                    .Reason($"Storm's Eye refresh ({remST:F1}s left).", "Refreshes Surging Tempest.")
                    .Factors($"Surging Tempest: {remST:F1}s", "After Maim")
                    .Alternatives("Storm's Path (more gauge)")
                    .Tip("Choose Storm's Eye when Surging Tempest is low.")
                    .Concept(WarConcepts.SurgingTempest)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.SurgingTempest, true, "ST refresh");
            });
    }

    #endregion

    #region Basic combo

    private void TryPushCombo(IAresContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        var level = context.Player.Level;
        var useAoE = context.Configuration.Tank.EnableAoEDamage &&
                     enemyCount >= context.Configuration.Tank.AoEMinTargets;

        if (useAoE)
        {
            TryPushAoeCombo(context, scheduler, targetId, enemyCount);
        }
        else
        {
            TryPushSingleTargetCombo(context, scheduler, targetId);
        }
    }

    private void TryPushSingleTargetCombo(IAresContext context, RotationScheduler scheduler, ulong targetId)
    {
        var level = context.Player.Level;

        // Step 3: finisher (Storm's Path if Surging Tempest healthy, else Storm's Eye)
        if (context.ComboStep == 2 && context.LastComboAction == WARActions.Maim.ActionId)
        {
            bool needsSurgingTempest = !context.HasSurgingTempest || context.SurgingTempestRemaining < 10f;
            var finisher = WARActions.GetComboFinisher(level, needsSurgingTempest);
            if (!context.ActionService.IsActionReady(finisher.ActionId)) return;

            var isStormsEye = finisher.ActionId == WARActions.StormsEye.ActionId;
            var behavior = isStormsEye ? AresAbilities.StormsEye : AresAbilities.StormsPath;
            var stempRem = context.SurgingTempestRemaining;

            scheduler.PushGcd(behavior, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = finisher.Name;
                    context.Debug.DamageState = $"{finisher.Name} (combo 3)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(finisher.ActionId, finisher.Name)
                        .AsTankDamage()
                        .Reason(isStormsEye
                            ? "Storm's Eye refresh Surging Tempest."
                            : "Storm's Path gauge generation.",
                            isStormsEye
                                ? "Storm's Eye - 10 gauge + Surging Tempest refresh."
                                : "Storm's Path - 20 gauge.")
                        .Factors("Combo 3", isStormsEye ? $"ST: {stempRem:F1}s (needs refresh)" : $"ST OK ({stempRem:F1}s)")
                        .Alternatives(isStormsEye ? "Storm's Path" : "Storm's Eye")
                        .Tip("Alternate based on Surging Tempest remaining.")
                        .Concept(WarConcepts.SurgingTempest)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(WarConcepts.SurgingTempest, true, "Combo finisher");
                });
            return;
        }

        // Step 2: Maim
        if (context.ComboStep == 1 &&
            context.LastComboAction == WARActions.HeavySwing.ActionId &&
            level >= WARActions.Maim.MinLevel)
        {
            if (!context.ActionService.IsActionReady(WARActions.Maim.ActionId)) return;
            scheduler.PushGcd(AresAbilities.Maim, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = WARActions.Maim.Name;
                    context.Debug.DamageState = "Maim (combo 2)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(WARActions.Maim.ActionId, WARActions.Maim.Name)
                        .AsTankDamage()
                        .Reason("Maim combo step 2.", "Grants 10 Beast Gauge.")
                        .Factors("Combo 2")
                        .Alternatives("Break combo (wastes progress)")
                        .Tip("Never break combo chain.")
                        .Concept(WarConcepts.BeastGauge)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(WarConcepts.BeastGauge, true, "Combo gauge");
                });
            return;
        }

        // Step 1 / restart: Heavy Swing
        if (!context.ActionService.IsActionReady(WARActions.HeavySwing.ActionId)) return;
        scheduler.PushGcd(AresAbilities.HeavySwing, targetId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.HeavySwing.Name;
                context.Debug.DamageState = "Heavy Swing (combo 1)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.HeavySwing.ActionId, WARActions.HeavySwing.Name)
                    .AsTankDamage()
                    .Reason("Heavy Swing starter.", "Starts ST combo.")
                    .Factors("Combo 1")
                    .Alternatives("Inner Chaos (Nascent Chaos only)", "Fell Cleave (50 gauge)")
                    .Tip("WAR combo flow: HS → Maim → finisher.")
                    .Concept(WarConcepts.BeastGauge)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.BeastGauge, true, "Combo start");
            });
    }

    private void TryPushAoeCombo(IAresContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        var level = context.Player.Level;

        // Step 2: Mythril Tempest
        if (context.ComboStep == 1 &&
            context.LastComboAction == WARActions.Overpower.ActionId &&
            level >= WARActions.MythrilTempest.MinLevel)
        {
            if (!context.ActionService.IsActionReady(WARActions.MythrilTempest.ActionId)) return;
            scheduler.PushGcd(AresAbilities.MythrilTempest, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = WARActions.MythrilTempest.Name;
                    context.Debug.DamageState = $"Mythril Tempest ({enemyCount}, combo 2)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(WARActions.MythrilTempest.ActionId, WARActions.MythrilTempest.Name)
                        .AsTankDamage()
                        .Reason($"Mythril Tempest AoE combo ({enemyCount}).", "20 Beast Gauge + Surging Tempest.")
                        .Factors($"{enemyCount} enemies", "Combo 2")
                        .Alternatives("ST combo")
                        .Tip("Complete AoE combo in pulls.")
                        .Concept(WarConcepts.BeastGauge)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(WarConcepts.BeastGauge, true, "AoE combo gauge");
                });
            return;
        }

        // Step 1: Overpower
        if (level < WARActions.Overpower.MinLevel)
        {
            // Fall through to ST
            TryPushSingleTargetCombo(context, scheduler, targetId);
            return;
        }

        if (!context.ActionService.IsActionReady(WARActions.Overpower.ActionId)) return;
        scheduler.PushGcd(AresAbilities.Overpower, targetId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.Overpower.Name;
                context.Debug.DamageState = $"Overpower ({enemyCount}, combo 1)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.Overpower.ActionId, WARActions.Overpower.Name)
                    .AsTankDamage()
                    .Reason($"Overpower AoE combo starter ({enemyCount}).", "AoE cone, sets up Mythril Tempest.")
                    .Factors($"{enemyCount} enemies", "Combo 1")
                    .Alternatives("Heavy Swing (single target)")
                    .Tip("With 3+ enemies, Overpower starts AoE chain.")
                    .Concept(WarConcepts.BeastGauge)
                    .Record();
                context.TrainingService?.RecordConceptApplication(WarConcepts.BeastGauge, true, "AoE combo start");
            });
    }

    #endregion
}
