using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific damage module.
/// Extends base damage logic with Chain Stratagem, Energy Drain, Baneful Impaction, and Aetherflow management.
/// </summary>
public sealed class DamageModule : BaseDamageModule<AthenaContext>, IAthenaModule
{
    #region Base Class Overrides - Configuration Properties

    protected override bool IsDamageEnabled(AthenaContext context) =>
        context.Configuration.Scholar.EnableSingleTargetDamage;

    protected override bool IsDoTEnabled(AthenaContext context) =>
        context.Configuration.Scholar.EnableDot;

    protected override bool IsAoEDamageEnabled(AthenaContext context) =>
        context.Configuration.Scholar.EnableAoEDamage;

    protected override int AoEMinTargets(AthenaContext context) =>
        context.Configuration.Scholar.AoEDamageMinTargets;

    protected override float DoTRefreshThreshold(AthenaContext context) =>
        context.Configuration.Scholar.DotRefreshThreshold;

    #endregion

    #region Base Class Overrides - Action Methods

    protected override uint GetDoTStatusId(AthenaContext context) =>
        SCHActions.GetDotStatusId(context.Player.Level);

    protected override ActionDefinition? GetDoTAction(AthenaContext context) =>
        SCHActions.GetDotForLevel(context.Player.Level);

    protected override ActionDefinition? GetAoEDamageAction(AthenaContext context) =>
        SCHActions.GetAoEDamageForLevel(context.Player.Level);

    protected override ActionDefinition GetSingleTargetAction(AthenaContext context, bool isMoving) =>
        SCHActions.GetDamageGcdForLevel(context.Player.Level, isMoving);

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetDpsState(AthenaContext context, string state) =>
        context.Debug.DpsState = state;

    protected override void SetAoEDpsState(AthenaContext context, string state) =>
        context.Debug.AoEDpsState = state;

    protected override void SetAoEDpsEnemyCount(AthenaContext context, int count) =>
        context.Debug.AoEDpsEnemyCount = count;

    protected override void SetPlannedAction(AthenaContext context, string action) =>
        context.Debug.PlannedAction = action;

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// SCH oGCD damage: Chain Stratagem, Baneful Impaction, Energy Drain, Aetherflow.
    /// </summary>
    protected override bool TryOgcdDamage(AthenaContext context)
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

        return false;
    }

    /// <summary>
    /// SCH movement damage: Ruin II is instant cast.
    /// </summary>
    protected override bool TryMovementDamage(AthenaContext context)
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

        if (context.ActionService.ExecuteGcd(SCHActions.RuinII, target.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.RuinII.Name);
            SetDpsState(context, "Ruin II (moving)");
            return true;
        }

        return false;
    }

    #endregion

    #region SCH-Specific oGCD Methods

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

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.ChainStratagem.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(SCHActions.ChainStratagem, target.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.ChainStratagem.Name);
            SetDpsState(context, "Chain Stratagem");
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

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.BanefulImpaction.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(SCHActions.BanefulImpaction, target.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.BanefulImpaction.Name);
            SetDpsState(context, "Baneful Impaction");
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

        if (context.ActionService.ExecuteOgcd(SCHActions.EnergyDrain, target.GameObjectId))
        {
            context.AetherflowService.ConsumeStack();
            SetPlannedAction(context, SCHActions.EnergyDrain.Name);
            SetDpsState(context, "Energy Drain");
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

        if (context.ActionService.ExecuteOgcd(SCHActions.Aetherflow, player.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.Aetherflow.Name);
            SetDpsState(context, "Aetherflow");
            return true;
        }

        return false;
    }

    #endregion

    #region Aetherflow Strategy Helpers

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

    public override void UpdateDebugState(AthenaContext context)
    {
        context.Debug.AetherflowState = $"{context.AetherflowService.CurrentStacks}/3";
    }
}
