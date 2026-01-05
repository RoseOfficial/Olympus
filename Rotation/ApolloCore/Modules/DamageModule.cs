using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Handles all DPS logic for the WHM rotation.
/// Includes DoT maintenance, AoE damage, single-target damage, and Afflatus Misery.
/// </summary>
public sealed class DamageModule : IApolloModule
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

    // Movement tracking
    private bool _hadTargetLastFrame;

    public int Priority => 50; // Low priority - DPS after healing
    public string Name => "Damage";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DpsState = "Not in combat";
            return false;
        }

        ExecuteDps(context, isMoving);
        context.Debug.PlanningState = "DPS";
        return false; // DPS doesn't block other actions
    }

    public void UpdateDebugState(ApolloContext context)
    {
        context.Debug.LilyCount = context.LilyCount;
        context.Debug.BloodLilyCount = context.BloodLilyCount;
        context.Debug.LilyStrategy = context.Configuration.Healing.LilyStrategy.ToString();
        context.Debug.SacredSightStacks = context.SacredSightStacks;
    }

    private void ExecuteDps(ApolloContext context, bool isMoving)
    {
        var player = context.Player;
        var config = context.Configuration;
        var dotStatusId = StatusHelper.GetDotStatusId(player.Level);

        IBattleNpc? target = null;
        ActionDefinition? actionDef = null;

        // Use cached values from context
        context.Debug.LilyCount = context.LilyCount;
        context.Debug.BloodLilyCount = context.BloodLilyCount;
        context.Debug.LilyStrategy = config.Healing.LilyStrategy.ToString();
        context.Debug.SacredSightStacks = context.SacredSightStacks;

        // Priority 0: Afflatus Misery (1240p AoE, costs 3 Blood Lily)
        if (context.BloodLilyCount >= 3 && player.Level >= WHMActions.AfflatusMisery.MinLevel)
        {
            if (IsDamageSpellEnabled(WHMActions.AfflatusMisery.ActionId, config))
            {
                target = context.TargetingService.FindEnemy(config.Targeting.EnemyStrategy, WHMActions.AfflatusMisery.Range, player);
                if (target is not null)
                {
                    actionDef = WHMActions.AfflatusMisery;
                    context.Debug.DpsState = "Afflatus Misery";
                    context.Debug.MiseryState = "Executing";
                }
                else
                {
                    context.Debug.MiseryState = "No target";
                }
            }
            else
            {
                context.Debug.MiseryState = "Disabled";
            }
        }
        else
        {
            context.Debug.MiseryState = context.BloodLilyCount < 3 ? $"{context.BloodLilyCount}/3 Blood Lily" : $"Level {player.Level} < 74";
        }

        if (isMoving && actionDef is null)
        {
            context.Debug.DpsState = "Moving";
            HandleMovingDps(context, ref target, ref actionDef, dotStatusId);
        }
        else if (!isMoving && actionDef is null)
        {
            HandleStationaryDps(context, ref target, ref actionDef, dotStatusId);
        }

        // Update debug
        if (target is not null)
        {
            var dist = Vector3.Distance(player.Position, target.Position);
            context.Debug.TargetInfo = $"{target.Name?.TextValue ?? "Unknown"} ({dist:F1}y)";
        }
        else if (actionDef is not null && actionDef.TargetType == ActionTargetType.Self)
        {
            context.Debug.TargetInfo = "Self (AoE)";
        }
        else
        {
            context.Debug.TargetInfo = "None";
        }

        // No action to execute
        if (actionDef is null)
        {
            if (_hadTargetLastFrame)
            {
                context.ActionTracker.LogAttempt(0, null, null, Models.ActionResult.NoTarget, player.Level);
            }
            _hadTargetLastFrame = false;
            return;
        }

        // For targeted spells, require a target
        if (target is null && actionDef.TargetType != ActionTargetType.Self)
        {
            if (_hadTargetLastFrame)
            {
                context.ActionTracker.LogAttempt(0, null, null, Models.ActionResult.NoTarget, player.Level);
            }
            _hadTargetLastFrame = false;
            return;
        }

        _hadTargetLastFrame = true;

        // Execute
        var executionTarget = actionDef.TargetType == ActionTargetType.Self
            ? player.GameObjectId
            : target!.GameObjectId;
        var targetName = target?.Name?.TextValue ?? player.Name?.TextValue ?? "Unknown";
        var targetHp = target?.CurrentHp ?? player.CurrentHp;

        if (ActionExecutor.ExecuteGcd(context, actionDef, executionTarget, targetName, targetHp))
        {
            // Success - ActionExecutor handles debug state and logging
        }
    }

    private void HandleMovingDps(ApolloContext context, ref IBattleNpc? target, ref ActionDefinition? actionDef,
        uint dotStatusId)
    {
        var player = context.Player;
        var config = context.Configuration;

        // Priority 1: Sacred Sight instant Glare IV
        if (context.SacredSightStacks > 0 && player.Level >= WHMActions.GlareIV.MinLevel)
        {
            if (IsDamageSpellEnabled(WHMActions.GlareIV.ActionId, config))
            {
                var (aoeTarget, hitCount) = context.TargetingService.FindBestAoETarget(
                    WHMActions.GlareIV.Radius,
                    WHMActions.GlareIV.Range,
                    player);

                if (aoeTarget is not null)
                {
                    target = aoeTarget;
                    actionDef = WHMActions.GlareIV;
                    if (hitCount >= config.Damage.AoEDamageMinTargets)
                    {
                        context.Debug.DpsState = $"Glare IV AoE ({hitCount} targets, {context.SacredSightStacks} stacks)";
                    }
                    else
                    {
                        context.Debug.DpsState = $"Sacred Sight Glare IV ({context.SacredSightStacks} stacks)";
                    }
                }
            }
        }

        // Priority 2: Instant DoT spells (Dia at 72+)
        if (actionDef is null && player.Level >= 72)
        {
            target = context.TargetingService.FindEnemyNeedingDot(dotStatusId, FFXIVConstants.DotRefreshThreshold, WHMActions.Dia.Range, player);
            if (target is not null && IsDoTSpellEnabled(WHMActions.GetDotForLevel(player.Level).ActionId, config))
                actionDef = WHMActions.GetDotForLevel(player.Level);
        }
    }

    private void HandleStationaryDps(ApolloContext context, ref IBattleNpc? target, ref ActionDefinition? actionDef,
        uint dotStatusId)
    {
        var player = context.Player;
        var config = context.Configuration;

        // Priority 0.5: Sacred Sight Glare IV
        if (context.SacredSightStacks > 0 && player.Level >= WHMActions.GlareIV.MinLevel)
        {
            if (IsDamageSpellEnabled(WHMActions.GlareIV.ActionId, config))
            {
                var (aoeTarget, hitCount) = context.TargetingService.FindBestAoETarget(
                    WHMActions.GlareIV.Radius,
                    WHMActions.GlareIV.Range,
                    player);

                if (aoeTarget is not null)
                {
                    target = aoeTarget;
                    actionDef = WHMActions.GlareIV;
                    if (hitCount >= config.Damage.AoEDamageMinTargets)
                    {
                        context.Debug.DpsState = $"Glare IV AoE ({hitCount} targets, {context.SacredSightStacks} stacks)";
                    }
                    else
                    {
                        context.Debug.DpsState = $"Sacred Sight Glare IV ({context.SacredSightStacks} stacks)";
                    }
                }
            }
        }

        // DoT > AoE Damage > Single-target Damage
        if (actionDef is null)
        {
            target = context.TargetingService.FindEnemyNeedingDot(dotStatusId, FFXIVConstants.DotRefreshThreshold, WHMActions.Aero.Range, player);
            if (target is not null)
            {
                var dotAction = WHMActions.GetDotForLevel(player.Level);
                if (IsDoTSpellEnabled(dotAction.ActionId, config))
                {
                    actionDef = dotAction;
                    context.Debug.DpsState = "DoT target found";
                }
            }

            // Check for AoE damage opportunity (Holy family) - only when Sacred Sight not available
            if (actionDef is null && player.Level >= WHMActions.Holy.MinLevel && context.SacredSightStacks == 0)
            {
                var enemyCount = context.TargetingService.CountEnemiesInRange(WHMActions.Holy.Radius, player);
                context.Debug.AoEDpsEnemyCount = enemyCount;

                if (enemyCount >= config.Damage.AoEDamageMinTargets)
                {
                    var aoeDamageAction = WHMActions.GetAoEDamageGcdForLevel(player.Level);
                    if (aoeDamageAction is not null && IsAoEDamageSpellEnabled(aoeDamageAction.ActionId, config))
                    {
                        actionDef = aoeDamageAction;
                        context.Debug.DpsState = $"AoE: {aoeDamageAction.Name}";
                        context.Debug.AoEDpsState = $"{enemyCount} enemies in range";
                    }
                }
                else
                {
                    context.Debug.AoEDpsState = $"{enemyCount} < {config.Damage.AoEDamageMinTargets} min";
                }
            }

            // Fall back to single-target damage
            if (actionDef is null)
            {
                target = context.TargetingService.FindEnemy(config.Targeting.EnemyStrategy, WHMActions.Stone.Range, player);
                if (target is not null)
                {
                    var damageAction = WHMActions.GetDamageGcdForLevel(player.Level);
                    if (IsDamageSpellEnabled(damageAction.ActionId, config))
                    {
                        actionDef = damageAction;
                        context.Debug.DpsState = $"Damage: {damageAction.Name}";
                    }
                }
                else
                {
                    context.Debug.DpsState = "No enemy found";
                }
            }
        }
    }

    private static bool IsDamageSpellEnabled(uint actionId, Configuration config) =>
        DamageSpellEnabledMap.TryGetValue(actionId, out var check) && check(config);

    private static bool IsDoTSpellEnabled(uint actionId, Configuration config) =>
        DotSpellEnabledMap.TryGetValue(actionId, out var check) && check(config);

    private static bool IsAoEDamageSpellEnabled(uint actionId, Configuration config) =>
        AoEDamageSpellEnabledMap.TryGetValue(actionId, out var check) && check(config);
}
