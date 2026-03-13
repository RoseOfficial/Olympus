using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.NikeCore.Modules;

/// <summary>
/// Handles the Samurai damage rotation.
/// Manages combos, Iaijutsu, Kaeshi, Ogi Namikiri, and Kenki spenders.
/// Extends BaseDpsDamageModule for shared damage module patterns.
/// </summary>
public sealed class DamageModule : BaseDpsDamageModule<INikeContext>, INikeModule
{
    // Kenki threshold for spending (Shinten/Kyuten cost 25)
    private const int KenkiSpendThreshold = 25;

    // Higanbana refresh threshold (DoT is 60s)
    private const float HiganbanaRefreshThreshold = 5f;

    #region Abstract Method Implementations

    /// <summary>
    /// Melee targeting range (3y) — used as fallback only.
    /// </summary>
    protected override float GetTargetingRange() => FFXIVConstants.MeleeTargetingRange;

    /// <summary>
    /// Use Hakaze to check melee range via game API for maximum accuracy.
    /// </summary>
    protected override uint GetRangeCheckActionId() => SAMActions.Hakaze.ActionId;

    /// <summary>
    /// AoE count range for SAM (5y for melee AoE abilities).
    /// </summary>
    protected override float GetAoECountRange() => 5f;

    /// <summary>
    /// Sets the damage state in the debug display.
    /// </summary>
    protected override void SetDamageState(INikeContext context, string state) =>
        context.Debug.DamageState = state;

    /// <summary>
    /// Sets the nearby enemy count in the debug display.
    /// </summary>
    protected override void SetNearbyEnemies(INikeContext context, int count) =>
        context.Debug.NearbyEnemies = count;

    /// <summary>
    /// Sets the planned action name in the debug display.
    /// </summary>
    protected override void SetPlannedAction(INikeContext context, string action) =>
        context.Debug.PlannedAction = action;

    /// <summary>
    /// oGCD damage for Samurai - Kenki spenders (Shinten, Kyuten).
    /// </summary>
    protected override bool TryOgcdDamage(INikeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Kenki spenders (Shinten/Kyuten)
        if (TryKenkiSpender(context, target, enemyCount))
            return true;

        return false;
    }

    private bool TryKenkiSpender(INikeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need 25 Kenki to spend
        if (context.Kenki < KenkiSpendThreshold)
            return false;

        // Don't overcap - always spend if near max
        var shouldSpend = context.Kenki >= 85;

        // Or spend if we have a comfortable amount
        if (!shouldSpend && context.Kenki >= 50)
            shouldSpend = true;

        if (!shouldSpend)
            return false;

        // Choose ST or AoE based on enemy count
        if (enemyCount >= AoeThreshold && level >= SAMActions.Kyuten.MinLevel)
        {
            if (context.ActionService.IsActionReady(SAMActions.Kyuten.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(SAMActions.Kyuten, player.GameObjectId))
                {
                    context.Debug.PlannedAction = SAMActions.Kyuten.Name;
                    context.Debug.DamageState = $"Kyuten ({enemyCount} enemies)";

                    // Training: Record Kyuten decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Kyuten.ActionId, SAMActions.Kyuten.Name)
                        .AsAoE(enemyCount)
                        .Target($"{enemyCount} enemies")
                        .Reason($"Spending 25 Kenki on Kyuten ({enemyCount} enemies)",
                            "Kyuten is the AoE Kenki spender. Use to prevent Kenki overcap. " +
                            "Prioritize Senei/Guren on cooldown, then use Kyuten/Shinten for excess.")
                        .Factors(new[] { $"Kenki: {context.Kenki}", $"{enemyCount} enemies", "Avoiding overcap" })
                        .Alternatives(new[] { "Use Shinten (less total damage)", "Hold for Senei/Guren (if soon)" })
                        .Tip("Don't sit at max Kenki. Spend regularly on Shinten/Kyuten.")
                        .Concept("sam_kenki_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_kenki_gauge", true, "AoE Kenki spending");

                    return true;
                }
            }
        }
        else if (level >= SAMActions.Shinten.MinLevel)
        {
            if (context.ActionService.IsActionReady(SAMActions.Shinten.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(SAMActions.Shinten, target.GameObjectId))
                {
                    context.Debug.PlannedAction = SAMActions.Shinten.Name;
                    context.Debug.DamageState = "Shinten";

                    // Training: Record Shinten decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Shinten.ActionId, SAMActions.Shinten.Name)
                        .AsMeleeResource("Kenki", context.Kenki)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Spending 25 Kenki on Shinten",
                            "Shinten is your primary single-target Kenki spender. " +
                            "Use to avoid overcapping Kenki (100 max). Keep some reserve for Zanshin (50).")
                        .Factors(new[] { $"Kenki: {context.Kenki}", "Avoiding overcap", "ST damage filler" })
                        .Alternatives(new[] { "Wait for Senei (if soon)", "Overcap Kenki (wastes gauge)" })
                        .Tip("Shinten is filler damage. Spend Kenki before it caps.")
                        .Concept("sam_kenki_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_kenki_gauge", true, "ST Kenki spending");

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Main GCD damage rotation for Samurai.
    /// Handles Kaeshi, Ogi Namikiri, Iaijutsu, Meikyo finishers, and combo rotation.
    /// </summary>
    protected override bool TryGcdDamage(INikeContext context, IBattleChara target, int enemyCount, bool isMoving)
    {
        // Priority 1: Kaeshi: Namikiri (after Ogi Namikiri)
        if (TryKaeshiNamikiri(context, target))
            return true;

        // Priority 2: Tsubame-gaeshi (after Iaijutsu)
        if (TryTsubameGaeshi(context, target))
            return true;

        // Priority 3: Ogi Namikiri (when ready)
        if (TryOgiNamikiri(context, target))
            return true;

        // Priority 4: Iaijutsu (when we have Sen)
        if (TryIaijutsu(context, target, enemyCount))
            return true;

        // Priority 5: Meikyo finishers (direct finisher usage)
        if (TryMeikyoFinisher(context, target, enemyCount))
            return true;

        // Priority 6: Combo rotation
        if (TryComboRotation(context, target, enemyCount))
            return true;

        return false;
    }

    #endregion

    #region Kaeshi: Namikiri

    private bool TryKaeshiNamikiri(INikeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SAMActions.KaeshiNamikiri.MinLevel)
            return false;

        // Need Kaeshi: Namikiri Ready buff
        if (!context.HasKaeshiNamikiriReady)
            return false;

        if (!context.ActionService.IsActionReady(SAMActions.KaeshiNamikiri.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(SAMActions.KaeshiNamikiri, target.GameObjectId))
        {
            context.Debug.PlannedAction = SAMActions.KaeshiNamikiri.Name;
            context.Debug.DamageState = "Kaeshi: Namikiri";

            // Training: Record Kaeshi: Namikiri decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(SAMActions.KaeshiNamikiri.ActionId, SAMActions.KaeshiNamikiri.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason("Following up Ogi Namikiri with Kaeshi: Namikiri",
                    "Kaeshi: Namikiri is a guaranteed follow-up after Ogi Namikiri. " +
                    "It has a short window so use it immediately. High potency burst damage.")
                .Factors(new[] { "Kaeshi: Namikiri Ready buff active", "Ogi Namikiri just used", "Burst window active" })
                .Alternatives(new[] { "Miss the window (buff expires)" })
                .Tip("Always use Kaeshi: Namikiri immediately after Ogi Namikiri.")
                .Concept("sam_iaijutsu")
                .Record();
            context.TrainingService?.RecordConceptApplication("sam_iaijutsu", true, "Kaeshi: Namikiri follow-up");

            return true;
        }

        return false;
    }

    #endregion

    #region Tsubame-gaeshi

    private bool TryTsubameGaeshi(INikeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SAMActions.TsubameGaeshi.MinLevel)
            return false;

        // Need Tsubame-gaeshi Ready buff
        if (!context.HasTsubameGaeshiReady)
            return false;

        // Get the correct Kaeshi action based on last Iaijutsu
        var kaeshiAction = SAMActions.GetKaeshiAction(context.LastIaijutsu);

        if (!context.ActionService.IsActionReady(kaeshiAction.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(kaeshiAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = kaeshiAction.Name;
            context.Debug.DamageState = kaeshiAction.Name;

            // Training: Record Tsubame-gaeshi decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(kaeshiAction.ActionId, kaeshiAction.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason($"Following up Iaijutsu with {kaeshiAction.Name}",
                    "Tsubame-gaeshi repeats your last Iaijutsu. " +
                    "Use immediately after Iaijutsu - the window is short. " +
                    "Kaeshi: Setsugekka is highest potency, Kaeshi: Goken for AoE.")
                .Factors(new[] { "Tsubame-gaeshi Ready buff active", $"Last Iaijutsu: {context.LastIaijutsu}", "Burst window active" })
                .Alternatives(new[] { "Miss the window (buff expires)", "Wrong Iaijutsu order" })
                .Tip("Iaijutsu → Tsubame-gaeshi is your core burst combo. Never skip it.")
                .Concept("sam_iaijutsu")
                .Record();
            context.TrainingService?.RecordConceptApplication("sam_iaijutsu", true, "Tsubame-gaeshi follow-up");

            return true;
        }

        return false;
    }

    #endregion

    #region Ogi Namikiri

    private bool TryOgiNamikiri(INikeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SAMActions.OgiNamikiri.MinLevel)
            return false;

        // Need Ogi Namikiri Ready buff
        if (!context.HasOgiNamikiriReady)
            return false;

        if (!context.ActionService.IsActionReady(SAMActions.OgiNamikiri.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(SAMActions.OgiNamikiri, target.GameObjectId))
        {
            context.Debug.PlannedAction = SAMActions.OgiNamikiri.Name;
            context.Debug.DamageState = "Ogi Namikiri";

            // Training: Record Ogi Namikiri decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(SAMActions.OgiNamikiri.ActionId, SAMActions.OgiNamikiri.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason("Using Ogi Namikiri (granted by Ikishoten)",
                    "Ogi Namikiri is SAM's highest potency GCD. Granted by Ikishoten. " +
                    "Always follow with Kaeshi: Namikiri. Use during burst windows for maximum effect.")
                .Factors(new[] { "Ogi Namikiri Ready buff active", "From Ikishoten", "Burst window active" })
                .Alternatives(new[] { "Hold for raid buffs (if close)", "Use outside burst (wastes damage)" })
                .Tip("Ogi Namikiri → Kaeshi: Namikiri → Zanshin is your biggest burst sequence.")
                .Concept("sam_burst_window")
                .Record();
            context.TrainingService?.RecordConceptApplication("sam_burst_window", true, "Ogi Namikiri burst");

            return true;
        }

        return false;
    }

    #endregion

    #region Iaijutsu

    private bool TryIaijutsu(INikeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need at least 1 Sen
        if (context.SenCount == 0)
            return false;

        var iaijutsuType = SAMActions.GetIaijutsuType(context.SenCount);

        // Decide which Iaijutsu to use
        switch (context.SenCount)
        {
            case 1:
                // Higanbana - only if DoT is not up or about to expire
                if (level < SAMActions.Higanbana.MinLevel)
                    return false;

                // Don't use Higanbana in AoE
                if (enemyCount >= AoeThreshold)
                    return false;

                // Check if Higanbana needs to be applied/refreshed
                if (context.HasHiganbanaOnTarget && context.HiganbanaRemaining > HiganbanaRefreshThreshold)
                    return false;

                return ExecuteIaijutsu(context, target, SAMActions.Higanbana, iaijutsuType);

            case 2:
                // Tenka Goken - AoE burst
                if (level < SAMActions.TenkaGoken.MinLevel)
                    return false;

                // Only use at 2 Sen if in AoE situation
                if (enemyCount < AoeThreshold)
                    return false;

                return ExecuteIaijutsu(context, target, SAMActions.TenkaGoken, iaijutsuType);

            case 3:
                // Midare Setsugekka - ST burst
                if (level < SAMActions.MidareSetsugekka.MinLevel)
                    return false;

                return ExecuteIaijutsu(context, target, SAMActions.MidareSetsugekka, iaijutsuType);
        }

        return false;
    }

    private bool ExecuteIaijutsu(INikeContext context, IBattleChara target, Models.Action.ActionDefinition action, SAMActions.IaijutsuType type)
    {
        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} ({context.SenCount} Sen)";
            context.Debug.LastIaijutsu = type;

            // Training: Record Iaijutsu decision based on type
            var (description, explanation, tip, conceptId) = GetIaijutsuTrainingInfo(type, context);
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(description, explanation)
                .Factors(new[] { $"Sen count: {context.SenCount}", GetSenState(context), "Iaijutsu ready" })
                .Alternatives(new[] { "Continue building Sen", "Use wrong Sen count" })
                .Tip(tip)
                .Concept(conceptId)
                .Record();
            context.TrainingService?.RecordConceptApplication("sam_sen_system", true, $"Iaijutsu: {action.Name}");

            return true;
        }

        return false;
    }

    private static (string description, string explanation, string tip, string conceptId) GetIaijutsuTrainingInfo(SAMActions.IaijutsuType type, INikeContext context)
    {
        return type switch
        {
            SAMActions.IaijutsuType.Higanbana => (
                $"Applying Higanbana DoT ({(context.HasHiganbanaOnTarget ? $"{context.HiganbanaRemaining:F1}s remaining" : "not on target")})",
                "Higanbana is SAM's 60s DoT. Apply once and refresh when <5s remains. " +
                "Don't use in AoE situations. The full duration deals more damage than Midare.",
                "Keep Higanbana up 100% of the time on single-target fights.",
                "sam_sen_system"),
            SAMActions.IaijutsuType.TenkaGoken => (
                "Using Tenka Goken (2 Sen AoE Iaijutsu)",
                "Tenka Goken is the 2-Sen AoE Iaijutsu. Use instead of Midare when hitting 3+ targets. " +
                "Still triggers Tsubame-gaeshi for Kaeshi: Goken follow-up.",
                "In AoE, use Tenka Goken at 2 Sen rather than building to 3.",
                "sam_aoe_rotation"),
            SAMActions.IaijutsuType.MidareSetsugekka => (
                "Using Midare Setsugekka (3 Sen burst)",
                "Midare Setsugekka is SAM's primary burst GCD. Requires all 3 Sen. " +
                "Always follow with Tsubame-gaeshi for Kaeshi: Setsugekka.",
                "Midare → Kaeshi: Setsugekka is your bread-and-butter burst combo.",
                "sam_iaijutsu"),
            _ => (
                "Using Iaijutsu",
                "Iaijutsu abilities consume Sen to deal high damage.",
                "Build Sen through combos, spend with Iaijutsu.",
                "sam_iaijutsu")
        };
    }

    private static string GetSenState(INikeContext context)
    {
        var sen = new System.Collections.Generic.List<string>();
        if (context.HasSetsu) sen.Add("Setsu");
        if (context.HasGetsu) sen.Add("Getsu");
        if (context.HasKa) sen.Add("Ka");
        return sen.Count > 0 ? string.Join("+", sen) : "No Sen";
    }

    #endregion

    #region Meikyo Finishers

    private bool TryMeikyoFinisher(INikeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need Meikyo Shisui active
        if (!context.HasMeikyoShisui || context.MeikyoStacks <= 0)
            return false;

        var useAoe = enemyCount >= AoeThreshold;

        if (useAoe)
        {
            return TryAoeMeikyoFinisher(context, target);
        }
        else
        {
            return TrySingleTargetMeikyoFinisher(context, target);
        }
    }

    private bool TrySingleTargetMeikyoFinisher(INikeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority during Meikyo:
        // 1. Gekko if we don't have Getsu
        // 2. Kasha if we don't have Ka
        // 3. Yukikaze if we don't have Setsu

        // Gekko (rear positional)
        if (!context.HasGetsu && level >= SAMActions.Gekko.MinLevel)
        {
            if (context.ActionService.IsActionReady(SAMActions.Gekko.ActionId))
            {
                bool correctPositional = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                string positionalHint = correctPositional ? "(rear)" : "(WRONG)";

                if (context.ActionService.ExecuteGcd(SAMActions.Gekko, target.GameObjectId))
                {
                    context.Debug.PlannedAction = SAMActions.Gekko.Name;
                    context.Debug.DamageState = $"Meikyo Gekko {positionalHint}";

                    // Training: Record Gekko positional decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Gekko.ActionId, SAMActions.Gekko.Name)
                        .AsPositional(correctPositional, "rear")
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Meikyo Gekko for Getsu Sen {positionalHint}",
                            "Gekko grants Getsu (Moon) Sen and has a rear positional for bonus damage. " +
                            "During Meikyo Shisui, you can use finishers directly without combos.")
                        .Factors(new[] { "Meikyo Shisui active", "Need Getsu Sen", correctPositional ? "At rear" : "Not at rear" })
                        .Alternatives(new[] { "Use Kasha instead (need Ka)", "Use Yukikaze (need Setsu)" })
                        .Tip("Gekko = rear, Kasha = flank. Use True North when you can't position.")
                        .Concept("sam_positionals")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_positionals", correctPositional, "Gekko rear positional");

                    return true;
                }
            }
        }

        // Kasha (flank positional)
        if (!context.HasKa && level >= SAMActions.Kasha.MinLevel)
        {
            if (context.ActionService.IsActionReady(SAMActions.Kasha.ActionId))
            {
                bool correctPositional = context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                string positionalHint = correctPositional ? "(flank)" : "(WRONG)";

                if (context.ActionService.ExecuteGcd(SAMActions.Kasha, target.GameObjectId))
                {
                    context.Debug.PlannedAction = SAMActions.Kasha.Name;
                    context.Debug.DamageState = $"Meikyo Kasha {positionalHint}";

                    // Training: Record Kasha positional decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Kasha.ActionId, SAMActions.Kasha.Name)
                        .AsPositional(correctPositional, "flank")
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Meikyo Kasha for Ka Sen {positionalHint}",
                            "Kasha grants Ka (Flower) Sen and has a flank positional for bonus damage. " +
                            "During Meikyo Shisui, you can use finishers directly without combos.")
                        .Factors(new[] { "Meikyo Shisui active", "Need Ka Sen", correctPositional ? "At flank" : "Not at flank" })
                        .Alternatives(new[] { "Use Gekko instead (need Getsu)", "Use Yukikaze (need Setsu)" })
                        .Tip("Kasha = flank, Gekko = rear. Use True North when you can't position.")
                        .Concept("sam_positionals")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_positionals", correctPositional, "Kasha flank positional");

                    return true;
                }
            }
        }

        // Yukikaze
        if (!context.HasSetsu && level >= SAMActions.Yukikaze.MinLevel)
        {
            if (context.ActionService.IsActionReady(SAMActions.Yukikaze.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SAMActions.Yukikaze, target.GameObjectId))
                {
                    context.Debug.PlannedAction = SAMActions.Yukikaze.Name;
                    context.Debug.DamageState = "Meikyo Yukikaze";
                    return true;
                }
            }
        }

        // If we have all Sen, use any finisher to consume Meikyo stacks
        // Prefer Gekko for damage
        if (level >= SAMActions.Gekko.MinLevel)
        {
            if (context.ActionService.IsActionReady(SAMActions.Gekko.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SAMActions.Gekko, target.GameObjectId))
                {
                    context.Debug.PlannedAction = SAMActions.Gekko.Name;
                    context.Debug.DamageState = "Meikyo Gekko (overflow)";
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryAoeMeikyoFinisher(INikeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE Meikyo priority:
        // 1. Mangetsu if we don't have Getsu (also refreshes Fugetsu)
        // 2. Oka if we don't have Ka (also refreshes Fuka)

        // Mangetsu
        if (!context.HasGetsu && level >= SAMActions.Mangetsu.MinLevel)
        {
            if (context.ActionService.IsActionReady(SAMActions.Mangetsu.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SAMActions.Mangetsu, player.GameObjectId))
                {
                    context.Debug.PlannedAction = SAMActions.Mangetsu.Name;
                    context.Debug.DamageState = "Meikyo Mangetsu";
                    return true;
                }
            }
        }

        // Oka
        if (!context.HasKa && level >= SAMActions.Oka.MinLevel)
        {
            if (context.ActionService.IsActionReady(SAMActions.Oka.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SAMActions.Oka, player.GameObjectId))
                {
                    context.Debug.PlannedAction = SAMActions.Oka.Name;
                    context.Debug.DamageState = "Meikyo Oka";
                    return true;
                }
            }
        }

        // Fallback to Mangetsu
        if (level >= SAMActions.Mangetsu.MinLevel)
        {
            if (context.ActionService.IsActionReady(SAMActions.Mangetsu.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SAMActions.Mangetsu, player.GameObjectId))
                {
                    context.Debug.PlannedAction = SAMActions.Mangetsu.Name;
                    context.Debug.DamageState = "Meikyo Mangetsu (overflow)";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Combo Rotation

    private bool TryComboRotation(INikeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold && level >= SAMActions.Fuga.MinLevel;

        if (useAoe)
        {
            return TryAoeCombo(context, target);
        }
        else
        {
            return TrySingleTargetCombo(context, target);
        }
    }

    private bool TrySingleTargetCombo(INikeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;
        var comboStep = context.ComboStep;

        // Get combo starter
        var comboStarter = SAMActions.GetComboStarter((byte)level);

        // Combo Step 2 -> Finisher (Gekko, Kasha, or Yukikaze)
        if (comboStep == 2)
        {
            // After Jinpu -> Gekko
            if (context.LastComboAction == SAMActions.Jinpu.ActionId && level >= SAMActions.Gekko.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Gekko.ActionId))
                {
                    bool correctPositional = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                    string positionalHint = correctPositional ? "(rear)" : "(WRONG)";

                    if (context.ActionService.ExecuteGcd(SAMActions.Gekko, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Gekko.Name;
                        context.Debug.DamageState = $"Gekko {positionalHint}";

                        // Training: Record Gekko combo finisher
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(SAMActions.Gekko.ActionId, SAMActions.Gekko.Name)
                            .AsPositional(correctPositional, "rear")
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"Combo finisher Gekko for Getsu Sen {positionalHint}",
                                "Gekko is the finisher after Jinpu. Grants Getsu (Moon) Sen and refreshes Fugetsu buff. " +
                                "Has a rear positional for bonus damage and extra Kenki.")
                            .Factors(new[] { "Combo step 2 (after Jinpu)", correctPositional ? "At rear" : "Not at rear", "Grants Getsu Sen" })
                            .Alternatives(new[] { "Break combo (miss finisher)", "Wrong positional (less damage)" })
                            .Tip("Gekko = rear. Position before finisher or use True North.")
                            .Concept("sam_positionals")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("sam_positionals", correctPositional, "Gekko combo rear");

                        return true;
                    }
                }
            }

            // After Shifu -> Kasha
            if (context.LastComboAction == SAMActions.Shifu.ActionId && level >= SAMActions.Kasha.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Kasha.ActionId))
                {
                    bool correctPositional = context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                    string positionalHint = correctPositional ? "(flank)" : "(WRONG)";

                    if (context.ActionService.ExecuteGcd(SAMActions.Kasha, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Kasha.Name;
                        context.Debug.DamageState = $"Kasha {positionalHint}";

                        // Training: Record Kasha combo finisher
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(SAMActions.Kasha.ActionId, SAMActions.Kasha.Name)
                            .AsPositional(correctPositional, "flank")
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"Combo finisher Kasha for Ka Sen {positionalHint}",
                                "Kasha is the finisher after Shifu. Grants Ka (Flower) Sen and refreshes Fuka buff. " +
                                "Has a flank positional for bonus damage and extra Kenki.")
                            .Factors(new[] { "Combo step 2 (after Shifu)", correctPositional ? "At flank" : "Not at flank", "Grants Ka Sen" })
                            .Alternatives(new[] { "Break combo (miss finisher)", "Wrong positional (less damage)" })
                            .Tip("Kasha = flank. Position before finisher or use True North.")
                            .Concept("sam_positionals")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("sam_positionals", correctPositional, "Kasha combo flank");

                        return true;
                    }
                }
            }
        }

        // Combo Step 1 -> Jinpu, Shifu, or Yukikaze
        if (comboStep == 1 && (context.LastComboAction == comboStarter.ActionId || context.LastComboAction == SAMActions.Hakaze.ActionId))
        {
            // Decide which path to take based on buffs and Sen
            // Priority: Refresh buffs > Build missing Sen

            // Need Fugetsu buff? -> Jinpu path
            if ((!context.HasFugetsu || context.FugetsuRemaining < 10f) && level >= SAMActions.Jinpu.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Jinpu.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(SAMActions.Jinpu, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Jinpu.Name;
                        context.Debug.DamageState = "Jinpu (Combo 2)";
                        return true;
                    }
                }
            }

            // Need Fuka buff? -> Shifu path
            if ((!context.HasFuka || context.FukaRemaining < 10f) && level >= SAMActions.Shifu.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Shifu.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(SAMActions.Shifu, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Shifu.Name;
                        context.Debug.DamageState = "Shifu (Combo 2)";
                        return true;
                    }
                }
            }

            // Need Setsu? -> Yukikaze
            if (!context.HasSetsu && level >= SAMActions.Yukikaze.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Yukikaze.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(SAMActions.Yukikaze, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Yukikaze.Name;
                        context.Debug.DamageState = "Yukikaze (Combo 2)";
                        return true;
                    }
                }
            }

            // Need Getsu? -> Jinpu path
            if (!context.HasGetsu && level >= SAMActions.Jinpu.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Jinpu.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(SAMActions.Jinpu, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Jinpu.Name;
                        context.Debug.DamageState = "Jinpu (for Getsu)";
                        return true;
                    }
                }
            }

            // Need Ka? -> Shifu path
            if (!context.HasKa && level >= SAMActions.Shifu.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Shifu.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(SAMActions.Shifu, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Shifu.Name;
                        context.Debug.DamageState = "Shifu (for Ka)";
                        return true;
                    }
                }
            }

            // Default to Jinpu if nothing specific needed
            if (level >= SAMActions.Jinpu.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Jinpu.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(SAMActions.Jinpu, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Jinpu.Name;
                        context.Debug.DamageState = "Jinpu (default)";
                        return true;
                    }
                }
            }
        }

        // Start combo with Hakaze/Gyofu
        if (context.ActionService.IsActionReady(comboStarter.ActionId))
        {
            if (context.ActionService.ExecuteGcd(comboStarter, target.GameObjectId))
            {
                context.Debug.PlannedAction = comboStarter.Name;
                context.Debug.DamageState = $"{comboStarter.Name} (Combo 1)";
                return true;
            }
        }

        return false;
    }

    private bool TryAoeCombo(INikeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;
        var comboStep = context.ComboStep;

        // Get AoE combo starter
        var aoeStarter = SAMActions.GetAoeComboStarter((byte)level);

        // Combo Step 1 -> Mangetsu or Oka
        if (comboStep == 1 && (context.LastComboAction == aoeStarter.ActionId || context.LastComboAction == SAMActions.Fuga.ActionId))
        {
            // Need Fugetsu or Getsu? -> Mangetsu
            if ((!context.HasFugetsu || !context.HasGetsu) && level >= SAMActions.Mangetsu.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Mangetsu.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(SAMActions.Mangetsu, player.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Mangetsu.Name;
                        context.Debug.DamageState = "Mangetsu (AoE 2)";
                        return true;
                    }
                }
            }

            // Need Fuka or Ka? -> Oka
            if ((!context.HasFuka || !context.HasKa) && level >= SAMActions.Oka.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Oka.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(SAMActions.Oka, player.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Oka.Name;
                        context.Debug.DamageState = "Oka (AoE 2)";
                        return true;
                    }
                }
            }

            // Default to Mangetsu
            if (level >= SAMActions.Mangetsu.MinLevel)
            {
                if (context.ActionService.IsActionReady(SAMActions.Mangetsu.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(SAMActions.Mangetsu, player.GameObjectId))
                    {
                        context.Debug.PlannedAction = SAMActions.Mangetsu.Name;
                        context.Debug.DamageState = "Mangetsu (default)";
                        return true;
                    }
                }
            }
        }

        // Start AoE combo with Fuko/Fuga
        if (context.ActionService.IsActionReady(aoeStarter.ActionId))
        {
            if (context.ActionService.ExecuteGcd(aoeStarter, player.GameObjectId))
            {
                context.Debug.PlannedAction = aoeStarter.Name;
                context.Debug.DamageState = $"{aoeStarter.Name} (AoE 1)";
                return true;
            }
        }

        return false;
    }

    #endregion
}
