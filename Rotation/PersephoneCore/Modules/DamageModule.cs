using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.PersephoneCore.Context;

namespace Olympus.Rotation.PersephoneCore.Modules;

/// <summary>
/// Handles the Summoner damage rotation.
/// Manages demi-summon phases, primal attunements, and filler spells.
/// </summary>
public sealed class DamageModule : IPersephoneModule
{
    public int Priority => 30; // Lower priority than buffs (higher number = lower priority)
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IPersephoneContext context, bool isMoving)
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
            FFXIVConstants.CasterTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        var useAoe = enemyCount >= AoeThreshold;

        // === PRIORITY 1: DEMI-SUMMON PHASE GCDs ===
        if (context.IsDemiSummonActive)
        {
            if (TryDemiSummonGcd(context, target, useAoe))
                return true;
        }

        // === PRIORITY 2: PRIMAL ATTUNEMENT GCDs (Gemshine) ===
        if (context.IsIfritAttuned || context.IsTitanAttuned || context.IsGarudaAttuned)
        {
            if (TryAttunementGcd(context, target, useAoe, isMoving))
                return true;
        }

        // === PRIORITY 3: PRIMAL FAVOR ABILITIES ===
        if (TryPrimalFavor(context, target, isMoving))
            return true;

        // === PRIORITY 4: SUMMON NEXT PRIMAL ===
        if (context.PrimalsAvailable > 0 && !context.IsDemiSummonActive)
        {
            if (TrySummonPrimal(context))
                return true;
        }

        // === PRIORITY 5: SUMMON DEMI (when no primals available) ===
        if (context.PrimalsAvailable == 0 && !context.IsDemiSummonActive)
        {
            if (TrySummonDemi(context))
                return true;
        }

        // === PRIORITY 6: RUIN IV (Further Ruin proc) ===
        // Only use between phases, not during demi-summon
        if (!context.IsDemiSummonActive && context.HasFurtherRuin)
        {
            if (TryRuin4(context, target))
                return true;
        }

        // === PRIORITY 7: FILLER (Ruin III / Tri-disaster) ===
        if (TryFillerGcd(context, target, useAoe, isMoving))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IPersephoneContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Demi-Summon Phase

    private bool TryDemiSummonGcd(IPersephoneContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        // Get the appropriate demi-summon GCD
        var action = SMNActions.GetDemiSummonGcd(
            context.IsBahamutActive,
            context.IsSolarBahamutActive,
            useAoe);

        if (level < action.MinLevel)
        {
            // Fallback to Ruin III if demi GCD not unlocked
            action = useAoe ? SMNActions.GetAoeSpell(level) : SMNActions.GetRuinSpell(level);
        }

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (Demi phase)";
            return true;
        }

        return false;
    }

    #endregion

    #region Primal Attunement

    private bool TryAttunementGcd(IPersephoneContext context, IBattleChara target, bool useAoe, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        // Get Gemshine action based on current attunement
        var action = SMNActions.GetGemshinAction(context.CurrentAttunement, useAoe);
        if (action == null)
            return false;

        // Ruby Rite has a cast time - check for movement
        if (context.IsIfritAttuned && isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            // Use Swiftcast for Ruby Rite if moving
            if (context.SwiftcastReady)
            {
                if (context.ActionService.ExecuteOgcd(SMNActions.Swiftcast, player.GameObjectId))
                {
                    context.Debug.DamageState = "Swiftcast for Ruby";
                    return true;
                }
            }
            // Otherwise, skip and use movement filler
            context.Debug.DamageState = "Moving, need instant for Ruby";
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} ({context.Debug.AttunementName} {context.AttunementStacks - 1})";
            return true;
        }

        return false;
    }

    #endregion

    #region Primal Favor Abilities

    private bool TryPrimalFavor(IPersephoneContext context, IBattleChara target, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        // Crimson Cyclone + Strike (Ifrit's Favor) - gap closer combo
        if (context.HasIfritsFavor && level >= SMNActions.CrimsonCyclone.MinLevel)
        {
            // Crimson Cyclone is a gap closer, safe to use when moving
            if (context.ActionService.ExecuteGcd(SMNActions.CrimsonCyclone, target.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.CrimsonCyclone.Name;
                context.Debug.DamageState = "Crimson Cyclone (gap closer)";
                return true;
            }
        }

        // Crimson Strike follow-up (after Crimson Cyclone)
        // This is handled by the game automatically when Crimson Cyclone changes to Strike

        // Slipstream (Garuda's Favor) - channeled ability
        if (context.HasGarudasFavor && level >= SMNActions.Slipstream.MinLevel)
        {
            // Slipstream is a cast, check for movement
            if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
            {
                // Use Swiftcast for Slipstream if available
                if (context.SwiftcastReady)
                {
                    if (context.ActionService.ExecuteOgcd(SMNActions.Swiftcast, player.GameObjectId))
                    {
                        context.Debug.DamageState = "Swiftcast for Slipstream";
                        return true;
                    }
                }
                context.Debug.DamageState = "Moving, hold Slipstream";
                return false;
            }

            if (context.ActionService.ExecuteGcd(SMNActions.Slipstream, target.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.Slipstream.Name;
                context.Debug.DamageState = "Slipstream";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Summon Primal

    private bool TrySummonPrimal(IPersephoneContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Don't summon if we're in attunement or demi phase
        if (context.IsDemiSummonActive || context.AttunementStacks > 0)
            return false;

        // Primal priority for opener: Titan > Garuda > Ifrit
        // Titan first for instant GCDs during burst
        // After opener, order matters less but Titan is good for movement

        if (context.CanSummonTitan && level >= SMNActions.SummonTitan.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.SummonTitan, player.GameObjectId))
            {
                context.Debug.PlannedAction = "Summon Titan";
                context.Debug.DamageState = "Summon Titan";
                return true;
            }
        }

        if (context.CanSummonGaruda && level >= SMNActions.SummonGaruda.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.SummonGaruda, player.GameObjectId))
            {
                context.Debug.PlannedAction = "Summon Garuda";
                context.Debug.DamageState = "Summon Garuda";
                return true;
            }
        }

        if (context.CanSummonIfrit && level >= SMNActions.SummonIfrit.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.SummonIfrit, player.GameObjectId))
            {
                context.Debug.PlannedAction = "Summon Ifrit";
                context.Debug.DamageState = "Summon Ifrit";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Summon Demi

    private bool TrySummonDemi(IPersephoneContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Don't summon if we still have primals or attunement
        if (context.PrimalsAvailable > 0 || context.AttunementStacks > 0)
            return false;

        // Check which demi-summon is available
        // At level 100, Solar Bahamut replaces every other Bahamut
        if (level >= SMNActions.SummonSolarBahamut.MinLevel)
        {
            // Try Solar Bahamut first (the game handles the alternation)
            if (context.ActionService.IsActionReady(SMNActions.SummonSolarBahamut.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SMNActions.SummonSolarBahamut, player.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.SummonSolarBahamut.Name;
                    context.Debug.DamageState = "Summon Solar Bahamut";
                    return true;
                }
            }
        }

        // Phoenix (Lv.80+)
        if (level >= SMNActions.SummonPhoenix.MinLevel)
        {
            if (context.ActionService.IsActionReady(SMNActions.SummonPhoenix.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SMNActions.SummonPhoenix, player.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.SummonPhoenix.Name;
                    context.Debug.DamageState = "Summon Phoenix";
                    return true;
                }
            }
        }

        // Bahamut (Lv.70+)
        if (level >= SMNActions.SummonBahamut.MinLevel)
        {
            if (context.ActionService.IsActionReady(SMNActions.SummonBahamut.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SMNActions.SummonBahamut, player.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.SummonBahamut.Name;
                    context.Debug.DamageState = "Summon Bahamut";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Ruin IV

    private bool TryRuin4(IPersephoneContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.Ruin4.MinLevel)
            return false;

        if (!context.HasFurtherRuin)
            return false;

        // Don't use during demi-summon phase (waste of GCDs)
        if (context.IsDemiSummonActive)
            return false;

        // Use if Further Ruin is about to expire
        if (context.FurtherRuinRemaining < 5f)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.Ruin4, target.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.Ruin4.Name;
                context.Debug.DamageState = $"Ruin IV (expiring: {context.FurtherRuinRemaining:F1}s)";
                return true;
            }
        }

        // Use as filler between phases
        if (!context.IsDemiSummonActive && context.PrimalsAvailable == 0 && context.AttunementStacks == 0)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.Ruin4, target.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.Ruin4.Name;
                context.Debug.DamageState = "Ruin IV (filler)";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Filler GCDs

    private bool TryFillerGcd(IPersephoneContext context, IBattleChara target, bool useAoe, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        // Ruin III has a cast time - use Ruin II for movement if needed
        if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            // Use Ruin IV if available (instant)
            if (context.HasFurtherRuin && level >= SMNActions.Ruin4.MinLevel)
            {
                if (context.ActionService.ExecuteGcd(SMNActions.Ruin4, target.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.Ruin4.Name;
                    context.Debug.DamageState = "Ruin IV (movement)";
                    return true;
                }
            }

            // Use Ruin II for movement (instant, lower potency)
            if (level >= SMNActions.Ruin2.MinLevel)
            {
                if (context.ActionService.ExecuteGcd(SMNActions.Ruin2, target.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.Ruin2.Name;
                    context.Debug.DamageState = "Ruin II (movement)";
                    return true;
                }
            }
        }

        // Standard filler
        var action = useAoe ? SMNActions.GetAoeSpell(level) : SMNActions.GetRuinSpell(level);

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = action.Name;
            return true;
        }

        return false;
    }

    #endregion
}
