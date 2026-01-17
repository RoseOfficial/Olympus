using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.NikeCore.Context;

namespace Olympus.Rotation.NikeCore.Modules;

/// <summary>
/// Handles the Samurai damage rotation.
/// Manages combos, Iaijutsu, Kaeshi, Ogi Namikiri, and Kenki spenders.
/// </summary>
public sealed class DamageModule : INikeModule
{
    public int Priority => 30; // After Buffs
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    // Kenki threshold for spending (Shinten/Kyuten cost 25)
    private const int KenkiSpendThreshold = 25;

    // Higanbana refresh threshold (DoT is 60s)
    private const float HiganbanaRefreshThreshold = 5f;

    public bool TryExecute(INikeContext context, bool isMoving)
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

        // oGCD Phase - weave Kenki spenders during GCD
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

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(INikeContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Damage

    private bool TryOgcdDamage(INikeContext context, IBattleChara target, int enemyCount)
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
                    return true;
                }
            }
        }

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
            return true;
        }

        return false;
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
