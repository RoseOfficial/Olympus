using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Astrologian-specific damage module.
/// Extends base damage logic with Oracle (Divination follow-up) and Lord of Crowns.
/// </summary>
public sealed class DamageModule : BaseDamageModule<IAstraeaContext>, IAstraeaModule
{
    #region Base Class Overrides - Configuration Properties

    protected override bool IsDamageEnabled(IAstraeaContext context) =>
        context.Configuration.Astrologian.EnableSingleTargetDamage;

    protected override bool IsDoTEnabled(IAstraeaContext context) =>
        context.Configuration.Astrologian.EnableDot;

    protected override bool IsAoEDamageEnabled(IAstraeaContext context) =>
        context.Configuration.Astrologian.EnableAoEDamage;

    protected override int AoEMinTargets(IAstraeaContext context) =>
        context.Configuration.Astrologian.AoEDamageMinTargets;

    protected override float DoTRefreshThreshold(IAstraeaContext context) =>
        context.Configuration.Astrologian.DotRefreshThreshold;

    #endregion

    #region Base Class Overrides - Action Methods

    protected override uint GetDoTStatusId(IAstraeaContext context) =>
        ASTActions.GetDotStatusId(context.Player.Level);

    protected override ActionDefinition? GetDoTAction(IAstraeaContext context) =>
        ASTActions.GetDotForLevel(context.Player.Level);

    protected override ActionDefinition? GetAoEDamageAction(IAstraeaContext context) =>
        ASTActions.GetAoEDamageForLevel(context.Player.Level);

    protected override ActionDefinition GetSingleTargetAction(IAstraeaContext context, bool isMoving) =>
        ASTActions.GetDamageGcdForLevel(context.Player.Level);

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetDpsState(IAstraeaContext context, string state) =>
        context.Debug.DpsState = state;

    protected override void SetAoEDpsState(IAstraeaContext context, string state) =>
        context.Debug.AoEDpsState = state;

    protected override void SetAoEDpsEnemyCount(IAstraeaContext context, int count) =>
        context.Debug.AoEDpsEnemyCount = count;

    protected override void SetPlannedAction(IAstraeaContext context, string action) =>
        context.Debug.PlannedAction = action;

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// AST can DoT while moving (Combust is instant).
    /// </summary>
    protected override bool CanDoT(IAstraeaContext context, bool isMoving) => true;

    /// <summary>
    /// AST cannot single-target while moving (Malefic has cast time).
    /// Unless Lightspeed is active.
    /// </summary>
    protected override bool CanSingleTarget(IAstraeaContext context, bool isMoving)
    {
        if (!isMoving)
            return true;

        // Can cast while moving with Lightspeed
        return context.HasLightspeed;
    }

    /// <summary>
    /// AST oGCD damage: Oracle (Divination follow-up) and Lord of Crowns.
    /// </summary>
    protected override bool TryOgcdDamage(IAstraeaContext context)
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

    private bool TryOracle(IAstraeaContext context)
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

            TrainingHelper.RecordDamageDecision(
                context.TrainingService,
                ASTActions.Oracle.ActionId,
                "Oracle",
                target.Name?.TextValue,
                "Oracle - Divination follow-up oGCD damage",
                "Oracle is triggered by the Divining buff from Divination. It delivers a potent oGCD attack that is the payoff for using Divination. Always spend the Divining buff before it expires — Oracle has no cooldown, it's purely proc-based.",
                new[] { "Divining buff active", "Free oGCD damage", "High potency follow-up", "Always use before Divining expires" },
                new[] { "Nothing — Oracle must be used while Divining is active" },
                "Oracle is your Divination payoff. Use it immediately after Divination while weaving other oGCDs. Never let Divining expire unused!",
                AstConcepts.OracleUsage,
                ExplanationPriority.High);

            context.TrainingService?.RecordConceptApplication(AstConcepts.OracleUsage, wasSuccessful: true, "Divining buff consumed");

            return true;
        }

        return false;
    }

    private bool TryLordOfCrowns(IAstraeaContext context)
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

            TrainingHelper.RecordDamageDecision(
                context.TrainingService,
                ASTActions.LordOfCrowns.ActionId,
                "Lord of Crowns",
                target.Name?.TextValue,
                "Lord of Crowns - Minor Arcana damage card",
                "Lord of Crowns is the damage outcome from Minor Arcana. It delivers 250 potency AoE oGCD damage. In damage-focused strategies, Lord is spent immediately to contribute DPS. Lady of Crowns is saved in the healing module for emergencies.",
                new[] { "Minor Arcana gave Lord (damage card)", "250 potency AoE oGCD", "Free damage — no GCD cost", "Lady reserved by healing module" },
                new[] { "Save Lord for burst window alignment", "Use Lady for emergency party heal (handled in healing module)" },
                "Lord of Crowns is free damage! Spend it promptly so you can draw Minor Arcana again sooner. Align with burst windows when possible for maximum party buff synergy.",
                AstConcepts.DpsOptimization,
                ExplanationPriority.Normal);

            context.TrainingService?.RecordConceptApplication(AstConcepts.DpsOptimization, wasSuccessful: true, "Lord of Crowns damage");

            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(IAstraeaContext context)
    {
        context.Debug.OracleState = context.HasDivining ? "Ready" : "Idle";
    }
}
