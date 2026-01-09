using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Handles all DPS logic for the Scholar rotation.
/// Includes DoT maintenance, AoE damage, single-target damage, Energy Drain, and Chain Stratagem.
/// </summary>
public sealed class DamageModule : IAthenaModule
{
    public int Priority => 50; // Low priority - DPS after healing
    public string Name => "Damage";

    public bool TryExecute(AthenaContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!context.InCombat)
        {
            context.Debug.DpsState = "Not in combat";
            return false;
        }

        // oGCD damage abilities first
        if (context.CanExecuteOgcd)
        {
            // Priority 1: Chain Stratagem (raid buff)
            if (TryChainStratagem(context))
                return true;

            // Priority 2: Baneful Impaction (when Impact Imminent is active)
            if (TryBanefulImpaction(context))
                return true;

            // Priority 3: Energy Drain (dump Aetherflow for damage)
            if (TryEnergyDrain(context))
                return true;

            // Priority 4: Aetherflow (get stacks)
            if (TryAetherflow(context))
                return true;
        }

        // GCD damage
        if (context.CanExecuteGcd)
        {
            // Priority 5: DoT maintenance
            if (!isMoving && TryDoT(context))
                return true;

            // Priority 6: AoE damage (Art of War)
            if (TryAoEDamage(context, isMoving))
                return true;

            // Priority 7: Single-target damage (Broil family)
            if (!isMoving && TrySingleTargetDamage(context))
                return true;

            // Priority 8: Ruin II for movement (instant cast)
            if (isMoving && TryRuinII(context))
                return true;
        }

        return false;
    }

    public void UpdateDebugState(AthenaContext context)
    {
        context.Debug.AetherflowState = $"{context.AetherflowService.CurrentStacks}/3";
    }

    #region oGCD Damage

    private bool TryChainStratagem(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableChainStratagem)
            return false;

        if (player.Level < SCHActions.ChainStratagem.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.ChainStratagem.ActionId))
            return false;

        // Find a boss or enemy to apply Chain Stratagem
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.ChainStratagem.Range,
            player);

        if (target == null)
            return false;

        var action = SCHActions.ChainStratagem;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DpsState = "Chain Stratagem";
            return true;
        }

        return false;
    }

    private bool TryBanefulImpaction(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableBanefulImpaction)
            return false;

        if (player.Level < SCHActions.BanefulImpaction.MinLevel)
            return false;

        // Check for Impact Imminent buff
        if (!context.StatusHelper.HasImpactImminent(player))
            return false;

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.BanefulImpaction.Range,
            player);

        if (target == null)
            return false;

        var action = SCHActions.BanefulImpaction;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DpsState = "Baneful Impaction";
            return true;
        }

        return false;
    }

    private bool TryEnergyDrain(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableEnergyDrain)
            return false;

        if (player.Level < SCHActions.EnergyDrain.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.EnergyDrain.ActionId))
            return false;

        var stacks = context.AetherflowService.CurrentStacks;
        if (stacks == 0)
            return false;

        // Decision based on Aetherflow strategy
        bool shouldDrain = config.AetherflowStrategy switch
        {
            AetherflowUsageStrategy.AggressiveDps => stacks > 0,
            AetherflowUsageStrategy.Balanced => ShouldDrainBalanced(context),
            AetherflowUsageStrategy.HealingPriority => ShouldDrainConservative(context),
            _ => false
        };

        if (!shouldDrain)
            return false;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.EnergyDrain.Range,
            player);

        if (target == null)
            return false;

        var action = SCHActions.EnergyDrain;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.AetherflowService.ConsumeStack();
            context.Debug.PlannedAction = action.Name;
            context.Debug.DpsState = "Energy Drain";
            return true;
        }

        return false;
    }

    private bool TryAetherflow(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableAetherflow)
            return false;

        if (player.Level < SCHActions.Aetherflow.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Aetherflow.ActionId))
            return false;

        // Only use when we have 0 stacks
        if (context.AetherflowService.CurrentStacks > 0)
            return false;

        var action = SCHActions.Aetherflow;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DpsState = "Aetherflow";
            return true;
        }

        return false;
    }

    #endregion

    #region GCD Damage

    private bool TryDoT(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableDot)
            return false;

        // Get the appropriate DoT for level
        var dotAction = SCHActions.GetDotForLevel(player.Level);
        if (dotAction == null)
            return false;

        var dotStatusId = SCHActions.GetDotStatusId(player.Level);
        if (dotStatusId == 0)
            return false;

        // Find enemy needing DoT
        var target = context.TargetingService.FindEnemyNeedingDot(
            dotStatusId,
            config.DotRefreshThreshold,
            dotAction.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteGcd(dotAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = dotAction.Name;
            context.Debug.DpsState = "DoT";
            return true;
        }

        return false;
    }

    private bool TryAoEDamage(AthenaContext context, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableAoEDamage)
            return false;

        if (player.Level < SCHActions.ArtOfWar.MinLevel)
            return false;

        // Count enemies in range
        var enemyCount = context.TargetingService.CountEnemiesInRange(SCHActions.ArtOfWar.Radius, player);
        context.Debug.AoEDpsEnemyCount = enemyCount;

        if (enemyCount < config.AoEDamageMinTargets)
        {
            context.Debug.AoEDpsState = $"{enemyCount} < {config.AoEDamageMinTargets} min";
            return false;
        }

        var action = SCHActions.GetAoEDamageForLevel(player.Level);
        if (action == null)
            return false;

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DpsState = $"AoE ({enemyCount} targets)";
            context.Debug.AoEDpsState = $"{enemyCount} enemies";
            return true;
        }

        return false;
    }

    private bool TrySingleTargetDamage(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableSingleTargetDamage)
            return false;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.Ruin.Range,
            player);

        if (target == null)
        {
            context.Debug.DpsState = "No enemy";
            return false;
        }

        var action = SCHActions.GetDamageGcdForLevel(player.Level, isMoving: false);
        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DpsState = action.Name;
            return true;
        }

        return false;
    }

    private bool TryRuinII(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableRuinII)
            return false;

        if (player.Level < SCHActions.RuinII.MinLevel)
            return false;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.RuinII.Range,
            player);

        if (target == null)
            return false;

        var action = SCHActions.RuinII;
        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DpsState = "Ruin II (moving)";
            return true;
        }

        return false;
    }

    #endregion

    #region Helper Methods

    private bool ShouldDrainBalanced(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var stacks = context.AetherflowService.CurrentStacks;

        // Check if Aetherflow is coming off cooldown soon
        var aetherflowCd = context.AetherflowService.GetCooldownRemaining();
        if (aetherflowCd <= config.AetherflowDumpWindow && stacks > 0)
            return true;

        // Keep reserve for healing
        if (stacks <= config.AetherflowReserve)
            return false;

        // Party is healthy, safe to drain
        var (avgHp, lowestHp, _) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
        return avgHp > 0.8f && lowestHp > 0.5f;
    }

    private bool ShouldDrainConservative(AthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var stacks = context.AetherflowService.CurrentStacks;

        // Only drain to prevent overcap
        var aetherflowCd = context.AetherflowService.GetCooldownRemaining();
        if (aetherflowCd <= config.AetherflowDumpWindow && stacks == 3)
            return true;

        return false;
    }

    #endregion
}
