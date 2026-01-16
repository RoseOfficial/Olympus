using Olympus.Data;
using Olympus.Rotation.ThemisCore.Context;

namespace Olympus.Rotation.ThemisCore.Modules;

/// <summary>
/// Handles the Paladin buff rotation.
/// Manages Fight or Flight and Requiescat timing for optimal damage windows.
/// </summary>
public sealed class BuffModule : IThemisModule
{
    public int Priority => 20; // After mitigation, before damage
    public string Name => "Buff";

    public bool TryExecute(IThemisContext context, bool isMoving)
    {
        if (!context.Configuration.Tank.EnableDamage)
        {
            context.Debug.BuffState = "Disabled";
            return false;
        }

        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        // Only use buffs during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        // Priority 1: Fight or Flight (damage buff)
        if (TryFightOrFlight(context))
            return true;

        // Priority 2: Requiescat (magic phase enabler)
        if (TryRequiescat(context))
            return true;

        // Priority 3: Tank stance management (Iron Will)
        if (TryIronWill(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IThemisContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Fight or Flight

    private bool TryFightOrFlight(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.FightOrFlight.MinLevel)
            return false;

        // Don't use if already active
        if (context.HasFightOrFlight)
        {
            context.Debug.BuffState = $"FoF active ({context.FightOrFlightRemaining:F1}s)";
            return false;
        }

        // Check if ready
        if (!context.ActionService.IsActionReady(PLDActions.FightOrFlight.ActionId))
            return false;

        // Use Fight or Flight when:
        // 1. We're about to do burst (combo is ready)
        // 2. Not during magic phase (Requiescat active)
        // 3. Target is available

        // Don't use during Requiescat - it buffs physical damage
        if (context.HasRequiescat)
        {
            context.Debug.BuffState = "Waiting (Requiescat active)";
            return false;
        }

        // Find a target to verify we're in combat
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.MeleeTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.BuffState = "No target";
            return false;
        }

        // Optimal timing: Use at combo start or when Sword Oath stacks are available
        // This ensures we get maximum GCDs under the buff
        var goodTiming = context.ComboStep <= 1 || context.HasSwordOath;

        if (goodTiming)
        {
            if (context.ActionService.ExecuteOgcd(PLDActions.FightOrFlight, player.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.FightOrFlight.Name;
                context.Debug.BuffState = "Fight or Flight activated";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Requiescat

    private bool TryRequiescat(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Requiescat.MinLevel)
            return false;

        // Don't use if already active
        if (context.HasRequiescat)
        {
            context.Debug.BuffState = $"Requiescat active ({context.RequiescatStacks} stacks)";
            return false;
        }

        // Check if ready
        if (!context.ActionService.IsActionReady(PLDActions.Requiescat.ActionId))
            return false;

        // Find a target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.MeleeTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.BuffState = "No target for Requiescat";
            return false;
        }

        // Use Requiescat when:
        // 1. Fight or Flight is on cooldown or has less than 5s remaining
        // 2. We have enough MP (though Requiescat makes spells free)
        // 3. Not in the middle of a physical combo

        // Ideal timing: After Fight or Flight window ends
        var fofOnCooldown = !context.ActionService.IsActionReady(PLDActions.FightOrFlight.ActionId);
        var fofAlmostOver = context.HasFightOrFlight && context.FightOrFlightRemaining < 5f;
        var comboReady = context.ComboStep <= 1;

        // Use when FoF is on cooldown and we're not mid-combo
        if ((fofOnCooldown || fofAlmostOver) && comboReady)
        {
            if (context.ActionService.ExecuteOgcd(PLDActions.Requiescat, target.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.Requiescat.Name;
                context.Debug.BuffState = "Requiescat activated";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Tank Stance

    private bool TryIronWill(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.IronWill.MinLevel)
            return false;

        // Only manage tank stance if we're the main tank or need to be
        // This requires enmity tracking

        // If we're supposed to be main tank but don't have stance
        if (context.IsMainTank && !context.HasTankStance)
        {
            if (!context.ActionService.IsActionReady(PLDActions.IronWill.ActionId))
                return false;

            if (context.ActionService.ExecuteOgcd(PLDActions.IronWill, player.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.IronWill.Name;
                context.Debug.BuffState = "Iron Will activated";
                return true;
            }
        }

        // If we're not main tank but have stance, consider dropping it
        // This is situational and depends on party composition
        // For now, we won't automatically drop stance

        return false;
    }

    #endregion
}
