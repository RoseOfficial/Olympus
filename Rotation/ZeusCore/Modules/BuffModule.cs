using Olympus.Data;
using Olympus.Rotation.ZeusCore.Context;

namespace Olympus.Rotation.ZeusCore.Modules;

/// <summary>
/// Handles the Dragoon buff management.
/// Manages Lance Charge (personal damage), Battle Litany (party crit),
/// Life Surge (guaranteed crit), and Dragon Sight (tether buff).
/// </summary>
public sealed class BuffModule : IZeusModule
{
    public int Priority => 20; // Higher priority than damage
    public string Name => "Buff";

    public bool TryExecute(IZeusContext context, bool isMoving)
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

        // Priority 1: Life Surge (before high-potency GCDs)
        if (TryLifeSurge(context))
            return true;

        // Priority 2: Lance Charge (personal burst)
        if (TryLanceCharge(context))
            return true;

        // Priority 3: Battle Litany (party buff)
        if (TryBattleLitany(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IZeusContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Life Surge

    private bool TryLifeSurge(IZeusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRGActions.LifeSurge.MinLevel)
            return false;

        // Already have Life Surge active
        if (context.HasLifeSurge)
        {
            context.Debug.BuffState = "Life Surge active";
            return false;
        }

        // Check if Life Surge is ready
        if (!context.ActionService.IsActionReady(DRGActions.LifeSurge.ActionId))
        {
            context.Debug.BuffState = "Life Surge on cooldown";
            return false;
        }

        // Use Life Surge before high-potency GCDs:
        // - Heavens' Thrust / Full Thrust (after Vorpal)
        // - Drakesbane / Wheeling Thrust / Fang and Claw (finisher procs)
        // - Coerthan Torment (AoE finisher)

        var shouldUseLifeSurge = false;

        // About to use a finisher proc
        if (context.HasFangAndClawBared || context.HasWheelInMotion)
        {
            shouldUseLifeSurge = true;
        }
        // About to use Heavens' Thrust / Full Thrust (after Vorpal Thrust in combo)
        else if (context.LastComboAction == DRGActions.VorpalThrust.ActionId &&
                 context.ComboTimeRemaining > 0)
        {
            shouldUseLifeSurge = true;
        }
        // About to use Coerthan Torment (after Sonic Thrust in AoE combo)
        else if (context.LastComboAction == DRGActions.SonicThrust.ActionId &&
                 context.ComboTimeRemaining > 0 &&
                 level >= DRGActions.CoerthanTorment.MinLevel)
        {
            shouldUseLifeSurge = true;
        }

        if (!shouldUseLifeSurge)
        {
            context.Debug.BuffState = "Waiting for high-potency GCD";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(DRGActions.LifeSurge, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.LifeSurge.Name;
            context.Debug.BuffState = "Activating Life Surge";
            return true;
        }

        return false;
    }

    #endregion

    #region Lance Charge

    private bool TryLanceCharge(IZeusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRGActions.LanceCharge.MinLevel)
            return false;

        // Already have Lance Charge active
        if (context.HasLanceCharge)
        {
            context.Debug.BuffState = $"Lance Charge active ({context.LanceChargeRemaining:F1}s)";
            return false;
        }

        // Check if Lance Charge is ready
        if (!context.ActionService.IsActionReady(DRGActions.LanceCharge.ActionId))
        {
            context.Debug.BuffState = "Lance Charge on cooldown";
            return false;
        }

        // Requirements for optimal Lance Charge usage:
        // 1. Power Surge should be active (personal damage buff)
        // 2. Ideally align with Battle Litany for maximum burst
        if (!context.HasPowerSurge)
        {
            context.Debug.BuffState = "Waiting for Power Surge";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(DRGActions.LanceCharge, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.LanceCharge.Name;
            context.Debug.BuffState = "Activating Lance Charge";
            return true;
        }

        return false;
    }

    #endregion

    #region Battle Litany

    private bool TryBattleLitany(IZeusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRGActions.BattleLitany.MinLevel)
            return false;

        // Already have Battle Litany active
        if (context.HasBattleLitany)
        {
            context.Debug.BuffState = $"Battle Litany active ({context.BattleLitanyRemaining:F1}s)";
            return false;
        }

        // Check if Battle Litany is ready
        if (!context.ActionService.IsActionReady(DRGActions.BattleLitany.ActionId))
        {
            context.Debug.BuffState = "Battle Litany on cooldown";
            return false;
        }

        // Battle Litany is a party buff - use with Lance Charge for maximum burst
        // But don't hold it too long (120s CD)
        var shouldUseLitany = context.HasLanceCharge ||
                              context.ActionService.IsActionReady(DRGActions.LanceCharge.ActionId);

        if (!shouldUseLitany)
        {
            // If Lance Charge is on cooldown and we don't have it, wait for alignment
            // unless we've been waiting too long
            context.Debug.BuffState = "Waiting for Lance Charge alignment";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(DRGActions.BattleLitany, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.BattleLitany.Name;
            context.Debug.BuffState = "Activating Battle Litany";
            return true;
        }

        return false;
    }

    #endregion
}
