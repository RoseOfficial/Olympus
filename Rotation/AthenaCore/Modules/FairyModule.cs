using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Scholar;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Handles fairy management for Scholar.
/// Responsible for summoning, fairy abilities, and Seraph transformations.
/// </summary>
public sealed class FairyModule : IAthenaModule
{
    public int Priority => 3; // Very high priority - fairy is essential
    public string Name => "Fairy";

    public bool TryExecute(AthenaContext context, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        // Priority 1: Summon fairy if not present
        if (TrySummonFairy(context, isMoving))
            return true;

        // Priority 2: Seraphism (level 100 transformation)
        if (context.CanExecuteOgcd && TrySeraphism(context))
            return true;

        // Priority 3: Summon Seraph
        if (context.CanExecuteOgcd && TrySummonSeraph(context))
            return true;

        // Priority 4: Consolation (Seraph ability)
        if (context.CanExecuteOgcd && TryConsolation(context))
            return true;

        // Priority 5: Fey Union (sustained single-target healing)
        if (context.CanExecuteOgcd && TryFeyUnion(context))
            return true;

        // Priority 6: Fey Blessing (AoE heal)
        if (context.CanExecuteOgcd && TryFeyBlessing(context))
            return true;

        // Priority 7: Whispering Dawn (AoE HoT)
        if (context.CanExecuteOgcd && TryWhisperingDawn(context))
            return true;

        // Priority 8: Fey Illumination (heal buff)
        if (context.CanExecuteOgcd && TryFeyIllumination(context))
            return true;

        return false;
    }

    public void UpdateDebugState(AthenaContext context)
    {
        context.Debug.FairyState = context.FairyStateManager.CurrentState.ToString();
        context.Debug.FairyGauge = context.FairyGaugeService.CurrentGauge;
    }

    private bool TrySummonFairy(AthenaContext context, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.AutoSummonFairy)
            return false;

        if (!context.FairyStateManager.NeedsSummon)
            return false;

        // Don't summon during Dissipation
        if (context.FairyStateManager.IsDissipationActive)
            return false;

        if (player.Level < SCHActions.SummonEos.MinLevel)
            return false;

        // Can't summon while moving (has cast time)
        if (isMoving)
            return false;

        if (!context.ActionService.CanExecuteGcd)
            return false;

        var action = SCHActions.SummonEos;
        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Summoning Fairy";
            return true;
        }

        return false;
    }

    private bool TrySummonSeraph(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (config.SeraphStrategy == SeraphUsageStrategy.Manual)
            return false;

        if (player.Level < SCHActions.SummonSeraph.MinLevel)
            return false;

        if (!context.FairyStateManager.CanUseEosAbilities)
            return false;

        // Check cooldown
        if (!context.ActionService.IsActionReady(SCHActions.SummonSeraph.ActionId))
            return false;

        // SaveForDamage: Check if party HP is low enough
        if (config.SeraphStrategy == SeraphUsageStrategy.SaveForDamage)
        {
            var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            if (avgHp > config.SeraphPartyHpThreshold)
                return false;
        }

        var action = SCHActions.SummonSeraph;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Seraph";
            return true;
        }

        return false;
    }

    private bool TrySeraphism(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (config.SeraphismStrategy == SeraphismUsageStrategy.Manual)
            return false;

        if (player.Level < SCHActions.Seraphism.MinLevel)
            return false;

        // Check cooldown
        if (!context.ActionService.IsActionReady(SCHActions.Seraphism.ActionId))
            return false;

        // SaveForDamage: Check if party HP is low enough
        if (config.SeraphismStrategy == SeraphismUsageStrategy.SaveForDamage)
        {
            var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            if (avgHp > config.SeraphPartyHpThreshold)
                return false;
        }

        var action = SCHActions.Seraphism;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Seraphism";
            return true;
        }

        return false;
    }

    private bool TryConsolation(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableConsolation)
            return false;

        if (!context.FairyStateManager.CanUseSeraphAbilities)
            return false;

        if (player.Level < SCHActions.Consolation.MinLevel)
            return false;

        // Check cooldown and charges
        if (!context.ActionService.IsActionReady(SCHActions.Consolation.ActionId))
            return false;

        // Use when party needs healing
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.AoEHealThreshold && injuredCount < config.AoEHealMinTargets)
            return false;

        var action = SCHActions.Consolation;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Consolation";
            return true;
        }

        return false;
    }

    private bool TryFeyUnion(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities)
            return false;

        if (!context.FairyStateManager.CanUseEosAbilities)
            return false;

        if (player.Level < SCHActions.FeyUnion.MinLevel)
            return false;

        // Check gauge requirement
        if (context.FairyGaugeService.CurrentGauge < config.FeyUnionMinGauge)
            return false;

        // Don't start if already active
        if (context.StatusHelper.HasFeyUnionActive(player))
            return false;

        // Find target needing sustained healing
        var target = context.PartyHelper.FindFeyUnionTarget(player, config.FeyUnionThreshold);
        if (target == null)
            return false;

        var action = SCHActions.FeyUnion;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Fey Union";
            return true;
        }

        return false;
    }

    private bool TryFeyBlessing(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities)
            return false;

        if (!context.FairyStateManager.CanUseEosAbilities)
            return false;

        if (player.Level < SCHActions.FeyBlessing.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.FeyBlessing.ActionId))
            return false;

        // Check party health
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.FeyBlessingThreshold)
            return false;

        var action = SCHActions.FeyBlessing;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Fey Blessing";
            return true;
        }

        return false;
    }

    private bool TryWhisperingDawn(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities)
            return false;

        if (!context.FairyStateManager.IsFairyAvailable)
            return false;

        if (player.Level < SCHActions.WhisperingDawn.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.WhisperingDawn.ActionId))
            return false;

        // Check party health and injury count
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.WhisperingDawnThreshold)
            return false;
        if (injuredCount < config.WhisperingDawnMinTargets)
            return false;

        var action = SCHActions.WhisperingDawn;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Whispering Dawn";
            return true;
        }

        return false;
    }

    private bool TryFeyIllumination(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities)
            return false;

        if (!context.FairyStateManager.IsFairyAvailable)
            return false;

        if (player.Level < SCHActions.FeyIllumination.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.FeyIllumination.ActionId))
            return false;

        // Use proactively when party needs healing boost
        var (avgHp, lowestHp, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (lowestHp > 0.5f && avgHp > 0.8f)
            return false;

        var action = SCHActions.FeyIllumination;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Fey Illumination";
            return true;
        }

        return false;
    }
}
