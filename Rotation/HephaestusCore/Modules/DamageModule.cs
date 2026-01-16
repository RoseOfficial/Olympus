using Olympus.Data;
using Olympus.Rotation.HephaestusCore.Context;

namespace Olympus.Rotation.HephaestusCore.Modules;

/// <summary>
/// Handles the Gunbreaker DPS rotation.
/// Manages Gnashing Fang combo, Continuation actions, cartridge spending, and basic combos.
/// </summary>
public sealed class DamageModule : IHephaestusModule
{
    public int Priority => 30; // Lower priority - damage comes after survival
    public string Name => "Damage";

    public bool TryExecute(IHephaestusContext context, bool isMoving)
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

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);

        // oGCD Phase
        if (context.CanExecuteOgcd)
        {
            // Priority 1: Continuation actions (MUST use before next GCD)
            if (TryContinuation(context, target.GameObjectId))
                return true;

            // Priority 2: Blasting Zone / Danger Zone
            if (TryBlastingZone(context, target.GameObjectId))
                return true;

            // Priority 3: Bow Shock (AoE DoT)
            if (TryBowShock(context))
                return true;

            // Priority 4: Rough Divide (gap closer / damage filler)
            if (TryRoughDivide(context, target.GameObjectId))
                return true;
        }

        // GCD Phase
        if (context.CanExecuteGcd)
        {
            // Priority 1: Continue Gnashing Fang combo if in progress
            if (TryGnashingFangCombo(context, target.GameObjectId))
                return true;

            // Priority 2: Reign of Beasts combo (Lv.100)
            if (TryReignOfBeastsCombo(context, target.GameObjectId))
                return true;

            // Priority 3: Double Down during No Mercy
            if (TryDoubleDown(context, target.GameObjectId, enemyCount))
                return true;

            // Priority 4: Sonic Break (DoT) during No Mercy
            if (TrySonicBreak(context, target.GameObjectId))
                return true;

            // Priority 5: Start Gnashing Fang combo
            if (TryStartGnashingFang(context, target.GameObjectId))
                return true;

            // Priority 6: Burst Strike / Fated Circle (cartridge spenders)
            if (TryCartridgeSpender(context, enemyCount, target.GameObjectId))
                return true;

            // Priority 7: Basic combo
            if (TryBasicCombo(context, enemyCount, target.GameObjectId))
                return true;
        }

        return false;
    }

    public void UpdateDebugState(IHephaestusContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Actions

    /// <summary>
    /// Continuation actions have highest priority.
    /// These MUST be used before the next GCD or the proc expires.
    /// </summary>
    private bool TryContinuation(IHephaestusContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Continuation.MinLevel)
            return false;

        // Ready to Rip (after Gnashing Fang)
        if (context.IsReadyToRip)
        {
            if (context.ActionService.IsActionReady(GNBActions.JugularRip.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(GNBActions.JugularRip, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.JugularRip.Name;
                    context.Debug.DamageState = "Jugular Rip!";
                    return true;
                }
            }
        }

        // Ready to Tear (after Savage Claw)
        if (context.IsReadyToTear)
        {
            if (context.ActionService.IsActionReady(GNBActions.AbdomenTear.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(GNBActions.AbdomenTear, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.AbdomenTear.Name;
                    context.Debug.DamageState = "Abdomen Tear!";
                    return true;
                }
            }
        }

        // Ready to Gouge (after Wicked Talon)
        if (context.IsReadyToGouge)
        {
            if (context.ActionService.IsActionReady(GNBActions.EyeGouge.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(GNBActions.EyeGouge, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.EyeGouge.Name;
                    context.Debug.DamageState = "Eye Gouge!";
                    return true;
                }
            }
        }

        // Ready to Blast (after Burst Strike, Lv.86+)
        if (context.IsReadyToBlast && level >= GNBActions.Hypervelocity.MinLevel)
        {
            if (context.ActionService.IsActionReady(GNBActions.Hypervelocity.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(GNBActions.Hypervelocity, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.Hypervelocity.Name;
                    context.Debug.DamageState = "Hypervelocity!";
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryBlastingZone(IHephaestusContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.DangerZone.MinLevel)
            return false;

        var action = GNBActions.GetBlastingZoneAction(level);

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(action, targetId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = action.Name;
            return true;
        }

        return false;
    }

    private bool TryBowShock(IHephaestusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.BowShock.MinLevel)
            return false;

        // Bow Shock is a ground-targeted AoE DoT
        // Use on cooldown during combat
        if (!context.ActionService.IsActionReady(GNBActions.BowShock.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(GNBActions.BowShock, player.GameObjectId))
        {
            context.Debug.PlannedAction = GNBActions.BowShock.Name;
            context.Debug.DamageState = "Bow Shock";
            return true;
        }

        return false;
    }

    private bool TryRoughDivide(IHephaestusContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.RoughDivide.MinLevel)
            return false;

        // Rough Divide is a gap closer with 2 charges
        // Use as damage filler, but keep 1 charge for mobility
        if (!context.ActionService.IsActionReady(GNBActions.RoughDivide.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(GNBActions.RoughDivide, targetId))
        {
            context.Debug.PlannedAction = GNBActions.RoughDivide.Name;
            context.Debug.DamageState = "Rough Divide";
            return true;
        }

        return false;
    }

    #endregion

    #region GCD Actions - Gnashing Fang Combo

    /// <summary>
    /// Continue Gnashing Fang combo if in progress.
    /// Gnashing Fang → Savage Claw → Wicked Talon
    /// </summary>
    private bool TryGnashingFangCombo(IHephaestusContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.GnashingFang.MinLevel)
            return false;

        // Not in combo
        if (!context.IsInGnashingFangCombo)
            return false;

        // Step 1: After Gnashing Fang, use Savage Claw
        if (context.GnashingFangStep == 1)
        {
            if (context.ActionService.IsActionReady(GNBActions.SavageClaw.ActionId))
            {
                if (context.ActionService.ExecuteGcd(GNBActions.SavageClaw, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.SavageClaw.Name;
                    context.Debug.DamageState = "Savage Claw (combo 2/3)";
                    return true;
                }
            }
        }

        // Step 2: After Savage Claw, use Wicked Talon
        if (context.GnashingFangStep == 2)
        {
            if (context.ActionService.IsActionReady(GNBActions.WickedTalon.ActionId))
            {
                if (context.ActionService.ExecuteGcd(GNBActions.WickedTalon, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.WickedTalon.Name;
                    context.Debug.DamageState = "Wicked Talon (combo 3/3)";
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Start a new Gnashing Fang combo when conditions are right.
    /// </summary>
    private bool TryStartGnashingFang(IHephaestusContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.GnashingFang.MinLevel)
            return false;

        // Already in combo
        if (context.IsInGnashingFangCombo)
            return false;

        // Need cartridges
        if (!context.CanUseGnashingFang)
        {
            context.Debug.DamageState = "GF: no cartridges";
            return false;
        }

        // Prefer to use during No Mercy window
        // But don't hold forever if cartridges are capped
        if (!context.HasNoMercy && !context.HasMaxCartridges)
        {
            // No No Mercy and not capped - can wait
            return false;
        }

        if (!context.ActionService.IsActionReady(GNBActions.GnashingFang.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(GNBActions.GnashingFang, targetId))
        {
            context.Debug.PlannedAction = GNBActions.GnashingFang.Name;
            context.Debug.DamageState = "Gnashing Fang (combo 1/3)";
            return true;
        }

        return false;
    }

    #endregion

    #region GCD Actions - Reign of Beasts Combo (Lv.100)

    /// <summary>
    /// Reign of Beasts combo at Lv.100.
    /// Triggered by Bloodfest granting Ready to Reign.
    /// Reign of Beasts → Noble Blood → Lion Heart
    /// </summary>
    private bool TryReignOfBeastsCombo(IHephaestusContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.ReignOfBeasts.MinLevel)
            return false;

        // Need Ready to Reign (from Bloodfest)
        if (!context.IsReadyToReign)
            return false;

        // The game handles the combo progression via action replacement
        // Just check if Reign of Beasts is ready and execute
        if (context.ActionService.IsActionReady(GNBActions.ReignOfBeasts.ActionId))
        {
            if (context.ActionService.ExecuteGcd(GNBActions.ReignOfBeasts, targetId))
            {
                context.Debug.PlannedAction = "Reign Combo";
                context.Debug.DamageState = "Reign of Beasts";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region GCD Actions - Cartridge Spenders

    /// <summary>
    /// Double Down - High potency 2-cartridge spender.
    /// Best used during No Mercy window.
    /// </summary>
    private bool TryDoubleDown(IHephaestusContext context, ulong targetId, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.DoubleDown.MinLevel)
            return false;

        // Need 2+ cartridges
        if (!context.CanUseDoubleDown)
            return false;

        // Only use during No Mercy for maximum value
        if (!context.HasNoMercy)
            return false;

        if (!context.ActionService.IsActionReady(GNBActions.DoubleDown.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(GNBActions.DoubleDown, targetId))
        {
            context.Debug.PlannedAction = GNBActions.DoubleDown.Name;
            context.Debug.DamageState = enemyCount > 1 ? $"Double Down ({enemyCount} enemies)" : "Double Down";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sonic Break - High potency DoT with 60s cooldown.
    /// Best used during No Mercy window.
    /// </summary>
    private bool TrySonicBreak(IHephaestusContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.SonicBreak.MinLevel)
            return false;

        // Prefer to use during No Mercy
        if (!context.HasNoMercy)
            return false;

        // Don't use if DoT is already active (would waste duration)
        if (context.HasSonicBreakDot)
            return false;

        if (!context.ActionService.IsActionReady(GNBActions.SonicBreak.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(GNBActions.SonicBreak, targetId))
        {
            context.Debug.PlannedAction = GNBActions.SonicBreak.Name;
            context.Debug.DamageState = "Sonic Break (DoT)";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Burst Strike (ST) or Fated Circle (AoE) for spending cartridges.
    /// </summary>
    private bool TryCartridgeSpender(IHephaestusContext context, int enemyCount, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Need at least 1 cartridge
        if (context.Cartridges < GNBActions.BurstStrikeCost)
            return false;

        // Spend conditions:
        // 1. Max cartridges (avoid overcap from combo finisher)
        // 2. During No Mercy window
        // 3. About to overcap from combo finisher
        var shouldSpend = context.HasMaxCartridges ||
                          context.HasNoMercy ||
                          (context.ComboStep == 2 && context.Cartridges >= 2);

        if (!shouldSpend)
            return false;

        // Choose AoE or ST
        if (enemyCount >= 3 && level >= GNBActions.FatedCircle.MinLevel)
        {
            if (context.ActionService.IsActionReady(GNBActions.FatedCircle.ActionId))
            {
                if (context.ActionService.ExecuteGcd(GNBActions.FatedCircle, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.FatedCircle.Name;
                    context.Debug.DamageState = $"Fated Circle ({enemyCount} enemies)";
                    return true;
                }
            }
        }
        else if (level >= GNBActions.BurstStrike.MinLevel)
        {
            if (context.ActionService.IsActionReady(GNBActions.BurstStrike.ActionId))
            {
                if (context.ActionService.ExecuteGcd(GNBActions.BurstStrike, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.BurstStrike.Name;
                    context.Debug.DamageState = $"Burst Strike ({context.Cartridges} cartridges)";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region GCD Actions - Basic Combo

    /// <summary>
    /// Basic combo progression.
    /// ST: Keen Edge → Brutal Shell → Solid Barrel (+1 cartridge)
    /// AoE: Demon Slice → Demon Slaughter (+1 cartridge)
    /// </summary>
    private bool TryBasicCombo(IHephaestusContext context, int enemyCount, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE combo (3+ enemies)
        if (enemyCount >= 3 && level >= GNBActions.DemonSlice.MinLevel)
        {
            return TryAoECombo(context, targetId);
        }

        // Single-target combo
        return TrySingleTargetCombo(context, targetId);
    }

    private bool TrySingleTargetCombo(IHephaestusContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Combo step 2: Solid Barrel (finisher, grants +1 cartridge)
        if (context.ComboStep == 2 && level >= GNBActions.SolidBarrel.MinLevel)
        {
            if (context.ActionService.IsActionReady(GNBActions.SolidBarrel.ActionId))
            {
                if (context.ActionService.ExecuteGcd(GNBActions.SolidBarrel, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.SolidBarrel.Name;
                    context.Debug.DamageState = "Solid Barrel (+1 cart)";
                    return true;
                }
            }
        }

        // Combo step 1: Brutal Shell
        if (context.ComboStep == 1 && level >= GNBActions.BrutalShell.MinLevel)
        {
            if (context.ActionService.IsActionReady(GNBActions.BrutalShell.ActionId))
            {
                if (context.ActionService.ExecuteGcd(GNBActions.BrutalShell, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.BrutalShell.Name;
                    context.Debug.DamageState = "Brutal Shell (combo)";
                    return true;
                }
            }
        }

        // Combo starter: Keen Edge
        if (context.ActionService.IsActionReady(GNBActions.KeenEdge.ActionId))
        {
            if (context.ActionService.ExecuteGcd(GNBActions.KeenEdge, targetId))
            {
                context.Debug.PlannedAction = GNBActions.KeenEdge.Name;
                context.Debug.DamageState = "Keen Edge (start)";
                return true;
            }
        }

        return false;
    }

    private bool TryAoECombo(IHephaestusContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Combo step 1: Demon Slaughter (finisher, grants +1 cartridge)
        if (context.ComboStep == 1 && level >= GNBActions.DemonSlaughter.MinLevel)
        {
            if (context.ActionService.IsActionReady(GNBActions.DemonSlaughter.ActionId))
            {
                if (context.ActionService.ExecuteGcd(GNBActions.DemonSlaughter, targetId))
                {
                    context.Debug.PlannedAction = GNBActions.DemonSlaughter.Name;
                    context.Debug.DamageState = "Demon Slaughter (+1 cart)";
                    return true;
                }
            }
        }

        // AoE starter: Demon Slice
        if (context.ActionService.IsActionReady(GNBActions.DemonSlice.ActionId))
        {
            if (context.ActionService.ExecuteGcd(GNBActions.DemonSlice, targetId))
            {
                context.Debug.PlannedAction = GNBActions.DemonSlice.Name;
                context.Debug.DamageState = "Demon Slice (AoE start)";
                return true;
            }
        }

        return false;
    }

    #endregion
}
