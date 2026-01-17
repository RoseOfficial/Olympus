using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ZeusCore.Context;

namespace Olympus.Rotation.ZeusCore.Modules;

/// <summary>
/// Handles the Dragoon damage rotation.
/// Manages combo execution, jump weaving, Life of the Dragon, and burst windows.
/// </summary>
public sealed class DamageModule : IZeusModule
{
    public int Priority => 30; // Lowest priority - damage after utility
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IZeusContext context, bool isMoving)
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

        // Priority 1: Positional procs (Fang and Claw, Wheeling Thrust, Drakesbane)
        if (TryPositionalProc(context, target))
            return true;

        // Priority 2: Continue combo based on enemy count
        if (enemyCount >= AoeThreshold)
        {
            if (TryAoeCombo(context, target))
                return true;
        }
        else
        {
            if (TrySingleTargetCombo(context, target))
                return true;
        }

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IZeusContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Damage

    private bool TryOgcdDamage(IZeusContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Mirage Dive (consume Dive Ready)
        if (TryMirageDive(context, target))
            return true;

        // Priority 2: Starcross (after Stardiver)
        if (TryStarcross(context, target))
            return true;

        // Priority 3: Rise of the Dragon (after Dragonfire Dive)
        if (TryRiseOfTheDragon(context, target))
            return true;

        // Priority 4: Wyrmwind Thrust (2 Firstmind stacks)
        if (TryWyrmwindThrust(context, target))
            return true;

        // Priority 5: Life of the Dragon actions
        if (context.IsLifeOfDragonActive)
        {
            // Nastrond (spam during Life)
            if (TryNastrond(context, target))
                return true;

            // Stardiver (Life finisher)
            if (TryStardiver(context, target))
                return true;
        }

        // Priority 6: Geirskogul (Life activation or just damage)
        if (TryGeirskogul(context, target))
            return true;

        // Priority 7: High Jump / Jump
        if (TryJump(context, target))
            return true;

        // Priority 8: Spineshatter Dive (gap closer / damage)
        if (TrySpineshatterDive(context, target))
            return true;

        // Priority 9: Dragonfire Dive (AoE damage)
        if (TryDragonfireDive(context, target))
            return true;

        return false;
    }

    private bool TryMirageDive(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.MirageDive.MinLevel)
            return false;

        if (!context.HasDiveReady)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.MirageDive.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.MirageDive, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.MirageDive.Name;
            context.Debug.DamageState = "Mirage Dive";
            return true;
        }

        return false;
    }

    private bool TryStarcross(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.Starcross.MinLevel)
            return false;

        if (!context.HasStarcrossReady)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Starcross.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.Starcross, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Starcross.Name;
            context.Debug.DamageState = "Starcross";
            return true;
        }

        return false;
    }

    private bool TryRiseOfTheDragon(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.RiseOfTheDragon.MinLevel)
            return false;

        if (!context.HasDraconianFire)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.RiseOfTheDragon.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.RiseOfTheDragon, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.RiseOfTheDragon.Name;
            context.Debug.DamageState = "Rise of the Dragon";
            return true;
        }

        return false;
    }

    private bool TryWyrmwindThrust(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.WyrmwindThrust.MinLevel)
            return false;

        // Need 2 Firstmind's Focus
        if (context.FirstmindsFocus < 2)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.WyrmwindThrust.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.WyrmwindThrust, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.WyrmwindThrust.Name;
            context.Debug.DamageState = $"Wyrmwind Thrust ({context.FirstmindsFocus} focus)";
            return true;
        }

        return false;
    }

    private bool TryNastrond(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.Nastrond.MinLevel)
            return false;

        if (!context.IsLifeOfDragonActive)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Nastrond.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.Nastrond, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Nastrond.Name;
            context.Debug.DamageState = $"Nastrond (Life: {context.LifeOfDragonRemaining:F1}s)";
            return true;
        }

        return false;
    }

    private bool TryStardiver(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.Stardiver.MinLevel)
            return false;

        if (!context.IsLifeOfDragonActive)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Stardiver.ActionId))
            return false;

        // Use Stardiver during Life window
        // Prefer using it after Nastronds when possible
        if (context.ActionService.ExecuteOgcd(DRGActions.Stardiver, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Stardiver.Name;
            context.Debug.DamageState = $"Stardiver (Life: {context.LifeOfDragonRemaining:F1}s)";
            return true;
        }

        return false;
    }

    private bool TryGeirskogul(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.Geirskogul.MinLevel)
            return false;

        // Don't use during Life of the Dragon (Nastrond replaces it)
        if (context.IsLifeOfDragonActive)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Geirskogul.ActionId))
            return false;

        // Use Geirskogul - at 2 eyes, this enters Life of the Dragon
        if (context.ActionService.ExecuteOgcd(DRGActions.Geirskogul, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Geirskogul.Name;
            var eyeInfo = context.EyeCount >= 2 ? " (entering Life!)" : $" ({context.EyeCount} eyes)";
            context.Debug.DamageState = $"Geirskogul{eyeInfo}";
            return true;
        }

        return false;
    }

    private bool TryJump(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.Jump.MinLevel)
            return false;

        var jumpAction = DRGActions.GetJumpAction((byte)level);

        if (!context.ActionService.IsActionReady(jumpAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(jumpAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = jumpAction.Name;
            context.Debug.DamageState = jumpAction.Name;
            return true;
        }

        return false;
    }

    private bool TrySpineshatterDive(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.SpineshatterDive.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.SpineshatterDive.ActionId))
            return false;

        // Use as damage oGCD (also works as gap closer)
        if (context.ActionService.ExecuteOgcd(DRGActions.SpineshatterDive, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.SpineshatterDive.Name;
            context.Debug.DamageState = "Spineshatter Dive";
            return true;
        }

        return false;
    }

    private bool TryDragonfireDive(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level < DRGActions.DragonfireDive.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.DragonfireDive.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.DragonfireDive, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.DragonfireDive.Name;
            context.Debug.DamageState = "Dragonfire Dive";
            return true;
        }

        return false;
    }

    #endregion

    #region Positional Procs

    private bool TryPositionalProc(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        // At level 92+, Drakesbane replaces both Fang and Claw and Wheeling Thrust
        if (level >= DRGActions.Drakesbane.MinLevel)
        {
            // Either proc grants Drakesbane
            if (context.HasFangAndClawBared || context.HasWheelInMotion)
            {
                if (context.ActionService.IsActionReady(DRGActions.Drakesbane.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DRGActions.Drakesbane, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = DRGActions.Drakesbane.Name;
                        context.Debug.DamageState = "Drakesbane";
                        return true;
                    }
                }
            }
            return false;
        }

        // Fang and Claw (flank positional)
        if (context.HasFangAndClawBared && level >= DRGActions.FangAndClaw.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.FangAndClaw.ActionId))
            {
                var positionalOk = context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.ActionService.ExecuteGcd(DRGActions.FangAndClaw, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.FangAndClaw.Name;
                    context.Debug.DamageState = $"Fang and Claw {(positionalOk ? "(flank OK)" : "(flank!)")}";
                    return true;
                }
            }
        }

        // Wheeling Thrust (rear positional)
        if (context.HasWheelInMotion && level >= DRGActions.WheelingThrust.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.WheelingThrust.ActionId))
            {
                var positionalOk = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.ActionService.ExecuteGcd(DRGActions.WheelingThrust, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.WheelingThrust.Name;
                    context.Debug.DamageState = $"Wheeling Thrust {(positionalOk ? "(rear OK)" : "(rear!)")}";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Single-Target Combo

    private bool TrySingleTargetCombo(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;
        var lastAction = context.LastComboAction;
        var comboActive = context.ComboTimeRemaining > 0;

        // After Vorpal Thrust -> Heavens' Thrust / Full Thrust
        if (comboActive && lastAction == DRGActions.VorpalThrust.ActionId)
        {
            var finisher = DRGActions.GetVorpalFinisher((byte)level);
            if (context.ActionService.IsActionReady(finisher.ActionId))
            {
                if (context.ActionService.ExecuteGcd(finisher, target.GameObjectId))
                {
                    context.Debug.PlannedAction = finisher.Name;
                    context.Debug.DamageState = finisher.Name;
                    return true;
                }
            }
        }

        // After Disembowel -> Chaotic Spring / Chaos Thrust (rear positional)
        if (comboActive && lastAction == DRGActions.Disembowel.ActionId)
        {
            var finisher = DRGActions.GetDisembowelFinisher((byte)level);
            if (context.ActionService.IsActionReady(finisher.ActionId))
            {
                var positionalOk = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.ActionService.ExecuteGcd(finisher, target.GameObjectId))
                {
                    context.Debug.PlannedAction = finisher.Name;
                    context.Debug.DamageState = $"{finisher.Name} {(positionalOk ? "(rear OK)" : "(rear!)")}";
                    return true;
                }
            }
        }

        // After True Thrust -> Choose Vorpal or Disembowel
        if (comboActive && lastAction == DRGActions.TrueThrust.ActionId)
        {
            // Prefer Disembowel line if:
            // 1. Power Surge is missing or about to expire (< 10s)
            // 2. DoT is missing or about to expire (< 5s)
            var needsPowerSurge = !context.HasPowerSurge || context.PowerSurgeRemaining < 10f;
            var needsDot = !context.HasDotOnTarget || context.DotRemaining < 5f;

            if ((needsPowerSurge || needsDot) && level >= DRGActions.Disembowel.MinLevel)
            {
                if (context.ActionService.IsActionReady(DRGActions.Disembowel.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DRGActions.Disembowel, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = DRGActions.Disembowel.Name;
                        context.Debug.DamageState = $"Disembowel (PS: {context.PowerSurgeRemaining:F1}s)";
                        return true;
                    }
                }
            }

            // Otherwise use Vorpal Thrust for damage
            if (level >= DRGActions.VorpalThrust.MinLevel)
            {
                if (context.ActionService.IsActionReady(DRGActions.VorpalThrust.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DRGActions.VorpalThrust, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = DRGActions.VorpalThrust.Name;
                        context.Debug.DamageState = "Vorpal Thrust";
                        return true;
                    }
                }
            }
        }

        // Start combo with True Thrust
        if (context.ActionService.IsActionReady(DRGActions.TrueThrust.ActionId))
        {
            if (context.ActionService.ExecuteGcd(DRGActions.TrueThrust, target.GameObjectId))
            {
                context.Debug.PlannedAction = DRGActions.TrueThrust.Name;
                context.Debug.DamageState = "True Thrust";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region AoE Combo

    private bool TryAoeCombo(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;
        var lastAction = context.LastComboAction;
        var comboActive = context.ComboTimeRemaining > 0;

        // After Sonic Thrust -> Coerthan Torment
        if (comboActive && lastAction == DRGActions.SonicThrust.ActionId &&
            level >= DRGActions.CoerthanTorment.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.CoerthanTorment.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRGActions.CoerthanTorment, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.CoerthanTorment.Name;
                    context.Debug.DamageState = "Coerthan Torment";
                    return true;
                }
            }
        }

        // After Doom Spike -> Sonic Thrust
        if (comboActive && lastAction == DRGActions.DoomSpike.ActionId &&
            level >= DRGActions.SonicThrust.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.SonicThrust.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRGActions.SonicThrust, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.SonicThrust.Name;
                    context.Debug.DamageState = "Sonic Thrust";
                    return true;
                }
            }
        }

        // Start AoE combo with Doom Spike
        if (level >= DRGActions.DoomSpike.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.DoomSpike.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRGActions.DoomSpike, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.DoomSpike.Name;
                    context.Debug.DamageState = "Doom Spike";
                    return true;
                }
            }
        }

        // Fall back to single target if AoE not available
        return TrySingleTargetCombo(context, target);
    }

    #endregion
}
