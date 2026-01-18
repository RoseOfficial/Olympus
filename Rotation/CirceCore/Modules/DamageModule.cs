using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.CirceCore.Context;

namespace Olympus.Rotation.CirceCore.Modules;

/// <summary>
/// Handles the Red Mage damage rotation.
/// Manages Dualcast flow, melee combo, finishers, and mana balance.
/// </summary>
public sealed class DamageModule : ICirceModule
{
    public int Priority => 30; // Lower priority than buffs (higher number = lower priority)
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(ICirceContext context, bool isMoving)
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

        // === PRIORITY 1: RESOLUTION (after Scorch) ===
        if (context.IsResolutionReady && level >= RDMActions.Resolution.MinLevel)
        {
            if (TryResolution(context, target))
                return true;
        }

        // === PRIORITY 2: SCORCH (after Verflare/Verholy) ===
        if (context.IsScorchReady && level >= RDMActions.Scorch.MinLevel)
        {
            if (TryScorch(context, target))
                return true;
        }

        // === PRIORITY 3: GRAND IMPACT (when ready) ===
        if (context.IsGrandImpactReady && level >= RDMActions.GrandImpact.MinLevel)
        {
            if (TryGrandImpact(context, target))
                return true;
        }

        // === PRIORITY 4: VERFLARE/VERHOLY (after Redoublement) ===
        if (context.IsFinisherReady && level >= RDMActions.Verflare.MinLevel)
        {
            if (TryFinisher(context, target))
                return true;
        }

        // === PRIORITY 5: MELEE COMBO (Enchanted Riposte → Zwerchhau → Redoublement) ===
        if (context.IsInMeleeCombo)
        {
            if (TryMeleeCombo(context, target))
                return true;
        }

        // === PRIORITY 6: START MELEE COMBO (at 50|50 mana) ===
        if (context.CanStartMeleeCombo && !context.IsInMeleeCombo)
        {
            if (TryStartMeleeCombo(context, target))
                return true;
        }

        // === PRIORITY 7: DUALCAST CONSUMER (Verthunder/Veraero or Proc) ===
        if (context.HasDualcast || context.HasSwiftcast || context.HasAcceleration)
        {
            if (TryDualcastConsumer(context, target, useAoe))
                return true;
        }

        // === PRIORITY 8: HARDCAST FILLER (Jolt or AoE) ===
        if (TryHardcastFiller(context, target, useAoe, isMoving))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(ICirceContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Finisher Sequence

    private bool TryResolution(ICirceContext context, IBattleChara target)
    {
        if (context.ActionService.ExecuteGcd(RDMActions.Resolution, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Resolution.Name;
            context.Debug.DamageState = "Resolution (final finisher)";
            return true;
        }
        return false;
    }

    private bool TryScorch(ICirceContext context, IBattleChara target)
    {
        if (context.ActionService.ExecuteGcd(RDMActions.Scorch, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Scorch.Name;
            context.Debug.DamageState = "Scorch (after Verflare/Verholy)";
            return true;
        }
        return false;
    }

    private bool TryGrandImpact(ICirceContext context, IBattleChara target)
    {
        if (context.ActionService.ExecuteGcd(RDMActions.GrandImpact, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.GrandImpact.Name;
            context.Debug.DamageState = "Grand Impact";
            return true;
        }
        return false;
    }

    private bool TryFinisher(ICirceContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        // Select finisher based on mana balance
        // Use the one that generates the LOWER mana type
        var finisher = RDMActions.GetFinisher(level, context.BlackMana, context.WhiteMana);

        if (context.ActionService.ExecuteGcd(finisher, target.GameObjectId))
        {
            context.Debug.PlannedAction = finisher.Name;
            context.Debug.DamageState = $"{finisher.Name} (finisher)";
            return true;
        }
        return false;
    }

    #endregion

    #region Melee Combo

    private bool TryMeleeCombo(ICirceContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        // Determine which combo action is next based on combo state
        switch (context.MeleeComboStep)
        {
            case 1: // After Riposte, use Zwerchhau
                if (level >= RDMActions.EnchantedZwerchhau.MinLevel)
                {
                    if (context.ActionService.ExecuteGcd(RDMActions.EnchantedZwerchhau, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = RDMActions.EnchantedZwerchhau.Name;
                        context.Debug.DamageState = "Enchanted Zwerchhau (combo 2)";
                        return true;
                    }
                }
                break;

            case 2: // After Zwerchhau, use Redoublement
                if (level >= RDMActions.EnchantedRedoublement.MinLevel)
                {
                    if (context.ActionService.ExecuteGcd(RDMActions.EnchantedRedoublement, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = RDMActions.EnchantedRedoublement.Name;
                        context.Debug.DamageState = "Enchanted Redoublement (combo 3)";
                        return true;
                    }
                }
                break;
        }

        return false;
    }

    private bool TryStartMeleeCombo(ICirceContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        // Check conditions for entering melee combo
        // Prefer to enter during burst windows or when mana is high
        var inBurst = context.HasEmbolden || context.HasManafication;
        var highMana = context.LowerMana >= 80;
        var verySoon = context.LowerMana >= 90; // About to cap

        // Always enter if about to cap on mana or in burst
        // Otherwise wait a bit to align with buffs
        if (!inBurst && !highMana && !verySoon)
        {
            // Check if Embolden is coming off cooldown soon
            if (context.EmboldenReady)
            {
                context.Debug.DamageState = "Hold melee for Embolden";
                return false;
            }
        }

        // Start with Enchanted Riposte
        if (context.ActionService.ExecuteGcd(RDMActions.EnchantedRiposte, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.EnchantedRiposte.Name;
            context.Debug.DamageState = "Enchanted Riposte (combo start)";
            return true;
        }

        return false;
    }

    #endregion

    #region Dualcast Consumer

    private bool TryDualcastConsumer(ICirceContext context, IBattleChara target, bool useAoe)
    {
        var level = context.Player.Level;

        // Priority for instant cast:
        // 1. Use proc if available and about to expire
        // 2. Use proc to balance mana
        // 3. Use long spell to balance mana

        // Check for procs
        var procSpell = RDMActions.GetProcSpell(
            level,
            context.HasVerfire,
            context.HasVerstone,
            context.VerfireRemaining,
            context.VerstoneRemaining,
            context.BlackMana,
            context.WhiteMana);

        // If we have Acceleration, procs are guaranteed after the long spell
        // So prefer the long spell to generate procs
        if (context.HasAcceleration && !context.HasAnyProc)
        {
            // Use long spell to generate procs
            return TryLongSpell(context, target, useAoe);
        }

        // If proc is about to expire, use it
        if (procSpell != null)
        {
            var procExpiring = (context.HasVerfire && context.VerfireRemaining < 5f) ||
                               (context.HasVerstone && context.VerstoneRemaining < 5f);

            if (procExpiring)
            {
                if (context.ActionService.ExecuteGcd(procSpell, target.GameObjectId))
                {
                    context.Debug.PlannedAction = procSpell.Name;
                    context.Debug.DamageState = $"{procSpell.Name} (expiring proc)";
                    return true;
                }
            }
        }

        // Use long spell with Dualcast
        return TryLongSpell(context, target, useAoe);
    }

    private bool TryLongSpell(ICirceContext context, IBattleChara target, bool useAoe)
    {
        var level = context.Player.Level;

        if (useAoe && level >= RDMActions.Impact.MinLevel)
        {
            // Use Impact for AoE
            if (context.ActionService.ExecuteGcd(RDMActions.Impact, target.GameObjectId))
            {
                context.Debug.PlannedAction = RDMActions.Impact.Name;
                context.Debug.DamageState = "Impact (AoE)";
                return true;
            }
        }

        // Single target: Use Verthunder or Veraero based on mana balance
        var longSpell = RDMActions.GetBalancedLongSpell(level, context.BlackMana, context.WhiteMana);

        if (context.ActionService.ExecuteGcd(longSpell, target.GameObjectId))
        {
            context.Debug.PlannedAction = longSpell.Name;
            context.Debug.DamageState = $"{longSpell.Name} (Dualcast)";
            return true;
        }

        return false;
    }

    #endregion

    #region Hardcast Filler

    private bool TryHardcastFiller(ICirceContext context, IBattleChara target, bool useAoe, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        // If moving and can't slidecast, use instant options
        if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            // Check for procs first (they're instant)
            var procSpell = RDMActions.GetProcSpell(
                level,
                context.HasVerfire,
                context.HasVerstone,
                context.VerfireRemaining,
                context.VerstoneRemaining,
                context.BlackMana,
                context.WhiteMana);

            if (procSpell != null)
            {
                if (context.ActionService.ExecuteGcd(procSpell, target.GameObjectId))
                {
                    context.Debug.PlannedAction = procSpell.Name;
                    context.Debug.DamageState = $"{procSpell.Name} (movement)";
                    return true;
                }
            }

            // Use Swiftcast for movement if available
            if (context.SwiftcastReady)
            {
                if (context.ActionService.ExecuteOgcd(RDMActions.Swiftcast, player.GameObjectId))
                {
                    context.Debug.DamageState = "Swiftcast for movement";
                    return true;
                }
            }

            // No instant option available, wait for slidecast window
            context.Debug.DamageState = "Moving, need instant";
            return false;
        }

        // Use proc if available (as filler to generate Dualcast while using the proc)
        var filler = RDMActions.GetProcSpell(
            level,
            context.HasVerfire,
            context.HasVerstone,
            context.VerfireRemaining,
            context.VerstoneRemaining,
            context.BlackMana,
            context.WhiteMana);

        if (filler != null)
        {
            if (context.ActionService.ExecuteGcd(filler, target.GameObjectId))
            {
                context.Debug.PlannedAction = filler.Name;
                context.Debug.DamageState = $"{filler.Name} (proc filler)";
                return true;
            }
        }

        // No proc, use Jolt for single target or Verthunder II/Veraero II for AoE
        if (useAoe)
        {
            var aoeHardcast = RDMActions.GetAoeHardcast(level, context.BlackMana, context.WhiteMana);
            if (context.ActionService.ExecuteGcd(aoeHardcast, target.GameObjectId))
            {
                context.Debug.PlannedAction = aoeHardcast.Name;
                context.Debug.DamageState = $"{aoeHardcast.Name} (AoE hardcast)";
                return true;
            }
        }

        // Standard Jolt filler
        var jolt = RDMActions.GetJoltSpell(level);
        if (context.ActionService.ExecuteGcd(jolt, target.GameObjectId))
        {
            context.Debug.PlannedAction = jolt.Name;
            context.Debug.DamageState = jolt.Name;
            return true;
        }

        return false;
    }

    #endregion
}
