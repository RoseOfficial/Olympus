using System;
using System.Collections.Generic;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// WHM-specific damage module (scheduler-driven).
/// Pushes DoT, AoE, and ST damage candidates plus Sacred Sight (Glare IV) and
/// Afflatus Misery special damage. Damage runs at the lowest priority.
/// </summary>
public sealed class DamageModule : BaseDamageModule<IApolloContext>, IApolloModule
{
    private static readonly string[] _afflatusMiseryFactors =
    {
        "Blood Lilies: 3/3 (Misery ready!)",
        "1240 potency AoE damage",
        "Instant cast",
        "Built from 3 Lily heals",
        "One of WHM's strongest damage skills",
    };

    private static readonly string[] _afflatusMiseryAlternatives =
    {
        "Nothing - always use Misery when ready",
        "Save for add spawn (if imminent)",
    };

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

    protected override bool IsDamageEnabled(IApolloContext context) => context.Configuration.EnableDamage;
    protected override bool IsDoTEnabled(IApolloContext context) => context.Configuration.EnableDoT;
    protected override bool IsAoEDamageEnabled(IApolloContext context) => context.Configuration.EnableDamage;
    protected override int AoEMinTargets(IApolloContext context) => context.Configuration.Damage.AoEDamageMinTargets;
    protected override float DoTRefreshThreshold(IApolloContext context) => FFXIVConstants.DotRefreshThreshold;

    protected override uint GetDoTStatusId(IApolloContext context)
    {
        if (context.Player.ClassJob.RowId == JobRegistry.Conjurer)
            return context.Player.Level >= 46 ? StatusHelper.StatusIds.AeroII : StatusHelper.StatusIds.Aero;
        return StatusHelper.GetDotStatusId(context.Player.Level);
    }

    protected override ActionDefinition? GetDoTAction(IApolloContext context)
    {
        if (context.Player.ClassJob.RowId == JobRegistry.Conjurer)
            return context.Player.Level >= WHMActions.AeroII.MinLevel ? WHMActions.AeroII :
                   context.Player.Level >= WHMActions.Aero.MinLevel ? WHMActions.Aero : null;
        return WHMActions.GetDotForLevel(context.Player.Level);
    }

    protected override ActionDefinition? GetAoEDamageAction(IApolloContext context) =>
        WHMActions.GetAoEDamageGcdForLevel(context.Player.Level);

    protected override ActionDefinition GetSingleTargetAction(IApolloContext context, bool isMoving)
    {
        if (context.Player.ClassJob.RowId == JobRegistry.Conjurer)
            return context.Player.Level >= WHMActions.StoneII.MinLevel ? WHMActions.StoneII : WHMActions.Stone;
        return WHMActions.GetDamageGcdForLevel(context.Player.Level);
    }

    protected override void SetDpsState(IApolloContext context, string state) => context.Debug.DpsState = state;
    protected override void SetAoEDpsState(IApolloContext context, string state) => context.Debug.AoEDpsState = state;
    protected override void SetAoEDpsEnemyCount(IApolloContext context, int count) => context.Debug.AoEDpsEnemyCount = count;
    protected override void SetPlannedAction(IApolloContext context, string action) => context.Debug.PlannedAction = action;

    protected override bool BlocksOnExecution => false;
    protected override bool CanDoT(IApolloContext context, bool isMoving) => true;

    protected override bool IsActionEnabled(IApolloContext context, ActionDefinition action)
    {
        var config = context.Configuration;
        if (DamageSpellEnabledMap.TryGetValue(action.ActionId, out var damageCheck)) return damageCheck(config);
        if (DotSpellEnabledMap.TryGetValue(action.ActionId, out var dotCheck)) return dotCheck(config);
        if (AoEDamageSpellEnabledMap.TryGetValue(action.ActionId, out var aoeCheck)) return aoeCheck(config);
        return true;
    }

    public override bool TryExecute(IApolloContext context, bool isMoving) => false;

    public new void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) return;
        if (context.TargetingService.IsDamageTargetingPaused()) { SetDpsState(context, "Paused (no target)"); return; }
        if (context.Configuration.Targeting.SuppressDamageOnForcedMovement
            && PlayerSafetyHelper.IsForcedMovementActive(context.Player))
        {
            SetDpsState(context, "Paused (forced movement)");
            return;
        }

        TryPushSpecialDamage(context, scheduler);
        TryPushDoT(context, scheduler, isMoving);
        TryPushAoEDamage(context, scheduler);
        TryPushSingleTargetDamage(context, scheduler, isMoving);
    }

    public override void UpdateDebugState(IApolloContext context)
    {
        context.Debug.LilyCount = context.LilyCount;
        context.Debug.BloodLilyCount = context.BloodLilyCount;
        context.Debug.LilyStrategy = context.Configuration.Healing.LilyStrategy.ToString();
        context.Debug.SacredSightStacks = context.SacredSightStacks;
    }

    private void TryPushSpecialDamage(IApolloContext context, RotationScheduler scheduler)
    {
        TryPushAfflatusMisery(context, scheduler);
        TryPushSacredSightGlare(context, scheduler);
    }

    private void TryPushAfflatusMisery(IApolloContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var config = context.Configuration;

        if (context.BloodLilyCount < 3) { context.Debug.MiseryState = $"{context.BloodLilyCount}/3 Blood Lily"; return; }
        if (player.Level < WHMActions.AfflatusMisery.MinLevel) { context.Debug.MiseryState = $"Level {player.Level} < 74"; return; }
        if (!IsActionEnabled(context, WHMActions.AfflatusMisery)) { context.Debug.MiseryState = "Disabled"; return; }

        var target = context.TargetingService.FindEnemy(config.Targeting.EnemyStrategy, WHMActions.AfflatusMisery.Range, player);
        if (target == null) { context.Debug.MiseryState = "No target"; return; }

        var capturedTarget = target;

        scheduler.PushGcd(ApolloAbilities.AfflatusMisery, target.GameObjectId, priority: 300,
            onDispatched: _ =>
            {
                context.Debug.DpsState = "Afflatus Misery";
                context.Debug.MiseryState = "Executing";
                SetPlannedAction(context, WHMActions.AfflatusMisery.Name);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.AfflatusMisery.ActionId,
                        ActionName = "Afflatus Misery",
                        Category = "Damage",
                        TargetName = targetName,
                        ShortReason = $"Afflatus Misery on {targetName} - 1240p AoE!",
                        DetailedReason = $"Afflatus Misery is WHM's strongest GCD damage skill at 1240 potency. Used on {targetName}. Always use Misery when available!",
                        Factors = _afflatusMiseryFactors,
                        Alternatives = _afflatusMiseryAlternatives,
                        Tip = "Never hold Misery too long - it's a huge DPS gain!",
                        ConceptId = WhmConcepts.AfflatusMiseryTiming,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }

    private void TryPushSacredSightGlare(IApolloContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var config = context.Configuration;

        if (context.SacredSightStacks == 0) return;
        if (player.Level < WHMActions.GlareIV.MinLevel) return;
        if (!IsActionEnabled(context, WHMActions.GlareIV)) return;

        var (aoeTarget, hitCount) = context.TargetingService.FindBestAoETarget(
            WHMActions.GlareIV.Radius, WHMActions.GlareIV.Range, player);
        if (aoeTarget == null) return;

        var capturedHitCount = hitCount;
        var capturedStacks = context.SacredSightStacks;

        scheduler.PushGcd(ApolloAbilities.GlareIV, aoeTarget.GameObjectId, priority: 305,
            onDispatched: _ =>
            {
                if (capturedHitCount >= config.Damage.AoEDamageMinTargets)
                    context.Debug.DpsState = $"Glare IV AoE ({capturedHitCount} targets, {capturedStacks} stacks)";
                else
                    context.Debug.DpsState = $"Sacred Sight Glare IV ({capturedStacks} stacks)";
                SetPlannedAction(context, WHMActions.GlareIV.Name);
            });
    }

    private void TryPushDoT(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!IsDoTEnabled(context)) return;

        var dotAction = GetDoTAction(context);
        if (dotAction == null) return;

        if (IsAoEDamageEnabled(context))
        {
            var aoeAction = GetAoEDamageAction(context);
            if (aoeAction != null)
            {
                var enemyCount = context.TargetingService.CountEnemiesInRange(aoeAction.Radius, context.Player);
                if (enemyCount >= AoEMinTargets(context)) { SetDpsState(context, $"DoT: skipped ({enemyCount} enemies, AoE preferred)"); return; }
            }
        }

        var dotCastTime = context.HasSwiftcast ? 0f : dotAction.CastTime;
        if (MechanicCastGate.ShouldBlock(context, dotCastTime)) { SetDpsState(context, "DoT: mechanic imminent"); return; }

        var dotStatusId = GetDoTStatusId(context);
        if (dotStatusId == 0) return;

        var target = context.TargetingService.FindEnemyNeedingDot(dotStatusId, DoTRefreshThreshold(context), dotAction.Range, context.Player);
        if (target == null) { SetDpsState(context, "DoT: no target"); return; }

        if (!IsActionEnabled(context, dotAction)) return;

        var capturedAction = dotAction;
        var behavior = new AbilityBehavior { Action = dotAction };

        scheduler.PushGcd(behavior, target.GameObjectId, priority: 310,
            onDispatched: _ =>
            {
                SetPlannedAction(context, capturedAction.Name);
                SetDpsState(context, "DoT");
            });
    }

    private void TryPushAoEDamage(IApolloContext context, RotationScheduler scheduler)
    {
        if (!IsAoEDamageEnabled(context)) return;

        if (context.SacredSightStacks > 0) return;

        var aoeAction = GetAoEDamageAction(context);
        if (aoeAction == null) return;
        if (!IsActionEnabled(context, aoeAction)) return;

        var aoeCastTime = context.HasSwiftcast ? 0f : aoeAction.CastTime;
        if (MechanicCastGate.ShouldBlock(context, aoeCastTime)) { SetAoEDpsState(context, "Holding: mechanic imminent"); return; }

        var enemyCount = context.TargetingService.CountEnemiesInRange(aoeAction.Radius, context.Player);
        SetAoEDpsEnemyCount(context, enemyCount);
        if (enemyCount < AoEMinTargets(context)) { SetAoEDpsState(context, $"{enemyCount} < {AoEMinTargets(context)} min"); return; }

        var targetId = aoeAction.TargetType == ActionTargetType.Self
            ? context.Player.GameObjectId
            : FindBestAoETarget(context, aoeAction);
        if (targetId == 0) return;

        var capturedAction = aoeAction;
        var capturedEnemyCount = enemyCount;
        var behavior = new AbilityBehavior { Action = aoeAction };

        scheduler.PushGcd(behavior, targetId, priority: 320,
            onDispatched: _ =>
            {
                SetPlannedAction(context, capturedAction.Name);
                SetDpsState(context, $"AoE ({capturedEnemyCount} targets)");
                SetAoEDpsState(context, $"{capturedEnemyCount} enemies");
            });
    }

    private void TryPushSingleTargetDamage(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!IsDamageEnabled(context)) { SetDpsState(context, "Damage disabled"); return; }

        var action = GetSingleTargetAction(context, isMoving);
        if (!IsActionEnabled(context, action)) { SetDpsState(context, $"Action disabled: {action.Name}"); return; }

        var stCastTime = context.HasSwiftcast ? 0f : action.CastTime;
        if (MechanicCastGate.ShouldBlock(context, stCastTime)) { SetDpsState(context, "Holding: mechanic imminent"); return; }

        var target = context.TargetingService.FindEnemy(context.Configuration.Targeting.EnemyStrategy, action.Range, context.Player);
        if (target == null) { SetDpsState(context, "No enemy found"); return; }

        var capturedAction = action;
        var behavior = new AbilityBehavior { Action = action };

        scheduler.PushGcd(behavior, target.GameObjectId, priority: 330,
            onDispatched: _ =>
            {
                SetPlannedAction(context, capturedAction.Name);
                SetDpsState(context, capturedAction.Name);
            });
    }
}
