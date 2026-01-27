using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// SGE-specific damage module.
/// Handles Dosis, Eukrasian Dosis (DoT), Phlegma, Toxikon, Dyskrasia, and Psyche.
/// Extends base damage logic with SGE-unique mechanics: Eukrasia-based DoT, charge-based Phlegma,
/// Addersting-based Toxikon for movement.
/// </summary>
public sealed class DamageModule : BaseDamageModule<IAsclepiusContext>, IAsclepiusModule
{
    #region Base Class Overrides - Configuration Properties

    protected override bool IsDamageEnabled(IAsclepiusContext context) =>
        context.Configuration.EnableDamage;

    protected override bool IsDoTEnabled(IAsclepiusContext context) =>
        context.Configuration.EnableDoT;

    protected override bool IsAoEDamageEnabled(IAsclepiusContext context) =>
        context.Configuration.Sage.EnableAoEDamage;

    protected override int AoEMinTargets(IAsclepiusContext context) =>
        context.Configuration.Sage.AoEDamageMinTargets;

    protected override float DoTRefreshThreshold(IAsclepiusContext context) =>
        FFXIVConstants.DotRefreshThreshold;

    #endregion

    #region Base Class Overrides - Action Methods

    protected override uint GetDoTStatusId(IAsclepiusContext context) =>
        SGEActions.GetDotStatusId(context.Player.Level);

    protected override ActionDefinition? GetDoTAction(IAsclepiusContext context) =>
        SGEActions.GetDotForLevel(context.Player.Level);

    protected override ActionDefinition? GetAoEDamageAction(IAsclepiusContext context) =>
        SGEActions.GetAoEDamageGcdForLevel(context.Player.Level);

    protected override ActionDefinition GetSingleTargetAction(IAsclepiusContext context, bool isMoving) =>
        SGEActions.GetDamageGcdForLevel(context.Player.Level);

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetDpsState(IAsclepiusContext context, string state) =>
        context.Debug.DpsState = state;

    protected override void SetAoEDpsState(IAsclepiusContext context, string state) =>
        context.Debug.AoEDpsState = state;

    protected override void SetAoEDpsEnemyCount(IAsclepiusContext context, int count) =>
        context.Debug.AoEDpsEnemyCount = count;

    protected override void SetPlannedAction(IAsclepiusContext context, string action) =>
        context.Debug.PlannedAction = action;

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// SGE DPS doesn't block other modules (healers continue to check other priorities).
    /// </summary>
    protected override bool BlocksOnExecution => false;

    /// <summary>
    /// SGE oGCD damage: Psyche (AoE damage ability).
    /// </summary>
    protected override bool TryOgcdDamage(IAsclepiusContext context)
    {
        return TryPsyche(context);
    }

    /// <summary>
    /// SGE special GCD damage: Phlegma (charge-based) and Toxikon when moving.
    /// </summary>
    protected override bool TrySpecialDamage(IAsclepiusContext context, bool isMoving)
    {
        // Priority 1: Phlegma (high potency, instant, charges)
        if (TryPhlegma(context))
            return true;

        // Priority 2: Toxikon while moving (consumes Addersting)
        if (isMoving && TryToxikon(context))
            return true;

        return false;
    }

    /// <summary>
    /// SGE DoT requires activating Eukrasia first (oGCD), then applying Eukrasian Dosis (GCD).
    /// Override to handle this unique two-step process.
    /// </summary>
    protected override bool TryDoT(IAsclepiusContext context)
    {
        if (!IsDoTEnabled(context))
            return false;

        var player = context.Player;
        if (player.Level < SGEActions.EukrasianDosis.MinLevel)
            return false;

        var dotAction = GetDoTAction(context);
        if (dotAction == null)
            return false;

        var dotStatusId = GetDoTStatusId(context);

        // If we have Eukrasia, apply the DoT
        if (context.HasEukrasia)
        {
            var enemy = context.TargetingService.FindEnemy(
                context.Configuration.Targeting.EnemyStrategy,
                dotAction.Range,
                player);

            if (enemy == null)
                return false;

            if (context.ActionService.ExecuteGcd(dotAction, enemy.GameObjectId))
            {
                SetPlannedAction(context, dotAction.Name);
                SetDpsState(context, "DoT Applied");
                return true;
            }

            return false;
        }

        // Check if we need to apply/refresh DoT
        var target = context.TargetingService.FindEnemyNeedingDot(
            dotStatusId,
            DoTRefreshThreshold(context),
            dotAction.Range,
            player);

        if (target == null)
        {
            context.Debug.DoTState = "Active";
            return false;
        }

        // Activate Eukrasia for DoT (this is an oGCD)
        if (context.CanExecuteOgcd)
        {
            var eukrasiaAction = SGEActions.Eukrasia;
            if (context.ActionService.ExecuteOgcd(eukrasiaAction, player.GameObjectId))
            {
                SetPlannedAction(context, eukrasiaAction.Name);
                SetDpsState(context, "Eukrasia for DoT");
                context.Debug.EukrasiaState = "Activating";
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// SGE cannot cast Dosis while moving (has cast time).
    /// Use Toxikon instead for movement damage.
    /// </summary>
    protected override bool CanSingleTarget(IAsclepiusContext context, bool isMoving) => !isMoving;

    /// <summary>
    /// SGE movement damage: Toxikon (instant cast, uses Addersting).
    /// </summary>
    protected override bool TryMovementDamage(IAsclepiusContext context)
    {
        return TryToxikon(context);
    }

    #endregion

    #region SGE-Specific Methods

    private bool TryPsyche(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePsyche)
            return false;

        if (player.Level < SGEActions.Psyche.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Psyche.ActionId))
        {
            context.Debug.PsycheState = "On CD";
            return false;
        }

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SGEActions.Psyche.Range,
            player);

        if (enemy == null)
        {
            context.Debug.PsycheState = "No target";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(SGEActions.Psyche, enemy.GameObjectId))
        {
            SetPlannedAction(context, SGEActions.Psyche.Name);
            SetDpsState(context, "Psyche");
            context.Debug.PsycheState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryPhlegma(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhlegma)
            return false;

        var phlegmaAction = SGEActions.GetPhlegmaForLevel(player.Level);
        if (phlegmaAction == null)
        {
            context.Debug.PhlegmaState = "Level too low";
            return false;
        }

        // Check charges
        var charges = context.ActionService.GetCurrentCharges(phlegmaAction.ActionId);
        if (charges < 1)
        {
            context.Debug.PhlegmaState = "No charges";
            return false;
        }

        // Use if we'd overcap charges
        var maxCharges = 2;
        var rechargingTime = context.ActionService.GetCooldownRemaining(phlegmaAction.ActionId);
        var shouldUse = charges >= maxCharges || (charges == maxCharges - 1 && rechargingTime < 5f);

        if (!shouldUse && charges < maxCharges)
        {
            context.Debug.PhlegmaState = $"Saving ({charges}/{maxCharges})";
            return false;
        }

        // Find enemy in range (Phlegma is close range)
        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            phlegmaAction.Range,
            player);

        if (enemy == null)
        {
            context.Debug.PhlegmaState = "Out of range";
            return false;
        }

        if (context.ActionService.ExecuteGcd(phlegmaAction, enemy.GameObjectId))
        {
            SetPlannedAction(context, phlegmaAction.Name);
            SetDpsState(context, phlegmaAction.Name);
            context.Debug.PhlegmaState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryToxikon(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableToxikon)
            return false;

        var toxikonAction = SGEActions.GetToxikonForLevel(player.Level);
        if (toxikonAction == null)
        {
            context.Debug.ToxikonState = "Level too low";
            return false;
        }

        // Requires Addersting
        if (context.AdderstingStacks < 1)
        {
            context.Debug.ToxikonState = "No Addersting";
            return false;
        }

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            toxikonAction.Range,
            player);

        if (enemy == null)
        {
            context.Debug.ToxikonState = "No target";
            return false;
        }

        if (context.ActionService.ExecuteGcd(toxikonAction, enemy.GameObjectId))
        {
            SetPlannedAction(context, toxikonAction.Name);
            SetDpsState(context, toxikonAction.Name);
            context.Debug.ToxikonState = "Executing";
            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(IAsclepiusContext context)
    {
        var player = context.Player;

        // Update DoT state
        var dotAction = SGEActions.GetDotForLevel(player.Level);
        var dotStatusId = SGEActions.GetDotStatusId(player.Level);

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            dotAction.Range,
            player);

        if (enemy != null)
        {
            var dotRemaining = GetStatusRemainingTime(enemy, dotStatusId, player.GameObjectId);
            context.Debug.DoTRemaining = dotRemaining;
            context.Debug.DoTState = dotRemaining > 0 ? $"{dotRemaining:F1}s" : "Not applied";
        }
        else
        {
            context.Debug.DoTState = "No target";
        }

        // Phlegma charges
        var phlegmaAction = SGEActions.GetPhlegmaForLevel(player.Level);
        if (phlegmaAction != null)
        {
            var charges = (int)context.ActionService.GetCurrentCharges(phlegmaAction.ActionId);
            context.Debug.PhlegmaCharges = charges;
            context.Debug.PhlegmaState = charges > 0 ? $"{charges} charges" : "No charges";
        }

        // Addersting for Toxikon
        context.Debug.AdderstingStacks = context.AdderstingStacks;
        context.Debug.ToxikonState = context.AdderstingStacks > 0 ? $"{context.AdderstingStacks} stacks" : "No Addersting";
    }

    #region Helpers

    private float GetStatusRemainingTime(IBattleChara target, uint statusId, ulong sourceId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId && status.SourceId == (uint)sourceId)
            {
                return status.RemainingTime;
            }
        }

        return 0f;
    }

    #endregion
}
