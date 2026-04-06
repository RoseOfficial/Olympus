using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.ThemisCore.Modules;

/// <summary>
/// Handles the Paladin DPS rotation.
/// Manages combo chains, DoT maintenance, and burst windows.
/// </summary>
public sealed class DamageModule : IThemisModule
{
    public int Priority => 30; // Lower priority than mitigation
    public string Name => "Damage";

    public bool TryExecute(IThemisContext context, bool isMoving)
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

        // Two-pass target search: melee range first, wide range as fallback for gap closers
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
            return false;
        }

        // Out of melee range: try Intervene (oGCD gap close) or Shield Lob (ranged GCD to open weave window)
        if (target == null && engageTarget != null)
        {
            if (context.Configuration.Tank.EnableIntervene &&
                context.CanExecuteOgcd && player.Level >= PLDActions.Intervene.MinLevel)
            {
                if (context.ActionService.IsActionReady(PLDActions.Intervene.ActionId))
                {
                    if (context.ActionService.ExecuteOgcd(PLDActions.Intervene, engageTarget.GameObjectId))
                    {
                        context.Debug.PlannedAction = PLDActions.Intervene.Name;
                        context.Debug.DamageState = "Intervene (gap close)";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(PLDActions.Intervene.ActionId, PLDActions.Intervene.Name)
                            .AsTankDamage()
                            .Target(engageTarget.Name?.TextValue)
                            .Reason(
                                "Intervene used as gap closer to reach melee range.",
                                "Intervene is a gap closer that also deals damage. Use it to quickly close distance to a target that has moved out of melee range.")
                            .Factors("Target out of melee range", "Intervene charge available", $"Target: {engageTarget.Name?.TextValue}")
                            .Alternatives("Shield Lob (ranged GCD, slower)", "Wait for target to move closer (DPS loss)")
                            .Tip("Intervene has 2 charges. Don't hold both charges - use freely for gap closing and weave windows.")
                            .Concept(PldConcepts.Intervene)
                            .Record();

                        context.TrainingService?.RecordConceptApplication(PldConcepts.Intervene, wasSuccessful: true);

                        return true;
                    }
                }
            }

            if (context.CanExecuteGcd && player.Level >= PLDActions.ShieldLob.MinLevel)
            {
                if (context.ActionService.IsActionReady(PLDActions.ShieldLob.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(PLDActions.ShieldLob, engageTarget.GameObjectId))
                    {
                        context.Debug.PlannedAction = PLDActions.ShieldLob.Name;
                        context.Debug.DamageState = "Shield Lob (ranged)";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(PLDActions.ShieldLob.ActionId, PLDActions.ShieldLob.Name)
                            .AsTankDamage()
                            .Target(engageTarget.Name?.TextValue)
                            .Reason(
                                "Shield Lob used as ranged attack while out of melee range.",
                                "Shield Lob is a 20y ranged GCD. Use it to maintain uptime and open a weave window when you can't reach the target in melee.")
                            .Factors("Target out of melee range", "Intervene not available", $"Target: {engageTarget.Name?.TextValue}")
                            .Alternatives("Intervene (faster gap close, but on CD)", "Hold GCD and close manually (DPS loss)")
                            .Tip("Shield Lob is a filler GCD for ranged situations. It keeps your GCD rolling and lets you weave oGCDs while repositioning.")
                            .Concept(PldConcepts.BurstWindow)
                            .Record();

                        context.TrainingService?.RecordConceptApplication(PldConcepts.BurstWindow, wasSuccessful: true);

                        return true;
                    }
                }
            }
        }

        if (target == null)
        {
            context.Debug.DamageState = "Target out of melee range";
            return false;
        }

        // oGCD damage first (during weave windows)
        if (context.CanExecuteOgcd)
        {
            if (TryOgcdDamage(context))
                return true;
        }

        // GCD damage
        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        // Priority 1: Magic phase (Requiescat active)
        if (context.HasRequiescat && TryMagicPhase(context))
            return true;

        // Priority 2: Atonement chain (Sword Oath stacks)
        if (context.HasSwordOath && TryAtonementChain(context))
            return true;

        // Priority 3: Goring Blade (if DoT about to fall off)
        if (TryGoringBlade(context))
            return true;

        // Priority 4: Main combo
        if (TryMainCombo(context))
            return true;

        // Priority 5: AoE rotation
        if (TryAoERotation(context))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IThemisContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Damage

    private bool TryOgcdDamage(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Find target
        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            PLDActions.FastBlade.ActionId,
            player);

        if (target == null)
            return false;

        // Circle of Scorn (AoE DoT oGCD)
        if (context.Configuration.Tank.EnableCircleOfScorn &&
            level >= PLDActions.CircleOfScorn.MinLevel &&
            context.ActionService.IsActionReady(PLDActions.CircleOfScorn.ActionId))
        {
            if (context.ActionService.ExecuteOgcd(PLDActions.CircleOfScorn, player.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.CircleOfScorn.Name;
                context.Debug.DamageState = "Circle of Scorn";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.CircleOfScorn.ActionId, PLDActions.CircleOfScorn.Name)
                    .AsTankDamage()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        "Circle of Scorn used on cooldown for AoE DoT damage.",
                        "Circle of Scorn applies a DoT to all enemies in range. Use it on cooldown as it's a significant damage contributor, especially in multi-target scenarios.")
                    .Factors("Circle of Scorn off cooldown", "Enemies in range", $"Target: {target.Name?.TextValue}")
                    .Alternatives("Delay for alignment (usually not worth it)", "Skip in single-target (still use it - 25s CD)")
                    .Tip("Circle of Scorn is a 25s cooldown AoE DoT. Use it on cooldown in both single and multi-target situations.")
                    .Concept(PldConcepts.CircleOfScorn)
                    .Record();

                context.TrainingService?.RecordConceptApplication(PldConcepts.CircleOfScorn, wasSuccessful: true);

                return true;
            }
        }

        // Expiacion (Lv.86) or Spirits Within (Lv.30)
        if (!context.Configuration.Tank.EnableSpiritsWithin)
            return false;

        var spiritsAction = level >= PLDActions.Expiacion.MinLevel
            ? PLDActions.Expiacion
            : PLDActions.SpiritsWithin;

        if (level >= spiritsAction.MinLevel &&
            context.ActionService.IsActionReady(spiritsAction.ActionId))
        {
            if (context.ActionService.ExecuteOgcd(spiritsAction, target.GameObjectId))
            {
                context.Debug.PlannedAction = spiritsAction.Name;
                context.Debug.DamageState = spiritsAction.Name;

                TrainingHelper.Decision(context.TrainingService)
                    .Action(spiritsAction.ActionId, spiritsAction.Name)
                    .AsTankDamage()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        $"{spiritsAction.Name} used on cooldown for single-target oGCD damage.",
                        $"{spiritsAction.Name} is a high-potency single-target oGCD. Use it on cooldown, ideally inside Fight or Flight windows for maximum damage.")
                    .Factors($"{spiritsAction.Name} off cooldown", $"Target in melee range: {target.Name?.TextValue}", context.HasFightOrFlight ? "Inside Fight or Flight" : "Outside burst window")
                    .Alternatives("Delay for burst (minor benefit, usually not worth delaying past 1 GCD)")
                    .Tip($"Use {spiritsAction.Name} on cooldown. Align with Fight or Flight when possible but don't delay it more than one GCD.")
                    .Concept(PldConcepts.Expiacion)
                    .Record();

                context.TrainingService?.RecordConceptApplication(PldConcepts.Expiacion, wasSuccessful: true);

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Magic Phase (Requiescat)

    private bool TryMagicPhase(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            25f, // Magic range
            player);

        if (target == null)
            return false;

        // Confiteor chain (Lv.80+)
        if (level >= PLDActions.Confiteor.MinLevel && context.ConfiteorStep > 0)
        {
            ActionDefinition confiteorAction = context.ConfiteorStep switch
            {
                1 => PLDActions.Confiteor,
                2 => PLDActions.BladeOfFaith,
                3 => PLDActions.BladeOfTruth,
                4 => PLDActions.BladeOfValor,
                _ => PLDActions.Confiteor
            };

            if (level >= confiteorAction.MinLevel)
            {
                if (context.ActionService.ExecuteGcd(confiteorAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = confiteorAction.Name;
                    context.Debug.DamageState = $"Confiteor chain ({context.ConfiteorStep}/4)";

                    var isConfiteorStart = context.ConfiteorStep == 1;
                    var conceptId = isConfiteorStart ? PldConcepts.Confiteor : PldConcepts.BladeCombo;

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(confiteorAction.ActionId, confiteorAction.Name)
                        .AsTankBurst()
                        .Target(target.Name?.TextValue)
                        .Reason(
                            $"Confiteor chain step {context.ConfiteorStep}/4 during Requiescat phase.",
                            "Confiteor → Blade of Faith → Blade of Truth → Blade of Valor is your highest-potency burst combo during Requiescat. Always complete the full chain.")
                        .Factors($"Requiescat active", $"Confiteor step: {context.ConfiteorStep}/4", $"Target: {target.Name?.TextValue}")
                        .Alternatives("Holy Spirit instead (lower potency)", "Delay chain (stacks expire if held)")
                        .Tip("Always complete the full Confiteor combo during Requiescat. Each step has very high potency and the chain must not be broken.")
                        .Concept(conceptId)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(conceptId, wasSuccessful: true);

                    return true;
                }
            }
        }

        // Check enemy count for Holy Circle vs Holy Spirit
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        var minAoE = context.Configuration.Tank.AoEMinTargets;

        // Holy Circle (AoE) if enough targets
        if (level >= PLDActions.HolyCircle.MinLevel &&
            enemyCount >= minAoE &&
            context.Configuration.Tank.EnableAoEDamage)
        {
            if (context.ActionService.ExecuteGcd(PLDActions.HolyCircle, player.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.HolyCircle.Name;
                context.Debug.DamageState = $"Holy Circle ({enemyCount} targets)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.HolyCircle.ActionId, PLDActions.HolyCircle.Name)
                    .AsTankBurst()
                    .Target("AoE")
                    .Reason(
                        $"Holy Circle used during Requiescat with {enemyCount} targets in range.",
                        "Holy Circle is free and instant during Requiescat. In AoE situations, it outperforms Holy Spirit and is your primary Requiescat spender.")
                    .Factors($"Requiescat stacks: {context.RequiescatStacks}", $"Enemy count: {enemyCount} (>= {minAoE} threshold)", "AoE damage enabled")
                    .Alternatives($"Holy Spirit ({enemyCount} targets, lower total potency)", "Confiteor chain (use after spenders)")
                    .Tip("During Requiescat in multi-target, use Holy Circle to spend all stacks before the Confiteor chain. It's free and hits everything.")
                    .Concept(PldConcepts.MagicPhase)
                    .Record();

                context.TrainingService?.RecordConceptApplication(PldConcepts.MagicPhase, wasSuccessful: true);

                return true;
            }
        }

        // Holy Spirit (single target)
        if (level >= PLDActions.HolySpirit.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(PLDActions.HolySpirit, target.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.HolySpirit.Name;
                context.Debug.DamageState = $"Holy Spirit ({context.RequiescatStacks} stacks)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.HolySpirit.ActionId, PLDActions.HolySpirit.Name)
                    .AsTankBurst()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        $"Holy Spirit used during Requiescat ({context.RequiescatStacks} stacks remaining).",
                        "Holy Spirit is your primary single-target magic GCD during Requiescat. It's instant and free during the buff window.")
                    .Factors($"Requiescat stacks: {context.RequiescatStacks}", $"Single target scenario ({enemyCount} enemies)", $"Target: {target.Name?.TextValue}")
                    .Alternatives("Holy Circle (better with 3+ targets)", "Confiteor chain (use when 0 stacks remain)")
                    .Tip("Spend all Requiescat stacks with Holy Spirit before the buff expires, then proceed to Confiteor if available.")
                    .Concept(PldConcepts.HolySpirit)
                    .Record();

                context.TrainingService?.RecordConceptApplication(PldConcepts.HolySpirit, wasSuccessful: true);

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Atonement Chain

    private bool TryAtonementChain(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Atonement.MinLevel)
            return false;

        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            PLDActions.FastBlade.ActionId,
            player);

        if (target == null)
            return false;

        // Get the correct Atonement action based on chain position
        ActionDefinition atonementAction = context.AtonementStep switch
        {
            1 => PLDActions.Atonement,
            2 => PLDActions.Supplication,
            3 => PLDActions.Sepulchre,
            _ => PLDActions.Atonement
        };

        // Use Atonement chain during Fight or Flight for burst
        // Or when we have stacks and need to spend them
        var shouldUseAtonement = context.HasFightOrFlight ||
                                  context.SwordOathStacks > 0;

        if (shouldUseAtonement)
        {
            if (context.ActionService.ExecuteGcd(atonementAction, target.GameObjectId))
            {
                context.Debug.PlannedAction = atonementAction.Name;
                context.Debug.DamageState = $"Atonement chain ({context.AtonementStep}/3)";

                // Training: Record atonement chain execution
                if (context.HasFightOrFlight)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(atonementAction.ActionId, atonementAction.Name)
                        .AsTankBurst()
                        .Target(target.Name?.TextValue)
                        .Reason(
                            $"Atonement chain during Fight or Flight ({context.FightOrFlightRemaining:F1}s remaining)",
                            "Atonement → Supplication → Sepulchre is a high-potency chain unlocked by Royal Authority. Use during Fight or Flight for maximum damage.")
                        .Factors($"Fight or Flight active ({context.FightOrFlightRemaining:F1}s)", $"Sword Oath stacks: {context.SwordOathStacks}", $"Chain position: {context.AtonementStep}/3")
                        .Alternatives("Save stacks for later burst (risk losing them)", "Use main combo instead (lower potency)")
                        .Tip("Always complete the full Atonement chain during Fight or Flight. The stacks only last 30 seconds, so don't hold them too long.")
                        .Concept(PldConcepts.AtonementChain)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(PldConcepts.AtonementChain, wasSuccessful: true);
                }
                else
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(atonementAction.ActionId, atonementAction.Name)
                        .AsTankDamage()
                        .Target(target.Name?.TextValue)
                        .Reason(
                            $"Atonement chain step {context.AtonementStep}/3 - spending Sword Oath stacks before expiry.",
                            "Sword Oath stacks last 30 seconds. Spend them via the Atonement chain before they expire, even outside burst windows.")
                        .Factors($"Sword Oath stacks: {context.SwordOathStacks}", $"Chain position: {context.AtonementStep}/3", "Fight or Flight not active")
                        .Alternatives("Hold for Fight or Flight (only if FoF is ready soon)", "Use main combo (resets combo, wastes stacks)")
                        .Tip("Atonement, Supplication, and Sepulchre each have high potency. Don't let them expire - spend stacks within 30 seconds of gaining them.")
                        .Concept(PldConcepts.AtonementChain)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(PldConcepts.AtonementChain, wasSuccessful: true);
                }

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Goring Blade

    private bool TryGoringBlade(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.GoringBlade.MinLevel)
            return false;

        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            PLDActions.FastBlade.ActionId,
            player);

        if (target == null)
            return false;

        // Refresh DoT when it has less than 5 seconds remaining
        // Or during Fight or Flight for optimal damage
        var shouldRefresh = context.GoringBladeRemaining < 5f ||
                           (context.HasFightOrFlight && context.GoringBladeRemaining < 10f);

        if (shouldRefresh)
        {
            // Check if Blade of Honor is available (Lv.100 upgrade)
            if (level >= PLDActions.BladeOfHonor.MinLevel && context.HasBladeOfHonor)
            {
                if (context.ActionService.ExecuteGcd(PLDActions.BladeOfHonor, target.GameObjectId))
                {
                    context.Debug.PlannedAction = PLDActions.BladeOfHonor.Name;
                    context.Debug.DamageState = "Blade of Honor";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(PLDActions.BladeOfHonor.ActionId, PLDActions.BladeOfHonor.Name)
                        .AsTankBurst()
                        .Target(target.Name?.TextValue)
                        .Reason(
                            "Blade of Honor used as the Lv.100 DoT refresh and AoE finisher.",
                            "Blade of Honor is the enhanced Goring Blade at level 100. Higher potency and AoE splash - use whenever available for DoT refresh.")
                        .Factors($"Blade of Honor available", $"DoT remaining: {context.GoringBladeRemaining:F1}s", context.HasFightOrFlight ? "Inside Fight or Flight" : "DoT refresh window")
                        .Alternatives("Goring Blade (lower level version)", "Skip (DoT falls off, major DPS loss)")
                        .Tip("Blade of Honor upgrades Goring Blade at level 100. Treat it identically but enjoy the higher potency and AoE component.")
                        .Concept(PldConcepts.GoringBlade)
                        .Record();

                    context.TrainingService?.RecordConceptApplication(PldConcepts.GoringBlade, wasSuccessful: true);

                    return true;
                }
            }

            if (context.ActionService.ExecuteGcd(PLDActions.GoringBlade, target.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.GoringBlade.Name;
                context.Debug.DamageState = $"Goring Blade (DoT {context.GoringBladeRemaining:F1}s)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.GoringBlade.ActionId, PLDActions.GoringBlade.Name)
                    .AsTankDamage()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        $"Goring Blade applied to refresh DoT ({context.GoringBladeRemaining:F1}s remaining).",
                        "Goring Blade applies a DoT that must be refreshed before it expires. Refresh under Fight or Flight for optimal timing alignment.")
                    .Factors($"DoT remaining: {context.GoringBladeRemaining:F1}s", context.HasFightOrFlight ? "Inside Fight or Flight (refresh early for alignment)" : "DoT falling off (<5s)", $"Target: {target.Name?.TextValue}")
                    .Alternatives("Delay until Fight or Flight (risk DoT falling off)", "Skip (significant DPS loss from missing DoT)")
                    .Tip("Keep Goring Blade up at all times. Refresh it under Fight or Flight when possible for the damage amplification.")
                    .Concept(PldConcepts.GoringBlade)
                    .Record();

                context.TrainingService?.RecordConceptApplication(PldConcepts.GoringBlade, wasSuccessful: true);

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Main Combo

    private bool TryMainCombo(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            PLDActions.FastBlade.ActionId,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        ActionDefinition comboAction;
        string comboNote;

        // Determine combo action based on current step
        switch (context.ComboStep)
        {
            case 0:
            case 1:
                // Start combo or first hit
                comboAction = PLDActions.FastBlade;
                comboNote = "Combo 1/3";
                break;

            case 2:
                // Second hit (Riot Blade)
                if (context.LastComboAction == PLDActions.FastBlade.ActionId &&
                    level >= PLDActions.RiotBlade.MinLevel)
                {
                    comboAction = PLDActions.RiotBlade;
                    comboNote = "Combo 2/3";
                }
                else
                {
                    // Combo broken, restart
                    comboAction = PLDActions.FastBlade;
                    comboNote = "Combo restart";
                }
                break;

            case 3:
                // Third hit (Royal Authority or Rage of Halone)
                if (context.LastComboAction == PLDActions.RiotBlade.ActionId)
                {
                    comboAction = PLDActions.GetComboFinisher(level);
                    comboNote = "Combo 3/3";
                }
                else
                {
                    // Combo broken, restart
                    comboAction = PLDActions.FastBlade;
                    comboNote = "Combo restart";
                }
                break;

            default:
                // Should not happen, but restart combo
                comboAction = PLDActions.FastBlade;
                comboNote = "Combo restart";
                break;
        }

        // Check combo timeout
        if (context.ComboTimeRemaining <= 0 && context.ComboStep > 1)
        {
            comboAction = PLDActions.FastBlade;
            comboNote = "Combo expired";
        }

        if (context.ActionService.ExecuteGcd(comboAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = comboAction.Name;
            context.Debug.DamageState = comboNote;

            var isFinisher = context.ComboStep == 3 || comboNote.Contains("3/3");
            var conceptId = isFinisher ? PldConcepts.AtonementChain : PldConcepts.BurstWindow;

            TrainingHelper.Decision(context.TrainingService)
                .Action(comboAction.ActionId, comboAction.Name)
                .AsTankDamage()
                .Target(target.Name?.TextValue)
                .Reason(
                    $"Main combo: {comboNote}.",
                    "Fast Blade → Riot Blade → Royal Authority is the foundation of PLD DPS. Royal Authority grants Sword Oath stacks for the Atonement chain.")
                .Factors($"Combo step: {comboNote}", context.HasFightOrFlight ? "Inside Fight or Flight burst" : "Outside burst window", $"Target: {target.Name?.TextValue}")
                .Alternatives("Atonement chain (higher priority if stacks available)", "Goring Blade (higher priority if DoT falling off)")
                .Tip("Complete the 1-2-3 combo consistently. Royal Authority is the finisher that grants Atonement stacks - the core of PLD's rotation.")
                .Concept(conceptId)
                .Record();

            context.TrainingService?.RecordConceptApplication(conceptId, wasSuccessful: true);

            return true;
        }

        // ExecuteGcd failed - likely action not usable
        context.Debug.DamageState = $"Execute failed: {comboAction.Name}";
        return false;
    }

    #endregion

    #region AoE Rotation

    private bool TryAoERotation(IThemisContext context)
    {
        if (!context.Configuration.Tank.EnableAoEDamage)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.TotalEclipse.MinLevel)
            return false;

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        var minAoE = context.Configuration.Tank.AoEMinTargets;

        if (enemyCount < minAoE)
            return false;

        ActionDefinition aoeAction;
        string aoeNote;

        // AoE combo
        if (context.ComboStep == 2 &&
            context.LastComboAction == PLDActions.TotalEclipse.ActionId &&
            level >= PLDActions.Prominence.MinLevel)
        {
            aoeAction = PLDActions.Prominence;
            aoeNote = $"AoE 2/2 ({enemyCount} targets)";
        }
        else
        {
            aoeAction = PLDActions.TotalEclipse;
            aoeNote = $"AoE 1/2 ({enemyCount} targets)";
        }

        if (context.ActionService.ExecuteGcd(aoeAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = aoeAction.Name;
            context.Debug.DamageState = aoeNote;

            TrainingHelper.Decision(context.TrainingService)
                .Action(aoeAction.ActionId, aoeAction.Name)
                .AsTankDamage()
                .Target("AoE")
                .Reason(
                    $"AoE combo: {aoeNote}.",
                    "Total Eclipse → Prominence is PLD's AoE combo. Use when 3+ enemies are present. Prominence also builds Oath Gauge.")
                .Factors($"Enemy count: {enemyCount} (>= {minAoE} threshold)", $"AoE combo step: {aoeNote}", "AoE damage enabled")
                .Alternatives("Single target combo (better for 1-2 targets)", "Holy Circle in Requiescat (better magic AoE)")
                .Tip("Switch to AoE rotation at 3+ targets. Total Eclipse → Prominence generates Oath Gauge via Prominence's combo bonus.")
                .Concept(PldConcepts.MagicPhase)
                .Record();

            context.TrainingService?.RecordConceptApplication(PldConcepts.MagicPhase, wasSuccessful: true);

            return true;
        }

        return false;
    }

    #endregion

}
