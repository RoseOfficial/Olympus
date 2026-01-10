using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Modules;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific damage module.
/// Extends base damage logic with Oracle (Divination follow-up) and Lord of Crowns.
/// </summary>
public sealed class DamageModule : BaseDamageModule<AstraeaContext>, IAstraeaModule
{
    #region Base Class Overrides - Configuration Properties

    protected override bool IsDamageEnabled(AstraeaContext context) =>
        context.Configuration.Astrologian.EnableSingleTargetDamage;

    protected override bool IsDoTEnabled(AstraeaContext context) =>
        context.Configuration.Astrologian.EnableDot;

    protected override bool IsAoEDamageEnabled(AstraeaContext context) =>
        context.Configuration.Astrologian.EnableAoEDamage;

    protected override int AoEMinTargets(AstraeaContext context) =>
        context.Configuration.Astrologian.AoEDamageMinTargets;

    protected override float DoTRefreshThreshold(AstraeaContext context) =>
        context.Configuration.Astrologian.DotRefreshThreshold;

    #endregion

    #region Base Class Overrides - Action Methods

    protected override uint GetDoTStatusId(AstraeaContext context) =>
        ASTActions.GetDotStatusId(context.Player.Level);

    protected override ActionDefinition? GetDoTAction(AstraeaContext context) =>
        ASTActions.GetDotForLevel(context.Player.Level);

    protected override ActionDefinition? GetAoEDamageAction(AstraeaContext context) =>
        ASTActions.GetAoEDamageForLevel(context.Player.Level);

    protected override ActionDefinition GetSingleTargetAction(AstraeaContext context, bool isMoving) =>
        ASTActions.GetDamageGcdForLevel(context.Player.Level);

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetDpsState(AstraeaContext context, string state) =>
        context.Debug.DpsState = state;

    protected override void SetAoEDpsState(AstraeaContext context, string state) =>
        context.Debug.AoEDpsState = state;

    protected override void SetAoEDpsEnemyCount(AstraeaContext context, int count) =>
        context.Debug.AoEDpsEnemyCount = count;

    protected override void SetPlannedAction(AstraeaContext context, string action) =>
        context.Debug.PlannedAction = action;

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// AST can DoT while moving (Combust is instant).
    /// </summary>
    protected override bool CanDoT(AstraeaContext context, bool isMoving) => true;

    /// <summary>
    /// AST cannot single-target while moving (Malefic has cast time).
    /// Unless Lightspeed is active.
    /// </summary>
    protected override bool CanSingleTarget(AstraeaContext context, bool isMoving)
    {
        if (!isMoving)
            return true;

        // Can cast while moving with Lightspeed
        return context.HasLightspeed;
    }

    /// <summary>
    /// AST oGCD damage: Oracle (Divination follow-up) and Lord of Crowns.
    /// </summary>
    protected override bool TryOgcdDamage(AstraeaContext context)
    {
        // Priority 1: Oracle (when Divining proc is active)
        if (TryOracle(context))
            return true;

        // Priority 2: Lord of Crowns (if we have Lord card and want to spend it for damage)
        if (TryLordOfCrowns(context))
            return true;

        return false;
    }

    #endregion

    #region AST-Specific oGCD Methods

    private bool TryOracle(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        if (!config.EnableOracle)
            return false;

        if (player.Level < ASTActions.Oracle.MinLevel)
            return false;

        // Check for Divining buff (Oracle proc from Divination)
        if (!context.HasDivining)
            return false;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            ASTActions.Oracle.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(ASTActions.Oracle, target.GameObjectId))
        {
            SetPlannedAction(context, ASTActions.Oracle.Name);
            context.Debug.OracleState = "Used";
            SetDpsState(context, "Oracle");
            return true;
        }

        return false;
    }

    private bool TryLordOfCrowns(AstraeaContext context)
    {
        var config = context.Configuration.Astrologian;
        var player = context.Player;

        // Lord is used for damage when we have it and don't need Lady for healing
        if (!context.CardService.HasLord)
            return false;

        if (player.Level < ASTActions.LordOfCrowns.MinLevel)
            return false;

        // Only use Lord in damage module if Minor Arcana strategy is OnCooldown or SaveForBurst
        // Emergency mode means we're saving Lady for healing
        if (config.MinorArcanaStrategy == Config.MinorArcanaUsageStrategy.EmergencyOnly)
        {
            // In emergency mode, we use Lord when we have it, since we'd use Lady for heals
            // This is a bit aggressive, but if we have Lord it means Lady isn't available
        }

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            ASTActions.LordOfCrowns.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(ASTActions.LordOfCrowns, target.GameObjectId))
        {
            SetPlannedAction(context, ASTActions.LordOfCrowns.Name);
            SetDpsState(context, "Lord of Crowns");
            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(AstraeaContext context)
    {
        context.Debug.OracleState = context.HasDivining ? "Ready" : "Idle";
    }
}
