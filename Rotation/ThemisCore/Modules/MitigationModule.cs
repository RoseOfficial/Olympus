using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ThemisCore.Context;

namespace Olympus.Rotation.ThemisCore.Modules;

/// <summary>
/// Handles the Paladin defensive rotation.
/// Manages personal mitigation, party mitigation, and invulnerability.
/// </summary>
public sealed class MitigationModule : IThemisModule
{
    public int Priority => 10; // High priority for defensives
    public string Name => "Mitigation";

    public bool TryExecute(IThemisContext context, bool isMoving)
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

        // Priority 1: Emergency invulnerability (Hallowed Ground)
        if (TryHallowedGround(context, hpPercent))
            return true;

        // Priority 2: Major cooldown (Sentinel/Guardian)
        if (TryMajorCooldown(context, hpPercent, damageRate))
            return true;

        // Priority 3: Rampart (role action)
        if (TryRampart(context, hpPercent, damageRate))
            return true;

        // Priority 4: Short cooldown (Sheltron/Holy Sheltron)
        if (TrySheltron(context, hpPercent))
            return true;

        // Priority 5: Bulwark (if available and not overlapping)
        if (TryBulwark(context, hpPercent, damageRate))
            return true;

        // Priority 6: Arm's Length for knockback immunity
        if (TryArmsLength(context))
            return true;

        // Priority 7: Party mitigation (Reprisal)
        if (TryReprisal(context))
            return true;

        // Priority 8: Divine Veil for party protection
        if (TryDivineVeil(context))
            return true;

        // Priority 9: Cover for protecting low HP ally
        if (TryCover(context, hpPercent))
            return true;

        return false;
    }

    public void UpdateDebugState(IThemisContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Emergency Mitigation

    private bool TryHallowedGround(IThemisContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.HallowedGround.MinLevel)
            return false;

        // Only use Hallowed Ground in emergencies
        if (hpPercent > 0.15f)
            return false;

        // Don't use if already under Hallowed Ground
        if (context.HasHallowedGround)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.HallowedGround.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.HallowedGround, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.HallowedGround.Name;
            context.Debug.MitigationState = $"Emergency invuln ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    #endregion

    #region Major Cooldowns

    private bool TryMajorCooldown(IThemisContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine which Sentinel variant to use
        var sentinelAction = PLDActions.GetSentinelAction(level);

        if (level < PLDActions.Sentinel.MinLevel)
            return false;

        // Check if we should use major cooldown
        if (!context.TankCooldownService.ShouldUseMajorCooldown(hpPercent, damageRate))
            return false;

        // Don't stack with Hallowed Ground
        if (context.HasHallowedGround)
            return false;

        // Don't stack with existing Sentinel/Guardian
        if (context.StatusHelper.HasSentinel(player))
            return false;

        if (!context.ActionService.IsActionReady(sentinelAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(sentinelAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = sentinelAction.Name;
            context.Debug.MitigationState = $"Major CD ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryRampart(IThemisContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Rampart.MinLevel)
            return false;

        // Check if we should use mitigation
        if (!context.TankCooldownService.ShouldUseMitigation(hpPercent, damageRate, context.HasActiveMitigation))
            return false;

        // Don't stack with Hallowed Ground
        if (context.HasHallowedGround)
            return false;

        // Don't use if already have Rampart
        if (context.StatusHelper.HasRampart(player))
            return false;

        // Use Rampart on cooldown if configured, or only when needed
        if (!context.Configuration.Tank.UseRampartOnCooldown && context.HasActiveMitigation)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.Rampart.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.Rampart, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.Rampart.Name;
            context.Debug.MitigationState = $"Rampart ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    #endregion

    #region Short Cooldowns

    private bool TrySheltron(IThemisContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Sheltron.MinLevel)
            return false;

        // Check if we should use short cooldown
        if (!context.TankCooldownService.ShouldUseShortCooldown(
            hpPercent,
            context.OathGauge,
            context.Configuration.Tank.SheltronMinGauge))
            return false;

        // Don't use if already have Sheltron active
        if (context.StatusHelper.HasSheltron(player))
            return false;

        // Don't stack with Hallowed Ground (waste of gauge)
        if (context.HasHallowedGround)
            return false;

        // Get the appropriate Sheltron action
        var sheltronAction = PLDActions.GetSheltronAction(level);

        if (!context.ActionService.IsActionReady(sheltronAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(sheltronAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = sheltronAction.Name;
            context.Debug.MitigationState = $"Sheltron ({context.OathGauge} gauge)";
            return true;
        }

        return false;
    }

    private bool TryBulwark(IThemisContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Bulwark.MinLevel)
            return false;

        // Bulwark is good for sustained damage, not tank busters
        // Use when taking moderate consistent damage
        if (damageRate < 300f || hpPercent > 0.80f)
            return false;

        // Don't stack with Hallowed Ground
        if (context.HasHallowedGround)
            return false;

        // Check if Bulwark is active
        if (context.StatusHelper.HasStatus(player, PLDActions.StatusIds.Bulwark))
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.Bulwark.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.Bulwark, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.Bulwark.Name;
            context.Debug.MitigationState = "Bulwark (sustained damage)";
            return true;
        }

        return false;
    }

    private bool TryArmsLength(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.ArmsLength.MinLevel)
            return false;

        // Arm's Length is primarily used for knockback immunity
        // For now, we'll skip automatic usage - requires mechanic detection
        // Could be enhanced with boss mechanic detection in the future

        return false;
    }

    #endregion

    #region Party Mitigation

    private bool TryReprisal(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Reprisal.MinLevel)
            return false;

        // Use Reprisal as a party mitigation tool
        // Best used before raidwides or during pulls with multiple enemies

        // Check if there are multiple enemies nearby
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        if (enemyCount < 2)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.Reprisal.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.Reprisal, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.Reprisal.Name;
            context.Debug.MitigationState = $"Reprisal ({enemyCount} enemies)";
            return true;
        }

        return false;
    }

    private bool TryDivineVeil(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.DivineVeil.MinLevel)
            return false;

        // Divine Veil needs to be triggered by a heal
        // Use proactively when party health is low
        var (avgHp, lowestHp, injuredCount) = context.PartyHealthMetrics;

        // Use when multiple party members are injured
        if (injuredCount < 3 || avgHp > 0.85f)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.DivineVeil.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.DivineVeil, player.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.DivineVeil.Name;
            context.Debug.MitigationState = $"Divine Veil ({injuredCount} injured)";
            return true;
        }

        return false;
    }

    private bool TryCover(IThemisContext context, float myHpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Cover.MinLevel)
            return false;

        // Don't cover if we're low HP ourselves
        if (myHpPercent < 0.60f)
            return false;

        // Find a party member who needs covering
        var coverTarget = context.PartyHelper.FindCoverTarget(player, 0.40f);
        if (coverTarget == null)
            return false;

        // Check distance to target (Cover range is 10y)
        var dx = player.Position.X - coverTarget.Position.X;
        var dy = player.Position.Y - coverTarget.Position.Y;
        var dz = player.Position.Z - coverTarget.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (distance > 10f)
            return false;

        if (!context.ActionService.IsActionReady(PLDActions.Cover.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(PLDActions.Cover, coverTarget.GameObjectId))
        {
            var targetHp = context.PartyHelper.GetHpPercent(coverTarget);
            context.Debug.PlannedAction = PLDActions.Cover.Name;
            context.Debug.MitigationState = $"Cover ({targetHp:P0} HP ally)";
            return true;
        }

        return false;
    }

    #endregion
}
