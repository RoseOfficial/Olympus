using Olympus.Data;
using Olympus.Rotation.AthenaCore.Context;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Handles buff and utility abilities for Scholar.
/// Includes Lucid Dreaming and Dissipation.
/// </summary>
public sealed class BuffModule : IAthenaModule
{
    public int Priority => 30; // After defensive, before damage
    public string Name => "Buff";

    public bool TryExecute(AthenaContext context, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!context.InCombat)
            return false;

        if (!context.CanExecuteOgcd)
            return false;

        // Priority 1: Lucid Dreaming (MP management)
        if (TryLucidDreaming(context))
            return true;

        // Priority 2: Dissipation (sacrifice fairy for Aetherflow)
        if (TryDissipation(context))
            return true;

        return false;
    }

    public void UpdateDebugState(AthenaContext context)
    {
        var player = context.Player;
        var mpPercent = player.MaxMp > 0 ? (float)player.CurrentMp / player.MaxMp : 1f;
        context.Debug.LucidState = mpPercent < context.Configuration.Scholar.LucidDreamingThreshold
            ? $"Low MP ({mpPercent:P0})"
            : $"OK ({mpPercent:P0})";
    }

    private bool TryLucidDreaming(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableLucidDreaming)
            return false;

        if (player.Level < SCHActions.LucidDreaming.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.LucidDreaming.ActionId))
            return false;

        // Check if we already have Lucid Dreaming active
        if (context.StatusHelper.HasLucidDreaming(player))
            return false;

        // Check MP threshold
        var mpPercent = player.MaxMp > 0 ? (float)player.CurrentMp / player.MaxMp : 1f;
        if (mpPercent > config.LucidDreamingThreshold)
            return false;

        var action = SCHActions.LucidDreaming;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.LucidState = "Lucid Dreaming";
            return true;
        }

        return false;
    }

    private bool TryDissipation(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableDissipation)
            return false;

        if (player.Level < SCHActions.Dissipation.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Dissipation.ActionId))
            return false;

        // Don't use if fairy is not present (need Eos to sacrifice)
        if (!context.FairyStateManager.IsFairyAvailable)
            return false;

        // Don't use if we already have stacks (would waste it)
        if (context.AetherflowService.CurrentStacks > 0)
            return false;

        // Check fairy gauge - don't waste high gauge
        if (context.FairyGaugeService.CurrentGauge > config.DissipationMaxFairyGauge)
            return false;

        // Check party health - only use when party is healthy
        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp < config.DissipationSafePartyHp)
            return false;

        var action = SCHActions.Dissipation;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Dissipation";
            return true;
        }

        return false;
    }
}
