using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.KratosCore.Context;

namespace Olympus.Rotation.KratosCore.Modules;

/// <summary>
/// Handles the Monk damage rotation.
/// Manages form cycling, positional optimization, Chakra spending, and burst windows.
/// </summary>
public sealed class DamageModule : IKratosModule
{
    public int Priority => 30; // Lowest priority - damage after utility
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IKratosContext context, bool isMoving)
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

        // Priority 1: Masterful Blitz variants (3 Beast Chakra accumulated)
        if (TryMasterfulBlitz(context, target, enemyCount))
            return true;

        // Priority 2: Fire's Rumination proc (after Riddle of Fire ends)
        if (TryFiresRumination(context, target))
            return true;

        // Priority 3: Wind's Rumination proc (after Riddle of Wind ends)
        if (TryWindsRumination(context, target))
            return true;

        // Priority 4: Perfect Balance form selection
        if (TryPerfectBalanceAction(context, target, enemyCount))
            return true;

        // Priority 5: Form-based rotation
        if (TryFormRotation(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IKratosContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Damage

    private bool TryOgcdDamage(IKratosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: The Forbidden Chakra / Enlightenment (5 Chakra)
        if (TryChakraSpender(context, target, enemyCount))
            return true;

        // Priority 2: Thunderclap (gap closer)
        if (TryThunderclap(context, target))
            return true;

        return false;
    }

    private bool TryChakraSpender(IKratosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need 5 Chakra to spend
        if (context.Chakra < 5)
            return false;

        // Choose ST or AoE based on enemy count
        if (enemyCount >= AoeThreshold && level >= MNKActions.HowlingFist.MinLevel)
        {
            // Use AoE Chakra spender
            var aoeAction = MNKActions.GetAoeChakraSpender(level);
            if (context.ActionService.IsActionReady(aoeAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(aoeAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = aoeAction.Name;
                    context.Debug.DamageState = $"{aoeAction.Name} ({enemyCount} enemies)";
                    return true;
                }
            }
        }
        else if (level >= MNKActions.SteelPeak.MinLevel)
        {
            // Use ST Chakra spender
            var stAction = MNKActions.GetChakraSpender(level);
            if (context.ActionService.IsActionReady(stAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(stAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = stAction.Name;
                    context.Debug.DamageState = stAction.Name;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryThunderclap(IKratosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.Thunderclap.MinLevel)
            return false;

        // Check distance - Thunderclap is a gap closer with 20y range
        var dx = player.Position.X - target.Position.X;
        var dz = player.Position.Z - target.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dz * dz);

        // Only use to close gap if out of melee range
        if (distance <= FFXIVConstants.MeleeTargetingRange)
            return false;

        if (distance > 20f)
            return false;

        if (!context.ActionService.IsActionReady(MNKActions.Thunderclap.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(MNKActions.Thunderclap, target.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.Thunderclap.Name;
            context.Debug.DamageState = "Thunderclap (gap close)";
            return true;
        }

        return false;
    }

    #endregion

    #region Masterful Blitz

    private bool TryMasterfulBlitz(IKratosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Blitz actions start at level 60 (Elixir Field/Flint Strike)
        if (level < MNKActions.ElixirField.MinLevel)
            return false;

        // Need 3 Beast Chakra to use Masterful Blitz
        if (context.BeastChakraCount < 3)
            return false;

        // Get the appropriate Blitz action based on Beast Chakra composition
        var blitzAction = MNKActions.GetBlitzAction(
            (byte)level,
            context.HasLunarNadi,
            context.HasSolarNadi,
            (MNKActions.BeastChakraType)context.BeastChakra1,
            (MNKActions.BeastChakraType)context.BeastChakra2,
            (MNKActions.BeastChakraType)context.BeastChakra3);

        if (blitzAction == null)
            return false;

        if (!context.ActionService.IsActionReady(blitzAction.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(blitzAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = blitzAction.Name;
            context.Debug.DamageState = $"{blitzAction.Name} (Blitz)";
            return true;
        }

        return false;
    }

    #endregion

    #region Rumination Procs

    private bool TryFiresRumination(IKratosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.FiresReply.MinLevel)
            return false;

        // Fire's Rumination proc is ready
        if (!context.HasFiresRumination)
            return false;

        if (!context.ActionService.IsActionReady(MNKActions.FiresReply.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MNKActions.FiresReply, target.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.FiresReply.Name;
            context.Debug.DamageState = "Fire's Reply";
            return true;
        }

        return false;
    }

    private bool TryWindsRumination(IKratosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.WindsReply.MinLevel)
            return false;

        // Wind's Rumination proc is ready
        if (!context.HasWindsRumination)
            return false;

        if (!context.ActionService.IsActionReady(MNKActions.WindsReply.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MNKActions.WindsReply, target.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.WindsReply.Name;
            context.Debug.DamageState = "Wind's Reply";
            return true;
        }

        return false;
    }

    #endregion

    #region Perfect Balance

    private bool TryPerfectBalanceAction(IKratosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Perfect Balance allows using any form GCD
        if (!context.HasPerfectBalance)
            return false;

        if (context.PerfectBalanceStacks <= 0)
            return false;

        // During Perfect Balance, select GCDs to build towards desired Blitz
        // Strategy: Depends on current Nadi state
        // - No Nadi: Build Lunar (3 different = Elixir Field)
        // - Lunar only: Build Solar (2 same + 1 different = Flint Strike/Rising Phoenix)
        // - Solar only: Build Lunar (3 different = Elixir Field)
        // - Both Nadi: Build 3 of same type for Phantom Rush

        var action = GetPerfectBalanceAction(context, enemyCount);
        if (action == null)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (PB: {context.PerfectBalanceStacks} stacks)";
            return true;
        }

        return false;
    }

    private ActionDefinition? GetPerfectBalanceAction(IKratosContext context, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Count current Beast Chakra types
        var hasOpo = context.BeastChakra1 == 1 || context.BeastChakra2 == 1 || context.BeastChakra3 == 1;
        var hasRaptor = context.BeastChakra1 == 2 || context.BeastChakra2 == 2 || context.BeastChakra3 == 2;
        var hasCoeurl = context.BeastChakra1 == 3 || context.BeastChakra2 == 3 || context.BeastChakra3 == 3;

        // Determine what we're building towards
        if (context.HasBothNadi)
        {
            // Building for Phantom Rush - need 3 of same type
            // Use Opo-opo GCDs (highest potency)
            return useAoe
                ? (level >= MNKActions.ShadowOfTheDestroyer.MinLevel ? MNKActions.ShadowOfTheDestroyer : MNKActions.ArmOfTheDestroyer)
                : GetOpoOpoAction(context, level);
        }
        else if (context.HasLunarNadi)
        {
            // Have Lunar, need Solar - build 2 same + 1 different
            // Use Opo-opo twice, then something else
            if (!hasOpo || (hasOpo && !hasRaptor && !hasCoeurl))
            {
                return useAoe
                    ? (level >= MNKActions.ShadowOfTheDestroyer.MinLevel ? MNKActions.ShadowOfTheDestroyer : MNKActions.ArmOfTheDestroyer)
                    : GetOpoOpoAction(context, level);
            }
            else
            {
                // Use a different form to complete Solar
                return useAoe
                    ? (level >= MNKActions.FourPointFury.MinLevel ? MNKActions.FourPointFury : MNKActions.TwinSnakes)
                    : GetRaptorAction(context, level);
            }
        }
        else
        {
            // Need Lunar first - build 3 different types
            if (!hasOpo)
            {
                return useAoe
                    ? (level >= MNKActions.ShadowOfTheDestroyer.MinLevel ? MNKActions.ShadowOfTheDestroyer : MNKActions.ArmOfTheDestroyer)
                    : GetOpoOpoAction(context, level);
            }
            else if (!hasRaptor)
            {
                return useAoe
                    ? (level >= MNKActions.FourPointFury.MinLevel ? MNKActions.FourPointFury : MNKActions.TwinSnakes)
                    : GetRaptorAction(context, level);
            }
            else if (!hasCoeurl)
            {
                return useAoe
                    ? (level >= MNKActions.Rockbreaker.MinLevel ? MNKActions.Rockbreaker : MNKActions.SnapPunch)
                    : GetCoeurlAction(context, level);
            }
        }

        // Fallback to Opo-opo
        return useAoe
            ? (level >= MNKActions.ShadowOfTheDestroyer.MinLevel ? MNKActions.ShadowOfTheDestroyer : MNKActions.ArmOfTheDestroyer)
            : GetOpoOpoAction(context, level);
    }

    #endregion

    #region Form Rotation

    private bool TryFormRotation(IKratosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Determine current form and execute appropriate action
        var form = context.CurrentForm;

        // Formless Fist allows any form - start with Opo-opo
        if (context.HasFormlessFist || form == MonkForm.Formless || form == MonkForm.None)
        {
            return TryOpoOpoForm(context, target, useAoe);
        }

        switch (form)
        {
            case MonkForm.OpoOpo:
                return TryOpoOpoForm(context, target, useAoe);

            case MonkForm.Raptor:
                return TryRaptorForm(context, target, useAoe);

            case MonkForm.Coeurl:
                return TryCoeurlForm(context, target, useAoe);

            default:
                // No form - start with Opo-opo
                return TryOpoOpoForm(context, target, useAoe);
        }
    }

    private bool TryOpoOpoForm(IKratosContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        if (useAoe)
        {
            // AoE: Arm of the Destroyer / Shadow of the Destroyer
            var aoeAction = level >= MNKActions.ShadowOfTheDestroyer.MinLevel
                ? MNKActions.ShadowOfTheDestroyer
                : MNKActions.ArmOfTheDestroyer;

            if (context.ActionService.IsActionReady(aoeAction.ActionId))
            {
                if (context.ActionService.ExecuteGcd(aoeAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = aoeAction.Name;
                    context.Debug.DamageState = $"{aoeAction.Name} (Opo-opo)";
                    return true;
                }
            }
        }

        // Single target: Choose based on procs and buffs
        var action = GetOpoOpoAction(context, level);

        if (context.ActionService.IsActionReady(action.ActionId))
        {
            string positional = action == MNKActions.Bootshine || action == MNKActions.LeapingOpo
                ? "(rear)" : "(flank)";
            bool correctPositional = action == MNKActions.Bootshine || action == MNKActions.LeapingOpo
                ? (context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity)
                : (context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity);

            if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} {positional}";
                return true;
            }
        }

        return false;
    }

    private bool TryRaptorForm(IKratosContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        if (useAoe)
        {
            // AoE: Four-point Fury
            if (level >= MNKActions.FourPointFury.MinLevel)
            {
                if (context.ActionService.IsActionReady(MNKActions.FourPointFury.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(MNKActions.FourPointFury, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = MNKActions.FourPointFury.Name;
                        context.Debug.DamageState = "Four-point Fury (Raptor)";
                        return true;
                    }
                }
            }
        }

        // Single target: Choose based on Disciplined Fist duration
        var action = GetRaptorAction(context, level);

        if (context.ActionService.IsActionReady(action.ActionId))
        {
            string positional = action == MNKActions.TrueStrike || action == MNKActions.RisingRaptor
                ? "(rear)" : "(flank)";

            if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} {positional}";
                return true;
            }
        }

        return false;
    }

    private bool TryCoeurlForm(IKratosContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        if (useAoe)
        {
            // AoE: Rockbreaker
            if (level >= MNKActions.Rockbreaker.MinLevel)
            {
                if (context.ActionService.IsActionReady(MNKActions.Rockbreaker.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(MNKActions.Rockbreaker, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = MNKActions.Rockbreaker.Name;
                        context.Debug.DamageState = "Rockbreaker (Coeurl)";
                        return true;
                    }
                }
            }
        }

        // Single target: Choose based on Demolish DoT
        var action = GetCoeurlAction(context, level);

        if (context.ActionService.IsActionReady(action.ActionId))
        {
            string positional = action == MNKActions.Demolish || action == MNKActions.PouncingCoeurl
                ? "(rear)" : "(flank)";

            if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} {positional}";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Action Selection Helpers

    private ActionDefinition GetOpoOpoAction(IKratosContext context, uint level)
    {
        // Dawntrail upgrades: Bootshine -> Leaping Opo, Dragon Kick -> (no upgrade)
        // Leaden Fist proc: Use Bootshine/Leaping Opo (rear)
        // Opo-opo's Fury proc: Skip Leaden Fist check
        // No proc: Use Dragon Kick (flank) to get Leaden Fist

        if (context.HasOpooposFury)
        {
            // With Opo-opo's Fury, prefer Leaping Opo/Bootshine for damage
            if (level >= MNKActions.LeapingOpo.MinLevel)
                return MNKActions.LeapingOpo;
            return MNKActions.Bootshine;
        }

        if (context.HasLeadenFist)
        {
            // Leaden Fist buff - use Bootshine/Leaping Opo to consume it
            if (level >= MNKActions.LeapingOpo.MinLevel)
                return MNKActions.LeapingOpo;
            return MNKActions.Bootshine;
        }

        // No Leaden Fist - use Dragon Kick to get it
        if (level >= MNKActions.DragonKick.MinLevel)
            return MNKActions.DragonKick;

        // Low level fallback
        return MNKActions.Bootshine;
    }

    private ActionDefinition GetRaptorAction(IKratosContext context, uint level)
    {
        // Dawntrail upgrades: True Strike -> Rising Raptor, Twin Snakes -> (no upgrade)
        // Raptor's Fury proc: Use any Raptor GCD
        // Disciplined Fist < 5s: Use Twin Snakes (flank) to refresh
        // Otherwise: Use True Strike/Rising Raptor (rear) for damage

        // Always maintain Disciplined Fist if it's low or missing
        if (!context.HasDisciplinedFist || context.DisciplinedFistRemaining < 5f)
        {
            if (level >= MNKActions.TwinSnakes.MinLevel)
                return MNKActions.TwinSnakes;
        }

        // With Raptor's Fury or sufficient buff, use damage action
        if (level >= MNKActions.RisingRaptor.MinLevel)
            return MNKActions.RisingRaptor;

        if (level >= MNKActions.TrueStrike.MinLevel)
            return MNKActions.TrueStrike;

        // Low level fallback
        return level >= MNKActions.TwinSnakes.MinLevel ? MNKActions.TwinSnakes : MNKActions.TrueStrike;
    }

    private ActionDefinition GetCoeurlAction(IKratosContext context, uint level)
    {
        // Dawntrail upgrades: Snap Punch -> Pouncing Coeurl, Demolish -> (no upgrade)
        // Coeurl's Fury proc: Use any Coeurl GCD
        // Demolish DoT < 3s: Use Demolish (rear) to refresh
        // Otherwise: Use Snap Punch/Pouncing Coeurl (flank) for damage

        // Apply/refresh Demolish if needed
        if (!context.HasDemolishOnTarget || context.DemolishRemaining < 3f)
        {
            if (level >= MNKActions.Demolish.MinLevel)
                return MNKActions.Demolish;
        }

        // With Coeurl's Fury or sufficient DoT, use damage action
        if (level >= MNKActions.PouncingCoeurl.MinLevel)
            return MNKActions.PouncingCoeurl;

        if (level >= MNKActions.SnapPunch.MinLevel)
            return MNKActions.SnapPunch;

        // Low level fallback
        return level >= MNKActions.Demolish.MinLevel ? MNKActions.Demolish : MNKActions.SnapPunch;
    }

    #endregion
}
