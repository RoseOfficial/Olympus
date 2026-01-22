using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.CalliopeCore.Context;

namespace Olympus.Rotation.CalliopeCore.Modules;

/// <summary>
/// Handles the Bard damage rotation.
/// Manages procs, DoTs, Apex Arrow, and filler GCDs.
/// </summary>
public sealed class DamageModule : ICalliopeModule
{
    public int Priority => 30; // Lowest priority - damage after buffs
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    // DoT refresh window (refresh between 3-7s remaining)
    private const float DotRefreshMin = 3f;
    private const float DotRefreshMax = 7f;

    public bool TryExecute(ICalliopeContext context, bool isMoving)
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
            FFXIVConstants.RangedTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // oGCD: Interrupt enemy casts (highest priority)
        if (context.CanExecuteOgcd && TryInterrupt(context, target))
            return true;

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(12f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // GCD Phase
        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        // === PROC PRIORITY (highest damage GCDs) ===

        // Priority 1: Resonant Arrow (after Barrage)
        if (TryResonantArrow(context, target))
            return true;

        // Priority 2: Radiant Encore (after Radiant Finale)
        if (TryRadiantEncore(context, target))
            return true;

        // Priority 3: Blast Arrow (after 80+ Soul Voice Apex Arrow)
        if (TryBlastArrow(context, target))
            return true;

        // Priority 4: Refulgent Arrow with Barrage (triple damage)
        if (TryBarragedRefulgent(context, target, enemyCount))
            return true;

        // Priority 5: Refulgent Arrow (Hawk's Eye proc)
        if (TryRefulgentArrow(context, target, enemyCount))
            return true;

        // === RESOURCE SPENDERS ===

        // Priority 6: Apex Arrow at 80+ during burst, or 100 to avoid overcap
        if (TryApexArrow(context, target))
            return true;

        // === DoT MANAGEMENT ===

        // Priority 7: Iron Jaws to refresh DoTs (or snapshot buffs)
        if (TryIronJaws(context, target))
            return true;

        // Priority 8: Apply DoTs if missing
        if (TryApplyDots(context, target))
            return true;

        // === FILLER ===

        // Priority 9: Filler GCD (Burst Shot / Heavy Shot)
        if (TryFiller(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(ICalliopeContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Procs

    private bool TryResonantArrow(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.ResonantArrow.MinLevel)
            return false;

        if (!context.HasResonantArrowReady)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.ResonantArrow.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BRDActions.ResonantArrow, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.ResonantArrow.Name;
            context.Debug.DamageState = "Resonant Arrow (Barrage follow-up)";
            return true;
        }

        return false;
    }

    private bool TryRadiantEncore(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.RadiantEncore.MinLevel)
            return false;

        if (!context.HasRadiantEncoreReady)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.RadiantEncore.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BRDActions.RadiantEncore, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.RadiantEncore.Name;
            context.Debug.DamageState = "Radiant Encore (RF follow-up)";
            return true;
        }

        return false;
    }

    private bool TryBlastArrow(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.BlastArrow.MinLevel)
            return false;

        if (!context.HasBlastArrowReady)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.BlastArrow.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BRDActions.BlastArrow, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.BlastArrow.Name;
            context.Debug.DamageState = "Blast Arrow (Apex follow-up)";
            return true;
        }

        return false;
    }

    private bool TryBarragedRefulgent(ICalliopeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Must have Barrage active
        if (!context.HasBarrage)
            return false;

        // Must have Hawk's Eye for Refulgent
        if (!context.HasHawksEye)
            return false;

        // For AoE, Barrage still best used on Refulgent (higher potency per hit)
        var action = BRDActions.GetProcAction(level);

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = $"Barrage + {action.Name}";
            context.Debug.DamageState = "Barraged Refulgent Arrow";
            return true;
        }

        return false;
    }

    private bool TryRefulgentArrow(ICalliopeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (!context.HasHawksEye)
            return false;

        // Use Shadowbite for AoE
        if (enemyCount >= AoeThreshold && level >= BRDActions.Shadowbite.MinLevel)
        {
            if (context.ActionService.IsActionReady(BRDActions.Shadowbite.ActionId))
            {
                if (context.ActionService.ExecuteGcd(BRDActions.Shadowbite, target.GameObjectId))
                {
                    context.Debug.PlannedAction = BRDActions.Shadowbite.Name;
                    context.Debug.DamageState = "Shadowbite (AoE proc)";
                    return true;
                }
            }
        }

        // Single target proc
        var action = BRDActions.GetProcAction(level);

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = "Refulgent Arrow (proc)";
            return true;
        }

        return false;
    }

    #endregion

    #region Apex Arrow

    private bool TryApexArrow(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.ApexArrow.MinLevel)
            return false;

        // Use at 80+ during burst for Blast Arrow follow-up
        // Or use at 100 to avoid overcapping
        bool shouldUse = false;

        if (context.SoulVoice >= 100)
        {
            shouldUse = true;
        }
        else if (context.SoulVoice >= 80)
        {
            // During burst window or no buffs to wait for
            shouldUse = context.HasRagingStrikes ||
                        !context.ActionService.IsActionReady(BRDActions.RagingStrikes.ActionId);
        }

        if (!shouldUse)
        {
            context.Debug.DamageState = $"Apex Arrow: {context.SoulVoice}/80";
            return false;
        }

        if (!context.ActionService.IsActionReady(BRDActions.ApexArrow.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BRDActions.ApexArrow, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.ApexArrow.Name;
            context.Debug.DamageState = $"Apex Arrow ({context.SoulVoice} SV)";
            return true;
        }

        return false;
    }

    #endregion

    #region DoT Management

    private bool TryIronJaws(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BRDActions.IronJaws.MinLevel)
            return false;

        // Need both DoTs on target to refresh
        if (!context.HasCausticBite || !context.HasStormbite)
            return false;

        // Refresh in the window (3-7s remaining)
        bool needsRefresh = context.CausticBiteRemaining <= DotRefreshMax ||
                            context.StormbiteRemaining <= DotRefreshMax;

        // Snapshot buffs with Iron Jaws during burst
        bool snapshotBuffs = context.HasRagingStrikes &&
                             context.CausticBiteRemaining < 20f; // Don't clip too early

        if (!needsRefresh && !snapshotBuffs)
            return false;

        // Don't let DoTs fall off
        if (context.CausticBiteRemaining < DotRefreshMin || context.StormbiteRemaining < DotRefreshMin)
            needsRefresh = true;

        if (!needsRefresh && !snapshotBuffs)
            return false;

        if (!context.ActionService.IsActionReady(BRDActions.IronJaws.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BRDActions.IronJaws, target.GameObjectId))
        {
            context.Debug.PlannedAction = BRDActions.IronJaws.Name;
            var reason = snapshotBuffs ? "snapshot buffs" : "refresh";
            context.Debug.DamageState = $"Iron Jaws ({reason})";
            return true;
        }

        return false;
    }

    private bool TryApplyDots(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Apply Stormbite first (higher potency DoT)
        if (!context.HasStormbite && level >= BRDActions.Windbite.MinLevel)
        {
            var stormAction = BRDActions.GetStormbite(level);
            if (context.ActionService.IsActionReady(stormAction.ActionId))
            {
                if (context.ActionService.ExecuteGcd(stormAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = stormAction.Name;
                    context.Debug.DamageState = $"{stormAction.Name} applied";
                    return true;
                }
            }
        }

        // Apply Caustic Bite
        if (!context.HasCausticBite && level >= BRDActions.VenomousBite.MinLevel)
        {
            var causticAction = BRDActions.GetCausticBite(level);
            if (context.ActionService.IsActionReady(causticAction.ActionId))
            {
                if (context.ActionService.ExecuteGcd(causticAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = causticAction.Name;
                    context.Debug.DamageState = $"{causticAction.Name} applied";
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Filler

    private bool TryFiller(ICalliopeContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE filler
        if (enemyCount >= AoeThreshold && level >= BRDActions.QuickNock.MinLevel)
        {
            var aoeAction = BRDActions.GetAoeFiller(level);
            if (context.ActionService.IsActionReady(aoeAction.ActionId))
            {
                if (context.ActionService.ExecuteGcd(aoeAction, player.GameObjectId))
                {
                    context.Debug.PlannedAction = aoeAction.Name;
                    context.Debug.DamageState = $"{aoeAction.Name} (AoE filler)";
                    return true;
                }
            }
        }

        // Single target filler
        var action = BRDActions.GetFiller(level);

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (filler)";
            return true;
        }

        return false;
    }

    #endregion

    #region Interrupt

    /// <summary>
    /// Attempts to interrupt an enemy cast using Head Graze.
    /// Coordinates with other Olympus instances to prevent duplicate interrupts.
    /// </summary>
    private bool TryInterrupt(ICalliopeContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Need at least Head Graze (Lv.24)
        if (level < BRDActions.HeadGraze.MinLevel)
            return false;

        // Check if target is casting something interruptible
        if (!target.IsCasting)
            return false;

        // Check the cast interruptible flag (game indicates this)
        if (!target.IsCastInterruptible)
            return false;

        var targetId = target.EntityId;
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;

        // Check IPC reservation
        if (coordConfig.EnableInterruptCoordination &&
            partyCoord?.IsInterruptTargetReservedByOther(targetId) == true)
        {
            context.Debug.DamageState = "Interrupt reserved by other";
            return false;
        }

        // Calculate remaining cast time in milliseconds
        var remainingCastTime = (target.TotalCastTime - target.CurrentCastTime) * 1000f;
        var castTimeMs = (int)remainingCastTime;

        // Try Head Graze
        if (context.ActionService.IsActionReady(BRDActions.HeadGraze.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, BRDActions.HeadGraze.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.DamageState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(BRDActions.HeadGraze, target.GameObjectId))
            {
                context.Debug.PlannedAction = BRDActions.HeadGraze.Name;
                context.Debug.DamageState = "Interrupted cast";
                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        return false;
    }

    #endregion
}
