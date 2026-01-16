using Olympus.Data;
using Olympus.Rotation.NyxCore.Context;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight defensive rotation.
/// Manages personal mitigation, party mitigation, and The Blackest Night with intelligent usage.
/// </summary>
public sealed class MitigationModule : INyxModule
{
    public int Priority => 10; // High priority for defensives
    public string Name => "Mitigation";

    public bool TryExecute(INyxContext context, bool isMoving)
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

        // CRITICAL: Skip ALL mitigation during Walking Dead - healers need to heal us to full
        if (context.HasWalkingDead)
        {
            context.Debug.MitigationState = "Walking Dead! Need heals";
            return false;
        }

        // Priority 1: Emergency invulnerability (Living Dead)
        if (TryLivingDead(context, hpPercent))
            return true;

        // Priority 2: The Blackest Night (intelligent usage)
        if (TryTheBlackestNight(context, hpPercent, damageRate))
            return true;

        // Priority 3: Major cooldown (Shadow Wall/Shadowed Vigil)
        if (TryShadowWall(context, hpPercent, damageRate))
            return true;

        // Priority 4: Rampart (role action)
        if (TryRampart(context, hpPercent, damageRate))
            return true;

        // Priority 5: Dark Mind (magic damage reduction)
        if (TryDarkMind(context, hpPercent))
            return true;

        // Priority 6: Oblation (self or ally)
        if (TryOblation(context, hpPercent))
            return true;

        // Priority 7: Dark Missionary (party protection)
        if (TryDarkMissionary(context))
            return true;

        // Priority 8: Reprisal (party mitigation via enemy debuff)
        if (TryReprisal(context))
            return true;

        // Priority 9: Arm's Length for knockback immunity
        if (TryArmsLength(context))
            return true;

        return false;
    }

    public void UpdateDebugState(INyxContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Emergency Mitigation

    private bool TryLivingDead(INyxContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.LivingDead.MinLevel)
            return false;

        // Only use Living Dead in emergencies
        if (hpPercent > 0.15f)
            return false;

        // Don't use if already under Living Dead or Walking Dead
        if (context.HasLivingDead || context.HasWalkingDead)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.LivingDead.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.LivingDead, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.LivingDead.Name;
            context.Debug.MitigationState = $"Emergency invuln ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    #endregion

    #region The Blackest Night - Intelligent Usage

    /// <summary>
    /// The Blackest Night intelligence matrix.
    /// TBN costs 3000 MP and provides 25% max HP shield.
    /// If the shield breaks, grants Dark Arts (free Edge/Flood of Shadow).
    /// This makes TBN damage-neutral when it breaks, and a DPS loss if it doesn't.
    /// </summary>
    private bool TryTheBlackestNight(INyxContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.TheBlackestNight.MinLevel)
            return false;

        // MP check - costs 3000 MP
        if (!context.HasEnoughMpForTbn)
        {
            context.Debug.MitigationState = "TBN: Not enough MP";
            return false;
        }

        // Don't use during Living Dead (waste)
        if (context.HasLivingDead)
            return false;

        // Already have TBN active
        if (context.HasTheBlackestNight)
            return false;

        // CRITICAL: Don't use if Dark Arts already active (would waste the proc)
        if (context.HasDarkArts)
        {
            context.Debug.MitigationState = "Dark Arts active, saving TBN";
            return false;
        }

        // TBN Decision Matrix:

        // 1. Emergency: HP < 40% - always use for survival
        if (hpPercent < 0.40f)
        {
            return ExecuteTbn(context, player.GameObjectId, "Emergency TBN");
        }

        // 2. Main tank taking meaningful damage: HP < 60%
        if (context.IsMainTank && hpPercent < 0.60f && damageRate > 0.03f)
        {
            return ExecuteTbn(context, player.GameObjectId, "Reactive TBN");
        }

        // 3. MP pooling consideration:
        // If MP is high (>6000) and HP is moderate (<70%), use TBN
        // This ensures we get Dark Arts procs for damage while not wasting MP
        if (context.CurrentMp >= 6000 && hpPercent < 0.70f && context.IsMainTank)
        {
            // Check if we're taking damage (shield will likely break)
            if (damageRate > 0.02f)
            {
                return ExecuteTbn(context, player.GameObjectId, "MP dump TBN");
            }
        }

        // 4. Consider TBN on party members (future enhancement)
        // For now, only self-TBN is implemented
        // Party TBN can be useful during raidwides to guarantee shield break

        return false;
    }

    private bool ExecuteTbn(INyxContext context, ulong targetId, string reason)
    {
        if (context.ActionService.ExecuteOgcd(DRKActions.TheBlackestNight, targetId))
        {
            context.Debug.PlannedAction = DRKActions.TheBlackestNight.Name;
            context.Debug.MitigationState = reason;
            return true;
        }
        return false;
    }

    #endregion

    #region Major Cooldowns

    private bool TryShadowWall(INyxContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine which Shadow Wall variant to use
        var shadowWallAction = DRKActions.GetShadowWallAction(level);

        if (level < DRKActions.ShadowWall.MinLevel)
            return false;

        // Check if we should use major cooldown
        if (!context.TankCooldownService.ShouldUseMajorCooldown(hpPercent, damageRate))
            return false;

        // Don't stack with Living Dead
        if (context.HasLivingDead)
            return false;

        // Don't stack with existing Shadow Wall
        if (context.HasShadowWall)
            return false;

        if (!context.ActionService.IsActionReady(shadowWallAction.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(shadowWallAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = shadowWallAction.Name;
            context.Debug.MitigationState = $"Major CD ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryRampart(INyxContext context, float hpPercent, float damageRate)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Rampart.MinLevel)
            return false;

        // Check if we should use mitigation
        if (!context.TankCooldownService.ShouldUseMitigation(hpPercent, damageRate, context.HasActiveMitigation))
            return false;

        // Don't stack with Living Dead
        if (context.HasLivingDead)
            return false;

        // Don't use if already have Rampart
        if (context.StatusHelper.HasRampart(player))
            return false;

        // Use Rampart on cooldown if configured, or only when needed
        if (!context.Configuration.Tank.UseRampartOnCooldown && context.HasActiveMitigation)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.Rampart.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.Rampart, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.Rampart.Name;
            context.Debug.MitigationState = $"Rampart ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    #endregion

    #region Short Cooldowns

    private bool TryDarkMind(INyxContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.DarkMind.MinLevel)
            return false;

        // Dark Mind only reduces magic damage
        // For now, use when HP is low (future: detect incoming magic damage)
        if (hpPercent > context.Configuration.Tank.MitigationThreshold)
            return false;

        // Don't stack with Living Dead
        if (context.HasLivingDead)
            return false;

        // Don't use if already have Dark Mind active
        if (context.HasDarkMind)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.DarkMind.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.DarkMind, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.DarkMind.Name;
            context.Debug.MitigationState = $"Dark Mind ({hpPercent:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryOblation(INyxContext context, float hpPercent)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Oblation.MinLevel)
            return false;

        // Oblation is a 10% DR with 2 charges
        // Use on self when taking damage, or on allies who need help

        // First, check if we need it ourselves
        if (hpPercent < 0.60f && !context.HasOblation && !context.HasLivingDead)
        {
            if (context.ActionService.IsActionReady(DRKActions.Oblation.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(DRKActions.Oblation, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DRKActions.Oblation.Name;
                    context.Debug.MitigationState = $"Self Oblation ({hpPercent:P0} HP)";
                    return true;
                }
            }
        }

        // If we're fine, look for party members who need help
        if (hpPercent >= 0.60f)
        {
            var oblationTarget = context.PartyHelper.FindOblationTarget(player, 0.60f);
            if (oblationTarget != null)
            {
                // Check distance (Oblation range is 30y)
                var dx = player.Position.X - oblationTarget.Position.X;
                var dy = player.Position.Y - oblationTarget.Position.Y;
                var dz = player.Position.Z - oblationTarget.Position.Z;
                var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

                if (distance <= 30f && context.ActionService.IsActionReady(DRKActions.Oblation.ActionId))
                {
                    if (context.ActionService.ExecuteOgcd(DRKActions.Oblation, oblationTarget.GameObjectId))
                    {
                        var targetHp = context.PartyHelper.GetHpPercent(oblationTarget);
                        context.Debug.PlannedAction = DRKActions.Oblation.Name;
                        context.Debug.MitigationState = $"Oblation ({targetHp:P0} HP ally)";
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool TryArmsLength(INyxContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.ArmsLength.MinLevel)
            return false;

        // Arm's Length is primarily used for knockback immunity
        // For now, we'll skip automatic usage - requires mechanic detection
        // Could be enhanced with boss mechanic detection in the future

        return false;
    }

    #endregion

    #region Party Mitigation

    private bool TryDarkMissionary(INyxContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.DarkMissionary.MinLevel)
            return false;

        // Dark Missionary is party-wide magic damage reduction
        // Best used before raidwides

        // Check if multiple party members are injured (sign of incoming damage)
        var (avgHp, lowestHp, injuredCount) = context.PartyHealthMetrics;

        // Use when multiple party members are injured
        if (injuredCount < 3 || avgHp > 0.85f)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.DarkMissionary.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.DarkMissionary, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.DarkMissionary.Name;
            context.Debug.MitigationState = $"Dark Missionary ({injuredCount} injured)";
            return true;
        }

        return false;
    }

    private bool TryReprisal(INyxContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Reprisal.MinLevel)
            return false;

        // Use Reprisal as a party mitigation tool
        // Best used before raidwides or during pulls with multiple enemies

        // Check if there are multiple enemies nearby
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        if (enemyCount < 2)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.Reprisal.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.Reprisal, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.Reprisal.Name;
            context.Debug.MitigationState = $"Reprisal ({enemyCount} enemies)";
            return true;
        }

        return false;
    }

    #endregion
}
