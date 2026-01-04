using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models;
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
    private const float DotRefreshThreshold = 3f;

    // Action enable lookup maps
    private static readonly Dictionary<uint, Func<Configuration, bool>> DamageSpellEnabledMap = new()
    {
        { WHMActions.Stone.ActionId, c => c.EnableDamage && c.EnableStone },
        { WHMActions.StoneII.ActionId, c => c.EnableDamage && c.EnableStoneII },
        { WHMActions.StoneIII.ActionId, c => c.EnableDamage && c.EnableStoneIII },
        { WHMActions.StoneIV.ActionId, c => c.EnableDamage && c.EnableStoneIV },
        { WHMActions.Glare.ActionId, c => c.EnableDamage && c.EnableGlare },
        { WHMActions.GlareIII.ActionId, c => c.EnableDamage && c.EnableGlareIII },
        { WHMActions.GlareIV.ActionId, c => c.EnableDamage && c.EnableGlareIV },
        { WHMActions.AfflatusMisery.ActionId, c => c.EnableDamage && c.EnableAfflatusMisery },
    };

    private static readonly Dictionary<uint, Func<Configuration, bool>> DotSpellEnabledMap = new()
    {
        { WHMActions.Aero.ActionId, c => c.EnableDoT && c.EnableAero },
        { WHMActions.AeroII.ActionId, c => c.EnableDoT && c.EnableAeroII },
        { WHMActions.Dia.ActionId, c => c.EnableDoT && c.EnableDia },
    };

    private static readonly Dictionary<uint, Func<Configuration, bool>> AoEDamageSpellEnabledMap = new()
    {
        { WHMActions.Holy.ActionId, c => c.EnableDamage && c.EnableHoly },
        { WHMActions.HolyIII.ActionId, c => c.EnableDamage && c.EnableHolyIII },
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
        var bloodLilies = StatusHelper.GetBloodLilyCount();
        var sacredSightStacks = StatusHelper.GetSacredSightStacks(context.Player);
        context.Debug.BloodLilyCount = bloodLilies;
        context.Debug.SacredSightStacks = sacredSightStacks;
    }

    private void ExecuteDps(ApolloContext context, bool isMoving)
    {
        var player = context.Player;
        var config = context.Configuration;
        var dotStatusId = StatusHelper.GetDotStatusId(player.Level);

        IBattleNpc? target = null;
        ActionDefinition? actionDef = null;

        // Track Blood Lily and Sacred Sight for debug
        var bloodLilies = StatusHelper.GetBloodLilyCount();
        var sacredSightStacks = StatusHelper.GetSacredSightStacks(player);
        context.Debug.BloodLilyCount = bloodLilies;
        context.Debug.SacredSightStacks = sacredSightStacks;

        // Priority 0: Afflatus Misery (1240p AoE, costs 3 Blood Lily)
        if (bloodLilies >= 3 && player.Level >= WHMActions.AfflatusMisery.MinLevel)
        {
            if (IsDamageSpellEnabled(WHMActions.AfflatusMisery.ActionId, config))
            {
                target = context.TargetingService.FindEnemy(config.EnemyStrategy, WHMActions.AfflatusMisery.Range, player);
                if (target != null)
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
            context.Debug.MiseryState = bloodLilies < 3 ? $"{bloodLilies}/3 Blood Lily" : $"Level {player.Level} < 74";
        }

        if (isMoving && actionDef == null)
        {
            context.Debug.DpsState = "Moving";
            HandleMovingDps(context, ref target, ref actionDef, sacredSightStacks, dotStatusId);
        }
        else if (!isMoving && actionDef == null)
        {
            HandleStationaryDps(context, ref target, ref actionDef, sacredSightStacks, dotStatusId);
        }

        // Update debug
        if (target != null)
        {
            var dist = Vector3.Distance(player.Position, target.Position);
            context.Debug.TargetInfo = $"{target.Name?.TextValue ?? "Unknown"} ({dist:F1}y)";
        }
        else if (actionDef != null && actionDef.TargetType == ActionTargetType.Self)
        {
            context.Debug.TargetInfo = "Self (AoE)";
        }
        else
        {
            context.Debug.TargetInfo = "None";
        }

        // No action to execute
        if (actionDef == null)
        {
            if (_hadTargetLastFrame)
            {
                context.ActionTracker.LogAttempt(0, null, null, ActionResult.NoTarget, player.Level);
            }
            _hadTargetLastFrame = false;
            return;
        }

        // For targeted spells, require a target
        if (target == null && actionDef.TargetType != ActionTargetType.Self)
        {
            if (_hadTargetLastFrame)
            {
                context.ActionTracker.LogAttempt(0, null, null, ActionResult.NoTarget, player.Level);
            }
            _hadTargetLastFrame = false;
            return;
        }

        _hadTargetLastFrame = true;

        // Execute
        var executionTarget = actionDef.TargetType == ActionTargetType.Self
            ? player.GameObjectId
            : target!.GameObjectId;
        var success = context.ActionService.ExecuteGcd(actionDef, executionTarget);
        if (success)
        {
            context.Debug.PlannedAction = actionDef.Name;
            var targetName = target?.Name?.TextValue ?? player.Name?.TextValue ?? "Unknown";
            var targetHp = target?.CurrentHp ?? player.CurrentHp;
            context.ActionTracker.LogAttempt(actionDef.ActionId, targetName, targetHp, ActionResult.Success, player.Level);
        }
    }

    private void HandleMovingDps(ApolloContext context, ref IBattleNpc? target, ref ActionDefinition? actionDef,
        int sacredSightStacks, uint dotStatusId)
    {
        var player = context.Player;
        var config = context.Configuration;

        // Priority 1: Sacred Sight instant Glare IV
        if (sacredSightStacks > 0 && player.Level >= WHMActions.GlareIV.MinLevel)
        {
            if (IsDamageSpellEnabled(WHMActions.GlareIV.ActionId, config))
            {
                var (aoeTarget, hitCount) = context.TargetingService.FindBestAoETarget(
                    WHMActions.GlareIV.Radius,
                    WHMActions.GlareIV.Range,
                    player);

                if (aoeTarget != null)
                {
                    target = aoeTarget;
                    actionDef = WHMActions.GlareIV;
                    if (hitCount >= config.AoEDamageMinTargets)
                    {
                        context.Debug.DpsState = $"Glare IV AoE ({hitCount} targets, {sacredSightStacks} stacks)";
                    }
                    else
                    {
                        context.Debug.DpsState = $"Sacred Sight Glare IV ({sacredSightStacks} stacks)";
                    }
                }
            }
        }

        // Priority 2: Instant DoT spells (Dia at 72+)
        if (actionDef == null && player.Level >= 72)
        {
            target = context.TargetingService.FindEnemyNeedingDot(dotStatusId, DotRefreshThreshold, WHMActions.Dia.Range, player);
            if (target != null && IsDoTSpellEnabled(WHMActions.GetDotForLevel(player.Level).ActionId, config))
                actionDef = WHMActions.GetDotForLevel(player.Level);
        }
    }

    private void HandleStationaryDps(ApolloContext context, ref IBattleNpc? target, ref ActionDefinition? actionDef,
        int sacredSightStacks, uint dotStatusId)
    {
        var player = context.Player;
        var config = context.Configuration;

        // Priority 0.5: Sacred Sight Glare IV
        if (sacredSightStacks > 0 && player.Level >= WHMActions.GlareIV.MinLevel)
        {
            if (IsDamageSpellEnabled(WHMActions.GlareIV.ActionId, config))
            {
                var (aoeTarget, hitCount) = context.TargetingService.FindBestAoETarget(
                    WHMActions.GlareIV.Radius,
                    WHMActions.GlareIV.Range,
                    player);

                if (aoeTarget != null)
                {
                    target = aoeTarget;
                    actionDef = WHMActions.GlareIV;
                    if (hitCount >= config.AoEDamageMinTargets)
                    {
                        context.Debug.DpsState = $"Glare IV AoE ({hitCount} targets, {sacredSightStacks} stacks)";
                    }
                    else
                    {
                        context.Debug.DpsState = $"Sacred Sight Glare IV ({sacredSightStacks} stacks)";
                    }
                }
            }
        }

        // DoT > AoE Damage > Single-target Damage
        if (actionDef == null)
        {
            target = context.TargetingService.FindEnemyNeedingDot(dotStatusId, DotRefreshThreshold, WHMActions.Aero.Range, player);
            if (target != null)
            {
                var dotAction = WHMActions.GetDotForLevel(player.Level);
                if (IsDoTSpellEnabled(dotAction.ActionId, config))
                {
                    actionDef = dotAction;
                    context.Debug.DpsState = "DoT target found";
                }
            }

            // Check for AoE damage opportunity (Holy family) - only when Sacred Sight not available
            if (actionDef == null && player.Level >= WHMActions.Holy.MinLevel && sacredSightStacks == 0)
            {
                var enemyCount = context.TargetingService.CountEnemiesInRange(WHMActions.Holy.Radius, player);
                context.Debug.AoEDpsEnemyCount = enemyCount;

                if (enemyCount >= config.AoEDamageMinTargets)
                {
                    var aoeDamageAction = WHMActions.GetAoEDamageGcdForLevel(player.Level);
                    if (aoeDamageAction != null && IsAoEDamageSpellEnabled(aoeDamageAction.ActionId, config))
                    {
                        actionDef = aoeDamageAction;
                        context.Debug.DpsState = $"AoE: {aoeDamageAction.Name}";
                        context.Debug.AoEDpsState = $"{enemyCount} enemies in range";
                    }
                }
                else
                {
                    context.Debug.AoEDpsState = $"{enemyCount} < {config.AoEDamageMinTargets} min";
                }
            }

            // Fall back to single-target damage
            if (actionDef == null)
            {
                target = context.TargetingService.FindEnemy(config.EnemyStrategy, WHMActions.Stone.Range, player);
                if (target != null)
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
