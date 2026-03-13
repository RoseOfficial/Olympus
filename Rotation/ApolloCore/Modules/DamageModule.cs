using System;
using System.Collections.Generic;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// WHM-specific damage module.
/// Extends base damage logic with Sacred Sight, Blood Lily (Afflatus Misery), and Lily gauge handling.
/// </summary>
public sealed class DamageModule : BaseDamageModule<ApolloContext>, IApolloModule
{
    // Action enable lookup maps
    private static readonly Dictionary<uint, Func<Configuration, bool>> DamageSpellEnabledMap = new()
    {
        { WHMActions.Stone.ActionId, c => c.EnableDamage && c.Damage.EnableStone },
        { WHMActions.StoneII.ActionId, c => c.EnableDamage && c.Damage.EnableStoneII },
        { WHMActions.StoneIII.ActionId, c => c.EnableDamage && c.Damage.EnableStoneIII },
        { WHMActions.StoneIV.ActionId, c => c.EnableDamage && c.Damage.EnableStoneIV },
        { WHMActions.Glare.ActionId, c => c.EnableDamage && c.Damage.EnableGlare },
        { WHMActions.GlareIII.ActionId, c => c.EnableDamage && c.Damage.EnableGlareIII },
        { WHMActions.GlareIV.ActionId, c => c.EnableDamage && c.Damage.EnableGlareIV },
        { WHMActions.AfflatusMisery.ActionId, c => c.EnableDamage && c.Damage.EnableAfflatusMisery },
    };

    private static readonly Dictionary<uint, Func<Configuration, bool>> DotSpellEnabledMap = new()
    {
        { WHMActions.Aero.ActionId, c => c.EnableDoT && c.Dot.EnableAero },
        { WHMActions.AeroII.ActionId, c => c.EnableDoT && c.Dot.EnableAeroII },
        { WHMActions.Dia.ActionId, c => c.EnableDoT && c.Dot.EnableDia },
    };

    private static readonly Dictionary<uint, Func<Configuration, bool>> AoEDamageSpellEnabledMap = new()
    {
        { WHMActions.Holy.ActionId, c => c.EnableDamage && c.Damage.EnableHoly },
        { WHMActions.HolyIII.ActionId, c => c.EnableDamage && c.Damage.EnableHolyIII },
    };

    #region Base Class Overrides - Configuration Properties

    protected override bool IsDamageEnabled(ApolloContext context) =>
        context.Configuration.EnableDamage;

    protected override bool IsDoTEnabled(ApolloContext context) =>
        context.Configuration.EnableDoT;

    protected override bool IsAoEDamageEnabled(ApolloContext context) =>
        context.Configuration.EnableDamage;

    protected override int AoEMinTargets(ApolloContext context) =>
        context.Configuration.Damage.AoEDamageMinTargets;

    protected override float DoTRefreshThreshold(ApolloContext context) =>
        FFXIVConstants.DotRefreshThreshold;

    #endregion

    #region Base Class Overrides - Action Methods

    protected override uint GetDoTStatusId(ApolloContext context)
    {
        // CNJ doesn't have Dia (WHM-only at 72+); cap at AeroII/Aero
        if (context.Player.ClassJob.RowId == JobRegistry.Conjurer)
            return context.Player.Level >= 46 ? StatusHelper.StatusIds.AeroII : StatusHelper.StatusIds.Aero;
        return StatusHelper.GetDotStatusId(context.Player.Level);
    }

    protected override ActionDefinition? GetDoTAction(ApolloContext context)
    {
        // CNJ doesn't have Dia (WHM-only at 72+); cap at AeroII/Aero
        if (context.Player.ClassJob.RowId == JobRegistry.Conjurer)
            return context.Player.Level >= WHMActions.AeroII.MinLevel ? WHMActions.AeroII :
                   context.Player.Level >= WHMActions.Aero.MinLevel ? WHMActions.Aero : null;
        return WHMActions.GetDotForLevel(context.Player.Level);
    }

    protected override ActionDefinition? GetAoEDamageAction(ApolloContext context) =>
        WHMActions.GetAoEDamageGcdForLevel(context.Player.Level);

    protected override ActionDefinition GetSingleTargetAction(ApolloContext context, bool isMoving)
    {
        // CNJ doesn't have Stone III+ or Glare+ (WHM-only); cap at StoneII/Stone
        if (context.Player.ClassJob.RowId == JobRegistry.Conjurer)
            return context.Player.Level >= WHMActions.StoneII.MinLevel ? WHMActions.StoneII : WHMActions.Stone;
        return WHMActions.GetDamageGcdForLevel(context.Player.Level);
    }

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetDpsState(ApolloContext context, string state) =>
        context.Debug.DpsState = state;

    protected override void SetAoEDpsState(ApolloContext context, string state) =>
        context.Debug.AoEDpsState = state;

    protected override void SetAoEDpsEnemyCount(ApolloContext context, int count) =>
        context.Debug.AoEDpsEnemyCount = count;

    protected override void SetPlannedAction(ApolloContext context, string action) =>
        context.Debug.PlannedAction = action;

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// WHM DPS doesn't block other actions.
    /// </summary>
    protected override bool BlocksOnExecution => false;

    /// <summary>
    /// All WHM/CNJ DoTs (Aero, Aero II, Dia) are instant cast, so DoT is always allowed while moving.
    /// </summary>
    protected override bool CanDoT(ApolloContext context, bool isMoving) => true;

    /// <summary>
    /// Check if action is enabled in WHM config.
    /// </summary>
    protected override bool IsActionEnabled(ApolloContext context, ActionDefinition action)
    {
        var config = context.Configuration;

        if (DamageSpellEnabledMap.TryGetValue(action.ActionId, out var damageCheck))
            return damageCheck(config);

        if (DotSpellEnabledMap.TryGetValue(action.ActionId, out var dotCheck))
            return dotCheck(config);

        if (AoEDamageSpellEnabledMap.TryGetValue(action.ActionId, out var aoeCheck))
            return aoeCheck(config);

        return true;
    }

    /// <summary>
    /// WHM special damage: Afflatus Misery (Blood Lily) and Sacred Sight (Glare IV).
    /// These have priority over regular damage rotation.
    /// </summary>
    protected override bool TrySpecialDamage(ApolloContext context, bool isMoving)
    {
        // Priority 1: Afflatus Misery (1240p AoE, costs 3 Blood Lily)
        if (TryAfflatusMisery(context))
            return true;

        // Priority 2: Sacred Sight Glare IV (instant, uses stacks)
        if (TrySacredSightGlare(context))
            return true;

        return false;
    }

    /// <summary>
    /// For AoE, skip Holy when Sacred Sight is available (Glare IV is better).
    /// </summary>
    protected override bool TryAoEDamage(ApolloContext context)
    {
        // Skip Holy if we have Sacred Sight stacks (Glare IV is better)
        if (context.SacredSightStacks > 0)
            return false;

        return base.TryAoEDamage(context);
    }

    #endregion

    #region WHM-Specific Methods

    private bool TryAfflatusMisery(ApolloContext context)
    {
        var player = context.Player;
        var config = context.Configuration;

        if (context.BloodLilyCount < 3)
        {
            context.Debug.MiseryState = $"{context.BloodLilyCount}/3 Blood Lily";
            return false;
        }

        if (player.Level < WHMActions.AfflatusMisery.MinLevel)
        {
            context.Debug.MiseryState = $"Level {player.Level} < 74";
            return false;
        }

        if (!IsActionEnabled(context, WHMActions.AfflatusMisery))
        {
            context.Debug.MiseryState = "Disabled";
            return false;
        }

        var target = context.TargetingService.FindEnemy(
            config.Targeting.EnemyStrategy,
            WHMActions.AfflatusMisery.Range,
            player);

        if (target == null)
        {
            context.Debug.MiseryState = "No target";
            return false;
        }

        if (context.ActionService.ExecuteGcd(WHMActions.AfflatusMisery, target.GameObjectId))
        {
            context.Debug.DpsState = "Afflatus Misery";
            context.Debug.MiseryState = "Executing";
            SetPlannedAction(context, WHMActions.AfflatusMisery.Name);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var shortReason = $"Afflatus Misery on {targetName} - 1240p AoE!";

                var factors = new[]
                {
                    "Blood Lilies: 3/3 (Misery ready!)",
                    "1240 potency AoE damage",
                    "Instant cast",
                    "Built from 3 Lily heals",
                    "One of WHM's strongest damage skills",
                };

                var alternatives = new[]
                {
                    "Nothing - always use Misery when ready",
                    "Save for add spawn (if imminent)",
                };

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = WHMActions.AfflatusMisery.ActionId,
                    ActionName = "Afflatus Misery",
                    Category = "Damage",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Afflatus Misery is WHM's strongest GCD damage skill at 1240 potency. It requires 3 Blood Lilies built from using Afflatus Solace/Rapture. Used on {targetName}. Always use Misery when available - it's your reward for using Lily heals!",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Never hold Misery too long - it's a huge DPS gain! Build Blood Lilies with Lily heals to unlock it.",
                    ConceptId = WhmConcepts.AfflatusMiseryTiming,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TrySacredSightGlare(ApolloContext context)
    {
        var player = context.Player;
        var config = context.Configuration;

        if (context.SacredSightStacks == 0)
            return false;

        if (player.Level < WHMActions.GlareIV.MinLevel)
            return false;

        if (!IsActionEnabled(context, WHMActions.GlareIV))
            return false;

        var (aoeTarget, hitCount) = context.TargetingService.FindBestAoETarget(
            WHMActions.GlareIV.Radius,
            WHMActions.GlareIV.Range,
            player);

        if (aoeTarget == null)
            return false;

        if (context.ActionService.ExecuteGcd(WHMActions.GlareIV, aoeTarget.GameObjectId))
        {
            if (hitCount >= config.Damage.AoEDamageMinTargets)
            {
                context.Debug.DpsState = $"Glare IV AoE ({hitCount} targets, {context.SacredSightStacks} stacks)";
            }
            else
            {
                context.Debug.DpsState = $"Sacred Sight Glare IV ({context.SacredSightStacks} stacks)";
            }
            SetPlannedAction(context, WHMActions.GlareIV.Name);
            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(ApolloContext context)
    {
        context.Debug.LilyCount = context.LilyCount;
        context.Debug.BloodLilyCount = context.BloodLilyCount;
        context.Debug.LilyStrategy = context.Configuration.Healing.LilyStrategy.ToString();
        context.Debug.SacredSightStacks = context.SacredSightStacks;
    }
}
