using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThemisCore.Abilities;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.ThemisCore.Modules;

/// <summary>
/// Handles the Paladin DPS rotation.
/// Scheduler-driven: pushes candidates with priorities; scheduler evaluates gates and dispatches.
/// </summary>
public sealed class DamageModule : IThemisModule
{
    public int Priority => 30;
    public string Name => "Damage";

    public bool TryExecute(IThemisContext context, bool isMoving) => false;

    public void UpdateDebugState(IThemisContext context)
    {
        // Debug state updated during CollectCandidates
    }

    #region CollectCandidates (scheduler path)

    public void CollectCandidates(IThemisContext context, RotationScheduler scheduler, bool isMoving)
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
            PLDActions.FastBlade.ActionId,
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

        // Out-of-melee branch: push Intervene (oGCD gap close) + Shield Lob (ranged filler)
        if (target == null)
        {
            var gapCloseBlocked = context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(engageTarget, player);
            if (gapCloseBlocked)
                context.Debug.DamageState = $"Intervene blocked: {context.TargetingService.GapCloserSafety.LastBlockReason}";
            else
                TryPushIntervene(context, scheduler, engageTarget.GameObjectId);
            TryPushShieldLob(context, scheduler, engageTarget.GameObjectId);
            return;
        }

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);

        // oGCD pushes
        TryPushCircleOfScorn(context, scheduler, player.GameObjectId, target.GameObjectId);
        TryPushExpiacion(context, scheduler, target.GameObjectId);
        if (!context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, player))
            TryPushIntervene(context, scheduler, target.GameObjectId);

        // GCD pushes — magic phase and burst spenders fire over basic combo
        TryPushConfiteorChain(context, scheduler, target.GameObjectId);
        TryPushHolyPhaseSpenders(context, scheduler, target.GameObjectId, player.GameObjectId, enemyCount);
        TryPushAtonementChain(context, scheduler, target.GameObjectId);
        TryPushGoringBlade(context, scheduler, target.GameObjectId);
        TryPushBasicCombo(context, scheduler, target.GameObjectId, player.GameObjectId, enemyCount);
    }

    #endregion

    #region Out-of-melee

    private void TryPushIntervene(IThemisContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (!context.Configuration.Tank.EnableIntervene) return;
        if (context.Player.Level < PLDActions.Intervene.MinLevel) return;
        if (!context.ActionService.IsActionReady(PLDActions.Intervene.ActionId)) return;

        scheduler.PushOgcd(ThemisAbilities.Intervene, targetId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.Intervene.Name;
                context.Debug.DamageState = "Intervene (gap close)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.Intervene.ActionId, PLDActions.Intervene.Name)
                    .AsTankDamage()
                    .Reason(
                        "Intervene used as gap closer.",
                        "Intervene is a gap closer that also deals damage.")
                    .Factors("Target out of melee range", "Intervene charge available")
                    .Alternatives("Shield Lob (ranged GCD, slower)")
                    .Tip("Intervene has 2 charges. Don't hold both charges.")
                    .Concept(PldConcepts.Intervene)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.Intervene, wasSuccessful: true);
            });
    }

    private void TryPushShieldLob(IThemisContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (context.Player.Level < PLDActions.ShieldLob.MinLevel) return;
        if (!context.ActionService.IsActionReady(PLDActions.ShieldLob.ActionId)) return;

        scheduler.PushGcd(ThemisAbilities.ShieldLob, targetId, priority: 10,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.ShieldLob.Name;
                context.Debug.DamageState = "Shield Lob (ranged)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.ShieldLob.ActionId, PLDActions.ShieldLob.Name)
                    .AsTankDamage()
                    .Reason(
                        "Shield Lob used as ranged attack while out of melee.",
                        "Shield Lob is a 20y ranged GCD. Maintains uptime when you can't reach the target.")
                    .Factors("Target out of melee range", "Intervene not available")
                    .Alternatives("Intervene (faster gap close)", "Hold GCD (DPS loss)")
                    .Tip("Shield Lob is a filler GCD for ranged situations.")
                    .Concept(PldConcepts.BurstWindow)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.BurstWindow, wasSuccessful: true);
            });
    }

    #endregion

    #region oGCD Damage

    private void TryPushCircleOfScorn(IThemisContext context, RotationScheduler scheduler, ulong selfId, ulong targetId)
    {
        if (!context.Configuration.Tank.EnableCircleOfScorn) return;
        if (context.Player.Level < PLDActions.CircleOfScorn.MinLevel) return;
        if (!context.ActionService.IsActionReady(PLDActions.CircleOfScorn.ActionId)) return;

        scheduler.PushOgcd(ThemisAbilities.CircleOfScorn, selfId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.CircleOfScorn.Name;
                context.Debug.DamageState = "Circle of Scorn";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.CircleOfScorn.ActionId, PLDActions.CircleOfScorn.Name)
                    .AsTankDamage()
                    .Reason(
                        "Circle of Scorn used on cooldown for AoE DoT damage.",
                        "Circle of Scorn applies a DoT to all enemies in range.")
                    .Factors("Circle of Scorn off cooldown")
                    .Alternatives("Delay for alignment (usually not worth it)")
                    .Tip("Circle of Scorn is a 25s cooldown AoE DoT. Use on cooldown.")
                    .Concept(PldConcepts.CircleOfScorn)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.CircleOfScorn, wasSuccessful: true);
            });
    }

    private void TryPushExpiacion(IThemisContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (!context.Configuration.Tank.EnableSpiritsWithin) return;

        var level = context.Player.Level;
        var behavior = level >= PLDActions.Expiacion.MinLevel ? ThemisAbilities.Expiacion : ThemisAbilities.SpiritsWithin;
        var action = level >= PLDActions.Expiacion.MinLevel ? PLDActions.Expiacion : PLDActions.SpiritsWithin;

        if (level < action.MinLevel) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        var hasFof = context.HasFightOrFlight;

        scheduler.PushOgcd(behavior, targetId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = action.Name;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsTankDamage()
                    .Reason(
                        $"{action.Name} used on cooldown for single-target oGCD damage.",
                        $"{action.Name} is a high-potency single-target oGCD.")
                    .Factors($"{action.Name} off cooldown", hasFof ? "Inside Fight or Flight" : "Outside burst window")
                    .Alternatives("Delay for burst (minor benefit)")
                    .Tip($"Use {action.Name} on cooldown. Align with Fight or Flight when possible.")
                    .Concept(PldConcepts.Expiacion)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.Expiacion, wasSuccessful: true);
            });
    }

    #endregion

    #region Magic Phase (Confiteor chain + Holy Spirit/Circle)

    private void TryPushConfiteorChain(IThemisContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (!context.HasRequiescat) return;
        var level = context.Player.Level;
        if (level < PLDActions.Confiteor.MinLevel) return;
        if (context.ConfiteorStep == 0) return;

        AbilityBehavior behavior;
        ActionDefinition action;
        int step = context.ConfiteorStep;

        switch (step)
        {
            case 1: behavior = ThemisAbilities.Confiteor; action = PLDActions.Confiteor; break;
            case 2: behavior = ThemisAbilities.BladeOfFaith; action = PLDActions.BladeOfFaith; break;
            case 3: behavior = ThemisAbilities.BladeOfTruth; action = PLDActions.BladeOfTruth; break;
            case 4: behavior = ThemisAbilities.BladeOfValor; action = PLDActions.BladeOfValor; break;
            default: return;
        }

        if (level < action.MinLevel) return;

        scheduler.PushGcd(behavior, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"Confiteor chain ({step}/4)";
                var conceptId = step == 1 ? PldConcepts.Confiteor : PldConcepts.BladeCombo;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsTankBurst()
                    .Reason(
                        $"Confiteor chain step {step}/4 during Requiescat phase.",
                        "Confiteor → Blade of Faith → Truth → Valor is your highest-potency burst combo.")
                    .Factors("Requiescat active", $"Confiteor step: {step}/4")
                    .Alternatives("Holy Spirit instead (lower potency)")
                    .Tip("Always complete the full Confiteor combo during Requiescat.")
                    .Concept(conceptId)
                    .Record();
                context.TrainingService?.RecordConceptApplication(conceptId, wasSuccessful: true);
            });
    }

    private void TryPushHolyPhaseSpenders(IThemisContext context, RotationScheduler scheduler, ulong targetId, ulong selfId, int enemyCount)
    {
        if (!context.HasRequiescat) return;
        var level = context.Player.Level;
        var minAoE = context.Configuration.Tank.AoEMinTargets;

        // Holy Circle (AoE) if threshold met and L72+
        if (level >= PLDActions.HolyCircle.MinLevel &&
            enemyCount >= minAoE &&
            context.Configuration.Tank.EnableAoEDamage)
        {
            // Cast-time gate: instant when Requiescat stacks > 0 or Swiftcast
            var holyCircleCastTime = context.RequiescatStacks > 0 || context.HasSwiftcast
                ? 0f
                : PLDActions.HolyCircle.CastTime;
            if (!MechanicCastGate.ShouldBlock(context, holyCircleCastTime))
            {
                var stacks = context.RequiescatStacks;
                scheduler.PushGcd(ThemisAbilities.HolyCircle, selfId, priority: 3,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = PLDActions.HolyCircle.Name;
                        context.Debug.DamageState = $"Holy Circle ({enemyCount} targets)";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(PLDActions.HolyCircle.ActionId, PLDActions.HolyCircle.Name)
                            .AsTankBurst()
                            .Reason(
                                $"Holy Circle during Requiescat with {enemyCount} targets.",
                                "Holy Circle is free and instant during Requiescat. Outperforms Holy Spirit in AoE.")
                            .Factors($"Requiescat stacks: {stacks}", $"Enemy count: {enemyCount} (>= {minAoE})")
                            .Alternatives($"Holy Spirit ({enemyCount} targets, lower total potency)")
                            .Tip("During Requiescat in multi-target, use Holy Circle.")
                            .Concept(PldConcepts.MagicPhase)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(PldConcepts.MagicPhase, wasSuccessful: true);
                    });
                return;
            }
        }

        // Holy Spirit (single target) L64+
        if (level >= PLDActions.HolySpirit.MinLevel)
        {
            var holySpiritCastTime = context.RequiescatStacks > 0 || context.HasDivineMight || context.HasSwiftcast
                ? 0f
                : PLDActions.HolySpirit.CastTime;
            if (MechanicCastGate.ShouldBlock(context, holySpiritCastTime)) return;

            var stacks = context.RequiescatStacks;
            scheduler.PushGcd(ThemisAbilities.HolySpirit, targetId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PLDActions.HolySpirit.Name;
                    context.Debug.DamageState = $"Holy Spirit ({stacks} stacks)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(PLDActions.HolySpirit.ActionId, PLDActions.HolySpirit.Name)
                        .AsTankBurst()
                        .Reason(
                            $"Holy Spirit during Requiescat ({stacks} stacks remaining).",
                            "Holy Spirit is your primary single-target magic GCD during Requiescat.")
                        .Factors($"Requiescat stacks: {stacks}", $"Single target scenario ({enemyCount} enemies)")
                        .Alternatives("Holy Circle (better with 3+ targets)", "Confiteor chain (use when 0 stacks remain)")
                        .Tip("Spend all Requiescat stacks with Holy Spirit before the buff expires.")
                        .Concept(PldConcepts.HolySpirit)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(PldConcepts.HolySpirit, wasSuccessful: true);
                });
        }
    }

    #endregion

    #region Atonement Chain

    private void TryPushAtonementChain(IThemisContext context, RotationScheduler scheduler, ulong targetId)
    {
        var level = context.Player.Level;
        if (level < PLDActions.Atonement.MinLevel) return;

        if (!context.HasSwordOath && !context.HasFightOrFlight) return;

        AbilityBehavior behavior;
        ActionDefinition action;
        int step = context.AtonementStep;

        switch (step)
        {
            case 1: behavior = ThemisAbilities.Atonement; action = PLDActions.Atonement; break;
            case 2: behavior = ThemisAbilities.Supplication; action = PLDActions.Supplication; break;
            case 3: behavior = ThemisAbilities.Sepulchre; action = PLDActions.Sepulchre; break;
            default: behavior = ThemisAbilities.Atonement; action = PLDActions.Atonement; break;
        }

        var hasFof = context.HasFightOrFlight;
        var fofRem = context.FightOrFlightRemaining;
        var oathStacks = context.SwordOathStacks;

        scheduler.PushGcd(behavior, targetId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"Atonement chain ({step}/3)";
                if (hasFof)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsTankBurst()
                        .Reason(
                            $"Atonement chain during Fight or Flight ({fofRem:F1}s remaining)",
                            "Atonement → Supplication → Sepulchre is a high-potency chain unlocked by Royal Authority.")
                        .Factors($"Fight or Flight active ({fofRem:F1}s)", $"Sword Oath stacks: {oathStacks}", $"Chain position: {step}/3")
                        .Alternatives("Save stacks for later burst", "Use main combo instead (lower potency)")
                        .Tip("Always complete the Atonement chain during Fight or Flight.")
                        .Concept(PldConcepts.AtonementChain)
                        .Record();
                }
                else
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsTankDamage()
                        .Reason(
                            $"Atonement chain step {step}/3 - spending Sword Oath before expiry.",
                            "Sword Oath stacks last 30 seconds. Spend them before they expire.")
                        .Factors($"Sword Oath stacks: {oathStacks}", $"Chain position: {step}/3")
                        .Alternatives("Hold for Fight or Flight", "Use main combo (resets combo, wastes stacks)")
                        .Tip("Don't let Atonement stacks expire.")
                        .Concept(PldConcepts.AtonementChain)
                        .Record();
                }
                context.TrainingService?.RecordConceptApplication(PldConcepts.AtonementChain, wasSuccessful: true);
            });
    }

    #endregion

    #region Goring Blade / Blade of Honor

    private void TryPushGoringBlade(IThemisContext context, RotationScheduler scheduler, ulong targetId)
    {
        var level = context.Player.Level;
        if (level < PLDActions.GoringBlade.MinLevel) return;

        var shouldRefresh = context.GoringBladeRemaining < 5f ||
                           (context.HasFightOrFlight && context.GoringBladeRemaining < 10f);
        if (!shouldRefresh) return;

        // Lv.100: prefer Blade of Honor if available
        if (level >= PLDActions.BladeOfHonor.MinLevel && context.HasBladeOfHonor)
        {
            var remaining = context.GoringBladeRemaining;
            scheduler.PushGcd(ThemisAbilities.BladeOfHonor, targetId, priority: 2,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PLDActions.BladeOfHonor.Name;
                    context.Debug.DamageState = "Blade of Honor";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(PLDActions.BladeOfHonor.ActionId, PLDActions.BladeOfHonor.Name)
                        .AsTankBurst()
                        .Reason(
                            "Blade of Honor used as the Lv.100 DoT refresh and AoE finisher.",
                            "Blade of Honor is the enhanced Goring Blade at level 100.")
                        .Factors("Blade of Honor available", $"DoT remaining: {remaining:F1}s")
                        .Alternatives("Goring Blade (lower level version)")
                        .Tip("Blade of Honor upgrades Goring Blade at level 100.")
                        .Concept(PldConcepts.GoringBlade)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(PldConcepts.GoringBlade, wasSuccessful: true);
                });
            return;
        }

        var remainingG = context.GoringBladeRemaining;
        var hasFof = context.HasFightOrFlight;
        scheduler.PushGcd(ThemisAbilities.GoringBlade, targetId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.GoringBlade.Name;
                context.Debug.DamageState = $"Goring Blade (DoT {remainingG:F1}s)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.GoringBlade.ActionId, PLDActions.GoringBlade.Name)
                    .AsTankDamage()
                    .Reason(
                        $"Goring Blade applied to refresh DoT ({remainingG:F1}s remaining).",
                        "Goring Blade applies a DoT that must be refreshed before expiry.")
                    .Factors($"DoT remaining: {remainingG:F1}s", hasFof ? "Inside Fight or Flight" : "DoT falling off (<5s)")
                    .Alternatives("Delay until Fight or Flight (risk DoT falling off)")
                    .Tip("Keep Goring Blade up at all times.")
                    .Concept(PldConcepts.GoringBlade)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.GoringBlade, wasSuccessful: true);
            });
    }

    #endregion

    #region Basic Combo (ST / AoE)

    private void TryPushBasicCombo(IThemisContext context, RotationScheduler scheduler, ulong targetId, ulong selfId, int enemyCount)
    {
        var level = context.Player.Level;
        var minAoE = context.Configuration.Tank.AoEMinTargets;
        var useAoE = context.Configuration.Tank.EnableAoEDamage
                     && enemyCount >= minAoE
                     && level >= PLDActions.TotalEclipse.MinLevel;

        if (useAoE)
        {
            TryPushAoECombo(context, scheduler, selfId, enemyCount, minAoE);
        }
        else
        {
            TryPushSingleTargetCombo(context, scheduler, targetId);
        }
    }

    private void TryPushAoECombo(IThemisContext context, RotationScheduler scheduler, ulong selfId, int enemyCount, int minAoE)
    {
        var level = context.Player.Level;

        // Step 2: Prominence (requires LastComboAction == TotalEclipse)
        if (context.ComboStep == 2 &&
            context.LastComboAction == PLDActions.TotalEclipse.ActionId &&
            level >= PLDActions.Prominence.MinLevel)
        {
            scheduler.PushGcd(ThemisAbilities.Prominence, selfId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PLDActions.Prominence.Name;
                    context.Debug.DamageState = $"AoE 2/2 ({enemyCount} targets)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(PLDActions.Prominence.ActionId, PLDActions.Prominence.Name)
                        .AsTankDamage()
                        .Reason(
                            $"AoE combo finisher: Prominence ({enemyCount} targets).",
                            "Total Eclipse → Prominence is PLD's AoE combo.")
                        .Factors($"Enemy count: {enemyCount} (>= {minAoE})", "AoE combo step 2", "AoE damage enabled")
                        .Alternatives("Single target combo (better for 1-2 targets)")
                        .Tip("Switch to AoE rotation at 3+ targets.")
                        .Concept(PldConcepts.MagicPhase)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(PldConcepts.MagicPhase, wasSuccessful: true);
                });
            return;
        }

        // Step 1 / starter: Total Eclipse
        scheduler.PushGcd(ThemisAbilities.TotalEclipse, selfId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.TotalEclipse.Name;
                context.Debug.DamageState = $"AoE 1/2 ({enemyCount} targets)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.TotalEclipse.ActionId, PLDActions.TotalEclipse.Name)
                    .AsTankDamage()
                    .Reason(
                        $"AoE combo starter: Total Eclipse ({enemyCount} targets).",
                        "Total Eclipse opens the AoE combo.")
                    .Factors($"Enemy count: {enemyCount} (>= {minAoE})", "AoE combo step 1")
                    .Alternatives("Single target combo (better for 1-2 targets)")
                    .Tip("Total Eclipse → Prominence generates Oath Gauge via Prominence's combo bonus.")
                    .Concept(PldConcepts.MagicPhase)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.MagicPhase, wasSuccessful: true);
            });
    }

    private void TryPushSingleTargetCombo(IThemisContext context, RotationScheduler scheduler, ulong targetId)
    {
        var level = context.Player.Level;

        // Step 3: Royal Authority / Rage of Halone
        if (context.ComboStep == 3 && context.LastComboAction == PLDActions.RiotBlade.ActionId)
        {
            var finisher = PLDActions.GetComboFinisher(level);
            var behavior = level >= PLDActions.RoyalAuthority.MinLevel
                ? ThemisAbilities.RoyalAuthority
                : (level >= PLDActions.RageOfHalone.MinLevel ? ThemisAbilities.RageOfHalone : ThemisAbilities.RiotBlade);

            scheduler.PushGcd(behavior, targetId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = finisher.Name;
                    context.Debug.DamageState = "Combo 3/3";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(finisher.ActionId, finisher.Name)
                        .AsTankDamage()
                        .Reason(
                            "Main combo finisher — grants Sword Oath stacks.",
                            "Fast Blade → Riot Blade → Royal Authority is the foundation of PLD DPS.")
                        .Factors("Combo step 3", context.HasFightOrFlight ? "Inside FoF burst" : "Outside burst")
                        .Alternatives("Atonement chain (higher priority if stacks available)")
                        .Tip("Complete the 1-2-3 combo consistently.")
                        .Concept(PldConcepts.AtonementChain)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(PldConcepts.AtonementChain, wasSuccessful: true);
                });
            return;
        }

        // Step 2: Riot Blade
        if (context.ComboStep == 2 &&
            context.LastComboAction == PLDActions.FastBlade.ActionId &&
            level >= PLDActions.RiotBlade.MinLevel)
        {
            scheduler.PushGcd(ThemisAbilities.RiotBlade, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PLDActions.RiotBlade.Name;
                    context.Debug.DamageState = "Combo 2/3";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(PLDActions.RiotBlade.ActionId, PLDActions.RiotBlade.Name)
                        .AsTankDamage()
                        .Reason(
                            "Main combo step 2 — Riot Blade.",
                            "Riot Blade restores MP and continues the combo.")
                        .Factors("Combo step 2")
                        .Alternatives("Break combo (wastes progress)")
                        .Tip("Always continue the combo.")
                        .Concept(PldConcepts.BurstWindow)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(PldConcepts.BurstWindow, wasSuccessful: true);
                });
            return;
        }

        // Starter: Fast Blade
        scheduler.PushGcd(ThemisAbilities.FastBlade, targetId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.FastBlade.Name;
                context.Debug.DamageState = "Combo 1/3";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.FastBlade.ActionId, PLDActions.FastBlade.Name)
                    .AsTankDamage()
                    .Reason(
                        "Main combo starter — Fast Blade.",
                        "Fast Blade opens the 1-2-3 combo.")
                    .Factors("Combo start")
                    .Alternatives("Atonement chain if Sword Oath stacks available")
                    .Tip("Fast Blade is the foundation of PLD's ST rotation.")
                    .Concept(PldConcepts.BurstWindow)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PldConcepts.BurstWindow, wasSuccessful: true);
            });
    }

    #endregion
}
