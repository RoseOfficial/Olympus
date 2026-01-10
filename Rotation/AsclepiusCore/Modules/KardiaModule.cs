using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Handles Kardia management for Sage.
/// Kardia places a buff on a target that heals them when the Sage deals damage.
/// Priority 3 - essential to have Kardia placed before combat.
/// </summary>
public sealed class KardiaModule : IAsclepiusModule
{
    public int Priority => 3; // Very high priority - Kardia is essential
    public string Name => "Kardia";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        // Priority 1: Place Kardia if not present
        if (context.CanExecuteOgcd && TryPlaceKardia(context))
            return true;

        // Priority 2: Soteria (boosted Kardia healing)
        if (context.CanExecuteOgcd && TrySoteria(context))
            return true;

        // Priority 3: Philosophia (party-wide Kardia)
        if (context.CanExecuteOgcd && TryPhilosophia(context))
            return true;

        // Priority 4: Swap Kardia to a more needy target
        if (context.CanExecuteOgcd && TrySwapKardia(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IAsclepiusContext context)
    {
        context.Debug.KardiaTarget = context.HasKardiaPlaced
            ? $"ID: {context.KardiaTargetId}"
            : "None";
        context.Debug.KardiaState = context.HasKardiaPlaced ? "Active" : "Not placed";
        context.Debug.SoteriaStacks = context.KardiaManager.GetSoteriaStacks(context.Player);
        context.Debug.SoteriaState = context.HasSoteria ? "Active" : "Idle";
        context.Debug.PhilosophiaState = context.HasPhilosophia ? "Active" : "Idle";
    }

    private bool TryPlaceKardia(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.AutoKardia)
            return false;

        // Already have Kardia placed
        if (context.HasKardiaPlaced)
            return false;

        if (player.Level < SGEActions.Kardia.MinLevel)
            return false;

        // Find the main tank to place Kardia on
        var target = FindKardiaTarget(context);
        if (target == null)
            return false;

        var action = SGEActions.Kardia;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.KardiaManager.RecordSwap(target.GameObjectId);
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Placing Kardia";
            context.LogKardiaDecision(target.Name?.TextValue ?? "Unknown", "Place", "Tank needs Kardia");
            return true;
        }

        return false;
    }

    private bool TrySoteria(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableSoteria)
            return false;

        if (player.Level < SGEActions.Soteria.MinLevel)
            return false;

        // Already have Soteria active
        if (context.HasSoteria)
            return false;

        // Must have Kardia placed
        if (!context.HasKardiaPlaced)
            return false;

        // Check cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Soteria.ActionId))
            return false;

        // Use when Kardia target is low
        var kardiaTarget = FindKardiaTargetById(context, context.KardiaTargetId);
        if (kardiaTarget == null)
            return false;

        var hpPercent = kardiaTarget.MaxHp > 0 ? (float)kardiaTarget.CurrentHp / kardiaTarget.MaxHp : 1f;
        if (hpPercent > config.SoteriaThreshold)
            return false;

        var action = SGEActions.Soteria;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Soteria";
            context.LogKardiaDecision(kardiaTarget.Name?.TextValue ?? "Unknown", "Soteria", $"HP {hpPercent:P0}");
            return true;
        }

        return false;
    }

    private bool TryPhilosophia(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhilosophia)
            return false;

        if (player.Level < SGEActions.Philosophia.MinLevel)
            return false;

        // Already have Philosophia active
        if (context.HasPhilosophia)
            return false;

        // Check cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Philosophia.ActionId))
            return false;

        // Use when party HP is low - provides party-wide Kardia healing
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.PhilosophiaThreshold)
            return false;

        var action = SGEActions.Philosophia;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Philosophia";
            return true;
        }

        return false;
    }

    private bool TrySwapKardia(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.KardiaSwapEnabled)
            return false;

        if (!context.HasKardiaPlaced)
            return false;

        if (!context.CanSwapKardia)
            return false;

        // Get current Kardia target HP
        var currentTarget = FindKardiaTargetById(context, context.KardiaTargetId);
        if (currentTarget == null)
            return false;

        var currentHpPercent = currentTarget.MaxHp > 0
            ? (float)currentTarget.CurrentHp / currentTarget.MaxHp
            : 1f;

        // Find a better target
        var (newTarget, newHpPercent) = FindBetterKardiaTarget(context, context.KardiaTargetId);
        if (newTarget == null)
            return false;

        // Check if we should swap using the KardiaManager logic
        if (!context.KardiaManager.ShouldSwapKardia(currentHpPercent, newHpPercent, config.KardiaSwapThreshold))
            return false;

        var action = SGEActions.Kardia;
        if (context.ActionService.ExecuteOgcd(action, newTarget.GameObjectId))
        {
            context.KardiaManager.RecordSwap(newTarget.GameObjectId);
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Kardia Swap";
            context.LogKardiaDecision(newTarget.Name?.TextValue ?? "Unknown", "Swap",
                $"From {currentHpPercent:P0} to {newHpPercent:P0}");
            return true;
        }

        return false;
    }

    private IBattleChara? FindKardiaTarget(IAsclepiusContext context)
    {
        var player = context.Player;

        // Priority 1: Tank in party
        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank != null)
            return tank;

        // Priority 2: Lowest HP party member (that isn't us)
        var lowestHp = context.PartyHelper.FindLowestHpPartyMember(player);
        if (lowestHp != null && lowestHp.GameObjectId != player.GameObjectId)
            return lowestHp;

        // Fallback: Self
        return player;
    }

    private IBattleChara? FindKardiaTargetById(IAsclepiusContext context, ulong targetId)
    {
        if (targetId == 0)
            return null;

        foreach (var member in context.PartyHelper.GetAllPartyMembers(context.Player))
        {
            if (member.GameObjectId == targetId)
                return member;
        }

        return null;
    }

    private (IBattleChara? target, float hpPercent) FindBetterKardiaTarget(
        IAsclepiusContext context,
        ulong currentTargetId)
    {
        IBattleChara? bestTarget = null;
        var lowestHp = 1f;

        foreach (var member in context.PartyHelper.GetAllPartyMembers(context.Player))
        {
            // Skip current target
            if (member.GameObjectId == currentTargetId)
                continue;

            // Skip self
            if (member.GameObjectId == context.Player.GameObjectId)
                continue;

            var hpPercent = member.MaxHp > 0 ? (float)member.CurrentHp / member.MaxHp : 1f;

            if (hpPercent < lowestHp)
            {
                lowestHp = hpPercent;
                bestTarget = member;
            }
        }

        return (bestTarget, lowestHp);
    }
}
