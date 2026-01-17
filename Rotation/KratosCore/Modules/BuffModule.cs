using Olympus.Data;
using Olympus.Rotation.KratosCore.Context;

namespace Olympus.Rotation.KratosCore.Modules;

/// <summary>
/// Handles the Monk buff management.
/// Manages Riddle of Fire (burst window), Brotherhood (party buff),
/// Perfect Balance (Blitz setup), and Riddle of Wind (auto-attacks).
/// </summary>
public sealed class BuffModule : IKratosModule
{
    public int Priority => 20; // After role actions
    public string Name => "Buff";

    public bool TryExecute(IKratosContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        // Only use buff actions during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Priority 1: Riddle of Fire (personal burst)
        if (TryRiddleOfFire(context))
            return true;

        // Priority 2: Brotherhood (party buff)
        if (TryBrotherhood(context))
            return true;

        // Priority 3: Perfect Balance (Blitz setup)
        if (TryPerfectBalance(context))
            return true;

        // Priority 4: Riddle of Wind (auto-attack speed)
        if (TryRiddleOfWind(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IKratosContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Riddle of Fire

    private bool TryRiddleOfFire(IKratosContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.RiddleOfFire.MinLevel)
            return false;

        // Already have Riddle of Fire active
        if (context.HasRiddleOfFire)
        {
            context.Debug.BuffState = $"RoF active ({context.RiddleOfFireRemaining:F1}s)";
            return false;
        }

        // Requirements for optimal Riddle of Fire usage:
        // 1. Disciplined Fist should be active (personal damage buff)
        // 2. Ideally align with Brotherhood window
        if (!context.HasDisciplinedFist)
        {
            context.Debug.BuffState = "Waiting for Disciplined Fist";
            return false;
        }

        // Check if Riddle of Fire is ready
        if (!context.ActionService.IsActionReady(MNKActions.RiddleOfFire.ActionId))
        {
            context.Debug.BuffState = "RoF on cooldown";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(MNKActions.RiddleOfFire, player.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.RiddleOfFire.Name;
            context.Debug.BuffState = "Activating Riddle of Fire";
            return true;
        }

        return false;
    }

    #endregion

    #region Brotherhood

    private bool TryBrotherhood(IKratosContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.Brotherhood.MinLevel)
            return false;

        // Already have Brotherhood active
        if (context.HasBrotherhood)
        {
            context.Debug.BuffState = "Brotherhood active";
            return false;
        }

        // Brotherhood is a party buff - ideally use with Riddle of Fire
        // But don't hold it too long
        if (!context.ActionService.IsActionReady(MNKActions.Brotherhood.ActionId))
        {
            context.Debug.BuffState = "Brotherhood on cooldown";
            return false;
        }

        // Best timing: Use with or just before Riddle of Fire
        // If RoF is active or ready, use Brotherhood
        if (!context.HasRiddleOfFire &&
            context.ActionService.IsActionReady(MNKActions.RiddleOfFire.ActionId) == false)
        {
            // RoF on cooldown and not active - wait for alignment if possible
            // But don't wait too long (Brotherhood has 120s CD)
            context.Debug.BuffState = "Waiting for RoF alignment";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(MNKActions.Brotherhood, player.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.Brotherhood.Name;
            context.Debug.BuffState = "Activating Brotherhood";
            return true;
        }

        return false;
    }

    #endregion

    #region Perfect Balance

    private bool TryPerfectBalance(IKratosContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.PerfectBalance.MinLevel)
            return false;

        // Already in Perfect Balance
        if (context.HasPerfectBalance)
        {
            context.Debug.BuffState = $"PB active ({context.PerfectBalanceStacks} stacks)";
            return false;
        }

        // Don't use if we already have 3 Beast Chakra (about to Blitz)
        if (context.BeastChakraCount >= 3)
        {
            context.Debug.BuffState = "Ready to Blitz";
            return false;
        }

        // Optimal Perfect Balance usage:
        // 1. Use during Riddle of Fire window for burst
        // 2. Build towards the Blitz we need based on Nadi state

        // Check if Perfect Balance is ready
        if (!context.ActionService.IsActionReady(MNKActions.PerfectBalance.ActionId))
        {
            context.Debug.BuffState = "PB on cooldown";
            return false;
        }

        // Best used during Riddle of Fire for maximum burst
        // But don't hold charges too long (2 charges at level 82+)
        bool shouldUsePB = context.HasRiddleOfFire ||
                           (context.HasDisciplinedFist && !context.ActionService.IsActionReady(MNKActions.RiddleOfFire.ActionId));

        if (!shouldUsePB)
        {
            context.Debug.BuffState = "Waiting for RoF for PB";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(MNKActions.PerfectBalance, player.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.PerfectBalance.Name;
            context.Debug.BuffState = "Activating Perfect Balance";
            return true;
        }

        return false;
    }

    #endregion

    #region Riddle of Wind

    private bool TryRiddleOfWind(IKratosContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.RiddleOfWind.MinLevel)
            return false;

        // Already have Riddle of Wind active
        if (context.HasRiddleOfWind)
        {
            context.Debug.BuffState = "RoW active";
            return false;
        }

        // Riddle of Wind increases auto-attack speed
        // Use during burst windows or as filler
        if (!context.ActionService.IsActionReady(MNKActions.RiddleOfWind.ActionId))
        {
            context.Debug.BuffState = "RoW on cooldown";
            return false;
        }

        // Use freely as a damage increase
        if (context.ActionService.ExecuteOgcd(MNKActions.RiddleOfWind, player.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.RiddleOfWind.Name;
            context.Debug.BuffState = "Activating Riddle of Wind";
            return true;
        }

        return false;
    }

    #endregion
}
