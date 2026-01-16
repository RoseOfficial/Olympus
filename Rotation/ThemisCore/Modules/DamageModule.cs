using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ThemisCore.Context;

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

        // If we get here, no action was taken
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
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.MeleeTargetingRange,
            player);

        if (target == null)
            return false;

        // Circle of Scorn (AoE DoT oGCD)
        if (level >= PLDActions.CircleOfScorn.MinLevel &&
            context.ActionService.IsActionReady(PLDActions.CircleOfScorn.ActionId))
        {
            if (context.ActionService.ExecuteOgcd(PLDActions.CircleOfScorn, player.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.CircleOfScorn.Name;
                context.Debug.DamageState = "Circle of Scorn";
                return true;
            }
        }

        // Expiacion (Lv.86) or Spirits Within (Lv.30)
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
                return true;
            }
        }

        // Intervene (gap closer, 2 charges) - only if not in melee range
        if (level >= PLDActions.Intervene.MinLevel)
        {
            var distance = GetDistanceToTarget(context.Player, target);
            if (distance > FFXIVConstants.MeleeTargetingRange && distance <= 20f)
            {
                if (context.ActionService.IsActionReady(PLDActions.Intervene.ActionId))
                {
                    if (context.ActionService.ExecuteOgcd(PLDActions.Intervene, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = PLDActions.Intervene.Name;
                        context.Debug.DamageState = "Gap close";
                        return true;
                    }
                }
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

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.MeleeTargetingRange,
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

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.MeleeTargetingRange,
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
                    return true;
                }
            }

            if (context.ActionService.ExecuteGcd(PLDActions.GoringBlade, target.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.GoringBlade.Name;
                context.Debug.DamageState = $"Goring Blade (DoT {context.GoringBladeRemaining:F1}s)";
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

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.MeleeTargetingRange,
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
            return true;
        }

        return false;
    }

    #endregion

    #region Helpers

    private static float GetDistanceToTarget(Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter player, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var dx = player.Position.X - target.Position.X;
        var dy = player.Position.Y - target.Position.Y;
        var dz = player.Position.Z - target.Position.Z;
        return (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    #endregion
}
