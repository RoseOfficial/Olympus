using Olympus.Data;
using Olympus.Rotation.HephaestusCore.Context;

namespace Olympus.Rotation.HephaestusCore.Modules;

/// <summary>
/// Handles the Gunbreaker buff management.
/// Manages tank stance, No Mercy, and Bloodfest.
/// </summary>
public sealed class BuffModule : IHephaestusModule
{
    public int Priority => 20; // Medium priority for buffs
    public string Name => "Buff";

    public bool TryExecute(IHephaestusContext context, bool isMoving)
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

        // Priority 1: Tank stance (Royal Guard) if missing
        if (TryRoyalGuard(context))
            return true;

        // Priority 2: No Mercy (damage buff)
        if (TryNoMercy(context))
            return true;

        // Priority 3: Bloodfest (cartridge generator)
        if (TryBloodfest(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IHephaestusContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Tank Stance

    private bool TryRoyalGuard(IHephaestusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.RoyalGuard.MinLevel)
            return false;

        // Check configuration
        if (!context.Configuration.Tank.AutoTankStance)
        {
            context.Debug.BuffState = "AutoTankStance disabled";
            return false;
        }

        // Already have Royal Guard
        if (context.HasRoyalGuard)
        {
            context.Debug.BuffState = "Royal Guard active";
            return false;
        }

        // Only auto-enable if we're in combat and don't have tank stance
        if (!context.ActionService.IsActionReady(GNBActions.RoyalGuard.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(GNBActions.RoyalGuard, player.GameObjectId))
        {
            context.Debug.PlannedAction = GNBActions.RoyalGuard.Name;
            context.Debug.BuffState = "Enabling Royal Guard";
            return true;
        }

        return false;
    }

    #endregion

    #region Damage Buffs

    private bool TryNoMercy(IHephaestusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.NoMercy.MinLevel)
            return false;

        // Don't use if already active
        if (context.HasNoMercy)
        {
            context.Debug.BuffState = $"No Mercy ({context.NoMercyRemaining:F1}s)";
            return false;
        }

        // No Mercy is a 60s cooldown, 20s duration
        // Best used when we have cartridges to spend during the window
        // At high levels, want to align with Double Down, Gnashing Fang, etc.

        // Basic usage: Use on cooldown during combat
        // More advanced: Would wait for Gnashing Fang to be ready
        // For now, use when ready and have at least some cartridges

        // At level 90+, prefer to have 2+ cartridges for Double Down
        if (level >= GNBActions.DoubleDown.MinLevel)
        {
            // Ideally have 2+ cartridges for Double Down
            // But don't delay too long - use if ready even with 1 cartridge
            if (context.Cartridges < 1)
            {
                context.Debug.BuffState = "No Mercy: waiting for cartridges";
                return false;
            }
        }

        if (!context.ActionService.IsActionReady(GNBActions.NoMercy.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(GNBActions.NoMercy, player.GameObjectId))
        {
            context.Debug.PlannedAction = GNBActions.NoMercy.Name;
            context.Debug.BuffState = "No Mercy activated";
            return true;
        }

        return false;
    }

    private bool TryBloodfest(IHephaestusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Bloodfest.MinLevel)
            return false;

        // Bloodfest grants 3 cartridges
        // Best used when cartridges are low to avoid overcap
        // At Lv.100+, also grants Ready to Reign

        // Don't waste - only use when we can get full benefit
        // Max cartridges = 3, so use when 0 to get full value
        // Can use at 1 cartridge if No Mercy is active (want to spend during buff)
        var maxBenefit = GNBActions.MaxCartridges - context.Cartridges;

        if (maxBenefit < 2)
        {
            // Would waste 2+ cartridges, wait
            context.Debug.BuffState = $"Bloodfest: {context.Cartridges}/3 cartridges (wait)";
            return false;
        }

        // Prefer to use during No Mercy for maximum damage output
        // But don't hold forever - use if cartridges are low
        if (!context.HasNoMercy && context.Cartridges > 0)
        {
            // Have some cartridges and no No Mercy - can wait
            // Unless No Mercy is on cooldown for a while
            return false;
        }

        if (!context.ActionService.IsActionReady(GNBActions.Bloodfest.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(GNBActions.Bloodfest, player.GameObjectId))
        {
            context.Debug.PlannedAction = GNBActions.Bloodfest.Name;
            context.Debug.BuffState = $"Bloodfest (+{GNBActions.BloodfestCartridges} cartridges)";
            return true;
        }

        return false;
    }

    #endregion
}
