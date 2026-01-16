using Olympus.Data;
using Olympus.Rotation.AresCore.Context;

namespace Olympus.Rotation.AresCore.Modules;

/// <summary>
/// Handles the Warrior defensive rotation.
/// Manages personal mitigation, party mitigation, and invulnerability.
/// </summary>
public sealed class MitigationModule : IAresModule
{
    public int Priority => 10; // High priority for defensives
    public string Name => "Mitigation";

    public bool TryExecute(IAresContext context, bool isMoving)
    {
        if (!context.Configuration.Tank.EnableMitigation)
        {
            context.Debug.MitigationState = "Disabled";
            return false;
        }

        if (!context.InCombat)
        {
            context.Debug.MitigationState = "Not in combat";
            return false;
        }

        // Only use defensives during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        var player = context.Player;
        var level = player.Level;
        var hpPercent = (float)player.CurrentHp / player.MaxHp;
        var damageRate = context.DamageIntakeService.GetDamageRate(player.EntityId);

        // Update cooldown service
        context.TankCooldownService.Update(hpPercent, damageRate);

        // Priority 1: Emergency invulnerability (Holmgang)
        if (TryHolmgang(context, hpPercent))
            return true;

        // Priority 2: Major cooldown (Vengeance/Damnation)
        if (TryMajorCooldown(context, hpPercent, damageRate))
            return true;

        // Priority 3: Rampart (role action)
        if (TryRampart(context, hpPercent, damageRate))
            return true;

        // Priority 4: Short cooldown (Bloodwhetting/Raw Intuition)
        if (TryBloodwhetting(context, hpPercent))
            return true;

        // Priority 5: Thrill of Battle (HP boost + heal)
        if (TryThrillOfBattle(context, hpPercent))
            return true;

        // Priority 6: Equilibrium (self-heal)
        if (TryEquilibrium(context, hpPercent))
            return true;

        // Priority 7: Arm's Length for knockback immunity
        if (TryArmsLength(context))
            return true;

        // Priority 8: Party mitigation (Reprisal)
        if (TryReprisal(context))
            return true;

        // Priority 9: Shake It Off for party protection
        if (TryShakeItOff(context))
            return true;

        // Priority 10: Nascent Flash for protecting low HP ally
        if (TryNascentFlash(context, hpPercent))
            return true;

        return false;
    }

    public void UpdateDebugState(IAresContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Emergency Mitigation

    private bool TryHolmgang(IAresContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Holmgang.MinLevel)
            return false;

        // Only use Holmgang in emergencies
        if (hpPercent > 0.15f)
            return false;

        // Don't use if already under Holmgang
        if (context.HasHolmgang)
            return false;

        // Find target for Holmgang (requires a target)
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            6f, // Holmgang range
            player);

        if (target == null)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Holmgang.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Holmgang, target.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Holmgang.Name;
            context.Debug.MitigationState = $"Emergency invuln ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    #endregion

    #region Major Cooldowns

    private bool TryMajorCooldown(IAresContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine which Vengeance variant to use
        var vengeanceAction = WARActions.GetVengeanceAction(level);

        if (level < WARActions.Vengeance.MinLevel)
            return false;

        // Check if we should use major cooldown
        if (!context.TankCooldownService.ShouldUseMajorCooldown(hpPercent, damageRate))
            return false;

        // Don't stack with Holmgang
        if (context.HasHolmgang)
            return false;

        // Don't stack with existing Vengeance/Damnation
        if (context.HasVengeance)
            return false;

        if (!context.ActionService.IsActionReady(vengeanceAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(vengeanceAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = vengeanceAction.Name;
            context.Debug.MitigationState = $"Major CD ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryRampart(IAresContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Rampart.MinLevel)
            return false;

        // Check if we should use mitigation
        if (!context.TankCooldownService.ShouldUseMitigation(hpPercent, damageRate, context.HasActiveMitigation))
            return false;

        // Don't stack with Holmgang
        if (context.HasHolmgang)
            return false;

        // Don't use if already have Rampart
        if (context.StatusHelper.HasRampart(player))
            return false;

        // Use Rampart on cooldown if configured, or only when needed
        if (!context.Configuration.Tank.UseRampartOnCooldown && context.HasActiveMitigation)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Rampart.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Rampart, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Rampart.Name;
            context.Debug.MitigationState = $"Rampart ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    #endregion

    #region Short Cooldowns

    private bool TryBloodwhetting(IAresContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.RawIntuition.MinLevel)
            return false;

        // Check if we should use short cooldown
        // Bloodwhetting is very strong and can be used more aggressively
        if (hpPercent > context.Configuration.Tank.MitigationThreshold)
            return false;

        // Don't use if already have Bloodwhetting/Raw Intuition active
        if (context.HasBloodwhetting)
            return false;

        // Don't stack with Holmgang (waste)
        if (context.HasHolmgang)
            return false;

        // Get the appropriate action
        var bloodwhettingAction = WARActions.GetBloodwhettingAction(level);

        if (!context.ActionService.IsActionReady(bloodwhettingAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(bloodwhettingAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = bloodwhettingAction.Name;
            context.Debug.MitigationState = $"Bloodwhetting ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryThrillOfBattle(IAresContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.ThrillOfBattle.MinLevel)
            return false;

        // Thrill of Battle is good for moderate damage or preemptive HP boost
        if (hpPercent > 0.70f)
            return false;

        // Don't stack with Holmgang
        if (context.HasHolmgang)
            return false;

        // Check if Thrill is active
        if (context.StatusHelper.HasThrillOfBattle(player))
            return false;

        if (!context.ActionService.IsActionReady(WARActions.ThrillOfBattle.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.ThrillOfBattle, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.ThrillOfBattle.Name;
            context.Debug.MitigationState = $"Thrill ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryEquilibrium(IAresContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Equilibrium.MinLevel)
            return false;

        // Equilibrium is a strong self-heal, use when HP is low
        if (hpPercent > 0.50f)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Equilibrium.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Equilibrium, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Equilibrium.Name;
            context.Debug.MitigationState = $"Equilibrium ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryArmsLength(IAresContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.ArmsLength.MinLevel)
            return false;

        // Arm's Length is primarily used for knockback immunity
        // For now, we'll skip automatic usage - requires mechanic detection
        // Could be enhanced with boss mechanic detection in the future

        return false;
    }

    #endregion

    #region Party Mitigation

    private bool TryReprisal(IAresContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Reprisal.MinLevel)
            return false;

        // Use Reprisal as a party mitigation tool
        // Best used before raidwides or during pulls with multiple enemies

        // Check if there are multiple enemies nearby
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        if (enemyCount < 2)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.Reprisal.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Reprisal, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Reprisal.Name;
            context.Debug.MitigationState = $"Reprisal ({enemyCount} enemies)";
            return true;
        }

        return false;
    }

    private bool TryShakeItOff(IAresContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.ShakeItOff.MinLevel)
            return false;

        // Shake It Off is a party shield and self-cleanse
        // Use proactively when party health is low
        var (avgHp, lowestHp, injuredCount) = context.PartyHealthMetrics;

        // Use when multiple party members are injured
        if (injuredCount < 3 || avgHp > 0.85f)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.ShakeItOff.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.ShakeItOff, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.ShakeItOff.Name;
            context.Debug.MitigationState = $"Shake It Off ({injuredCount} injured)";
            return true;
        }

        return false;
    }

    private bool TryNascentFlash(IAresContext context, float myHpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.NascentFlash.MinLevel)
            return false;

        // Don't use Nascent Flash if we're low HP ourselves
        if (myHpPercent < 0.60f)
            return false;

        // Find a party member who needs protection
        var flashTarget = context.PartyHelper.FindNascentFlashTarget(player, 0.60f);
        if (flashTarget == null)
            return false;

        // Check distance to target (Nascent Flash range is 30y)
        var dx = player.Position.X - flashTarget.Position.X;
        var dy = player.Position.Y - flashTarget.Position.Y;
        var dz = player.Position.Z - flashTarget.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (distance > 30f)
            return false;

        if (!context.ActionService.IsActionReady(WARActions.NascentFlash.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.NascentFlash, flashTarget.GameObjectId))
        {
            var targetHp = context.PartyHelper.GetHpPercent(flashTarget);
            context.Debug.PlannedAction = WARActions.NascentFlash.Name;
            context.Debug.MitigationState = $"Nascent Flash ({targetHp:P0} HP ally)";
            return true;
        }

        return false;
    }

    #endregion
}
