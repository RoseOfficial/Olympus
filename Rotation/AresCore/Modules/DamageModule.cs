using Olympus.Data;
using Olympus.Rotation.AresCore.Context;

namespace Olympus.Rotation.AresCore.Modules;

/// <summary>
/// Handles the Warrior damage rotation.
/// Manages combo chains, Beast Gauge spending, and burst windows.
/// </summary>
public sealed class DamageModule : IAresModule
{
    public int Priority => 30; // Lowest priority - damage after utility
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IAresContext context, bool isMoving)
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
            3f, // Melee range
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

        // Priority 1: Primal Rend (during Inner Release)
        if (TryPrimalRend(context, target))
            return true;

        // Priority 2: Primal Ruination (follow-up to Primal Rend)
        if (TryPrimalRuination(context, target))
            return true;

        // Priority 3: Inner Chaos (Nascent Chaos active)
        if (TryInnerChaos(context, target, enemyCount))
            return true;

        // Priority 4: Fell Cleave / Decimate (gauge spender)
        if (TryGaugeSpender(context, target, enemyCount))
            return true;

        // Priority 5: Surging Tempest refresh (if low)
        if (TrySurgingTempestRefresh(context, target, enemyCount))
            return true;

        // Priority 6: Main combo / AoE combo
        if (TryCombo(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IAresContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Damage

    private bool TryOgcdDamage(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Upheaval (single target oGCD)
        if (TryUpheaval(context, target, enemyCount))
            return true;

        // Priority 2: Orogeny (AoE oGCD)
        if (TryOrogeny(context, target, enemyCount))
            return true;

        // Priority 3: Onslaught (gap closer / extra damage)
        if (TryOnslaught(context, target))
            return true;

        return false;
    }

    private bool TryUpheaval(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Upheaval.MinLevel)
            return false;

        // Prefer Orogeny for AoE
        if (enemyCount >= AoeThreshold && level >= WARActions.Orogeny.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Upheaval.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Upheaval, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Upheaval.Name;
            context.Debug.DamageState = "Upheaval";
            return true;
        }

        return false;
    }

    private bool TryOrogeny(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Orogeny.MinLevel)
            return false;

        // Only use in AoE situations
        if (enemyCount < AoeThreshold)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Orogeny.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Orogeny, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Orogeny.Name;
            context.Debug.DamageState = $"Orogeny ({enemyCount} enemies)";
            return true;
        }

        return false;
    }

    private bool TryOnslaught(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Onslaught.MinLevel)
            return false;

        // Use Onslaught to weave extra damage
        // At level 88+, has 3 charges - can use more freely
        if (!context.ActionService.IsActionReady(WARActions.Onslaught.ActionId))
            return false;

        // Check distance - Onslaught is a gap closer with 20y range
        var dx = player.Position.X - target.Position.X;
        var dy = player.Position.Y - target.Position.Y;
        var dz = player.Position.Z - target.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

        // Don't use if already in melee range and we want to save charges
        // Use freely if we have multiple charges (level 88+) or need to close gap
        if (distance <= 3f && level >= 88)
        {
            // In melee range with 3 charges - use as extra damage
            if (context.ActionService.ExecuteOgcd(WARActions.Onslaught, target.GameObjectId))
            {
                context.Debug.PlannedAction = WARActions.Onslaught.Name;
                context.Debug.DamageState = "Onslaught (weave)";
                return true;
            }
        }
        else if (distance > 3f && distance <= 20f)
        {
            // Gap close usage
            if (context.ActionService.ExecuteOgcd(WARActions.Onslaught, target.GameObjectId))
            {
                context.Debug.PlannedAction = WARActions.Onslaught.Name;
                context.Debug.DamageState = "Onslaught (gap close)";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Primal Abilities

    private bool TryPrimalRend(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.PrimalRend.MinLevel)
            return false;

        // Primal Rend Ready is granted by Inner Release
        if (!context.HasPrimalRendReady)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.PrimalRend.ActionId))
            return false;

        // Primal Rend is a ranged GCD (20y) - can use even at distance
        if (context.ActionService.ExecuteGcd(WARActions.PrimalRend, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.PrimalRend.Name;
            context.Debug.DamageState = "Primal Rend";
            return true;
        }

        return false;
    }

    private bool TryPrimalRuination(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.PrimalRuination.MinLevel)
            return false;

        // Primal Ruination Ready is granted after using Primal Rend
        if (!context.HasPrimalRuinationReady)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.PrimalRuination.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.PrimalRuination, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.PrimalRuination.Name;
            context.Debug.DamageState = "Primal Ruination";
            return true;
        }

        return false;
    }

    #endregion

    #region Inner Chaos

    private bool TryInnerChaos(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Nascent Chaos enables either Inner Chaos (ST) or Chaotic Cyclone (AoE)
        if (!context.HasNascentChaos)
            return false;

        // Choose ST or AoE based on enemy count
        if (enemyCount >= AoeThreshold && level >= WARActions.ChaoticCyclone.MinLevel)
        {
            return TryChaoticCyclone(context, target, enemyCount);
        }
        else if (level >= WARActions.InnerChaos.MinLevel)
        {
            return TryInnerChaosST(context, target);
        }

        return false;
    }

    private bool TryInnerChaosST(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;

        if (!context.ActionService.IsActionReady(WARActions.InnerChaos.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.InnerChaos, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.InnerChaos.Name;
            context.Debug.DamageState = "Inner Chaos";
            return true;
        }

        return false;
    }

    private bool TryChaoticCyclone(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;

        if (!context.ActionService.IsActionReady(WARActions.ChaoticCyclone.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.ChaoticCyclone, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.ChaoticCyclone.Name;
            context.Debug.DamageState = $"Chaotic Cyclone ({enemyCount} enemies)";
            return true;
        }

        return false;
    }

    #endregion

    #region Gauge Spenders

    private bool TryGaugeSpender(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // During Inner Release, all gauge spenders are free (no cost)
        // Outside IR, need 50 gauge

        bool canSpend = context.HasInnerRelease || context.BeastGauge >= 50;
        if (!canSpend)
            return false;

        // Choose ST or AoE spender
        if (enemyCount >= AoeThreshold)
        {
            return TryDecimate(context, target, enemyCount);
        }
        else
        {
            return TryFellCleave(context, target);
        }
    }

    private bool TryFellCleave(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Get the appropriate Fell Cleave action
        var fellCleaveAction = WARActions.GetFellCleaveAction(level);

        if (level < WARActions.FellCleave.MinLevel)
        {
            // Pre-54, use Inner Beast instead
            if (level >= WARActions.InnerBeast.MinLevel)
            {
                fellCleaveAction = WARActions.InnerBeast;
            }
            else
            {
                return false;
            }
        }

        if (!context.ActionService.IsActionReady(fellCleaveAction.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(fellCleaveAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = fellCleaveAction.Name;
            if (context.HasInnerRelease)
                context.Debug.DamageState = $"{fellCleaveAction.Name} (IR: {context.InnerReleaseStacks} stacks)";
            else
                context.Debug.DamageState = $"{fellCleaveAction.Name} ({context.BeastGauge} gauge)";
            return true;
        }

        return false;
    }

    private bool TryDecimate(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Get the appropriate Decimate action
        var decimateAction = WARActions.GetDecimateAction(level);

        if (level < WARActions.SteelCyclone.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(decimateAction.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(decimateAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = decimateAction.Name;
            if (context.HasInnerRelease)
                context.Debug.DamageState = $"{decimateAction.Name} ({enemyCount} enemies, IR)";
            else
                context.Debug.DamageState = $"{decimateAction.Name} ({enemyCount} enemies)";
            return true;
        }

        return false;
    }

    #endregion

    #region Surging Tempest Management

    private bool TrySurgingTempestRefresh(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Surging Tempest is granted by:
        // - Storm's Eye (ST combo finisher, level 50+)
        // - Mythril Tempest (AoE combo finisher, level 40+)

        // Only refresh if buff is about to expire
        if (context.HasSurgingTempest && context.SurgingTempestRemaining > 10f)
            return false;

        // During Inner Release, prefer gauge spenders over refreshing
        if (context.HasInnerRelease && context.InnerReleaseStacks > 0)
            return false;

        // Check if we can finish the combo
        if (enemyCount >= AoeThreshold)
        {
            return TryMythrilTempestFinish(context, target, enemyCount);
        }
        else
        {
            return TryStormsEyeFinish(context, target);
        }
    }

    private bool TryStormsEyeFinish(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.StormsEye.MinLevel)
            return false;

        // Storm's Eye is the 3rd hit of the combo
        // Need to check if we're at the right combo step
        if (context.ComboStep != 2 || context.LastComboAction != WARActions.Maim.ActionId)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.StormsEye.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.StormsEye, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.StormsEye.Name;
            context.Debug.DamageState = "Storm's Eye (refresh buff)";
            return true;
        }

        return false;
    }

    private bool TryMythrilTempestFinish(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.MythrilTempest.MinLevel)
            return false;

        // Mythril Tempest is the 2nd hit of the AoE combo
        if (context.ComboStep != 1 || context.LastComboAction != WARActions.Overpower.ActionId)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.MythrilTempest.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(WARActions.MythrilTempest, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.MythrilTempest.Name;
            context.Debug.DamageState = $"Mythril Tempest ({enemyCount} enemies, refresh buff)";
            return true;
        }

        return false;
    }

    #endregion

    #region Main Combo

    private bool TryCombo(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE rotation for 3+ enemies
        if (enemyCount >= AoeThreshold)
        {
            return TryAoeCombo(context, target, enemyCount);
        }

        // Single target combo
        return TrySingleTargetCombo(context, target);
    }

    private bool TrySingleTargetCombo(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // ST Combo: Heavy Swing -> Maim -> Storm's Path/Eye
        // Storm's Eye grants Surging Tempest (damage buff)
        // Storm's Path grants more gauge

        // Combo step 3: Finisher
        if (context.ComboStep == 2 && context.LastComboAction == WARActions.Maim.ActionId)
        {
            // Choose finisher based on Surging Tempest status
            // Need to refresh if buff is low (< 10s) or missing
            bool needsSurgingTempest = !context.HasSurgingTempest || context.SurgingTempestRemaining < 10f;
            var finisherAction = WARActions.GetComboFinisher(level, needsSurgingTempest);

            if (context.ActionService.IsActionReady(finisherAction.ActionId))
            {
                if (context.ActionService.ExecuteGcd(finisherAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = finisherAction.Name;
                    context.Debug.DamageState = $"{finisherAction.Name} (combo 3)";
                    return true;
                }
            }
        }

        // Combo step 2: Maim
        if (context.ComboStep == 1 && context.LastComboAction == WARActions.HeavySwing.ActionId)
        {
            if (level >= WARActions.Maim.MinLevel)
            {
                if (context.ActionService.IsActionReady(WARActions.Maim.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(WARActions.Maim, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = WARActions.Maim.Name;
                        context.Debug.DamageState = "Maim (combo 2)";
                        return true;
                    }
                }
            }
        }

        // Combo step 1: Heavy Swing (always available)
        if (context.ActionService.IsActionReady(WARActions.HeavySwing.ActionId))
        {
            if (context.ActionService.ExecuteGcd(WARActions.HeavySwing, target.GameObjectId))
            {
                context.Debug.PlannedAction = WARActions.HeavySwing.Name;
                context.Debug.DamageState = "Heavy Swing (combo 1)";
                return true;
            }
        }

        return false;
    }

    private bool TryAoeCombo(IAresContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE Combo: Overpower -> Mythril Tempest

        // Combo step 2: Mythril Tempest
        if (context.ComboStep == 1 && context.LastComboAction == WARActions.Overpower.ActionId)
        {
            if (level >= WARActions.MythrilTempest.MinLevel)
            {
                if (context.ActionService.IsActionReady(WARActions.MythrilTempest.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(WARActions.MythrilTempest, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = WARActions.MythrilTempest.Name;
                        context.Debug.DamageState = $"Mythril Tempest ({enemyCount} enemies, combo 2)";
                        return true;
                    }
                }
            }
        }

        // Combo step 1: Overpower
        if (level >= WARActions.Overpower.MinLevel)
        {
            if (context.ActionService.IsActionReady(WARActions.Overpower.ActionId))
            {
                if (context.ActionService.ExecuteGcd(WARActions.Overpower, target.GameObjectId))
                {
                    context.Debug.PlannedAction = WARActions.Overpower.Name;
                    context.Debug.DamageState = $"Overpower ({enemyCount} enemies, combo 1)";
                    return true;
                }
            }
        }

        // Fallback to single target if AoE not available
        return TrySingleTargetCombo(context, target);
    }

    #endregion
}
