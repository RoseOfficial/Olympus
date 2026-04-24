using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.HephaestusCore.Modules;

/// <summary>
/// Handles the Gunbreaker DPS rotation.
/// Manages Gnashing Fang combo, Continuation actions, cartridge spending, and basic combos.
/// </summary>
public sealed class DamageModule : IHephaestusModule
{
    public int Priority => 30; // Lower priority - damage comes after survival
    public string Name => "Damage";

    public bool TryExecute(IHephaestusContext context, bool isMoving) => false;

    public void UpdateDebugState(IHephaestusContext context)
    {
        // Debug state updated during TryExecute
    }

    #region CollectCandidates (scheduler path)

    public void CollectCandidates(IHephaestusContext context, RotationScheduler scheduler, bool isMoving)
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
            GNBActions.KeenEdge.ActionId,
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

        // Out-of-melee branch: push gap-closer + ranged filler then return
        if (target == null)
        {
            var gapCloseBlocked = context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(engageTarget, player);
            if (gapCloseBlocked)
                context.Debug.DamageState = $"Trajectory blocked: {context.TargetingService.GapCloserSafety.LastBlockReason}";
            else
                TryPushTrajectory(context, scheduler, engageTarget.GameObjectId, priority: 4);
            TryPushLightningShot(context, scheduler, engageTarget.GameObjectId);
            return;
        }

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);

        // oGCD pushes
        TryPushContinuations(context, scheduler, target.GameObjectId);
        TryPushBlastingZone(context, scheduler, target.GameObjectId);
        TryPushBowShock(context, scheduler);
        if (!context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, player))
            TryPushTrajectory(context, scheduler, target.GameObjectId, priority: 4);

        // GCD pushes
        TryPushGnashingFangCombo(context, scheduler, target.GameObjectId);
        TryPushReignOfBeastsCombo(context, scheduler, target.GameObjectId);
        TryPushDoubleDown(context, scheduler, target.GameObjectId, enemyCount);
        TryPushSonicBreak(context, scheduler, target.GameObjectId);

        var aoe = context.Configuration.Tank.EnableAoEDamage
                  && enemyCount >= context.Configuration.Tank.AoEMinTargets;
        if (!aoe)
            TryPushStartGnashingFang(context, scheduler, target.GameObjectId);

        TryPushCartridgeSpender(context, scheduler, target.GameObjectId, enemyCount);
        TryPushBasicCombo(context, scheduler, target.GameObjectId, enemyCount);
    }

    // Push all 5 continuation procs at priority 1; scheduler's ProcBuff gate picks the active one.
    private void TryPushContinuations(IHephaestusContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (!context.Configuration.Tank.EnableContinuation) return;
        if (context.Player.Level < GNBActions.Continuation.MinLevel) return;

        scheduler.PushOgcd(GnbAbilities.JugularRip, targetId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.JugularRip.Name;
                context.Debug.DamageState = "Jugular Rip!";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.JugularRip.ActionId, GNBActions.JugularRip.Name)
                    .AsTankDamage()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Jugular Rip - Continuation after Gnashing Fang",
                        "Jugular Rip is the first Continuation oGCD, triggered by Gnashing Fang's Ready to Rip proc. " +
                        "It must be used before the next GCD or the proc expires. " +
                        "Each step of the Gnashing Fang combo (GF -> Savage Claw -> Wicked Talon) enables its own Continuation oGCD.")
                    .Factors("Ready to Rip proc active", "Must weave before next GCD", "Part of Gnashing Fang combo chain")
                    .Alternatives("Skip (proc wasted - avoid at all costs)")
                    .Tip("Continuation oGCDs are free damage - always weave them immediately after each Gnashing Fang combo step.")
                    .Concept("gnb_continuation")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_continuation", true, "Jugular Rip weave");
            });

        scheduler.PushOgcd(GnbAbilities.AbdomenTear, targetId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.AbdomenTear.Name;
                context.Debug.DamageState = "Abdomen Tear!";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.AbdomenTear.ActionId, GNBActions.AbdomenTear.Name)
                    .AsTankDamage()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Abdomen Tear - Continuation after Savage Claw",
                        "Abdomen Tear is the second Continuation oGCD, triggered by Savage Claw's Ready to Tear proc. " +
                        "Must be woven between Savage Claw and Wicked Talon. " +
                        "These Continuation oGCDs are the primary damage amplifiers of the Gnashing Fang combo.")
                    .Factors("Ready to Tear proc active", "Must weave before next GCD", "Combo step 2 of 3")
                    .Alternatives("Skip (proc wasted - avoid at all costs)")
                    .Tip("Keep weaving Continuation oGCDs after each Gnashing Fang combo GCD - they are free, high-potency additions.")
                    .Concept("gnb_continuation")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_continuation", true, "Abdomen Tear weave");
            });

        scheduler.PushOgcd(GnbAbilities.EyeGouge, targetId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.EyeGouge.Name;
                context.Debug.DamageState = "Eye Gouge!";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.EyeGouge.ActionId, GNBActions.EyeGouge.Name)
                    .AsTankDamage()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Eye Gouge - Continuation after Wicked Talon",
                        "Eye Gouge is the third and final Continuation oGCD, triggered by Wicked Talon's Ready to Gouge proc. " +
                        "This completes the full Gnashing Fang combo chain: GF -> JR -> Savage Claw -> AT -> Wicked Talon -> EG. " +
                        "Missing any Continuation means significant DPS loss.")
                    .Factors("Ready to Gouge proc active", "Final step of Gnashing Fang combo", "Completes full combo chain")
                    .Alternatives("Skip (proc wasted - significant DPS loss)")
                    .Tip("The complete Gnashing Fang combo sequence generates 3 Continuation procs - each must be woven between combo GCDs.")
                    .Concept("gnb_continuation")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_continuation", true, "Eye Gouge weave");
            });

        if (context.Player.Level >= GNBActions.Hypervelocity.MinLevel)
        {
            scheduler.PushOgcd(GnbAbilities.Hypervelocity, targetId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = GNBActions.Hypervelocity.Name;
                    context.Debug.DamageState = "Hypervelocity!";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(GNBActions.Hypervelocity.ActionId, GNBActions.Hypervelocity.Name)
                        .AsTankDamage()
                        .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                        .Reason(
                            "Hypervelocity - Continuation after Burst Strike",
                            "Hypervelocity is the Continuation oGCD for Burst Strike (available from Lv.86). " +
                            "Burst Strike grants Ready to Blast, enabling this free oGCD follow-up. " +
                            "Always weave Hypervelocity immediately after Burst Strike.")
                        .Factors("Ready to Blast proc active (from Burst Strike)", "Available from Lv.86", "Free oGCD follow-up")
                        .Alternatives("Skip (proc wasted - avoid)")
                        .Tip("At Lv.86+, every Burst Strike is worth more because it generates a free Hypervelocity oGCD. Plan weave slots accordingly.")
                        .Concept("gnb_continuation")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_continuation", true, "Hypervelocity weave");
                });
        }

        if (context.Player.Level >= GNBActions.FatedBrand.MinLevel)
        {
            scheduler.PushOgcd(GnbAbilities.FatedBrand, targetId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = GNBActions.FatedBrand.Name;
                    context.Debug.DamageState = "Fated Brand!";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(GNBActions.FatedBrand.ActionId, GNBActions.FatedBrand.Name)
                        .AsTankDamage()
                        .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                        .Reason(
                            "Fated Brand - Continuation after Fated Circle",
                            "Fated Brand is the Continuation oGCD for Fated Circle (available from Lv.96). " +
                            "Fated Circle grants Ready to Brand, enabling this free AoE oGCD follow-up. " +
                            "Always weave Fated Brand immediately after Fated Circle.")
                        .Factors("Ready to Brand proc active (from Fated Circle)", "Available from Lv.96", "Free AoE oGCD follow-up")
                        .Alternatives("Skip (proc wasted - avoid)")
                        .Tip("At Lv.96+, Fated Circle generates a free Fated Brand oGCD -- always weave it for bonus AoE damage.")
                        .Concept("gnb_continuation")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_continuation", true, "Fated Brand weave");
                });
        }
    }

    private void TryPushBlastingZone(IHephaestusContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (context.Player.Level < GNBActions.DangerZone.MinLevel) return;

        var action = GNBActions.GetBlastingZoneAction(context.Player.Level);
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        scheduler.PushOgcd(GnbAbilities.BlastingZone, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = action.Name;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsTankDamage()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        $"Using {action.Name} on cooldown",
                        $"{action.Name} is a high-potency oGCD (the upgraded version of Danger Zone available from Lv.80). " +
                        "Use it on cooldown throughout the fight, preferring to align it inside No Mercy windows for the damage bonus. " +
                        "It shares a cooldown with Danger Zone at lower levels.")
                    .Factors("Blasting Zone / Danger Zone ready", context.HasNoMercy ? "No Mercy active for bonus damage" : "Used on cooldown", "High potency oGCD")
                    .Alternatives("Save for No Mercy window (minor optimization if cooldown aligns)")
                    .Tip("Blasting Zone is your highest-potency oGCD outside of Continuation - use it on cooldown and try to fit it inside No Mercy.")
                    .Concept("gnb_blasting_zone")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_blasting_zone", true, "Blasting Zone / Danger Zone used");
            });
    }

    private void TryPushBowShock(IHephaestusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableBowShock) return;
        if (context.Player.Level < GNBActions.BowShock.MinLevel) return;
        if (!context.ActionService.IsActionReady(GNBActions.BowShock.ActionId)) return;

        // Bow Shock is self-targeted (AoE centered on player)
        scheduler.PushOgcd(GnbAbilities.BowShock, context.Player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.BowShock.Name;
                context.Debug.DamageState = "Bow Shock";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.BowShock.ActionId, GNBActions.BowShock.Name)
                    .AsTankDamage()
                    .Reason(
                        "Bow Shock on cooldown",
                        "Bow Shock is an AoE DoT oGCD that applies a damage-over-time to all nearby enemies. " +
                        "Use on cooldown during combat - it hits all enemies around you, making it especially valuable in multi-target situations. " +
                        "Best used inside No Mercy for the damage bonus.")
                    .Factors("Bow Shock ready", context.HasNoMercy ? "No Mercy active" : "Used on cooldown", "AoE DoT application")
                    .Alternatives("Save for No Mercy window (minor gain if cooldown aligns)")
                    .Tip("Bow Shock is an AoE ability - it shines in dungeon pulls. Always use it on cooldown and inside No Mercy when possible.")
                    .Concept("gnb_bow_shock")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_bow_shock", true, "Bow Shock applied");
            });
    }

    private void TryPushTrajectory(IHephaestusContext context, RotationScheduler scheduler, ulong targetId, int priority)
    {
        if (!context.Configuration.Tank.EnableTrajectory) return;
        if (context.Player.Level < GNBActions.Trajectory.MinLevel) return;
        if (!context.ActionService.IsActionReady(GNBActions.Trajectory.ActionId)) return;

        scheduler.PushOgcd(GnbAbilities.Trajectory, targetId, priority: priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.Trajectory.Name;
                context.Debug.DamageState = "Trajectory";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.Trajectory.ActionId, GNBActions.Trajectory.Name)
                    .AsTankDamage()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Trajectory gap closer / oGCD filler",
                        "Trajectory is a gap-closing oGCD with 2 charges that also deals damage. " +
                        "Use it to close distance to enemies or as an oGCD filler during the rotation. " +
                        "Keeping at least 1 charge for mobility is generally advisable.")
                    .Factors("Trajectory charge available", "Used as gap closer or oGCD filler", "2 charges allow flexible use")
                    .Alternatives("Save both charges for emergency mobility")
                    .Tip("Trajectory has 2 charges - use one for damage during rotation and keep the other for repositioning during mechanics.")
                    .Concept("gnb_trajectory")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_trajectory", true, "Trajectory used");
            });
    }

    private void TryPushLightningShot(IHephaestusContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (context.Player.Level < GNBActions.LightningShot.MinLevel) return;
        if (!context.ActionService.IsActionReady(GNBActions.LightningShot.ActionId)) return;

        scheduler.PushGcd(GnbAbilities.LightningShot, targetId, priority: 10,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.LightningShot.Name;
                context.Debug.DamageState = "Lightning Shot (ranged)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.LightningShot.ActionId, GNBActions.LightningShot.Name)
                    .AsTankDamage()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Lightning Shot - ranged attack while out of melee range",
                        "Lightning Shot is GNB's ranged attack GCD, used when the enemy is out of melee range. " +
                        "It keeps the GCD rolling and deals damage while you close the gap. " +
                        "Avoid prolonged use since melee abilities are much higher potency.")
                    .Factors("Target out of melee range", "Keeps GCD rolling", "Ranged fallback")
                    .Alternatives("Use Trajectory to close gap and return to melee (higher total damage)")
                    .Tip("Lightning Shot is a fallback for when you can't reach the enemy. Use Trajectory to quickly close the gap and resume melee rotation.")
                    .Concept("gnb_blasting_zone")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_blasting_zone", true, "Ranged GCD used");
            });
    }

    // Push Savage Claw (step 1) and Wicked Talon (step 2) at priority 1;
    // scheduler's ComboStep gate selects the correct one based on AmmoComboStep.
    private void TryPushGnashingFangCombo(IHephaestusContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (context.Player.Level < GNBActions.GnashingFang.MinLevel) return;

        scheduler.PushGcd(GnbAbilities.SavageClaw, targetId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.SavageClaw.Name;
                context.Debug.DamageState = "Savage Claw (combo 2/3)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.SavageClaw.ActionId, GNBActions.SavageClaw.Name)
                    .AsTankDamage()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Savage Claw - Gnashing Fang combo step 2",
                        "Savage Claw is the second step of the Gnashing Fang combo (combo 2/3). " +
                        "It grants Ready to Tear, enabling the Abdomen Tear Continuation oGCD. " +
                        "Always weave Abdomen Tear between this GCD and Wicked Talon.")
                    .Factors("In Gnashing Fang combo (step 1 complete)", "Grants Ready to Tear proc", "Weave Abdomen Tear after this")
                    .Alternatives("Break combo (wasted cartridge - never do this)")
                    .Tip("After pressing Savage Claw, immediately weave Abdomen Tear in the oGCD window before the next GCD.")
                    .Concept("gnb_gnashing_fang")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_gnashing_fang", true, "Savage Claw combo step");
            });

        scheduler.PushGcd(GnbAbilities.WickedTalon, targetId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.WickedTalon.Name;
                context.Debug.DamageState = "Wicked Talon (combo 3/3)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.WickedTalon.ActionId, GNBActions.WickedTalon.Name)
                    .AsTankDamage()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Wicked Talon - Gnashing Fang combo step 3 (finisher)",
                        "Wicked Talon is the third and final step of the Gnashing Fang combo. " +
                        "It grants Ready to Gouge, enabling the Eye Gouge Continuation oGCD. " +
                        "This completes the combo - weave Eye Gouge after this GCD.")
                    .Factors("In Gnashing Fang combo (step 2 complete)", "Grants Ready to Gouge proc", "Finishes combo chain")
                    .Alternatives("Break combo (never viable)")
                    .Tip("Wicked Talon finishes the Gnashing Fang combo. Follow it with Eye Gouge to complete the full 6-action sequence.")
                    .Concept("gnb_gnashing_fang")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_gnashing_fang", true, "Wicked Talon combo finisher");
            });
    }

    // Push Noble Blood (step 3) and Lion Heart (step 4) at priority 2;
    // scheduler's ComboStep gate selects based on AmmoComboStep.
    // Execution uses ReplacementBaseId=ReignOfBeasts so UseAction receives the base ID.
    private void TryPushReignOfBeastsCombo(IHephaestusContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (context.Player.Level < GNBActions.ReignOfBeasts.MinLevel) return;

        // Step 1: start the chain when ReadyToReign is active (ProcBuff gate in scheduler)
        scheduler.PushGcd(GnbAbilities.ReignOfBeasts, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.ReignOfBeasts.Name;
                context.Debug.DamageState = "Reign of Beasts (combo 1/3)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.ReignOfBeasts.ActionId, GNBActions.ReignOfBeasts.Name)
                    .AsTankBurst()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Reign of Beasts combo (Lv.100)",
                        "Reign of Beasts is the first step of GNB's Lv.100 combo (Reign of Beasts -> Noble Blood -> Lion Heart). " +
                        "It is triggered by the Ready to Reign proc granted by Bloodfest. " +
                        "This combo is available every other No Mercy window when Bloodfest aligns.")
                    .Factors("Ready to Reign proc active (from Bloodfest)", "Lv.100 exclusive combo", "High potency burst sequence")
                    .Alternatives("Delay (risk losing proc)")
                    .Tip("The Reign of Beasts combo is a 3-step sequence -- always complete all 3 hits for maximum damage.")
                    .Concept("gnb_reign_of_beasts")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_reign_of_beasts", true, "Reign of Beasts combo started");
            });

        // Steps 2-3: Noble Blood and Lion Heart tracked via AmmoComboStep 3/4
        scheduler.PushGcd(GnbAbilities.NobleBlood, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.NobleBlood.Name;
                context.Debug.DamageState = "Noble Blood (combo 2/3)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.NobleBlood.ActionId, GNBActions.NobleBlood.Name)
                    .AsTankBurst()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Noble Blood - Reign combo step 2",
                        "Noble Blood is the second step of the Reign of Beasts combo. " +
                        "Always continue the combo to completion -- breaking it wastes the burst sequence.")
                    .Factors("In Reign of Beasts combo (step 1 complete)", "Combo step 2 of 3")
                    .Alternatives("Break combo (never viable - wastes burst)")
                    .Tip("Continue the combo to Lion Heart for maximum burst damage.")
                    .Concept("gnb_reign_of_beasts")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_reign_of_beasts", true, "Noble Blood combo step");
            });

        scheduler.PushGcd(GnbAbilities.LionHeart, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.LionHeart.Name;
                context.Debug.DamageState = "Lion Heart (combo 3/3)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.LionHeart.ActionId, GNBActions.LionHeart.Name)
                    .AsTankBurst()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Lion Heart - Reign combo finisher",
                        "Lion Heart is the third and final step of the Reign of Beasts combo. " +
                        "This completes the full burst sequence with the highest potency hit (1200).")
                    .Factors("In Reign of Beasts combo (step 2 complete)", "Final combo step", "Highest potency of the 3 hits")
                    .Alternatives("Break combo (never viable)")
                    .Tip("Lion Heart finishes the Reign combo -- the full sequence is one of GNB's highest burst windows.")
                    .Concept("gnb_reign_of_beasts")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_reign_of_beasts", true, "Lion Heart combo finisher");
            });
    }

    private void TryPushDoubleDown(IHephaestusContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        if (context.Player.Level < GNBActions.DoubleDown.MinLevel) return;
        if (!context.CanUseDoubleDown) return;
        if (!context.HasNoMercy) return;
        if (!context.ActionService.IsActionReady(GNBActions.DoubleDown.ActionId)) return;

        scheduler.PushGcd(GnbAbilities.DoubleDown, targetId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.DoubleDown.Name;
                context.Debug.DamageState = enemyCount > 1 ? $"Double Down ({enemyCount} enemies)" : "Double Down";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.DoubleDown.ActionId, GNBActions.DoubleDown.Name)
                    .AsTankBurst()
                    .Target("Enemy")
                    .Reason(
                        $"Double Down during No Mercy ({context.Cartridges} cartridges)",
                        "Double Down is GNB's highest potency GCD, costing 2 cartridges. " +
                        "It's a massive AoE attack that should ONLY be used during No Mercy window. " +
                        "At 1200 potency (base), it's more efficient than 2 Burst Strikes.")
                    .Factors(
                        "No Mercy active for +20% damage",
                        $"Have {context.Cartridges} cartridges (cost: 2)",
                        enemyCount > 1 ? $"{enemyCount} enemies for AoE value" : "Single target but highest potency")
                    .Alternatives("Use 2 Burst Strikes instead (lower total potency)", "Wait for more targets (might lose No Mercy window)")
                    .Tip("Double Down is your biggest hit - never use it outside No Mercy. Plan cartridge usage to have 2+ when No Mercy comes up.")
                    .Concept("gnb_double_down")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_double_down", true, "Burst window priority");
            });
    }

    private void TryPushSonicBreak(IHephaestusContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (context.Player.Level < GNBActions.SonicBreak.MinLevel) return;
        if (!context.HasNoMercy) return;
        if (context.HasSonicBreakDot) return;
        if (!context.ActionService.IsActionReady(GNBActions.SonicBreak.ActionId)) return;

        scheduler.PushGcd(GnbAbilities.SonicBreak, targetId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.SonicBreak.Name;
                context.Debug.DamageState = "Sonic Break (DoT)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.SonicBreak.ActionId, GNBActions.SonicBreak.Name)
                    .AsTankBurst()
                    .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                    .Reason(
                        "Sonic Break during No Mercy window",
                        "Sonic Break is a high-potency GCD that applies a DoT to the target (60s cooldown). " +
                        "It must be used during No Mercy to benefit from the +20% damage bonus throughout its DoT duration. " +
                        "Never use Sonic Break outside No Mercy as the DoT ticks would not benefit from the buff.")
                    .Factors("No Mercy active", "Sonic Break DoT not already running", "60s cooldown aligns with No Mercy")
                    .Alternatives("Skip this window (DoT would wear before cooldown resets)")
                    .Tip("Apply Sonic Break early in the No Mercy window so all DoT ticks benefit from the +20% bonus.")
                    .Concept("gnb_sonic_break")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_sonic_break", true, "Sonic Break during No Mercy");
            });
    }

    private void TryPushStartGnashingFang(IHephaestusContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (context.Player.Level < GNBActions.GnashingFang.MinLevel) return;
        if (context.Cartridges < 1) return;

        // Hold briefly when No Mercy is imminent to align the combo under the buff
        if (!context.HasNoMercy && !context.HasMaxCartridges)
        {
            var nmCd = context.ActionService.GetCooldownRemaining(GNBActions.NoMercy.ActionId);
            if (nmCd > 0 && nmCd < 8f) return;
        }

        if (!context.ActionService.IsActionReady(GNBActions.GnashingFang.ActionId)) return;

        scheduler.PushGcd(GnbAbilities.GnashingFang, targetId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.GnashingFang.Name;
                context.Debug.DamageState = "Gnashing Fang (combo 1/3)";
                var duringNoMercy = context.HasNoMercy;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.GnashingFang.ActionId, GNBActions.GnashingFang.Name)
                    .AsTankResource(context.Cartridges)
                    .Reason(
                        duringNoMercy
                            ? "Starting Gnashing Fang combo during No Mercy"
                            : "Starting Gnashing Fang combo (cartridges capped)",
                        "Gnashing Fang is GNB's signature 3-hit combo (GF -> Savage Claw -> Wicked Talon), each hit enabling a Continuation oGCD. " +
                        "Costs 1 cartridge but delivers high potency. Ideally used during No Mercy window for maximum damage.")
                    .Factors(
                        duringNoMercy ? "No Mercy active for +20% damage" : "Cartridges capped, must spend",
                        $"Have {context.Cartridges} cartridge(s)",
                        "Combo unlocks 3 Continuation oGCDs")
                    .Alternatives("Use Burst Strike instead (lower potency)", "Wait for No Mercy (risk overcapping)")
                    .Tip("Gnashing Fang is your highest priority cartridge spender - always use it during No Mercy if available.")
                    .Concept("gnb_gnashing_fang")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_gnashing_fang", true, duringNoMercy ? "Burst window combo" : "Cartridge management");
            });
    }

    private void TryPushCartridgeSpender(IHephaestusContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        if (context.Cartridges < GNBActions.BurstStrikeCost) return;

        var aboutToOvercapSt = context.ComboStep == 2 && context.Cartridges >= 2;
        var aboutToOvercapAoe = context.ComboStep == 1 &&
                                context.LastComboAction == GNBActions.DemonSlice.ActionId &&
                                context.Cartridges >= 2;
        var shouldSpend = context.HasMaxCartridges ||
                          context.HasNoMercy ||
                          aboutToOvercapSt ||
                          aboutToOvercapAoe;
        if (!shouldSpend) return;

        var aoe = context.Configuration.Tank.EnableAoEDamage
                  && enemyCount >= context.Configuration.Tank.AoEMinTargets
                  && context.Player.Level >= GNBActions.FatedCircle.MinLevel;

        if (aoe)
        {
            if (!context.ActionService.IsActionReady(GNBActions.FatedCircle.ActionId)) return;
            scheduler.PushGcd(GnbAbilities.FatedCircle, targetId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = GNBActions.FatedCircle.Name;
                    context.Debug.DamageState = $"Fated Circle ({enemyCount} enemies)";
                    var duringNoMercyAoE = context.HasNoMercy;
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(GNBActions.FatedCircle.ActionId, GNBActions.FatedCircle.Name)
                        .AsTankResource(context.Cartridges)
                        .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                        .Reason(
                            $"Fated Circle AoE ({enemyCount} enemies)",
                            "Fated Circle is GNB's AoE cartridge spender, replacing Burst Strike when 3+ enemies are present. " +
                            "At 3+ targets it deals more total damage than Burst Strike. " +
                            "Like Burst Strike, it grants a Continuation proc (Fated Brand) at Lv.86+.")
                        .Factors(
                            $"{enemyCount} enemies in range (AoE threshold: 3)",
                            duringNoMercyAoE ? "No Mercy active" : context.HasMaxCartridges ? "Cartridges capped" : "Spending to avoid overcap",
                            $"Have {context.Cartridges} cartridge(s)")
                        .Alternatives("Use Burst Strike (only better at 1-2 targets)")
                        .Tip("Use Fated Circle over Burst Strike whenever 3 or more enemies are present - the AoE potency makes it superior.")
                        .Concept("gnb_burst_strike")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_cartridge_gauge", true, "Fated Circle AoE spend");
                });
        }
        else
        {
            if (context.Player.Level < GNBActions.BurstStrike.MinLevel) return;
            if (!context.ActionService.IsActionReady(GNBActions.BurstStrike.ActionId)) return;
            scheduler.PushGcd(GnbAbilities.BurstStrike, targetId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = GNBActions.BurstStrike.Name;
                    context.Debug.DamageState = $"Burst Strike ({context.Cartridges} cartridges)";
                    var duringNoMercy = context.HasNoMercy;
                    var capped = context.HasMaxCartridges;
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(GNBActions.BurstStrike.ActionId, GNBActions.BurstStrike.Name)
                        .AsTankResource(context.Cartridges)
                        .Reason(
                            duringNoMercy
                                ? "Burst Strike during No Mercy"
                                : capped ? "Burst Strike (avoiding overcap)" : "Burst Strike (combo finisher coming)",
                            "Burst Strike is GNB's single-target cartridge spender (1 cartridge). " +
                            "At Lv.86+, it grants Ready to Blast for Hypervelocity follow-up. " +
                            "Use during No Mercy or when capped to avoid wasting cartridges from combo finisher.")
                        .Factors(
                            duringNoMercy ? "No Mercy active" : (capped ? "Cartridges capped at 3" : "Combo finisher would overcap"),
                            $"Have {context.Cartridges} cartridge(s)",
                            context.Player.Level >= 86 ? "Grants Hypervelocity oGCD" : "Below Lv.86 - no Hypervelocity")
                        .Alternatives("Use Gnashing Fang instead (if available)", "Save for Double Down (if 2+ cartridges and No Mercy soon)")
                        .Tip("Gnashing Fang is higher priority than Burst Strike when available. Use Burst Strike to prevent overcapping.")
                        .Concept("gnb_burst_strike")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_cartridge_gauge", true, "Cartridge management");
                });
        }
    }

    private void TryPushBasicCombo(IHephaestusContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        var level = context.Player.Level;

        // AoE combo path
        if (context.Configuration.Tank.EnableAoEDamage &&
            enemyCount >= context.Configuration.Tank.AoEMinTargets &&
            level >= GNBActions.DemonSlice.MinLevel)
        {
            TryPushAoECombo(context, scheduler, targetId);
            return;
        }

        TryPushSingleTargetCombo(context, scheduler, targetId);
    }

    private void TryPushSingleTargetCombo(IHephaestusContext context, RotationScheduler scheduler, ulong targetId)
    {
        var level = context.Player.Level;

        // Cartridge overcap prevention: push Burst Strike at priority 6.5 (same queue position via
        // the priority int; we use the basic combo finisher check here). If Solid Barrel would
        // overcap cartridges, push BurstStrike at priority higher than Solid Barrel (priority 6)
        // before basic combo runs at priority 7. This is handled by TryPushCartridgeSpender
        // (aboutToOvercapSt path) which fires BurstStrike at priority 6. Basic combo continues here.

        if (context.ComboStep == 2 && level >= GNBActions.SolidBarrel.MinLevel &&
            context.ActionService.IsActionReady(GNBActions.SolidBarrel.ActionId))
        {
            scheduler.PushGcd(GnbAbilities.SolidBarrel, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = GNBActions.SolidBarrel.Name;
                    context.Debug.DamageState = "Solid Barrel (+1 cart)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(GNBActions.SolidBarrel.ActionId, GNBActions.SolidBarrel.Name)
                        .AsTankResource(context.Cartridges)
                        .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                        .Reason(
                            "Solid Barrel - combo finisher (+1 cartridge)",
                            "Solid Barrel is the finisher of GNB's basic single-target combo (Keen Edge -> Brutal Shell -> Solid Barrel). " +
                            "It grants +1 cartridge on hit, which is the primary way to generate ammunition between Bloodfest uses. " +
                            "Completing this combo is essential for maintaining a steady cartridge supply.")
                        .Factors("Combo step 2 complete", "Grants +1 cartridge on hit", $"Current cartridges: {context.Cartridges}")
                        .Alternatives("Breaking combo (wastes the cartridge generation)")
                        .Tip("Complete the full combo (Keen Edge -> Brutal Shell -> Solid Barrel) to generate cartridges. Aim to have 2+ cartridges when No Mercy comes up.")
                        .Concept("gnb_cartridge_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_cartridge_gauge", true, "Solid Barrel cartridge generation");
                });
            return;
        }

        // ComboStep == 1 is ambiguous — it is set by both Keen Edge (ST) and Demon Slice (AoE).
        // The LastComboAction guard prevents firing Brutal Shell after Demon Slice (which would
        // break the AoE combo and deal single-target damage instead).
        if (context.ComboStep == 1 &&
            context.LastComboAction == GNBActions.KeenEdge.ActionId &&
            level >= GNBActions.BrutalShell.MinLevel &&
            context.ActionService.IsActionReady(GNBActions.BrutalShell.ActionId))
        {
            scheduler.PushGcd(GnbAbilities.BrutalShell, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = GNBActions.BrutalShell.Name;
                    context.Debug.DamageState = "Brutal Shell (combo)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(GNBActions.BrutalShell.ActionId, GNBActions.BrutalShell.Name)
                        .AsCombo(2)
                        .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                        .Reason(
                            "Brutal Shell - combo step 2",
                            "Brutal Shell is the middle step of GNB's basic combo (Keen Edge -> Brutal Shell -> Solid Barrel). " +
                            "It also provides a small self-shield when it hits. " +
                            "Continue the combo to reach Solid Barrel and generate a cartridge.")
                        .Factors("Combo step 1 complete", "Small self-shield on hit", "Required for Solid Barrel follow-up")
                        .Alternatives("Break combo (wastes progress)")
                        .Tip("Always continue the combo to Solid Barrel - each completed set generates 1 cartridge for your burst windows.")
                        .Concept("gnb_cartridge_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_cartridge_gauge", true, "Brutal Shell combo step");
                });
            return;
        }

        // Starter: Keen Edge (always available)
        if (context.ActionService.IsActionReady(GNBActions.KeenEdge.ActionId))
        {
            scheduler.PushGcd(GnbAbilities.KeenEdge, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = GNBActions.KeenEdge.Name;
                    context.Debug.DamageState = "Keen Edge (start)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(GNBActions.KeenEdge.ActionId, GNBActions.KeenEdge.Name)
                        .AsCombo(1)
                        .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                        .Reason(
                            "Keen Edge - basic combo starter",
                            "Keen Edge is the opener of GNB's basic single-target combo. " +
                            "Use it whenever higher-priority actions (Gnashing Fang, Burst Strike, etc.) are unavailable. " +
                            "The full combo (Keen Edge -> Brutal Shell -> Solid Barrel) generates 1 cartridge.")
                        .Factors("No higher-priority GCDs available", "Starts basic combo chain", "Leads to cartridge generation")
                        .Alternatives("Use Gnashing Fang (if cartridges available)", "Use Burst Strike (if capped)")
                        .Tip("The basic combo fills GCDs between cartridge spenders. Keep the combo chain going to maintain steady cartridge income.")
                        .Concept("gnb_cartridge_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_cartridge_gauge", true, "Keen Edge combo start");
                });
        }
    }

    private void TryPushAoECombo(IHephaestusContext context, RotationScheduler scheduler, ulong targetId)
    {
        var level = context.Player.Level;

        // ComboStep == 1 is ambiguous — it is set by both Keen Edge (ST) and Demon Slice (AoE).
        // The LastComboAction guard prevents firing Demon Slaughter after Keen Edge (which would
        // break the ST combo and deal AoE damage on a single target at half potency).
        if (context.ComboStep == 1 &&
            context.LastComboAction == GNBActions.DemonSlice.ActionId &&
            level >= GNBActions.DemonSlaughter.MinLevel &&
            context.ActionService.IsActionReady(GNBActions.DemonSlaughter.ActionId))
        {
            scheduler.PushGcd(GnbAbilities.DemonSlaughter, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = GNBActions.DemonSlaughter.Name;
                    context.Debug.DamageState = "Demon Slaughter (+1 cart)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(GNBActions.DemonSlaughter.ActionId, GNBActions.DemonSlaughter.Name)
                        .AsTankResource(context.Cartridges)
                        .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                        .Reason(
                            "Demon Slaughter - AoE combo finisher (+1 cartridge)",
                            "Demon Slaughter is the finisher of GNB's AoE combo (Demon Slice -> Demon Slaughter). " +
                            "It hits all nearby enemies and grants +1 cartridge, just like Solid Barrel does in the single-target combo. " +
                            "Use this AoE combo when 3+ enemies are present.")
                        .Factors("AoE combo step 1 complete", "Grants +1 cartridge", $"Current cartridges: {context.Cartridges}", "3+ enemies in range")
                        .Alternatives("Single-target combo (only better with 1-2 enemies)")
                        .Tip("The AoE combo (Demon Slice -> Demon Slaughter) generates cartridges just as efficiently as the single-target combo in multi-enemy situations.")
                        .Concept("gnb_cartridge_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_cartridge_gauge", true, "Demon Slaughter AoE cartridge gen");
                });
            return;
        }

        if (context.ActionService.IsActionReady(GNBActions.DemonSlice.ActionId))
        {
            scheduler.PushGcd(GnbAbilities.DemonSlice, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = GNBActions.DemonSlice.Name;
                    context.Debug.DamageState = "Demon Slice (AoE start)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(GNBActions.DemonSlice.ActionId, GNBActions.DemonSlice.Name)
                        .AsCombo(1)
                        .Target(context.CurrentTarget?.Name.TextValue ?? "Enemy")
                        .Reason(
                            "Demon Slice - AoE combo starter",
                            "Demon Slice is the opener of GNB's AoE combo (Demon Slice -> Demon Slaughter). " +
                            "Use it when 3+ enemies are present to maximize AoE damage and generate cartridges efficiently. " +
                            "Follow with Demon Slaughter for the full combo and cartridge generation.")
                        .Factors("3+ enemies in range", "AoE combo starter", "Leads to Demon Slaughter and cartridge gain")
                        .Alternatives("Keen Edge (only better at 1-2 targets)")
                        .Tip("Switch between the ST combo (Keen Edge line) and AoE combo (Demon Slice line) based on enemy count. The threshold is 3 enemies.")
                        .Concept("gnb_cartridge_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_cartridge_gauge", true, "Demon Slice AoE combo start");
                });
        }
    }

    #endregion
}
