using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.RoleActionHelpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.NikeCore.Abilities;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.NikeCore.Modules;

/// <summary>
/// Handles the Samurai damage rotation (scheduler-driven).
/// Manages combos, Iaijutsu, Kaeshi, Ogi Namikiri, Kenki spenders.
/// </summary>
public sealed class DamageModule : INikeModule
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


    public bool TryExecute(INikeContext context, bool isMoving) => false;

    public void UpdateDebugState(INikeContext context) { }

    public void CollectCandidates(INikeContext context, RotationScheduler scheduler, bool isMoving)
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
            SAMActions.Hakaze.ActionId,
            player);
        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return;
        }

        var aoeEnabled = context.Configuration.Samurai.EnableAoERotation;
        var aoeThreshold = context.Configuration.Samurai.AoEMinTargets;
        var rawEnemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = rawEnemyCount;
        var enemyCount = aoeEnabled ? rawEnemyCount : 0;
        var useAoE = enemyCount >= aoeThreshold;

        // oGCDs
        TryPushFeint(context, scheduler, target);
        TryPushSecondWind(context, scheduler);
        TryPushKenkiSpender(context, scheduler, target, useAoE);

        // GCDs (priority order)
        TryPushKaeshiNamikiri(context, scheduler, target);
        TryPushTsubameGaeshi(context, scheduler, target);
        TryPushOgiNamikiri(context, scheduler, target);
        TryPushIaijutsu(context, scheduler, target, useAoE);
        TryPushMeikyoFinisher(context, scheduler, target, useAoE);
        TryPushComboRotation(context, scheduler, target, enemyCount, useAoE);
    }

    #region oGCDs

    private void TryPushFeint(INikeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var player = context.Player;
        if (player.Level < RoleActions.Feint.MinLevel) return;
        if (!context.ActionService.IsActionReady(RoleActions.Feint.ActionId)) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;
        if (coordConfig.EnableCooldownCoordination &&
            partyCoord?.WasActionUsedByOther(RoleActions.Feint.ActionId, 15f) == true)
        {
            return;
        }

        scheduler.PushOgcd(NikeAbilities.Feint, target.GameObjectId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Feint.Name;
                partyCoord?.OnCooldownUsed(RoleActions.Feint.ActionId, 90_000);
            });
    }

    private void TryPushSecondWind(INikeContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.MeleeShared.EnableSecondWind) return;

        RoleActionPushers.TryPushSecondWind(
            context, scheduler, NikeAbilities.SecondWind,
            hpThresholdPct: context.Configuration.MeleeShared.SecondWindHpThreshold,
            priority: 6,
            onDispatched: _ => context.Debug.PlannedAction = RoleActions.SecondWind.Name);
    }

    private void TryPushKenkiSpender(INikeContext context, RotationScheduler scheduler, IBattleChara target, bool useAoE)
    {
        var player = context.Player;
        var level = player.Level;
        if (context.Kenki < context.Configuration.Samurai.KenkiMinGauge) return;
        if (context.Configuration.Samurai.EnableBurstPooling && ShouldHoldForBurst(8f) && context.Kenki < context.Configuration.Samurai.KenkiReserveForBurst) return;

        var shouldSpend = context.Kenki >= context.Configuration.Samurai.KenkiOvercapThreshold || context.Kenki >= 50;
        if (!shouldSpend) return;

        if (useAoE && level >= SAMActions.Kyuten.MinLevel)
        {
            if (!context.Configuration.Samurai.EnableKyuten) return;
            if (!context.ActionService.IsActionReady(SAMActions.Kyuten.ActionId)) return;

            scheduler.PushOgcd(NikeAbilities.Kyuten, player.GameObjectId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SAMActions.Kyuten.Name;
                    context.Debug.DamageState = $"Kyuten (AoE)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Kyuten.ActionId, SAMActions.Kyuten.Name)
                        .AsAoE(0)
                        .Target("AoE group")
                        .Reason($"Spending 25 Kenki on Kyuten",
                            "Kyuten is the AoE Kenki spender. Use to prevent Kenki overcap. " +
                            "Prioritize Senei/Guren on cooldown, then use Kyuten/Shinten for excess.")
                        .Factors(new[] { $"Kenki: {context.Kenki}", "AoE situation", "Avoiding overcap" })
                        .Alternatives(new[] { "Use Shinten (less total damage)", "Hold for Senei/Guren (if soon)" })
                        .Tip("Don't sit at max Kenki. Spend regularly on Shinten/Kyuten.")
                        .Concept("sam_kenki_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_kenki_gauge", true, "AoE Kenki spending");
                });
            return;
        }

        if (level >= SAMActions.Shinten.MinLevel)
        {
            if (!context.Configuration.Samurai.EnableShinten) return;
            if (!context.ActionService.IsActionReady(SAMActions.Shinten.ActionId)) return;

            scheduler.PushOgcd(NikeAbilities.Shinten, target.GameObjectId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SAMActions.Shinten.Name;
                    context.Debug.DamageState = "Shinten";

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
                });
        }
    }

    #endregion

    #region GCDs

    private void TryPushKaeshiNamikiri(INikeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var player = context.Player;
        if (player.Level < SAMActions.KaeshiNamikiri.MinLevel) return;
        if (!context.HasKaeshiNamikiriReady) return;
        if (!context.ActionService.IsActionReady(SAMActions.KaeshiNamikiri.ActionId)) return;

        scheduler.PushGcd(NikeAbilities.KaeshiNamikiri, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SAMActions.KaeshiNamikiri.Name;
                context.Debug.DamageState = "Kaeshi: Namikiri";

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
            });
    }

    private void TryPushTsubameGaeshi(INikeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Samurai.EnableTsubamegaeshi) return;
        var player = context.Player;
        if (player.Level < SAMActions.TsubameGaeshi.MinLevel) return;
        if (!context.HasTsubameGaeshiReady) return;

        var kaeshiAction = SAMActions.GetKaeshiAction(context.LastIaijutsu);
        if (!context.ActionService.IsActionReady(kaeshiAction.ActionId)) return;

        var ability = kaeshiAction.ActionId switch
        {
            var id when id == SAMActions.KaeshiHiganbana.ActionId => NikeAbilities.KaeshiHiganbana,
            var id when id == SAMActions.KaeshiGoken.ActionId => NikeAbilities.KaeshiGoken,
            var id when id == SAMActions.KaeshiSetsugekka.ActionId => NikeAbilities.KaeshiSetsugekka,
            _ => NikeAbilities.KaeshiSetsugekka,
        };

        scheduler.PushGcd(ability, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = kaeshiAction.Name;
                context.Debug.DamageState = kaeshiAction.Name;

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
            });
    }

    private void TryPushOgiNamikiri(INikeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Samurai.EnableOgiNamikiri) return;
        var player = context.Player;
        if (player.Level < SAMActions.OgiNamikiri.MinLevel) return;
        if (!context.HasOgiNamikiriReady) return;
        if (!context.ActionService.IsActionReady(SAMActions.OgiNamikiri.ActionId)) return;

        scheduler.PushGcd(NikeAbilities.OgiNamikiri, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SAMActions.OgiNamikiri.Name;
                context.Debug.DamageState = "Ogi Namikiri";

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
            });
    }

    private void TryPushIaijutsu(INikeContext context, RotationScheduler scheduler, IBattleChara target, bool useAoE)
    {
        if (!context.Configuration.Samurai.EnableIaijutsu) return;
        var player = context.Player;
        var level = player.Level;
        if (context.SenCount == 0) return;

        switch (context.SenCount)
        {
            case 1:
                if (!context.Configuration.Samurai.MaintainHiganbana) return;
                if (level < SAMActions.Higanbana.MinLevel) return;
                if (useAoE) return;
                if (context.HasHiganbanaOnTarget && context.HiganbanaRemaining > context.Configuration.Samurai.HiganbanaRefreshThreshold) return;
                var targetHpPercent = target.MaxHp > 0 ? (float)target.CurrentHp / target.MaxHp : 1f;
                if (targetHpPercent < context.Configuration.Samurai.HiganbanaMinTargetHp) return;
                PushIaijutsu(context, scheduler, target, SAMActions.Higanbana, NikeAbilities.Higanbana, SAMActions.IaijutsuType.Higanbana);
                break;

            case 2:
                if (level < SAMActions.TenkaGoken.MinLevel) return;
                if (!useAoE) return;
                PushIaijutsu(context, scheduler, target, SAMActions.TenkaGoken, NikeAbilities.TenkaGoken, SAMActions.IaijutsuType.TenkaGoken);
                break;

            case 3:
                if (level < SAMActions.MidareSetsugekka.MinLevel) return;
                PushIaijutsu(context, scheduler, target, SAMActions.MidareSetsugekka, NikeAbilities.MidareSetsugekka, SAMActions.IaijutsuType.MidareSetsugekka);
                break;
        }
    }

    private void PushIaijutsu(INikeContext context, RotationScheduler scheduler, IBattleChara target,
                              ActionDefinition action, AbilityBehavior ability, SAMActions.IaijutsuType type)
    {
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        scheduler.PushGcd(ability, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} ({context.SenCount} Sen)";
                context.Debug.LastIaijutsu = type;

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
            });
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
            _ => ("Using Iaijutsu", "Iaijutsu abilities consume Sen.", "Build Sen through combos.", "sam_iaijutsu")
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

    private void TryPushMeikyoFinisher(INikeContext context, RotationScheduler scheduler, IBattleChara target, bool useAoE)
    {
        if (!context.HasMeikyoShisui || context.MeikyoStacks <= 0) return;

        if (useAoE) TryPushAoeMeikyoFinisher(context, scheduler, target);
        else TryPushSingleTargetMeikyoFinisher(context, scheduler, target);
    }

    private void TryPushSingleTargetMeikyoFinisher(INikeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var level = context.Player.Level;

        if (!context.HasGetsu && level >= SAMActions.Gekko.MinLevel
            && context.ActionService.IsActionReady(SAMActions.Gekko.ActionId))
        {
            bool correctPositional = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
            if (context.Configuration.Samurai.EnforcePositionals && !correctPositional && !context.Configuration.Samurai.AllowPositionalLoss) return;
            scheduler.PushGcd(NikeAbilities.Gekko, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SAMActions.Gekko.Name;
                    context.Debug.DamageState = $"Meikyo Gekko {(correctPositional ? "(rear)" : "(WRONG)")}";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Gekko.ActionId, SAMActions.Gekko.Name)
                        .AsPositional(correctPositional, "rear")
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Meikyo Gekko for Getsu Sen",
                            "Gekko grants Getsu (Moon) Sen and has a rear positional for bonus damage. " +
                            "During Meikyo Shisui, you can use finishers directly without combos.")
                        .Factors(new[] { "Meikyo Shisui active", "Need Getsu Sen", correctPositional ? "At rear" : "Not at rear" })
                        .Alternatives(new[] { "Use Kasha instead (need Ka)", "Use Yukikaze (need Setsu)" })
                        .Tip("Gekko = rear, Kasha = flank. Use True North when you can't position.")
                        .Concept("sam_positionals")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_positionals", correctPositional, "Gekko rear positional");
                });
            return;
        }

        if (!context.HasKa && level >= SAMActions.Kasha.MinLevel
            && context.ActionService.IsActionReady(SAMActions.Kasha.ActionId))
        {
            bool correctPositional = context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity;
            if (context.Configuration.Samurai.EnforcePositionals && !correctPositional && !context.Configuration.Samurai.AllowPositionalLoss) return;
            scheduler.PushGcd(NikeAbilities.Kasha, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SAMActions.Kasha.Name;
                    context.Debug.DamageState = $"Meikyo Kasha {(correctPositional ? "(flank)" : "(WRONG)")}";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Kasha.ActionId, SAMActions.Kasha.Name)
                        .AsPositional(correctPositional, "flank")
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Meikyo Kasha for Ka Sen",
                            "Kasha grants Ka (Flower) Sen and has a flank positional for bonus damage. " +
                            "During Meikyo Shisui, you can use finishers directly without combos.")
                        .Factors(new[] { "Meikyo Shisui active", "Need Ka Sen", correctPositional ? "At flank" : "Not at flank" })
                        .Alternatives(new[] { "Use Gekko instead (need Getsu)", "Use Yukikaze (need Setsu)" })
                        .Tip("Kasha = flank, Gekko = rear. Use True North when you can't position.")
                        .Concept("sam_positionals")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_positionals", correctPositional, "Kasha flank positional");
                });
            return;
        }

        if (!context.HasSetsu && level >= SAMActions.Yukikaze.MinLevel
            && context.ActionService.IsActionReady(SAMActions.Yukikaze.ActionId))
        {
            scheduler.PushGcd(NikeAbilities.Yukikaze, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SAMActions.Yukikaze.Name;
                    context.Debug.DamageState = "Meikyo Yukikaze";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Yukikaze.ActionId, SAMActions.Yukikaze.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason("Meikyo Yukikaze for Setsu Sen",
                            "Yukikaze grants Setsu (Snow) Sen during Meikyo Shisui. " +
                            "Use it to build the missing Setsu Sen without a combo chain.")
                        .Factors(new[] { "Meikyo Shisui active", "Need Setsu Sen", "Combo skip" })
                        .Alternatives(new[] { "Use Gekko (need Getsu)", "Use Kasha (need Ka)" })
                        .Tip("Meikyo → Gekko → Kasha → Yukikaze builds all 3 Sen instantly.")
                        .Concept("sam_meikyo_shisui")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_meikyo_shisui", true, "Meikyo Yukikaze Setsu");
                });
            return;
        }

        // Overflow — all Sen held: prefer Gekko
        if (level >= SAMActions.Gekko.MinLevel
            && context.ActionService.IsActionReady(SAMActions.Gekko.ActionId))
        {
            scheduler.PushGcd(NikeAbilities.Gekko, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SAMActions.Gekko.Name;
                    context.Debug.DamageState = "Meikyo Gekko (overflow)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Gekko.ActionId, SAMActions.Gekko.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason("Meikyo Gekko (overflow — all Sen held)",
                            "All Sen are already held. Gekko consumes a Meikyo stack and still deals combo-level damage. " +
                            "Prefer using Meikyo stacks to build missing Sen rather than overflowing.")
                        .Factors(new[] { "Meikyo Shisui active", "All Sen already held", "Using stack to avoid waste" })
                        .Alternatives(new[] { "Delay Meikyo until Sen are spent (better timing)" })
                        .Tip("Activate Meikyo Shisui before spending Sen so all stacks build toward Iaijutsu.")
                        .Concept("sam_meikyo_shisui")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_meikyo_shisui", true, "Meikyo overflow Gekko");
                });
        }
    }

    private void TryPushAoeMeikyoFinisher(INikeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (!context.HasGetsu && level >= SAMActions.Mangetsu.MinLevel
            && context.ActionService.IsActionReady(SAMActions.Mangetsu.ActionId))
        {
            scheduler.PushGcd(NikeAbilities.Mangetsu, player.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SAMActions.Mangetsu.Name;
                    context.Debug.DamageState = "Meikyo Mangetsu";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Mangetsu.ActionId, SAMActions.Mangetsu.Name)
                        .AsAoE(0)
                        .Target("Nearby enemies")
                        .Reason("Meikyo Mangetsu for Getsu Sen (AoE)",
                            "Mangetsu grants Getsu (Moon) Sen and refreshes Fugetsu buff in AoE. " +
                            "During Meikyo Shisui, use it directly without a combo chain.")
                        .Factors(new[] { "Meikyo Shisui active", "Need Getsu Sen", "AoE situation" })
                        .Alternatives(new[] { "Use Oka (need Ka)", "Use ST finishers (fewer enemies)" })
                        .Tip("In AoE, use Mangetsu (Getsu) and Oka (Ka) under Meikyo Shisui.")
                        .Concept("sam_meikyo_shisui")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_meikyo_shisui", true, "AoE Meikyo Mangetsu");
                });
            return;
        }

        if (!context.HasKa && level >= SAMActions.Oka.MinLevel
            && context.ActionService.IsActionReady(SAMActions.Oka.ActionId))
        {
            scheduler.PushGcd(NikeAbilities.Oka, player.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SAMActions.Oka.Name;
                    context.Debug.DamageState = "Meikyo Oka";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Oka.ActionId, SAMActions.Oka.Name)
                        .AsAoE(0)
                        .Target("Nearby enemies")
                        .Reason("Meikyo Oka for Ka Sen (AoE)",
                            "Oka grants Ka (Flower) Sen and refreshes Fuka buff in AoE. " +
                            "During Meikyo Shisui, use it directly without a combo chain.")
                        .Factors(new[] { "Meikyo Shisui active", "Need Ka Sen", "AoE situation" })
                        .Alternatives(new[] { "Use Mangetsu (need Getsu)", "Use ST finishers (fewer enemies)" })
                        .Tip("In AoE, use Mangetsu (Getsu) and Oka (Ka) under Meikyo Shisui.")
                        .Concept("sam_meikyo_shisui")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_meikyo_shisui", true, "AoE Meikyo Oka");
                });
            return;
        }

        if (level >= SAMActions.Mangetsu.MinLevel
            && context.ActionService.IsActionReady(SAMActions.Mangetsu.ActionId))
        {
            scheduler.PushGcd(NikeAbilities.Mangetsu, player.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SAMActions.Mangetsu.Name;
                    context.Debug.DamageState = "Meikyo Mangetsu (overflow)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SAMActions.Mangetsu.ActionId, SAMActions.Mangetsu.Name)
                        .AsAoE(0)
                        .Target("Nearby enemies")
                        .Reason("Meikyo Mangetsu (overflow — Getsu and Ka already held)",
                            "Both Getsu and Ka Sen are held. Mangetsu consumes a Meikyo stack while still dealing AoE damage.")
                        .Factors(new[] { "Meikyo Shisui active", "Getsu and Ka already held", "Using stack to avoid waste" })
                        .Alternatives(new[] { "Delay Meikyo until Sen spent (better timing)" })
                        .Tip("Use Meikyo Shisui after spending Sen so stacks build toward Tenka Goken.")
                        .Concept("sam_meikyo_shisui")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_meikyo_shisui", true, "AoE Meikyo overflow");
                });
        }
    }

    private void TryPushComboRotation(INikeContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount, bool useAoE)
    {
        var level = context.Player.Level;

        // Continue an in-progress AoE combo even if the enemy count dropped below threshold mid-chain.
        if (!useAoE && enemyCount > 0 && IsInAoeCombo(context))
            useAoE = true;

        if (useAoE) TryPushAoeCombo(context, scheduler, target);
        else TryPushSingleTargetCombo(context, scheduler, target);
    }

    private static bool IsInAoeCombo(INikeContext context)
    {
        if (context.ComboStep == 0) return false;
        return context.LastComboAction == SAMActions.Fuga.ActionId ||
               context.LastComboAction == SAMActions.Fuko.ActionId;
    }

    private void TryPushSingleTargetCombo(INikeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;
        var comboStep = context.ComboStep;
        var comboStarter = SAMActions.GetComboStarter((byte)level);
        var starterAbility = comboStarter == SAMActions.Gyofu ? NikeAbilities.Gyofu : NikeAbilities.Hakaze;

        // Step 2 — finishers
        if (comboStep == 2)
        {
            if (context.LastComboAction == SAMActions.Jinpu.ActionId && level >= SAMActions.Gekko.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Gekko.ActionId))
            {
                bool correctPositional = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.Configuration.Samurai.EnforcePositionals && !correctPositional && !context.Configuration.Samurai.AllowPositionalLoss) return;
                scheduler.PushGcd(NikeAbilities.Gekko, target.GameObjectId, priority: 6,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = SAMActions.Gekko.Name;
                        context.Debug.DamageState = $"Gekko {(correctPositional ? "(rear)" : "(WRONG)")}";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(SAMActions.Gekko.ActionId, SAMActions.Gekko.Name)
                            .AsPositional(correctPositional, "rear")
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"Combo finisher Gekko for Getsu Sen",
                                "Gekko is the finisher after Jinpu. Grants Getsu (Moon) Sen and refreshes Fugetsu buff. " +
                                "Has a rear positional for bonus damage and extra Kenki.")
                            .Factors(new[] { "Combo step 2 (after Jinpu)", correctPositional ? "At rear" : "Not at rear", "Grants Getsu Sen" })
                            .Alternatives(new[] { "Break combo (miss finisher)", "Wrong positional (less damage)" })
                            .Tip("Gekko = rear. Position before finisher or use True North.")
                            .Concept("sam_positionals")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("sam_positionals", correctPositional, "Gekko combo rear");
                    });
                return;
            }

            if (context.LastComboAction == SAMActions.Shifu.ActionId && level >= SAMActions.Kasha.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Kasha.ActionId))
            {
                bool correctPositional = context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.Configuration.Samurai.EnforcePositionals && !correctPositional && !context.Configuration.Samurai.AllowPositionalLoss) return;
                scheduler.PushGcd(NikeAbilities.Kasha, target.GameObjectId, priority: 6,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = SAMActions.Kasha.Name;
                        context.Debug.DamageState = $"Kasha {(correctPositional ? "(flank)" : "(WRONG)")}";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(SAMActions.Kasha.ActionId, SAMActions.Kasha.Name)
                            .AsPositional(correctPositional, "flank")
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"Combo finisher Kasha for Ka Sen",
                                "Kasha is the finisher after Shifu. Grants Ka (Flower) Sen and refreshes Fuka buff. " +
                                "Has a flank positional for bonus damage and extra Kenki.")
                            .Factors(new[] { "Combo step 2 (after Shifu)", correctPositional ? "At flank" : "Not at flank", "Grants Ka Sen" })
                            .Alternatives(new[] { "Break combo (miss finisher)", "Wrong positional (less damage)" })
                            .Tip("Kasha = flank. Position before finisher or use True North.")
                            .Concept("sam_positionals")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("sam_positionals", correctPositional, "Kasha combo flank");
                    });
                return;
            }
        }

        // Step 1 — choose Jinpu/Shifu/Yukikaze
        if (comboStep == 1 && (context.LastComboAction == comboStarter.ActionId || context.LastComboAction == SAMActions.Hakaze.ActionId))
        {
            if ((!context.HasFugetsu || context.FugetsuRemaining < 10f) && level >= SAMActions.Jinpu.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Jinpu.ActionId))
            {
                PushComboStep2(context, scheduler, target, SAMActions.Jinpu, NikeAbilities.Jinpu,
                    $"Jinpu to refresh Fugetsu ({(context.HasFugetsu ? $"{context.FugetsuRemaining:F1}s left" : "missing")})",
                    "Jinpu refreshes Fugetsu (+13% damage up). Keep this buff active at all times. " +
                    "Follow with Gekko to grant Getsu Sen and deal positional damage.",
                    new[] { "Combo step 2 (after Hakaze/Gyofu)", context.HasFugetsu ? $"Fugetsu {context.FugetsuRemaining:F1}s" : "Fugetsu missing", "Refreshing buff" },
                    "Keep both Fugetsu and Fuka up. They expire in 40s — refresh before 10s.",
                    "sam_fugetsu_fuka", "Fugetsu refresh via Jinpu");
                return;
            }
            if ((!context.HasFuka || context.FukaRemaining < 10f) && level >= SAMActions.Shifu.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Shifu.ActionId))
            {
                PushComboStep2(context, scheduler, target, SAMActions.Shifu, NikeAbilities.Shifu,
                    $"Shifu to refresh Fuka ({(context.HasFuka ? $"{context.FukaRemaining:F1}s left" : "missing")})",
                    "Shifu refreshes Fuka (+13% haste). Keep this buff active at all times. " +
                    "Follow with Kasha to grant Ka Sen and deal positional damage.",
                    new[] { "Combo step 2 (after Hakaze/Gyofu)", context.HasFuka ? $"Fuka {context.FukaRemaining:F1}s" : "Fuka missing", "Refreshing buff" },
                    "Fuka increases GCD speed. Letting it drop costs DPS.",
                    "sam_fugetsu_fuka", "Fuka refresh via Shifu");
                return;
            }
            if (!context.HasSetsu && level >= SAMActions.Yukikaze.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Yukikaze.ActionId))
            {
                PushComboStep2(context, scheduler, target, SAMActions.Yukikaze, NikeAbilities.Yukikaze,
                    "Yukikaze for Setsu Sen (missing)",
                    "Yukikaze grants Setsu (Snow) Sen when used as a combo finisher. " +
                    "You need all 3 Sen for Midare Setsugekka. No positional required.",
                    new[] { "Combo step 2", "Setsu Sen missing", "No positional needed" },
                    "Yukikaze is the easiest Sen to build — no positional requirement.",
                    "sam_sen_system", "Setsu Sen via Yukikaze");
                return;
            }
            if (!context.HasGetsu && level >= SAMActions.Jinpu.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Jinpu.ActionId))
            {
                PushComboStep2(context, scheduler, target, SAMActions.Jinpu, NikeAbilities.Jinpu,
                    "Jinpu to build Getsu Sen (missing)",
                    "Jinpu leads to Gekko which grants Getsu (Moon) Sen. " +
                    "Also refreshes Fugetsu buff. Has a rear positional for bonus Kenki.",
                    new[] { "Combo step 2", "Getsu Sen missing", "Fugetsu maintained" },
                    "Jinpu → Gekko (rear) builds Getsu and refreshes Fugetsu.",
                    "sam_sen_system", "Getsu path via Jinpu");
                return;
            }
            if (!context.HasKa && level >= SAMActions.Shifu.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Shifu.ActionId))
            {
                PushComboStep2(context, scheduler, target, SAMActions.Shifu, NikeAbilities.Shifu,
                    "Shifu to build Ka Sen (missing)",
                    "Shifu leads to Kasha which grants Ka (Flower) Sen. " +
                    "Also refreshes Fuka buff. Has a flank positional for bonus Kenki.",
                    new[] { "Combo step 2", "Ka Sen missing", "Fuka maintained" },
                    "Shifu → Kasha (flank) builds Ka and refreshes Fuka.",
                    "sam_sen_system", "Ka path via Shifu");
                return;
            }
            // Default to Jinpu
            if (level >= SAMActions.Jinpu.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Jinpu.ActionId))
            {
                PushComboStep2(context, scheduler, target, SAMActions.Jinpu, NikeAbilities.Jinpu,
                    "Jinpu (combo step 2 — default path)",
                    "Jinpu leads to Gekko for Getsu Sen and refreshes Fugetsu. " +
                    "Used as the default mid-combo step when no specific Sen or buff is urgent.",
                    new[] { "Combo step 2", "Buffs maintained", "Default rotation" },
                    "Rotate Jinpu and Shifu paths evenly to maintain both Fugetsu and Fuka.",
                    "sam_fugetsu_fuka", "Default Jinpu path");
                return;
            }
        }

        // Combo starter (Hakaze/Gyofu)
        if (context.ActionService.IsActionReady(comboStarter.ActionId))
        {
            scheduler.PushGcd(starterAbility, target.GameObjectId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = comboStarter.Name;
                    context.Debug.DamageState = $"{comboStarter.Name} (Combo 1)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(comboStarter.ActionId, comboStarter.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"{comboStarter.Name} — starting single-target combo",
                            $"{comboStarter.Name} is SAM's combo opener. It leads to Jinpu, Shifu, or Yukikaze. " +
                            "Always follow up — breaking combo wastes potency.")
                        .Factors(new[] { "Combo step 1", "No active combo", "Starting rotation" })
                        .Alternatives(new[] { "Use Meikyo to skip combo (if active)" })
                        .Tip($"After {comboStarter.Name}: Jinpu → Gekko (rear) or Shifu → Kasha (flank) or Yukikaze.")
                        .Concept("sam_combo_rotation")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_combo_rotation", true, $"Combo starter {comboStarter.Name}");
                });
        }
    }

    private static void PushComboStep2(INikeContext context, RotationScheduler scheduler, IBattleChara target,
                                        ActionDefinition action, AbilityBehavior ability,
                                        string reason, string explanation, string[] factors, string tip,
                                        string concept, string conceptApplication)
    {
        scheduler.PushGcd(ability, target.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (Combo 2)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason(reason, explanation)
                    .Factors(factors)
                    .Alternatives(new[] { "Wait for buff alignment", "Switch combo path" })
                    .Tip(tip)
                    .Concept(concept)
                    .Record();
                context.TrainingService?.RecordConceptApplication(concept, true, conceptApplication);
            });
    }

    private void TryPushAoeCombo(INikeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;
        var comboStep = context.ComboStep;
        var aoeStarter = SAMActions.GetAoeComboStarter((byte)level);
        var starterAbility = aoeStarter == SAMActions.Fuko ? NikeAbilities.Fuko : NikeAbilities.Fuga;

        // Step 1 → Mangetsu/Oka
        if (comboStep == 1 && (context.LastComboAction == aoeStarter.ActionId || context.LastComboAction == SAMActions.Fuga.ActionId))
        {
            if ((!context.HasFugetsu || !context.HasGetsu) && level >= SAMActions.Mangetsu.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Mangetsu.ActionId))
            {
                scheduler.PushGcd(NikeAbilities.Mangetsu, player.GameObjectId, priority: 6,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = SAMActions.Mangetsu.Name;
                        context.Debug.DamageState = "Mangetsu (AoE 2)";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(SAMActions.Mangetsu.ActionId, SAMActions.Mangetsu.Name)
                            .AsAoE(0)
                            .Target("Nearby enemies")
                            .Reason($"Mangetsu for Getsu Sen{(!context.HasFugetsu ? " and Fugetsu" : "")} (AoE combo step 2)",
                                "Mangetsu grants Getsu (Moon) Sen and refreshes Fugetsu buff in AoE. " +
                                "Follow Fuko/Fuga with Mangetsu or Oka based on which Sen and buffs are needed.")
                            .Factors(new[] { "AoE combo step 2", context.HasGetsu ? "Fugetsu missing" : "Getsu Sen missing", "Multiple enemies" })
                            .Alternatives(new[] { "Oka (if Ka or Fuka more urgent)" })
                            .Tip("AoE combo: Fuko/Fuga → Mangetsu (Getsu) or Oka (Ka). Alternate for both.")
                            .Concept("sam_aoe_rotation")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("sam_aoe_rotation", true, "AoE Mangetsu combo");
                    });
                return;
            }
            if ((!context.HasFuka || !context.HasKa) && level >= SAMActions.Oka.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Oka.ActionId))
            {
                scheduler.PushGcd(NikeAbilities.Oka, player.GameObjectId, priority: 6,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = SAMActions.Oka.Name;
                        context.Debug.DamageState = "Oka (AoE 2)";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(SAMActions.Oka.ActionId, SAMActions.Oka.Name)
                            .AsAoE(0)
                            .Target("Nearby enemies")
                            .Reason($"Oka for Ka Sen{(!context.HasFuka ? " and Fuka" : "")} (AoE combo step 2)",
                                "Oka grants Ka (Flower) Sen and refreshes Fuka buff in AoE. " +
                                "Alternate with Mangetsu to maintain both Fugetsu and Fuka.")
                            .Factors(new[] { "AoE combo step 2", context.HasKa ? "Fuka missing" : "Ka Sen missing", "Multiple enemies" })
                            .Alternatives(new[] { "Mangetsu (if Getsu or Fugetsu more urgent)" })
                            .Tip("AoE combo: Fuko/Fuga → Mangetsu (Getsu) or Oka (Ka). Alternate for both.")
                            .Concept("sam_aoe_rotation")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("sam_aoe_rotation", true, "AoE Oka combo");
                    });
                return;
            }
            // Default Mangetsu
            if (level >= SAMActions.Mangetsu.MinLevel
                && context.ActionService.IsActionReady(SAMActions.Mangetsu.ActionId))
            {
                scheduler.PushGcd(NikeAbilities.Mangetsu, player.GameObjectId, priority: 6,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = SAMActions.Mangetsu.Name;
                        context.Debug.DamageState = "Mangetsu (default)";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(SAMActions.Mangetsu.ActionId, SAMActions.Mangetsu.Name)
                            .AsAoE(0)
                            .Target("Nearby enemies")
                            .Reason("Mangetsu (AoE combo step 2 — default)",
                                "Default AoE combo finisher. Refreshes Fugetsu and grants Getsu Sen.")
                            .Factors(new[] { "AoE combo step 2", "Default finisher", "Multiple enemies" })
                            .Alternatives(new[] { "Oka (if Ka or Fuka more urgent)" })
                            .Tip("Keep alternating Mangetsu and Oka to maintain Fugetsu, Fuka, and both Sen.")
                            .Concept("sam_aoe_rotation")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("sam_aoe_rotation", true, "AoE default Mangetsu");
                    });
                return;
            }
        }

        // AoE combo starter
        if (context.ActionService.IsActionReady(aoeStarter.ActionId))
        {
            scheduler.PushGcd(starterAbility, player.GameObjectId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = aoeStarter.Name;
                    context.Debug.DamageState = $"{aoeStarter.Name} (AoE 1)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(aoeStarter.ActionId, aoeStarter.Name)
                        .AsAoE(0)
                        .Target("Nearby enemies")
                        .Reason($"{aoeStarter.Name} — starting AoE combo",
                            $"{aoeStarter.Name} is the AoE combo opener. Follow with Mangetsu (Getsu) or Oka (Ka). " +
                            "Use instead of single-target combo at 3+ enemies.")
                        .Factors(new[] { "AoE combo step 1", "Multiple enemies", "Starting AoE rotation" })
                        .Alternatives(new[] { "Use Hakaze/Gyofu (fewer enemies)" })
                        .Tip($"AoE opener: {aoeStarter.Name} → Mangetsu or Oka → Tenka Goken.")
                        .Concept("sam_aoe_rotation")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("sam_aoe_rotation", true, $"AoE starter {aoeStarter.Name}");
                });
        }
    }

    #endregion
}
